using System.Collections.Generic;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.AI
{
    public static class AIRoster
    {
        /// <summary>
        /// All curated, proven AI strategies for use in UI and simulation.
        /// </summary>
        public static readonly List<IMutationSpendingStrategy> ProvenStrategies = new List<IMutationSpendingStrategy>
        {
            // You can of course adjust these as your meta changes
            new ParameterizedSpendingStrategy(
                strategyName: "Growth/Resilience",
                prioritizeHighTier: true,
                maxTier: MutationTier.Tier3,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.AdaptiveExpression, MutationIds.NecrohyphalInfiltration, MutationIds.RegenerativeHyphae }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Mutator Growth",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.CreepingMold }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Minor Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "SurgeFreq_10",
                targetMutationIds: new List<int>
                {
                    MutationIds.HyphalSurge,
                    MutationIds.HyphalVectoring
                },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                maxTier: MutationTier.Tier4
                // You can add/override more options as needed
            )
        };

        // (Optional) A dictionary by name for UI selection or reference
        public static readonly Dictionary<string, IMutationSpendingStrategy> ProvenStrategiesByName =
            ProvenStrategies.ToDictionary(s => s.StrategyName, s => s);
    }
}
