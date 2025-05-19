using FungusToast.Core.Config;
using FungusToast.Core.Core.Mutations;
using System.Collections.Generic;
using System.Globalization;

namespace FungusToast.Core.Mutations
{
    public static class MutationRepository
    {
        public static (Dictionary<int, Mutation> all, Dictionary<int, Mutation> roots) BuildFullMutationSet()
        {
            var allMutations = new Dictionary<int, Mutation>();
            var rootMutations = new Dictionary<int, Mutation>();

            string FormatFloat(float value) => value % 1 == 0 ? ((int)value).ToString() : value.ToString("0.000", CultureInfo.InvariantCulture);
            string FormatPercent(float value) => value.ToString("P2", CultureInfo.InvariantCulture);

            Mutation MakeRoot(Mutation m)
            {
                allMutations[m.Id] = m;
                rootMutations[m.Id] = m;
                return m;
            }

            Mutation MakeChild(Mutation m, params MutationPrerequisite[] prereqs)
            {
                m.Prerequisites.AddRange(prereqs);
                allMutations[m.Id] = m;

                foreach (var prereq in prereqs)
                    if (allMutations.TryGetValue(prereq.MutationId, out var parent))
                        parent.Children.Add(m);

                return m;
            }

            void AddTendril(int id, string dir)
            {
                MakeChild(new Mutation(
                    id: id,
                    name: $"Tendril {dir}",
                    description: $"Each level grants a {FormatPercent(GameBalance.DiagonalGrowthEffectPerLevel)} chance to grow in the {dir.ToLower()} direction.",
                    flavorText: $"Polarity vectors align hyphal tip extension toward {dir.ToLower()} moisture gradients.",
                    type: dir switch
                    {
                        "Northwest" => MutationType.GrowthDiagonal_NW,
                        "Northeast" => MutationType.GrowthDiagonal_NE,
                        "Southeast" => MutationType.GrowthDiagonal_SE,
                        "Southwest" => MutationType.GrowthDiagonal_SW,
                        _ => throw new System.Exception("Invalid direction")
                    },
                    effectPerLevel: GameBalance.DiagonalGrowthEffectPerLevel,
                    pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                    maxLevel: GameBalance.DiagonalGrowthMaxLevel,
                    category: MutationCategory.Growth,
                    tier: MutationTier.Tier2
                ),
                new MutationPrerequisite(MutationIds.MycelialBloom, 10));
            }

            // Tier-1 Roots
            MakeRoot(new Mutation(
                id: MutationIds.MycelialBloom,
                name: "Mycelial Bloom",
                description: $"Each level grants a {FormatPercent(GameBalance.MycelialBloomEffectPerLevel)} increased chance to grow in the four cardinal directions.",
                flavorText: "Hyphal strands thicken and surge outward, driven by nutrient cues and quorum signals.",
                type: MutationType.GrowthChance,
                effectPerLevel: GameBalance.MycelialBloomEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.MycelialBloomMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier1
            ));

            MakeRoot(new Mutation(
                id: MutationIds.HomeostaticHarmony,
                name: "Homeostatic Harmony",
                description: $"Each level reduces self-death probability during decay by {FormatPercent(GameBalance.HomeostaticHarmonyEffectPerLevel)}.",
                flavorText: "Oscillatory homeostasis stabilizes intracellular pressure and toxin accumulation.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: GameBalance.HomeostaticHarmonyEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.HomeostaticHarmonyMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier1
            ));

            MakeRoot(new Mutation(
                id: MutationIds.SilentBlight,
                name: "Silent Blight",
                description: $"Each level increases the base decay chance of enemy cells by {FormatPercent(GameBalance.SilentBlightEffectPerLevel)}.",
                flavorText: "A dormant enzymatic payload triggers necrotic collapse in adjacent competitors.",
                type: MutationType.EnemyDecayChance,
                effectPerLevel: GameBalance.SilentBlightEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.SilentBlightMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier1
            ));

            MakeRoot(new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Each level grants a {FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel)} chance to gain an additional mutation point each round.",
                flavorText: "Epigenetic drift activates opportunistic transcription bursts in volatile environments.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier1
            ));

