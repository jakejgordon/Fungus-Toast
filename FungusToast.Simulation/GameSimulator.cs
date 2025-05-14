using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Simulation.GameSimulation.Models;

namespace FungusToast.Simulation.GameSimulation
{
    public class GameSimulator
    {
        public GameResult RunSimulation(List<IMutationSpendingStrategy> strategies, int seed)
        {
            var rng = new Random(seed);
            var (players, board) = InitializeGame(strategies, rng);

            var processor = new GrowthPhaseProcessor(board, players);
            var allMutations = MutationRegistry.GetAll().ToList();

            int turn = 0;
            bool gameEnded = false;
            bool isCountdownActive = false;
            int roundsRemainingUntilGameEnd = 0;

            while (turn < 100 && !gameEnded)
            {
                int occupied = board.GetAllCells().Count;
                int total = board.Width * board.Height;
                float ratio = (float)occupied / total;

                if (!isCountdownActive && ratio >= GameBalance.GameEndTileOccupancyThreshold)
                {
                    isCountdownActive = true;
                    roundsRemainingUntilGameEnd = GameBalance.TurnsAfterEndGameTileOccupancyThresholdMet;
                }
                else if (isCountdownActive)
                {
                    roundsRemainingUntilGameEnd--;
                    if (roundsRemainingUntilGameEnd <= 0)
                    {
                        gameEnded = true;
                        break;
                    }
                }

                // Mutation Phase
                foreach (var player in players)
                {
                    player.MutationPoints = player.GetMutationPointIncome() + player.GetBonusMutationPoints();
                    player.TryTriggerAutoUpgrade(allMutations);
                    player.MutationStrategy?.SpendMutationPoints(player, allMutations);
                }

                // Growth Phase
                for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
                    processor.ExecuteSingleCycle();

                // Decay Phase
                DeathEngine.ExecuteDeathCycle(board, players);
                turn++;
            }

            var result = GameResult.From(board, players, turn);

            // ✅ Summary output
            var winner = result.PlayerResults.First(p => p.PlayerId == result.WinnerId);
            Console.WriteLine($"Game complete (Turn {result.TurnsPlayed}) — Winner: Player {winner.PlayerId} ({winner.StrategyName})");
            foreach (var pr in result.PlayerResults.OrderBy(p => p.PlayerId))
            {
                Console.WriteLine($"  - Player {pr.PlayerId}: {pr.LivingCells} alive / {pr.DeadCells} dead ({pr.StrategyName})");
            }

            return result;
        }

        private (List<Player> players, GameBoard board) InitializeGame(List<IMutationSpendingStrategy> strategies, Random rng)
        {
            int playerCount = strategies.Count;
            var players = new List<Player>();

            for (int i = 0; i < playerCount; i++)
            {
                var player = new Player(
                    playerId: i,
                    playerName: $"AI {i + 1}",
                    playerType: PlayerTypeEnum.AI,
                    aiType: AITypeEnum.Random
                );
                player.SetMutationStrategy(strategies[i]);
                players.Add(player);
            }

            var board = new GameBoard(GameBalance.BoardWidth, GameBalance.BoardHeight, playerCount);

            var allTileIds = board.AllTiles()
                                  .Where(t => !t.IsOccupied)
                                  .Select(t => t.TileId)
                                  .OrderBy(_ => rng.Next())
                                  .ToList();

            for (int i = 0; i < playerCount && i < allTileIds.Count; i++)
            {
                int tileId = allTileIds[i];
                board.SpawnSporeForPlayer(players[i], tileId);
            }

            return (players, board);
        }
    }
}
