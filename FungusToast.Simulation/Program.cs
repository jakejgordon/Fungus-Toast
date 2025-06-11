using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;

class Program
{
    private const int NumberOfSimulationGames = 1;

    static void Main()
    {

        var mutatorGrowth = new ParameterizedSpendingStrategy(
            strategyName: "Mutator Growth",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.CreepingMold });

        var toxinBadGuy = new ParameterizedSpendingStrategy(
            strategyName: "Toxins and Creeping Mold",
            prioritizeHighTier: false,
            targetMutationIds: new List<int> { MutationIds.NecrophyticBloom, MutationIds.NecrotoxicConversion, MutationIds.CreepingMold });

        var toxinBadGuyExtreme = new ParameterizedSpendingStrategy(
            strategyName: "Maxed Mycotoxin Potentiation",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.MycotoxinPotentiation, MutationIds.MycotoxinTracer, MutationIds.NecrotoxicConversion });

        var growthAndResilienceMax3HighTier = new ParameterizedSpendingStrategy(
            strategyName: "GrowthResilience_Max3_HighTier",
            maxTier: MutationTier.Tier3,
            prioritizeHighTier: true,
            priorityMutationCategories: new List<MutationCategory>
            {
                MutationCategory.Growth,
                MutationCategory.CellularResilience
            });

        
        var min = new ParameterizedSpendingStrategy(
            strategyName: "Min 1",
            prioritizeHighTier: false);


        var regenerativeHyphaeFocus = new ParameterizedSpendingStrategy(
            strategyName: "Reg Hyphae and Necrohyphal",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.AnabolicInversion, MutationIds.RegenerativeHyphae, MutationIds.NecrohyphalInfiltration });

        var powerMutations1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Mutations 1",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

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
        aggregator.PrintSummary(results.GameResults, results.CumulativeDeathReasons, results.TrackingContext);

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
        usageTracker.PrintReport(allPlayerResults);


        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }
}
