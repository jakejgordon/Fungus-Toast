using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class PlayerMutationUsageTracker
    {
        private readonly Dictionary<string, Dictionary<int, List<int>>> strategyMutationLevels = new();

        public void TrackGameResult(GameResult result)
        {
            foreach (var player in result.PlayerResults)
            {
                string strategy = player.StrategyName ?? "None";

                if (!strategyMutationLevels.TryGetValue(strategy, out var mutationDict))
                {
                    mutationDict = new Dictionary<int, List<int>>();
                    strategyMutationLevels[strategy] = mutationDict;
                }

                var knownMutationIds = new HashSet<int>(mutationDict.Keys);
                var allMutationIds = new HashSet<int>(knownMutationIds.Union(player.MutationLevels.Keys));

                foreach (var mutationId in allMutationIds)
                {
                    if (!mutationDict.TryGetValue(mutationId, out var levelList))
                    {
                        levelList = new List<int>();
                        mutationDict[mutationId] = levelList;
                    }

                    int level = player.MutationLevels.TryGetValue(mutationId, out var val) ? val : 0;
                    levelList.Add(level);
                }
            }
        }

        public void PrintReport(List<PlayerResult> allPlayerResults)
        {
            // Group by (PlayerId, Strategy) for accurate per-player/per-strategy reporting
            var playerStrategyGroups = allPlayerResults
                .GroupBy(r => (r.PlayerId, r.StrategyName ?? "None"))
                .OrderBy(g => g.Key.Item1); // Order by PlayerId

            Console.WriteLine("\nPlayer-Mutation Usage Summary (per Player, all games):");
            Console.WriteLine("{0,8} | {1,-37} | {2,-32} | {3,10} | {4,10} | {5,-32} | {6,20} | {7,20} | {8,12}",
                "PlayerId", "Strategy", "Mutation Name", "Games", "AvgLvl",
                "Mutation Effect(s)", "Avg Effect (All Games)", "Avg (w/ Upgrade Only)", "Avg Alive");

            Console.WriteLine(new string('-', 8) + "-|-" + new string('-', 37) + "-|-" + new string('-', 32) + "-|-" +
                              new string('-', 10) + "-|-" + new string('-', 10) + "-|-" +
                              new string('-', 32) + "-|-" + new string('-', 20) + "-|-" +
                              new string('-', 20) + "-|-" +
                              new string('-', 12));

            // Effect mapping
            var mutationEffectFields = new List<(int mutationId, string propertyName, string label)>
    {
        (MutationIds.RegenerativeHyphae, nameof(PlayerResult.ReclaimedCells), "Reclaims"),
        (MutationIds.CreepingMold, nameof(PlayerResult.CreepingMoldMoves), "Mold Movements"),
        (MutationIds.Necrosporulation, nameof(PlayerResult.NecrosporulationSpores), "Necro Spores"),
        (MutationIds.SporocidalBloom, nameof(PlayerResult.SporocidalSpores), "Spore Drops"),
        (MutationIds.SporocidalBloom, nameof(PlayerResult.SporocidalKills), "Spore Kills"),
        (MutationIds.NecrophyticBloom, nameof(PlayerResult.NecrophyticSpores), "Spore Drops"),
        (MutationIds.NecrophyticBloom, nameof(PlayerResult.NecrophyticReclaims), "Reclaims"),
        (MutationIds.MycotoxinTracer, nameof(PlayerResult.MycotoxinTracerSpores), "Toxin Drops"),
        (MutationIds.MycotoxinPotentiation, nameof(PlayerResult.ToxinAuraKills), "Toxin Aura Kills"),
        (MutationIds.MycotoxinCatabolism, nameof(PlayerResult.MycotoxinCatabolisms), "Toxin Catabolisms"),
        (MutationIds.MycotoxinCatabolism, nameof(PlayerResult.CatabolizedMutationPoints), "Catabolized MP"),
        (MutationIds.AdaptiveExpression, nameof(PlayerResult.AdaptiveExpressionPointsEarned), "Bonus MP"),
        (MutationIds.MutatorPhenotype, nameof(PlayerResult.MutatorPhenotypePointsEarned), "Mutator Free MP"),
        (MutationIds.HyperadaptiveDrift, nameof(PlayerResult.HyperadaptiveDriftPointsEarned), "Hyperadaptive Free MP"),
        (MutationIds.NecrohyphalInfiltration, nameof(PlayerResult.NecrohyphalInfiltrations), "Infiltrations"),
        (MutationIds.NecrohyphalInfiltration, nameof(PlayerResult.NecrohyphalCascades), "Cascades"),
        (MutationIds.PutrefactiveMycotoxin, nameof(PlayerResult.PutrefactiveMycotoxinKills), "PM Kills"),
        (MutationIds.NecrotoxicConversion, nameof(PlayerResult.NecrotoxicConversionReclaims), "Necrotoxic Reclaims"),
        (MutationIds.HyphalSurge, nameof(PlayerResult.HyphalSurgeGrowths), "Hyphal Surge Growths"),
        (MutationIds.TendrilNorthwest, nameof(PlayerResult.TendrilNorthwestGrownCells), "Grown Cells"),
        (MutationIds.TendrilNortheast, nameof(PlayerResult.TendrilNortheastGrownCells), "Grown Cells"),
        (MutationIds.TendrilSoutheast, nameof(PlayerResult.TendrilSoutheastGrownCells), "Grown Cells"),
        (MutationIds.TendrilSouthwest, nameof(PlayerResult.TendrilSouthwestGrownCells), "Grown Cells"),
    };

            foreach (var group in playerStrategyGroups)
            {
                int playerId = group.Key.Item1;
                string strategy = group.Key.Item2;
                var playerResults = group.ToList();
                int games = playerResults.Count;

                // All mutation IDs this player ever had (union)
                var allMutationIds = playerResults
                    .SelectMany(r => r.MutationLevels.Keys)
                    .Distinct()
                    .OrderBy(id => id);

                // Compute average alive for this player
                float avgAlive = games > 0 ? (float)playerResults.Average(r => r.LivingCells) : 0f;

                foreach (var mutationId in allMutationIds)
                {
                    // Fix: cast Average to float to resolve compile error
                    float avgLevel = (games > 0)
                        ? (float)playerResults.Average(r => r.MutationLevels.TryGetValue(mutationId, out var lvl) ? lvl : 0)
                        : 0f;

                    // Only count games where player had the upgrade
                    var gamesWithUpgrade = playerResults.Where(r => r.MutationLevels.TryGetValue(mutationId, out var lvl) && lvl > 0).ToList();
                    int gamesWithUpgradeCount = gamesWithUpgrade.Count;

                    List<string> labels = new();
                    List<string> avgEffects = new();
                    List<string> avgEffectsWithUpgrade = new();

                    // Special: group Infiltrations/Cascades in one row
                    if (mutationId == MutationIds.NecrohyphalInfiltration)
                    {
                        int totalInf = playerResults.Sum(r => r.NecrohyphalInfiltrations);
                        int totalCas = playerResults.Sum(r => r.NecrohyphalCascades);
                        int infWithUpgrade = gamesWithUpgrade.Sum(r => r.NecrohyphalInfiltrations);
                        int casWithUpgrade = gamesWithUpgrade.Sum(r => r.NecrohyphalCascades);

                        labels.Add("Infiltrations / Cascades");
                        string avgInf = games > 0 ? ((float)totalInf / games).ToString("F2") : "0.00";
                        string avgCas = games > 0 ? ((float)totalCas / games).ToString("F2") : "0.00";
                        string avgInfWithUpgrade = gamesWithUpgradeCount > 0 ? ((float)infWithUpgrade / gamesWithUpgradeCount).ToString("F2") : "0.00";
                        string avgCasWithUpgrade = gamesWithUpgradeCount > 0 ? ((float)casWithUpgrade / gamesWithUpgradeCount).ToString("F2") : "0.00";

                        avgEffects.Add($"{avgInf} / {avgCas}");
                        avgEffectsWithUpgrade.Add($"{avgInfWithUpgrade} / {avgCasWithUpgrade}");
                    }
                    else
                    {
                        foreach (var (effectMutationId, propertyName, label) in mutationEffectFields.Where(x => x.mutationId == mutationId))
                        {
                            int totalEffect = playerResults.Sum(pr => (int)(typeof(PlayerResult).GetProperty(propertyName)?.GetValue(pr) ?? 0));
                            int effectWithUpgrade = gamesWithUpgrade.Sum(pr => (int)(typeof(PlayerResult).GetProperty(propertyName)?.GetValue(pr) ?? 0));

                            float avgEffectPerGame = games > 0 ? (float)totalEffect / games : 0f;
                            float avgEffectPerUpgradeGame = gamesWithUpgradeCount > 0 ? (float)effectWithUpgrade / gamesWithUpgradeCount : 0f;
                            labels.Add(label);
                            avgEffects.Add(avgEffectPerGame.ToString("F2"));
                            avgEffectsWithUpgrade.Add(avgEffectPerUpgradeGame.ToString("F2"));
                        }
                    }

                    string effectLabel = string.Join(" / ", labels);
                    string effectAvgStr = string.Join(" / ", avgEffects);
                    string effectAvgWithUpgradeStr = string.Join(" / ", avgEffectsWithUpgrade);

                    // Print row
                    Console.WriteLine("{0,8} | {1,-37} | {2,-32} | {3,10} | {4,10:F2} | {5,-32} | {6,20} | {7,20} | {8,12:F2}",
                        playerId,
                        Truncate(strategy, 37),
                        Truncate(MutationRegistry.GetById(mutationId)?.Name ?? $"[ID {mutationId}]", 32),
                        games,
                        avgLevel,
                        Truncate(effectLabel, 32),
                        effectAvgStr,
                        effectAvgWithUpgradeStr,
                        avgAlive);
                }
            }

            Console.WriteLine(new string('-', 245));
        }

        // Helper for column truncation
        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];


    }
}
