using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

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
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyphalVectoring)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations v2",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow=>Kill=>Reclaim",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Cata. B. => Putr. Rejuv",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                   new TargetMutationGoal(MutationIds.CatabolicRebirth),
                   new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Necrotoxic Moderate Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycelialBloom, 10),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion),
                    new TargetMutationGoal(MutationIds.SporocidalBloom)
                }
            ),
                                    new ParameterizedSpendingStrategy(
                strategyName: "Necrotoxic Moderate Economy 2",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycelialBloom, 20),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion),
                    new TargetMutationGoal(MutationIds.SporocidalBloom)
                }
            ),
            /*
            new ParameterizedSpendingStrategy(
                strategyName: "Mutator Growth",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MutatorPhenotype),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Minor Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy
            ),
            */
            new ParameterizedSpendingStrategy(
                strategyName: "Best_MaxEcon_Surge5_HyphalSurge",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 5,
                economyBias: EconomyBias.MaxEconomy,
                maxTier: MutationTier.Tier4
            )
        };

        /// <summary>
        /// Testing strategies for specific scenarios (not included in proven strategies)
        /// </summary>
        public static readonly List<IMutationSpendingStrategy> TestingStrategies = new List<IMutationSpendingStrategy>
        {
            new ParameterizedSpendingStrategy(
                strategyName: "Toxin Spammer",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotoxinTracer, 15),  // Reduced from 30 due to nerfed scaling
                    new TargetMutationGoal(MutationIds.SporocidalBloom, 5),   // High level for spore drops
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin, 5), // High level for toxin kills
                    new TargetMutationGoal(MutationIds.MycelialBloom, 30),    // Max level for growth
                    new TargetMutationGoal(MutationIds.HomeostaticHarmony, 30) // Max level for resilience
                },
                economyBias: EconomyBias.MaxEconomy
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Neutralizing Defender",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycelialBloom, 30),    // Max level for growth
                    new TargetMutationGoal(MutationIds.HomeostaticHarmony, 30), // Max level for resilience
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 15), // High level for survival
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae, 10) // High level for reclamation
                },
                economyBias: EconomyBias.MaxEconomy,
                mycovariantPreferences: new List<MycovariantPreference>
                {
                    new MycovariantPreference(MycovariantIds.NeutralizingMantleId, 10, "Primary defense against toxins"),
                    new MycovariantPreference(MycovariantIds.PlasmidBountyId, 5, "Economy boost")
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Jetting Aggressor",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycelialBloom, 30),    // Max level for growth
                    new TargetMutationGoal(MutationIds.CreepingMold, 10),     // High level for movement
                    new TargetMutationGoal(MutationIds.HyphalVectoring, 5),   // High level for directional growth
                    new TargetMutationGoal(MutationIds.Necrosporulation, 5)   // High level for spore production
                },
                economyBias: EconomyBias.MaxEconomy,
                mycovariantPreferences: new List<MycovariantPreference>
                {
                    new MycovariantPreference(new [] {
                        MycovariantIds.JettingMyceliumEastId,
                        MycovariantIds.JettingMyceliumWestId,
                        MycovariantIds.JettingMyceliumNorthId,
                        MycovariantIds.JettingMyceliumSouthId
                    }, 10, "Primary offensive tool (any Jetting Mycelium)"),
                    new MycovariantPreference(MycovariantIds.PlasmidBountyId, 5, "Economy boost")
                }
            )
        };

        // Optional: a dictionary by name for UI selection or reference
        public static readonly Dictionary<string, IMutationSpendingStrategy> ProvenStrategiesByName =
            ProvenStrategies.ToDictionary(s => s.StrategyName, s => s);

        public static readonly Dictionary<string, IMutationSpendingStrategy> TestingStrategiesByName =
            TestingStrategies.ToDictionary(s => s.StrategyName, s => s);

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
