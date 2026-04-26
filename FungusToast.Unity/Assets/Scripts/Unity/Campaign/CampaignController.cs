using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Persistence;
using FungusToast.Core.Players;
using FungusToast.Unity.Save;
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
        public bool HasResumableRun => State != null && !State.requiresNewCampaignStart;
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
        public bool IsAwaitingDefeatCarryoverSelection => State != null && State.pendingDefeatCarryoverSelection;
        public bool IsCompleted => State != null && State.campaignCompleted;
        public CampaignVictorySnapshot PendingVictorySnapshot => State?.pendingVictorySnapshot;
        public int HumanMoldIndex => State != null ? State.humanMoldIndex : 0;
        public int CurrentLevelGameplaySeed => State != null ? State.currentLevelGameplaySeed : 0;
        public MoldinessProgressSnapshot MoldinessProgress => MoldinessProgression.GetSnapshot(State?.moldiness);
        public bool HasPendingMoldinessUnlockChoice => State?.moldiness?.pendingUnlockTriggers?.Count > 0;
        public bool HasSavedGameplayCheckpoint => State?.hasInLevelGameplayCheckpoint == true;
        public bool HasUnlockedCampaignAdaptationDraftRedraw => HasUnlockedMoldinessReward(MoldinessUnlockCatalog.SporeSiftingRewardId);
        public bool CanUsePendingAdaptationDraftRedraw => State != null
            && State.pendingAdaptationSelection
            && HasUnlockedCampaignAdaptationDraftRedraw
            && !State.pendingAdaptationDraftRedrawUsed
            && (State.pendingAdaptationDraftChoiceIds?.Count ?? 0) > 0;

        public bool HasUnlockedMoldinessReward(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId)
                && State?.moldiness?.unlockedRewardIds?.Any(id => string.Equals(id, unlockId, StringComparison.Ordinal)) == true;
        }

        public List<Mycovariant> GetEligibleMycovariantsForCampaignDraft(IEnumerable<Mycovariant> allMycovariants)
        {
            if (allMycovariants == null)
            {
                return new List<Mycovariant>();
            }

            var permanentlyUnlockedMycovariants = new HashSet<int>(State?.moldiness?.unlockedMycovariantIds ?? new List<int>());
            var currentUnlockLevel = State?.moldiness?.unlockLevel ?? 0;

            return allMycovariants
                .Where(x => !x.IsLocked || (x.RequiredMoldinessUnlockLevel <= currentUnlockLevel && permanentlyUnlockedMycovariants.Contains(x.Id)))
                .ToList();
        }

        public void StartNew(int humanMoldIndex = 0, int? levelIndexOverride = null, IReadOnlyList<string> temporaryTestingAdaptationIds = null)
        {
            if (progression.MaxLevels == 0) throw new InvalidOperationException("CampaignProgression has no levels defined.");

            int targetLevelIndex = Mathf.Clamp(levelIndexOverride ?? 0, 0, progression.MaxLevels - 1);
            bool isLevelOverrideRun = targetLevelIndex > 0;
            var previousState = isLevelOverrideRun ? null : State ?? CampaignSaveService.Load();
            var carryoverAdaptationIds = isLevelOverrideRun
                ? new List<string>()
                : GetCarryoverAdaptationIdsForNewCampaignStart(previousState);
            var persistentMoldinessState = previousState?.moldiness ?? MoldinessProgression.CreateDefaultState();
            MoldinessUnlockService.NormalizeProgressionState(persistentMoldinessState);
            var targetSpec = progression.Get(targetLevelIndex);
            int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            State = new CampaignState { seed = newSeed };
            var preset = ResolveBoardPreset(targetSpec, targetLevelIndex);
            if (preset == null) throw new InvalidOperationException($"Campaign level {targetLevelIndex} has no BoardPreset assigned.");
            State = new CampaignState
            {
                runId = Guid.NewGuid().ToString(),
                levelIndex = targetLevelIndex,
                boardPresetId = preset.presetId,
                seed = newSeed,
                boardWidth = preset.boardWidth,
                boardHeight = preset.boardHeight,
                humanMoldIndex = Mathf.Max(0, humanMoldIndex),
                pendingAdaptationSelection = false,
                pendingAdaptationDraftChoiceIds = new List<string>(),
                pendingAdaptationDraftRedrawUsed = false,
                campaignCompleted = false,
                pendingVictorySnapshot = null,
                pendingDefeatCarryoverSelection = false,
                pendingDefeatCarryoverOptions = new List<string>(),
                pendingNextRunCarryoverAdaptationIds = new List<string>(),
                requiresNewCampaignStart = false,
                resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, targetLevelIndex),
                temporaryTestingAdaptationIds = SanitizeTemporaryTestingAdaptationIds(temporaryTestingAdaptationIds),
                moldiness = persistentMoldinessState,
                currentLevelGameplaySeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                hasInLevelGameplayCheckpoint = false,
                inLevelRuntimeSnapshot = null,
                inLevelRandomState = null
            };
            State.selectedAdaptationIds ??= new List<string>();
            foreach (var carryoverAdaptationId in carryoverAdaptationIds)
            {
                if (!State.selectedAdaptationIds.Contains(carryoverAdaptationId))
                {
                    State.selectedAdaptationIds.Add(carryoverAdaptationId);
                }
            }

            var startingAdaptId = MoldCatalog.GetStartingAdaptationId(humanMoldIndex);
            if (!string.IsNullOrEmpty(startingAdaptId))
            {
                if (!State.selectedAdaptationIds.Contains(startingAdaptId))
                {
                    State.selectedAdaptationIds.Insert(0, startingAdaptId);
                }
            }
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] New campaign started. RunId={State.runId} Level={targetLevelIndex} Preset={preset.presetId}");
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
            State.pendingDefeatCarryoverOptions ??= new List<string>();
            State.pendingNextRunCarryoverAdaptationIds ??= new List<string>();
            State.pendingAdaptationDraftChoiceIds ??= new List<string>();
            State.temporaryTestingAdaptationIds ??= new List<string>();
            State.moldiness ??= MoldinessProgression.CreateDefaultState();
            bool normalizedMoldinessState = MoldinessUnlockService.NormalizeProgressionState(State.moldiness);
            bool normalizedPendingAdaptationDraftState = NormalizePendingAdaptationDraftState();
            if (!State.requiresNewCampaignStart)
            {
                RestorePendingNextRunCarryoverToSelectedAdaptations();
            }

            SanitizePendingDefeatCarryoverOptions();
            if (!State.pendingAdaptationSelection)
            {
                State.pendingVictorySnapshot = null;
            }

            EnsureResolvedAiLineup();
            if (normalizedMoldinessState || normalizedPendingAdaptationDraftState)
            {
                CampaignSaveService.Save(State);
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

        public bool TryGetPendingMoldinessRewardSnapshot(out CampaignVictorySnapshot snapshot)
        {
            snapshot = null;
            if (State?.moldiness?.pendingUnlockTriggers == null || State.moldiness.pendingUnlockTriggers.Count == 0)
            {
                return false;
            }

            if (State.pendingVictorySnapshot != null)
            {
                State.pendingVictorySnapshot.pendingMoldinessUnlockCount = State.moldiness.pendingUnlockTriggers.Count;
                snapshot = State.pendingVictorySnapshot;
                return true;
            }

            var moldinessSnapshot = MoldinessProgress;
            snapshot = new CampaignVictorySnapshot
            {
                clearedLevelDisplay = Math.Max(1, State.levelIndex + 1),
                moldinessAwarded = 0,
                moldinessProgressBeforeAward = moldinessSnapshot.CurrentProgress,
                moldinessProgressAfterAward = moldinessSnapshot.CurrentProgress,
                moldinessThresholdAfterAward = moldinessSnapshot.CurrentThreshold,
                moldinessTierBeforeAward = moldinessSnapshot.CurrentTierIndex,
                moldinessTierAfterAward = moldinessSnapshot.CurrentTierIndex,
                pendingMoldinessUnlockCount = State.moldiness.pendingUnlockTriggers.Count,
                rows = new List<CampaignVictoryPlayerRow>()
            };
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

        public void SaveGameplayCheckpoint(RoundStartRuntimeSnapshot snapshot, RandomStateSnapshot randomState, int gameplaySeed)
        {
            if (State == null)
            {
                return;
            }

            State.currentLevelGameplaySeed = gameplaySeed;
            State.hasInLevelGameplayCheckpoint = true;
            State.inLevelRuntimeSnapshot = snapshot;
            State.inLevelRandomState = randomState;
            CampaignSaveService.Save(State);
        }

        public bool TryGetGameplayCheckpoint(out RoundStartRuntimeSnapshot snapshot, out RandomStateSnapshot randomState)
        {
            snapshot = null;
            randomState = null;
            if (State == null || !State.hasInLevelGameplayCheckpoint || State.inLevelRuntimeSnapshot == null)
            {
                return false;
            }

            snapshot = State.inLevelRuntimeSnapshot;
            randomState = State.inLevelRandomState;

            if (!IsValidGameplayCheckpoint(snapshot))
            {
                Debug.LogWarning("[CampaignController] Ignoring invalid saved gameplay checkpoint and starting the level fresh.");
                ClearGameplayCheckpoint();
                snapshot = null;
                randomState = null;
                return false;
            }

            return true;
        }

        public void ClearGameplayCheckpoint(bool saveAfterClear = true)
        {
            if (State == null)
            {
                return;
            }

            State.hasInLevelGameplayCheckpoint = false;
            State.inLevelRuntimeSnapshot = null;
            State.inLevelRandomState = null;
            if (saveAfterClear)
            {
                CampaignSaveService.Save(State);
            }
        }

        private static bool IsValidGameplayCheckpoint(RoundStartRuntimeSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            if (snapshot.BoardWidth <= 0 || snapshot.BoardHeight <= 0)
            {
                return false;
            }

            if (snapshot.Players == null || snapshot.Players.Count == 0)
            {
                return false;
            }

            return snapshot.Players.Any(player => player != null && player.PlayerType == PlayerTypeEnum.Human);
        }

        public void Delete()
        {
            CampaignSaveService.Delete();
            State = null;
        }

        public bool ResetMoldinessProgression()
        {
            if (State == null)
            {
                State = CampaignSaveService.Load();
            }

            if (State == null)
            {
                return false;
            }

            State.selectedAdaptationIds ??= new List<string>();
            State.pendingDefeatCarryoverOptions ??= new List<string>();
            State.pendingNextRunCarryoverAdaptationIds ??= new List<string>();
            State.resolvedAiStrategyNames ??= new List<string>();
            State.temporaryTestingAdaptationIds ??= new List<string>();
            State.moldiness = MoldinessProgression.CreateDefaultState();
            State.pendingNextRunCarryoverAdaptationIds = new List<string>();

            if (State.pendingDefeatCarryoverSelection)
            {
                State.pendingDefeatCarryoverSelection = false;
                State.pendingDefeatCarryoverOptions = new List<string>();
                ResetRunAfterDefeat();
                Debug.Log("[CampaignController] Reset moldiness progression and cleared pending defeat carryover state.");
                return true;
            }

            State.pendingDefeatCarryoverOptions = new List<string>();
            SynchronizePendingVictorySnapshotWithCurrentMoldiness();
            CampaignSaveService.Save(State);
            Debug.Log("[CampaignController] Reset moldiness progression and cleared persistent campaign rewards.");
            return true;
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
                ClearGameplayCheckpoint(saveAfterClear: false);
                ClearTemporaryTestingAdaptations(saveAfterClear: false);
                State.requiresNewCampaignStart = true;
                BeginDefeatCarryoverSelectionOrReset();
                return;
            }

            // Victory path
            int clearedLevelDisplay = State.levelIndex + 1;
            var moldinessAward = MoldinessProgression.AwardForLevelClear(State.moldiness, clearedLevelDisplay);
            LogMoldinessAward(clearedLevelDisplay, moldinessAward);
            State.pendingVictorySnapshot ??= new CampaignVictorySnapshot();
            State.pendingVictorySnapshot.clearedLevelDisplay = clearedLevelDisplay;
            State.pendingVictorySnapshot.moldinessAwarded = moldinessAward.AmountAwarded;
            State.pendingVictorySnapshot.moldinessProgressBeforeAward = moldinessAward.PreviousProgress;
            State.pendingVictorySnapshot.moldinessProgressAfterAward = moldinessAward.NewProgress;
            State.pendingVictorySnapshot.moldinessThresholdAfterAward = moldinessAward.CurrentThreshold;
            State.pendingVictorySnapshot.moldinessTierBeforeAward = moldinessAward.PreviousTierIndex;
            State.pendingVictorySnapshot.moldinessTierAfterAward = moldinessAward.NewTierIndex;
            State.pendingVictorySnapshot.pendingMoldinessUnlockCount = State.moldiness?.pendingUnlockTriggers?.Count ?? 0;

            int nextIndex = State.levelIndex + 1;
            if (nextIndex >= progression.MaxLevels)
            {
                // Final victory.
                ClearGameplayCheckpoint(saveAfterClear: false);
                ClearTemporaryTestingAdaptations(saveAfterClear: false);
                State.pendingAdaptationSelection = false;
                ResetPendingAdaptationDraftState(resetRedrawUsage: true);
                State.campaignCompleted = true;
                State.pendingVictorySnapshot = null;
                CampaignSaveService.Save(State);
                Debug.Log($"[CampaignController] Campaign completed! RunId={State.runId} Levels={progression.MaxLevels}");
                return;
            }

            // Mid-run victory: wait for adaptation pick before advancing.
            ClearGameplayCheckpoint(saveAfterClear: false);
            State.pendingAdaptationSelection = true;
            ResetPendingAdaptationDraftState(resetRedrawUsage: true);
            CampaignSaveService.Save(State);
        }

        public List<AdaptationDefinition> GetAdaptationDraftChoices(System.Random random, int count, string forcedAdaptationId = "")
        {
            if (State == null)
            {
                return new List<AdaptationDefinition>();
            }

            if (State.pendingAdaptationSelection)
            {
                var persistedChoices = ResolvePendingAdaptationDraftChoices();
                if (persistedChoices.Count > 0)
                {
                    return persistedChoices;
                }
            }

            var generatedChoices = BuildAdaptationDraftChoices(random, count, forcedAdaptationId);
            if (State.pendingAdaptationSelection)
            {
                PersistPendingAdaptationDraftChoices(generatedChoices);
            }

            return generatedChoices;
        }

        public bool TryUsePendingAdaptationDraftRedraw(System.Random random, int count, string forcedAdaptationId, out List<AdaptationDefinition> redrawnChoices)
        {
            redrawnChoices = new List<AdaptationDefinition>();
            if (!CanUsePendingAdaptationDraftRedraw)
            {
                return false;
            }

            var currentOfferIds = State.pendingAdaptationDraftChoiceIds ?? new List<string>();
            redrawnChoices = BuildAdaptationDraftChoices(random, count, forcedAdaptationId, currentOfferIds);
            if (redrawnChoices.Count == 0)
            {
                return false;
            }

            State.pendingAdaptationDraftRedrawUsed = true;
            PersistPendingAdaptationDraftChoices(redrawnChoices);
            return true;
        }

        public List<MoldinessUnlockDefinition> GetPendingMoldinessUnlockOffers(System.Random random, int count)
        {
            if (State?.moldiness == null || State.moldiness.pendingUnlockTriggers == null || State.moldiness.pendingUnlockTriggers.Count == 0)
            {
                return new List<MoldinessUnlockDefinition>();
            }

            bool hadPendingChoice = State.moldiness.pendingUnlockChoice?.offeredUnlockIds?.Count > 0;
            var offers = MoldinessUnlockService.GenerateOffers(State.moldiness, random, count);
            bool generatedPendingChoice = !hadPendingChoice && State.moldiness.pendingUnlockChoice?.offeredUnlockIds?.Count > 0;
            if (generatedPendingChoice)
            {
                CampaignSaveService.Save(State);
            }

            return offers;
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

            SynchronizePendingVictorySnapshotAfterMoldinessResolution();

            CampaignSaveService.Save(State);
            string targetLabel = result.Definition.Type switch
            {
                MoldinessUnlockType.UnlockAdaptation => result.Definition.AdaptationId,
                MoldinessUnlockType.UnlockMycovariant => result.Definition.MycovariantId.ToString(),
                _ => result.Definition.Type.ToString()
            };
            Debug.Log($"[CampaignController] Applied Moldiness unlock '{result.Definition.Id}' ({targetLabel}).");
            return true;
        }

        public bool TryContinueWithoutMoldinessUnlock()
        {
            if (State?.moldiness == null || State.moldiness.pendingUnlockTriggers == null || State.moldiness.pendingUnlockTriggers.Count == 0)
            {
                return false;
            }

            var offers = GetPendingMoldinessUnlockOffers(new System.Random(State.seed), 3);
            if (offers.Count > 0)
            {
                return false;
            }

            State.moldiness.pendingUnlockChoice = null;
            State.moldiness.pendingUnlockTriggers.RemoveAt(0);
            SynchronizePendingVictorySnapshotAfterMoldinessResolution();
            CampaignSaveService.Save(State);
            Debug.Log("[CampaignController] No moldiness rewards were available for the pending threshold; continuing without a reward.");
            return true;
        }

        public bool TryQueueForcedMoldinessRewardForTesting(System.Random random, int count)
        {
            if (State?.moldiness == null || random == null || !State.pendingAdaptationSelection)
            {
                return false;
            }

            State.moldiness.pendingUnlockTriggers ??= new List<MoldinessUnlockTrigger>();
            if (State.moldiness.pendingUnlockTriggers.Count > 0)
            {
                return true;
            }

            int triggerTierIndex = Math.Max(0, State.moldiness.unlockLevel - 1);
            var trigger = new MoldinessUnlockTrigger
            {
                tierIndex = triggerTierIndex,
                threshold = MoldinessProgression.GetThresholdForTier(triggerTierIndex),
                overflowAfterUnlock = State.moldiness.currentProgress
            };

            State.moldiness.pendingUnlockTriggers.Add(trigger);
            var offers = MoldinessUnlockService.GenerateOffers(State.moldiness, random, count);
            if (offers.Count == 0)
            {
                State.moldiness.pendingUnlockTriggers.RemoveAt(State.moldiness.pendingUnlockTriggers.Count - 1);
                State.moldiness.pendingUnlockChoice = null;
                return false;
            }

            State.pendingVictorySnapshot ??= new CampaignVictorySnapshot();
            State.pendingVictorySnapshot.pendingMoldinessUnlockCount = State.moldiness.pendingUnlockTriggers.Count;
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Queued a forced moldiness reward selection for testing with {offers.Count} offer(s).");
            return true;
        }

        public IReadOnlyList<AdaptationDefinition> GetSelectedAdaptations()
        {
            var activeAdaptationIds = GetAllActiveAdaptationIds();
            if (activeAdaptationIds.Count == 0)
            {
                return Array.Empty<AdaptationDefinition>();
            }

            var selected = new List<AdaptationDefinition>();
            foreach (var adaptationId in activeAdaptationIds)
            {
                if (AdaptationRepository.TryGetById(adaptationId, out var adaptation))
                {
                    selected.Add(adaptation);
                }
            }

            return selected;
        }

        public void SetTemporaryTestingAdaptationIds(IReadOnlyList<string> adaptationIds)
        {
            if (State == null)
            {
                return;
            }

            var sanitizedAdaptationIds = SanitizeTemporaryTestingAdaptationIds(adaptationIds);
            State.temporaryTestingAdaptationIds ??= new List<string>();
            if (State.temporaryTestingAdaptationIds.SequenceEqual(sanitizedAdaptationIds, StringComparer.Ordinal))
            {
                return;
            }

            State.temporaryTestingAdaptationIds = sanitizedAdaptationIds;
            CampaignSaveService.Save(State);
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

            if (State.pendingAdaptationDraftChoiceIds != null
                && State.pendingAdaptationDraftChoiceIds.Count > 0
                && !State.pendingAdaptationDraftChoiceIds.Contains(adaptationId))
            {
                Debug.LogWarning($"[CampaignController] Adaptation '{adaptationId}' is not part of the current pending offer.");
                return false;
            }

            State.selectedAdaptationIds.Add(adaptationId);
            ClearTemporaryTestingAdaptations(saveAfterClear: false);
            State.pendingAdaptationSelection = false;
            ResetPendingAdaptationDraftState(resetRedrawUsage: true);
            State.pendingVictorySnapshot = null;
            AdvanceToNextLevel();
            return true;
        }

        public IReadOnlyList<AdaptationDefinition> GetPendingDefeatCarryoverOptions()
        {
            if (State?.pendingDefeatCarryoverOptions == null || State.pendingDefeatCarryoverOptions.Count == 0)
            {
                return Array.Empty<AdaptationDefinition>();
            }

            var options = new List<AdaptationDefinition>();
            foreach (var adaptationId in State.pendingDefeatCarryoverOptions)
            {
                if (AdaptationRepository.TryGetById(adaptationId, out var adaptation))
                {
                    options.Add(adaptation);
                }
            }

            return options;
        }

        public bool TryConfirmDefeatCarryoverSelection(IReadOnlyList<string> selectedAdaptationIds)
        {
            if (State == null || !State.pendingDefeatCarryoverSelection)
            {
                return false;
            }

            int availableOptionCount = State.pendingDefeatCarryoverOptions?.Count ?? 0;
            int capacity = Math.Min(
                Math.Max(0, State.moldiness?.failedRunAdaptationCarryoverCount ?? 0),
                Math.Max(0, availableOptionCount));
            var selectedIds = selectedAdaptationIds == null
                ? new List<string>()
                : selectedAdaptationIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

            if (selectedIds.Count != capacity)
            {
                Debug.LogWarning($"[CampaignController] Defeat carryover selection must choose exactly {capacity} adaptation(s).");
                return false;
            }

            var validOptions = new HashSet<string>(State.pendingDefeatCarryoverOptions ?? new List<string>(), StringComparer.Ordinal);
            if (selectedIds.Any(id => !validOptions.Contains(id)))
            {
                Debug.LogWarning("[CampaignController] Defeat carryover selection included an invalid adaptation id.");
                return false;
            }

            State.pendingNextRunCarryoverAdaptationIds = selectedIds;
            State.pendingDefeatCarryoverSelection = false;
            State.pendingDefeatCarryoverOptions = new List<string>();
            ResetRunAfterDefeat();
            return true;
        }

        public bool TryAdvanceWithoutAdaptationReward()
        {
            if (State == null || !State.pendingAdaptationSelection)
            {
                return false;
            }

            ClearTemporaryTestingAdaptations(saveAfterClear: false);
            State.pendingAdaptationSelection = false;
            ResetPendingAdaptationDraftState(resetRedrawUsage: true);
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
            ResetPendingAdaptationDraftState(resetRedrawUsage: true);
            State.campaignCompleted = false;
            State.resolvedAiStrategyNames = BuildResolvedAiStrategyNames(preset, targetIndex);
            State.currentLevelGameplaySeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            State.hasInLevelGameplayCheckpoint = false;
            State.inLevelRuntimeSnapshot = null;
            State.inLevelRandomState = null;
            // Seed retained across victories for reproducibility
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Advanced to level {State.levelIndex}. Preset={preset.presetId}");
        }

        /// <summary>
        /// Reset the run after defeat (player returns to mode select with fresh level0 state).
        /// </summary>
        private void BeginDefeatCarryoverSelectionOrReset()
        {
            State.moldiness ??= MoldinessProgression.CreateDefaultState();
            int carryoverCapacity = Math.Max(0, State.moldiness.failedRunAdaptationCarryoverCount);
            var carryoverOptions = GetEligibleDefeatCarryoverAdaptationIds(State.selectedAdaptationIds ?? new List<string>());
            int selectableCarryoverCount = Math.Min(carryoverCapacity, carryoverOptions.Count);
            ClearActiveRunStateForNewCampaign();

            if (selectableCarryoverCount > 0 && carryoverOptions.Count > 0)
            {
                State.pendingDefeatCarryoverSelection = true;
                State.pendingDefeatCarryoverOptions = carryoverOptions;
                State.pendingVictorySnapshot = null;
                CampaignSaveService.Save(State);
                Debug.Log($"[CampaignController] Defeat carryover selection pending. Capacity={selectableCarryoverCount}, Options={carryoverOptions.Count}.");
                return;
            }

            State.pendingNextRunCarryoverAdaptationIds = new List<string>();
            ResetRunAfterDefeat();
        }

        private void ResetRunAfterDefeat()
        {
            var carryoverIds = (State.pendingNextRunCarryoverAdaptationIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            ClearActiveRunStateForNewCampaign();
            State.pendingNextRunCarryoverAdaptationIds = new List<string>(carryoverIds);

            CampaignSaveService.Save(State);
            Debug.Log("[CampaignController] Active run cleared after defeat; next start will begin a fresh campaign run.");
        }

        private void ClearActiveRunStateForNewCampaign()
        {
            State.runId = string.Empty;
            State.levelIndex = 0;
            State.selectedAdaptationIds ??= new List<string>();
            State.selectedAdaptationIds.Clear();
            State.seed = 0;
            State.moldiness ??= MoldinessProgression.CreateDefaultState();
            State.boardPresetId = string.Empty;
            State.boardWidth = 0;
            State.boardHeight = 0;
            State.currentLevelGameplaySeed = 0;
            State.hasInLevelGameplayCheckpoint = false;
            State.inLevelRuntimeSnapshot = null;
            State.inLevelRandomState = null;
            State.pendingAdaptationSelection = false;
            ResetPendingAdaptationDraftState(resetRedrawUsage: true);
            State.campaignCompleted = false;
            State.pendingVictorySnapshot = null;
            State.pendingDefeatCarryoverSelection = false;
            State.pendingDefeatCarryoverOptions = new List<string>();
            State.requiresNewCampaignStart = true;
            State.resolvedAiStrategyNames = new List<string>();
        }

        private void RestorePendingNextRunCarryoverToSelectedAdaptations()
        {
            if (State == null)
            {
                return;
            }

            if (State.requiresNewCampaignStart)
            {
                return;
            }

            var pendingCarryoverIds = GetEligibleDefeatCarryoverAdaptationIds(State.pendingNextRunCarryoverAdaptationIds ?? new List<string>());
            if (pendingCarryoverIds.Count == 0)
            {
                return;
            }

            State.selectedAdaptationIds ??= new List<string>();
            bool changed = false;
            foreach (var adaptationId in pendingCarryoverIds)
            {
                if (State.selectedAdaptationIds.Contains(adaptationId))
                {
                    continue;
                }

                State.selectedAdaptationIds.Add(adaptationId);
                changed = true;
            }

            if (State.pendingNextRunCarryoverAdaptationIds.Count > 0)
            {
                State.pendingNextRunCarryoverAdaptationIds = new List<string>();
                changed = true;
            }

            if (changed)
            {
                CampaignSaveService.Save(State);
            }
        }

        private void SynchronizePendingVictorySnapshotWithCurrentMoldiness()
        {
            if (State == null)
            {
                return;
            }

            if (!State.pendingAdaptationSelection)
            {
                State.pendingVictorySnapshot = null;
                return;
            }

            State.pendingVictorySnapshot ??= new CampaignVictorySnapshot();
            var moldinessSnapshot = MoldinessProgression.GetSnapshot(State.moldiness);
            State.pendingVictorySnapshot.moldinessAwarded = 0;
            State.pendingVictorySnapshot.moldinessProgressBeforeAward = moldinessSnapshot.CurrentProgress;
            State.pendingVictorySnapshot.moldinessProgressAfterAward = moldinessSnapshot.CurrentProgress;
            State.pendingVictorySnapshot.moldinessThresholdAfterAward = moldinessSnapshot.CurrentThreshold;
            State.pendingVictorySnapshot.moldinessTierBeforeAward = moldinessSnapshot.CurrentTierIndex;
            State.pendingVictorySnapshot.moldinessTierAfterAward = moldinessSnapshot.CurrentTierIndex;
            State.pendingVictorySnapshot.pendingMoldinessUnlockCount = 0;
        }

        private void SynchronizePendingVictorySnapshotAfterMoldinessResolution()
        {
            if (State == null)
            {
                return;
            }

            if (State.pendingVictorySnapshot != null)
            {
                State.pendingVictorySnapshot.pendingMoldinessUnlockCount = State.moldiness?.pendingUnlockTriggers?.Count ?? 0;
                if (!State.pendingAdaptationSelection && State.pendingVictorySnapshot.pendingMoldinessUnlockCount == 0)
                {
                    State.pendingVictorySnapshot = null;
                }
            }
        }

        private void SanitizePendingDefeatCarryoverOptions()
        {
            if (State == null)
            {
                return;
            }

            var sanitizedOptions = GetEligibleDefeatCarryoverAdaptationIds(State.pendingDefeatCarryoverOptions ?? new List<string>());
            bool optionsChanged = !(State.pendingDefeatCarryoverOptions ?? new List<string>()).SequenceEqual(sanitizedOptions, StringComparer.Ordinal);

            if (!optionsChanged)
            {
                return;
            }

            State.pendingDefeatCarryoverOptions = sanitizedOptions;
            if (State.pendingDefeatCarryoverSelection && sanitizedOptions.Count == 0)
            {
                State.pendingDefeatCarryoverSelection = false;
                State.pendingNextRunCarryoverAdaptationIds = new List<string>();
                ResetRunAfterDefeat();
                return;
            }

            CampaignSaveService.Save(State);
        }

        private void PersistPendingAdaptationDraftChoices(IEnumerable<AdaptationDefinition> choices)
        {
            if (State == null)
            {
                return;
            }

            State.pendingAdaptationDraftChoiceIds = (choices ?? Array.Empty<AdaptationDefinition>())
                .Where(choice => choice != null && !string.IsNullOrWhiteSpace(choice.Id))
                .Select(choice => choice.Id)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            CampaignSaveService.Save(State);
        }

        private List<AdaptationDefinition> ResolvePendingAdaptationDraftChoices()
        {
            if (State?.pendingAdaptationDraftChoiceIds == null || State.pendingAdaptationDraftChoiceIds.Count == 0)
            {
                return new List<AdaptationDefinition>();
            }

            var selected = new HashSet<string>(GetAllActiveAdaptationIds(), StringComparer.Ordinal);
            var resolved = new List<AdaptationDefinition>();
            foreach (var adaptationId in State.pendingAdaptationDraftChoiceIds)
            {
                if (string.IsNullOrWhiteSpace(adaptationId)
                    || !AdaptationRepository.TryGetById(adaptationId, out var adaptation)
                    || adaptation == null
                    || adaptation.IsStartingAdaptation
                    || selected.Contains(adaptationId)
                    || resolved.Any(existing => string.Equals(existing.Id, adaptationId, StringComparison.Ordinal)))
                {
                    return new List<AdaptationDefinition>();
                }

                resolved.Add(adaptation);
            }

            return resolved;
        }

        private List<AdaptationDefinition> BuildAdaptationDraftChoices(
            System.Random random,
            int count,
            string forcedAdaptationId = "",
            IEnumerable<string> deprioritizedAdaptationIds = null)
        {
            if (State == null)
            {
                return new List<AdaptationDefinition>();
            }

            random ??= new System.Random(State.seed);

            var selected = new HashSet<string>(GetAllActiveAdaptationIds(), StringComparer.Ordinal);
            var eligible = CampaignDraftEligibility.GetEligibleAdaptations(
                AdaptationRepository.All,
                selected,
                State.moldiness?.unlockedAdaptationIds,
                State.moldiness?.unlockLevel ?? 0);

            AdaptationDefinition forcedAdaptation = null;
            if (!string.IsNullOrWhiteSpace(forcedAdaptationId)
                && AdaptationRepository.TryGetById(forcedAdaptationId, out var forcedDefinition)
                && forcedDefinition != null
                && !forcedDefinition.IsStartingAdaptation
                && !selected.Contains(forcedDefinition.Id))
            {
                forcedAdaptation = forcedDefinition;
                if (!eligible.Any(choice => string.Equals(choice.Id, forcedAdaptation.Id, StringComparison.Ordinal)))
                {
                    eligible.Add(forcedAdaptation);
                }
            }

            if (eligible.Count == 0)
            {
                return eligible;
            }

            if ((deprioritizedAdaptationIds == null || !deprioritizedAdaptationIds.Any())
                && forcedAdaptation == null
                && (count <= 0 || count >= eligible.Count))
            {
                return eligible;
            }

            int desiredCount = count <= 0 ? eligible.Count : Mathf.Min(count, eligible.Count);
            var deprioritizedIds = new HashSet<string>(
                (deprioritizedAdaptationIds ?? Array.Empty<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.Ordinal);

            var choicePool = eligible
                .Where(choice => forcedAdaptation == null || !string.Equals(choice.Id, forcedAdaptation.Id, StringComparison.Ordinal))
                .ToList();
            ShuffleInPlace(random, choicePool);

            var prioritizedChoices = choicePool
                .Where(choice => !deprioritizedIds.Contains(choice.Id))
                .ToList();
            var fallbackChoices = choicePool
                .Where(choice => deprioritizedIds.Contains(choice.Id))
                .ToList();

            var result = new List<AdaptationDefinition>(desiredCount);
            if (forcedAdaptation != null)
            {
                result.Add(forcedAdaptation);
            }

            int remainingSlots = Mathf.Max(0, desiredCount - result.Count);
            result.AddRange(prioritizedChoices.Take(remainingSlots));

            remainingSlots = Mathf.Max(0, desiredCount - result.Count);
            if (remainingSlots > 0)
            {
                result.AddRange(fallbackChoices.Take(remainingSlots));
            }

            if (forcedAdaptation != null)
            {
                ShuffleInPlace(random, result);
            }

            return result;
        }

        private bool NormalizePendingAdaptationDraftState()
        {
            if (State == null)
            {
                return false;
            }

            bool changed = false;
            State.pendingAdaptationDraftChoiceIds ??= new List<string>();

            var sanitizedChoiceIds = State.pendingAdaptationDraftChoiceIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (!State.pendingAdaptationDraftChoiceIds.SequenceEqual(sanitizedChoiceIds, StringComparer.Ordinal))
            {
                State.pendingAdaptationDraftChoiceIds = sanitizedChoiceIds;
                changed = true;
            }

            if (!State.pendingAdaptationSelection)
            {
                if (State.pendingAdaptationDraftChoiceIds.Count > 0)
                {
                    State.pendingAdaptationDraftChoiceIds.Clear();
                    changed = true;
                }

                if (State.pendingAdaptationDraftRedrawUsed)
                {
                    State.pendingAdaptationDraftRedrawUsed = false;
                    changed = true;
                }

                return changed;
            }

            if (State.pendingAdaptationDraftChoiceIds.Count > 0 && ResolvePendingAdaptationDraftChoices().Count != State.pendingAdaptationDraftChoiceIds.Count)
            {
                State.pendingAdaptationDraftChoiceIds.Clear();
                changed = true;
            }

            return changed;
        }

        private void ResetPendingAdaptationDraftState(bool resetRedrawUsage)
        {
            if (State == null)
            {
                return;
            }

            State.pendingAdaptationDraftChoiceIds ??= new List<string>();
            State.pendingAdaptationDraftChoiceIds.Clear();
            if (resetRedrawUsage)
            {
                State.pendingAdaptationDraftRedrawUsed = false;
            }
        }

        private static void ShuffleInPlace<T>(System.Random random, IList<T> items)
        {
            if (random == null || items == null)
            {
                return;
            }

            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private List<string> GetAllActiveAdaptationIds()
        {
            var activeIds = new List<string>();
            if (State?.selectedAdaptationIds != null)
            {
                activeIds.AddRange(State.selectedAdaptationIds);
            }

            if (State?.temporaryTestingAdaptationIds != null)
            {
                activeIds.AddRange(State.temporaryTestingAdaptationIds);
            }

            return activeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> SanitizeTemporaryTestingAdaptationIds(IReadOnlyList<string> adaptationIds)
        {
            return (adaptationIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Where(id => AdaptationRepository.TryGetById(id, out var adaptation)
                    && adaptation != null
                    && !adaptation.IsStartingAdaptation)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private void ClearTemporaryTestingAdaptations(bool saveAfterClear = true)
        {
            if (State?.temporaryTestingAdaptationIds == null || State.temporaryTestingAdaptationIds.Count == 0)
            {
                return;
            }

            State.temporaryTestingAdaptationIds.Clear();
            if (saveAfterClear)
            {
                CampaignSaveService.Save(State);
            }
        }

        private static List<string> GetEligibleDefeatCarryoverAdaptationIds(IEnumerable<string> adaptationIds)
        {
            return (adaptationIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Where(IsEligibleDefeatCarryoverAdaptationId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> GetCarryoverAdaptationIdsForNewCampaignStart(CampaignState previousState)
        {
            if (previousState == null)
            {
                return new List<string>();
            }

            var pendingCarryoverIds = GetEligibleDefeatCarryoverAdaptationIds(previousState.pendingNextRunCarryoverAdaptationIds ?? new List<string>());
            if (pendingCarryoverIds.Count > 0)
            {
                return pendingCarryoverIds;
            }

            if (previousState.requiresNewCampaignStart)
            {
                return new List<string>();
            }

            if (previousState.pendingAdaptationSelection
                || previousState.pendingDefeatCarryoverSelection
                || previousState.campaignCompleted
                || previousState.levelIndex != 0)
            {
                return new List<string>();
            }

            return GetEligibleDefeatCarryoverAdaptationIds(previousState.selectedAdaptationIds ?? new List<string>());
        }

        private static bool IsEligibleDefeatCarryoverAdaptationId(string adaptationId)
        {
            return AdaptationRepository.TryGetById(adaptationId, out var adaptation)
                && adaptation != null
                && !adaptation.IsStartingAdaptation;
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
                        resolved.Add(AIRoster.NormalizeCampaignStrategyName(aiSpec.strategyName));
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
                .Select(AIRoster.NormalizeCampaignStrategyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
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
            var normalizedName = AIRoster.NormalizeCampaignStrategyName(strategyName);
            return AIRoster.CampaignStrategiesByName.ContainsKey(normalizedName)
                || AIRoster.ProvenStrategiesByName.ContainsKey(normalizedName);
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
