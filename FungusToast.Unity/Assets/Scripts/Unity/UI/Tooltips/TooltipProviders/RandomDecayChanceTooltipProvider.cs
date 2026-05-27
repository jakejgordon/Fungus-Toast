using System.Text;
using UnityEngine;
using FungusToast.Core;
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
        public readonly struct RandomDecayChanceBreakdown
        {
            public RandomDecayChanceBreakdown(float baseChance, float roundModifier, float mycelialBloomModifier, float harmonyReduction, float effectiveChance)
            {
                BaseChance = baseChance;
                RoundModifier = roundModifier;
                MycelialBloomModifier = mycelialBloomModifier;
                HarmonyReduction = harmonyReduction;
                EffectiveChance = effectiveChance;
            }

            public float BaseChance { get; }
            public float RoundModifier { get; }
            public float MycelialBloomModifier { get; }
            public float HarmonyReduction { get; }
            public float EffectiveChance { get; }
        }

        private GameBoard board;
        private Player player; // perspective player

        public static RandomDecayChanceBreakdown BuildBreakdown(GameBoard board, Player player)
        {
            float baseChance = GameBalance.BaseRandomDecayChance;
            float roundModifier = board != null ? GameBalance.GetAdditionalRandomDecayChance(board.CurrentRound) : 0f;
            float mycelialBloomModifier = player != null
                ? player.GetMutationLevel(MutationIds.MycelialBloom) * GameBalance.MycelialBloomRandomDecayPenaltyPerLevel
                : 0f;
            float harmonyReduction = player != null ? player.GetMutationEffect(MutationType.DefenseSurvival) : 0f;
            float effectiveChance = Mathf.Max(0f, baseChance + roundModifier + mycelialBloomModifier - harmonyReduction);

            return new RandomDecayChanceBreakdown(baseChance, roundModifier, mycelialBloomModifier, harmonyReduction, effectiveChance);
        }

        public void Initialize(GameBoard gameBoard, Player perspectivePlayer)
        {
            board = gameBoard;
            player = perspectivePlayer;
        }

        public string GetTooltipText()
        {
            if (board == null) return "Random Decay Chance: (board not set)";

            int currentRound = board.CurrentRound;
            int scalingStart = GameBalance.RandomDecayScalingStartRound;
            float perRound = GameBalance.RandomDecayAdditionalChancePerRound; // fraction per round after start
            int bloomLevel = player != null ? player.GetMutationLevel(MutationIds.MycelialBloom) : 0;
            int harmonyLevel = player != null ? player.GetMutationLevel(MutationIds.HomeostaticHarmony) : 0;
            RandomDecayChanceBreakdown breakdown = BuildBreakdown(board, player);

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
            sb.AppendLine($"Base Chance: <b>{breakdown.BaseChance * 100f:0.###}%</b>");
            sb.AppendLine($"Round Modifier: <b>+{breakdown.RoundModifier * 100f:0.###}%</b>");
            sb.AppendLine($"Mycelial Bloom Modifier: <b>+{breakdown.MycelialBloomModifier * 100f:0.###}%</b> (Level {bloomLevel})");
            sb.AppendLine($"Homeostatic Harmony Reduction: <b>-{breakdown.HarmonyReduction * 100f:0.###}%</b> (Level {harmonyLevel})");
            sb.AppendLine($"Effective Chance: <b>{breakdown.EffectiveChance * 100f:0.###}%</b>");
            return sb.ToString();
        }
    }
}
