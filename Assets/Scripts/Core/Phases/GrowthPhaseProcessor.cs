using System.Collections.Generic;
using FungusToast.Core.Players;
using FungusToast.Core.Growth;
using FungusToast.Core.Board;

namespace FungusToast.Core.Phases
{
    public class GrowthPhaseProcessor
    {
        private readonly GameBoard board;
        private readonly List<Player> players;

        public GrowthPhaseProcessor(GameBoard board, List<Player> players)
        {
            this.board = board;
            this.players = players;
        }

        public void ExecuteSingleCycle()
        {
            GrowthEngine.ExecuteGrowthCycle(board, players);
        }
    }
}
