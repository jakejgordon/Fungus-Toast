using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating Substrate Ecology category mutations.
    /// </summary>
    public static class SubstrateEcologyMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            helper.MakeRoot(new Mutation(
                id: MutationIds.AeratedFrontier,
                name: "Aerated Frontier",
                description: "Helps your colony spread from established cells with room to branch.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.AeratedFrontierEffectPerLevel)} growth chance to every growth attempt from a living cell at least {GameBalance.AeratedFrontierMinimumEligibleGrowthCycleAge} Growth Cycles old with at least {GameBalance.AeratedFrontierRequiredOpenOrthogonalSpaces} open cardinal neighbors. An open neighbor has no cell, toxin, nutrient patch, permanent block, or active chemobeacon.",
                flavorText: "Loose pores around the hyphal tip keep oxygen and moisture flowing through the advancing substrate.",
                type: MutationType.AeratedFrontierGrowthChance,
                effectPerLevel: GameBalance.AeratedFrontierEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.AeratedFrontierMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier1
            ));

            helper.MakeChild(new Mutation(
                id: MutationIds.CrustwardTropism,
                name: "Crustward Tropism",
                description: "Helps your colony press outward toward the playable crust.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.CrustwardTropismEffectPerLevel)} growth chance to every legal cardinal or enabled Tendril diagonal attempt whose target is closer to the playable crust than its source.\n" +
                             "<b>Max Level Bonus:</b> Once per Growth Cycle, the first qualifying attempt that would place a cell on the playable crust succeeds automatically.",
                flavorText: "Hyphal tips align with the drying gradient at the loaf's perimeter, seeking the exposed crust.",
                type: MutationType.CrustwardTropismGrowthChance,
                effectPerLevel: GameBalance.CrustwardTropismEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.CrustwardTropismMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier2
            ),
                new MutationPrerequisite(MutationIds.AeratedFrontier, 10));

            helper.MakeChild(new Mutation(
                id: MutationIds.CompactionPressure,
                name: "Compaction Pressure",
                description: "Helps your colony push through cramped territory.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.CompactionPressureEffectPerLevel)} growth chance to every growth attempt from a living cell with {GameBalance.CompactionPressureMinimumLegalOrthogonalTargets}-{GameBalance.CompactionPressureMaximumLegalOrthogonalTargets} legal orthogonal growth targets. Fully sealed cells gain no attempt.",
                flavorText: "Compressed hyphae redirect their force into the few pores that remain.",
                type: MutationType.CompactionPressureGrowthChance,
                effectPerLevel: GameBalance.CompactionPressureEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.CompactionPressureMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier2
            ),
                new MutationPrerequisite(MutationIds.AeratedFrontier, 10));

            helper.MakeChildWithAnyPrerequisiteGroup(new Mutation(
                id: MutationIds.DetritalEnzymes,
                name: "Detrital Enzymes",
                description: "Helps your colony spread through nearby dead matter.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.DetritalEnzymesEffectPerLevel)} growth chance when the legal target has at least one orthogonally adjacent non-toxic dead cell. Dead cells from any colony qualify, and additional dead neighbors do not stack.\n" +
                             $"<b>Max Level Bonus:</b> Gain an additional {helper.FormatPercent(GameBalance.DetritalEnzymesDenseDeadMatterBonus)} growth chance when the target has at least {GameBalance.DetritalEnzymesDenseDeadMatterRequiredNeighbors} adjacent non-toxic dead cells.",
                flavorText: "Released enzymes soften the remnants of fallen colonies, opening a transient path through the detritus.",
                type: MutationType.DetritalEnzymesGrowthChance,
                effectPerLevel: GameBalance.DetritalEnzymesEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.DetritalEnzymesMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier3
            ),
                new MutationPrerequisite(MutationIds.CrustwardTropism, 1),
                new MutationPrerequisite(MutationIds.CompactionPressure, 1));

            helper.MakeChild(new Mutation(
                id: MutationIds.ToxinMargin,
                name: "Toxin Margin",
                description: "Helps your colony grow around enemy toxin fields.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.ToxinMarginEffectPerLevel)} growth chance to every legal cardinal or enabled Tendril diagonal attempt whose empty target is orthogonally adjacent to at least one enemy-owned toxin. Multiple toxins do not stack, and this bonus never grows into or removes toxin tiles.",
                flavorText: "Stabilized hyphal tips trace the chemical boundary, finding the few viable pores around an inhibitory field.",
                type: MutationType.ToxinMarginGrowthChance,
                effectPerLevel: GameBalance.ToxinMarginEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.ToxinMarginMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier3
            ),
                new MutationPrerequisite(MutationIds.AeratedFrontier, 5),
                new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5));

            helper.MakeChild(new Mutation(
                id: MutationIds.NecrophyticBloom,
                name: "Necrophytic Bloom",
                description:
                    $"Large clusters of your dead cells can compost into neutral nutrient patches.\n\n" +
                    $"<b>Technical:</b> At Decay Phase end, each dead non-toxin cluster of at least {GameBalance.NecrophyticBloomBaseClusterThreshold} cells is split into groups of {GameBalance.NecrophyticBloomBaseClusterThreshold} cells, and each group independently has a {helper.FormatPercent(GameBalance.NecrophyticBloomBaseCompostChance, 1)} chance to convert into a neutral nutrient patch, up to {GameBalance.NecrophyticBloomMaxPatchSize} tiles and {GameBalance.NecrophyticBloomMaxPatchesPerRound} patches per round. Each level lowers the group size by {GameBalance.NecrophyticBloomClusterThresholdReductionPerLevel} and increases compost chance by {helper.FormatPercent(GameBalance.NecrophyticBloomCompostChanceIncreasePerLevel, 1)}, so larger dead masses get more chances to hit the round's patch cap.\n" +
                    $"<b>Max Level Bonus:</b> Can also create Hypervariation Development patches.",
                flavorText: "The colony learns to compost its dead into concentrated nourishment, turning loss into contested resources.",
                type: MutationType.NecrophyticBloomSporeDrop,
                effectPerLevel: GameBalance.NecrophyticBloomCompostChanceIncreasePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4) + 1,
                maxLevel: GameBalance.NecrophyticBloomMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.HyphalSurge, 2),
                new MutationPrerequisite(MutationIds.DetritalEnzymes, 3),
                new MutationPrerequisite(MutationIds.AdaptiveExpression, 3));

            helper.MakeChild(new Mutation(
                id: MutationIds.MycotoxinFission,
                name: "Toxinborne Seeding",
                description: "Lets a mobile toxin carry a newly grown cell into enemy territory.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.ToxinborneSeedingEffectPerLevel)} growth chance when an empty growth target is orthogonally adjacent to one of your toxins. After a successful qualifying growth, that toxin relocates to an empty tile next to an enemy living cell, keeping only its remaining lifespan. The newly grown cell travels with it and lands in a random open orthogonal tile next to the toxin. If no such tile is open, the carried cell is lost. The carried cell cannot trigger another seeding.",
                flavorText: "A toxin-bearing vesicle carries a living hyphal fragment through the air, planting a fragile outpost where its chemical payload comes to rest.",
                type: MutationType.ToxinborneSeedingGrowthChance,
                effectPerLevel: GameBalance.ToxinborneSeedingEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.ToxinborneSeedingMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier5
            ),
                new MutationPrerequisite(MutationIds.NecrophyticBloom, 1),
                new MutationPrerequisite(MutationIds.SporicidalBloom, 1));
        }
    }
}
