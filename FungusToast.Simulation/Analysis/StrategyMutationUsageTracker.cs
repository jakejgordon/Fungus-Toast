using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class StrategyMutationUsageTracker
    {
        // strategy → mutationId → list of levels (including 0s for games where not used)
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

                // Get a full list of all mutation IDs used by this strategy so far
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

        public void PrintReport()
        {
            Console.WriteLine("\nStrategy-Mutation Usage Summary:");
            Console.WriteLine("Strategy                              | Mutation Name                   | Games Used | Avg Level");
            Console.WriteLine("-------------------------------------|----------------------------------|------------|-----------");

            foreach (var strategy in strategyMutationLevels.Keys.OrderBy(k => k))
            {
                var mutations = strategyMutationLevels[strategy];
                foreach (var kv in mutations.OrderBy(kv => kv.Key))
                {
                    int mutationId = kv.Key;
                    List<int> levels = kv.Value;

                    int gamesUsed = levels.Count(lvl => lvl > 0);
                    if (gamesUsed == 0) continue; // Skip unused mutations

                    float avgLevel = levels.Count > 0 ? (float)levels.Sum() / levels.Count : 0f;

                    var mutation = MutationRegistry.GetById(mutationId);
                    string name = mutation?.Name ?? $"[ID {mutationId}]";

                    Console.WriteLine(
                        $"{Truncate(strategy, 37),-37} | {Truncate(name, 32),-32} | {gamesUsed,10} | {avgLevel,9:F2}");
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
