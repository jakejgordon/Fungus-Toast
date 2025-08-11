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
                description: "At the start of each decay phase, you release toxin spores based on this mutation's level and your number of failed growth attempts. The number of toxins scales with your mutation level (with diminishing returns) and failed growths, but is capped based on board size. Toxins are placed on unoccupied tiles orthogonally adjacent to enemy cells, creating temporary toxin zones that block enemy growth and reclamation.",
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
                id: MutationIds.SporocidalBloom,
                name: "Sporocidal Bloom",
                description:
                    "At the end of each round, your colony vents toxic spores that disperse across the board, avoiding your own territory. " +
                    "Each level of this mutation releases spores at approximately " + helper.FormatPercent(0.07f) + " per living fungal cell, scaling with your colony's size and mutation level.\n" +
                    "\n" +
                    "Each spore targets tiles that do not contain your living or dead cells:\n" +
                    "• If it lands on an enemy fungal cell, it kills that cell and leaves a toxin in its place.\n" +
                    "• If it lands on an empty tile or existing toxin, it becomes a toxin (or refreshes the existing one).\n" +
                    "• Spores cannot target tiles containing your own living or dead cells.\n" +
                    "\n" +
                    "<b>Max Level Bonus:</b> Removes 25% of empty tiles from the target pool, greatly increasing the likelihood of hitting enemy cells.",
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
                description: $"Each level grants a {helper.FormatPercent(GameBalance.NecrotoxicConversionReclaimChancePerLevel)} chance to immediately reclaim any cell that dies to your toxin effects. " +
                             $"This applies to deaths from Putrefactive Mycotoxin, Mycotoxin Potentiation, Sporocidal Bloom, and Putrefactive Cascade effects. " +
                             $"When triggered, the dead cell is instantly converted to a living cell under your control, creating aggressive territorial expansion through chemical warfare.",
                flavorText: "Advanced necrotoxin synthesis converts cellular death into immediate colonization, hijacking enemy metabolism to fuel instantaneous territorial conversion through toxic alchemy.",
                type: MutationType.NecrotoxicConversion,
                effectPerLevel: GameBalance.NecrotoxicConversionReclaimChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.NecrotoxicConversionMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.SporocidalBloom, 1),
            new MutationPrerequisite(MutationIds.MutatorPhenotype, 5));

            helper.MakeChild(new Mutation(
                id: MutationIds.PutrefactiveRejuvenation,
                name: "Putrefactive Rejuvenation",
                description: $"Whenever your mold kills an orthogonally adjacent living enemy cell (e.g., via Putrefactive Mycotoxin), it saps the nutrients and removes <b>{GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel}</b> growth cycles per mutation level from the age of any friendly living cells within <b>{GameBalance.PutrefactiveRejuvenationEffectRadius}</b> tiles of the poisoned cell. At max level, the distance is doubled.\n" +
                $"Additionally, each level increases the effectiveness of Putrefactive Mycotoxin by <b>{helper.FormatPercent(GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel)}</b>.",
                flavorText: "The colony's most advanced toxins not only destroy rivals, but catalyze a surge of rejuvenation—siphoning the essence of the fallen to extend its own life.",
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
                description: $"Each level increases the effectiveness of Putrefactive Mycotoxin by {helper.FormatPercent(GameBalance.PutrefactiveCascadeEffectivenessBonus)} " +
                             $"and grants a {helper.FormatPercent(GameBalance.PutrefactiveCascadeCascadeChance)} chance for each putrefactive kill to cascade " +
                             $"to the next enemy living cell in the same orthogonal direction. Cascades can chain indefinitely until they fail or run out of targets.\n\n" +
                             $"<b>Max Level Bonus:</b> Cascaded kills leave toxin tiles instead of dead cells, creating a trail of contamination.",
                flavorText: "Advanced mycotoxin synthesis enables directional propagation through cellular membranes, creating cascading waves of putrefaction that surge through enemy ranks like dominoes of death.",
                type: MutationType.PutrefactiveCascade,
                effectPerLevel: GameBalance.PutrefactiveCascadeCascadeChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier6),
                maxLevel: GameBalance.PutrefactiveCascadeMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier6
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveRejuvenation, 1),
            new MutationPrerequisite(MutationIds.HyphalVectoring, 1));
        }
    }
}