using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Logging;
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

    public enum StrategyStatus
    {
        Testing,
        Proven,
        Loser
    }

    public sealed class StrategyProfile
    {
        public StrategyProfile(
            string strategyName,
            StrategySetEnum strategySet,
            StrategyTheme theme,
            StrategyStatus status,
            string intent,
            StrategyPowerTier powerTier,
            StrategyRole role,
            StrategyLifecycle lifecycle,
            IReadOnlyCollection<DifficultyBand> difficultyBands,
            CampaignDifficulty? campaignDifficulty,
            StrategyPool pools,
            IReadOnlyCollection<CounterTag> favoredAgainst,
            IReadOnlyCollection<CounterTag> weakAgainst,
            string notes)
        {
            StrategyName = strategyName;
            StrategySet = strategySet;
            Theme = theme;
            Status = status;
            Intent = intent;
            PowerTier = powerTier;
            Role = role;
            Lifecycle = lifecycle;
            DifficultyBands = difficultyBands;
            CampaignDifficulty = campaignDifficulty;
            Pools = pools;
            FavoredAgainst = favoredAgainst;
            WeakAgainst = weakAgainst;
            Notes = notes;
        }

        public string StrategyName { get; }
        public StrategySetEnum StrategySet { get; }
        public StrategyTheme Theme { get; }
        public StrategyStatus Status { get; }
        public string Intent { get; }
        public StrategyPowerTier PowerTier { get; }
        public StrategyRole Role { get; }
        public StrategyLifecycle Lifecycle { get; }
        public IReadOnlyCollection<DifficultyBand> DifficultyBands { get; }
        public CampaignDifficulty? CampaignDifficulty { get; }
        public StrategyPool Pools { get; }
        public IReadOnlyCollection<CounterTag> FavoredAgainst { get; }
        public IReadOnlyCollection<CounterTag> WeakAgainst { get; }
        public string Notes { get; }
    }

    public static class AIRoster
    {
        /// <summary>
        /// All curated, proven AI strategies for use in UI and simulation.
        /// </summary>
        private static readonly List<IMutationSpendingStrategy> RawProvenStrategies = new List<IMutationSpendingStrategy>
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
            // Proven promotion: AI13 shell with anabolic-first opener
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI13_AnabolicFirst",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),
            // Proven promotion: original AI13 shell
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI13_BalancedControl_MaxEconomy",
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
            // Experimental beacon/regression cascade line for balance testing
            new ParameterizedSpendingStrategy(
                strategyName: "TST_AnabolicBeaconNecroRegressionCascade",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion, 1),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, GameBalance.ChemotacticBeaconMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 5,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Growth)
            ),
            // Experimental creeping/bloom/regression cascade line with anabolic opener
            new ParameterizedSpendingStrategy(
                strategyName: "TST_AnabolicCreepingNecroRegressionCascade",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion, 1),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Growth)
            ),
            // Experimental creeping/bloom/regression cascade line without anabolic opener
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CreepingNecroRegressionCascade",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Growth)
            ),
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
            ),
            // Campaign balance harness: safe player-proxy baseline (also in RawCampaignStrategies)
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignPlayer_SafeBaseline",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 2),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Economy, MycovariantCategory.Resistance)
            ),
        };

        /// <summary>
        /// Campaign strategies: mirror ProvenStrategies but with simple names AI1..AI N for UI draft/select purposes.
        /// </summary>
        private static readonly List<IMutationSpendingStrategy> RawCampaignStrategies = new List<IMutationSpendingStrategy>
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
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon },
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
                maxTier: MutationTier.Tier5
            ),
            // AI12
            new ParameterizedSpendingStrategy(
                strategyName: "AI12",
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
            // AI13
            new ParameterizedSpendingStrategy(
                strategyName: "AI13",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth)
            ),
            // Curated campaign aliases for modern roster use in board presets and campaign-balance harness.
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_TierCap_GrowthResilience_Easy",
                prioritizeHighTier: true,
                maxTier: MutationTier.Tier3,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Reclaim_Scavenger_Easy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                maxTier: MutationTier.Tier3,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.Necrosporulation, 2),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration, 2),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, 2)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation, MycovariantCategory.Economy)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Surge_Pulsar_Easy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                maxTier: MutationTier.Tier3,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge, 3),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 2),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, 1)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChitinFortification },
                surgeAttemptTurnFrequency: 8,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Resistance)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Reclaim_InfiltrationSurge_Easy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration, GameBalance.NecrohyphalInfiltrationMaxLevel),
                    new TargetMutationGoal(MutationIds.HyphalSurge, GameBalance.HyphalSurgeMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 8,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation, MycovariantCategory.Growth)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Defense_ResilientShell_Easy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 5),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 5),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae, 5)
                },
                surgePriorityIds: new List<int> { MutationIds.ChitinFortification },
                surgeAttemptTurnFrequency: 8,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance, MycovariantCategory.Growth)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Defense_ReclaimShell_Easy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                maxTier: MutationTier.Tier3,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 5),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 3),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Surge_BeaconTempo_Medium",
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Control_AnabolicRebirth_Medium",
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
                strategyName: "CMP_Surge_GrowthTempo_Medium",
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
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Growth_Pressure_Medium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 3),
                    new TargetMutationGoal(MutationIds.CreepingMold, 3),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae, 2),
                    new TargetMutationGoal(MutationIds.AnabolicInversion, 1)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Economy)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Bloom_FortifyMimic_Medium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                maxTier: MutationTier.Tier4,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 5),
                    new TargetMutationGoal(MutationIds.MimeticResilience, GameBalance.MimeticResilienceMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.ChitinFortification, MutationIds.MimeticResilience },
                surgeAttemptTurnFrequency: 9,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Economy_KillReclaim_Medium",
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
                strategyName: "CMP_Economy_TempoReclaim_Medium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 3),
                    new TargetMutationGoal(MutationIds.NecrohyphalInfiltration, 2),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, 5),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.Necrosporulation, 1)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 8,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Bloom_CreepingNecro_Medium",
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
                strategyName: "CMP_Bloom_BeaconRegression_Medium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.OntogenicRegression),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Bloom_AnabolicRegression_Medium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.OntogenicRegression),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Control_AnabolicFirst_Hard",
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
            new ParameterizedSpendingStrategy(
                strategyName: "CMP_Economy_LateSpike_Hard",
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
                strategyName: "CMP_Bloom_CreepingRegression_Elite",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.OntogenicRegression),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy)
            ),
            // Campaign balance harness: safe player-proxy baseline
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignPlayer_SafeBaseline",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 2),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Economy, MycovariantCategory.Resistance)
            ),
            // Training mold: slow resilient defense with visible resistant cells
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Training_ResilientMycelium",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 5),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 5),
                    new TargetMutationGoal(MutationIds.RegenerativeHyphae, 5)
                },
                surgePriorityIds: new List<int> { MutationIds.ChitinFortification },
                surgeAttemptTurnFrequency: 8,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance, MycovariantCategory.Growth)
            ),
            // Training mold: reckless growth that overextends and burns points on a bad tendril plan
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Training_Overextender",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.CreepingMold, 2),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 3),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.TendrilNorthwest, 5),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, 2)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth)
            ),
            // Training mold: slow fungicide/resilience turtle that eventually shows adjacent poison
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Training_ToxicTurtle",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotoxinPotentiation, 3),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 5),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin, GameBalance.PutrefactiveMycotoxinMaxLevel),
                    new TargetMutationGoal(MutationIds.MycotoxinCatabolism)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Resistance, MycovariantCategory.Economy)
            )
        };

        /// <summary>
        /// Testing strategies for specific scenarios (not included in proven strategies)
        /// </summary>
        private static readonly List<IMutationSpendingStrategy> RawTestingStrategies = new List<IMutationSpendingStrategy>
        {
            // Canonical 8-player archetype harness for ongoing balance tuning.
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch01_GrowthResilience",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.HypersystemicRegeneration, GameBalance.HypersystemicRegenerationMaxLevel)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch02_ResilienceGrowth",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.CellularResilience,
                    MutationCategory.Growth,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel),
                    new TargetMutationGoal(MutationIds.HyphalSurge, 1),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 1),
                    new TargetMutationGoal(MutationIds.MimeticResilience, 1),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.HypersystemicRegeneration, GameBalance.HypersystemicRegenerationMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChitinFortification, MutationIds.MimeticResilience },
                surgeAttemptTurnFrequency: 6
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch03_FungicideSurge",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Fungicide,
                    MutationCategory.MycelialSurges,
                    MutationCategory.CellularResilience
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotoxinTracer, 5),
                    new TargetMutationGoal(MutationIds.MycelialBloom, 7),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.MimeticResilience, 1),
                    new TargetMutationGoal(MutationIds.SporicidalBloom, 3),
                    new TargetMutationGoal(MutationIds.CompetitiveAntagonism, 1),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin, GameBalance.PutrefactiveMycotoxinMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveRejuvenation, GameBalance.PutrefactiveRejuvenationMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion, GameBalance.NecrotoxicConversionMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon, MutationIds.MimeticResilience, MutationIds.CompetitiveAntagonism },
                surgeAttemptTurnFrequency: 5
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch04_DriftGrowth",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.GeneticDrift,
                    MutationCategory.Growth
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion, GameBalance.AnabolicInversionMaxLevel),
                    new TargetMutationGoal(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, GameBalance.AdaptiveExpressionMaxLevel),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrophyticBloom, GameBalance.NecrophyticBloomMaxLevel)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch05_DriftResilience",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.GeneticDrift,
                    MutationCategory.CellularResilience,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion, GameBalance.AnabolicInversionMaxLevel),
                    new TargetMutationGoal(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel),
                    new TargetMutationGoal(MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression, GameBalance.AdaptiveExpressionMaxLevel),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.MimeticResilience, 1),
                    new TargetMutationGoal(MutationIds.CompetitiveAntagonism, 1),
                    new TargetMutationGoal(MutationIds.OntogenicRegression, GameBalance.OntogenicRegressionMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon, MutationIds.MimeticResilience, MutationIds.CompetitiveAntagonism },
                surgeAttemptTurnFrequency: 6
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch06_SurgeGrowth",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.MycelialSurges,
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge, 1),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 1),
                    new TargetMutationGoal(MutationIds.HyphalSurge, 2),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 2),
                    new TargetMutationGoal(MutationIds.CreepingMold, 1),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 5)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon, MutationIds.ChitinFortification },
                surgeAttemptTurnFrequency: 5
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch07_DriftFungicide",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.GeneticDrift,
                    MutationCategory.Fungicide,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotoxinTracer, 5),
                    new TargetMutationGoal(MutationIds.AnabolicInversion, GameBalance.AnabolicInversionMaxLevel),
                    new TargetMutationGoal(MutationIds.MycotoxinPotentiation, 5),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.MycotoxinTracer, 15),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift, GameBalance.HyperadaptiveDriftMaxLevel),
                    new TargetMutationGoal(MutationIds.SporicidalBloom, GameBalance.SporicidalBloomMaxLevel),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion, GameBalance.NecrotoxicConversionMaxLevel),
                    new TargetMutationGoal(MutationIds.PutrefactiveCascade, GameBalance.PutrefactiveCascadeMaxLevel)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 5
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_Arch08_SurgeResilience",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.MycelialSurges,
                    MutationCategory.CellularResilience,
                    MutationCategory.Growth
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyphalSurge, 1),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, 1),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 1),
                    new TargetMutationGoal(MutationIds.ChitinFortification, 1),
                    new TargetMutationGoal(MutationIds.ChronoresilientCytoplasm, 2),
                    new TargetMutationGoal(MutationIds.CreepingMold, GameBalance.CreepingMoldMaxLevel),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 2),
                    new TargetMutationGoal(MutationIds.HyphalSurge, 2),
                    new TargetMutationGoal(MutationIds.MycotropicInduction, GameBalance.MycotropicInductionMaxLevel),
                    new TargetMutationGoal(MutationIds.Necrosporulation, 1),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon, 3)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon, MutationIds.ChitinFortification },
                surgeAttemptTurnFrequency: 5
            ),

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

            // 4) Tempo surges with growth/drift backbone
            new ParameterizedSpendingStrategy(
                strategyName: "TST_HyphalSurgeTempo",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.GeneticDrift,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AdaptiveExpression),
                    new TargetMutationGoal(MutationIds.MycotropicInduction),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 5,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Economy, MycovariantCategory.Growth)
            ),

            // 4b) Diagnostic variant: same shell, but push surge activations and surge pickups slightly later
            new ParameterizedSpendingStrategy(
                strategyName: "TST_HyphalSurgeTempo_DelayedSurge",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.GeneticDrift,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AdaptiveExpression),
                    new TargetMutationGoal(MutationIds.MycotropicInduction),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon),
                    new TargetMutationGoal(MutationIds.HyphalSurge)
                },
                surgePriorityIds: new List<int> { MutationIds.ChemotacticBeacon, MutationIds.HyphalSurge },
                surgeAttemptTurnFrequency: 7,
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

            // 10b) Diagnostic variant: keep anabolic opener, but delay the necro conversion package
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_AnabolicFirst_DelayedNecro",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.Necrosporulation)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),

            // 10c) Diagnostic variant: keep anabolic opener, but delay catabolism-style conversion follow-through
            new ParameterizedSpendingStrategy(
                strategyName: "TST_BalancedControl_AnabolicFirst_DelayedCatabolism",
                prioritizeHighTier: true,
                economyBias: EconomyBias.ModerateEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth),
                    new TargetMutationGoal(MutationIds.MycotoxinCatabolism)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
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

            // 14) Low-tier fungicide/genetic skirmisher with anti-leader surge
            new ParameterizedSpendingStrategy(
                strategyName: "TST_LowTierSurgeSkirmisher",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Fungicide,
                    MutationCategory.GeneticDrift,
                    MutationCategory.MycelialSurges
                },
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.MycotoxinPotentiation),
                    new TargetMutationGoal(MutationIds.AdaptiveExpression),
                    new TargetMutationGoal(MutationIds.MycotoxinCatabolism),
                    new TargetMutationGoal(MutationIds.PutrefactiveMycotoxin),
                    new TargetMutationGoal(MutationIds.CompetitiveAntagonism),
                    new TargetMutationGoal(MutationIds.NecrotoxicConversion)
                },
                surgePriorityIds: new List<int> { MutationIds.CompetitiveAntagonism },
                surgeAttemptTurnFrequency: 5,
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Fungicide, MycovariantCategory.Resistance)
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
            ),

            // 16) Campaign mirror: AI7 hyphal surge/beacon line
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI7_Beacon",
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
                    new TargetMutationGoal(MutationIds.HyphalSurge),
                    new TargetMutationGoal(MutationIds.ChemotacticBeacon)
                },
                surgePriorityIds: new List<int> { MutationIds.HyphalSurge, MutationIds.ChemotacticBeacon },
                surgeAttemptTurnFrequency: 10,
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy
            ),

            // 17) Campaign mirror: AI12 balanced control anabolic-first
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst",
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

            // 18) Campaign mirror: AI13 balanced control max-economy
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI13_BalancedControl_MaxEconomy",
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

            // Temporary AI13 permutations for tuning analysis
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI13_AnabolicFirst",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth, MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_ModerateEconomy",
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
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_NoPreferredMyco",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_AnabolicFirst_ModerateEconomy",
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
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_AnabolicFirst_NoPreferredMyco",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                }
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Growth)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_AnabolicFirst_ReclamationOnlyMyco",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MaxEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
                    new TargetMutationGoal(MutationIds.Necrosporulation),
                    new TargetMutationGoal(MutationIds.CatabolicRebirth)
                },
                preferredMycovariantIds: MycovariantCategoryHelper.GetPreferredMycovariantIds(MycovariantCategory.Reclamation)
            ),
            new ParameterizedSpendingStrategy(
                strategyName: "TMP_AI13_AnabolicFirst_MinorEconomy",
                prioritizeHighTier: true,
                economyBias: EconomyBias.MinorEconomy,
                targetMutationGoals: new List<TargetMutationGoal>
                {
                    new TargetMutationGoal(MutationIds.AnabolicInversion),
                    new TargetMutationGoal(MutationIds.CreepingMold),
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
                ["TST_Arch01_GrowthResilience"] = StrategyTheme.Defense,
                ["TST_Arch02_ResilienceGrowth"] = StrategyTheme.Defense,
                ["TST_Arch03_FungicideSurge"] = StrategyTheme.Offense,
                ["TST_Arch04_DriftGrowth"] = StrategyTheme.EconomyRamp,
                ["TST_Arch05_DriftResilience"] = StrategyTheme.Counterplay,
                ["TST_Arch06_SurgeGrowth"] = StrategyTheme.SurgeTempo,
                ["TST_Arch07_DriftFungicide"] = StrategyTheme.Counterplay,
                ["TST_Arch08_SurgeResilience"] = StrategyTheme.SurgeTempo,
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
                ["TST_LowTierSurgeSkirmisher"] = StrategyTheme.Counterplay,
                ["TST_BalancedControl_MinorEconomy"] = StrategyTheme.Control,
                ["TST_CampaignMirror_AI13_AnabolicFirst"] = StrategyTheme.Control,
                ["TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco"] = StrategyTheme.Control,
                ["TST_CampaignMirror_AI13_BalancedControl_MaxEconomy"] = StrategyTheme.EconomyRamp,
                ["TST_AnabolicBeaconNecroRegressionCascade"] = StrategyTheme.Control,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = StrategyTheme.Control,
                ["TST_CreepingNecroRegressionCascade"] = StrategyTheme.Control,
                ["CMP_TierCap_GrowthResilience_Easy"] = StrategyTheme.TierCap,
                ["CMP_Reclaim_Scavenger_Easy"] = StrategyTheme.Reclamation,
                ["CMP_Surge_Pulsar_Easy"] = StrategyTheme.SurgeTempo,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = StrategyTheme.Reclamation,
                ["CMP_Defense_ResilientShell_Easy"] = StrategyTheme.Defense,
                ["CMP_Defense_ReclaimShell_Easy"] = StrategyTheme.Defense,
                ["CMP_Surge_BeaconTempo_Medium"] = StrategyTheme.SurgeTempo,
                ["CMP_Control_AnabolicRebirth_Medium"] = StrategyTheme.Control,
                ["CMP_Surge_GrowthTempo_Medium"] = StrategyTheme.SurgeTempo,
                ["CMP_Growth_Pressure_Medium"] = StrategyTheme.Offense,
                ["CMP_Bloom_FortifyMimic_Medium"] = StrategyTheme.Attrition,
                ["CMP_Economy_KillReclaim_Medium"] = StrategyTheme.EconomyRamp,
                ["CMP_Economy_TempoReclaim_Medium"] = StrategyTheme.EconomyRamp,
                ["CMP_Bloom_CreepingNecro_Medium"] = StrategyTheme.Control,
                ["CMP_Bloom_BeaconRegression_Medium"] = StrategyTheme.Control,
                ["CMP_Bloom_AnabolicRegression_Medium"] = StrategyTheme.Control,
                ["CMP_Control_AnabolicFirst_Hard"] = StrategyTheme.Control,
                ["CMP_Economy_LateSpike_Hard"] = StrategyTheme.LateGameSpike,
                ["CMP_Bloom_CreepingRegression_Elite"] = StrategyTheme.Control,
                ["TST_CampaignPlayer_SafeBaseline"] = StrategyTheme.Control,
                ["TST_Training_ResilientMycelium"] = StrategyTheme.Defense,
                ["TST_Training_Overextender"] = StrategyTheme.Mobility,
                ["TST_Training_ToxicTurtle"] = StrategyTheme.Attrition,
                ["Grow>Defend>Kill"] = StrategyTheme.Defense,
                ["Grow>Kill>Reclaim(Econ)"] = StrategyTheme.EconomyRamp,
                ["Grow>Kill>Reclaim(Econ/Reclaim)"] = StrategyTheme.Reclamation,
                ["SurgeFreq_10_Hyphal"] = StrategyTheme.SurgeTempo,
                ["Best_MaxEcon_Surge10_HyphalSurge"] = StrategyTheme.SurgeTempo,
                ["Power Mutations Max Econ"] = StrategyTheme.LateGameSpike,
                ["Growth/Resilience"] = StrategyTheme.TierCap,
            };

        private static readonly Dictionary<string, StrategyStatus> ExplicitStrategyStatusesByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_AnabolicBeaconNecroRegressionCascade"] = StrategyStatus.Proven,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = StrategyStatus.Proven,
                ["TST_CreepingNecroRegressionCascade"] = StrategyStatus.Proven,
                ["CMP_Reclaim_Scavenger_Easy"] = StrategyStatus.Proven,
                ["CMP_Surge_Pulsar_Easy"] = StrategyStatus.Proven,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = StrategyStatus.Proven,
                ["CMP_Defense_ResilientShell_Easy"] = StrategyStatus.Proven,
                ["CMP_Defense_ReclaimShell_Easy"] = StrategyStatus.Proven,
                ["CMP_Surge_BeaconTempo_Medium"] = StrategyStatus.Proven,
                ["CMP_Control_AnabolicRebirth_Medium"] = StrategyStatus.Proven,
                ["CMP_Surge_GrowthTempo_Medium"] = StrategyStatus.Proven,
                ["CMP_Growth_Pressure_Medium"] = StrategyStatus.Proven,
                ["CMP_Bloom_FortifyMimic_Medium"] = StrategyStatus.Proven,
                ["CMP_Economy_TempoReclaim_Medium"] = StrategyStatus.Proven,
                ["TST_CampaignPlayer_SafeBaseline"] = StrategyStatus.Testing,
                ["TST_Training_ResilientMycelium"] = StrategyStatus.Testing,
                ["TST_Training_Overextender"] = StrategyStatus.Testing,
                ["TST_Training_ToxicTurtle"] = StrategyStatus.Testing,
            };

        private static readonly Dictionary<string, StrategyPowerTier> ExplicitPowerTiersByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_Arch01_GrowthResilience"] = StrategyPowerTier.Standard,
                ["TST_Arch02_ResilienceGrowth"] = StrategyPowerTier.Standard,
                ["TST_Arch03_FungicideSurge"] = StrategyPowerTier.Spike,
                ["TST_Arch04_DriftGrowth"] = StrategyPowerTier.Strong,
                ["TST_Arch05_DriftResilience"] = StrategyPowerTier.Standard,
                ["TST_Arch06_SurgeGrowth"] = StrategyPowerTier.Standard,
                ["TST_Arch07_DriftFungicide"] = StrategyPowerTier.Strong,
                ["TST_Arch08_SurgeResilience"] = StrategyPowerTier.Standard,
                ["TST_BalancedControl_AnabolicFirst"] = StrategyPowerTier.Strong,
                ["TST_BalancedControl_MaxEconomy"] = StrategyPowerTier.Strong,
                ["TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst"] = StrategyPowerTier.Strong,
                ["TST_CampaignMirror_AI13_AnabolicFirst"] = StrategyPowerTier.Strong,
                ["TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco"] = StrategyPowerTier.Strong,
                ["TST_CampaignMirror_AI13_BalancedControl_MaxEconomy"] = StrategyPowerTier.Strong,
                ["TST_FortressResilience"] = StrategyPowerTier.Weak,
                ["TST_OpportunisticCounterplay"] = StrategyPowerTier.Weak,
                ["TST_RebirthAttrition"] = StrategyPowerTier.Weak,
                ["TST_LowTierEconomyGrinder"] = StrategyPowerTier.Weak,
                ["TST_LowTierSurgeSkirmisher"] = StrategyPowerTier.Weak,
                ["Growth/Resilience"] = StrategyPowerTier.Weak,
                ["AI6"] = StrategyPowerTier.Weak,
                ["AI12"] = StrategyPowerTier.Weak,
                ["AI13"] = StrategyPowerTier.Strong,
                ["AI1"] = StrategyPowerTier.Strong,
                ["AI2"] = StrategyPowerTier.Strong,
                ["AI3"] = StrategyPowerTier.Strong,
                ["AI10"] = StrategyPowerTier.Strong,
                ["TST_CreepingNecroRegressionCascade"] = StrategyPowerTier.Strong,
                ["CMP_Bloom_CreepingRegression_Elite"] = StrategyPowerTier.Strong,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = StrategyPowerTier.Standard,
                ["TST_AnabolicBeaconNecroRegressionCascade"] = StrategyPowerTier.Standard,
                ["CMP_Bloom_BeaconRegression_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Bloom_AnabolicRegression_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Economy_KillReclaim_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Bloom_CreepingNecro_Medium"] = StrategyPowerTier.Standard,
                ["CMP_TierCap_GrowthResilience_Easy"] = StrategyPowerTier.Weak,
                ["CMP_Reclaim_Scavenger_Easy"] = StrategyPowerTier.Weak,
                ["CMP_Surge_Pulsar_Easy"] = StrategyPowerTier.Standard,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = StrategyPowerTier.Weak,
                ["CMP_Defense_ResilientShell_Easy"] = StrategyPowerTier.Weak,
                ["CMP_Defense_ReclaimShell_Easy"] = StrategyPowerTier.Weak,
                ["CMP_Surge_BeaconTempo_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Control_AnabolicRebirth_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Surge_GrowthTempo_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Growth_Pressure_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Bloom_FortifyMimic_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Economy_TempoReclaim_Medium"] = StrategyPowerTier.Standard,
                ["CMP_Control_AnabolicFirst_Hard"] = StrategyPowerTier.Strong,
                ["TST_LateGameSpike"] = StrategyPowerTier.Spike,
                ["Power Mutations Max Econ"] = StrategyPowerTier.Spike,
                ["Best_MaxEcon_Surge10_HyphalSurge"] = StrategyPowerTier.Spike,
                ["SurgeFreq_10_Hyphal"] = StrategyPowerTier.Spike,
            };

        private static readonly Dictionary<string, StrategyRole> ExplicitRolesByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_Arch01_GrowthResilience"] = StrategyRole.Experimental,
                ["TST_Arch02_ResilienceGrowth"] = StrategyRole.Experimental,
                ["TST_Arch03_FungicideSurge"] = StrategyRole.Experimental,
                ["TST_Arch04_DriftGrowth"] = StrategyRole.Experimental,
                ["TST_Arch05_DriftResilience"] = StrategyRole.Experimental,
                ["TST_Arch06_SurgeGrowth"] = StrategyRole.Experimental,
                ["TST_Arch07_DriftFungicide"] = StrategyRole.Experimental,
                ["TST_Arch08_SurgeResilience"] = StrategyRole.Experimental,
                ["TST_HyperEconomyRamp"] = StrategyRole.Experimental,
                ["TST_EarlyReclaimerSwarm"] = StrategyRole.Experimental,
                ["TST_ToxinSiege"] = StrategyRole.Experimental,
                ["TST_HyphalSurgeTempo"] = StrategyRole.Experimental,
                ["TST_FortressResilience"] = StrategyRole.Training,
                ["TST_OpportunisticCounterplay"] = StrategyRole.Training,
                ["TST_Tier3PlateauSpecialist"] = StrategyRole.Experimental,
                ["TST_LateGameSpike"] = StrategyRole.Experimental,
                ["TST_BalancedGeneralistControl"] = StrategyRole.Experimental,
                ["TST_BalancedControl_NoPreferredMyco"] = StrategyRole.Experimental,
                ["TST_RebirthAttrition"] = StrategyRole.Training,
                ["TST_BalancedControl_MaxEconomy"] = StrategyRole.Experimental,
                ["TST_LowTierEconomyGrinder"] = StrategyRole.Training,
                ["TST_LowTierSurgeSkirmisher"] = StrategyRole.Training,
                ["TST_BalancedControl_MinorEconomy"] = StrategyRole.Experimental,
                ["TST_CampaignMirror_AI7_Hyphal"] = StrategyRole.Experimental,
                ["TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst"] = StrategyRole.Experimental,
                ["TST_CampaignMirror_AI13_BalancedControl_MaxEconomy"] = StrategyRole.Experimental,
                ["TST_AnabolicBeaconNecroRegressionCascade"] = StrategyRole.Experimental,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = StrategyRole.Experimental,
                ["TST_CreepingNecroRegressionCascade"] = StrategyRole.Boss,
                ["CMP_TierCap_GrowthResilience_Easy"] = StrategyRole.Training,
                ["CMP_Reclaim_Scavenger_Easy"] = StrategyRole.Training,
                ["CMP_Surge_Pulsar_Easy"] = StrategyRole.Experimental,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = StrategyRole.Training,
                ["CMP_Defense_ResilientShell_Easy"] = StrategyRole.Training,
                ["CMP_Defense_ReclaimShell_Easy"] = StrategyRole.Training,
                ["CMP_Surge_BeaconTempo_Medium"] = StrategyRole.Experimental,
                ["CMP_Control_AnabolicRebirth_Medium"] = StrategyRole.Experimental,
                ["CMP_Surge_GrowthTempo_Medium"] = StrategyRole.Experimental,
                ["CMP_Growth_Pressure_Medium"] = StrategyRole.Experimental,
                ["CMP_Bloom_FortifyMimic_Medium"] = StrategyRole.Experimental,
                ["CMP_Economy_KillReclaim_Medium"] = StrategyRole.Experimental,
                ["CMP_Economy_TempoReclaim_Medium"] = StrategyRole.Experimental,
                ["CMP_Bloom_CreepingNecro_Medium"] = StrategyRole.Experimental,
                ["CMP_Bloom_BeaconRegression_Medium"] = StrategyRole.Experimental,
                ["CMP_Bloom_AnabolicRegression_Medium"] = StrategyRole.Experimental,
                ["CMP_Control_AnabolicFirst_Hard"] = StrategyRole.Experimental,
                ["CMP_Economy_LateSpike_Hard"] = StrategyRole.Boss,
                ["CMP_Bloom_CreepingRegression_Elite"] = StrategyRole.Boss,
                ["TST_CampaignPlayer_SafeBaseline"] = StrategyRole.Experimental,
                ["TST_Training_ResilientMycelium"] = StrategyRole.Training,
                ["TST_Training_Overextender"] = StrategyRole.Training,
                ["TST_Training_ToxicTurtle"] = StrategyRole.Training,
                ["TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco"] = StrategyRole.Experimental,
                ["AI1"] = StrategyRole.Boss,
                ["AI2"] = StrategyRole.Boss,
                ["AI3"] = StrategyRole.Boss,
                ["AI10"] = StrategyRole.Boss,
                ["AI13"] = StrategyRole.Boss,
                ["AI6"] = StrategyRole.Training,
                ["AI12"] = StrategyRole.Training,
                ["AI13"] = StrategyRole.Training,
            };

        private static readonly Dictionary<string, StrategyLifecycle> ExplicitLifecycleByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_Arch01_GrowthResilience"] = StrategyLifecycle.Active,
                ["TST_Arch02_ResilienceGrowth"] = StrategyLifecycle.Active,
                ["TST_Arch03_FungicideSurge"] = StrategyLifecycle.Active,
                ["TST_Arch04_DriftGrowth"] = StrategyLifecycle.Active,
                ["TST_Arch05_DriftResilience"] = StrategyLifecycle.Active,
                ["TST_Arch06_SurgeGrowth"] = StrategyLifecycle.Active,
                ["TST_Arch07_DriftFungicide"] = StrategyLifecycle.Active,
                ["TST_Arch08_SurgeResilience"] = StrategyLifecycle.Active,
                ["TST_BalancedControl_AnabolicFirst"] = StrategyLifecycle.NeedsTuning,
                ["TST_BalancedControl_MaxEconomy"] = StrategyLifecycle.NeedsTuning,
                ["TST_CampaignMirror_AI12_BalancedControl_AnabolicFirst"] = StrategyLifecycle.NeedsTuning,
                ["TST_CampaignMirror_AI13_BalancedControl_MaxEconomy"] = StrategyLifecycle.NeedsTuning,
                ["TST_AnabolicBeaconNecroRegressionCascade"] = StrategyLifecycle.Active,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = StrategyLifecycle.Active,
                ["TST_CreepingNecroRegressionCascade"] = StrategyLifecycle.Active,
                ["CMP_TierCap_GrowthResilience_Easy"] = StrategyLifecycle.Active,
                ["CMP_Reclaim_Scavenger_Easy"] = StrategyLifecycle.Active,
                ["CMP_Surge_Pulsar_Easy"] = StrategyLifecycle.Active,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = StrategyLifecycle.Active,
                ["CMP_Defense_ResilientShell_Easy"] = StrategyLifecycle.Active,
                ["CMP_Defense_ReclaimShell_Easy"] = StrategyLifecycle.Active,
                ["CMP_Surge_BeaconTempo_Medium"] = StrategyLifecycle.Active,
                ["CMP_Control_AnabolicRebirth_Medium"] = StrategyLifecycle.Active,
                ["CMP_Surge_GrowthTempo_Medium"] = StrategyLifecycle.Active,
                ["CMP_Growth_Pressure_Medium"] = StrategyLifecycle.Active,
                ["CMP_Bloom_FortifyMimic_Medium"] = StrategyLifecycle.Active,
                ["CMP_Economy_KillReclaim_Medium"] = StrategyLifecycle.Active,
                ["CMP_Economy_TempoReclaim_Medium"] = StrategyLifecycle.Active,
                ["CMP_Bloom_CreepingNecro_Medium"] = StrategyLifecycle.Active,
                ["CMP_Bloom_BeaconRegression_Medium"] = StrategyLifecycle.Active,
                ["CMP_Bloom_AnabolicRegression_Medium"] = StrategyLifecycle.Active,
                ["CMP_Control_AnabolicFirst_Hard"] = StrategyLifecycle.Active,
                ["CMP_Economy_LateSpike_Hard"] = StrategyLifecycle.Active,
                ["CMP_Bloom_CreepingRegression_Elite"] = StrategyLifecycle.Active,
                ["TST_CampaignPlayer_SafeBaseline"] = StrategyLifecycle.Active,
                ["TST_Training_ResilientMycelium"] = StrategyLifecycle.Active,
                ["TST_Training_Overextender"] = StrategyLifecycle.Active,
                ["TST_Training_ToxicTurtle"] = StrategyLifecycle.Active,
                ["TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco"] = StrategyLifecycle.Active,
                ["TST_FortressResilience"] = StrategyLifecycle.NeedsTuning,
                ["TST_OpportunisticCounterplay"] = StrategyLifecycle.NeedsTuning,
                ["TST_RebirthAttrition"] = StrategyLifecycle.NeedsTuning,
            };

        private static readonly Dictionary<string, DifficultyBand[]> ExplicitDifficultyBandsByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_Arch01_GrowthResilience"] = new[] { DifficultyBand.Normal },
                ["TST_Arch02_ResilienceGrowth"] = new[] { DifficultyBand.Normal },
                ["TST_Arch03_FungicideSurge"] = new[] { DifficultyBand.Hard },
                ["TST_Arch04_DriftGrowth"] = new[] { DifficultyBand.Hard },
                ["TST_Arch05_DriftResilience"] = new[] { DifficultyBand.Normal },
                ["TST_Arch06_SurgeGrowth"] = new[] { DifficultyBand.Normal },
                ["TST_Arch07_DriftFungicide"] = new[] { DifficultyBand.Hard },
                ["TST_Arch08_SurgeResilience"] = new[] { DifficultyBand.Normal },
                ["AI6"] = new[] { DifficultyBand.Easy },
                ["AI12"] = new[] { DifficultyBand.Easy },
                ["AI13"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["AI7"] = new[] { DifficultyBand.Normal },
                ["AI8"] = new[] { DifficultyBand.Normal },
                ["AI9"] = new[] { DifficultyBand.Normal },
                ["AI11"] = new[] { DifficultyBand.Normal },
                ["AI1"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["AI2"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["AI3"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["AI10"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_LowTierEconomyGrinder"] = new[] { DifficultyBand.Easy },
                ["TST_LowTierSurgeSkirmisher"] = new[] { DifficultyBand.Easy },
                ["TST_BalancedGeneralistControl"] = new[] { DifficultyBand.Normal },
                ["TST_BalancedControl_AnabolicFirst"] = new[] { DifficultyBand.Hard },
                ["TST_BalancedControl_MaxEconomy"] = new[] { DifficultyBand.Hard },
                ["TST_CampaignMirror_AI13_AnabolicFirst"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_CampaignMirror_AI13_AnabolicFirst_GrowthOnlyMyco"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_CampaignMirror_AI13_BalancedControl_MaxEconomy"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_AnabolicBeaconNecroRegressionCascade"] = new[] { DifficultyBand.Normal },
                ["TST_AnabolicCreepingNecroRegressionCascade"] = new[] { DifficultyBand.Normal },
                ["CMP_TierCap_GrowthResilience_Easy"] = new[] { DifficultyBand.Easy },
                ["CMP_Reclaim_Scavenger_Easy"] = new[] { DifficultyBand.Easy },
                ["CMP_Surge_Pulsar_Easy"] = new[] { DifficultyBand.Normal },
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = new[] { DifficultyBand.Easy },
                ["CMP_Defense_ResilientShell_Easy"] = new[] { DifficultyBand.Easy },
                ["CMP_Defense_ReclaimShell_Easy"] = new[] { DifficultyBand.Easy },
                ["CMP_Surge_BeaconTempo_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Control_AnabolicRebirth_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Surge_GrowthTempo_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Growth_Pressure_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Bloom_FortifyMimic_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Economy_KillReclaim_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Economy_TempoReclaim_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Bloom_CreepingNecro_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Bloom_BeaconRegression_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Bloom_AnabolicRegression_Medium"] = new[] { DifficultyBand.Normal },
                ["CMP_Control_AnabolicFirst_Hard"] = new[] { DifficultyBand.Hard },
                ["CMP_Economy_LateSpike_Hard"] = new[] { DifficultyBand.Hard },
                ["CMP_Bloom_CreepingRegression_Elite"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_CreepingNecroRegressionCascade"] = new[] { DifficultyBand.Hard, DifficultyBand.Elite },
                ["TST_CampaignPlayer_SafeBaseline"] = new[] { DifficultyBand.Normal },
                ["TST_Training_ResilientMycelium"] = new[] { DifficultyBand.Easy },
                ["TST_Training_Overextender"] = new[] { DifficultyBand.Easy },
                ["TST_Training_ToxicTurtle"] = new[] { DifficultyBand.Easy },
            };

        private static readonly Dictionary<string, CampaignDifficulty> ExplicitCampaignDifficultyByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AI6"] = CampaignDifficulty.Training,
                ["AI12"] = CampaignDifficulty.Easy,
                ["AI13"] = CampaignDifficulty.Hard,
                ["AI7"] = CampaignDifficulty.Medium,
                ["AI8"] = CampaignDifficulty.Medium,
                ["AI9"] = CampaignDifficulty.Medium,
                ["AI11"] = CampaignDifficulty.Medium,
                ["AI1"] = CampaignDifficulty.Elite,
                ["AI2"] = CampaignDifficulty.Elite,
                ["AI3"] = CampaignDifficulty.Elite,
                ["AI10"] = CampaignDifficulty.Elite,
                ["Growth/Resilience"] = CampaignDifficulty.Easy,
                ["Grow>Kill>Reclaim(Econ)"] = CampaignDifficulty.Medium,
                ["Grow>Kill>Reclaim(Econ/Reclaim)"] = CampaignDifficulty.Medium,
                ["Creeping>Necrosporulation"] = CampaignDifficulty.Medium,
                ["TST_CampaignPlayer_SafeBaseline"] = CampaignDifficulty.Medium,
                ["TST_Training_ResilientMycelium"] = CampaignDifficulty.Training,
                ["TST_Training_Overextender"] = CampaignDifficulty.Easy,
                ["TST_Training_ToxicTurtle"] = CampaignDifficulty.Easy,
                ["TST_AnabolicBeaconNecroRegressionCascade"] = CampaignDifficulty.Medium,
                ["TST_AnabolicCreepingNecroRegressionCascade"] = CampaignDifficulty.Medium,
                ["CMP_TierCap_GrowthResilience_Easy"] = CampaignDifficulty.Easy,
                ["CMP_Reclaim_Scavenger_Easy"] = CampaignDifficulty.Easy,
                ["CMP_Surge_Pulsar_Easy"] = CampaignDifficulty.Medium,
                ["CMP_Reclaim_InfiltrationSurge_Easy"] = CampaignDifficulty.Easy,
                ["CMP_Defense_ResilientShell_Easy"] = CampaignDifficulty.Easy,
                ["CMP_Defense_ReclaimShell_Easy"] = CampaignDifficulty.Easy,
                ["CMP_Surge_BeaconTempo_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Control_AnabolicRebirth_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Surge_GrowthTempo_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Growth_Pressure_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Bloom_FortifyMimic_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Economy_KillReclaim_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Economy_TempoReclaim_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Bloom_CreepingNecro_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Bloom_BeaconRegression_Medium"] = CampaignDifficulty.Medium,
                ["CMP_Bloom_AnabolicRegression_Medium"] = CampaignDifficulty.Medium,
                ["TST_BalancedControl_AnabolicFirst"] = CampaignDifficulty.Hard,
                ["CMP_Control_AnabolicFirst_Hard"] = CampaignDifficulty.Hard,
                ["Power Mutations Max Econ"] = CampaignDifficulty.Hard,
                ["CMP_Economy_LateSpike_Hard"] = CampaignDifficulty.Hard,
                ["CMP_Bloom_CreepingRegression_Elite"] = CampaignDifficulty.Elite,
                ["TST_CreepingNecroRegressionCascade"] = CampaignDifficulty.Elite,
            };

        private static readonly Dictionary<string, CounterTag[]> ExplicitFavoredAgainstByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_HyperEconomyRamp"] = new[] { new CounterTag(StrategyArchetype.Control, reason: "Punishes slower setups if left alone.") },
                ["TST_LowTierSurgeSkirmisher"] = new[] { new CounterTag(StrategyArchetype.EconomyRamp, reason: "Cheap pressure can disrupt greedy openings.") },
                ["TST_OpportunisticCounterplay"] = new[] { new CounterTag(StrategyArchetype.LateGameSpike, reason: "Flexible pivoting can punish telegraphed spikes.") },
            };

        private static readonly Dictionary<string, CounterTag[]> ExplicitWeakAgainstByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["TST_HyperEconomyRamp"] = new[] { new CounterTag(StrategyArchetype.Offense, reason: "Greedy ramps are vulnerable to early pressure.") },
                ["TST_LateGameSpike"] = new[] { new CounterTag(StrategyArchetype.SurgeTempo, reason: "Burst-tempo lines can end games before the spike lands.") },
                ["TST_BalancedControl_AnabolicFirst"] = new[] { new CounterTag(StrategyArchetype.Counterplay, reason: "Well-timed disruption can blunt the setup line.") },
                ["TST_BalancedControl_MaxEconomy"] = new[] { new CounterTag(StrategyArchetype.Counterplay, reason: "Greedier control openings give counters more room.") },
            };

        public static readonly List<IMutationSpendingStrategy> ProvenStrategies;
        public static readonly List<IMutationSpendingStrategy> TestingStrategies;
        public static readonly List<IMutationSpendingStrategy> CampaignStrategies;

        public static readonly Dictionary<string, IMutationSpendingStrategy> ProvenStrategiesByName;
        public static readonly Dictionary<string, IMutationSpendingStrategy> TestingStrategiesByName;
        public static readonly Dictionary<string, IMutationSpendingStrategy> CampaignStrategiesByName;

        static AIRoster()
        {
            StrategyRegistry.Reset();
            StrategyRegistry.Register(StrategySetEnum.Proven, RawProvenStrategies, strategy => BuildCatalogEntry(strategy, StrategySetEnum.Proven));
            StrategyRegistry.Register(StrategySetEnum.Testing, RawTestingStrategies, strategy => BuildCatalogEntry(strategy, StrategySetEnum.Testing));
            StrategyRegistry.Register(StrategySetEnum.Campaign, RawCampaignStrategies, strategy => BuildCatalogEntry(strategy, StrategySetEnum.Campaign));
            StrategyRegistry.Register(StrategySetEnum.Mycovariants, MycovariantPermutations(), strategy => BuildCatalogEntry(strategy, StrategySetEnum.Mycovariants));

            ProvenStrategies = StrategyRegistry.GetStrategies(StrategySetEnum.Proven);
            TestingStrategies = StrategyRegistry.GetStrategies(StrategySetEnum.Testing);
            CampaignStrategies = StrategyRegistry.GetStrategies(StrategySetEnum.Campaign);

            ProvenStrategiesByName = StrategyRegistry.GetStrategyDictionary(StrategySetEnum.Proven);
            TestingStrategiesByName = StrategyRegistry.GetStrategyDictionary(StrategySetEnum.Testing);
            CampaignStrategiesByName = StrategyRegistry.GetStrategyDictionary(StrategySetEnum.Campaign);

            AuditSurgeBackboneSynergy(TestingStrategies, nameof(TestingStrategies));
        }

        private static void AuditSurgeBackboneSynergy(IEnumerable<IMutationSpendingStrategy> strategies, string strategySetName)
        {
            foreach (var strategy in strategies.OfType<ParameterizedSpendingStrategy>())
            {
                if (strategy.SurgePriorityIds.Count == 0)
                {
                    continue;
                }

                var backboneCategories = GetBackboneCategories(strategy).ToHashSet();
                if (backboneCategories.Count == 0)
                {
                    CoreLogger.Log?.Invoke($"[AIRosterAudit] {strategySetName}/{strategy.StrategyName}: surge-prioritizing strategy has no non-surge backbone category in target goals.");
                    continue;
                }

                foreach (var surgeMutationId in strategy.SurgePriorityIds)
                {
                    var suggested = MutationSynergyCatalog.GetSuggestedBackboneCategories(surgeMutationId);
                    if (suggested.Count == 0)
                    {
                        continue;
                    }

                    bool hasOverlap = backboneCategories.Any(suggested.Contains);
                    if (hasOverlap)
                    {
                        continue;
                    }

                    var surgeName = MutationRepository.All.TryGetValue(surgeMutationId, out var mutation)
                        ? mutation.Name
                        : $"UnknownSurge({surgeMutationId})";
                    var actual = string.Join(", ", backboneCategories.OrderBy(c => c));
                    var expected = MutationSynergyCatalog.DescribeBackboneCategories(surgeMutationId);
                    CoreLogger.Log?.Invoke($"[AIRosterAudit] {strategySetName}/{strategy.StrategyName}: surge '{surgeName}' expects backbone [{expected}] but strategy backbone is [{actual}].");
                }
            }
        }

        private static IEnumerable<MutationCategory> GetBackboneCategories(ParameterizedSpendingStrategy strategy)
        {
            foreach (var goal in strategy.TargetMutationGoals)
            {
                if (!MutationRepository.All.TryGetValue(goal.MutationId, out var mutation))
                {
                    continue;
                }

                // Only non-surge mutation goals contribute to persistent backbone.
                if (mutation.IsSurge || mutation.Category == MutationCategory.MycelialSurges)
                {
                    continue;
                }

                if (mutation.Category is MutationCategory.Growth or MutationCategory.CellularResilience or MutationCategory.Fungicide or MutationCategory.GeneticDrift)
                {
                    yield return mutation.Category;
                }
            }
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
                .Select(s => BuildStrategyProfile(s, strategySet))
                .ToList();
        }

        public static StrategyProfile? GetStrategyProfile(StrategySetEnum strategySet, string strategyName)
        {
            return GetStrategyProfiles(strategySet)
                .FirstOrDefault(p => string.Equals(p.StrategyName, strategyName, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<StrategyCatalogEntry> GetStrategyCatalogEntries(StrategySetEnum strategySet)
        {
            return StrategyRegistry.GetCatalogEntries(strategySet);
        }

        public static StrategyCatalogEntry? GetStrategyCatalogEntry(StrategySetEnum strategySet, string strategyName)
        {
            return GetStrategyCatalogEntries(strategySet)
                .FirstOrDefault(p => string.Equals(p.StrategyName, strategyName, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<StrategyCatalogEntry> GetStrategyCatalogEntries(
            StrategySetEnum strategySet,
            StrategyCatalogFilter? filter)
        {
            var entries = GetStrategyCatalogEntries(strategySet);
            if (filter == null || filter.IsEmpty)
            {
                return entries;
            }

            return entries.Where(filter.Matches).ToList();
        }

        public static List<IMutationSpendingStrategy> GetStrategiesByFilter(
            StrategySetEnum strategySet,
            StrategyCatalogFilter? filter)
        {
            var strategyDictionary = GetStrategyDictionary(strategySet);
            return GetStrategyCatalogEntries(strategySet, filter)
                .Where(entry => strategyDictionary.ContainsKey(entry.StrategyName))
                .Select(entry => strategyDictionary[entry.StrategyName])
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

        private static StrategyProfile BuildStrategyProfile(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            var theme = GetThemeForStrategy(strategy);
            var status = GetStatusForStrategy(strategy, strategySet);
            var powerTier = GetPowerTierForStrategy(strategy, strategySet);
            var role = GetRoleForStrategy(strategy, strategySet);
            var lifecycle = GetLifecycleForStrategy(strategy, strategySet);
            var difficultyBands = GetDifficultyBandsForStrategy(strategy, strategySet);
            var campaignDifficulty = GetCampaignDifficultyForStrategy(strategy, strategySet);
            var pools = GetPoolsForStrategy(strategySet);
            var favoredAgainst = GetFavoredAgainstForStrategy(strategy);
            var weakAgainst = GetWeakAgainstForStrategy(strategy);
            var notes = BuildNotes(strategy, strategySet, powerTier, role, lifecycle);

            return new StrategyProfile(
                strategy.StrategyName,
                strategySet,
                theme,
                status,
                BuildIntentLabel(strategy),
                powerTier,
                role,
                lifecycle,
                difficultyBands,
                campaignDifficulty,
                pools,
                favoredAgainst,
                weakAgainst,
                notes);
        }

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<AdaptationSynergySet>> SuggestedAdaptationSetsByStrategyName =
            new Dictionary<string, IReadOnlyList<AdaptationSynergySet>>(StringComparer.OrdinalIgnoreCase)
            {
                // ── ELITE COMBOS (3 adaptations each) ──

                ["CMP_Bloom_CreepingRegression_Elite"] = new[]
                {
                    new AdaptationSynergySet(
                        "The Necrotoxin Gauntlet",
                        "Opens with toxins on all enemy spores, poisons kill adjacently on decay, and expired toxins chain-spread. Three stages of the same kill vector layered together.",
                        new[] { AdaptationIds.SporeSalvo, AdaptationIds.MycotoxicHalo, AdaptationIds.VesicleBurst })
                },
                ["AI10"] = new[]
                {
                    new AdaptationSynergySet(
                        "The Necrotoxin Gauntlet",
                        "Opens with toxins on all enemy spores, poisons kill adjacently on decay, and expired toxins chain-spread. Three stages of the same kill vector layered together.",
                        new[] { AdaptationIds.SporeSalvo, AdaptationIds.MycotoxicHalo, AdaptationIds.VesicleBurst })
                },

                ["CMP_Defense_ResilientShell_Easy"] = new[]
                {
                    new AdaptationSynergySet(
                        "Iron Shell",
                        "Every grown cell gets Resistance (first per round), edge cells always resist, and dying cells beside resistant ones leave clean tiles. The board gradually becomes an untouchable fortress.",
                        new[] { AdaptationIds.AegisHyphae, AdaptationIds.CrustalCallus, AdaptationIds.SaprophageRing })
                },
                ["AI3"] = new[]
                {
                    new AdaptationSynergySet(
                        "Iron Shell",
                        "Every grown cell gets Resistance (first per round), edge cells always resist, and dying cells beside resistant ones leave clean tiles. The board gradually becomes an untouchable fortress.",
                        new[] { AdaptationIds.AegisHyphae, AdaptationIds.CrustalCallus, AdaptationIds.SaprophageRing })
                },

                ["CMP_Economy_LateSpike_Hard"] = new[]
                {
                    new AdaptationSynergySet(
                        "The Economancer",
                        "Surges cost less, maxing mutations earns bonus MP, and Retrograde Bloom hands a free T5 upgrade while sacrificing T1 junk. Explosive late-game mutation acceleration.",
                        new[] { AdaptationIds.HyphalEconomy, AdaptationIds.ApicalYield, AdaptationIds.RetrogradeBloom })
                },
                ["AI1"] = new[]
                {
                    new AdaptationSynergySet(
                        "The Economancer",
                        "Surges cost less, maxing mutations earns bonus MP, and Retrograde Bloom hands a free T5 upgrade while sacrificing T1 junk. Explosive late-game mutation acceleration.",
                        new[] { AdaptationIds.HyphalEconomy, AdaptationIds.ApicalYield, AdaptationIds.RetrogradeBloom })
                },
                ["AI2"] = new[]
                {
                    new AdaptationSynergySet(
                        "The Economancer",
                        "Surges cost less, maxing mutations earns bonus MP, and Retrograde Bloom hands a free T5 upgrade while sacrificing T1 junk. Explosive late-game mutation acceleration.",
                        new[] { AdaptationIds.HyphalEconomy, AdaptationIds.ApicalYield, AdaptationIds.RetrogradeBloom })
                },

                // ── BOSS COMBOS (6 adaptations each) ──

                ["TST_CreepingNecroRegressionCascade"] = new[]
                {
                    new AdaptationSynergySet(
                        "Thanatophyte",
                        "Opens with toxins on all enemies, gets instant kills on new toxin drops, toxins kill adjacently on decay, expired toxins chain-spread, bridges into the enemy cluster early, and earns bonus MP to fund the cascade. Every vector of the toxin kill chain is supercharged.",
                        new[] { AdaptationIds.SporeSalvo, AdaptationIds.MycotoxicHalo, AdaptationIds.MycotoxicLash, AdaptationIds.VesicleBurst, AdaptationIds.HyphalBridge, AdaptationIds.ApicalYield })
                },
                ["TST_AnabolicCreepingNecroRegressionCascade"] = new[]
                {
                    new AdaptationSynergySet(
                        "Thanatophyte",
                        "Opens with toxins on all enemies, gets instant kills on new toxin drops, toxins kill adjacently on decay, expired toxins chain-spread, bridges into the enemy cluster early, and earns bonus MP to fund the cascade. Every vector of the toxin kill chain is supercharged.",
                        new[] { AdaptationIds.SporeSalvo, AdaptationIds.MycotoxicHalo, AdaptationIds.MycotoxicLash, AdaptationIds.VesicleBurst, AdaptationIds.HyphalBridge, AdaptationIds.ApicalYield })
                },

                ["TST_AnabolicBeaconNecroRegressionCascade"] = new[]
                {
                    new AdaptationSynergySet(
                        "Rhizolith",
                        "Grows resistant cells every round, edge cells always resist, dead cells next to resistant ones vanish leaving no corpse lane, border threats are cleared on contact, and two repositioning tools ensure territorial coverage. Nearly impossible to contain once established.",
                        new[] { AdaptationIds.AegisHyphae, AdaptationIds.CrustalCallus, AdaptationIds.SaprophageRing, AdaptationIds.MarginalClamp, AdaptationIds.DistalSpore, AdaptationIds.ConidialRelay })
                },

                ["CMP_Control_AnabolicFirst_Hard"] = new[]
                {
                    new AdaptationSynergySet(
                        "Voltaic Bloom",
                        "Starts with aggro toxin placement, drafts mycovariants first every round, surges cost less + maxing mutations earns MP + Retrograde Bloom fires a free T5. The economic engine compounds until the board drowns in high-tier mutations.",
                        new[] { AdaptationIds.HyphalEconomy, AdaptationIds.ApicalYield, AdaptationIds.RetrogradeBloom, AdaptationIds.AscusPrimacy, AdaptationIds.MycotoxicHalo, AdaptationIds.SporeSalvo })
                },
            };

        private static StrategyCatalogEntry BuildCatalogEntry(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            var profile = BuildStrategyProfile(strategy, strategySet);
            var suggestedAdaptationSets = SuggestedAdaptationSetsByStrategyName.TryGetValue(profile.StrategyName, out var sets)
                ? sets
                : null;
            return new StrategyCatalogEntry(
                profile.StrategyName,
                profile.StrategySet,
                (StrategyArchetype)profile.Theme,
                profile.Status,
                profile.PowerTier,
                profile.Role,
                profile.Lifecycle,
                profile.DifficultyBands,
                profile.CampaignDifficulty,
                profile.Pools,
                profile.Intent,
                profile.Notes,
                profile.FavoredAgainst,
                profile.WeakAgainst,
                suggestedAdaptationSets);
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

        public static StrategyStatus GetStatusForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (ExplicitStrategyStatusesByName.TryGetValue(strategy.StrategyName, out var explicitStatus))
            {
                return explicitStatus;
            }

            return strategySet switch
            {
                StrategySetEnum.Proven => StrategyStatus.Proven,
                StrategySetEnum.Campaign => StrategyStatus.Proven,
                StrategySetEnum.Testing => StrategyStatus.Testing,
                StrategySetEnum.Mycovariants => StrategyStatus.Testing,
                _ => StrategyStatus.Testing
            };
        }

        public static StrategyPowerTier GetPowerTierForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (ExplicitPowerTiersByName.TryGetValue(strategy.StrategyName, out var explicitTier))
            {
                return explicitTier;
            }

            return strategySet switch
            {
                StrategySetEnum.Testing => StrategyPowerTier.Standard,
                StrategySetEnum.Campaign => StrategyPowerTier.Standard,
                _ => StrategyPowerTier.Standard
            };
        }

        public static StrategyRole GetRoleForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (strategySet == StrategySetEnum.Proven
                && (string.Equals(strategy.StrategyName, "TST_CampaignMirror_AI13_BalancedControl_MaxEconomy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(strategy.StrategyName, "TST_CampaignMirror_AI13_AnabolicFirst", StringComparison.OrdinalIgnoreCase)))
            {
                return StrategyRole.Baseline;
            }

            if (ExplicitRolesByName.TryGetValue(strategy.StrategyName, out var explicitRole))
            {
                return explicitRole;
            }

            return strategySet switch
            {
                StrategySetEnum.Testing => StrategyRole.Experimental,
                StrategySetEnum.Mycovariants => StrategyRole.Experimental,
                _ => StrategyRole.Baseline
            };
        }

        public static StrategyLifecycle GetLifecycleForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (strategySet == StrategySetEnum.Proven
                && (string.Equals(strategy.StrategyName, "TST_CampaignMirror_AI13_BalancedControl_MaxEconomy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(strategy.StrategyName, "TST_CampaignMirror_AI13_AnabolicFirst", StringComparison.OrdinalIgnoreCase)))
            {
                return StrategyLifecycle.Active;
            }

            if (ExplicitLifecycleByName.TryGetValue(strategy.StrategyName, out var explicitLifecycle))
            {
                return explicitLifecycle;
            }

            return StrategyLifecycle.Active;
        }

        public static IReadOnlyCollection<DifficultyBand> GetDifficultyBandsForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (ExplicitDifficultyBandsByName.TryGetValue(strategy.StrategyName, out var explicitBands))
            {
                return explicitBands;
            }

            return strategySet switch
            {
                StrategySetEnum.Campaign => new[] { DifficultyBand.Normal },
                _ => Array.Empty<DifficultyBand>()
            };
        }

        public static CampaignDifficulty? GetCampaignDifficultyForStrategy(IMutationSpendingStrategy strategy, StrategySetEnum strategySet)
        {
            if (ExplicitCampaignDifficultyByName.TryGetValue(strategy.StrategyName, out var explicitDifficulty))
            {
                return explicitDifficulty;
            }

            return strategySet switch
            {
                StrategySetEnum.Campaign => CampaignDifficulty.Medium,
                _ => null
            };
        }

        public static StrategyPool GetPoolsForStrategy(StrategySetEnum strategySet)
        {
            return strategySet switch
            {
                StrategySetEnum.Proven => StrategyPool.SimulationBaseline,
                StrategySetEnum.Testing => StrategyPool.SimulationExperimental,
                StrategySetEnum.Campaign => StrategyPool.Campaign,
                StrategySetEnum.Mycovariants => StrategyPool.MycovariantLab | StrategyPool.SimulationExperimental,
                _ => StrategyPool.None
            };
        }

        public static IReadOnlyCollection<CounterTag> GetFavoredAgainstForStrategy(IMutationSpendingStrategy strategy)
        {
            return ExplicitFavoredAgainstByName.TryGetValue(strategy.StrategyName, out var explicitCounters)
                ? explicitCounters
                : Array.Empty<CounterTag>();
        }

        public static IReadOnlyCollection<CounterTag> GetWeakAgainstForStrategy(IMutationSpendingStrategy strategy)
        {
            return ExplicitWeakAgainstByName.TryGetValue(strategy.StrategyName, out var explicitCounters)
                ? explicitCounters
                : Array.Empty<CounterTag>();
        }

        private static string BuildNotes(
            IMutationSpendingStrategy strategy,
            StrategySetEnum strategySet,
            StrategyPowerTier powerTier,
            StrategyRole role,
            StrategyLifecycle lifecycle)
        {
            var notes = new List<string>();

            if (strategySet == StrategySetEnum.Campaign)
            {
                notes.Add("Campaign-eligible strategy.");
            }

            if (role == StrategyRole.Training)
            {
                notes.Add("Intentionally suitable for weaker opposition or onboarding difficulty.");
            }
            else if (role == StrategyRole.Boss)
            {
                notes.Add("Intended for high-pressure encounters.");
            }
            else if (role == StrategyRole.Spice)
            {
                notes.Add("Intentionally uneven or novelty-oriented for roster variety.");
            }
            else if (role == StrategyRole.Experimental)
            {
                notes.Add("Primary home is experimentation and simulation tuning.");
            }

            if (lifecycle == StrategyLifecycle.NeedsTuning)
            {
                notes.Add("Flagged for follow-up balance tuning.");
            }

            if (powerTier == StrategyPowerTier.Weak)
            {
                notes.Add("Expected to perform below baseline by design or current tuning.");
            }
            else if (powerTier == StrategyPowerTier.Spike)
            {
                notes.Add("High-variance ceiling; matchup and tempo matter heavily.");
            }

            return string.Join(" ", notes);
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
            var strategies = StrategyRegistry.GetStrategies(strategySet);
            if (strategies.Count == 0)
            {
                throw new ArgumentException($"Unknown strategy set: {strategySet}", nameof(strategySet));
            }

            return strategies;
        }

        private static Dictionary<string, IMutationSpendingStrategy> GetStrategyDictionary(StrategySetEnum strategySet)
        {
            var strategies = StrategyRegistry.GetStrategyDictionary(strategySet);
            if (strategies.Count == 0)
            {
                throw new ArgumentException($"Unknown strategy set: {strategySet}", nameof(strategySet));
            }

            return strategies;
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
