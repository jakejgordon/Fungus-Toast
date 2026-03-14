using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using FungusToast.Core.Config;

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
        private static readonly string retrogradeBloomLostLevels =
            AdaptationGameBalance.RetrogradeBloomTier1LevelsLost.ToString(CultureInfo.InvariantCulture);
        private static readonly string retrogradeBloomGainedLevels =
            AdaptationGameBalance.RetrogradeBloomTier5LevelsGained.ToString(CultureInfo.InvariantCulture);

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
                        "Your cells that die beside one of your resistant cells are consumed, leaving the tile empty instead of a corpse.",
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
                        "crustal_callus")
                });

        public static IReadOnlyList<AdaptationDefinition> All => all;

        public static bool TryGetById(string id, out AdaptationDefinition adaptation)
        {
            adaptation = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
            return adaptation != null;
        }
    }
}
