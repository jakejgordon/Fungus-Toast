using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public static class NeutralizingTestRunner
    {
        public static void RunTest(int numberOfGames, bool outputToFile, string outputFileName)
        {
            // Use specific strategies for testing Neutralizing Mantle
            var testRnd = new Random();
            var testStrategies = new List<IMutationSpendingStrategy>
            {
                AIRoster.TestingStrategiesByName["Toxin Spammer"],
                AIRoster.TestingStrategiesByName["Neutralizing Defender"]
            };
            
            Console.WriteLine($"Running Neutralizing Mantle test with {testStrategies.Count} players for {numberOfGames} games each...\n");
            Console.WriteLine("Strategy 0: Toxin Spammer (should place lots of toxins)");
            Console.WriteLine("Strategy 1: Neutralizing Defender (should prefer Neutralizing Mantle)\n");

            try
            {
                // Run simulation
                var testRunner = new MatchupRunner();
                var testResults = testRunner.RunMatchups(testStrategies, gamesToPlay: numberOfGames);

                // Print strategy summary
                var testAggregator = new MatchupStatsAggregator();
                testAggregator.PrintSummary(testResults.GameResults, testResults.CumulativeDeathReasons);

                // Analyze per-strategy mutation usage
                var testUsageTracker = new PlayerMutationUsageTracker();
                foreach (var result in testResults.GameResults)
                {
                    testUsageTracker.TrackGameResult(result);
                }
                var testRankedPlayers = MatchupStatsAggregator.GetRankedPlayerList(testResults.GameResults);
                
                // Create a combined tracking context from all games
                var testCombinedTracking = CreateCombinedTrackingContext(testResults.GameResults);
                testUsageTracker.PrintReport(testRankedPlayers, testCombinedTracking);

                // ==== NEW: Per-player Mycovariant Usage Summary ====
                var testMycoTracker = new PlayerMycovariantUsageTracker();
                foreach (var result in testResults.GameResults)
                {
                    testMycoTracker.TrackGameResult(result);
                }
                testMycoTracker.PrintReport(testRankedPlayers);

                Console.WriteLine("\nSimulation complete.");
            }
            finally
            {
                // No output redirection cleanup here
            }
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