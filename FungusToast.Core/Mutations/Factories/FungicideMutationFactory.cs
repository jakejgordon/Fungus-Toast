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
                description: "Scatters toxin tiles along enemy borders to slow expansion.\n\n" +
                             "<b>Technical:</b> At the start of each Decay Phase, place toxin tiles on random empty tiles next to living enemies. Higher levels increase expected toxin output through stronger base rolls, more spillover from failed Growth Phases, and an extra boost when your colony has few living cells. Output is capped by board size, and toxin tiles block normal growth and cannot be reclaimed.",
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
                             $"<b>Technical:</b> Each level extends the lifespan of new toxin tiles by {helper.FormatFloat(GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel)} Growth Cycle(s) and adds a {helper.FormatPercent(GameBalance.MycotoxinPotentiationKillChancePerLevel)} chance for each toxin tile to kill an enemy fungal cell adjacent in a cardinal direction (up / down / left / right) during the Decay Phase.",
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
                description: $"Lets living cells poison adjacent enemies just by touching them.\n\n" +
                             $"<b>Technical:</b> Each level adds a {helper.FormatPercent(GameBalance.PutrefactiveMycotoxinEffectPerLevel)} death chance to enemy cells adjacent to your living fungal cells in a cardinal direction (up / down / left / right).\n" +
                             $"<b>Max Level Bonus:</b> Your active Chemotactic Beacon also applies this kill chance within <b>2 tiles</b>, including diagonals.",
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
                    "<b>Technical:</b> During the Decay Phase, drop toxic spores scaling with colony size and level at approximately " + helper.FormatPercent(GameBalance.SporicialBloomEffectPerLevel) + " of living cells per level. Spores target tiles outside your territory: hits on enemy living cells kill and leave toxins, while empty, dead, or existing toxin tiles become toxins.\n" +
                    "<b>Max Level Bonus:</b> Removes 25% of empty tiles from the target pool, greatly increasing enemy hit chance.",
                flavorText:
                    "Once mature, the colony begins venting spores laced with cytotoxic compounds, intelligently avoiding friendly territory while poisoning competitors and sterilizing contested ground. At peak evolution, the spores develop enhanced targeting, seeking out living enemies with lethal precision.",
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
                description: $"Your toxin kills can turn straight into captured living cells.\n\n" +
                             $"<b>Technical:</b> Each level grants a {helper.FormatPercent(GameBalance.NecrotoxicConversionReclaimChancePerLevel)} chance to instantly reclaim any cell killed by your toxin effects (Putrefactive Mycotoxin, Mycotoxin Potentiation, Sporicidal Bloom, or Putrefactive Cascade), converting it to a living cell under your control.",
                flavorText: "Advanced necrotoxin synthesis converts cellular death into immediate colonization, hijacking enemy metabolism to fuel instantaneous territorial conversion through toxic alchemy.",
                type: MutationType.NecrotoxicConversion,
                effectPerLevel: GameBalance.NecrotoxicConversionReclaimChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.NecrotoxicConversionMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.SporicidalBloom, 1),
            new MutationPrerequisite(MutationIds.MutatorPhenotype, 5));

            helper.MakeChild(new Mutation(
                id: MutationIds.PutrefactiveRejuvenation,
                name: "Putrefactive Rejuvenation",
                description: $"Putrefactive kills can rejuvenate nearby friendly living cells.\n\n" +
                             $"<b>Technical:</b> When Putrefactive Mycotoxin kills an adjacent enemy cell, friendly living cells within <b>{GameBalance.PutrefactiveRejuvenationEffectRadius}</b> tiles lose <b>{GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel}</b> Growth Cycles of age per level. Each level also boosts Putrefactive Mycotoxin effectiveness by <b>{helper.FormatPercent(GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel)}</b>.\n" +
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
                             $"<b>Max Level Bonus:</b> Cascaded kills leave toxin tiles instead of dead cells.",
                flavorText: "Advanced mycotoxin synthesis enables directional propagation through cellular membranes, creating cascading waves of putrefaction that surge through enemy ranks like dominoes of death.",
                type: MutationType.PutrefactiveCascade,
                effectPerLevel: GameBalance.PutrefactiveCascadeCascadeChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier6),
                maxLevel: GameBalance.PutrefactiveCascadeMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier6
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveRejuvenation, 1),
            new MutationPrerequisite(MutationIds.ChemotacticBeacon, 1));
        }
    }
}