using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating Fungicide category mutations.
    /// </summary>
    public static class FungicideMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            // Tier-1 Root
            helper.MakeRoot(new Mutation(
                id: MutationIds.MycotoxinTracer,
                name: "Mycotoxin Tracer",
                description: "Toxifies empty tiles along enemy borders to slow expansion.\n\n" +
                             "<b>Technical:</b> At the start of each Decay Phase, toxify random empty tiles next to living enemies. Higher levels increase expected toxin output through stronger base rolls, more spillover from failed Growth Phases, and an extra boost when your colony has few living cells. Output is capped by board size, and toxin tiles block normal growth until an effect overgrows them.",
                flavorText: "Microscopic chemical trails seep outward, clouding enemy borders in dormant inhibition fields.",
                type: MutationType.FungicideToxinSpores,
                effectPerLevel: GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: 50,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier1
            ));

            // Tier-2
            helper.MakeChild(new Mutation(
                id: MutationIds.MycotoxinPotentiation,
                name: "Mycotoxin Potentiation",
                description: $"Makes each toxin last longer and gives it a chance to kill nearby enemies.\n\n" +
                             $"<b>Technical:</b> Each level extends the lifespan of new toxin tiles by {helper.FormatFloat(GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel)} Growth Cycle{(GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel == 1 ? string.Empty : "s")} and adds a {helper.FormatPercent(GameBalance.MycotoxinPotentiationKillChancePerLevel)} chance for each toxin tile to kill an enemy fungal cell orthogonally adjacent (up / down / left / right) during the Decay Phase.",
                flavorText: "Toxins thicken with stabilizing glycoproteins, lingering longer and lashing out at encroaching invaders.",
                type: MutationType.ToxinKillAura,
                effectPerLevel: GameBalance.MycotoxinPotentiationKillChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.MycotoxinPotentiationMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier2
            ),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 5));

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.PutrefactiveMycotoxin,
                name: "Putrefactive Mycotoxin",
                description: $"Lets living cells kill adjacent enemies just by touching them.\n\n" +
                             $"<b>Technical:</b> Each level adds a {helper.FormatPercent(GameBalance.PutrefactiveMycotoxinEffectPerLevel)} death chance to enemy cells adjacent to your living fungal cells orthogonally (up / down / left / right).\n" +
                             $"<b>Max Level Bonus:</b> Your active Chemotactic Beacon also applies this kill chance within 2 tiles of its marker, including diagonals.",
                flavorText: "Secretes lipid-bound mycotoxins through adjacent cell walls, disrupting membrane integrity.",
                type: MutationType.AdjacentFungicide,
                effectPerLevel: GameBalance.PutrefactiveMycotoxinEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.PutrefactiveMycotoxinMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.MycotoxinPotentiation, 1));

            // Tier-4
            helper.MakeChild(new Mutation(
                id: MutationIds.SporicidalBloom,
                name: "Sporicidal Bloom",
                description:
                    "Turns a large colony into a wave of toxic spore drops.\n\n" +
                    "<b>Technical:</b> During the Decay Phase, drop toxic spores scaling with colony size and level at approximately " + helper.FormatPercent(GameBalance.SporicialBloomEffectPerLevel) + " of living cells per level. Spores target tiles outside your territory: hits on enemy living cells poison them, while empty tiles and dead cells are toxified.\n" +
                    "<b>Max Level Bonus:</b> Removes 25% of empty tiles from the target pool, greatly increasing enemy hit chance.",
                flavorText:
                    "Once mature, the colony vents cytotoxic spores that settle everywhere except its own territory. At peak expression, the spores stop wasting themselves on bare crumb.",
                type: MutationType.FungicideSporeDrop,
                effectPerLevel: GameBalance.SporicialBloomEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.SporicidalBloomMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier4
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveMycotoxin, 1),
            new MutationPrerequisite(MutationIds.MycelialBloom, 7)
            );

            // Tier-5
            helper.MakeChild(new Mutation(
                id: MutationIds.NecrotoxicConversion,
                name: "Necrotoxic Conversion",
                description: $"Your toxin kills can turn straight into reclaimed living cells.\n\n" +
                             $"<b>Technical:</b> Each level grants a {helper.FormatPercent(GameBalance.NecrotoxicConversionReclaimChancePerLevel)} chance to instantly reclaim any cell killed by your toxin effects (Putrefactive Mycotoxin, Mycotoxin Potentiation, Sporicidal Bloom, or Putrefactive Cascade) as a living cell under your control.",
                flavorText: "Necrotoxin does not wait for the corpse to cool; the colony grows into the tissue it just killed.",
                type: MutationType.NecrotoxicConversion,
                effectPerLevel: GameBalance.NecrotoxicConversionReclaimChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.NecrotoxicConversionMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveMycotoxin, 5),
            new MutationPrerequisite(MutationIds.RegenerativeHyphae, 1));

            helper.MakeChild(new Mutation(
                id: MutationIds.PutrefactiveRejuvenation,
                name: "Putrefactive Rejuvenation",
                description: $"Putrefactive kills can rejuvenate your nearby living cells.\n\n" +
                             $"<b>Technical:</b> When Putrefactive Mycotoxin kills an adjacent enemy cell, your living cells within {GameBalance.PutrefactiveRejuvenationEffectRadius} tiles (including diagonals) lose {GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel} Growth Cycles of age per level. Each level also boosts Putrefactive Mycotoxin effectiveness by {helper.FormatPercent(GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel)}.\n" +
                             $"<b>Max Level Bonus:</b> Rejuvenation radius is doubled.",
                flavorText: "The colony's most advanced toxins not only destroy rivals, but catalyze a surge of rejuvenation, siphoning the essence of the fallen to extend its own life.",
                type: MutationType.PutrefactiveRejuvenation,
                effectPerLevel: GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.PutrefactiveRejuvenationMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveMycotoxin, 2),
            new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 1));

            // Tier-6
            helper.MakeChild(new Mutation(
                id: MutationIds.PutrefactiveCascade,
                name: "Putrefactive Cascade",
                description: $"A putrefactive kill can keep traveling in the same direction through more enemies.\n\n" +
                             $"<b>Technical:</b> Each level boosts Putrefactive Mycotoxin by {helper.FormatPercent(GameBalance.PutrefactiveCascadeEffectivenessBonus)} and grants a {helper.FormatPercent(GameBalance.PutrefactiveCascadeCascadeChance)} chance for each putrefactive kill to chain to the next living enemy cell in the same direction. Chains continue until they miss, hit an empty tile, reach a dead or toxin tile, hit one of your own cells, or run off the board.\n" +
                             $"<b>Max Level Bonus:</b> Cascaded kills poison their targets instead of leaving dead cells.",
                flavorText: "Putrefaction learns direction: each ruptured membrane primes the next one along the line.",
                type: MutationType.PutrefactiveCascade,
                effectPerLevel: GameBalance.PutrefactiveCascadeCascadeChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier6),
                maxLevel: GameBalance.PutrefactiveCascadeMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier6
            ),
            new MutationPrerequisite(MutationIds.NecrotoxicConversion, 1),
            new MutationPrerequisite(MutationIds.ChemotacticBeacon, 1));
        }
    }
}
