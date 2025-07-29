using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;
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
            // Economic focus for mycovariants
            new ParameterizedSpendingStrategy(
                strategyName: "Grow>Kill>Reclaim(Econ)",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            // Economy and reclamation mycovariant focus
            new ParameterizedSpendingStrategy(
                strategyName: "Grow>Kill>Reclaim(Econ/Reclaim)",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Creeping>Necrosporulation",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations Max Econ",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                }
            ),
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
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyphalVectoring)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy),
            new ParameterizedSpendingStrategy(
                strategyName: "Anabolic>Grow>CatabR>PutreRegen",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                   new TargetMutationGoal(MutationIds.AnabolicInversion),
                   new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                   new TargetMutationGoal(MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                   new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow>Defend>Kill",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                   new TargetMutationGoal(MutationIds.TendrilNortheast, 2),
                   new TargetMutationGoal(MutationIds.TendrilNorthwest, 2),
                   new TargetMutationGoal(MutationIds.TendrilSoutheast, 2),
                   new TargetMutationGoal(MutationIds.TendrilSouthwest, 2),
                   new TargetMutationGoal(MutationIds.AnabolicInversion, 2),
                   new TargetMutationGoal(MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                   new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel),
                   new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow=>Kill(Max Econ)",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion, GameBalance.NecrotoxicConversionMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Fungicide)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Best_MaxEcon_Surge10_HyphalSurge",
                prioritizeHighTier: true,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.MycelialSurges,
                    MutationCategory.Growth
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 7,
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
                strategyName: "Grow>Kill>Reclaim(Econ)",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow=>Kill",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Fungicide)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations v2",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                }
            ),
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
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyphalVectoring)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy),
            new ParameterizedSpendingStrategy(
                strategyName: "Grow>Defend>Kill",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                   new TargetMutationGoal(MutationIds.TendrilNortheast, 2),
                   new TargetMutationGoal(MutationIds.TendrilNorthwest, 2),
                   new TargetMutationGoal(MutationIds.TendrilSoutheast, 2),
                   new TargetMutationGoal(MutationIds.TendrilSouthwest, 2),
                   new TargetMutationGoal(MutationIds.AnabolicInversion, 2),
                   new TargetMutationGoal(MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                   new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel),
                   new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Hyphal Surge>Necrotoxic ",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge, 4),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, 5)
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

        /// <summary>
        /// Returns 8 permutations of the top-performing "Grow=>Kill=>Reclaim" strategy,
        /// each with different preferred mycovariant themes to test effectiveness.
        /// </summary>
        public static List<IMutationSpendingStrategy> MycovariantPermutations()
        {
            return new List<IMutationSpendingStrategy>
            {
                // 1. Pure Economic Focus - maximize mutation points for faster development
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Econ)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
                ),


                // 2. Toxin Specialist - enhanced toxin warfare
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Toxin)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Fungicide)
                ),

                // 3. Defensive Foundation - build strong base before attacking
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Resistance)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance)
                ),

                // 4. Hybrid Economic-Assault - balance between growth and offense
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Power I)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: new List<int>
                    {
                        MycovariantIds.PlasmidBountyIIIId,      
                        MycovariantIds.NeutralizingMantleId,     
                        MycovariantIds.SurgicalInoculationId,
                        MycovariantIds.ReclamationRhizomorphsId
                    }
                ),

                // 5. Adaptive Specialist - mixed utility with reclamation focus
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Power II)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: new List<int>
                    {
                        MycovariantIds.ReclamationRhizomorphsId,
                        MycovariantIds.SurgicalInoculationId,
                        MycovariantIds.NeutralizingMantleId,
                        MycovariantIds.PlasmidBountyIIIId,
                        MycovariantIds.PlasmidBountyIIId,
                    }
                ),

                // 6. Defensive Foundation - build strong base before attacking
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Reclaim)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation)
                ),
                
                // 7. Economy then reclaim
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Econ/Reclaim)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Reclamation)
                ),

                // 8. Resist growth then resist
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow>Kill>Reclaim(Grow/Resist)",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>
                    {
                        new TargetMutationGoal(MutationIds.CreepingMold),
                        new TargetMutationGoal(MutationIds.Necrosporulation),
                        new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
                    },
                    preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Resistance)
                ),
            };
        }
    }
}
