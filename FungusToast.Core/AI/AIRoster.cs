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
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                }
            ),
                                    new ParameterizedSpendingStrategy(
                strategyName: "Necrotoxic Max Economy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, 3),
                    new TargetMutationGoal(MutationIds.AnabolicInversion, 3),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, 5)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "Best_MaxEcon_Surge10_HyphalSurge",
                prioritizeHighTier: true,
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
            ),
            
            // Example using the new preferredMycovariantIds parameter for simpler preference specification
            new ParameterizedSpendingStrategy(
                strategyName: "Economic Focus (Simple Prefs)",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, 5),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, 5),
                    new TargetMutationGoal(MutationIds.MycelialBloom, 20)
                },
                economyBias: EconomyBias.MaxEconomy,
                preferredMycovariantIds: new List<int>
                {
                    MycovariantIds.PlasmidBountyIIIId,   // First preference: Plasmid Bounty III
                    MycovariantIds.PlasmidBountyIIId,    // Second preference: Plasmid Bounty II  
                    MycovariantIds.PlasmidBountyId,      // Third preference: Plasmid Bounty I
                    MycovariantIds.EnduringToxaphoresId  // Fourth preference: Enduring Toxaphores
                }
            ),
            
            new ParameterizedSpendingStrategy(
                strategyName: "Bastion Defender (Simple Prefs)",
                prioritizeHighTier: true,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycelialBloom, 30),
                    new TargetMutationGoal(MutationIds.HomeostaticHarmony, 30),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae, 10)
                },
                economyBias: EconomyBias.ModerateEconomy,
                preferredMycovariantIds: new List<int>
                {
                    MycovariantIds.MycelialBastionIIIId,     // First preference: Mycelial Bastion III
                    MycovariantIds.MycelialBastionIIId,      // Second preference: Mycelial Bastion II
                    MycovariantIds.MycelialBastionIId,       // Third preference: Mycelial Bastion I
                    MycovariantIds.HyphalResistanceTransferId, // Fourth preference: Hyphal Resistance Transfer
                    MycovariantIds.ReclamationRhizomorphsId  // Fifth preference: Reclamation Rhizomorphs
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
                    strategyName: "Grow=>Kill=>Reclaim (Economic)",
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
                        MycovariantIds.PlasmidBountyIIIId,   // 30 mutation points
                        MycovariantIds.PlasmidBountyIIId,    // 20 mutation points  
                        MycovariantIds.PlasmidBountyId      // 7 mutation points
                    }
                ),

                // 2. Aggressive Assault - direct offensive capabilities
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Jetting)",
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
                        MycovariantIds.JettingMyceliumNorthId,    // Explosive directional growth + toxins
                        MycovariantIds.JettingMyceliumEastId,     // Alternative direction
                        MycovariantIds.JettingMyceliumSouthId,    // Alternative direction
                        MycovariantIds.JettingMyceliumWestId 
                    }
                ),

                // 3. Toxin Specialist - enhanced toxin warfare
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Toxin)",
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
                        MycovariantIds.EnduringToxaphoresId,      // Make toxins last longer
                        MycovariantIds.BallistosporeDischargeIIIId, // 20 toxin spores
                        MycovariantIds.BallistosporeDischargeIIId,  // 15 toxin spores
                        MycovariantIds.BallistosporeDischargeIId    // 10 toxin spores
                    }
                ),

                // 4. Defensive Foundation - build strong base before attacking
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Resistance I)",
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
                        MycovariantIds.MycelialBastionIIId,  
                        MycovariantIds.MycelialBastionIIIId,      
                        MycovariantIds.HyphalResistanceTransferId,
                        MycovariantIds.SurgicalInoculationId  
                    }
                ),

                // 5. Hybrid Economic-Assault - balance between growth and offense
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Power I)",
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

                // 6. Fortress Assault - defensive foundation with toxin offense
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Resistance II)",
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
                        MycovariantIds.HyphalResistanceTransferId, 
                        MycovariantIds.SurgicalInoculationId,     
                        MycovariantIds.MycelialBastionIIId,    
                        MycovariantIds.MycelialBastionIId
                    }
                ),

                // 7. Adaptive Specialist - mixed utility with reclamation focus
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Power II)",
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

                // 8. Elite Assault - high-tier offensive mycovariants
                new ParameterizedSpendingStrategy(
                    strategyName: "Grow=>Kill=>Reclaim (Elite)",
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
                        MycovariantIds.MycelialBastionIIIId,      // Elite defense (15 resistant cells)
                        MycovariantIds.BallistosporeDischargeIIIId, // Elite toxin assault (20 spores)
                        MycovariantIds.PlasmidBountyIIIId,        // Elite economy (30 points)
                    }
                )
            };
        }
    }
}
