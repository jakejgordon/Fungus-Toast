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
                    new AdaptationDefinition("adaptation_5", "Adaptation 5", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_6", "Adaptation 6", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_7", "Adaptation 7", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_8", "Adaptation 8", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_9", "Adaptation 9", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_10", "Adaptation 10", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_11", "Adaptation 11", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_12", "Adaptation 12", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_13", "Adaptation 13", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_14", "Adaptation 14", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_15", "Adaptation 15", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_16", "Adaptation 16", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_17", "Adaptation 17", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_18", "Adaptation 18", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_19", "Adaptation 19", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_20", "Adaptation 20", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_21", "Adaptation 21", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_22", "Adaptation 22", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_23", "Adaptation 23", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_24", "Adaptation 24", "Placeholder adaptation with no gameplay effect yet."),
                    new AdaptationDefinition("adaptation_25", "Adaptation 25", "Placeholder adaptation with no gameplay effect yet.")
                });

        public static IReadOnlyList<AdaptationDefinition> All => all;

        public static bool TryGetById(string id, out AdaptationDefinition adaptation)
        {
            adaptation = all.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
            return adaptation != null;
        }
    }
}
