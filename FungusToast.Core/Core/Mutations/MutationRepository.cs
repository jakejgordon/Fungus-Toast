using FungusToast.Core.Config;
using System.Globalization;

namespace FungusToast.Core.Mutations
{
    public static class MutationRepository
    {
        public static readonly Dictionary<int, Mutation> All;
        public static readonly Dictionary<int, Mutation> Roots;

        /// Cached list of prerequisite chains per mutation ID, ordered from root to leaf.
        public static readonly Dictionary<int, List<Mutation>> PrerequisiteChains;

        static MutationRepository()
        {
            (All, Roots) = BuildFullMutationSet();

            PrerequisiteChains = new Dictionary<int, List<Mutation>>();
            foreach (var mutation in All.Values)
            {
                PrerequisiteChains[mutation.Id] = GetPrerequisiteChain(mutation, All);
            }
        }

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
                id: MutationIds.MycotoxinTracer,
                name: "Mycotoxin Tracer",
                description: "During the decay phase, you release toxin spores based on this mutation’s level and your number of failed growth attempts. " +
                             "These spores settle on nearby unoccupied tiles adjacent to enemies, creating temporary toxin zones that prevent enemy reclamation or growth.",
                flavorText: "Microscopic chemical trails seep outward, clouding enemy borders in dormant inhibition fields.",
                type: MutationType.FungicideToxinSpores,
                effectPerLevel: GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: 50,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier1
            ));



            MakeRoot(new Mutation(
                id: MutationIds.MutatorPhenotype,
                name: "Mutator Phenotype",
                description: $"Each level grants a {FormatPercent(GameBalance.MutatorPhenotypeEffectPerLevel)} chance to automatically upgrade a random mutation each round.",
                flavorText: "Transposons disrupt regulatory silencing, igniting stochastic trait amplification.",
                type: MutationType.AutoUpgradeRandom,
                effectPerLevel: GameBalance.MutatorPhenotypeEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.MutatorPhenotypeMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier1
            ));


            // Tier-2
            MakeChild(new Mutation(
                id: MutationIds.ChronoresilientCytoplasm,
                name: "Chronoresilient Cytoplasm",
                description: $"Each level increases the age threshold before death risk begins by {FormatFloat(GameBalance.ChronoresilientCytoplasmEffectPerLevel)} growth cycles.",
                flavorText: "Temporal buffering vesicles shield core organelles from oxidative stress.",
                type: MutationType.AgeAndRandomnessDecayResistance,
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
            ), new MutationPrerequisite(MutationIds.MycotoxinTracer, 10));

            AddTendril(MutationIds.TendrilNorthwest, "Northwest");
            AddTendril(MutationIds.TendrilNortheast, "Northeast");
            AddTendril(MutationIds.TendrilSoutheast, "Southeast");
            AddTendril(MutationIds.TendrilSouthwest, "Southwest");

            MakeChild(new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Each level grants a {FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel)} chance to gain an additional mutation point each round.",
                flavorText: "Epigenetic drift activates opportunistic transcription bursts in volatile environments.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 5));

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
                type: MutationType.AdjacentFungicide,
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
            ), new MutationPrerequisite(MutationIds.AdaptiveExpression, 3));

            MakeChild(new Mutation(
                id: MutationIds.MycotropicInduction,
                name: "Mycotropic Induction",
                description: $"Each level increases all diagonal growth probabilities by a multiplier of {FormatPercent(GameBalance.MycotropicInductionEffectPerLevel)}.",
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
                description: $"After growth and before decay, each living cell has a {FormatPercent(GameBalance.RegenerativeHyphaeReclaimChance)} chance to reclaim one orthogonally adjacent dead cell it previously owned. " +
                             $"Only one attempt can be made on each dead cell per round.",
                flavorText: "Regrowth cascades from necrotic margins, guided by residual cytoplasmic signaling.",
                type: MutationType.ReclaimOwnDeadCells,
                effectPerLevel: GameBalance.RegenerativeHyphaeReclaimChance,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.RegenerativeHyphaeMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.Necrosporulation, 1),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 1));


            MakeChild(new Mutation(
                id: MutationIds.CreepingMold,
                name: "Creeping Mold",
                description: $"Each level grants a {FormatPercent(GameBalance.CreepingMoldMoveChancePerLevel)} chance to move into a target tile after a failed growth attempt (replaces the original cell).",
                flavorText: "Hyphal strands abandon anchor points to invade fresh substrate through pseudopodial crawling.",
                type: MutationType.CreepingMovementOnFailedGrowth,
                effectPerLevel: GameBalance.CreepingMoldMoveChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.CreepingMoldMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 1));


            MakeChild(new Mutation(
                id: MutationIds.SporocidalBloom,
                name: "Sporocidal Bloom",
                description: $"Each round, releases toxic spores that settle randomly across the board. " +
                             $"Each level releases spores at approximately {FormatPercent(0.07f)} per living fungal cell. " +
                             $"For example, a colony with 40 living cells at level 3 will drop about 8 spores.",
                flavorText: "Once mature, the colony begins to vent spores laced with cytotoxic compounds. " +
                            "These drifting agents settle on competitors and degrade viable mycelial tissue.",
                type: MutationType.FungicideSporeDrop,
                effectPerLevel: GameBalance.SporicialBloomEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.SporocidalBloomMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier4
            ),
            new MutationPrerequisite(MutationIds.PutrefactiveMycotoxin, 1),
            new MutationPrerequisite(MutationIds.Necrosporulation, 1));

            MakeChild(new Mutation(
                id: MutationIds.NecrophyticBloom,
                name: "Necrophytic Bloom",
                description: $"Once {FormatPercent(GameBalance.NecrophyticBloomActivationThreshold)} of the board is occupied, this mutation activates. " +
                             $"Each level grants {FormatFloat(GameBalance.NecrophyticBloomSporesPerLevel)} spores per dead cell owned at activation. " +
                             $"After activation, each new death releases spores scaled by remaining board space — fewer spores drop as crowding increases.",
                flavorText: "When overcrowding threatens expansion, the colony enters a necrophytic phase, reanimating its fallen cells with explosive spore dispersal.",
                type: MutationType.NecrophyticBloomSporeDrop,
                effectPerLevel: GameBalance.NecrophyticBloomSporesPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.NecrophyticBloomMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier4
            ),
            new MutationPrerequisite(MutationIds.SporocidalBloom, 1),
            new MutationPrerequisite(MutationIds.Necrosporulation, 1));

            return (allMutations, rootMutations);
        }

        private static List<Mutation> GetPrerequisiteChain(Mutation mutation, Dictionary<int, Mutation> allMutations)
        {
            var visited = new HashSet<int>();
            var chain = new List<Mutation>();

            void Visit(Mutation m)
            {
                if (!visited.Add(m.Id))
                    return;

                foreach (var prereq in m.Prerequisites)
                {
                    if (allMutations.TryGetValue(prereq.MutationId, out var prereqMutation))
                    {
                        Visit(prereqMutation);
                    }
                }

                chain.Add(m); // Add after visiting prereqs (post-order)
            }

            Visit(mutation);
            return chain;
        }


    }
}

