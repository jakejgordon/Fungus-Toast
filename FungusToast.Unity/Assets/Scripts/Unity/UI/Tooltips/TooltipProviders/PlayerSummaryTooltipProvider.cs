using System.Linq;
using System.Text;
using UnityEngine;
using FungusToast.Core.AI;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using FungusToast.Core.Common;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Tooltips;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Supplies tooltip content for a player's mold icon in the player summary row.
    /// Shows: Player Name, Highest Mutation (by tier desc then level desc), and Mycovariants list.
    /// </summary>
    public class PlayerSummaryTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private Player player;
        private System.Collections.Generic.List<Player> allPlayers;

        /// <summary>
        /// Initialize this provider with the player whose data to display.
        /// </summary>
        public void Initialize(Player targetPlayer, System.Collections.Generic.List<Player> players)
        {
            player = targetPlayer;
            allPlayers = players;
        }

        public string GetTooltipText()
        {
            if (player == null)
                return "Player: (unset)";

            var sb = new StringBuilder();
            var manager = GameManager.Instance;
            bool showStrategyName = manager != null && manager.IsTestingModeEnabled;
            var campaignAiProfile = GetVisibleCampaignAiProfile(player, manager);

            if (campaignAiProfile != null)
            {
                sb.AppendLine($"<b>Opponent:</b> {campaignAiProfile.FriendlyName}");
                sb.AppendLine($"<b>Strategy:</b> {campaignAiProfile.AIPlayerIntentions}");
                sb.AppendLine("<b>Campaign Unlock:</b> Strain Profiling");
            }
            else
            {
                sb.AppendLine($"<b>Player Name:</b> {player.PlayerName}");

                if (showStrategyName)
                    sb.AppendLine($"<b>Strategy:</b> {GetStrategyDisplayText(player)}");
            }

            // Highest Mutation: pick by highest tier then highest level
            string highestMutationText = GetHighestMutationText(player);
            sb.AppendLine(highestMutationText);

            // Mycovariants list (wrapping allowed by UI)
            string mycoList = GetMycovariantsList(player);
            sb.AppendLine($"<b>Mycovariants:</b> {mycoList}");

            // Adaptations list
            string adaptationList = GetAdaptationsList(player);
            sb.AppendLine($"<b>Adaptations:</b> {adaptationList}");

            return sb.ToString().TrimEnd();
        }

        private static string GetStrategyDisplayText(Player p)
        {
            if (p.PlayerType == PlayerTypeEnum.Human)
                return "Human Player";

            return string.IsNullOrWhiteSpace(p.MutationStrategy?.StrategyName)
                ? "Unassigned"
                : p.MutationStrategy.StrategyName;
        }

        private static StrategyCatalogEntry GetVisibleCampaignAiProfile(Player p, GameManager manager)
        {
            if (p == null || manager == null || manager.CurrentGameMode != GameMode.Campaign)
            {
                return null;
            }

            if (p.PlayerType == PlayerTypeEnum.Human)
            {
                return null;
            }

            var campaignController = manager.CampaignController;
            if (campaignController == null
                || !campaignController.HasUnlockedMoldinessReward(MoldinessUnlockCatalog.StrainProfilingRewardId))
            {
                return null;
            }

            var strategyName = p.MutationStrategy?.StrategyName;
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return null;
            }

            var entry = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, strategyName);
            if (entry == null
                || string.IsNullOrWhiteSpace(entry.FriendlyName)
                || string.IsNullOrWhiteSpace(entry.AIPlayerIntentions))
            {
                return null;
            }

            return entry;
        }

        private static string GetHighestMutationText(Player p)
        {
            if (p.PlayerMutations == null || p.PlayerMutations.Count == 0)
                return "<b>Highest Mutation:</b> None";

            // Select only mutations with level > 0
            var owned = p.PlayerMutations.Values
                .Where(pm => pm.CurrentLevel > 0)
                .Select(pm => new
                {
                    pm.Mutation.Tier,
                    TierNumber = pm.Mutation.TierNumber,
                    pm.Mutation.Name,
                    pm.CurrentLevel
                })
                .ToList();

            if (owned.Count == 0)
                return "<b>Highest Mutation:</b> None";

            var top = owned
                .OrderByDescending(x => x.TierNumber)
                .ThenByDescending(x => x.CurrentLevel)
                .First();

            return $"<b>Highest Mutation:</b> Tier {top.TierNumber} – {top.Name} (Lv {top.CurrentLevel})";
        }

        private static string GetMycovariantsList(Player p)
        {
            if (p.PlayerMycovariants == null || p.PlayerMycovariants.Count == 0)
                return "None";

            var names = p.PlayerMycovariants
                .Select(pm => pm.Mycovariant?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (names.Count == 0)
                return "None";

            return string.Join(", ", names);
        }

        private static string GetAdaptationsList(Player p)
        {
            if (p.PlayerAdaptations == null || p.PlayerAdaptations.Count == 0)
                return "None";

            var names = p.PlayerAdaptations
                .Select(pa => pa.Adaptation?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (names.Count == 0)
                return "None";

            return string.Join(", ", names);
        }
    }
}
