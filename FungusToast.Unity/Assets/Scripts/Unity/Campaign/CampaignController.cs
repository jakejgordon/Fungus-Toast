using System;
using System.Collections.Generic;
using System.Linq;
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
        public BoardPreset CurrentBoardPreset => CurrentLevelSpec?.boardPreset;
        public bool IsAwaitingAdaptationSelection => State != null && State.pendingAdaptationSelection;
        public bool IsCompleted => State != null && State.campaignCompleted;
        public CampaignVictorySnapshot PendingVictorySnapshot => State?.pendingVictorySnapshot;

        public void StartNew()
        {
            if (progression.MaxLevels == 0) throw new InvalidOperationException("CampaignProgression has no levels defined.");
            var firstSpec = progression.Get(0);
            var preset = firstSpec.boardPreset;
            if (preset == null) throw new InvalidOperationException("Level0 has no BoardPreset assigned.");
            State = new CampaignState
            {
                runId = Guid.NewGuid().ToString(),
                levelIndex = 0,
                unlockedMutationTierMax = preset.mutationTierMax,
                boardPresetId = preset.presetId,
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                boardWidth = preset.boardWidth,
                boardHeight = preset.boardHeight,
                pendingAdaptationSelection = false,
                campaignCompleted = false,
                pendingVictorySnapshot = null
            };
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
            if (!State.pendingAdaptationSelection)
            {
                State.pendingVictorySnapshot = null;
            }
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

        public List<AdaptationDefinition> GetAdaptationDraftChoices(System.Random random, int count)
        {
            if (State == null)
            {
                return new List<AdaptationDefinition>();
            }

            var selected = new HashSet<string>(State.selectedAdaptationIds ?? new List<string>(), StringComparer.Ordinal);
            var remaining = AdaptationRepository.All
                .Where(x => !selected.Contains(x.Id))
                .ToList();

            if (remaining.Count == 0)
            {
                return remaining;
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

            return remaining.Take(count).ToList();
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
            if (spec.boardPreset == null)
            {
                Debug.LogError($"[CampaignController] Level {targetIndex} has no BoardPreset – aborting advance.");
                return;
            }
            var preset = spec.boardPreset;
            State.levelIndex = targetIndex;
            State.boardPresetId = preset.presetId;
            State.unlockedMutationTierMax = preset.mutationTierMax;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            State.campaignCompleted = false;
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
            State.boardPresetId = preset.presetId;
            State.unlockedMutationTierMax = preset.mutationTierMax;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            State.pendingAdaptationSelection = false;
            State.campaignCompleted = false;
            State.pendingVictorySnapshot = null;
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Run reset after defeat. New RunId={State.runId} Preset={preset.presetId}");
        }
    }
}
