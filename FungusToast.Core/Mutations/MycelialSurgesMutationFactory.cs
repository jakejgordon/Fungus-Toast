using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating MycelialSurges category mutations.
    /// </summary>
    public static class MycelialSurgesMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            // Tier-2
            helper.MakeChild(new Mutation(
                id: MutationIds.HyphalSurge,
                name: "Hyphal Surge",
                description: $"Increases your hyphal outgrowth chance by {helper.FormatPercent(GameBalance.HyphalSurgeEffectPerLevel)} per level for {GameBalance.HyphalSurgeDurationRounds} rounds. Each activation costs {GameBalance.HyphalSurgePointsPerActivation} mutation points plus {GameBalance.HyphalSurgePointIncreasePerLevel} per level already gained.",
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

            helper.MakeChild(new Mutation(
                id: MutationIds.HyphalVectoring,
                name: "Hyphal Vectoring",
                description:
                    $"At the end of the Growth Phase (for {GameBalance.HyphalVectoringSurgeDuration} turns after activation), this mutation projects a straight line of living fungal cells toward the center of the toast. " +
                    $"It spawns {GameBalance.HyphalVectoringBaseTiles} cells at level 0, plus {helper.FormatFloat(GameBalance.HyphalVectoringTilesPerLevel)} per level.\n\n" +
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

            helper.MakeChild(new Mutation(
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
            helper.MakeChild(new Mutation(
                id: MutationIds.MimeticResilience,
                name: "Mimetic Resilience",
                description: $"For {GameBalance.MimeticResilienceSurgeDuration} rounds after activation, " +
                             $"at the end of each Growth Phase, attempts to place 1 resistant cell adjacent to " +
                             $"resistant cells belonging to each player with at least {helper.FormatPercent(GameBalance.MimeticResilienceMinimumCellAdvantageThreshold)} more living cells and controlling " +
                             $"at least {helper.FormatPercent(GameBalance.MimeticResilienceMinimumBoardControlThreshold)} of the board. Prioritizes infesting enemy cells over empty placements. Each activation costs {GameBalance.MimeticResiliencePointsPerActivation} " +
                             $"mutation points, increasing by {GameBalance.MimeticResiliencePointIncreasePerLevel} per level.",
                flavorText: "Driven to the edge of extinction, the colony activates mimetic pathways, copying the defensive adaptations of thriving neighbors and spreading borrowed resistance through its own weakened cells.",
                type: MutationType.MimeticResilience,
                effectPerLevel: 1f, // One resistant cell per activation
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.MimeticResilienceMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier3,
                isSurge: true,
                surgeDuration: GameBalance.MimeticResilienceSurgeDuration,
                pointsPerActivation: GameBalance.MimeticResiliencePointsPerActivation,
                pointIncreasePerLevel: GameBalance.MimeticResiliencePointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.ChitinFortification, 1), // MycelialSurges
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 3)); // CellularResilience
        }
    }
}