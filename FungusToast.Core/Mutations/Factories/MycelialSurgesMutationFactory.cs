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
                name: "Autolytic Surge",
                description: $"Accelerates your colony's growth at the cost of making its cells more likely to die.\n\n" +
                             $"<b>Technical:</b> While active, each level adds {helper.FormatPercent(GameBalance.HyphalSurgeEffectPerLevel)} orthogonal growth chance (up / down / left / right) per Growth Cycle and {helper.FormatPercent(GameBalance.HyphalSurgeRandomDecayPenaltyPerLevel)} random decay chance per Decay Phase for {GameBalance.HyphalSurgeDurationRounds} rounds. Each activation costs {GameBalance.HyphalSurgePointsPerActivation} mutation points plus {GameBalance.HyphalSurgePointIncreasePerLevel} per current level.",
                flavorText: "The colony drives rapid outward growth by dissolving part of its own living network into fresh substrate.",
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
                id: MutationIds.NecroticClearance,
                name: "Necrotic Clearance",
                description:
                    "Lets your living cells clear nearby dead cells before enemies can reclaim them.\n\n" +
                    $"<b>Technical:</b> While active, before each Growth Phase, each living cell has a {helper.FormatPercent(GameBalance.NecroticClearanceChancePerLevel)} chance per level to clear one orthogonally adjacent (up / down / left / right) dead cell you own, preferring dead cells next to enemy living cells. Attempts against those contested dead cells have double chance. Cleared dead cells cannot be reclaimed. Each activation costs {GameBalance.NecroticClearancePointsPerActivation} mutation points plus {GameBalance.NecroticClearancePointIncreasePerLevel} per current level.",
                flavorText: "The colony dissolves compromised tissue before rival hyphae can turn it into a foothold.",
                type: MutationType.NecroticClearance,
                effectPerLevel: GameBalance.NecroticClearanceChancePerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.NecroticClearanceMaxLevel,
                category: MutationCategory.MycelialSurges,
                tier: MutationTier.Tier2,
                isSurge: true,
                surgeDuration: GameBalance.NecroticClearanceSurgeDuration,
                pointsPerActivation: GameBalance.NecroticClearancePointsPerActivation,
                pointIncreasePerLevel: GameBalance.NecroticClearancePointIncreasePerLevel
            ),
            new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5)
            );

            helper.MakeChild(new Mutation(
                id: MutationIds.ChemotacticBeacon,
                name: "Chemotactic Beacon",
                description:
                    $"Lets you place a target marker and grow a straight line toward it.\n\n" +
                    $"<b>Technical:</b> On activation, mark one empty non-nutrient tile. At Growth Phase end while the surge is active, grow a line of {GameBalance.ChemotacticBeaconBaseTiles} + {GameBalance.ChemotacticBeaconTilesPerLevel}/level living cells along the line from your starting spore toward that marker, starting just past your furthest living cell on that line. Once the line reaches the marker, remaining growth spirals clockwise around it. This growth overgrows toxins, reclaims dead cells, infests enemy living cells, and colonizes empty tiles in its path.\n" +
                    $"Buffed by: Putrefactive Mycotoxin.",
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
                    $"Lets you try to copy Resistant footholds from stronger enemies.\n\n" +
                    $"<b>Technical:</b> While active, target enemy players with {helper.FormatPercent(GameBalance.MimeticResilienceMinimumCellAdvantageThreshold, 1)}+ more living cells and {helper.FormatPercent(GameBalance.MimeticResilienceMinimumBoardControlThreshold, 1)}+ board control. Around each of their Resistant living cells, attempt to place one of your own Resistant cells within level + 1 tiles (including diagonals), preferring to infest enemy living cells, then overgrow enemy toxins, colonize empty tiles, or reclaim dead cells. Each success makes later attempts against that same enemy 5% less likely, and the effect stops after 20 successes against one enemy in the same Growth Phase. Each activation costs {GameBalance.MimeticResiliencePointsPerActivation} mutation points plus {GameBalance.MimeticResiliencePointIncreasePerLevel} per level.",
                flavorText: "The colony copies the hardened tissue of whichever rival is winning, and plants it in their territory.",
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
            new MutationPrerequisite(MutationIds.ChitinFortification, 1),
            new MutationPrerequisite(MutationIds.MycotoxinTracer, 10)
            );

            helper.MakeChild(new Mutation(
                id: MutationIds.CompetitiveAntagonism,
                name: "Competitive Antagonism",
                description:
                    $"Makes your toxin pressure focus more on stronger colonies.\n\n" +
                    $"<b>Technical:</b> While active, Mycotoxin Tracer targets tiles next to larger colonies first. Sporicidal Bloom also removes extra empty tiles and most tiles belonging to smaller colonies from its target pool, so stronger enemies are hit more often. Each activation costs {GameBalance.CompetitiveAntagonismPointsPerActivation} mutation points plus {GameBalance.CompetitiveAntagonismPointIncreasePerLevel} per level.",
                flavorText: "Chemoreceptors tune to the scent of the largest rival colony, and the toxin output follows.",
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
                    $"While active, permanently makes part of your colony Resistant before each Growth Phase.\n\n" +
                    $"<b>Technical:</b> While active, before each Growth Phase, {GameBalance.ChitinFortificationCellsPerLevel} random non-Resistant living cells per level become Resistant. Resistant cells cannot be killed, infested, or poisoned, and they stay Resistant after the surge ends. Each activation costs {GameBalance.ChitinFortificationPointsPerActivation} mutation points plus {GameBalance.ChitinFortificationPointIncreasePerLevel} per level.",
                flavorText: "Rapid chitin synthesis sheathes a few cells in armor that outlasts the surge that made it.",
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
