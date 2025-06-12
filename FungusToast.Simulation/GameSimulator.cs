using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.GameSimulation
{
    public class GameSimulator
    {
        public GameResult RunSimulation(
            List<IMutationSpendingStrategy> strategies,
            int seed,
            int gameIndex = -1,
            int totalGames = -1,
            DateTime? startTime = null,
            SimulationTrackingContext? context = null
        )
        {
            var rng = new Random(seed);
            var (players, board) = InitializeGame(strategies, rng);
            var allMutations = MutationRegistry.GetAll().ToList();

            var simTracking = context ?? new SimulationTrackingContext();

            int turn = 0;
            bool gameEnded = false;
            bool isCountdownActive = false;
            int roundsRemainingUntilGameEnd = 0;

            while (turn < GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger && !gameEnded)
            {
                board.IncrementRound();

                if (!isCountdownActive && board.ShouldTriggerEndgame())
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

                TurnEngine.AssignMutationPoints(board, players, allMutations, rng, simTracking);
                MutationEffectProcessor.ApplyStartOfTurnEffects(board, players, rng);
                TurnEngine.RunGrowthPhase(board, players, rng, simTracking);
                TurnEngine.RunDecayPhase(board, players, simTracking.FailedGrowthsByPlayerId, simTracking, simTracking);

                // 🔥 TICK DOWN ALL ACTIVE SURGES FOR ALL PLAYERS
                foreach (var player in players)
                    player.TickDownActiveSurges();

                turn++;
            }

            // Track reclaimed cells per player
            foreach (var player in players)
            {
                int reclaims = board.CountReclaimedCellsByPlayer(player.PlayerId);
                simTracking.SetReclaims(player.PlayerId, reclaims);
            }

            var result = GameResult.From(board, players, turn, simTracking);

            if (gameIndex > 0 && totalGames > 0)
            {
                float percent = (float)gameIndex / totalGames * 100;
                string elapsed = startTime.HasValue
                    ? (DateTime.UtcNow - startTime.Value).ToString(@"hh\:mm\:ss")
                    : "??";

                Console.WriteLine($"Game {gameIndex}/{totalGames} complete — {percent:0.00}% (Elapsed: {elapsed})");
            }
            else
            {
                var winner = result.PlayerResults.First(p => p.PlayerId == result.WinnerId);
                Console.WriteLine($"Game complete (Turn {result.TurnsPlayed}) — Winner: Player {winner.PlayerId} ({winner.StrategyName})");

                foreach (var pr in result.PlayerResults.OrderBy(p => p.PlayerId))
                {
                    Console.WriteLine($"  - Player {pr.PlayerId}: {pr.LivingCells} alive / {pr.DeadCells} dead ({pr.StrategyName})");
                }
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
