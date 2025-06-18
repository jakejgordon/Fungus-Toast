using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.StrategySets;

class Program
{
    private const int NumberOfSimulationGames = 500;

    static void Main()
    {
        //var strategies = CreatePredefinedStrategies();

        //int playerCount = 7;
        var rnd = new Random(); // Or any deterministic seed you want

        /*
        var strategies = EconomyBiasStrategyFactory.CreateEconomyBiasStrategies(
            playerCount: playerCount,
            rnd: rnd,
            targetMutationIds: new List<int> { MutationIds.CreepingMold, MutationIds.NecrohyphalInfiltration },
            surgeAttemptTurnFrequency: 10,
            prioritizeHighTier: true
        );
        */

        var strategies = MixedEconomySurgeStrategyFactory.CreateEightEconomySurgeStrategies(8);

        /*
        var killerToxin = new ParameterizedSpendingStrategy(
            strategyName: "Toxins",
            prioritizeHighTier: true,
            targetMutationIds: new List<int> { MutationIds.NecrophyticBloom, MutationIds.NecrotoxicConversion, MutationIds.SporocidalBloom });

        strategies.Add(killerToxin);
        */

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
        usageTracker.PrintReport(allPlayerResults);


        Console.WriteLine("\nSimulation complete. Press any key to exit.");
        Console.ReadKey();
    }

    private static List<IMutationSpendingStrategy> CreatePredefinedStrategies()
    {
        var mutatorGrowth = new ParameterizedSpendingStrategy(
            strategyName: "Mutator Growth",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 10,
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

        return new List<IMutationSpendingStrategy>
    {
        mutatorGrowth,
        toxinBadGuy,
        toxinBadGuyExtreme,
        growthAndResilienceMax3HighTier,
        min,
        regenerativeHyphaeFocus,
        powerMutations1
    };
    }

    private static List<IMutationSpendingStrategy> CreateSurgeMutationTestingStrategies()
    {
        var powerMutations1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 1 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 1,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations2 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 2 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 2,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations3 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 3 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 3,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations4 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 4 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 4,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations5 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 5 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 5,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations6 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 10 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 10,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations7 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 20 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 20,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var powerMutations8 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 50 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 50,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

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
        var mut1 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 1 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 1,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut2 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 2 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 2,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut3 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 3 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 3,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut4 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 4 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 4,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut5 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 5 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 5,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut6 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 10 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 10,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut7 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 20 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 20,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

        var mut8 = new ParameterizedSpendingStrategy(
            strategyName: "Power Skillz - 50 turn surge frequency",
            prioritizeHighTier: true,
            surgeAttemptTurnFrequency: 50,
            targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.Necrosporulation, MutationIds.NecrohyphalInfiltration });

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
