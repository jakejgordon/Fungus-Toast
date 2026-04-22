using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using FungusToast.Core.Config;
using FungusToast.Core.Players;

namespace FungusToast.Core.Campaign
{
    /// <summary>
    /// Static adaptation catalog for campaign progression rewards.
    /// </summary>
    public static class AdaptationRepository
    {
        private static readonly string mycotoxicHaloPercent =
            (AdaptationGameBalance.MycotoxicHaloOrthogonalKillChanceBonus * 100f).ToString("0.0", CultureInfo.InvariantCulture);
        private static readonly string mycotoxicLashPercent =
            (AdaptationGameBalance.MycotoxicLashToxinDropKillChance * 100f).ToString("0.0", CultureInfo.InvariantCulture);
        private static readonly string vesicleBurstPercent =
            (AdaptationGameBalance.VesicleBurstExpiredToxinPopChance * 100f).ToString("0.0", CultureInfo.InvariantCulture);
        private static readonly string retrogradeBloomLostLevels =
            AdaptationGameBalance.RetrogradeBloomTier1LevelsLost.ToString(CultureInfo.InvariantCulture);
        private static readonly string retrogradeBloomGainedLevels =
            AdaptationGameBalance.RetrogradeBloomTier5LevelsGained.ToString(CultureInfo.InvariantCulture);
        private static readonly string hyphalPrimingLevelsGranted =
            AdaptationGameBalance.HyphalPrimingLevelsGranted.ToString(CultureInfo.InvariantCulture);
        private static readonly string tropicLysisRadius =
            AdaptationGameBalance.TropicLysisRadius.ToString(CultureInfo.InvariantCulture);

        // Starting adaptation computed description strings
        private static readonly string obliqueFilamentOrthogonalPercent =
            (AdaptationGameBalance.ObliqueFilamentOrthogonalPenalty * 100f).ToString("0.0", CultureInfo.InvariantCulture);
        private static readonly string obliqueFilamentDiagonalPercent =
            (AdaptationGameBalance.ObliqueFilamentDiagonalBonus * 100f).ToString("0.00", CultureInfo.InvariantCulture);
        private static readonly int centripetalShiftTiles =
            (int)Math.Ceiling(GameBalance.BoardWidth * AdaptationGameBalance.CentripetalGerminationShiftFactor);
        private static readonly string putrefactiveResiliencePercent =
            (AdaptationGameBalance.PutrefactiveResilienceKillChanceReduction * 100f).ToString("0", CultureInfo.InvariantCulture);

