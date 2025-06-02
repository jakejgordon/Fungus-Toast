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

        public void PrintReport(List<PlayerResult> allPlayerResults)
        {
            Console.WriteLine("\nStrategy-Mutation Usage Summary:");
            Console.WriteLine("{0,-37} | {1,-32} | {2,10} | {3,10} | {4,-20} | {5,13}",
                "Strategy", "Mutation Name", "Games Used", "Avg Level", "Mutation Effect", "Effect Count");

            Console.WriteLine(new string('-', 37) + "-|-" + new string('-', 32) + "-|-" +
                              new string('-', 10) + "-|-" + new string('-', 10) + "-|-" +
                              new string('-', 20) + "-|-" + new string('-', 13));

            // Step 1: Define which properties map to mutation effects
            var mutationEffectFields = new Dictionary<string, (int mutationId, string label)>
            {
                { nameof(PlayerResult.ReclaimedCells), (MutationIds.RegenerativeHyphae, "Reclaims") },
                { nameof(PlayerResult.CreepingMoldMoves), (MutationIds.CreepingMold, "Mold Movements") },
                { nameof(PlayerResult.NecroSpores), (MutationIds.Necrosporulation, "Necro Spores") },
                { nameof(PlayerResult.SporocidalSpores), (MutationIds.SporocidalBloom, "Sporicidal Drops") },
                { nameof(PlayerResult.NecrophyticSpores), (MutationIds.NecrophyticBloom, "Necrophytic Spores") },
                { nameof(PlayerResult.NecrophyticReclaims), (MutationIds.NecrophyticBloom, "Necrophytic Reclaims") },
            };

            // Step 2: Aggregate effect counts from player results
            var mutationEffectSums = new Dictionary<string, Dictionary<int, (int count, string label)>>();

            foreach (var result in allPlayerResults)
            {
                var strategy = result.StrategyName;

                if (!mutationEffectSums.TryGetValue(strategy, out var strategyDict))
                {
                    strategyDict = new Dictionary<int, (int, string)>();
                    mutationEffectSums[strategy] = strategyDict;
                }

                foreach (var prop in typeof(PlayerResult).GetProperties())
                {
                    if (mutationEffectFields.TryGetValue(prop.Name, out var info))
                    {
                        int value = (int)(prop.GetValue(result) ?? 0);
                        if (value > 0)
                        {
                            if (strategyDict.TryGetValue(info.mutationId, out var existing))
                                strategyDict[info.mutationId] = (existing.count + value, info.label);
                            else
                                strategyDict[info.mutationId] = (value, info.label);
                        }
                    }
                }
            }

            // Step 3: Print usage + effect counts
            foreach (var strategy in strategyMutationLevels.Keys.OrderBy(k => k))
            {
                var mutations = strategyMutationLevels[strategy];
                mutationEffectSums.TryGetValue(strategy, out var effectDict); // may be null

                foreach (var kv in mutations.OrderBy(kv => kv.Key))
                {
                    int mutationId = kv.Key;
                    List<int> levels = kv.Value;

                    int gamesUsed = levels.Count(lvl => lvl > 0);
                    if (gamesUsed == 0) continue;

                    float avgLevel = levels.Count > 0 ? (float)levels.Sum() / levels.Count : 0f;

                    var mutation = MutationRegistry.GetById(mutationId);
                    string name = mutation?.Name ?? $"[ID {mutationId}]";

                    string effectLabel = "";
                    string effectCountStr = "";

                    if (effectDict != null && effectDict.TryGetValue(mutationId, out var entry))
                    {
                        effectLabel = entry.label;
                        effectCountStr = entry.count.ToString();
                    }

                    Console.WriteLine("{0,-37} | {1,-32} | {2,10} | {3,10:F2} | {4,-20} | {5,13}",
                        Truncate(strategy, 37),
                        Truncate(name, 32),
                        gamesUsed,
                        avgLevel,
                        Truncate(effectLabel, 20),
                        effectCountStr);
                }
            }

            Console.WriteLine(new string('-', 130));
        }



        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
