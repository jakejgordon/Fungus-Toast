using FungusToast.Core.Config;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations.Factories
{
    /// <summary>
    /// Factory for creating CellularResilience category mutations.
    /// </summary>
    public static class CellularResilienceMutationFactory
    {
        public static void CreateMutations(
            Dictionary<int, Mutation> allMutations,
            Dictionary<int, Mutation> rootMutations,
            MutationBuilderHelper helper)
        {
            // Tier-1 Root
            helper.MakeRoot(new Mutation(
                id: MutationIds.HomeostaticHarmony,
                name: "Homeostatic Harmony",
                description: $"Each level reduces the chance of random and age-based deaths by {helper.FormatPercent(GameBalance.HomeostaticHarmonyEffectPerLevel)}.",
                flavorText: "Oscillatory homeostasis stabilizes intracellular pressure and toxin accumulation.",
                type: MutationType.DefenseSurvival,
                effectPerLevel: GameBalance.HomeostaticHarmonyEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier1),
                maxLevel: GameBalance.HomeostaticHarmonyMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier1
            ));

            // Tier-2
            helper.MakeChild(new Mutation(
                id: MutationIds.ChronoresilientCytoplasm,
                name: "Chronoresilient Cytoplasm",
                description: $"Each level increases the age threshold before death risk begins by {helper.FormatFloat(GameBalance.ChronoresilientCytoplasmEffectPerLevel)} growth cycles.",
                flavorText: "Temporal buffering vesicles shield core organelles from oxidative stress.",
                type: MutationType.AgeAndRandomnessDecayResistance,
                effectPerLevel: GameBalance.ChronoresilientCytoplasmEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2),
                maxLevel: GameBalance.ChronoresilientCytoplasmMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier2
            ), new MutationPrerequisite(MutationIds.HomeostaticHarmony, 5));

            // Tier-3
            helper.MakeChild(new Mutation(
                id: MutationIds.Necrosporulation,
                name: "Necrosporulation",
                description: $"Each level grants a {helper.FormatPercent(GameBalance.NecrosporulationEffectPerLevel)} chance to spawn a new cell at a random spot on the toast when a fungal cell dies.",
                flavorText: "Cytoplasmic apoptosis releases sporogenic factors for opportunistic rebirth.",
                type: MutationType.Necrosporulation,
                effectPerLevel: GameBalance.NecrosporulationEffectPerLevel,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3),
                maxLevel: GameBalance.NecrosporulationMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier3
            ), new MutationPrerequisite(MutationIds.ChronoresilientCytoplasm, 5));

            // Tier-5
            helper.MakeChild(new Mutation(
                id: MutationIds.NecrohyphalInfiltration,
                name: "Necrohyphal Infiltration",
                description:
                    $"Each level grants a {helper.FormatPercent(GameBalance.NecrohyphalInfiltrationChancePerLevel)} chance for living cells to invade an orthogonally adjacent dead enemy cell. " +
                    $"On success, each level also grants a {helper.FormatPercent(GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel)} chance to chain into another adjacent dead cell.",
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

            // Tier-6
            helper.MakeChild(new Mutation(
                id: MutationIds.CatabolicRebirth,
                name: "Catabolic Rebirth",
                description: $"When a toxin expires next to one of your dead cells, each level grants a {helper.FormatPercent(GameBalance.CatabolicRebirthResurrectionChancePerLevel)} chance to revive it as a living cell.\n" +
                             $"<b>Max Level Bonus:</b> Enemy toxins adjacent to your dead cells age twice as fast.",
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

            // Tier-7
            helper.MakeChild(new Mutation(
                id: MutationIds.HypersystemicRegeneration,
                name: "Hypersystemic Regeneration",
                description: $"Each level boosts Regenerative Hyphae effectiveness by {helper.FormatPercent(GameBalance.HypersystemicRegenerationEffectivenessBonus)} " +
                             $"and grants a {helper.FormatPercent(GameBalance.HypersystemicRegenerationResistanceChance)} chance for reclaimed cells to become resistant.\n" +
                             $"<b>Max Level Bonus:</b> Regenerative Hyphae can also reclaim diagonally adjacent cells.",
                flavorText: "The mycelium achieves ultimate regenerative mastery, orchestrating systemic cellular resurrection with enhanced defensive capabilities and expanded reach across the substrate matrix.",
                type: MutationType.HypersystemicRegeneration,
                effectPerLevel: GameBalance.HypersystemicRegenerationEffectivenessBonus,
                pointsPerUpgrade: GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier7),
                maxLevel: GameBalance.HypersystemicRegenerationMaxLevel,
                category: MutationCategory.CellularResilience,
                tier: MutationTier.Tier7
            ),
            new MutationPrerequisite(MutationIds.CatabolicRebirth, 1), // Tier 6 CellularResilience
            new MutationPrerequisite(MutationIds.MycotropicInduction, 1)); // Tier 3 Growth
        }
    }
}