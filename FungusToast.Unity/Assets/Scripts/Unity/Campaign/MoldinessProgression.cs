using System;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    [Serializable]
    public class MoldinessProgressionState
    {
        public int currentProgress;
        public int currentTierIndex;
        public int lifetimeEarned;
        public List<MoldinessUnlockTrigger> pendingUnlockTriggers = new();
        public List<string> unlockedMetaIds = new();
        public List<string> unlockedAdaptationIds = new();
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
        private static readonly int[] RewardByClearedLevelDisplay =
        {
            1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8
        };

        private static readonly int[] ThresholdsByTier =
        {
            6, 9, 12, 15, 18, 21, 24, 27, 30, 34
        };

        public static MoldinessProgressionState CreateDefaultState()
        {
            return new MoldinessProgressionState();
        }

        public static int GetRewardForClearedLevel(int clearedLevelDisplay)
        {
            if (clearedLevelDisplay <= 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(clearedLevelDisplay - 1, 0, RewardByClearedLevelDisplay.Length - 1);
            return RewardByClearedLevelDisplay[index];
        }

        public static int GetThresholdForTier(int tierIndex)
        {
            if (tierIndex < 0)
            {
                tierIndex = 0;
            }

            if (tierIndex < ThresholdsByTier.Length)
            {
                return ThresholdsByTier[tierIndex];
            }

            int overflowTierIndex = tierIndex - ThresholdsByTier.Length + 1;
            return ThresholdsByTier[ThresholdsByTier.Length - 1] + (overflowTierIndex * 4);
        }

        public static MoldinessProgressSnapshot GetSnapshot(MoldinessProgressionState state)
        {
            state ??= CreateDefaultState();
            state.pendingUnlockTriggers ??= new List<MoldinessUnlockTrigger>();

            return new MoldinessProgressSnapshot(
                state.currentProgress,
                state.currentTierIndex,
                GetThresholdForTier(state.currentTierIndex),
                state.lifetimeEarned,
                state.pendingUnlockTriggers.Count);
        }

        public static MoldinessAwardResult AwardForLevelClear(MoldinessProgressionState state, int clearedLevelDisplay)
        {
            state ??= CreateDefaultState();
            int amount = GetRewardForClearedLevel(clearedLevelDisplay);
            return ApplyAward(state, amount);
        }

        public static MoldinessAwardResult ApplyAward(MoldinessProgressionState state, int amount)
        {
            state ??= CreateDefaultState();
            state.pendingUnlockTriggers ??= new List<MoldinessUnlockTrigger>();

            int safeAmount = Mathf.Max(0, amount);
            int previousProgress = state.currentProgress;
            int previousTierIndex = state.currentTierIndex;

            if (safeAmount == 0)
            {
                return new MoldinessAwardResult(
                    0,
                    previousProgress,
                    state.currentProgress,
                    previousTierIndex,
                    state.currentTierIndex,
                    GetThresholdForTier(state.currentTierIndex),
                    state.lifetimeEarned,
                    Array.Empty<MoldinessUnlockTrigger>());
            }

            state.currentProgress += safeAmount;
            state.lifetimeEarned += safeAmount;

            var newTriggers = new List<MoldinessUnlockTrigger>();
            while (state.currentProgress >= GetThresholdForTier(state.currentTierIndex))
            {
                int threshold = GetThresholdForTier(state.currentTierIndex);
                state.currentProgress -= threshold;

                var trigger = new MoldinessUnlockTrigger
                {
                    tierIndex = state.currentTierIndex,
                    threshold = threshold,
                    overflowAfterUnlock = state.currentProgress
                };

                newTriggers.Add(trigger);
                state.pendingUnlockTriggers.Add(trigger);
                state.currentTierIndex++;
            }

            return new MoldinessAwardResult(
                safeAmount,
                previousProgress,
                state.currentProgress,
                previousTierIndex,
                state.currentTierIndex,
                GetThresholdForTier(state.currentTierIndex),
                state.lifetimeEarned,
                newTriggers);
        }
    }
}
