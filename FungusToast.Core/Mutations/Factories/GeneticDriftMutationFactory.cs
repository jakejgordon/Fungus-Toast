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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.MutatorPhenotypeEffectPerLevel)} chance to automatically upgrade a random mutation at the start of each Mutation Phase.",
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel)} chance to gain an additional mutation point at the start of each Mutation Phase. If the first point is awarded, each level also grants a {helper.FormatPercent(GameBalance.AdaptiveExpressionSecondPointChancePerLevel)} chance to earn a second mutation point.",
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
                description: $"At the start of each growth phase, each level grants a {helper.FormatPercent(GameBalance.MycotoxinCatabolismCleanupChancePerLevel)} chance for each living fungal cell to metabolize each orthogonally adjacent toxin tile. " +
                             $"Each toxin tile metabolized this way grants a {helper.FormatPercent(GameBalance.MycotoxinCatabolismMutationPointChancePerLevel)} chance to gain a bonus mutation point, up to one point per tile (multiple points possible per turn, depending on the number of toxin tiles catabolized).",
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
                description: $"Each level adds a {helper.FormatPercent(GameBalance.AnabolicInversionGapBonusPerLevel)} chance to earn 1–5 bonus mutation points at the start of each Mutation Phase when you control fewer living cells than other players. The chance increases the further behind you are, and the bonus amount is weighted - the further behind you are, the higher chance of getting the maximum payout.",
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
                    $"Activates once {helper.FormatPercent(GameBalance.NecrophyticBloomActivationThreshold)} of the board is occupied. " +
                    $"At that moment, all of your previously dead, non-toxin fungal cells release " +
                    $"{helper.FormatFloat(GameBalance.NecrophyticBloomSporesPerDeathPerLevel)} spores per cell per level. " +
                    $"Released spores randomly drop on the board, reclaiming any dead cell they land upon. " +
                    $"After activation, each new death releases additional spores with diminishing effectiveness as crowding increases.",
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
                    $"Enhances your Mutator Phenotype's auto-upgrade ability. Each level provides a {helper.FormatPercent(GameBalance.HyperadaptiveDriftHigherTierChancePerLevel)} chance to target higher-tier mutations (Tier 2-4) instead of Tier 1, and a {helper.FormatPercent(GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel)} chance to upgrade Tier 1 mutations twice instead of once.\n" +
                    "\n" +
                    "At max level, automatically upgrades an additional Tier 1 mutation whenever this effect triggers.\n" +
                    "\n" +
                    "This works passively with your existing Mutator Phenotype auto-upgrade system.",
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.OntogenicRegressionChancePerLevel)} chance of devolving {GameBalance.OntogenicRegressionTier1LevelsToConsume} levels from a tier 1 mutation into a random tier 5 or 6 mutation at the start of the Mutation Phase, even if prerequisites are not met. At max level, the ability triggers twice.",
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