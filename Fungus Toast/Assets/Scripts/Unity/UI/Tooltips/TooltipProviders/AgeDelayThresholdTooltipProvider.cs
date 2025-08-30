using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Attach alongside TooltipTrigger and assign the player via Initialize().
    /// </summary>
    public class AgeDelayThresholdTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private Player player;

        public void Initialize(Player tracked)
        {
            player = tracked;
        }

        public string GetTooltipText()
        {
            if (player == null) return "Age Decay Threshold: player not set";
            int level = player.GetMutationLevel(MutationIds.ChronoresilientCytoplasm);
            float perLevel = GameBalance.ChronoresilientCytoplasmEffectPerLevel;
            float total = level * perLevel;
            float mutationEffect = player.GetMutationEffect(MutationType.DefenseSurvival);
            float baseThreshold = GameBalance.AgeAtWhichDecayChanceIncreases;
            int adjustedThreshold = (int)baseThreshold + (int)mutationEffect;
            var sb = new StringBuilder();
            sb.AppendLine("<b><color=#88e0ff>Chronoresilient Cytoplasm</color></b>");
            sb.AppendLine($"After reaching <b>{baseThreshold}</b> growth cycles, living cells gain a <b>{GameBalance.AgeDeathFactorPerGrowthCycle:0.###}%</b> "
                + $"decay chance per each additional growth cycle. Each level grants <b>{perLevel}</b> additional growth cycles before a cell is at risk of age-based decay.");
            sb.AppendLine("");
            sb.AppendLine($"Current Level: <b>{level}</b>");
            sb.AppendLine($"Additional Cycles: <b>{total}</b>");
            sb.AppendLine();
            sb.AppendLine($"Base Age Threshold: {baseThreshold}");
            sb.AppendLine($"Adjusted Age Threshold: <b>{adjustedThreshold}</b>");
            return sb.ToString();
        }
    }
}
