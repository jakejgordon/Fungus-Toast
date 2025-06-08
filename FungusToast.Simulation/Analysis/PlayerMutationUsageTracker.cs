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
            Console.WriteLine("\nPlayer-Mutation Usage Summary:");
            Console.WriteLine("{0,-37} | {1,-32} | {2,10} | {3,10} | {4,-32} | {5,32}",
                "Strategy", "Mutation Name", "Games Used", "Avg Level", "Mutation Effect(s)", "Avg Effect Count(s)");

            Console.WriteLine(new string('-', 37) + "-|-" + new string('-', 32) + "-|-" +
                              new string('-', 10) + "-|-" + new string('-', 10) + "-|-" +
                              new string('-', 32) + "-|-" + new string('-', 32));

            var mutationEffectFields = new List<(int mutationId, string propertyName, string label)>
            {
                (MutationIds.RegenerativeHyphae, nameof(PlayerResult.ReclaimedCells), "Reclaims"),
                (MutationIds.CreepingMold, nameof(PlayerResult.CreepingMoldMoves), "Mold Movements"),
                (MutationIds.Necrosporulation, nameof(PlayerResult.NecrosporulationSpores), "Necro Spores"),
                (MutationIds.SporocidalBloom, nameof(PlayerResult.SporocidalSpores), "Sporicidal Drops"),
                (MutationIds.NecrophyticBloom, nameof(PlayerResult.NecrophyticSpores), "Necrophytic Spores"),
                (MutationIds.NecrophyticBloom, nameof(PlayerResult.NecrophyticReclaims), "Necrophytic Reclaims"),
                (MutationIds.MycotoxinTracer, nameof(PlayerResult.MycotoxinTracerSpores), "Mycotoxin Spores"),
                (MutationIds.MycotoxinPotentiation, nameof(PlayerResult.ToxinAuraKills), "Toxin Aura Kills"),
                (MutationIds.MycotoxinCatabolism, nameof(PlayerResult.MycotoxinCatabolisms), "Toxin Catabolisms"),
                (MutationIds.MycotoxinCatabolism, nameof(PlayerResult.CatabolizedMutationPoints), "Catabolized MP"),
                (MutationIds.MutatorPhenotype, nameof(PlayerResult.MutatorPhenotypePointsEarned), "Mutator Free MP"),
                (MutationIds.HyperadaptiveDrift, nameof(PlayerResult.HyperadaptiveDriftPointsEarned), "Hyperadaptive Free MP"),
                // Necrohyphal: track both fields for single-row effect
                (MutationIds.NecrohyphalInfiltration, nameof(PlayerResult.NecrohyphalInfiltrations), "Infiltrations"),
                (MutationIds.NecrohyphalInfiltration, nameof(PlayerResult.NecrohyphalCascades), "Cascades"),
            };

            var strategyGamesPlayed = allPlayerResults
                .GroupBy(r => r.StrategyName)
                .ToDictionary(g => g.Key, g => g.Count());

            // Aggregate effects
            var effectAgg = new Dictionary<string, Dictionary<int, Dictionary<string, int>>>(); // strategy -> mutationId -> (label -> sum)
            foreach (var result in allPlayerResults)
            {
                string strategy = result.StrategyName ?? "None";
                if (!effectAgg.TryGetValue(strategy, out var perMutation))
                {
                    perMutation = new Dictionary<int, Dictionary<string, int>>();
                    effectAgg[strategy] = perMutation;
                }

                foreach (var (mutationId, propertyName, label) in mutationEffectFields)
                {
                    var prop = typeof(PlayerResult).GetProperty(propertyName);
                    if (prop == null) continue;
                    int value = (int)(prop.GetValue(result) ?? 0);
                    if (value == 0) continue;

                    if (!perMutation.TryGetValue(mutationId, out var labelDict))
                    {
                        labelDict = new Dictionary<string, int>();
                        perMutation[mutationId] = labelDict;
                    }
                    labelDict[label] = labelDict.GetValueOrDefault(label, 0) + value;
                }
            }

            foreach (var strategy in strategyMutationLevels.Keys.OrderBy(k => k))
            {
                var mutations = strategyMutationLevels[strategy];
                effectAgg.TryGetValue(strategy, out var mutationEffects);

                int gamesForStrategy = strategyGamesPlayed.TryGetValue(strategy, out var cnt) ? cnt : 0;

                foreach (var kv in mutations.OrderBy(kv => kv.Key))
                {
                    int mutationId = kv.Key;
                    List<int> levels = kv.Value;

                    int gamesUsed = levels.Count(lvl => lvl > 0);
                    if (gamesUsed == 0) continue;

                    float avgLevel = levels.Count > 0 ? (float)levels.Sum() / levels.Count : 0f;

                    var mutation = MutationRegistry.GetById(mutationId);
                    string name = mutation?.Name ?? $"[ID {mutationId}]";

                    List<string> labels = new();
                    List<string> avgEffects = new();

                    // Special logic: For Necrohyphal, group both Infiltrations/Cascades into a single row
                    if (mutationId == MutationIds.NecrohyphalInfiltration)
                    {
                        int infiltrations = 0, cascades = 0;
                        if (mutationEffects != null && mutationEffects.TryGetValue(mutationId, out var effectDict))
                        {
                            infiltrations = effectDict.GetValueOrDefault("Infiltrations", 0);
                            cascades = effectDict.GetValueOrDefault("Cascades", 0);
                        }
                        labels.Add("Infiltrations / Cascades");
                        string avgInf = gamesForStrategy > 0 ? ((float)infiltrations / gamesForStrategy).ToString("F2") : "0.00";
                        string avgCas = gamesForStrategy > 0 ? ((float)cascades / gamesForStrategy).ToString("F2") : "0.00";
                        avgEffects.Add($"{avgInf} / {avgCas}");
                    }
                    else
                    {
                        if (mutationEffects != null && mutationEffects.TryGetValue(mutationId, out var effectDict) && effectDict.Count > 0)
                        {
                            foreach (var kvp in effectDict)
                            {
                                labels.Add(kvp.Key);
                                float avg = (gamesForStrategy > 0) ? (float)kvp.Value / gamesForStrategy : 0f;
                                avgEffects.Add(avg.ToString("F2"));
                            }
                        }
                    }

                    string effectLabel = string.Join(" / ", labels);
                    string effectAvgStr = string.Join(" / ", avgEffects);

                    Console.WriteLine("{0,-37} | {1,-32} | {2,10} | {3,10:F2} | {4,-32} | {5,32}",
                        Truncate(strategy, 37),
                        Truncate(name, 32),
                        gamesUsed,
                        avgLevel,
                        Truncate(effectLabel, 32),
                        effectAvgStr);
                }
            }

            Console.WriteLine(new string('-', 170));
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
