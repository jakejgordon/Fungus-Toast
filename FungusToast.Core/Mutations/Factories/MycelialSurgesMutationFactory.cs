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
                description: $"For {GameBalance.HyphalSurgeDurationRounds} rounds, increases your hyphal outgrowth chance by {helper.FormatPercent(GameBalance.HyphalSurgeEffectPerLevel)} per level. Each activation costs {GameBalance.HyphalSurgePointsPerActivation} mutation points plus {GameBalance.HyphalSurgePointIncreasePerLevel} per level already gained.",
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
                id: MutationIds.ChemotacticBeacon,
                name: "Chemotactic Beacon",
                description:
                    $"For {GameBalance.ChemotacticBeaconSurgeDuration} rounds, marks one empty non-nutrient tile as a Chemobeacon on activation. At Growth Phase end, projects a line of {GameBalance.ChemotacticBeaconBaseTiles} + {GameBalance.ChemotacticBeaconTilesPerLevel}/level living cells toward the marker. Replaces toxins, dead cells, enemy cells, and empty tiles in its path; skips over friendly living cells.",
                flavorText: "A volatile lure condenses over bare toast, exhaling a phantom food trail that bends the colony's advance while the beacon itself slowly evaporates.",
                type: MutationType.ChemotacticBeacon,
                effectPerLevel: GameBalance.ChemotacticBeaconTilesPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.ChemotacticBeaconMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier2,
                isSurge: true,
                surgeDuration: GameBalance.ChemotacticBeaconSurgeDuration,
                pointsPerActivation: GameBalance.ChemotacticBeaconPointsPerActivation,
                pointIncreasePerLevel: GameBalance.ChemotacticBeaconPointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.MycelialBloom, 7)
            );

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.MimeticResilience,
                name: "Mimetic Resilience",
                description:
                    $"For {GameBalance.MimeticResilienceSurgeDuration} rounds, places resistant cells near rival resistant cells of players with {helper.FormatPercent(GameBalance.MimeticResilienceMinimumCellAdvantageThreshold, 1)}+ more living cells and {helper.FormatPercent(GameBalance.MimeticResilienceMinimumBoardControlThreshold, 1)}+ board control. " +
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
                pointIncreasePerLevel: GameBalance.MimeticResiliencePointIncreasePerLevel,
                aiTags: MutationAITags.CatchUp
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
                pointIncreasePerLevel: GameBalance.CompetitiveAntagonismPointIncreasePerLevel,
                aiTags: MutationAITags.CatchUp
            ),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 15)
            );

            helper.MakeChild(new Mutation(
                id: MutationIds.ChitinFortification,
                name: "Chitin Fortification",
                description:
                    $"For {GameBalance.ChitinFortificationSurgeDuration} rounds, before each Growth Phase, {GameBalance.ChitinFortificationCellsPerLevel} random living cell(s) per level gain Resistant. " +
                    $"Resistant living cells cannot be killed or replaced. " +
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