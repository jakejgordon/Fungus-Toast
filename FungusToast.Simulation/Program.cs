using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;

class Program
{
    private const int NumberOfSimulationGames = 10;

    static void Main()
    {
        var rnd = new Random(); // Or any deterministic seed you want

        var strategies = AIRoster.GetRandomProvenStrategies(8, rnd);

        Console.WriteLine($"Running simulation with {strategies.Count} players...\n");

        // Run simulation
        var runner = new MatchupRunner();
        var results = runner.RunMatchups(strategies, gamesToPlay: NumberOfSimulationGames);

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
        var allPlayerResults = results.GameResults.SelectMany(r => r.PlayerResults).ToList();
        var rankedPlayers = MatchupStatsAggregator.GetRankedPlayerList(results.GameResults);
        usageTracker.PrintReport(allPlayerResults, rankedPlayers);

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
