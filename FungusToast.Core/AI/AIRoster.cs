using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Enum representing different sets of AI strategies available.
    /// </summary>
    public enum StrategySetEnum
    {
        /// <summary>
        /// Proven strategies that have been tested and validated.
        /// </summary>
        Proven,
        
        /// <summary>
        /// Testing strategies for experimental scenarios.
        /// </summary>
        Testing,
        
        /// <summary>
        /// Mycovariant permutation strategies focusing on different mycovariant themes.
        /// </summary>
        Mycovariants,
        
        /// <summary>
        /// Campaign strategies (simple naming) mirroring ProvenStrategies.
        /// </summary>
        Campaign
    }

    public enum StrategySelectionPolicy
    {
        RandomUnique,
        CoverageBalanced,
        StratifiedCycle
    }

    public enum StrategyTheme
    {
        Balanced,
        EconomyRamp,
        Reclamation,
        Offense,
        SurgeTempo,
        Defense,
        Control,
        Mobility,
        Attrition,
        Counterplay,
        LateGameSpike,
        TierCap
    }

    public sealed class StrategyProfile
    {
        public StrategyProfile(string strategyName, StrategySetEnum strategySet, StrategyTheme theme, string intent)
        {
            StrategyName = strategyName;
            StrategySet = strategySet;
            Theme = theme;
            Intent = intent;
        }

        public string StrategyName { get; }
        public StrategySetEnum StrategySet { get; }
        public StrategyTheme Theme { get; }
        public string Intent { get; }
    }

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
                strategyName: "Mutate>Grow>Kill(Max Econ)",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel),
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
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
            // Proven variant: balanced control with Anabolic Inversion before Creeping Mold
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_AnabolicFirst",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),
            // Proven variant: balanced control path with max economy bias
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_MaxEconomy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "SurgeFreq_10_Hyphal",
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
                strategyName: "Grow>Mutate>Kill(Max Econ)",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel),
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
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
        /// Campaign strategies: mirror ProvenStrategies but with simple names AI1..AI N for UI draft/select purposes.
        /// </summary>
        public static readonly List<IMutationSpendingStrategy> CampaignStrategies = new List<IMutationSpendingStrategy>
        {
            // AI1
            new ParameterizedSpendingStrategy(
                strategyName: "AI1",
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
            // AI2
            new ParameterizedSpendingStrategy(
                strategyName: "AI2",
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
            // AI3
            new ParameterizedSpendingStrategy(
                strategyName: "AI3",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel),
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            // AI4
            new ParameterizedSpendingStrategy(
                strategyName: "AI4",
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
            // AI5
            new ParameterizedSpendingStrategy(
                strategyName: "AI5",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                }
            ),
            // AI6
            new ParameterizedSpendingStrategy(
                strategyName: "AI6",
                prioritizeHighTier: true,
                maxTier: MutationTier.Tier3,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                }
            ),
            // AI7
            new ParameterizedSpendingStrategy(
                strategyName: "AI7",
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
            // AI8
            new ParameterizedSpendingStrategy(
                strategyName: "AI8",
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
            // AI9
            new ParameterizedSpendingStrategy(
                strategyName: "AI9",
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
            // AI10
            new ParameterizedSpendingStrategy(
                strategyName: "AI10",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel),
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            // AI11
            new ParameterizedSpendingStrategy(
                strategyName: "AI11",
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
            // 1) Hyper-economy ramp into late pressure
            new ParameterizedSpendingStrategy(
                strategyName: "TST_HyperEconomyRamp",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),

            // 2) Early reclamation swarm and territory retake
            new ParameterizedSpendingStrategy(
                strategyName: "TST_EarlyReclaimerSwarm",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration),
                    new TargetMutationGoal(MutationIds.Necrosporulation)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation)
            ),

            // 3) Balanced control variant: start with Anabolic Inversion before Creeping Mold
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_AnabolicFirst",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),

            // 4) Tempo surges and vectoring mobility
            new ParameterizedSpendingStrategy(
                strategyName: "TST_HyphalSurgeTempo",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyphalVectoring)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 5,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Growth)
            ),

            // 5) Defensive shell that transitions into pressure
            new ParameterizedSpendingStrategy(
                strategyName: "TST_FortressResilience",
                prioritizeHighTier: true,
                economyBias: EconomyBias.Neutral,
                maxTier: MutationTier.Tier4,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.TendrilNortheast, 2),
                    new TargetMutationGoal(MutationIds.TendrilNorthwest, 2),
                    new TargetMutationGoal(MutationIds.TendrilSoutheast, 2),
                    new TargetMutationGoal(MutationIds.TendrilSouthwest, 2),
                    new TargetMutationGoal(MutationIds.MimeticResilience, 2),
                    new TargetMutationGoal(MutationIds.AnabolicInversion, 2)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance)
            ),

            // 6) Counterplay generalist that keeps options open
            new ParameterizedSpendingStrategy(
                strategyName: "TST_OpportunisticCounterplay",
                prioritizeHighTier: true,
                economyBias: EconomyBias.Neutral,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Fungicide,
                    MutationCategory.GeneticDrift,
                    MutationCategory.Growth
                },
                maxTier: MutationTier.Tier5,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AdaptiveExpression),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.CreepingMold)
                }
            ),

            // 7) Tier-3 specialist for cost-effective mid-game consistency
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Tier3PlateauSpecialist",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                maxTier: MutationTier.Tier3,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience,
                    MutationCategory.Fungicide
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin)
                }
            ),

            // 8) Delayed spike strategy with high late-game ceiling
            new ParameterizedSpendingStrategy(
                strategyName: "TST_LateGameSpike",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CatabolicRebirth, GameBalance.CatabolicRebirthMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Resistance)
            ),

            // 9) Balanced all-rounder with stable control path
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedGeneralistControl",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),

            // 10) Balanced control variant: no preferred mycovariant list
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_NoPreferredMyco",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                }
            ),

            // 11) Rebirth attrition loop
            new ParameterizedSpendingStrategy(
                strategyName: "TST_RebirthAttrition",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation, MycovariantCategory.Resistance)
            ),

            // 12) Balanced control variant: max economy bias
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_MaxEconomy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),

            // 13) Tier1/Tier2 economy-resilience grinder
            new ParameterizedSpendingStrategy(
                strategyName: "TST_LowTierEconomyGrinder",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.GeneticDrift,
                    MutationCategory.CellularResilience
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MutatorPhenotype),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm),
                    new TargetMutationGoal(MutationIds.MycotoxinCatabolism)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Resistance)
            ),

            // 14) Tier1/Tier2 surge-control skirmisher
            new ParameterizedSpendingStrategy(
                strategyName: "TST_LowTierSurgeSkirmisher",
                prioritizeHighTier: true,
                economyBias: EconomyBias.Neutral,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.MycelialSurges,
                    MutationCategory.Fungicide
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.HyphalVectoring),
                    new TargetMutationGoal(MutationIds.MycotoxinPotentiation),
                    new TargetMutationGoal(MutationIds.ChitinFortification)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 4,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Fungicide, MycovariantCategory.Growth)
            ),

            // 15) Balanced control variant: minor economy bias
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_MinorEconomy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            )
        };

        private static readonly Dictionary<string, StrategyTheme> ExplicitStrategyThemesByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_HyperEconomyRamp"] = StrategyTheme.EconomyRamp,
                ["TST_EarlyReclaimerSwarm"] = StrategyTheme.Reclamation,
                ["TST_BalancedControl_AnabolicFirst"] = StrategyTheme.Control,
                ["TST_HyphalSurgeTempo"] = StrategyTheme.SurgeTempo,
                ["TST_FortressResilience"] = StrategyTheme.Defense,
                ["TST_OpportunisticCounterplay"] = StrategyTheme.Counterplay,
                ["TST_Tier3PlateauSpecialist"] = StrategyTheme.TierCap,
                ["TST_LateGameSpike"] = StrategyTheme.LateGameSpike,
                ["TST_BalancedGeneralistControl"] = StrategyTheme.Control,
                ["TST_BalancedControl_NoPreferredMyco"] = StrategyTheme.Control,
                ["TST_RebirthAttrition"] = StrategyTheme.Attrition,
                ["TST_BalancedControl_MaxEconomy"] = StrategyTheme.Control,
                ["TST_LowTierEconomyGrinder"] = StrategyTheme.TierCap,
                ["TST_LowTierSurgeSkirmisher"] = StrategyTheme.TierCap,
                ["TST_BalancedControl_MinorEconomy"] = StrategyTheme.Control,
                ["Grow>Defend>Kill"] = StrategyTheme.Defense,
                ["Grow>Kill>Reclaim(Econ)"] = StrategyTheme.EconomyRamp,
                ["Grow>Kill>Reclaim(Econ/Reclaim)"] = StrategyTheme.Reclamation,
                ["SurgeFreq_10_Hyphal"] = StrategyTheme.SurgeTempo,
                ["Best_MaxEcon_Surge10_HyphalSurge"] = StrategyTheme.SurgeTempo,
                ["Power Mutations Max Econ"] = StrategyTheme.LateGameSpike,
                ["Growth/Resilience"] = StrategyTheme.TierCap,
                ["TST_BalancedControl_AnabolicFirst"] = StrategyTheme.Control,
                ["TST_BalancedControl_MaxEconomy"] = StrategyTheme.Control,
            };

        public static readonly Dictionary<string, IMutationSpendingStrategy> ProvenStrategiesByName;
        public static readonly Dictionary<string, IMutationSpendingStrategy> TestingStrategiesByName;
        public static readonly Dictionary<string, IMutationSpendingStrategy> CampaignStrategiesByName;

        static AIRoster()
        {
            ProvenStrategiesByName = BuildStrategyDictionary(ProvenStrategies, nameof(ProvenStrategies));
            TestingStrategiesByName = BuildStrategyDictionary(TestingStrategies, nameof(TestingStrategies));
            CampaignStrategiesByName = BuildStrategyDictionary(CampaignStrategies, nameof(CampaignStrategies));
        }

        private static Dictionary<string, IMutationSpendingStrategy> BuildStrategyDictionary(
            IEnumerable<IMutationSpendingStrategy> strategies,
            string strategySetName)
        {
            var dict = new Dictionary<string, IMutationSpendingStrategy>(StringComparer.OrdinalIgnoreCase);
            foreach (var strategy in strategies)
            {
                if (dict.ContainsKey(strategy.StrategyName))
                {
                    throw new InvalidOperationException(
                        $"Duplicate strategy name '{strategy.StrategyName}' found in {strategySetName}. Strategy names must be unique within a set.");
                }

                dict[strategy.StrategyName] = strategy;
            }

            return dict;
        }

        /// <summary>
        /// Returns the specified number of strategies from the chosen strategy set.
        /// </summary>
        public static List<IMutationSpendingStrategy> GetStrategies(
            int numberOfPlayers,
            StrategySetEnum strategySet,
            Random? rng = null,
            StrategySelectionPolicy selectionPolicy = StrategySelectionPolicy.RandomUnique,
            int cycleIndex = 0)
        {
            var result = new List<IMutationSpendingStrategy>();
            if (numberOfPlayers <= 0)
            {
                return result;
            }

            rng ??= new Random();
            var sourceStrategies = GetSourceStrategies(strategySet);
            var selected = SelectStrategiesByPolicy(sourceStrategies, numberOfPlayers, selectionPolicy, cycleIndex, rng);
            result.AddRange(selected);

            int remaining = numberOfPlayers - result.Count;
            for (int i = 1; i <= remaining; i++)
            {
                result.Add(new RandomMutationSpendingStrategy($"LegacyRandom #{i}"));
            }

            return result;
        }

        public static IReadOnlyList<StrategyProfile> GetStrategyProfiles(StrategySetEnum strategySet)
        {
            return GetSourceStrategies(strategySet)
                .Select(s => new StrategyProfile(
                    s.StrategyName,
                    strategySet,
                    GetThemeForStrategy(s),
                    BuildIntentLabel(s)))
                .ToList();
        }

        public static List<IMutationSpendingStrategy> GetStrategiesByName(
            StrategySetEnum strategySet,
            IEnumerable<string> strategyNames,
            out List<string> missingNames)
        {
            var requestedNames = strategyNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToList();

            var sourceByName = GetStrategyDictionary(strategySet);
            var selected = new List<IMutationSpendingStrategy>(requestedNames.Count);
            missingNames = new List<string>();

            foreach (var strategyName in requestedNames)
            {
                if (!sourceByName.TryGetValue(strategyName, out var strategy))
                {
                    missingNames.Add(strategyName);
                    continue;
                }

                selected.Add(strategy);
            }

            return selected;
        }

        public static StrategyTheme GetThemeForStrategy(IMutationSpendingStrategy strategy)
        {
            if (ExplicitStrategyThemesByName.TryGetValue(strategy.StrategyName, out var explicitTheme))
            {
                return explicitTheme;
            }

            var name = strategy.StrategyName;
            if (name.Contains("Surge", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.SurgeTempo;
            if (name.Contains("Reclaim", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.Reclamation;
            if (name.Contains("Econ", StringComparison.OrdinalIgnoreCase) || name.Contains("Mutate", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.EconomyRamp;
            if (name.Contains("Toxin", StringComparison.OrdinalIgnoreCase) || name.Contains("Kill", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.Offense;
            if (name.Contains("Resilience", StringComparison.OrdinalIgnoreCase) || name.Contains("Defend", StringComparison.OrdinalIgnoreCase) || name.Contains("Resistance", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.Defense;
            if (name.Contains("Vector", StringComparison.OrdinalIgnoreCase) || name.Contains("Mobility", StringComparison.OrdinalIgnoreCase)) return StrategyTheme.Mobility;
            return StrategyTheme.Balanced;
        }

        private static string BuildIntentLabel(IMutationSpendingStrategy strategy)
        {
            return GetThemeForStrategy(strategy) switch
            {
                StrategyTheme.EconomyRamp => "Front-load economy and scale into high-tier pressure",
                StrategyTheme.Reclamation => "Retake territory and convert attrition into board control",
                StrategyTheme.Offense => "Maximize kill pressure and toxin conversion",
                StrategyTheme.SurgeTempo => "Leverage surge windows and timing bursts",
                StrategyTheme.Defense => "Stabilize with resilient growth before counter-attacking",
                StrategyTheme.Counterplay => "Flexible line to respond to opponent mutation plans",
                StrategyTheme.Mobility => "Create angle pressure with movement-heavy upgrades",
                StrategyTheme.Attrition => "Win through long-cycle death and rebirth exchanges",
                StrategyTheme.LateGameSpike => "Bank resources for high-impact late upgrades",
                StrategyTheme.TierCap => "Concentrate value in constrained tier bands",
                _ => "Balanced all-purpose mutation progression"
            };
        }

        private static List<IMutationSpendingStrategy> GetSourceStrategies(StrategySetEnum strategySet)
        {
            return strategySet switch
            {
                StrategySetEnum.Proven => ProvenStrategies,
                StrategySetEnum.Testing => TestingStrategies,
                StrategySetEnum.Mycovariants => MycovariantPermutations(),
                StrategySetEnum.Campaign => CampaignStrategies,
                _ => throw new ArgumentException($"Unknown strategy set: {strategySet}", nameof(strategySet))
            };
        }

        private static Dictionary<string, IMutationSpendingStrategy> GetStrategyDictionary(StrategySetEnum strategySet)
        {
            return strategySet switch
            {
                StrategySetEnum.Proven => ProvenStrategiesByName,
                StrategySetEnum.Testing => TestingStrategiesByName,
                StrategySetEnum.Campaign => CampaignStrategiesByName,
                StrategySetEnum.Mycovariants => BuildStrategyDictionary(MycovariantPermutations(), nameof(MycovariantPermutations)),
                _ => throw new ArgumentException($"Unknown strategy set: {strategySet}", nameof(strategySet))
            };
        }

        private static List<IMutationSpendingStrategy> SelectStrategiesByPolicy(
            List<IMutationSpendingStrategy> sourceStrategies,
            int requestedCount,
            StrategySelectionPolicy selectionPolicy,
            int cycleIndex,
            Random rng)
        {
            var maxAvailable = Math.Min(requestedCount, sourceStrategies.Count);
            if (maxAvailable <= 0)
            {
                return new List<IMutationSpendingStrategy>();
            }

            return selectionPolicy switch
            {
                StrategySelectionPolicy.CoverageBalanced => SelectCoverageBalanced(sourceStrategies, maxAvailable, rng),
                StrategySelectionPolicy.StratifiedCycle => SelectStratifiedCycle(sourceStrategies, maxAvailable, cycleIndex),
                _ => sourceStrategies.OrderBy(_ => rng.Next()).Take(maxAvailable).ToList(),
            };
        }

        private static List<IMutationSpendingStrategy> SelectCoverageBalanced(
            List<IMutationSpendingStrategy> sourceStrategies,
            int requestedCount,
            Random rng)
        {
            var shuffled = sourceStrategies.OrderBy(_ => rng.Next()).ToList();
            var selected = new List<IMutationSpendingStrategy>(requestedCount);
            var seenThemes = new HashSet<StrategyTheme>();

            foreach (var strategy in shuffled)
            {
                var theme = GetThemeForStrategy(strategy);
                if (seenThemes.Add(theme))
                {
                    selected.Add(strategy);
                    if (selected.Count >= requestedCount)
                    {
                        return selected;
                    }
                }
            }

            foreach (var strategy in shuffled)
            {
                if (selected.Contains(strategy))
                {
                    continue;
                }

                selected.Add(strategy);
                if (selected.Count >= requestedCount)
                {
                    break;
                }
            }

            return selected;
        }

        private static List<IMutationSpendingStrategy> SelectStratifiedCycle(
            List<IMutationSpendingStrategy> sourceStrategies,
            int requestedCount,
            int cycleIndex)
        {
            var ordered = sourceStrategies
                .OrderBy(s => GetThemeForStrategy(s))
                .ThenBy(s => s.StrategyName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int offset = ordered.Count == 0 ? 0 : ((cycleIndex % ordered.Count) + ordered.Count) % ordered.Count;
            var selected = new List<IMutationSpendingStrategy>(requestedCount);

            for (int i = 0; i < requestedCount; i++)
            {
                selected.Add(ordered[(offset + i) % ordered.Count]);
            }

            return selected;
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
