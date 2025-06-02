using FungusToast.Core.AI;
using FungusToast.Core.Core.Mutations;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;
using System;
using System.Collections.Generic;

class Program
{
    private const int NumberOfSimulationGames = 10;

    static void Main()
    {

        var highTier = new ParameterizedSpendingStrategy(
            strategyName: "HighTier",
            prioritizeHighTier: true);

        var growthAndResilienceMax3HighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilience_Max3_HighTier",
            maxTier: MutationTier.Tier3,
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                MutationCategory.Growth,
                MutationCategory.CellularResilience
            });

        var growthAndResilienceHighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilience_HighTier",
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                        MutationCategory.Growth,
                        MutationCategory.CellularResilience
            });

        /**last place in 10000 game simulation
        var growthResilienceGeneticDriftHighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilienceGeneticDrift_HighTier",
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                                MutationCategory.Growth,
                                MutationCategory.CellularResilience,
                                MutationCategory.GeneticDrift
            });
        **/
        
        var max2 = new ParameterizedSpendingStrategy(
            strategyName: "Max2",
            prioritizeHighTier: false);

        var regenerativeHyphaeFocus = new ParameterizedSpendingStrategy(
            strategyName: "Regenerative Hyphae Focus",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.RegenerativeHyphae });

        var strategies = new List<IMutationSpendingStrategy>
        {
            new SmartRandomMutationSpendingStrategy(),
            new GrowthThenDefenseSpendingStrategy(),
            new RandomMutationSpendingStrategy(),
            max2,
            highTier,
            growthAndResilienceMax3HighTier,
            growthAndResilienceHighTier,
            regenerativeHyphaeFocus
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

        // Analyze per-strategy mutation usage
        var usageTracker = new StrategyMutationUsageTracker();
        foreach (var result in results)
        {
            usageTracker.TrackGameResult(result);
        }
        usageTracker.PrintReport();

        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }
}
