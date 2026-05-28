using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating GeneticDrift category mutations.
    /// </summary>
    public static class GeneticDriftMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            // Tier-1 Root
            helper.MakeRoot(new Mutation(
                id: MutationIds.MutatorPhenotype,
                name: "Mutator Phenotype",
                description: $"Grants a chance of generating a free Tier 1 mutation upgrade at the start of the Mutation Phase.\n\n" +
                             $"<b>Technical:</b> Each level gives a {helper.FormatPercent(GameBalance.MutatorPhenotypeEffectPerLevel, 1)} chance to automatically upgrade one random Tier 1 mutation at Mutation Phase start.",
                flavorText: "Transposons disrupt regulatory silencing, igniting stochastic trait amplification.",
                type: MutationType.AutoUpgradeRandom,
                effectPerLevel: GameBalance.MutatorPhenotypeEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.MutatorPhenotypeMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier1
            ));

            // Tier-2
            helper.MakeChild(new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Can generate extra mutation points at the start of Mutation Phase.\n\n" +
                             $"<b>Technical:</b> Each level gives a {helper.FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel, 1)} chance to gain 1 bonus mutation point at Mutation Phase start. If that point is awarded, each level also gives a {helper.FormatPercent(GameBalance.AdaptiveExpressionSecondPointChancePerLevel, 1)} chance to gain a second bonus mutation point.",
                flavorText: "Epigenetic drift activates opportunistic transcription bursts in volatile environments.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 5));

            helper.MakeChild(new Mutation(
                id: MutationIds.MycotoxinCatabolism,
                name: "Mycotoxin Catabolism",
                description: $"Lets living cells break down nearby toxins for cleanup and occasional mutation points.\n\n" +
                             $"<b>Technical:</b> At growth start, each living cell rolls {helper.FormatPercent(GameBalance.MycotoxinCatabolismCleanupChancePerLevel, 1)} per level to consume each adjacent toxin in a cardinal direction (up / down / left / right). Each toxin consumed this way has a {helper.FormatPercent(GameBalance.MycotoxinCatabolismMutationPointChancePerLevel, 1)} chance to award 1 bonus mutation point, up to {GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound} per round.",
                flavorText: "Evolved metabolic pathways enable the breakdown of toxic compounds, reclaiming nutrients from chemical hazards and occasionally triggering adaptive bursts of mutation.",
                type: MutationType.ToxinCleanupAndMPBonus,
                effectPerLevel: GameBalance.MycotoxinCatabolismCleanupChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.MycotoxinCatabolismMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 2));

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.AnabolicInversion,
                name: "Anabolic Inversion",
                description: $"Falling behind can turn into a burst of mutation points.\n\n" +
                    $"<b>Technical:</b> At Mutation Phase start, when you trail in living cells, each level adds a {helper.FormatPercent(GameBalance.AnabolicInversionGapBonusPerLevel, 1)} chance to gain 1-5 bonus mutation points. Larger living-cell deficits increase both the odds and the payout.",
                flavorText: "Under metabolic duress, anabolism inverts into compensatory feedback loops, prioritizing genomic plasticity.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AnabolicInversionGapBonusPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.AnabolicInversionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.AdaptiveExpression, 3));

            // Tier-4
            helper.MakeChild(new Mutation(
                id: MutationIds.NecrophyticBloom,
                name: "Necrophytic Bloom",
                description:
                    $"Large clusters of your dead cells can compost into neutral nutrient patches.\n\n" +
                    $"<b>Technical:</b> At Decay Phase end, each dead non-toxin cluster of at least {GameBalance.NecrophyticBloomBaseClusterThreshold} cells has a {helper.FormatPercent(GameBalance.NecrophyticBloomBaseCompostChance, 1)} chance to convert into a neutral nutrient patch, up to {GameBalance.NecrophyticBloomMaxPatchSize} tiles and {GameBalance.NecrophyticBloomMaxPatchesPerRound} patches per round. Each level lowers the cluster requirement by {GameBalance.NecrophyticBloomClusterThresholdReductionPerLevel} and increases compost chance by {helper.FormatPercent(GameBalance.NecrophyticBloomCompostChanceIncreasePerLevel, 1)}.\n" +
                    $"<b>Max Level Bonus:</b> Can also create Hypervariation Development patches.",
                flavorText: "The colony learns to compost its dead into concentrated nourishment, turning loss into contested resources.",
                type: MutationType.NecrophyticBloomSporeDrop,
                effectPerLevel: GameBalance.NecrophyticBloomCompostChanceIncreasePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4) + 1,
                maxLevel: GameBalance.NecrophyticBloomMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier4
            ),
            new MutationPrerequisite(MutationIds.AnabolicInversion, 1),
            new MutationPrerequisite(MutationIds.Necrosporulation, 1));

            // Tier-5
            helper.MakeChild(new Mutation(
                id: MutationIds.HyperadaptiveDrift,
                name: "Hyperadaptive Drift",
                description:
                    $"Makes Mutator Phenotype reach further up your tree and sometimes chain extra Tier 1 upgrades.\n\n" +
                    $"<b>Technical:</b> When Mutator Phenotype triggers, each level gives a {helper.FormatPercent(GameBalance.HyperadaptiveDriftHigherTierChancePerLevel, 1)} chance to target a Tier 2-4 non-surge mutation instead of Tier 1. If it falls back to Tier 1, each level also gives a {helper.FormatPercent(GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel, 1)} chance to make up to {GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes} total upgrade attempts on that mutation.\n" +
                    $"<b>Max Level Bonus:</b> Also makes one extra Tier 1 upgrade attempt on a second mutation.",
                flavorText: "The colony achieves a state of hyperplasticity, capable of instantaneous genomic restructuring and parallel mutation cascades.",
                type: MutationType.FreeMutationUpgrade,
                effectPerLevel: 1.0f,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.HyperadaptiveDriftMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.NecrophyticBloom, 1),
            new MutationPrerequisite(MutationIds.MutatorPhenotype, GameBalance.MutatorPhenotypeMaxLevel - 2),
            new MutationPrerequisite(MutationIds.MycotoxinPotentiation, 1),
            new MutationPrerequisite(MutationIds.AdaptiveExpression, 1),
            new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 1));

            // Tier-6
            helper.MakeChild(new Mutation(
                id: MutationIds.OntogenicRegression,
                name: "Ontogenic Regression",
                description: $"Can trade away early mutations to force a late-game upgrade.\n\n" +
                             $"<b>Technical:</b> At Mutation Phase start, each level gives a {helper.FormatPercent(GameBalance.OntogenicRegressionChancePerLevel, 1)} chance to remove {GameBalance.OntogenicRegressionTier1LevelsToConsume} levels from a random Tier 1 mutation and add 1 level to a random Tier 5 or 6 mutation, ignoring prerequisites. If the roll fails or no valid source or target exists, gain {GameBalance.OntogenicRegressionFailureConsolationPoints} mutation points instead.\n" +
                             $"<b>Max Level Bonus:</b> Rolls twice. Before each successful mutation swap, it can also kill adjacent non-resistant enemy living cells touching your colony; every {GameBalance.OntogenicRegressionEnemyKillsPerLevelReduction} kills reduces the Tier 1 levels consumed by 1, down to a minimum cost of 1.",
                flavorText: "Ultimate genomic instability unlocks forbidden evolutionary pathways, sacrificing foundational adaptations to achieve impossible transcendence through ontogenic reversal.",
                type: MutationType.OntogenicRegression,
                effectPerLevel: GameBalance.OntogenicRegressionChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier6),
                maxLevel: GameBalance.OntogenicRegressionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier6
            ),
            new MutationPrerequisite(MutationIds.HyperadaptiveDrift, 2),
            new MutationPrerequisite(MutationIds.MycelialBloom, 10),
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 10),
            new MutationPrerequisite(MutationIds.MutatorPhenotype, 10));
        }
    }
}