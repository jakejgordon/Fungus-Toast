using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;

class Program
{
    private const int NumberOfSimulationGames = 100;

    static void Main()
    {

        var mutatorGrowth = new ParameterizedSpendingStrategy(
            strategyName: "Mutator Growth",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.AnabolicInversion, MutationIds.CreepingMold });

        var toxinBadGuy = new ParameterizedSpendingStrategy(
            strategyName: "Toxin Bad Guy",
            prioritizeHighTier: false,
            targetMutationIds: new List<int> { MutationIds.NecrophyticBloom, MutationIds.PutrefactiveMycotoxin, MutationIds.CreepingMold });

        var toxinBadGuyExtreme = new ParameterizedSpendingStrategy(
            strategyName: "Maxed Mycotoxin Potentiation",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.MycotoxinPotentiation, MutationIds.MycotoxinTracer });

        var growthAndResilienceMax3HighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilience_Max3_HighTier",
            maxTier: MutationTier.Tier3,
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                MutationCategory.Growth,
                MutationCategory.CellularResilience
            });

        /*
        var growthAndResilienceHighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilience_HighTier",
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                        MutationCategory.Growth,
                        MutationCategory.CellularResilience
            });
        */
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
        
        
        var max2 = new ParameterizedSpendingStrategy(
            strategyName: "Max2",
            prioritizeHighTier: true);
        **/
        var min = new ParameterizedSpendingStrategy(
            strategyName: "Min 1",
            prioritizeHighTier: false);


        var regenerativeHyphaeFocus = new ParameterizedSpendingStrategy(
            strategyName: "Regenerative Hyphae Focus",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.RegenerativeHyphae });

        var powerMutations1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Mutations 1",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.AdaptiveExpression, MutationIds.Necrosporulation, MutationIds.RegenerativeHyphae });

        var strategies = new List<IMutationSpendingStrategy>
        {
            powerMutations1,
            new RandomMutationSpendingStrategy(),
            min,
            toxinBadGuy,
            mutatorGrowth,
            growthAndResilienceMax3HighTier,
            toxinBadGuyExtreme,
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
        var allPlayerResults = results.SelectMany(r => r.PlayerResults).ToList();
        usageTracker.PrintReport(allPlayerResults);

        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }
}
