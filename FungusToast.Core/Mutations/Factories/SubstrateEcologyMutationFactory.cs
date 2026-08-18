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
                description: "Helps your colony spread from cells with room to branch.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.AeratedFrontierEffectPerLevel)} growth chance to every growth attempt from a living cell with at least {GameBalance.AeratedFrontierRequiredOpenOrthogonalSpaces} open cardinal neighbors. An open neighbor has no cell, toxin, nutrient patch, permanent block, or active chemobeacon.",
                flavorText: "Loose pores around the hyphal tip keep oxygen and moisture flowing through the advancing substrate.",
                type: MutationType.AeratedFrontierGrowthChance,
                effectPerLevel: GameBalance.AeratedFrontierEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.AeratedFrontierMaxLevel,
                category: MutationCategory.SubstrateEcology,
                tier: MutationTier.Tier1
            ));
        }
    }
}