        private static readonly ReadOnlyCollection<AdaptationDefinition> all =
            new ReadOnlyCollection<AdaptationDefinition>(
                new List<AdaptationDefinition>
                {
                    new AdaptationDefinition(
                        "adaptation_1",
                        "Conidial Relay",
                        $"At the end of round {AdaptationGameBalance.ConidialRelayTriggerRound}, your starting spore takes flight and lands on a random unoccupied tile.",
                        "conidial_relay"),
                    new AdaptationDefinition(
                        "adaptation_2",
                        "Hyphal Economy",
                        $"For the rest of the campaign, your Mycelial Surges cost {AdaptationGameBalance.HyphalEconomySurgeCostReduction} fewer mutation {(AdaptationGameBalance.HyphalEconomySurgeCostReduction == 1 ? "point" : "points")} to activate.",
                        "hyphal_economy"),
                    new AdaptationDefinition(
                        "adaptation_3",
                        "Mycotoxic Halo",
                        $"For the rest of the campaign, your toxins gain +{mycotoxicHaloPercent}% chance to kill orthogonally adjacent living cells during decay. This stacks with Mycotoxin Potentiation.",
                        "mycotoxic_halo"),
                    new AdaptationDefinition(
                        "adaptation_4",
                        "Mycotoxic Lash",
                        $"For the rest of the campaign, each new toxin drop has a {mycotoxicLashPercent}% chance to instantly kill the first orthogonally adjacent enemy living cell.",
                        "mycotoxic_lash"),
                    new AdaptationDefinition(
                        AdaptationIds.RetrogradeBloom,
                        "Retrograde Bloom",
                        $"At the start of round {AdaptationGameBalance.RetrogradeBloomTriggerRound}, {retrogradeBloomLostLevels} random Tier 1 mutation levels devolve and {retrogradeBloomGainedLevels} random Tier 5 mutation level evolves for free.",
                        "retrograde_bloom"),
                    new AdaptationDefinition(
                        AdaptationIds.AegisHyphae,
                        "Aegis Hyphae",
                        $"Each round, the first {AdaptationGameBalance.AegisHyphaeCellsPerRound} {(AdaptationGameBalance.AegisHyphaeCellsPerRound == 1 ? "cell" : "cells")} you grow gain Resistance.",
                        "aegis_hyphae"),
                    new AdaptationDefinition(
                        AdaptationIds.SaprophageRing,
                        "Saprophage Ring",
                        "Your cells that die beside one of your resistant cells are consumed, leaving the tile empty instead of a dead cell.",
                        "saprophage_ring"),
                    new AdaptationDefinition(
                        AdaptationIds.MarginalClamp,
                        "Marginal Clamp",
                        "For the rest of the campaign, whenever one of your living cells grows beside an enemy living cell or any toxin on the crust, those border threats are cleared immediately. Resistant enemy cells still survive.",
                        "marginal_clamp"),
                    new AdaptationDefinition(
                        AdaptationIds.ApicalYield,
                        "Apical Yield",
                        $"For the rest of the campaign, whenever one of your mutations reaches max level, gain {AdaptationGameBalance.ApicalYieldMutationPointAward} free mutation points.",
                        "apical_yield"),
                    new AdaptationDefinition(
                        AdaptationIds.CrustalCallus,
                        "Crustal Callus",
                        "For the rest of the campaign, whenever one of your living cells establishes itself on the board edge (the crust), it gains Resistance.",
                        "crustal_callus"),
                    new AdaptationDefinition(
                        AdaptationIds.DistalSpore,
                        "Distal Spore",
                        $"At the start of round {AdaptationGameBalance.DistalSporeTriggerRound}, a resistant cell arches from your starting spore into the corner of the toast most distant from it. It replaces any non-resistant occupant. If that corner holds a resistant cell, it roots in the nearest non-resistant tile to that corner instead.",
                        "distal_spore"),
                    new AdaptationDefinition(
                        AdaptationIds.AscusPrimacy,
                        "Ascus Primacy",
                        "For the rest of the campaign, you always draft first during Mycovariant drafting, regardless of how many living cells you control.",
                        "ascus_primacy"),
                    new AdaptationDefinition(
                        AdaptationIds.SporeSalvo,
                        "Spore Salvo",
                        "At the start of the game, your starting spore launches one toxin into the nearest open tile beside each enemy starting spore.",
                        "spore_salvo",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.HyphalBridge,
                        "Hyphal Bridge",
                        $"At the end of round {AdaptationGameBalance.HyphalBridgeTriggerRound}, 4 living cells drop in a straight line at equal intervals between your starting cell and the nearest enemy starting cell. They replace any non-resistant occupant and skip resistant cells.",
                        "hyphal_bridge",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.VesicleBurst,
                        "Vesicle Burst",
                        $"For the rest of the campaign, each of your expired toxins has a {vesicleBurstPercent}% chance to pop and drop friendly toxins into every orthogonally adjacent tile that is empty or occupied by a non-resistant enemy cell, dead cell, or toxin.",
                        "vesicle_burst",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.RhizomorphicHunger,
                        "Rhizomorphic Hunger",
                        $"Your colony hunts nutrients with predatory efficiency. Orthogonal growth attempts targeting a nutrient patch tile gain +{(int)(AdaptationGameBalance.RhizomorphicHungerGrowthBonus * 100)}% growth chance. When you claim a nutrient patch, its reward is calculated as if the patch were one tile larger than it actually is.",
                        "rhizomorphic_hunger"),
                    new AdaptationDefinition(
                        AdaptationIds.MycelialCrescendo,
                        "Mycelial Crescendo",
                        $"At round {AdaptationGameBalance.MycelialCrescendoFirstTriggerRound} and round {AdaptationGameBalance.MycelialCrescendoSecondTriggerRound}, your colony erupts with unsolicited evolutionary pressure — a random inactive Mycelial Surge activates for free.",
                        "mycelial_crescendo"),
                    new AdaptationDefinition(
                        AdaptationIds.OssifiedAdvance,
                        "Ossified Advance",
                        $"For the rest of the campaign, each of your resistant cells gains +{(int)(AdaptationGameBalance.OssifiedAdvanceOrthogonalBonus * 100)}% orthogonal growth chance.",
                        "ossified_advance"),
                    new AdaptationDefinition(
                        AdaptationIds.ConidiaAscent,
                        "Conidia Ascent",
                        $"At the start of round {AdaptationGameBalance.ConidiaAscentTriggerRound}, if you have a full 3x3 block of killable living cells and any completely empty 2x2 opening, that colony fragment blasts away. The 3x3 source block dies and a new 2x2 colony roots in a random open patch.",
                        "conidia_ascent"),
                    new AdaptationDefinition(
                        AdaptationIds.HyphalPriming,
                        "Hyphal Priming",
                        $"At the start of round {AdaptationGameBalance.HyphalPrimingTriggerRound}'s Mutation Phase, a random Tier 2 mutation outside Mycelial Surges gains {hyphalPrimingLevelsGranted} free levels. Prerequisites are ignored.",
                        "hyphal_priming",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.TropicLysis,
                        "Tropic Lysis",
                        $"Whenever a Mycovariant draft ends and you drafted a Mycovariant, clear all enemy cells, dead cells, and toxins within {tropicLysisRadius} tiles of your starting spore and active Chemotactic Beacon, leaving those tiles empty. If no beacon is active, clear only around your starting spore; resistant enemy living cells survive.",
                        "tropic_lysis",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.PrimePulse,
                        "Prime Pulse",
                        $"At the start of each game, roll one equal-chance payout: {AdaptationGameBalance.PrimePulseFirstTriggerRound} mutation points on round {AdaptationGameBalance.PrimePulseFirstTriggerRound}, {AdaptationGameBalance.PrimePulseSecondTriggerRound} mutation points on round {AdaptationGameBalance.PrimePulseSecondTriggerRound}, or {AdaptationGameBalance.PrimePulseThirdTriggerRound} mutation points on round {AdaptationGameBalance.PrimePulseThirdTriggerRound}. Gain it at the start of that round's Mutation Phase.",
                        "prime_pulse",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.HyphalEcho,
                        "Hyphal Echo",
                        $"For the rest of the campaign, your Mycelial Surges last {AdaptationGameBalance.HyphalEchoSurgeDurationBonus} additional round{(AdaptationGameBalance.HyphalEchoSurgeDurationBonus == 1 ? string.Empty : "s")}.",
                        "hyphal_echo",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 32),
                    // Starting adaptations — assigned by mold selection, never offered in mid-run drafts
                    new AdaptationDefinition(
                        AdaptationIds.ObliqueFilament,
                        "Oblique Filament",
                        $"Your hyphae trade -{obliqueFilamentOrthogonalPercent}% orthogonal growth chance for +{obliqueFilamentDiagonalPercent}% diagonal growth chance in each direction.",
                        "oblique_filament",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.ThanatrophicRebound,
                        "Thanatrophic Rebound",
                        "The first time one of your living cells dies, it immediately reclaims itself as a resistant cell.",
                        "thanatrophic_rebound",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.ToxinPrimacy,
                        "Toxin Primacy",
                        $"Your colony starts the game with Mycotoxin Tracer already at level {AdaptationGameBalance.ToxinPrimacyStartingLevel}.",
                        "toxin_primacy",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.CentripetalGermination,
                        "Centripetal Germination",
                        $"Your starting spore is placed {centripetalShiftTiles} tiles closer to the center of the board.",
                        "centripetal_germination",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.SignalEconomy,
                        "Signal Economy",
                        "Your Chemotactic Beacon surge costs 1 fewer mutation point to activate.",
                        "signal_economy",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.LiminalSporemeal,
                        $"Liminal Sporemeal",
                        $"At the start of the game, a {AdaptationGameBalance.LiminalSporemealPatchSize}-tile Sporemeal nutrient patch is placed near the board edge closest to your starting spore.",
                        "liminal_sporemeal",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.PutrefactiveResilience,
                        "Putrefactive Resilience",
                        $"Your cells have -{putrefactiveResiliencePercent}% reduced chance of being killed by Putrefactive Mycotoxins and Mycotoxin Potentiation.",
                        "putrefactive_resilience",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.CompoundReserve,
                        "Compound Reserve",
                        $"When you store {AdaptationGameBalance.CompoundReserveBankingThreshold} or more mutation points in a turn, gain {AdaptationGameBalance.CompoundReserveBonusPoints} additional mutation point.",
                        "compound_reserve",
                        isStartingAdaptation: true),
                });

