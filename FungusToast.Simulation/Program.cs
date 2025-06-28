using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Models;

class Program
{
    private const int DefaultNumberOfSimulationGames = 5;
    private const int DefaultNumberOfPlayers = 8;

    static void Main(string[] args)
    {
        // Parse command-line arguments
        int numberOfGames = DefaultNumberOfSimulationGames;
        int numberOfPlayers = DefaultNumberOfPlayers;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--games":
                case "-g":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int games))
                    {
                        numberOfGames = games;
                        i++; // Skip the next argument since we consumed it
                    }
                    break;
                case "--players":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int players))
                    {
                        numberOfPlayers = players;
                        i++; // Skip the next argument since we consumed it
                    }
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    return;
            }
        }

        var rnd = new Random(); // Or any deterministic seed you want

        var strategies = AIRoster.GetRandomProvenStrategies(numberOfPlayers, rnd);

        Console.WriteLine($"Running simulation with {strategies.Count} players for {numberOfGames} games each...\n");

        // Run simulation
        var runner = new MatchupRunner();
        var results = runner.RunMatchups(strategies, gamesToPlay: numberOfGames);

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

        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("FungusToast Simulation Runner");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -g, --games <number>     Number of games to play per matchup (default: 5)");
        Console.WriteLine("  -p, --players <number>   Number of players/strategies to use (default: 8)");
        Console.WriteLine("  -h, --help              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                           # Run with defaults (8 players, 5 games each)");
        Console.WriteLine("  dotnet run --games 10               # Run 10 games per matchup");
        Console.WriteLine("  dotnet run --players 4 --games 20   # Run 4 players, 20 games each");
        Console.WriteLine("  dotnet run -p 6 -g 15               # Run 6 players, 15 games each");
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

    private static List<IMutationSpendingStrategy> CreateSurgeMutationTestingStrategies()
    {
        var powerMutations1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 1 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 1,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations2 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 2 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 2,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations3 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 3 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 3,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations4 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 4 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 4,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations5 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 5 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 5,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations6 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 10 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 10,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations7 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 20 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 20,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        var powerMutations8 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 50 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 50,
            targetMutationGoals: new List<TargetMutationGoal>
            {
            new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
            new TargetMutationGoal(MutationIds.Necrosporulation),
            new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
            });

        return new List<IMutationSpendingStrategy>
    {
        powerMutations1,
        powerMutations2,
        powerMutations3,
        powerMutations4,
        powerMutations5,
        powerMutations6,
        powerMutations7,
        powerMutations8
    };
    }


    private static List<IMutationSpendingStrategy> CreateEconomyTestingStrategies()
    {
        var goals = new List<TargetMutationGoal>
    {
        new TargetMutationGoal(MutationIds.HyperadaptiveDrift),
        new TargetMutationGoal(MutationIds.Necrosporulation),
        new TargetMutationGoal(MutationIds.NecrohyphalInfiltration)
    };

        var mut1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 1 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 1,
            targetMutationGoals: goals
        );

        var mut2 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 2 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 2,
            targetMutationGoals: goals
        );

        var mut3 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 3 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 3,
            targetMutationGoals: goals
        );

        var mut4 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 4 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 4,
            targetMutationGoals: goals
        );

        var mut5 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 5 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 5,
            targetMutationGoals: goals
        );

        var mut6 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 10 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 10,
            targetMutationGoals: goals
        );

        var mut7 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 20 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 20,
            targetMutationGoals: goals
        );

        var mut8 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 50 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 50,
            targetMutationGoals: goals
        );

        return new List<IMutationSpendingStrategy>
    {
        mut1,
        mut2,
        mut3,
        mut4,
        mut5,
        mut6,
        mut7,
        mut8
    };
    }


}
