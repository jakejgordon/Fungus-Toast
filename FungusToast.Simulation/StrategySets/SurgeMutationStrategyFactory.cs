using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.StrategySets
{
    public static class SurgeMutationStrategyFactory
    {
        /// <summary>
        /// Creates a set of ParameterizedSpendingStrategy instances, each varying by surge attempt frequency and/or target surge mutation(s).
        /// Any remaining player slots are filled with RandomMutationSpendingStrategy, then the list is shuffled.
        /// </summary>
        /// <param name="playerCount">Total number of players for the game (max 8 typical).</param>
        /// <param name="rnd">Random instance for shuffling.</param>
        /// <param name="targetSurgeMutationIds">List of mutation IDs to be prioritized as surges. If null, uses common surges.</param>
        /// <param name="frequencies">List of surge attempt frequencies to test. If null, uses defaults (e.g., [1,2,3,5,10,20,50]).</param>
        /// <param name="prioritizeHighTier">Whether to prioritize higher-tier upgrades (default: true).</param>
        /// <param name="maxTier">Maximum mutation tier to allow (default: Tier10).</param>
        /// <returns>Shuffled list of strategies for simulation.</returns>
        public static List<IMutationSpendingStrategy> CreateSurgeMutationStrategies(
            int playerCount,
            Random rnd,
            List<int>? targetSurgeMutationIds = null,
            List<int>? frequencies = null,
            bool prioritizeHighTier = true,
            MutationTier maxTier = MutationTier.Tier10
        )
        {
            // Typical default surges (adjust as needed for your project)
            var defaultSurges = new List<int>
            {
                MutationIds.HyphalSurge,
                MutationIds.HyphalVectoring
                // Add other surge mutation IDs here as needed
            };

            var surges = targetSurgeMutationIds ?? defaultSurges;
            var freqList = frequencies ?? new List<int> { 1, 2, 3, 5, 10, 20, 50 };

            var strategies = freqList.Select(freq =>
                new ParameterizedSpendingStrategy(
                    strategyName: $"SurgeFreq_{freq}",
                    prioritizeHighTier: prioritizeHighTier,
                    surgeAttemptTurnFrequency: freq,
                    targetMutationIds: surges,
                    surgePriorityIds: surges,
                    maxTier: maxTier
                )
            ).Cast<IMutationSpendingStrategy>().ToList();

            return StrategySetUtils.FillWithRandomStrategiesAndShuffle(strategies, playerCount, rnd);
        }
    }
}
