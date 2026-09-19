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
                        $"For the rest of the campaign, your toxins gain +{mycotoxicHaloPercent}% chance to kill orthogonally adjacent (up / down / left / right) enemy living cells during the Decay Phase. This stacks with Mycotoxin Potentiation.",
                        "mycotoxic_halo"),
                    new AdaptationDefinition(
                        "adaptation_4",
                        "Mycotoxic Lash",
                        $"For the rest of the campaign, each new toxin drop has a {mycotoxicLashPercent}% chance to instantly kill the first orthogonally adjacent (up / down / left / right) enemy living cell.",
                        "mycotoxic_lash"),
                    new AdaptationDefinition(
                        AdaptationIds.RetrogradeBloom,
                        "Retrograde Bloom",
                        $"At the start of round {AdaptationGameBalance.RetrogradeBloomTriggerRound}, {retrogradeBloomLostLevels} random Tier 1 mutation levels devolve and {retrogradeBloomGainedLevels} random Tier 5 mutation level evolves for free.",
                        "retrograde_bloom"),
                    new AdaptationDefinition(
                        AdaptationIds.AegisHyphae,
                        "Aegis Hyphae",
                        $"Each round, the first {AdaptationGameBalance.AegisHyphaeCellsPerRound} {(AdaptationGameBalance.AegisHyphaeCellsPerRound == 1 ? "cell" : "cells")} you grow become Resistant.",
                        "aegis_hyphae"),
                    new AdaptationDefinition(
                        AdaptationIds.SaprophageRing,
                        "Saprophage Ring",
                        "For the rest of the campaign, whenever one of your cells dies orthogonally adjacent (up / down / left / right) to one of your Resistant cells, it is cleared, leaving the tile empty instead of a dead cell.",
                        "saprophage_ring"),
                    new AdaptationDefinition(
                        AdaptationIds.MarginalClamp,
                        "Marginal Clamp",
                        "For the rest of the campaign, whenever one of your living cells grows orthogonally adjacent (up / down / left / right) to an enemy living cell or any toxin sitting on the board edge (the crust), those crust neighbors are cleared immediately. Enemy Resistant cells are unaffected.",
                        "marginal_clamp"),
                    new AdaptationDefinition(
                        AdaptationIds.ApicalYield,
                        "Apical Yield",
                        $"For the rest of the campaign, whenever one of your mutations reaches max level, gain {AdaptationGameBalance.ApicalYieldMutationPointAward} free mutation points.",
                        "apical_yield"),
                    new AdaptationDefinition(
                        AdaptationIds.CrustalCallus,
                        "Crustal Callus",
                        "For the rest of the campaign, whenever one of your living cells establishes itself on the board edge (the crust), it becomes Resistant.",
                        "crustal_callus"),
                    new AdaptationDefinition(
                        AdaptationIds.DistalSpore,
                        "Distal Spore",
                        $"At the start of round {AdaptationGameBalance.DistalSporeTriggerRound}, a Resistant cell arches from your starting spore into the corner of the toast most distant from it, replacing any non-Resistant occupant. If that corner holds a Resistant cell, it roots in the nearest non-Resistant tile to that corner instead.",
                        "distal_spore"),
                    new AdaptationDefinition(
                        AdaptationIds.AscusPrimacy,
                        "Ascus Primacy",
                        "For the rest of the campaign, you always draft first during Mycovariant drafting, regardless of how many living cells you control.",
                        "ascus_primacy"),
                    new AdaptationDefinition(
                        AdaptationIds.SporeSalvo,
                        "Spore Salvo",
                        "At the start of the game, your starting spore launches one toxin to toxify the nearest empty tile beside each enemy starting spore.",
                        "spore_salvo",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.HyphalBridge,
                        "Hyphal Bridge",
                        $"At the end of round {AdaptationGameBalance.HyphalBridgeTriggerRound}, {AdaptationGameBalance.HyphalBridgeCellCount} living cells drop in a straight line at equal intervals between your starting spore and the nearest enemy starting spore, replacing any non-Resistant occupant and skipping Resistant cells.",
                        "hyphal_bridge",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.VesicleBurst,
                        "Vesicle Burst",
                        $"For the rest of the campaign, each of your expired toxins has a {vesicleBurstPercent}% chance to pop, poisoning orthogonally adjacent (up / down / left / right) non-Resistant enemy living cells and spreading your toxin to adjacent empty tiles, dead cells, and enemy toxins.",
                        "vesicle_burst",
                        isLocked: true,
                        requiredMoldinessUnlockLevel: 1),
                    new AdaptationDefinition(
                        AdaptationIds.RhizomorphicHunger,
                        "Rhizomorphic Hunger",
                        $"For the rest of the campaign, orthogonal (up / down / left / right) growth attempts targeting a nutrient patch tile gain +{(int)(AdaptationGameBalance.RhizomorphicHungerGrowthBonus * 100)}% growth chance. When you claim a nutrient patch, its reward is calculated as if the patch were one tile larger than it actually is.",
                        "rhizomorphic_hunger"),
                    new AdaptationDefinition(
                        AdaptationIds.MycelialCrescendo,
                        "Mycelial Crescendo",
                        $"At the start of round {AdaptationGameBalance.MycelialCrescendoFirstTriggerRound}'s and round {AdaptationGameBalance.MycelialCrescendoSecondTriggerRound}'s Mutation Phase, a random inactive Mycelial Surge activates for free.",
                        "mycelial_crescendo"),
                    new AdaptationDefinition(
                        AdaptationIds.OssifiedAdvance,
                        "Ossified Advance",
                        $"For the rest of the campaign, each of your Resistant cells gains +{(int)(AdaptationGameBalance.OssifiedAdvanceOrthogonalBonus * 100)}% orthogonal growth chance (up / down / left / right).",
                        "ossified_advance"),
                    new AdaptationDefinition(
                        AdaptationIds.ConidiaAscent,
                        "Conidia Ascent",
                        $"At the start of round {AdaptationGameBalance.ConidiaAscentTriggerRound}, if you have a full 3x3 block of non-Resistant living cells and any completely empty 2x2 opening, that fragment breaks away: the 3x3 source block dies and a new 2x2 colony colonizes a random empty 2x2 opening.",
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
                        $"Whenever a Mycovariant draft ends and you drafted a Mycovariant, clear all enemy cells, dead cells, and toxins within {tropicLysisRadius} tiles (including diagonals) of your starting spore and your active Chemotactic Beacon marker, leaving those tiles empty. If no marker is active, clear only around your starting spore. Enemy Resistant cells are unaffected.",
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
                    // Starting adaptations - assigned by mold selection, never offered in mid-run drafts
                    new AdaptationDefinition(
                        AdaptationIds.ObliqueFilament,
                        "Oblique Filament",
                        $"Your hyphae trade -{obliqueFilamentOrthogonalPercent}% orthogonal growth chance (up / down / left / right) for +{obliqueFilamentDiagonalPercent}% diagonal growth chance in each direction.",
                        "oblique_filament",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.ThanatrophicRebound,
                        "Thanatrophic Rebound",
                        $"The first {AdaptationGameBalance.ThanatrophicReboundReclaimCount} times one of your living cells dies, it immediately reclaims itself as a Resistant cell.",
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
                        $"Your Tier 2 Mycelial Surges (Autolytic Surge, Necrotic Clearance, Chemotactic Beacon, and Chitin Fortification) cost {AdaptationGameBalance.SignalEconomyTier2SurgeCostReduction} fewer mutation {(AdaptationGameBalance.SignalEconomyTier2SurgeCostReduction == 1 ? "point" : "points")} to activate.",
                        "signal_economy",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.LiminalSporemeal,
                        "Liminal Sporemeal",
                        $"At the start of the game, a {AdaptationGameBalance.LiminalSporemealPatchSize}-tile Sporemeal Patch is placed near the board edge (the crust) closest to your starting spore.",
                        "liminal_sporemeal",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.PutrefactiveResilience,
                        "Putrefactive Resilience",
                        $"Your cells are {putrefactiveResiliencePercent}% less likely to be killed by Putrefactive Mycotoxin and Mycotoxin Potentiation.",
                        "putrefactive_resilience",
                        isStartingAdaptation: true),
                    new AdaptationDefinition(
                        AdaptationIds.CompoundReserve,
                        "Compound Reserve",
                        $"When you store {AdaptationGameBalance.CompoundReserveBankingThreshold} or more mutation points in a turn, gain {AdaptationGameBalance.CompoundReserveBonusPoints} additional mutation {(AdaptationGameBalance.CompoundReserveBonusPoints == 1 ? "point" : "points")}.",
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
                ? $"This stage's pulse triggered on round {triggerRound}, granting {triggerRound} mutation {pointLabel} at the start of that Mutation Phase."
                : $"This stage's pulse will trigger on round {triggerRound}, granting {triggerRound} mutation {pointLabel} at the start of that Mutation Phase. The trigger round and mutation points awarded are assigned at the start of each new campaign stage.";
        }

        public static bool TryGetById(string id, out AdaptationDefinition adaptation)
        {
            adaptation = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
            return adaptation != null;
        }
    }
}
