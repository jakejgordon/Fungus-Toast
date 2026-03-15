using FungusToast.Core.AI;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Simulation.Export;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation
{
    public static class SimulationRunner
    {
        public static void RunStandardSimulation(
            int numberOfPlayers,
            int numberOfGames,
            List<IMutationSpendingStrategy>? strategies = null,
            int boardWidth = GameBalance.BoardWidth,
            int boardHeight = GameBalance.BoardHeight,
            bool enableKeyboardInterrupt = true,
            int? baseSeed = null,
            StrategySetEnum strategySet = StrategySetEnum.Testing,
            SlotAssignmentPolicy slotAssignmentPolicy = SlotAssignmentPolicy.Fixed,
            SimulationRunMetadata? runMetadata = null,
            bool exportParquet = true)
        {
            // Use TestingStrategies as default if none provided
            strategies ??= AIRoster.TestingStrategies;

            var effectiveSeed = baseSeed ?? 0;

            Console.WriteLine($"Running simulation with {strategies.Count} players for {numberOfGames} games each...\n");
            Console.WriteLine($"Strategy Set: {strategySet} | Base Seed: {effectiveSeed} | Slot Policy: {slotAssignmentPolicy}\n");

            // Run simulation
            var results = MatchupRunner.RunMatchups(
                strategies,
                gamesToPlay: numberOfGames,
                boardWidth: boardWidth,
                boardHeight: boardHeight,
                enableKeyboardInterrupt: enableKeyboardInterrupt,
                baseSeed: effectiveSeed,
                slotAssignmentPolicy: slotAssignmentPolicy);

            PrintParityInvariantSummary(results.GameResults);

            // Print strategy summary
            MatchupStatsAggregator.PrintSummary(results.GameResults, results.CumulativeDeathReasons);

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

            if (exportParquet && runMetadata != null)
            {
                var exportFolder = SimulationParquetExporter.Export(results, runMetadata);
                Console.WriteLine($"Parquet export complete: {exportFolder}");
            }

            Console.WriteLine("\nSimulation complete.");
        }

        private static void PrintParityInvariantSummary(List<GameResult> gameResults)
        {
            if (gameResults.Count == 0)
                return;

            int failedGames = 0;
            var failedDetails = new List<string>();

            for (int i = 0; i < gameResults.Count; i++)
            {
                var report = gameResults[i].ParityInvariantReport;
                if (report == null || report.AllPassed)
                    continue;

                failedGames++;
                var mismatches = report.Checks
                    .Where(c => !c.IsMatch)
                    .Select(c => $"{c.Name} expected={c.Expected}, actual={c.Actual}")
                    .ToList();
                failedDetails.Add($"Game {i + 1}: {string.Join("; ", mismatches)}");
            }

            Console.WriteLine();
            Console.WriteLine("=== Parity Invariant Summary ===");
            Console.WriteLine($"Games evaluated: {gameResults.Count}");
            Console.WriteLine($"Games with invariant mismatches: {failedGames}");

            foreach (var detail in failedDetails.Take(5))
            {
                Console.WriteLine($"- {detail}");
            }

            if (failedDetails.Count > 5)
            {
                Console.WriteLine($"- ...and {failedDetails.Count - 5} more games with mismatches.");
            }
        }

        private static SimulationTrackingContext CreateCombinedTrackingContext(List<GameResult> gameResults)
        {
            var combined = new SimulationTrackingContext();

            foreach (var result in gameResults)
            {
                if (result.TrackingContext != null)
                {
                    combined.MergeFirstUpgradeRoundsFrom(result.TrackingContext);
                }
            }

            return combined;
        }
    }
}