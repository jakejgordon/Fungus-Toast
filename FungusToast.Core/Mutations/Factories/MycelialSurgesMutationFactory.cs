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
                type: MutationType.HyphalSurge,
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
                    $"For {GameBalance.HyphalVectoringSurgeDuration} turns after activation, projects a line of {GameBalance.HyphalVectoringBaseTiles} + {GameBalance.HyphalVectoringTilesPerLevel}/level living cells toward the toast center at Growth Phase end. " +
                    $"Replaces toxins, dead cells, enemy cells, and empty tiles in its path; skips over friendly living cells. " +
                    $"Each activation costs {GameBalance.HyphalVectoringPointsPerActivation} mutation points plus {GameBalance.HyphalVectoringSurgePointIncreasePerLevel} per level.",
                flavorText: "Hyphal networks realign toward the center of the substrate, bulldozing through opposition with deliberate, uncompromising purpose.",
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
            new MutationPrerequisite(MutationIds.MycelialBloom, 7)
            );

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.MimeticResilience,
                name: "Mimetic Resilience",
                description:
                    $"For {GameBalance.MimeticResilienceSurgeDuration} rounds after activation, places resistant cells near rival resistant cells of players with {helper.FormatPercent(GameBalance.MimeticResilienceMinimumCellAdvantageThreshold, 1)}+ more living cells and {helper.FormatPercent(GameBalance.MimeticResilienceMinimumBoardControlThreshold, 1)}+ board control. " +
                    $"Placement range: level + 1 tiles from target. Prefers infesting enemy cells over empty placement. " +
                    $"Each activation costs {GameBalance.MimeticResiliencePointsPerActivation} mutation points plus {GameBalance.MimeticResiliencePointIncreasePerLevel} per level.",
                flavorText: "The colony analyzes and replicates the defensive adaptations of more successful rivals, establishing resistant footholds in their territories through biomimetic infiltration.",
                type: MutationType.MimeticResilience,
                effectPerLevel: 1.0f, // Static effect: always 1 placement per qualifying target player
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.MimeticResilienceMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier3,
                isSurge: true,
                surgeDuration: GameBalance.MimeticResilienceSurgeDuration,
                pointsPerActivation: GameBalance.MimeticResiliencePointsPerActivation,
                pointIncreasePerLevel: GameBalance.MimeticResiliencePointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 3)
            );

            helper.MakeChild(new Mutation(
                id: MutationIds.CompetitiveAntagonism,
                name: "Competitive Antagonism",
                description:
                    $"For {GameBalance.CompetitiveAntagonismSurgeDuration} rounds, biases Mycotoxin Tracers, Sporicidal Bloom, and Necrophytic Bloom toward stronger colonies. " +
                    $"Sporicidal Bloom favors enemy cells over empty tiles; Necrophytic Bloom skips friendly and weaker targets; Tracers prioritize stronger borders. " +
                    $"Each activation costs {GameBalance.CompetitiveAntagonismPointsPerActivation} mutation points plus {GameBalance.CompetitiveAntagonismPointIncreasePerLevel} per level.",
                flavorText: "Evolved chemoreceptors identify and aggressively target thriving competitors, directing toxin production and necrophytic expansion toward the most successful rival colonies with lethal precision.",
                type: MutationType.CompetitiveAntagonism,
                effectPerLevel: 1.0f, // Effect strength per level
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.CompetitiveAntagonismMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier3,
                isSurge: true,
                surgeDuration: GameBalance.CompetitiveAntagonismSurgeDuration,
                pointsPerActivation: GameBalance.CompetitiveAntagonismPointsPerActivation,
                pointIncreasePerLevel: GameBalance.CompetitiveAntagonismPointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 15)
            );

            helper.MakeChild(new Mutation(
                id: MutationIds.ChitinFortification,
                name: "Chitin Fortification",
                description:
                    $"On activation, grants resistance to {GameBalance.ChitinFortificationCellsPerLevel} random living cell(s) per level for {GameBalance.ChitinFortificationDurationRounds} rounds. " +
                    $"Resistant cells cannot be killed or replaced until the surge expires. " +
                    $"Each activation costs {GameBalance.ChitinFortificationPointsPerActivation} mutation points plus {GameBalance.ChitinFortificationPointIncreasePerLevel} per level.",
                flavorText: "Rapid chitin synthesis creates an impenetrable exoskeleton around select cells, rendering them invulnerable to all forms of destruction for a limited time.",
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
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5)
            );
        }
    }
}