            // Tier-2
            MakeChild(new Mutation(
                id: MutationIds.ChronoresilientCytoplasm,
                name: "Chronoresilient Cytoplasm",
                description: $"Each level increases the age threshold before death risk begins by {FormatFloat(GameBalance.ChronoresilientCytoplasmEffectPerLevel)} growth cycles.",
                flavorText: "Temporal buffering vesicles shield core organelles from oxidative stress.",
                type: MutationType.SelfAgeResetThreshold,
                effectPerLevel: GameBalance.ChronoresilientCytoplasmEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.ChronoresilientCytoplasmMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10));

            MakeChild(new Mutation(
                id: MutationIds.EncystedSpores,
                name: "Encysted Spores",
                description: $"Each level grants a {FormatPercent(GameBalance.EncystedSporesEffectPerLevel)} death pressure bonus against enemies surrounded on three or more sides.",
                flavorText: "Encapsulation triggers lytic enzyme excretion in high-density microclimates.",
                type: MutationType.EncystedSporeMultiplier,
                effectPerLevel: GameBalance.EncystedSporesEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.EncystedSporesMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.SilentBlight, 10));

            AddTendril(MutationIds.TendrilNorthwest, "Northwest");
            AddTendril(MutationIds.TendrilNortheast, "Northeast");
            AddTendril(MutationIds.TendrilSoutheast, "Southeast");
            AddTendril(MutationIds.TendrilSouthwest, "Southwest");

            MakeChild(new Mutation(
                id: MutationIds.MutatorPhenotype,
                name: "Mutator Phenotype",
                description: $"Each level grants a {FormatPercent(GameBalance.MutatorPhenotypeEffectPerLevel)} chance to automatically upgrade a random mutation each round.",
                flavorText: "Transposons disrupt regulatory silencing, igniting stochastic trait amplification.",
                type: MutationType.AutoUpgradeRandom,
                effectPerLevel: GameBalance.MutatorPhenotypeEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.MutatorPhenotypeMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.AdaptiveExpression, 5));

            // Tier-3
            MakeChild(new Mutation(
                id: MutationIds.Necrosporulation,
                name: "Necrosporulation",
                description: $"Each level grants a {FormatPercent(GameBalance.NecrosporulationEffectPerLevel)} chance to spawn a new cell when a fungal cell dies.",
                flavorText: "Cytoplasmic apoptosis releases sporogenic factors for opportunistic rebirth.",
                type: MutationType.SporeOnDeathChance,
                effectPerLevel: GameBalance.NecrosporulationEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.NecrosporulationMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 5));

            MakeChild(new Mutation(
                id: MutationIds.PutrefactiveMycotoxin,
                name: "Putrefactive Mycotoxin",
                description: $"Each level adds a {FormatPercent(GameBalance.PutrefactiveMycotoxinEffectPerLevel)} death chance to enemy cells adjacent to tiles you control.",
                flavorText: "Secretes lipid-bound mycotoxins through adjacent cell walls, disrupting membrane integrity.",
                type: MutationType.OpponentExtraDeathChance,
                effectPerLevel: GameBalance.PutrefactiveMycotoxinEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.PutrefactiveMycotoxinMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.EncystedSpores, 5));

            MakeChild(new Mutation(
                id: MutationIds.AnabolicInversion,
                name: "Anabolic Inversion",
                description: $"Each level adds a {FormatPercent(GameBalance.AnabolicInversionGapBonusPerLevel)} chance to earn {GameBalance.AnabolicInversionPointsPerUpgrade} bonus points when you trail in living cells.",
                flavorText: "Under metabolic duress, anabolism inverts into compensatory feedback loops, prioritizing genomic plasticity.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AnabolicInversionGapBonusPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.AnabolicInversionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 3));

            MakeChild(new Mutation(
                id: MutationIds.MycotropicInduction,
                name: "Mycotropic Induction",
                description: $"Each level increases all diagonal growth probabilities by {FormatPercent(GameBalance.MycotropicInductionEffectPerLevel)}.",
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
            MakeChild(new Mutation(
                id: MutationIds.RegenerativeHyphae,
                name: "Regenerative Hyphae",
                description: $"Each living cell has a {FormatPercent(GameBalance.RegenerativeHyphaeReclaimChance)} chance to reclaim an orthogonally adjacent dead cell it previously owned during the growth phase.",
                flavorText: "Regrowth cascades from necrotic margins, guided by residual cytoplasmic signaling.",
                type: MutationType.ReclaimOwnDeadCells,
                effectPerLevel: GameBalance.RegenerativeHyphaeReclaimChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.RegenerativeHyphaeMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 1));

            return (allMutations, rootMutations);
        }
    }
}
