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
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.AeratedFrontierEffectPerLevel)} growth chance to every growth attempt from a living cell older than {GameBalance.AeratedFrontierMinimumExclusiveGrowthCycleAge} Growth Cycles with at least {GameBalance.AeratedFrontierRequiredOpenOrthogonalSpaces} open cardinal neighbors. An open neighbor has no cell, toxin, nutrient patch, permanent block, or active chemobeacon.",
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
                new MutationPrerequisite(MutationIds.CrustwardTropism, 1));
        }
    }
}
