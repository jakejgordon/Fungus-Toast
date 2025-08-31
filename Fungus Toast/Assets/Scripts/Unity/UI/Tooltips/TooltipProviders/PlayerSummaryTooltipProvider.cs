using System.Linq;
using System.Text;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
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

            // Player Name
            sb.AppendLine($"<b>Player Name:</b> {player.PlayerName}");

            // Highest Mutation: pick by highest tier then highest level
            string highestMutationText = GetHighestMutationText(player);
            sb.AppendLine(highestMutationText);

            // Mycovariants list (wrapping allowed by UI)
            string mycoList = GetMycovariantsList(player);
            sb.AppendLine($"<b>Mycovariants:</b> {mycoList}");

            return sb.ToString().TrimEnd();
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
    }
}
