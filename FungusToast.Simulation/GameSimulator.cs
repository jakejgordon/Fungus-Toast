using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core;

namespace FungusToast.Simulation.GameSimulation
{
    public class GameSimulator
    {
        public GameResult RunSimulation(List<IStrategy> strategies, int seed)
        {
            var rng = new Random(seed);
            var players = GameFactory.CreatePlayers(strategies.Count, strategies, rng);
            var board = GameFactory.CreateInitialBoard(players.Count);

            var processor = new GrowthPhaseProcessor(board, players);
            int turn = 0;

            while (!GameEndConditionMet(board) && turn < 100)
            {
                foreach (var player in players)
                {
                    player.MutationPoints = player.GetMutationPointIncome() + player.GetBonusMutationPoints();
                    player.TryTriggerAutoUpgrade(MutationManager.Instance.GetAllMutations().Values.ToList());
                    player.MutationStrategy?.SpendMutationPoints(player, MutationManager.Instance.GetAllMutations().Values.ToList());
                }

                for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
                    processor.ExecuteSingleCycle();

                DeathEngine.ExecuteDeathCycle(board, players);
                turn++;
            }

            return GameResult.From(board, players, turn);
        }

        private bool GameEndConditionMet(GameBoard board)
        {
            int total = board.Width * board.Height;
            int occupied = board.GetAllCells().Count;
            return (float)occupied / total >= GameBalance.GameEndTileOccupancyThreshold;
        }
    }
}
