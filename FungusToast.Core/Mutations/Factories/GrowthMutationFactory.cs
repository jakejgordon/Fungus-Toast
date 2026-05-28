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
                description: $"Expands your colony faster in the four cardinal directions (up / down / left / right), but makes it more vulnerable during decay.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.MycelialBloomEffectPerLevel)} cardinal growth chance (up / down / left / right) and {helper.FormatPercent(GameBalance.MycelialBloomRandomDecayPenaltyPerLevel)} random decay chance.",
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
                description: $"Turns all Tendrils into stronger diagonal branches.\n\n" +
                             $"<b>Technical:</b> Each level increases every Tendril diagonal growth chance by {helper.FormatPercent(GameBalance.MycotropicInductionEffectPerLevel)} of that Tendril's own chance.",
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
                description: $"Brings your own dead cells back at the edge of a living colony.\n\n" +
                             $"<b>Technical:</b> After growth and before decay, each living cell rolls {helper.FormatPercent(GameBalance.RegenerativeHyphaeReclaimChance)} per level to reclaim one dead cell you previously owned adjacent in a cardinal direction (up / down / left / right). Each dead cell is checked at most once per round.",
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
                description: $"Failed growth can become repositioning, letting a cell crawl into the tile it missed.\n\n" +
                             $"<b>Technical:</b> When a growth attempt fails, each level gives a {helper.FormatPercent(GameBalance.CreepingMoldMoveChancePerLevel)} chance to move into that target tile instead if it is at least as open as the source and has at least two open sides.\n" +
                             $"<b>Max Level Bonus:</b> Can jump over one blocking toxin in a cardinal direction (up / down / left / right) to land on the next open tile beyond it.",
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
            string directionWithHint = direction switch
            {
                "Northwest" => "northwest (up and to the left)",
                "Northeast" => "northeast (up and to the right)",
                "Southeast" => "southeast (down and to the right)",
                "Southwest" => "southwest (down and to the left)",
                _ => direction.ToLower()
            };

            helper.MakeChild(new Mutation(
                id: id,
                name: $"Tendril {direction}",
                description: $"Pushes growth toward the {directionWithHint}, trading away some normal spread.\n\n" +
                             $"<b>Technical:</b> Each level adds {helper.FormatPercent(GameBalance.TendrilDiagonalGrowthEffectPerLevel)} diagonal growth chance to the {directionWithHint} and subtracts {helper.FormatPercent(GameBalance.TendrilOrthogonalGrowthPenaltyPerLevel)} cardinal growth chance (up / down / left / right), to a {helper.FormatPercent(GameBalance.TendrilOrthogonalGrowthMinimumChance)} floor.",
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