using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Core.Metrics;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Phases
{
    public class GrowthPhaseProcessor
    {
        private readonly GameBoard board;
        private readonly List<Player> players;
        private readonly Random rng;
        private readonly IGrowthAndDecayObserver? observer;

        public GrowthPhaseProcessor(GameBoard board, List<Player> players, Random rng, IGrowthAndDecayObserver? observer = null)
        {
            this.board = board;
            this.players = players;
            this.rng = rng;
            this.observer = observer;
        }

        public Dictionary<int, int> ExecuteSingleCycle()
        {
            return GrowthEngine.ExecuteGrowthCycle(board, players, rng, observer);
        }
    }
}
