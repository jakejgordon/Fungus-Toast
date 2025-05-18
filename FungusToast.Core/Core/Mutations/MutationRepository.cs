using FungusToast.Core.Config;
using FungusToast.Core.Core.Mutations;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public static class MutationRepository
    {
        public static (Dictionary<int, Mutation> all, Dictionary<int, Mutation> roots) BuildFullMutationSet()
        {
            var allMutations = new Dictionary<int, Mutation>();
            var rootMutations = new Dictionary<int, Mutation>();

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

            // Tier-1 Roots
            MakeRoot(new Mutation(MutationIds.MycelialBloom, "Mycelial Bloom",
                $"Each level grants a {GameBalance.MycelialBloomEffectPerLevel:P0} increased chance to grow in the four cardinal directions.",
                "Hyphal strands thicken and surge outward, driven by nutrient cues and quorum signals.",
                MutationType.GrowthChance, GameBalance.MycelialBloomEffectPerLevel, 1,
                GameBalance.MycelialBloomMaxLevel, MutationCategory.Growth, MutationTier.Tier1));

            MakeRoot(new Mutation(MutationIds.HomeostaticHarmony, "Homeostatic Harmony",
                $"Each level reduces self-death probability during decay by {GameBalance.HomeostaticHarmonyEffectPerLevel:P0}.",
                "Oscillatory homeostasis stabilizes intracellular pressure and toxin accumulation.",
                MutationType.DefenseSurvival, GameBalance.HomeostaticHarmonyEffectPerLevel, 1,
                GameBalance.HomeostaticHarmonyMaxLevel, MutationCategory.CellularResilience, MutationTier.Tier1));

            MakeRoot(new Mutation(MutationIds.SilentBlight, "Silent Blight",
                $"Each level increases the base decay chance of enemy cells by {GameBalance.SilentBlightEffectPerLevel:P0}.",
                "A dormant enzymatic payload triggers necrotic collapse in adjacent competitors.",
                MutationType.EnemyDecayChance, GameBalance.SilentBlightEffectPerLevel, 1,
                GameBalance.SilentBlightMaxLevel, MutationCategory.Fungicide, MutationTier.Tier1));

            MakeRoot(new Mutation(MutationIds.AdaptiveExpression, "Adaptive Expression",
                $"Each level grants a {GameBalance.AdaptiveExpressionEffectPerLevel:P0} chance to gain an additional mutation point each round.",
                "Epigenetic drift activates opportunistic transcription bursts in volatile environments.",
                MutationType.BonusMutationPointChance, GameBalance.AdaptiveExpressionEffectPerLevel, 1,
                GameBalance.AdaptiveExpressionMaxLevel, MutationCategory.GeneticDrift, MutationTier.Tier1));

            // Tier-2
            MakeChild(new Mutation(MutationIds.ChronoresilientCytoplasm, "Chronoresilient Cytoplasm",
                $"Each level increases the age threshold before death risk begins by {GameBalance.ChronoresilientCytoplasmEffectPerLevel} growth cycles.",
                "Temporal buffering vesicles shield core organelles from oxidative stress.",
                MutationType.SelfAgeResetThreshold, GameBalance.ChronoresilientCytoplasmEffectPerLevel, 1,
                GameBalance.ChronoresilientCytoplasmMaxLevel, MutationCategory.CellularResilience, MutationTier.Tier2),
                new MutationPrerequisite(MutationIds.HomeostaticHarmony, 10));

            MakeChild(new Mutation(MutationIds.EncystedSpores, "Encysted Spores",
                $"Each level grants a {GameBalance.EncystedSporesEffectPerLevel:P0} death pressure bonus against enemies surrounded on three or more sides.",
                "Encapsulation triggers lytic enzyme excretion in high-density microclimates.",
                MutationType.EncystedSporeMultiplier, GameBalance.EncystedSporesEffectPerLevel, 1,
                GameBalance.EncystedSporesMaxLevel, MutationCategory.Fungicide, MutationTier.Tier2),
                new MutationPrerequisite(MutationIds.SilentBlight, 10));

            AddTendril(MutationIds.TendrilNorthwest, "Northwest");
            AddTendril(MutationIds.TendrilNortheast, "Northeast");
            AddTendril(MutationIds.TendrilSoutheast, "Southeast");
            AddTendril(MutationIds.TendrilSouthwest, "Southwest");

            MakeChild(new Mutation(MutationIds.MutatorPhenotype, "Mutator Phenotype",
                $"Each level grants a {GameBalance.MutatorPhenotypeEffectPerLevel:P0} chance to automatically upgrade a random mutation each round.",
                "Transposons disrupt regulatory silencing, igniting stochastic trait amplification.",
                MutationType.AutoUpgradeRandom, GameBalance.MutatorPhenotypeEffectPerLevel, 1,
                GameBalance.MutatorPhenotypeMaxLevel, MutationCategory.GeneticDrift, MutationTier.Tier2),
                new MutationPrerequisite(MutationIds.AdaptiveExpression, 5));

            // Tier-3
            MakeChild(new Mutation(MutationIds.Necrosporulation, "Necrosporulation",
                $"Each level grants a {GameBalance.NecrosporulationEffectPerLevel:P0} chance to spawn a new cell when a fungal cell dies.",
                "Cytoplasmic apoptosis releases sporogenic factors for opportunistic rebirth.",
                MutationType.SporeOnDeathChance, GameBalance.NecrosporulationEffectPerLevel, 1,
                GameBalance.NecrosporulationMaxLevel, MutationCategory.CellularResilience, MutationTier.Tier3),
                new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 5));

            MakeChild(new Mutation(MutationIds.PutrefactiveMycotoxin, "Putrefactive Mycotoxin",
                $"Each level adds a {GameBalance.PutrefactiveMycotoxinEffectPerLevel:P0} death chance to enemy cells adjacent to tiles you control.",
                "Secretes lipid-bound mycotoxins through adjacent cell walls, disrupting membrane integrity.",
                MutationType.OpponentExtraDeathChance, GameBalance.PutrefactiveMycotoxinEffectPerLevel, 1,
                GameBalance.PutrefactiveMycotoxinMaxLevel, MutationCategory.Fungicide, MutationTier.Tier3),
                new MutationPrerequisite(MutationIds.EncystedSpores, 5));

            MakeChild(new Mutation(MutationIds.AnabolicInversion, "Anabolic Inversion",
                $"Each level adds a {GameBalance.AnabolicInversionGapBonusPerLevel:P0} chance to earn {GameBalance.AnabolicInversionPointsPerUpgrade} bonus points when you trail in living cells.",
                "Under metabolic duress, anabolism inverts into compensatory feedback loops, prioritizing genomic plasticity.",
                MutationType.BonusMutationPointChance, GameBalance.AnabolicInversionGapBonusPerLevel,
                GameBalance.AnabolicInversionPointsPerUpgrade, GameBalance.AnabolicInversionMaxLevel,
                MutationCategory.GeneticDrift, MutationTier.Tier3),
                new MutationPrerequisite(MutationIds.MutatorPhenotype, 3));

            MakeChild(new Mutation(MutationIds.MycotropicInduction, "Mycotropic Induction",
                $"Each level increases all diagonal growth probabilities by {GameBalance.MycotropicInductionEffectPerLevel:P0}.",
                "Signal transduction pathways activate branching vesicle recruitment along orthogonal gradients.",
                MutationType.TendrilDirectionalMultiplier, GameBalance.MycotropicInductionEffectPerLevel, 1,
                GameBalance.MycotropicInductionMaxLevel, MutationCategory.Growth, MutationTier.Tier3),
                new MutationPrerequisite(MutationIds.TendrilNorthwest, 1),
                new MutationPrerequisite(MutationIds.TendrilNortheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSoutheast, 1),
                new MutationPrerequisite(MutationIds.TendrilSouthwest, 1));

            return (allMutations, rootMutations);

            void AddTendril(int id, string dir)
            {
                MakeChild(new Mutation(id, $"Tendril {dir}",
                    $"Each level grants a {GameBalance.DiagonalGrowthEffectPerLevel:P0} chance to grow in the {dir.ToLower()} direction.",
                    $"Polarity vectors align hyphal tip extension toward {dir.ToLower()} moisture gradients.",
                    dir switch
                    {
                        "Northwest" => MutationType.GrowthDiagonal_NW,
                        "Northeast" => MutationType.GrowthDiagonal_NE,
                        "Southeast" => MutationType.GrowthDiagonal_SE,
                        "Southwest" => MutationType.GrowthDiagonal_SW,
                        _ => throw new System.Exception("Invalid direction")
                    }, GameBalance.DiagonalGrowthEffectPerLevel, 1, GameBalance.DiagonalGrowthMaxLevel,
                    MutationCategory.Growth, MutationTier.Tier2),
                    new MutationPrerequisite(MutationIds.MycelialBloom, 10));
            }
        }
    }
}
