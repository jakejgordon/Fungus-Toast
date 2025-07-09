using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public abstract class MycovariantBase
    {
        public List<int> SynergyWith { get; set; } = new List<int>();
        public bool AIPrioritizeEarly { get; set; } = false;

        public float GetSynergyBonus(Player player)
        {
            int synergyCount = SynergyWith.Count(id => player.PlayerMycovariants.Any(pm => pm.MycovariantId == id));
            return synergyCount * MycovariantGameBalance.MycovariantSynergyBonus;
        }

        public float GetEarlyGameBonus(GameBoard board)
        {
            return AIPrioritizeEarly && board.CurrentRound < MycovariantAIGameBalance.EarlyGameRoundThreshold
                ? MycovariantAIGameBalance.EarlyGameAIScoreBonus
                : 0f;
        }

        public virtual float GetBaseAIScore(Player player, GameBoard board) => 0f;

        public virtual float GetAIScore(Player player, GameBoard board)
        {
            return GetBaseAIScore(player, board) + GetSynergyBonus(player) + GetEarlyGameBonus(board);
        }
    }
} 