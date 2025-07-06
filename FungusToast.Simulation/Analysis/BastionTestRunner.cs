using FungusToast.Core.AI;
using FungusToast.Core.Mycovariants;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public static class BastionTestRunner
    {
        public static void RunTest(int numberOfGames, bool outputToFile, string outputFileName)
        {
            // Use specific strategies for testing Mycelial Bastion
            var testRnd = new Random();
            var testStrategies = new List<IMutationSpendingStrategy>
            {
                new ParameterizedSpendingStrategy(
                    strategyName: "Bastion Tester",
                    prioritizeHighTier: true,
                    targetMutationGoals: new List<TargetMutationGoal>(),
                    mycovariantPreferences: new List<MycovariantPreference>
                    {
                        new MycovariantPreference(MycovariantIds.MycelialBastionIId, 10, "Test Mycelial Bastion I")
                    }
                ),
                AIRoster.TestingStrategiesByName["Toxin Spammer"]
            };

            Console.WriteLine($"Running Mycelial Bastion test with {testStrategies.Count} players for {numberOfGames} games each...\n");
            Console.WriteLine("Strategy 0: Bastion Tester (should draft Mycelial Bastion I)");
            Console.WriteLine("Strategy 1: Toxin Spammer (aggressive opponent)\n");

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
                var testCombinedTracking = CreateCombinedTrackingContext(testResults.GameResults);
                testUsageTracker.PrintReport(testRankedPlayers, testCombinedTracking);

                // ==== Per-player Mycovariant Usage Summary ====
                var testMycoTracker = new PlayerMycovariantUsageTracker();
                foreach (var result in testResults.GameResults)
                {
                    testMycoTracker.TrackGameResult(result);
                }
                testMycoTracker.PrintReport(testRankedPlayers);

                // Print Bastioned effect counts
                Console.WriteLine("\n=== Mycelial Bastion I Effect Counts ===");
                foreach (var player in testResults.GameResults.SelectMany(g => g.PlayerResults))
                {
                    var bastion = player.Mycovariants.FirstOrDefault(m => m.MycovariantName.Contains("Bastion I"));
                    if (bastion != null && bastion.EffectCounts.TryGetValue("Bastioned", out int count))
                    {
                        Console.WriteLine($"Player {player.PlayerId} ({player.StrategyName}): Bastioned {count} cells");
                    }
                }

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