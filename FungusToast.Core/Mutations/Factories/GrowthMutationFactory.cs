using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating Growth category mutations.
    /// </summary>
    public static class GrowthMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            // Tier-1 Root
            helper.MakeRoot(new Mutation(
                id: MutationIds.MycelialBloom,
                name: "Mycelial Bloom",
                description: $"Each level grants a {helper.FormatPercent(GameBalance.MycelialBloomEffectPerLevel)} increased chance to grow in the four cardinal directions.",
                flavorText: "Hyphal strands thicken and surge outward, driven by nutrient cues and quorum signals.",
                type: MutationType.GrowthChance,
                effectPerLevel: GameBalance.MycelialBloomEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.MycelialBloomMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier1
            ));

            // Tier-2 Tendrils
            CreateTendril(MutationIds.TendrilNorthwest, "Northwest", MutationType.GrowthDiagonal_NW, helper);
            CreateTendril(MutationIds.TendrilNortheast, "Northeast", MutationType.GrowthDiagonal_NE, helper);
            CreateTendril(MutationIds.TendrilSoutheast, "Southeast", MutationType.GrowthDiagonal_SE, helper);
            CreateTendril(MutationIds.TendrilSouthwest, "Southwest", MutationType.GrowthDiagonal_SW, helper);

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.MycotropicInduction,
                name: "Mycotropic Induction",
                description: $"Each level increases all diagonal growth probabilities by a multiplier of {helper.FormatPercent(GameBalance.MycotropicInductionEffectPerLevel)}.",
                flavorText: "Signal transduction pathways activate branching vesicle recruitment along orthogonal gradients.",
                type: MutationType.TendrilDirectionalMultiplier,
                effectPerLevel: GameBalance.MycotropicInductionEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.MycotropicInductionMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier3
            ),
                new MutationPrerequisite(MutationIds.TendrilNorthwest, 1),
                new MutationPrerequisite(MutationIds.TendrilNortheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSoutheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSouthwest, 1));

            // Tier-4
            helper.MakeChild(new Mutation(
                id: MutationIds.RegenerativeHyphae,
                name: "Regenerative Hyphae",
                description: $"After growth and before decay, each living cell has a {helper.FormatPercent(GameBalance.RegenerativeHyphaeReclaimChance)} chance per level to reclaim one orthogonally adjacent dead cell it previously owned. " +
                             $"Only one attempt can be made on each dead cell per round.",
                flavorText: "Regrowth cascades from necrotic margins, guided by residual cytoplasmic signaling.",
                type: MutationType.ReclaimOwnDeadCells,
                effectPerLevel: GameBalance.RegenerativeHyphaeReclaimChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.RegenerativeHyphaeMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.Necrosporulation, 2),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 1));

            helper.MakeChild(new Mutation(
                id: MutationIds.CreepingMold,
                name: "Creeping Mold",
                description: $"Each level grants a {helper.FormatPercent(GameBalance.CreepingMoldMoveChancePerLevel)} chance to move into a target tile after a failed growth attempt (replaces the original cell). At max level, Creeping Mold can jump over a single toxin tile: if a failed growth is blocked by a toxin in a cardinal direction, and the roll succeeds, the mold will leap over the toxin and attempt to land on the next tile in the same direction.",
                flavorText: "Hyphal strands abandon anchor points to invade fresh substrate through pseudopodial crawling.",
                type: MutationType.CreepingMovementOnFailedGrowth,
                effectPerLevel: GameBalance.CreepingMoldMoveChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.CreepingMoldMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 3));
        }

        private static void CreateTendril(int id, string direction, MutationType type, MutationBuilderHelper helper)
        {
            helper.MakeChild(new Mutation(
                id: id,
                name: $"Tendril {direction}",
                description: $"Each level grants a {helper.FormatPercent(GameBalance.TendrilDiagonalGrowthEffectPerLevel)} chance to grow in the {direction.ToLower()} direction.",
                flavorText: $"Polarity vectors align hyphal tip extension toward {direction.ToLower()} moisture gradients.",
                type: type,
                effectPerLevel: GameBalance.TendrilDiagonalGrowthEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.TendrilDiagonalGrowthMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier2
            ),
            new MutationPrerequisite(MutationIds.MycelialBloom, 10));
        }
    }
}