using System;
using System.Collections.Generic;
using FungusToast.Core.AI;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.GameSimulation;
using FungusToast.Simulation.GameSimulation.Models;

class Program
{
    static void Main()
    {
        Console.WriteLine("Running simulation with 4 players...\n");

        // 1. Declare/initialize strategies (one per player)
        /*
        IMutationSpendingStrategy player0 = new SmartRandomMutationSpendingStrategy();
        IMutationSpendingStrategy player1 = new SmartRandomMutationSpendingStrategy();//new RandomMutationSpendingStrategy();
        IMutationSpendingStrategy player2 = new SmartRandomMutationSpendingStrategy();// new GrowthThenDefenseSpendingStrategy();
        IMutationSpendingStrategy player3 = new SmartRandomMutationSpendingStrategy();//new GrowthThenDefenseSpendingStrategy();
        */


        // 2. Add all to a List in order
        var strategies = new List<IMutationSpendingStrategy>
        {
            new SmartRandomMutationSpendingStrategy(),
            new GrowthThenDefenseSpendingStrategy(),
            new RandomMutationSpendingStrategy(),
            new MutationFocusedMutationSpendingStrategy()
        };

        // 3. Run simulation
        var runner = new MatchupRunner();
        var results = runner.RunMatchups(strategies, gamesToPlay: 200);

        // 4. Print strategy summary
        var aggregator = new MatchupStatsAggregator();
        aggregator.PrintSummary(results);

        // 5. Analyze mutation impact
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
