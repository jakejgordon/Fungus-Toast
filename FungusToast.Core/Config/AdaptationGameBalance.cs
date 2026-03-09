using System.Globalization;

namespace FungusToast.Core.Config
{
    public static class AdaptationGameBalance
    {
        public const int ConidialRelayTriggerRound = 10;
        public const int HyphalEconomySurgeCostReduction = 1;
        public const float MycotoxicHaloOrthogonalKillChanceBonus = 0.02f;

        public static string GetConidialRelayDescription()
        {
            return $"At the end of round {ConidialRelayTriggerRound}, your starting spore takes flight and lands on a random unoccupied tile.";
        }

        public static string GetHyphalEconomyDescription()
        {
            string pointLabel = HyphalEconomySurgeCostReduction == 1 ? "point" : "points";
            return $"For the rest of the campaign, your Mycelial Surges cost {HyphalEconomySurgeCostReduction} fewer mutation {pointLabel} to activate.";
        }

        public static string GetMycotoxicHaloDescription()
        {
            string percent = (MycotoxicHaloOrthogonalKillChanceBonus * 100f).ToString("0.0", CultureInfo.InvariantCulture);
            return $"For the rest of the campaign, your toxins gain +{percent}% chance to kill orthogonally adjacent living cells during decay. This stacks with Mycotoxin Potentiation.";
        }
    }
}