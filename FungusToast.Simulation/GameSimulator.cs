using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Common;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FungusToast.Simulation.GameSimulation
{
    public class GameSimulator
    {
        public static GameResult RunSimulation(
            List<IMutationSpendingStrategy> strategies,
            int seed,
            SimulationTrackingContext context,
            int gameIndex = -1,
            int totalGames = -1,
            DateTime? startTime = null,
            int boardWidth = GameBalance.BoardWidth,
            int boardHeight = GameBalance.BoardHeight,
            bool shuffleStartingSpores = true,
            bool enableNutrientPatches = true,
            bool enableMycovariantDraft = true,
            IReadOnlyCollection<int>? permanentlyBlockedTileIds = null,
            IReadOnlyList<(int x, int y)>? startingPositionOverride = null,
            IReadOnlyList<IReadOnlyList<string>>? startingAdaptationIds = null,
            IReadOnlyDictionary<int, (int x, int y)>? preferredPositionsByPlayerId = null
        )
        {
            var gameStopwatch = Stopwatch.StartNew();
            var randomStreams = new RandomStreamContract(seed);
            var rng = randomStreams.Gameplay;
            var (players, board) = InitializeGame(
                strategies,
                rng,
                context,
                boardWidth,
                boardHeight,
                shuffleStartingSpores,
                enableNutrientPatches,
                permanentlyBlockedTileIds,
                startingPositionOverride,
                startingAdaptationIds,
                preferredPositionsByPlayerId);
            var resolvedStartingPositions = players.ToDictionary(
                player => player.PlayerId,
                player =>
                {
                    var initialSpore = board.GetAllCellsOwnedBy(player.PlayerId)
                        .First(cell => (cell.SourceOfGrowth ?? GrowthSource.Unknown) == GrowthSource.InitialSpore);
                    return board.GetXYFromTileId(initialSpore.TileId);
                });
            var resolvedStartingAdaptations = players.ToDictionary(
                player => player.PlayerId,
                player => (IReadOnlyList<string>)player.PlayerAdaptations
                    .Select(adaptation => adaptation.Adaptation.Id)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList());
            var allMutations = MutationRegistry.GetAll().ToList();
            var allMycovariants = MycovariantRepository.All;
            var mycovariantPoolManager = new MycovariantPoolManager();
            mycovariantPoolManager.InitializePool(allMycovariants, rng);

            var simTracking = context ?? new SimulationTrackingContext();

            int mutationPhaseStartCount = 0;
            int preGrowthPhaseCount = 0;
            int preGrowthCycleCount = 0;
            int postGrowthPhaseCount = 0;
            int postGrowthPhaseCompletedCount = 0;
            int decayPhaseCount = 0;
            int postDecayPhaseCount = 0;

            board.MutationPhaseStart += () => mutationPhaseStartCount++;
            board.PreGrowthPhase += () => preGrowthPhaseCount++;
            board.PreGrowthCycle += () => preGrowthCycleCount++;
            board.PostGrowthPhase += () => postGrowthPhaseCount++;
            board.PostGrowthPhaseCompleted += () => postGrowthPhaseCompletedCount++;
            board.DecayPhase += _ => decayPhaseCount++;
            board.PostDecayPhase += () => postDecayPhaseCount++;

            bool gameEnded = false;
            string terminationReason = "round-cap";
            var eliminationRoundByPlayerId = new Dictionary<int, int>();
            bool isCountdownActive = false;
            int roundsRemainingUntilGameEnd = 0;

            while (board.CurrentRound < GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger && !gameEnded)
            {
                // Mycovariant Draft Phase
                if (enableMycovariantDraft)
                {
                    while (board.TryDequeuePendingHypervariationDraftPlayerId(out int playerId))
                    {
                        Player? draftPlayer = players.FirstOrDefault(player => player.PlayerId == playerId);
                        if (draftPlayer == null)
                        {
                            continue;
                        }

                        MycovariantDraftManager.RunDraft(
                            new List<Player> { draftPlayer },
                            mycovariantPoolManager,
                            board,
                            rng,
                            player => randomStreams.CreateAiDecisionRandom(
                                player.PlayerId,
                                board.CurrentRound,
                                "mycovariant-draft",
                                player.PlayerMycovariants.Count),
                            simTracking);
                    }
                }

                if (enableMycovariantDraft && MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(board.CurrentRound))
                {
                    MycovariantDraftManager.RunDraft(
                        players,
                        mycovariantPoolManager,
                        board,
                        rng,
                        player => randomStreams.CreateAiDecisionRandom(
                            player.PlayerId,
                            board.CurrentRound,
                            "mycovariant-draft",
                            player.PlayerMycovariants.Count),
                        simTracking);
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
                        terminationReason = "board-occupancy-countdown";
                        break;
                    }
                }

                RoundContext roundContext = new RoundContext();
                TurnEngine.AssignMutationPoints(
                    board,
                    players,
                    allMutations,
                    rng,
                    player => randomStreams.CreateAiDecisionRandom(
                        player.PlayerId,
                        board.CurrentRound,
                        "mutation-spending"),
                    simTracking);
                TurnEngine.RunGrowthPhase(board, players, rng, simTracking);
                TurnEngine.RunDecayPhase(board, players, simTracking.FailedGrowthsByPlayerId, rng, simTracking);

                foreach (var player in players)
                {
                    if (!eliminationRoundByPlayerId.ContainsKey(player.PlayerId)
                        && board.GetAllCellsOwnedBy(player.PlayerId).All(cell => !cell.IsAlive))
                    {
                        eliminationRoundByPlayerId[player.PlayerId] = board.CurrentRound;
                    }
                }

                // TICK DOWN ALL ACTIVE SURGES FOR ALL PLAYERS
                foreach (var player in players)
                    player.TickDownActiveSurges();
                board.SynchronizeChemobeaconsWithSurges(players);

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
            result.GameIndex = gameIndex > 0 ? gameIndex : 0;
            result.GameSeed = seed;
            gameStopwatch.Stop();
            result.RuntimeMilliseconds = gameStopwatch.Elapsed.TotalMilliseconds;
            result.TerminationReason = terminationReason;
            result.EliminationRoundByPlayerId = eliminationRoundByPlayerId;
            result.BoardWidth = boardWidth;
            result.BoardHeight = boardHeight;
            result.StartingPositionsByPlayerId = resolvedStartingPositions;
            result.StartingAdaptationIdsByPlayerId = resolvedStartingAdaptations;
            result.ParityInvariantReport = BuildParityInvariantReport(
                board,
                mutationPhaseStartCount,
                preGrowthPhaseCount,
                preGrowthCycleCount,
                postGrowthPhaseCount,
                postGrowthPhaseCompletedCount,
                decayPhaseCount,
                postDecayPhaseCount);

            if (gameIndex > 0 && totalGames > 0)
            {
                float percent = (float)gameIndex / totalGames * 100;
                string elapsed = startTime.HasValue
                    ? (DateTime.UtcNow - startTime.Value).ToString(@"hh\:mm\:ss")
                    : "??";
                string winnerInfo = FormatWinnerInfo(result);
                Console.WriteLine($"Game {gameIndex}/{totalGames} - Turn {result.TurnsPlayed} - {percent:0.00}% (Elapsed: {elapsed}) - {winnerInfo}");

            }
            else
            {
                Console.WriteLine($"Game complete (Turn {result.TurnsPlayed}) — {FormatWinnerInfo(result)}");

                foreach (var pr in result.PlayerResults.OrderBy(p => p.PlayerId))
                {
                    Console.WriteLine($"  - Player {pr.PlayerId}: {pr.LivingCells} alive / {pr.DeadCells} dead ({pr.StrategyName})");
                }
            }

            return result;
        }

        private static string FormatWinnerInfo(GameResult result)
        {
            var winners = result.PlayerResults
                .Where(player => result.IsWinningPlayer(player.PlayerId))
                .OrderBy(player => player.PlayerId)
                .ToList();
            if (winners.Count == 0) return "Winner: ?";
            if (winners.Count == 1) return $"Winner: Player {winners[0].PlayerId} ({winners[0].StrategyName})";
            return $"Co-winners: {string.Join(", ", winners.Select(player => $"Player {player.PlayerId} ({player.StrategyName})"))}";
        }

        private static ParityInvariantReport BuildParityInvariantReport(
            GameBoard board,
            int mutationPhaseStartCount,
            int preGrowthPhaseCount,
            int preGrowthCycleCount,
            int postGrowthPhaseCount,
            int postGrowthPhaseCompletedCount,
            int decayPhaseCount,
            int postDecayPhaseCount)
        {
            int completedRounds = Math.Max(0, board.CurrentRound - 1);
            int expectedGrowthCycles = completedRounds * GameBalance.TotalGrowthCycles;

            var checks = new List<InvariantCheckResult>
            {
                new() { Name = "MutationPhaseStart events", Expected = completedRounds, Actual = mutationPhaseStartCount },
                new() { Name = "PreGrowthPhase events", Expected = completedRounds, Actual = preGrowthPhaseCount },
                new() { Name = "PreGrowthCycle events", Expected = expectedGrowthCycles, Actual = preGrowthCycleCount },
                new() { Name = "PostGrowthPhase events", Expected = completedRounds, Actual = postGrowthPhaseCount },
                new() { Name = "PostGrowthPhaseCompleted events", Expected = completedRounds, Actual = postGrowthPhaseCompletedCount },
                new() { Name = "DecayPhase events", Expected = completedRounds, Actual = decayPhaseCount },
                new() { Name = "PostDecayPhase events", Expected = completedRounds, Actual = postDecayPhaseCount },
                new() { Name = "CurrentGrowthCycle counter", Expected = expectedGrowthCycles, Actual = board.CurrentGrowthCycle }
            };

            return new ParityInvariantReport
            {
                CompletedRounds = completedRounds,
                TotalGrowthCyclesPerRound = GameBalance.TotalGrowthCycles,
                Checks = checks
            };
        }



        private static (List<Player> players, GameBoard board) InitializeGame(
            List<IMutationSpendingStrategy> strategies,
            Random rng,
            ISimulationObserver observer,
            int boardWidth = GameBalance.BoardWidth,
            int boardHeight = GameBalance.BoardHeight,
            bool shuffleStartingSpores = true,
            bool enableNutrientPatches = true,
            IReadOnlyCollection<int>? permanentlyBlockedTileIds = null,
            IReadOnlyList<(int x, int y)>? startingPositionOverride = null,
            IReadOnlyList<IReadOnlyList<string>>? startingAdaptationIds = null,
            IReadOnlyDictionary<int, (int x, int y)>? preferredPositionsByPlayerId = null)
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

            var board = new GameBoard(boardWidth, boardHeight, playerCount, permanentlyBlockedTileIds);

            GameRulesEventSubscriber.SubscribeAll(board, players, rng, observer);
            AnalyticsEventSubscriber.Subscribe(board, observer);

            // Add each player to the board's Players list
            foreach (var player in players)
                board.Players.Add(player);

            // Use the shared starting spore placement utility
            var edgeOffsets = strategies
                .Select(strategy => strategy is ParameterizedSpendingStrategy parameterized ? parameterized.StartingSporeEdgeOffset : 0)
                .ToArray();
            StartingSporeUtility.PlaceStartingSpores(
                board,
                players,
                rng,
                shuffleStartingSpores,
                startingPositionOverride,
                edgeOffsets,
                preferredPositionsByPlayerId,
                enforceMinimumPlayableEdgeDistanceForPreferredPositions: false,
                ignoreMinimumPlayableEdgeDistancePlayerIds: null);
            if (enableNutrientPatches)
            {
                NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(board, players, rng, observer);
            }
            if (startingAdaptationIds != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (i >= startingAdaptationIds.Count) break;
                    foreach (var id in startingAdaptationIds[i])
                    {
                        if (AdaptationRepository.TryGetById(id, out var def))
                            players[i].TryAddAdaptation(def);
                        else
                            Console.WriteLine($"[Simulation] Warning: Unknown adaptation id '{id}' for player slot {i}. Skipping.");
                    }
                }
            }

            AdaptationEffectProcessor.OnStartingSporesEstablished(board, players, rng);

            return (players, board);
        }
    }
}
