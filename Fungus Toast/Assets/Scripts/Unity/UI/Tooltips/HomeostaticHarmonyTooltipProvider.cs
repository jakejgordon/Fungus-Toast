using System.Text;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using FungusToast.Core.Config;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Example dynamic tooltip provider for Homeostatic Harmony breakdown.
    /// Attach alongside TooltipTrigger and assign the player via Initialize().
    /// </summary>
    public class HomeostaticHarmonyTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private Player player;
        private System.Collections.Generic.List<Player> allPlayers;

        public void Initialize(Player tracked, System.Collections.Generic.List<Player> players)
        {
            player = tracked;
            allPlayers = players;
        }

        public string GetTooltipText()
        {
            if (player == null) return "Homeostatic Harmony: (player not set)";
            int level = player.GetMutationLevel(MutationIds.HomeostaticHarmony);
            float perLevel = GameBalance.HomeostaticHarmonyEffectPerLevel * 100f;
            float total = level * perLevel;
            float rawRandom = player.GetBaseMycelialDegradationRisk(allPlayers);
            float randomPercent = rawRandom * 100f;
            var sb = new StringBuilder();
            sb.AppendLine("<b><color=#88e0ff>Homeostatic Harmony</color></b>");
            sb.AppendLine($"Each level grants <b>{perLevel:0.###}%</b> reduced random decay chance.");
            sb.AppendLine($"Current Level: <b>{level}</b>");
            sb.AppendLine($"Total Reduction: <b>{total:0.###}%</b>");
            sb.AppendLine();
            sb.AppendLine($"Base Random Decay Risk Before Reduction: {randomPercent:0.###}%");
            sb.AppendLine($"Effective Random Decay Risk After Reduction: <b>{Mathf.Max(0f, randomPercent - total):0.###}%</b>");
            return sb.ToString();
        }
    }
}
