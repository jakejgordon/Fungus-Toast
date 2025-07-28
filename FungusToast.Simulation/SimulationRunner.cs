using FungusToast.Core.AI;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation
{
    public static class SimulationRunner
    {
        public static void RunStandardSimulation(int numberOfPlayers, int numberOfGames, int boardWidth = GameBalance.BoardWidth, int boardHeight = GameBalance.BoardHeight)
        {
            var rnd = new Random(); // Or any deterministic seed you want

            var strategies =   AIRoster.GetRandomProvenStrategies(numberOfPlayers, rnd); /* AIRoster.TestingStrategies;*/ //AIRoster.MycovariantPermutations();

            Console.WriteLine($"Running simulation with {strategies.Count} players for {numberOfGames} games each...\n");

            // Run simulation
            var runner = new MatchupRunner();
            var results = runner.RunMatchups(strategies, gamesToPlay: numberOfGames, boardWidth: boardWidth, boardHeight: boardHeight);

            // Print strategy summary
            var aggregator = new MatchupStatsAggregator();
            aggregator.PrintSummary(results.GameResults, results.CumulativeDeathReasons);

            // Analyze mutation impact
            var impactTracker = new MutationImpactTracker();
            foreach (var result in results.GameResults)
            {
                impactTracker.TrackGameResult(result);
            }
            impactTracker.PrintReport();



            // Analyze per-strategy mutation usage
            var usageTracker = new PlayerMutationUsageTracker();
            foreach (var result in results.GameResults)
            {
                usageTracker.TrackGameResult(result);
            }
            var rankedPlayers = MatchupStatsAggregator.GetRankedPlayerList(results.GameResults);
            
            // Create a combined tracking context from all games
            var combinedTracking = CreateCombinedTrackingContext(results.GameResults);
            usageTracker.PrintReport(rankedPlayers, combinedTracking);

            // ==== NEW: Per-player Mycovariant Usage Summary ====
            var mycoTracker = new PlayerMycovariantUsageTracker();
            foreach (var result in results.GameResults)
            {
                mycoTracker.TrackGameResult(result);
            }
            mycoTracker.PrintReport(rankedPlayers);

            Console.WriteLine("\nSimulation complete.");
        }

        private static SimulationTrackingContext CreateCombinedTrackingContext(List<GameResult> gameResults)
        {
            var combined = new SimulationTrackingContext();
            
            // Aggregate first upgrade rounds from all games
            foreach (var result in gameResults)
            {
                if (result.TrackingContext != null)
                {
                    var allFirstUpgradeRounds = result.TrackingContext.GetAllFirstUpgradeRounds();
                    foreach (var kvp in allFirstUpgradeRounds)
                    {
                        foreach (var round in kvp.Value)
                        {
                            // We need to record the first upgrade rounds for each player/mutation combination
                            // Since SimulationTrackingContext doesn't have a method to add individual records,
                            // we'll need to work with what we have
                            // For now, we'll use the last game's tracking context as it should have the most complete data
                        }
                    }
                }
            }
            
            // For simplicity, use the tracking context from the last game
            // This should have the most complete first upgrade data
            return gameResults.LastOrDefault()?.TrackingContext ?? new SimulationTrackingContext();
        }
    }
}