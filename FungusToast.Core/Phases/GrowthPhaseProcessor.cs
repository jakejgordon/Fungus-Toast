using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Core.Phases
{
    public class GrowthPhaseProcessor
    {
        private readonly GameBoard board;
        private readonly List<Player> players;
        private readonly Random rng;
        private readonly ISimulationObserver observer;

        public GrowthPhaseProcessor(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            this.board = board;
            this.players = players;
            this.rng = rng;
            this.observer = observer;
        }

        public Dictionary<int, int> ExecuteSingleCycle(RoundContext roundContext)
        {
            return GrowthEngine.ExecuteGrowthCycle(board, players, rng, roundContext, observer);
        }
    }
}
