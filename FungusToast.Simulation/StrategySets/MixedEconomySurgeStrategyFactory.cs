using System;
using System.Collections.Generic;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.StrategySets
{
    public static class MixedEconomySurgeStrategyFactory
    {
        // Hard-coded surge mutations (add more to this list as needed)
        private static readonly List<int> AllSurgeMutationIds = new List<int>
        {
            MutationIds.HyphalSurge,
            MutationIds.HyphalVectoring,
            // Add new surge mutation IDs here as they’re implemented!
        };

        /// <summary>
        /// Returns up to 8 unique parameterized AIs with all surges equally weighted. Only returns as many as playerCount.
        /// </summary>
        public static List<IMutationSpendingStrategy> CreateEightStrategiesWithAllSurgesEquallyWeighted(
            int playerCount,
            List<int>? targetSurgeMutationIds = null,
            bool prioritizeHighTier = true,
            MutationTier maxTier = MutationTier.Tier4)
        {
            targetSurgeMutationIds ??= AllSurgeMutationIds;

            var combos = new (EconomyBias bias, int surgeFreq)[]
            {
                (EconomyBias.IgnoreEconomy, 100),
                (EconomyBias.IgnoreEconomy, 5),
                (EconomyBias.MinorEconomy, 20),
                (EconomyBias.MinorEconomy, 5),
                (EconomyBias.ModerateEconomy, 20),
                (EconomyBias.ModerateEconomy, 5),
                (EconomyBias.MaxEconomy, 20),
                (EconomyBias.MaxEconomy, 5)
            };

            var strategies = new List<IMutationSpendingStrategy>();

            for (int i = 0; i < Math.Min(playerCount, combos.Length); i++)
            {
                var stratName = $"Econ_{combos[i].bias}_Surge_{combos[i].surgeFreq}";

                // NEW: Add HyperadaptiveDrift for MaxEconomy
                var targets = new List<int>(targetSurgeMutationIds);
                if (combos[i].bias == EconomyBias.MaxEconomy &&
                    !targets.Contains(MutationIds.HyperadaptiveDrift))
                {
                    targets.Add(MutationIds.HyperadaptiveDrift); // <-- CHANGED
                }

                strategies.Add(new ParameterizedSpendingStrategy(
                    strategyName: stratName,
                    targetMutationIds: targets, // <-- CHANGED
                    surgeAttemptTurnFrequency: combos[i].surgeFreq,
                    prioritizeHighTier: prioritizeHighTier,
                    maxTier: maxTier,
                    economyBias: combos[i].bias
                ));
            }

            return strategies;
        }

        /// <summary>
        /// Returns up to 8 unique parameterized AIs: varying economy, surge frequency, and allowed surge mutations.
        /// Some will enable both surges, others only one.
        /// </summary>
        public static List<IMutationSpendingStrategy> CreateEightEconomySurgeStrategies(
            int playerCount,
            bool prioritizeHighTier = true,
            MutationTier maxTier = MutationTier.Tier4)
        {
            var hyphalSurgeOnly = new List<int> { MutationIds.HyphalSurge };
            var hyphalVectoringOnly = new List<int> { MutationIds.HyphalVectoring };
            var bothSurges = AllSurgeMutationIds.ToList(); // Safe to extend for more surges

            var combos = new (string name, EconomyBias bias, int surgeFreq, List<int> allowedSurges)[]
            {
                // 1: Both surges, conservative economy
                ("Econ_Ignore_Surge100_Both", EconomyBias.IgnoreEconomy, 100, bothSurges),
                // 2: Both surges, frequent activation, ignore economy
                ("Econ_Ignore_Surge5_Both", EconomyBias.IgnoreEconomy, 5, bothSurges),
                // 3: HyphalSurge only, minor economy
                ("Econ_Minor_Surge20_HyphalSurge", EconomyBias.MinorEconomy, 20, hyphalSurgeOnly),
                // 4: HyphalVectoring only, minor economy
                ("Econ_Minor_Surge20_HyphalVectoring", EconomyBias.MinorEconomy, 20, hyphalVectoringOnly),
                // 5: Both surges, moderate economy
                ("Econ_Moderate_Surge20_Both", EconomyBias.ModerateEconomy, 20, bothSurges),
                // 6: Both surges, moderate economy, frequent
                ("Econ_Moderate_Surge5_Both", EconomyBias.ModerateEconomy, 5, bothSurges),
                // 7: HyphalSurge only, max economy, frequent
                ("Econ_Max_Surge5_HyphalSurge", EconomyBias.MaxEconomy, 5, hyphalSurgeOnly),
                // 8: HyphalVectoring only, max economy, frequent
                ("Econ_Max_Surge5_HyphalVectoring", EconomyBias.MaxEconomy, 5, hyphalVectoringOnly),
            };

            var strategies = new List<IMutationSpendingStrategy>();
            for (int i = 0; i < Math.Min(playerCount, combos.Length); i++)
            {
                var c = combos[i];

                // NEW: Add HyperadaptiveDrift for MaxEconomy
                var targets = bothSurges.ToList();
                if (c.bias == EconomyBias.MaxEconomy &&
                    !targets.Contains(MutationIds.HyperadaptiveDrift))
                {
                    targets.Add(MutationIds.HyperadaptiveDrift); // <-- CHANGED
                }

                strategies.Add(new ParameterizedSpendingStrategy(
                    strategyName: c.name,
                    prioritizeHighTier: prioritizeHighTier,
                    targetMutationIds: targets, // <-- CHANGED
                    surgePriorityIds: c.allowedSurges,
                    surgeAttemptTurnFrequency: c.surgeFreq,
                    economyBias: c.bias,
                    maxTier: maxTier
                ));
            }
            return strategies;
        }
    }
}
