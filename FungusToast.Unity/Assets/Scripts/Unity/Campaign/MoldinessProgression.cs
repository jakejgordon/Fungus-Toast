using System;
using System.Collections.Generic;
using CoreCampaign = FungusToast.Core.Campaign;

namespace FungusToast.Unity.Campaign
{
    [Serializable]
    public class MoldinessProgressionState
    {
        public int currentProgress;
        public int currentTierIndex;
        public int lifetimeEarned;
        public int highestUnlockedCampaignStartDifficultyIndex;
        public List<MoldinessUnlockTrigger> pendingUnlockTriggers = new();
        public int unlockLevel;
        public int failedRunAdaptationCarryoverCount;
        public List<string> unlockedRewardIds = new();
        public List<string> unlockedAdaptationIds = new();
        public List<int> unlockedMycovariantIds = new();
        public MoldinessUnlockChoiceState pendingUnlockChoice;
    }

    [Serializable]
    public class MoldinessUnlockTrigger
    {
        public int tierIndex;
        public int threshold;
        public int overflowAfterUnlock;
    }

    public readonly struct MoldinessProgressSnapshot
    {
        public MoldinessProgressSnapshot(int currentProgress, int currentTierIndex, int currentThreshold, int lifetimeEarned, int pendingUnlockCount)
        {
            CurrentProgress = currentProgress;
            CurrentTierIndex = currentTierIndex;
            CurrentThreshold = currentThreshold;
            LifetimeEarned = lifetimeEarned;
            PendingUnlockCount = pendingUnlockCount;
        }

        public int CurrentProgress { get; }
        public int CurrentTierIndex { get; }
        public int CurrentThreshold { get; }
        public int LifetimeEarned { get; }
        public int PendingUnlockCount { get; }
    }

    public readonly struct MoldinessAwardResult
    {
        public MoldinessAwardResult(
            int amountAwarded,
            int previousProgress,
            int newProgress,
            int previousTierIndex,
            int newTierIndex,
            int currentThreshold,
            int lifetimeEarned,
            IReadOnlyList<MoldinessUnlockTrigger> unlockTriggers)
        {
            AmountAwarded = amountAwarded;
            PreviousProgress = previousProgress;
            NewProgress = newProgress;
            PreviousTierIndex = previousTierIndex;
            NewTierIndex = newTierIndex;
            CurrentThreshold = currentThreshold;
            LifetimeEarned = lifetimeEarned;
            UnlockTriggers = unlockTriggers ?? Array.Empty<MoldinessUnlockTrigger>();
        }

        public int AmountAwarded { get; }
        public int PreviousProgress { get; }
        public int NewProgress { get; }
        public int PreviousTierIndex { get; }
        public int NewTierIndex { get; }
        public int CurrentThreshold { get; }
        public int LifetimeEarned { get; }
        public IReadOnlyList<MoldinessUnlockTrigger> UnlockTriggers { get; }
        public bool TriggeredUnlock => UnlockTriggers.Count > 0;
    }

    public static class MoldinessProgression
    {
        public static MoldinessProgressionState CreateDefaultState()
        {
            return new MoldinessProgressionState();
        }

        public static int GetRewardForClearedLevel(int clearedLevelDisplay, bool isFinalCampaignVictory = false)
        {
            return CoreCampaign.MoldinessProgression.GetRewardForClearedLevel(clearedLevelDisplay, isFinalCampaignVictory);
        }

        public static int GetThresholdForTier(int tierIndex)
        {
            return CoreCampaign.MoldinessProgression.GetThresholdForTier(tierIndex);
        }

        public static MoldinessProgressSnapshot GetSnapshot(MoldinessProgressionState state)
        {
            var snapshot = CoreCampaign.MoldinessProgression.GetSnapshot(ToCoreState(state));
            return new MoldinessProgressSnapshot(
                snapshot.CurrentProgress,
                snapshot.CurrentTierIndex,
                snapshot.CurrentThreshold,
                snapshot.LifetimeEarned,
                snapshot.PendingUnlockCount);
        }

        public static MoldinessAwardResult AwardForLevelClear(
            MoldinessProgressionState state,
            int clearedLevelDisplay,
            bool isFinalCampaignVictory = false)
        {
            var targetState = state ?? CreateDefaultState();
            var coreState = ToCoreState(targetState);
            var result = CoreCampaign.MoldinessProgression.AwardForLevelClear(coreState, clearedLevelDisplay, isFinalCampaignVictory);
            ApplyCoreState(coreState, targetState);
            return FromCoreResult(result);
        }

        public static MoldinessAwardResult ApplyAward(MoldinessProgressionState state, int amount)
        {
            var targetState = state ?? CreateDefaultState();
            var coreState = ToCoreState(targetState);
            var result = CoreCampaign.MoldinessProgression.ApplyAward(coreState, amount);
            ApplyCoreState(coreState, targetState);
            return FromCoreResult(result);
        }

