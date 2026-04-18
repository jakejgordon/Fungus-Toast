using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Campaign;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Orchestrates active campaign lifecycle and exposes current level spec.
    /// </summary>
    public class CampaignController
    {
        private readonly CampaignProgression progression;
        public CampaignState State { get; private set; }

        public CampaignController(CampaignProgression progression)
        {
            this.progression = progression;
        }

        public bool HasActiveRun => State != null;
        public CampaignProgression.LevelSpec CurrentLevelSpec => (State != null && State.levelIndex < progression.MaxLevels) ? progression.Get(State.levelIndex) : null;
        public BoardPreset CurrentBoardPreset
        {
            get
            {
                var spec = CurrentLevelSpec;
                if (spec == null) return null;
                return FindOrResolveBoardPreset(spec, State.levelIndex);
            }
        }
        public IReadOnlyList<string> CurrentResolvedAiStrategyNames => State != null ? State.resolvedAiStrategyNames : Array.Empty<string>();
        public bool IsAwaitingAdaptationSelection => State != null && State.pendingAdaptationSelection;
        public bool IsCompleted => State != null && State.campaignCompleted;
        public CampaignVictorySnapshot PendingVictorySnapshot => State?.pendingVictorySnapshot;
        public int HumanMoldIndex => State != null ? State.humanMoldIndex : 0;
        public MoldinessProgressSnapshot MoldinessProgress => MoldinessProgression.GetSnapshot(State?.moldiness);
        public bool HasPendingMoldinessUnlockChoice => State?.moldiness?.pendingUnlockTriggers?.Count > 0;

        public void StartNew(int humanMoldIndex = 0)
        {
            if (progression.MaxLevels == 0) throw new InvalidOperationException("CampaignProgression has no levels defined.");
            var firstSpec = progression.Get(0);
            // Seed must be set before ResolveBoardPreset can use it for boss pool selection.
            int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            State = new CampaignState { seed = newSeed };
            var preset = ResolveBoardPreset(firstSpec, 0);
            if (preset == null) throw new InvalidOperationException("Level0 has no BoardPreset assigned.");
            State = new CampaignState
            {
                runId = Guid.NewGuid().ToString(),
                levelIndex = 0,
                boardPresetId = preset.presetId,
                seed = newSeed,
                boardWidth = preset.boardWidth,
                boardHeight = preset.boardHeight,
                humanMoldIndex = Mathf.Max(0, humanMoldIndex),
                pendingAdaptationSelection = false,
                campaignCompleted = false,
                pendingVictorySnapshot = null,
                resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, 0),
                moldiness = MoldinessProgression.CreateDefaultState()
            };
            var startingAdaptId = MoldCatalog.GetStartingAdaptationId(humanMoldIndex);
            if (!string.IsNullOrEmpty(startingAdaptId))
            {
                State.selectedAdaptationIds ??= new List<string>();
                State.selectedAdaptationIds.Add(startingAdaptId);
            }
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] New campaign started. RunId={State.runId} Preset={preset.presetId}");
        }

        public void Resume()
        {
            var loaded = CampaignSaveService.Load();
            if (loaded == null)
            {
                Debug.LogWarning("[CampaignController] Resume requested but no save exists. Starting new.");
                StartNew();
                return;
            }
            State = loaded;
            State.selectedAdaptationIds ??= new List<string>();
            State.resolvedAiStrategyNames ??= new List<string>();
            State.moldiness ??= MoldinessProgression.CreateDefaultState();
            State.moldiness.pendingUnlockTriggers ??= new List<MoldinessUnlockTrigger>();
            State.moldiness.unlockedContentIds ??= new List<string>();
            if (!State.pendingAdaptationSelection)
            {
                State.pendingVictorySnapshot = null;
            }

            EnsureResolvedAiLineup();
            Debug.Log($"[CampaignController] Resumed campaign RunId={State.runId} Level={State.levelIndex} PresetId={State.boardPresetId}");
        }

        public bool TryGetPendingVictorySnapshot(out CampaignVictorySnapshot snapshot)
        {
            snapshot = null;
            if (State == null || !State.pendingAdaptationSelection || State.pendingVictorySnapshot == null)
            {
                return false;
            }

            snapshot = State.pendingVictorySnapshot;
            return true;
        }

        public void SetPendingVictorySnapshot(CampaignVictorySnapshot snapshot)
        {
            if (State == null)
            {
                return;
            }

            State.pendingVictorySnapshot = snapshot;
            CampaignSaveService.Save(State);
        }

        public void Delete()
        {
            CampaignSaveService.Delete();
            State = null;
        }

        /// <summary>
        /// Called by GameManager when a campaign level ends. Handles victory progression or defeat reset.
        /// </summary>
        public void OnGameFinished(bool victory)
        {
            if (State == null)
            {
                return;
            }
            if (!victory)
            {
                ResetRunAfterDefeat();
                return;
            }

            // Victory path
            int clearedLevelDisplay = State.levelIndex + 1;
            var moldinessAward = MoldinessProgression.AwardForLevelClear(State.moldiness, clearedLevelDisplay);
            LogMoldinessAward(clearedLevelDisplay, moldinessAward);

            int nextIndex = State.levelIndex + 1;
            if (nextIndex >= progression.MaxLevels)
            {
                // Final victory.
                State.pendingAdaptationSelection = false;
                State.campaignCompleted = true;
                State.pendingVictorySnapshot = null;
                CampaignSaveService.Save(State);
                Debug.Log($"[CampaignController] Campaign completed! RunId={State.runId} Levels={progression.MaxLevels}");
                return;
            }

            // Mid-run victory: wait for adaptation pick before advancing.
            State.pendingAdaptationSelection = true;
            CampaignSaveService.Save(State);
        }

        public List<AdaptationDefinition> GetAdaptationDraftChoices(System.Random random, int count, string forcedAdaptationId = "")
        {
            if (State == null)
            {
                return new List<AdaptationDefinition>();
            }

            var selected = new HashSet<string>(State.selectedAdaptationIds ?? new List<string>(), StringComparer.Ordinal);
            var remaining = AdaptationRepository.All
                .Where(x => !x.IsStartingAdaptation)
                .Where(x => !selected.Contains(x.Id))
                .ToList();

            if (remaining.Count == 0)
            {
                return remaining;
            }

            AdaptationDefinition forcedAdaptation = null;
            if (!string.IsNullOrWhiteSpace(forcedAdaptationId))
            {
                forcedAdaptation = remaining.FirstOrDefault(x => string.Equals(x.Id, forcedAdaptationId, StringComparison.Ordinal));
            }

            if (count <= 0 || count >= remaining.Count)
            {
                return remaining;
            }

            // Fisher-Yates shuffle then take N for stable uniqueness without duplicates.
            for (int i = remaining.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (remaining[i], remaining[swapIndex]) = (remaining[swapIndex], remaining[i]);
            }

            if (forcedAdaptation != null)
            {
                remaining.RemoveAll(x => string.Equals(x.Id, forcedAdaptation.Id, StringComparison.Ordinal));

                var forcedChoices = remaining.Take(Math.Max(0, count - 1)).ToList();
                forcedChoices.Add(forcedAdaptation);

                for (int i = forcedChoices.Count - 1; i > 0; i--)
                {
                    int swapIndex = random.Next(i + 1);
                    (forcedChoices[i], forcedChoices[swapIndex]) = (forcedChoices[swapIndex], forcedChoices[i]);
                }

                return forcedChoices;
            }

            return remaining.Take(count).ToList();
        }

        public List<MoldinessUnlockDefinition> GetPendingMoldinessUnlockOffers(System.Random random, int count)
        {
            if (State?.moldiness == null || State.moldiness.pendingUnlockTriggers == null || State.moldiness.pendingUnlockTriggers.Count == 0)
            {
                return new List<MoldinessUnlockDefinition>();
            }

            return MoldinessUnlockService.GenerateOffers(State.moldiness, random, count);
        }

        public bool TryApplyMoldinessUnlock(string unlockId)
        {
            if (State?.moldiness == null)
            {
                return false;
            }

            var result = MoldinessUnlockService.ApplyUnlockChoice(State.moldiness, unlockId);
            if (!result.Applied)
            {
                return false;
            }

            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Applied Moldiness unlock '{result.Definition.Id}' ({result.Definition.ContentId}).");
            return true;
        }

        public IReadOnlyList<AdaptationDefinition> GetSelectedAdaptations()
        {
            if (State?.selectedAdaptationIds == null || State.selectedAdaptationIds.Count == 0)
            {
                return Array.Empty<AdaptationDefinition>();
            }

            var selected = new List<AdaptationDefinition>();
            foreach (var adaptationId in State.selectedAdaptationIds)
            {
                if (AdaptationRepository.TryGetById(adaptationId, out var adaptation))
                {
                    selected.Add(adaptation);
                }
            }

            return selected;
        }

        public bool TrySelectAdaptationAndAdvance(string adaptationId)
        {
            if (State == null || !State.pendingAdaptationSelection)
            {
                return false;
            }

            if (!AdaptationRepository.TryGetById(adaptationId, out _))
            {
                Debug.LogWarning($"[CampaignController] Unknown adaptation id '{adaptationId}'.");
                return false;
            }

            if (State.selectedAdaptationIds.Contains(adaptationId))
            {
                Debug.LogWarning($"[CampaignController] Adaptation '{adaptationId}' already selected this run.");
                return false;
            }

            State.selectedAdaptationIds.Add(adaptationId);
            State.pendingAdaptationSelection = false;
            State.pendingVictorySnapshot = null;
            AdvanceToNextLevel();
            return true;
        }

        public bool TryAdvanceWithoutAdaptationReward()
        {
            if (State == null || !State.pendingAdaptationSelection)
            {
                return false;
            }

            State.pendingAdaptationSelection = false;
            State.pendingVictorySnapshot = null;
            AdvanceToNextLevel();
            return true;
        }

        /// <summary>
        /// Advance to the next level (mid-run victory).
        /// </summary>
        private void AdvanceToNextLevel()
        {
            int targetIndex = State.levelIndex + 1;
            if (targetIndex >= progression.MaxLevels)
            {
                Debug.LogWarning("[CampaignController] AdvanceToNextLevel called but already at final level.");
                return;
            }
            var spec = progression.Get(targetIndex);
            var preset = ResolveBoardPreset(spec, targetIndex);
            if (preset == null)
            {
                Debug.LogError($"[CampaignController] Level {targetIndex} has no BoardPreset – aborting advance.");
                return;
            }
            if (spec.HasBossPool)
            {
                Debug.Log($"[CampaignController] Boss level {targetIndex}: selected preset {preset.presetId} from pool of {spec.bossBoardPresets.Count}");
            }
            State.levelIndex = targetIndex;
            State.boardPresetId = preset.presetId;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            State.campaignCompleted = false;
            State.resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, targetIndex);
            // Seed retained across victories for reproducibility
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Advanced to level {State.levelIndex}. Preset={preset.presetId}");
        }

        /// <summary>
        /// Reset the run after defeat (player returns to mode select with fresh level0 state).
        /// </summary>
        private void ResetRunAfterDefeat()
        {
            if (progression.MaxLevels == 0)
            {
                Debug.LogError("[CampaignController] Cannot reset campaign – no levels defined.");
                return;
            }
            var firstSpec = progression.Get(0);
            if (firstSpec.boardPreset == null)
            {
                Debug.LogError("[CampaignController] Cannot reset campaign – Level0 preset missing.");
                return;
            }
            var preset = firstSpec.boardPreset;
            State.runId = Guid.NewGuid().ToString();
            State.levelIndex = 0;
            State.selectedAdaptationIds.Clear();
            State.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            State.moldiness ??= MoldinessProgression.CreateDefaultState();
            State.boardPresetId = preset.presetId;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            State.pendingAdaptationSelection = false;
            State.campaignCompleted = false;
            State.pendingVictorySnapshot = null;
            State.resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, 0);
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Run reset after defeat. New RunId={State.runId} Preset={preset.presetId}");
        }

        private static void LogMoldinessAward(int clearedLevelDisplay, MoldinessAwardResult award)
        {
            if (award.AmountAwarded <= 0)
            {
                Debug.Log($"[CampaignController] Moldiness: cleared level {clearedLevelDisplay}, no reward awarded.");
                return;
            }

            Debug.Log($"[CampaignController] Moldiness: cleared level {clearedLevelDisplay}, +{award.AmountAwarded}, progress {award.PreviousProgress}->{award.NewProgress}/{award.CurrentThreshold}, tier {award.PreviousTierIndex}->{award.NewTierIndex}, lifetime {award.LifetimeEarned}.");

            if (!award.TriggeredUnlock)
            {
                return;
            }

            for (int i = 0; i < award.UnlockTriggers.Count; i++)
            {
                var trigger = award.UnlockTriggers[i];
                Debug.Log($"[CampaignController] Moldiness unlock triggered: tier {trigger.tierIndex + 1}, threshold {trigger.threshold}, overflow {trigger.overflowAfterUnlock}.");
            }
        }

        public int GetCurrentAiPlayerCount()
        {
            if (State?.resolvedAiStrategyNames != null && State.resolvedAiStrategyNames.Count > 0)
            {
                return State.resolvedAiStrategyNames.Count;
            }

            return CurrentBoardPreset?.GetConfiguredAiPlayerCount() ?? 0;
        }

        private void EnsureResolvedAiLineup()
        {
            var spec = CurrentLevelSpec;
            if (spec == null) return;

            var preset = FindOrResolveBoardPreset(spec, State.levelIndex);
            if (preset == null)
            {
                return;
            }

            State.resolvedAiStrategyNames ??= new List<string>();
            if (State.resolvedAiStrategyNames.Count > 0)
            {
                return;
            }

            State.resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, State.levelIndex);
            CampaignSaveService.Save(State);
        }

        private BoardPreset ResolveBoardPreset(CampaignProgression.LevelSpec spec, int levelIndex)
        {
            if (spec.HasBossPool)
            {
                int seed = unchecked((State.seed * 397) ^ levelIndex);
                var random = new System.Random(seed);
                int idx = random.Next(spec.bossBoardPresets.Count);
                return spec.bossBoardPresets[idx];
            }
            return spec.boardPreset;
        }

        private BoardPreset FindOrResolveBoardPreset(CampaignProgression.LevelSpec spec, int levelIndex)
        {
            if (!string.IsNullOrEmpty(State?.boardPresetId))
            {
                if (spec.HasBossPool)
                {
                    var match = spec.bossBoardPresets.FirstOrDefault(p => p != null && p.presetId == State.boardPresetId);
                    if (match != null) return match;
                }
                if (spec.boardPreset != null && spec.boardPreset.presetId == State.boardPresetId)
                    return spec.boardPreset;
            }
            return ResolveBoardPreset(spec, levelIndex);
        }

        private List<string> BuildResolvedAiStrategyNames(BoardPreset preset, int levelIndex)
        {
            var resolved = new List<string>();
            if (preset == null)
            {
                return resolved;
            }

            if (preset.aiPlayers != null && preset.aiPlayers.Count > 0)
            {
                foreach (var aiSpec in preset.aiPlayers)
                {
                    if (aiSpec != null && !string.IsNullOrWhiteSpace(aiSpec.strategyName))
                    {
                        resolved.Add(aiSpec.strategyName);
                    }
                }

                return resolved;
            }

            if (preset.aiStrategyPool == null || preset.aiStrategyPool.Count == 0 || preset.pooledAiPlayerCount <= 0)
            {
                return resolved;
            }

            var uniqueEligible = preset.aiStrategyPool
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .Where(IsKnownCampaignStrategyName)
                .ToList();

            if (uniqueEligible.Count == 0)
            {
                Debug.LogWarning($"[CampaignController] Preset '{preset.presetId}' has an AI pool but no resolvable strategies.");
                return resolved;
            }

            int desiredCount = Mathf.Min(preset.pooledAiPlayerCount, uniqueEligible.Count);
            int presetHash = GetStableStringHash(preset.presetId);
            int seed = State != null
                ? unchecked((State.seed * 397) ^ levelIndex ^ presetHash)
                : unchecked(levelIndex ^ presetHash);
            var random = new System.Random(seed);
            var shuffled = uniqueEligible.OrderBy(_ => random.Next()).ToList();
            resolved.AddRange(shuffled.Take(desiredCount));
            return resolved;
        }

        private static bool IsKnownCampaignStrategyName(string strategyName)
        {
            return AIRoster.CampaignStrategiesByName.ContainsKey(strategyName)
                || AIRoster.ProvenStrategiesByName.ContainsKey(strategyName);
        }

        private static int GetStableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                foreach (char c in value)
                {
                    hash = (hash * 31) + c;
                }

                return hash;
            }
        }
    }
}
