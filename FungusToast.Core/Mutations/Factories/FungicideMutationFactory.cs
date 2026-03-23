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
                description: "At the start of each decay phase, release toxin spores based on three factors: a random base amount that scales with level (with diminishing returns), a random bonus from failed growth attempts, and an early-game bonus when failed growths are high relative to your living cells. Total spores are capped by board size. Spores place toxins on empty tiles adjacent to enemy cells, blocking their growth and reclamation.",
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
                description: $"Each level extends the lifespan of new toxin tiles by {helper.FormatFloat(GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel)} growth cycle(s), " +
                             $"and grants a {helper.FormatPercent(GameBalance.MycotoxinPotentiationKillChancePerLevel)} chance per level to kill an orthogonally adjacent enemy fungal cell from each toxin tile during the Decay Phase.",
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
                description: $"Each level adds a {helper.FormatPercent(GameBalance.PutrefactiveMycotoxinEffectPerLevel)} death chance to enemy cells orthogonally adjacent to your living fungal cells.",
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
                    "At the end of each round, vent toxic spores scaling with colony size and level (approximately " + helper.FormatPercent(0.07f) + " of living cells per level). " +
                    "Spores target tiles outside your territory: hits on enemy cells kill and leave toxins; empty or existing toxin tiles become toxins.\n" +
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.NecrotoxicConversionReclaimChancePerLevel)} chance to instantly reclaim any cell killed by your toxin effects " +
                             $"(Putrefactive Mycotoxin, Mycotoxin Potentiation, Sporicidal Bloom, or Putrefactive Cascade), converting it to a living cell under your control.",
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
                description: $"When your mold kills an adjacent enemy cell, friendly living cells within <b>{GameBalance.PutrefactiveRejuvenationEffectRadius}</b> tiles lose <b>{GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel}</b> age per level. " +
                             $"Each level also boosts Putrefactive Mycotoxin effectiveness by <b>{helper.FormatPercent(GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel)}</b>.\n" +
                             $"<b>Max Level Bonus:</b> Rejuvenation radius is doubled.",
                flavorText: "The colony's most advanced toxins not only destroy rivals, but catalyze a surge of rejuvenation�siphoning the essence of the fallen to extend its own life.",
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
                description: $"Each level boosts Putrefactive Mycotoxin by {helper.FormatPercent(GameBalance.PutrefactiveCascadeEffectivenessBonus)} " +
                             $"and grants a {helper.FormatPercent(GameBalance.PutrefactiveCascadeCascadeChance)} chance for each putrefactive kill to chain to the next enemy cell in the same direction. " +
                             $"Chains continue until they miss or run out of targets.\n" +
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