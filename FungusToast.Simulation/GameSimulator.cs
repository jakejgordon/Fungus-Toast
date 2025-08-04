using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.GameSimulation
{
    public class GameSimulator
    {
        public GameResult RunSimulation(
            List<IMutationSpendingStrategy> strategies,
            int seed,
            SimulationTrackingContext context,
            int gameIndex = -1,
            int totalGames = -1,
            DateTime? startTime = null,
            int boardWidth = GameBalance.BoardWidth,
            int boardHeight = GameBalance.BoardHeight
        )
        {
            var rng = new Random(seed);
            var (players, board) = InitializeGame(strategies, rng, context, boardWidth, boardHeight);
            var allMutations = MutationRegistry.GetAll().ToList();
            var allMycovariants = MycovariantRepository.All;
            var mycovariantPoolManager = new MycovariantPoolManager();

            var simTracking = context ?? new SimulationTrackingContext();

            bool gameEnded = false;
            bool isCountdownActive = false;
            int roundsRemainingUntilGameEnd = 0;

            while (board.CurrentRound < GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger && !gameEnded)
            {
                // Mycovariant Draft Phase
                if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(board.CurrentRound))
                {
                    mycovariantPoolManager.InitializePool(allMycovariants, rng);
                    MycovariantDraftManager.RunDraft(players, mycovariantPoolManager, board, rng, simTracking);
                }

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

                RoundContext roundContext = new RoundContext();
                TurnEngine.AssignMutationPoints(board, players, allMutations, rng, simTracking);
                TurnEngine.RunGrowthPhase(board, players, rng, simTracking);
                TurnEngine.RunDecayPhase(board, players, simTracking.FailedGrowthsByPlayerId, rng, simTracking);

                // TICK DOWN ALL ACTIVE SURGES FOR ALL PLAYERS
                foreach (var player in players)
                    player.TickDownActiveSurges();

                // INCREMENT ROUND at end!
                board.IncrementRound();
            }

            // Track reclaimed cells per player
            foreach (var player in players)
            {
                int reclaims = board.CountReclaimedCellsByPlayer(player.PlayerId);
                simTracking.SetReclaims(player.PlayerId, reclaims);
            }

            // Record first-acquired rounds for each mutation per player
            simTracking.RecordFirstUpgradeRounds(players);

            var result = GameResult.From(board, players, board.CurrentRound, simTracking);

            if (gameIndex > 0 && totalGames > 0)
            {
                float percent = (float)gameIndex / totalGames * 100;
                string elapsed = startTime.HasValue
                    ? (DateTime.UtcNow - startTime.Value).ToString(@"hh\:mm\:ss")
                    : "??";
                var winner = result.PlayerResults.FirstOrDefault(p => p.PlayerId == result.WinnerId);
                string winnerInfo = winner != null
                    ? $"Winner: Player {winner.PlayerId} ({winner.StrategyName})"
                    : "Winner: ?";
                Console.WriteLine($"Game {gameIndex}/{totalGames} - Turn {result.TurnsPlayed} - {percent:0.00}% (Elapsed: {elapsed}) - {winnerInfo}");

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



        private (List<Player> players, GameBoard board) InitializeGame(List<IMutationSpendingStrategy> strategies, Random rng, ISimulationObserver observer, int boardWidth = GameBalance.BoardWidth, int boardHeight = GameBalance.BoardHeight)
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

            var board = new GameBoard(boardWidth, boardHeight, playerCount);

            GameRulesEventSubscriber.SubscribeAll(board, players, rng, observer);
            AnalyticsEventSubscriber.Subscribe(board, observer);

            // Add each player to the board's Players list
            foreach (var player in players)
                board.Players.Add(player);

            // Use the shared starting spore placement utility
            StartingSporeUtility.PlaceStartingSpores(board, players, rng);

            return (players, board);
        }
    }
}
