using System;
using System.Collections.Generic;
using System.Linq;
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
             // The following are "best of" mutations in their categories
            new ParameterizedSpendingStrategy(
                strategyName: "SurgeFreq_10",
                targetMutationIds: new List<int>
                {
                    MutationIds.HyphalSurge,
                    MutationIds.HyphalVectoring,
                },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations v2",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationIds: new List<int>
                {
                    MutationIds.CreepingMold,
                    MutationIds.RegenerativeHyphae
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow=>Kill=>Reclaim",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.CreepingMold, MutationIds.Necrosporulation, MutationIds.PutrefactiveMycotoxin, MutationIds.NecrohyphalInfiltration }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Necrotoxic Moderate Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationIds: new List<int> { MutationIds.NecrotoxicConversion, MutationIds.SporocidalBloom }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Mutator Growth",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.MutatorPhenotype, MutationIds.CreepingMold, MutationIds.HyperadaptiveDrift }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Minor Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Best_MaxEcon_Surge5_HyphalSurge",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> {
                    MutationIds.HyphalSurge,
                    MutationIds.HyperadaptiveDrift
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 5,
                economyBias: EconomyBias.MaxEconomy,
                maxTier: MutationTier.Tier4
            )
        };

        // Optional: a dictionary by name for UI selection or reference
        public static readonly Dictionary<string, IMutationSpendingStrategy> ProvenStrategiesByName =
            ProvenStrategies.ToDictionary(s => s.StrategyName, s => s);

        /// <summary>
        /// Returns up to X unique random proven strategies. If count exceeds available strategies,
        /// fills the remainder with uniquely-named RandomMutationSpendingStrategy instances.
        /// </summary>
        public static List<IMutationSpendingStrategy> GetRandomProvenStrategies(int count, Random? rng = null)
        {
            var result = new List<IMutationSpendingStrategy>();
            if (count <= 0)
                return result;

            rng ??= new Random();

            // Create a randomized, unique set from ProvenStrategies
            var shuffled = ProvenStrategies
                .OrderBy(_ => rng.Next())
                .Take(Math.Min(count, ProvenStrategies.Count))
                .ToList();

            result.AddRange(shuffled);

            // Fill remainder with unique-named RandomMutationSpendingStrategy
            int remaining = count - result.Count;
            for (int i = 1; i <= remaining; i++)
            {
                result.Add(new RandomMutationSpendingStrategy($"LegacyRandom #{i}"));
            }

            return result;
        }
    }
}
