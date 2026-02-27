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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.MutatorPhenotypeEffectPerLevel, 1)} chance to automatically upgrade a random mutation at the start of each Mutation Phase.",
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel, 1)} chance to gain an additional mutation point at the start of each Mutation Phase. If the first point is awarded, each level also grants a {helper.FormatPercent(GameBalance.AdaptiveExpressionSecondPointChancePerLevel, 1)} chance to earn a second mutation point.",
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
                description: $"At growth start, each level grants a {helper.FormatPercent(GameBalance.MycotoxinCatabolismCleanupChancePerLevel, 1)} chance for living cells to consume each adjacent toxin tile. " +
                             $"Each consumed toxin has a {helper.FormatPercent(GameBalance.MycotoxinCatabolismMutationPointChancePerLevel, 1)} chance to award a bonus mutation point (max {GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound} per round).",
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
                description: $"When you trail in living cells, each level adds a {helper.FormatPercent(GameBalance.AnabolicInversionGapBonusPerLevel, 1)} chance to earn 1-5 bonus mutation points at Mutation Phase start. Larger deficits raise both odds and payout size.\n" +
                    $"<b>Max Level Bonus:</b> Boosts Mycotoxin Catabolism cleanup chance by {helper.FormatPercent(GameBalance.AnabolicInversionCatabolismCleanupMultiplier)} against players with more living cells.",
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
                    $"Activates once {helper.FormatPercent(GameBalance.NecrophyticBloomActivationThreshold, 1)} of the board is occupied. " +
                    $"Your dead non-toxin cells release {helper.FormatFloat(GameBalance.NecrophyticBloomSporesPerDeathPerLevel)} spores per cell per level, reclaiming any dead cell they land on. " +
                    $"After activation, new deaths continue releasing spores with diminishing returns as crowding increases.",
                flavorText: "When population pressure nears collapse, the mycelium initiates necrophytic recovery — resurrecting fallen cells and seeding the surface in desperate bloom.",
                type: MutationType.NecrophyticBloomSporeDrop,
                effectPerLevel: GameBalance.NecrophyticBloomSporesPerDeathPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
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
                    $"Enhances Mutator Phenotype. Each level grants a {helper.FormatPercent(GameBalance.HyperadaptiveDriftHigherTierChancePerLevel, 1)} chance to auto-upgrade a Tier 2-4 mutation instead of Tier 1. " +
                    $"On Tier 1 fallback, each level has a {helper.FormatPercent(GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel, 1)} chance to upgrade it {GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes} extra times.\n" +
                    $"<b>Max Level Bonus:</b> Also upgrades an additional Tier 1 mutation {GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes} times.",
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.OntogenicRegressionChancePerLevel, 1)} chance at Mutation Phase start to consume {GameBalance.OntogenicRegressionTier1LevelsToConsume} levels from a Tier 1 mutation and convert them into a random Tier 5 or 6 upgrade, ignoring prerequisites.\n" +
                             $"<b>Max Level Bonus:</b> Triggers twice.",
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