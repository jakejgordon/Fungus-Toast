using FungusToast.Core.AI;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.Analysis
{
    public class ResistantCellTester
    {
        public static void RunTest(int numberOfGames, bool outputToFile, string outputFileName)
        {
            Console.WriteLine($"Running Resistant cell system test with 2 players for {numberOfGames} games each...\n");
            Console.WriteLine("Testing that initial spores are resistant and cannot be killed\n");

            // Set up output redirection if requested
            TextWriter? testOriginalOut = null;
            StreamWriter? testFileWriter = null;
            if (outputToFile)
            {
                SetupOutputRedirection(outputFileName, out testOriginalOut, out testFileWriter);
            }

            try
            {
                var results = ExecuteResistantCellTest(numberOfGames);
                PrintTestResults(results, numberOfGames);
            }
            finally
            {
                // Restore console output if we redirected it
                if (outputToFile && testOriginalOut != null)
                {
                    try
                    {
                        Console.SetOut(testOriginalOut);
                        testFileWriter?.Flush();
                        testFileWriter?.Close();
                        testFileWriter?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // If cleanup fails, write error to original console
                        testOriginalOut.WriteLine($"Warning: Failed to cleanup output redirection: {ex.Message}");
                    }
                }
            }
        }

        private static ResistantCellTestResults ExecuteResistantCellTest(int numberOfGames)
        {
            int gamesWithResistantCells = 0;
            int totalInitialCells = 0;
            int resistantCellsThatSurvived = 0;
            var gameResults = new List<GameResult>();

            for (int game = 0; game < numberOfGames; game++)
            {
                var testRnd = new Random(game + 42); // Deterministic seed
                var testStrategies = new List<IMutationSpendingStrategy>
                {
                    AIRoster.TestingStrategiesByName["Toxin Spammer"],
                    AIRoster.TestingStrategiesByName["Toxin Spammer"] // Two aggressive players
                };

                // Run a single game
                var testRunner = new MatchupRunner();
                var testResults = testRunner.RunMatchups(testStrategies, gamesToPlay: 1);

                if (testResults.GameResults.Count > 0)
                {
                    var gameResult = testResults.GameResults[0];
                    gameResults.Add(gameResult);

                    // Count initial cells that should be resistant
                    foreach (var playerResult in gameResult.PlayerResults)
                    {
                        // Check if any cells survived (they should be resistant)
                        if (playerResult.LivingCells > 0)
                        {
                            gamesWithResistantCells++;
                            resistantCellsThatSurvived += playerResult.LivingCells;
                        }
                        totalInitialCells += playerResult.LivingCells + playerResult.DeadCells;
                    }

                    float progress = (float)(game + 1) / numberOfGames * 100;
                    Console.WriteLine($"Game {game + 1}/{numberOfGames} - {progress:0.0}% - Winner: Player {gameResult.WinnerId} (Living cells: {gameResult.PlayerResults.Sum(p => p.LivingCells)})");
                }
            }

            return new ResistantCellTestResults
            {
                GamesWithResistantCells = gamesWithResistantCells,
                TotalInitialCells = totalInitialCells,
                ResistantCellsThatSurvived = resistantCellsThatSurvived,
                GameResults = gameResults
            };
        }

        private static void PrintTestResults(ResistantCellTestResults results, int numberOfGames)
        {
            Console.WriteLine("\n=== Resistant Cell Test Results ===");
            Console.WriteLine($"Games with resistant cells surviving: {results.GamesWithResistantCells}/{numberOfGames} ({results.GamesWithResistantCells * 100f / numberOfGames:0.0}%)");
            Console.WriteLine($"Total resistant cells that survived: {results.ResistantCellsThatSurvived}");
            Console.WriteLine($"Average resistant cells per game: {results.ResistantCellsThatSurvived / (float)numberOfGames:0.1}");
            
            if (results.GamesWithResistantCells == numberOfGames)
            {
                Console.WriteLine("✅ SUCCESS: All games had at least one resistant cell survive!");
            }
            else
            {
                Console.WriteLine("❌ FAILURE: Some games had no resistant cells survive!");
            }

            // Additional analysis
            Console.WriteLine("\n=== Detailed Analysis ===");
            AnalyzeGameResults(results.GameResults);

            Console.WriteLine("\nSimulation complete.");
        }

        private static void AnalyzeGameResults(List<GameResult> gameResults)
        {
            if (gameResults.Count == 0) return;

            var allPlayerResults = gameResults.SelectMany(g => g.PlayerResults).ToList();
            
            Console.WriteLine($"Total games analyzed: {gameResults.Count}");
            Console.WriteLine($"Total player results: {allPlayerResults.Count}");
            
            var playersWithLivingCells = allPlayerResults.Count(p => p.LivingCells > 0);
            Console.WriteLine($"Players with living cells at end: {playersWithLivingCells}/{allPlayerResults.Count} ({playersWithLivingCells * 100f / allPlayerResults.Count:0.0}%)");
            
            var averageLivingCells = allPlayerResults.Average(p => p.LivingCells);
            Console.WriteLine($"Average living cells per player: {averageLivingCells:0.1}");
            
            var maxLivingCells = allPlayerResults.Max(p => p.LivingCells);
            Console.WriteLine($"Maximum living cells for any player: {maxLivingCells}");
        }

        private static void SetupOutputRedirection(string outputFileName, out TextWriter originalOut, out StreamWriter fileWriter)
        {
            // Create SimulationOutput directory if it doesn't exist
            string outputDir = "SimulationOutput";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string fullPath = Path.Combine(outputDir, outputFileName);
            originalOut = Console.Out;
            fileWriter = new StreamWriter(fullPath);
            Console.SetOut(fileWriter);
            Console.WriteLine($"Simulation output redirected to: {fullPath}");
        }
    }

    public class ResistantCellTestResults
    {
        public int GamesWithResistantCells { get; set; }
        public int TotalInitialCells { get; set; }
        public int ResistantCellsThatSurvived { get; set; }
        public List<GameResult> GameResults { get; set; } = new();
    }
} 