        public static IReadOnlyList<AdaptationDefinition> All => all;

        public static int GetCentripetalGerminationShiftTiles(int boardWidth)
        {
            int safeBoardWidth = Math.Max(1, boardWidth);
            return (int)Math.Ceiling(safeBoardWidth * AdaptationGameBalance.CentripetalGerminationShiftFactor);
        }

        public static string GetTooltipDescription(AdaptationDefinition adaptation, int boardWidth)
        {
            if (adaptation == null)
            {
                return string.Empty;
            }

            if (string.Equals(adaptation.Id, AdaptationIds.CentripetalGermination, StringComparison.Ordinal))
            {
                int shiftTiles = GetCentripetalGerminationShiftTiles(boardWidth);
                string tileLabel = shiftTiles == 1 ? "tile" : "tiles";
                return $"Your starting spore is placed {shiftTiles} {tileLabel} closer to the center of the board.";
            }

            return adaptation.Description;
        }

        public static string GetTooltipDescription(PlayerAdaptation playerAdaptation, int boardWidth)
        {
            if (playerAdaptation?.Adaptation == null)
            {
                return string.Empty;
            }

            if (string.Equals(playerAdaptation.Adaptation.Id, AdaptationIds.PrimePulse, StringComparison.Ordinal))
            {
                return GetPrimePulseTooltipDescription(playerAdaptation);
            }

            return GetTooltipDescription(playerAdaptation.Adaptation, boardWidth);
        }

        private static string GetPrimePulseTooltipDescription(PlayerAdaptation playerAdaptation)
        {
            if (!playerAdaptation.HasRuntimeValue)
            {
                return playerAdaptation.Adaptation.Description;
            }

            int triggerRound = playerAdaptation.RuntimeValue;
            string pointLabel = triggerRound == 1 ? "point" : "points";
            return playerAdaptation.HasTriggered
                ? $"This level's pulse triggered on round {triggerRound}, granting {triggerRound} mutation {pointLabel} at the start of that Mutation Phase."
                : $"This level's pulse will trigger on round {triggerRound}, granting {triggerRound} mutation {pointLabel} at the start of that Mutation Phase. The trigger round and mutation points awarded are assigned at the start of each new campaign level.";
        }

        public static bool TryGetById(string id, out AdaptationDefinition adaptation)
        {
            adaptation = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
            return adaptation != null;
        }
    }
}
