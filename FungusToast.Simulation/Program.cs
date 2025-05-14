using System;
using System.Collections.Generic;
using FungusToast.Core.AI;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.GameSimulation;

class Program
{
    static void Main()
    {
        Console.WriteLine("Running simulation...");

        // 🔁 Create fixed strategy instances
        var rngA = new Random(1);
        var rngB = new Random(2);

        IMutationSpendingStrategy strategyA = new RandomMutationSpendingStrategy();
        IMutationSpendingStrategy strategyB = new SmartRandomMutationSpendingStrategy(); //new GrowthThenDefenseSpendingStrategy();

        // 🏁 Run headless batch
        var runner = new MatchupRunner();
        var results = runner.RunMatchups(strategyA, strategyB, gamesToPlay: 100);

        // 📊 Print summary
        var aggregator = new MatchupStatsAggregator();
        aggregator.PrintSummary(results);

        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }
}
