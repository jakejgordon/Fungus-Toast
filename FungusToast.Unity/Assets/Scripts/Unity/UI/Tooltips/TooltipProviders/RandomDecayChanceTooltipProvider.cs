using System.Text;
using UnityEngine;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Dynamic tooltip provider for the Random Decay Chance label.
    /// Displays scaling, Mycelial Bloom pressure, Homeostatic Harmony mitigation, and effective chance.
    /// </summary>
    public class RandomDecayChanceTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private GameBoard board;
        private Player player; // perspective player

        public void Initialize(GameBoard gameBoard, Player perspectivePlayer)
        {
            board = gameBoard;
            player = perspectivePlayer;
        }

        public string GetTooltipText()
        {
            if (board == null) return "Random Decay Chance: (board not set)";

            int currentRound = board.CurrentRound;
            float baseChance = GameBalance.BaseRandomDecayChance; // fraction
            int scalingStart = GameBalance.RandomDecayScalingStartRound;
            float perRound = GameBalance.RandomDecayAdditionalChancePerRound; // fraction per round after start

            float roundModifier = GameBalance.GetAdditionalRandomDecayChance(currentRound);

            int bloomLevel = player != null ? player.GetMutationLevel(MutationIds.MycelialBloom) : 0;
            float mycelialBloomModifier = bloomLevel * GameBalance.MycelialBloomRandomDecayPenaltyPerLevel;

            int harmonyLevel = player != null ? player.GetMutationLevel(MutationIds.HomeostaticHarmony) : 0;
            float harmonyReduction = harmonyLevel * GameBalance.HomeostaticHarmonyEffectPerLevel;

            float effective = baseChance + roundModifier + mycelialBloomModifier - harmonyReduction;
            if (effective < 0f) effective = 0f;

            var sb = new StringBuilder();
            string warningHex = ColorUtility.ToHtmlStringRGB(UIStyleTokens.State.Warning);
            sb.AppendLine($"<b><color=#{warningHex}>Random Decay Chance</color></b>");
            sb.AppendLine("Chance that a living, non-resistant cell dies randomly during each decay phase.");
            sb.AppendLine();

            if (currentRound < scalingStart)
            {
                int roundsUntil = scalingStart - currentRound;
                sb.AppendLine($"Scaling begins at round <b>{scalingStart}</b> (in {roundsUntil} round{(roundsUntil == 1 ? "" : "s")}). After that it increases by <b>{perRound * 100f:0.###}%</b> per round.");
            }
            else
            {
                sb.AppendLine($"Since round <b>{scalingStart}</b>, this chance increases by <b>{perRound * 100f:0.###}%</b> each round.");
            }

            sb.AppendLine("Increased by <b>Mycelial Bloom</b> and mitigated by <b>Homeostatic Harmony</b>.");
            sb.AppendLine();

            sb.AppendLine("<b>Current Values</b>:");
            sb.AppendLine($"Base Chance: <b>{baseChance * 100f:0.###}%</b>");
            sb.AppendLine($"Round Modifier: <b>+{roundModifier * 100f:0.###}%</b>");
            sb.AppendLine($"Mycelial Bloom Modifier: <b>+{mycelialBloomModifier * 100f:0.###}%</b> (Level {bloomLevel})");
            sb.AppendLine($"Homeostatic Harmony Reduction: <b>-{harmonyReduction * 100f:0.###}%</b> (Level {harmonyLevel})");
            sb.AppendLine($"Effective Chance: <b>{effective * 100f:0.###}%</b>");
            return sb.ToString();
        }
    }
}
