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
                    description: $"Each level grants a {FormatPercent(GameBalance.TendrilDiagonalGrowthEffectPerLevel)} chance to grow in the {dir.ToLower()} direction.",
                    flavorText: $"Polarity vectors align hyphal tip extension toward {dir.ToLower()} moisture gradients.",
                    type: dir switch
                    {
                        "Northwest" => MutationType.GrowthDiagonal_NW,
                        "Northeast" => MutationType.GrowthDiagonal_NE,
                        "Southeast" => MutationType.GrowthDiagonal_SE,
                        "Southwest" => MutationType.GrowthDiagonal_SW,
                        _ => throw new System.Exception("Invalid direction")
                    },
                    effectPerLevel: GameBalance.TendrilDiagonalGrowthEffectPerLevel,
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
                description: "At the start of each decay phase, you release toxin spores based on this mutation's level and your number of failed growth attempts. The number of toxins scales with your mutation level (with diminishing returns) and failed growths, but is capped based on board size. Toxins are placed on unoccupied tiles orthogonally adjacent to enemy cells, creating temporary toxin zones that block enemy growth and reclamation.",
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
            ), new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5));

            MakeChild(new Mutation(
                id: MutationIds.MycotoxinPotentiation,
                name: "Mycotoxin Potentiation",
                description: $"Each level extends the lifespan of new toxin tiles by {FormatFloat(GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel)} growth cycle(s), " +
                             $"and grants a {FormatPercent(GameBalance.MycotoxinPotentiationKillChancePerLevel)} chance per level to kill an orthogonally adjacent enemy fungal cell from each toxin tile during the Decay Phase.",
                flavorText: "Toxins thicken with stabilizing glycoproteins, lingering longer and lashing out at encroaching invaders.",
                type: MutationType.ToxinKillAura,
                effectPerLevel: GameBalance.MycotoxinPotentiationKillChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.MycotoxinPotentiationMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier2
            ),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 5));


            AddTendril(MutationIds.TendrilNorthwest, "Northwest");
            AddTendril(MutationIds.TendrilNortheast, "Northeast");
            AddTendril(MutationIds.TendrilSoutheast, "Southeast");
            AddTendril(MutationIds.TendrilSouthwest, "Southwest");

            MakeChild(new Mutation(
                id: MutationIds.AdaptiveExpression,
                name: "Adaptive Expression",
                description: $"Each level grants a {FormatPercent(GameBalance.AdaptiveExpressionEffectPerLevel)} chance to gain an additional mutation point each round. If the first point is awarded, each level also grants a {FormatPercent(GameBalance.AdaptiveExpressionSecondPointChancePerLevel)} chance to earn a second mutation point.",
                flavorText: "Epigenetic drift activates opportunistic transcription bursts in volatile environments.",
                type: MutationType.BonusMutationPointChance,
                effectPerLevel: GameBalance.AdaptiveExpressionEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.AdaptiveExpressionMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 5));


            MakeChild(new Mutation(
                id: MutationIds.MycotoxinCatabolism,
                name: "Mycotoxin Catabolism",
                description: $"At the start of each growth phase, each level grants a {FormatPercent(GameBalance.MycotoxinCatabolismCleanupChancePerLevel)} chance for each living fungal cell to metabolize each orthogonally adjacent toxin tile. " +
                             $"Each toxin tile metabolized this way grants a {FormatPercent(GameBalance.MycotoxinCatabolismMutationPointChancePerLevel)} chance to gain a bonus mutation point, up to one point per tile (multiple points possible per turn, depending on the number of toxin tiles catabolized).",
                flavorText: "Evolved metabolic pathways enable the breakdown of toxic compounds, reclaiming nutrients from chemical hazards and occasionally triggering adaptive bursts of mutation.",
                type: MutationType.ToxinCleanupAndMPBonus,
                effectPerLevel: GameBalance.MycotoxinCatabolismCleanupChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.MycotoxinCatabolismMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.MutatorPhenotype, 2));


            MakeChild(new Mutation(
                id: MutationIds.HyphalSurge,
                name: "Hyphal Surge",
                description: $"Increases your hyphal outgrowth chance by {FormatPercent(GameBalance.HyphalSurgeEffectPerLevel)} per level for {GameBalance.HyphalSurgeDurationRounds} rounds. Each activation costs {GameBalance.HyphalSurgePointsPerActivation} mutation points plus {GameBalance.HyphalSurgePointIncreasePerLevel} per level already gained.",
                flavorText: "A fleeting burst of energy, driving a furious wave of mycelial expansion across new ground.",
                type: MutationType.GrowthChance,
                effectPerLevel: GameBalance.HyphalSurgeEffectPerLevel,
                pointsPerUpgrade: GameBalance.HyphalSurgePointsPerActivation,
                maxLevel: GameBalance.HyphalSurgeMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier2,
                isSurge: true,
                surgeDuration: GameBalance.HyphalSurgeDurationRounds,
                pointsPerActivation: GameBalance.HyphalSurgePointsPerActivation,
                pointIncreasePerLevel: GameBalance.HyphalSurgePointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.MycelialBloom, 5)
            );

            MakeChild(new Mutation(
                id: MutationIds.HyphalVectoring,
                name: "Hyphal Vectoring",
                description:
                    $"At the end of the Growth Phase (for {GameBalance.HyphalVectoringSurgeDuration} turns after activation), this mutation projects a straight line of living fungal cells toward the center of the toast. " +
                    $"It spawns {GameBalance.HyphalVectoringBaseTiles} cells at level 0, plus {FormatFloat(GameBalance.HyphalVectoringTilesPerLevel)} per level.\n\n" +
                    $"The origin is intelligently selected to prioritize: paths with fewest friendly cells, maximum enemy cells to infest, and proximity to center. " +
                    $"Cells replace anything in their path (toxins, dead mold, enemy mold, empty space) and **skip over friendly living mold** without interruption. " +
                    $"Each activation costs {GameBalance.HyphalVectoringPointsPerActivation} mutation points, increasing by {GameBalance.HyphalVectoringSurgePointIncreasePerLevel} per level. " +
                    $"This mutation can only activate once per {GameBalance.HyphalVectoringSurgeDuration} turns.",
                flavorText:
                    "Guided by centripetal nutrient gradients, apex hyphae launch invasive pulses straight into the heart of contested substrate.",
                type: MutationType.HyphalVectoring,
                effectPerLevel: GameBalance.HyphalVectoringTilesPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.HyphalVectoringMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier2,
                isSurge: true,
                surgeDuration: GameBalance.HyphalVectoringSurgeDuration,
                pointsPerActivation: GameBalance.HyphalVectoringPointsPerActivation,
                pointIncreasePerLevel: GameBalance.HyphalVectoringSurgePointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.TendrilNorthwest, 1),
            new MutationPrerequisite(MutationIds.TendrilSoutheast, 1));

            MakeChild(new Mutation(
                id: MutationIds.ChitinFortification,
                name: "Chitin Fortification",
                description: $"At the start of each Growth Phase (for {GameBalance.ChitinFortificationSurgeDuration} rounds after activation), " +
                             $"{GameBalance.ChitinFortificationCellsPerLevel} random living fungal cells per level gain permanent resistance, " +
                             $"making them immune to all death effects. " +
                             $"Each activation costs {GameBalance.ChitinFortificationPointsPerActivation} mutation points, " +
                             $"increasing by {GameBalance.ChitinFortificationPointIncreasePerLevel} per level gained.",
                flavorText: "Accelerated chitin synthesis reinforces cellular walls with crystalline matrices, forming impenetrable barriers against hostile incursions.",
                type: MutationType.ChitinFortification,
                effectPerLevel: GameBalance.ChitinFortificationCellsPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.ChitinFortificationMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier2,
                isSurge: true,
                surgeDuration: GameBalance.ChitinFortificationSurgeDuration,
                pointsPerActivation: GameBalance.ChitinFortificationPointsPerActivation,
                pointIncreasePerLevel: GameBalance.ChitinFortificationPointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5));


            // Tier-3
            MakeChild(new Mutation(
                id: MutationIds.Necrosporulation,
                name: "Necrosporulation",
                description: $"Each level grants a {FormatPercent(GameBalance.NecrosporulationEffectPerLevel)} chance to spawn a new cell at a random spot on the toast when a fungal cell dies.",
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
                description: $"Each level adds a {FormatPercent(GameBalance.PutrefactiveMycotoxinEffectPerLevel)} death chance to enemy cells orthogonally adjacent to your living fungal cells.",
                flavorText: "Secretes lipid-bound mycotoxins through adjacent cell walls, disrupting membrane integrity.",
                type: MutationType.AdjacentFungicide,
                effectPerLevel: GameBalance.PutrefactiveMycotoxinEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.PutrefactiveMycotoxinMaxLevel,
                category: MutationCategory.Fungicide,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.MycotoxinPotentiation, 1));

            MakeChild(new Mutation(
                id: MutationIds.AnabolicInversion,
                name: "Anabolic Inversion",
                description: $"Each level adds a {FormatPercent(GameBalance.AnabolicInversionGapBonusPerLevel)} chance to earn 1–5 bonus mutation points when you control fewer living cells than other players. The chance increases the further behind you are, and the bonus amount is weighted - the further behind you are, the higher chance of getting the maximum payout.",
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
                description: $"After growth and before decay, each living cell has a {FormatPercent(GameBalance.RegenerativeHyphaeReclaimChance)} chance per level to reclaim one orthogonally adjacent dead cell it previously owned. " +
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


            MakeChild(new Mutation(
                id: MutationIds.CreepingMold,
                name: "Creeping Mold",
                description: $"Each level grants a {FormatPercent(GameBalance.CreepingMoldMoveChancePerLevel)} chance to move into a target tile after a failed growth attempt (replaces the original cell). At max level, Creeping Mold can jump over a single toxin tile: if a failed growth is blocked by a toxin in a cardinal direction, and the roll succeeds, the mold will leap over the toxin and attempt to land on the next tile in the same direction.",
                flavorText: "Hyphal strands abandon anchor points to invade fresh substrate through pseudopodial crawling.",
                type: MutationType.CreepingMovementOnFailedGrowth,
                effectPerLevel: GameBalance.CreepingMoldMoveChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.CreepingMoldMaxLevel,
                category: MutationCategory.Growth,
                tier: MutationTier.Tier4
            ),
                new MutationPrerequisite(MutationIds.MycotropicInduction, 3));


            MakeChild(new Mutation(
                id: MutationIds.SporocidalBloom,
                name: "Sporocidal Bloom",
                description:
                    "At the end of each round, your colony vents toxic spores that disperse randomly across the board. " +
                    "Each level of this mutation releases spores at approximately " + FormatPercent(0.07f) + " per living fungal cell, scaling with your colony's size and mutation level.\n" +
                    "\n" +
                    "Each spore lands on a random tile:\n" +
                    "• If it lands on an enemy fungal cell, it kills that cell and leaves a toxin in its place.\n" +
                    "• If it lands on or orthogonally adjacent to one of your own living cells, nothing happens.\n" +
                    "• If it lands on an empty tile that is not orthogonally adjacent to your living cells, it becomes a toxin.",
                flavorText:
                    "Once mature, the colony begins venting spores laced with cytotoxic compounds, drifting indiscriminately to poison competitors and sterilize open ground.",
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


            MakeChild(new Mutation(
                id: MutationIds.NecrotoxicConversion,
                name: "Necrotoxic Conversion",
                description: $"Each level grants a {FormatPercent(GameBalance.NecrotoxicConversionReclaimChancePerLevel)} chance to immediately reclaim any cell that dies to your toxin effects. " +
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

            MakeChild(new Mutation(
                id: MutationIds.NecrophyticBloom,
                name: "Necrophytic Bloom",
                description:
                    $"Activates once {FormatPercent(GameBalance.NecrophyticBloomActivationThreshold)} of the board is occupied. " +
                    $"At that moment, all of your previously dead, non-toxin fungal cells release " +
                    $"{FormatFloat(GameBalance.NecrophyticBloomSporesPerDeathPerLevel)} spores per cell per level. " +
                    $"Released spores randomly drop on the board, reclaiming any dead cell they land upon. " +
                    $"After activation, each new death releases additional spores with diminishing effectiveness as crowding increases.",
                flavorText: "When population pressure nears collapse, the mycelium initiates necrophytic recovery — resurrecting fallen cells and seeding the surface in desperate bloom.",
                type: MutationType.NecrophyticBloomSporeDrop,
                effectPerLevel: GameBalance.NecrophyticBloomSporesPerDeathPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.NecrophyticBloomMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier4
            ),
            new MutationPrerequisite(MutationIds.AnabolicInversion, 1),
            new MutationPrerequisite(MutationIds.Necrosporulation, 1));

            //Tier-5
            MakeChild(new Mutation(
                id: MutationIds.NecrohyphalInfiltration,
                name: "Necrohyphal Infiltration",
                description:
                    $"Each level grants a {FormatPercent(GameBalance.NecrohyphalInfiltrationChancePerLevel)} chance for your living cells to grow into an orthogonally adjacent dead enemy cell. " +
                    $"When successful, each level also grants a {FormatPercent(GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel)} chance to immediately cascade into another orthogonally adjacent dead cell, potentially chaining across the battlefield.",
                flavorText: "Necrohyphae tunnel through decaying rivals, infiltrating their remains and reawakening them as loyal extensions of the colony. On rare occasions, this necrotic surge propagates, consuming entire graveyards in a wave of resurrection.",
                type: MutationType.NecrohyphalInfiltration,
                effectPerLevel: GameBalance.NecrohyphalInfiltrationChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier4),
                maxLevel: GameBalance.NecrohyphalInfiltrationMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.RegenerativeHyphae, 1),
            new MutationPrerequisite(MutationIds.MycotoxinPotentiation, 1));

            MakeChild(new Mutation(
                id: MutationIds.PutrefactiveRejuvenation,
                name: "Putrefactive Rejuvenation",
                description: $"Whenever your mold kills an orthogonally adjacent living enemy cell (e.g., via Putrefactive Mycotoxin), it saps the nutrients and removes <b>{GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel}</b> growth cycles per mutation level from the age of any friendly living cells within <b>{GameBalance.PutrefactiveRejuvenationEffectRadius}</b> tiles of the poisoned cell. At max level, the distance is doubled.\n" +
                $"Additionally, each level increases the effectiveness of Putrefactive Mycotoxin by <b>{FormatPercent(GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel)}</b>.",
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

            MakeChild(new Mutation(
                id: MutationIds.CatabolicRebirth,
                name: "Catabolic Rebirth",
                description: $"When a toxin expires orthogonally adjacent to one of your dead cells, there is a {FormatPercent(GameBalance.CatabolicRebirthResurrectionChancePerLevel)} chance per level to revive that dead cell as a living cell. " +
                             $"This represents the energy released during toxin breakdown catalyzing cellular regeneration in your dormant cells.\n" +
                             $"<b>Max Level Bonus:</b> Enemy toxin tiles orthogonally adjacent to your dead cells lose age twice as fast.",
                flavorText: "The breakdown of toxic compounds releases catalytic energy that triggers dormant cellular machinery, resurrecting fallen cells through the metabolic alchemy of catabolic processes. At full power, the colony's presence accelerates the decay of enemy toxins, purifying the battlefield for a final resurgence.",
                type: MutationType.ToxinExpirationResurrection,
                effectPerLevel: GameBalance.CatabolicRebirthResurrectionChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier6),
                maxLevel: GameBalance.CatabolicRebirthMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier6
            ),
            new MutationPrerequisite(MutationIds.NecrohyphalInfiltration, 1),
            new MutationPrerequisite(MutationIds.AnabolicInversion, 1));

            // Tier-6
            MakeChild(new Mutation(
                id: MutationIds.PutrefactiveCascade,
                name: "Putrefactive Cascade",
                description: $"Each level increases the effectiveness of Putrefactive Mycotoxin by {FormatPercent(GameBalance.PutrefactiveCascadeEffectivenessBonus)} " +
                             $"and grants a {FormatPercent(GameBalance.PutrefactiveCascadeCascadeChance)} chance for each putrefactive kill to cascade " +
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

            MakeChild(new Mutation(
                id: MutationIds.HyperadaptiveDrift,
                name: "Hyperadaptive Drift",
                description:
                    $"Enhances your Mutator Phenotype's auto-upgrade ability. Each level provides a {FormatPercent(GameBalance.HyperadaptiveDriftHigherTierChancePerLevel)} chance to target higher-tier mutations (Tier 2-4) instead of Tier 1, and a {FormatPercent(GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel)} chance to upgrade Tier 1 mutations twice instead of once.\n" +
                    "\n" +
                    "At max level, automatically upgrades an additional Tier 1 mutation whenever this effect triggers.\n" +
                    "\n" +
                    "This works passively with your existing Mutator Phenotype auto-upgrade system.",
                flavorText: "The colony achieves a state of hyperplasticity, capable of instantaneous genomic restructuring and parallel mutation cascades.",
                type: MutationType.FreeMutationUpgrade,
                effectPerLevel: 1.0f,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier5),
                maxLevel: GameBalance.HyperadaptiveDriftMaxLevel,
                category: MutationCategory.GeneticDrift,
                tier: MutationTier.Tier5
            ),
            new MutationPrerequisite(MutationIds.NecrophyticBloom, 1),
            new MutationPrerequisite(MutationIds.MycotropicInduction, 1),
            new MutationPrerequisite(MutationIds.MycotoxinPotentiation, 1),
            new MutationPrerequisite(MutationIds.AdaptiveExpression, 1),
            new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 1));

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