        private static MoldinessAwardResult FromCoreResult(CoreCampaign.MoldinessAwardResult result)
        {
            var triggers = new List<MoldinessUnlockTrigger>();
            if (result.UnlockTriggers != null)
            {
                foreach (var trigger in result.UnlockTriggers)
                {
                    triggers.Add(new MoldinessUnlockTrigger
                    {
                        tierIndex = trigger.tierIndex,
                        threshold = trigger.threshold,
                        overflowAfterUnlock = trigger.overflowAfterUnlock
                    });
                }
            }

            return new MoldinessAwardResult(
                result.AmountAwarded,
                result.PreviousProgress,
                result.NewProgress,
                result.PreviousTierIndex,
                result.NewTierIndex,
                result.CurrentThreshold,
                result.LifetimeEarned,
                triggers);
        }

        private static CoreCampaign.MoldinessProgressionState ToCoreState(MoldinessProgressionState state)
        {
            state ??= CreateDefaultState();

            return new CoreCampaign.MoldinessProgressionState
            {
                currentProgress = state.currentProgress,
                currentTierIndex = state.currentTierIndex,
                lifetimeEarned = state.lifetimeEarned,
                highestUnlockedCampaignStartDifficultyIndex = state.highestUnlockedCampaignStartDifficultyIndex,
                pendingUnlockTriggers = CopyTriggersToCore(state.pendingUnlockTriggers),
                unlockLevel = state.unlockLevel,
                failedRunAdaptationCarryoverCount = state.failedRunAdaptationCarryoverCount,
                unlockedRewardIds = state.unlockedRewardIds != null ? new List<string>(state.unlockedRewardIds) : new List<string>(),
                unlockedAdaptationIds = state.unlockedAdaptationIds != null ? new List<string>(state.unlockedAdaptationIds) : new List<string>(),
                unlockedMycovariantIds = state.unlockedMycovariantIds != null ? new List<int>(state.unlockedMycovariantIds) : new List<int>(),
                pendingUnlockChoice = state.pendingUnlockChoice == null
                    ? null
                    : new CoreCampaign.MoldinessUnlockChoiceState
                    {
                        triggerTierIndex = state.pendingUnlockChoice.triggerTierIndex,
                        offeredUnlockIds = state.pendingUnlockChoice.offeredUnlockIds != null
                            ? new List<string>(state.pendingUnlockChoice.offeredUnlockIds)
                            : new List<string>()
                    }
            };
        }

        private static void ApplyCoreState(CoreCampaign.MoldinessProgressionState source, MoldinessProgressionState target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.currentProgress = source.currentProgress;
            target.currentTierIndex = source.currentTierIndex;
            target.lifetimeEarned = source.lifetimeEarned;
            target.highestUnlockedCampaignStartDifficultyIndex = source.highestUnlockedCampaignStartDifficultyIndex;
            target.pendingUnlockTriggers = CopyTriggersFromCore(source.pendingUnlockTriggers);
            target.unlockLevel = source.unlockLevel;
            target.failedRunAdaptationCarryoverCount = source.failedRunAdaptationCarryoverCount;
            target.unlockedRewardIds = source.unlockedRewardIds != null ? new List<string>(source.unlockedRewardIds) : new List<string>();
            target.unlockedAdaptationIds = source.unlockedAdaptationIds != null ? new List<string>(source.unlockedAdaptationIds) : new List<string>();
            target.unlockedMycovariantIds = source.unlockedMycovariantIds != null ? new List<int>(source.unlockedMycovariantIds) : new List<int>();
            target.pendingUnlockChoice = source.pendingUnlockChoice == null
                ? null
                : new MoldinessUnlockChoiceState
                {
                    triggerTierIndex = source.pendingUnlockChoice.triggerTierIndex,
                    offeredUnlockIds = source.pendingUnlockChoice.offeredUnlockIds != null
                        ? new List<string>(source.pendingUnlockChoice.offeredUnlockIds)
                        : new List<string>()
                };
        }

        private static List<CoreCampaign.MoldinessUnlockTrigger> CopyTriggersToCore(IReadOnlyList<MoldinessUnlockTrigger> triggers)
        {
            var copy = new List<CoreCampaign.MoldinessUnlockTrigger>();
            if (triggers == null)
            {
                return copy;
            }

            foreach (var trigger in triggers)
            {
                if (trigger == null)
                {
                    continue;
                }

                copy.Add(new CoreCampaign.MoldinessUnlockTrigger
                {
                    tierIndex = trigger.tierIndex,
                    threshold = trigger.threshold,
                    overflowAfterUnlock = trigger.overflowAfterUnlock
                });
            }

            return copy;
        }

        private static List<MoldinessUnlockTrigger> CopyTriggersFromCore(IReadOnlyList<CoreCampaign.MoldinessUnlockTrigger> triggers)
        {
            var copy = new List<MoldinessUnlockTrigger>();
            if (triggers == null)
            {
                return copy;
            }

            foreach (var trigger in triggers)
            {
                if (trigger == null)
                {
                    continue;
                }

                copy.Add(new MoldinessUnlockTrigger
                {
                    tierIndex = trigger.tierIndex,
                    threshold = trigger.threshold,
                    overflowAfterUnlock = trigger.overflowAfterUnlock
                });
            }

            return copy;
        }
    }
}
