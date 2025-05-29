using System;
using System.Collections.Generic;
using FungusToast.Core.AI;
using FungusToast.Simulation.Analysis;

class Program
{
    private const int NumberOfSimulationGames = 100;

    static void Main()
    {
        var strategies = new List<IMutationSpendingStrategy>
        {
            new SmartRandomMutationSpendingStrategy(),
            new GrowthThenDefenseSpendingStrategy(),
            new RandomMutationSpendingStrategy(),
            new MutationFocusedMutationSpendingStrategy(),
            new SmartRandomMutationSpendingStrategy(),
            new GrowthThenDefenseSpendingStrategy(),
            new RandomMutationSpendingStrategy(),
            new MutationFocusedMutationSpendingStrategy()
        };

        int playerCount = strategies.Count;
        Console.WriteLine($"Running simulation with {playerCount} players...\n");

        // Run simulation
        var runner = new MatchupRunner();
        var results = runner.RunMatchups(strategies, gamesToPlay: NumberOfSimulationGames);

        // Print strategy summary
        var aggregator = new MatchupStatsAggregator();
        aggregator.PrintSummary(results);

        // Analyze mutation impact
        var impactTracker = new MutationImpactTracker();
        foreach (var result in results)
        {
            impactTracker.TrackGameResult(result);
        }
        impactTracker.PrintReport();

        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }
}
