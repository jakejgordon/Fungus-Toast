using System;
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
        private readonly Random rng;

        public GrowthPhaseProcessor(GameBoard board, List<Player> players, Random rng)
        {
            this.board = board;
            this.players = players;
            this.rng = rng;
        }

        public void ExecuteSingleCycle()
        {
            GrowthEngine.ExecuteGrowthCycle(board, players, rng);
        }
    }
}
