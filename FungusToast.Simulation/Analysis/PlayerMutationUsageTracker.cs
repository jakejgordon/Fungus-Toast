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
            var playerStrategyGroups = allPlayerResults
                .GroupBy(r => (r.PlayerId, r.StrategyName ?? "None"))
                .OrderBy(g => g.Key.Item1);

            Console.WriteLine("\nPlayer-Mutation Usage Summary (per Player, all games):");
            Console.WriteLine("{0,8} | {1,-37} | {2,-32} | {3,10} | {4,10} | {5,-32} | {6,24} | {7,24} | {8,12}",
                "PlayerId", "Strategy", "Mutation Name", "Games", "AvgLvl",
                "Mutation Effect(s)", "Avg Effect (All Games)", "Total Effect (All Games)", "Avg Alive");

            Console.WriteLine(new string('-', 8) + "-|-" + new string('-', 37) + "-|-" + new string('-', 32) + "-|-" +
                              new string('-', 10) + "-|-" + new string('-', 10) + "-|-" +
                              new string('-', 32) + "-|-" + new string('-', 24) + "-|-" +
                              new string('-', 24) + "-|-" +
                              new string('-', 12));

            var mutationEffectFields = GetMutationEffectFields();

            foreach (var group in playerStrategyGroups)
            {
                int playerId = group.Key.Item1;
                string strategy = group.Key.Item2;
                var playerResults = group.ToList();
                int games = playerResults.Count;
                float avgAlive = games > 0 ? (float)playerResults.Average(r => r.LivingCells) : 0f;

                var allMutationIds = playerResults
                    .SelectMany(r => r.MutationLevels.Keys)
                    .Distinct()
                    .OrderBy(id => id);

                foreach (var mutationId in allMutationIds)
                {
                    float avgLevel = (games > 0)
                        ? (float)playerResults.Average(r => r.MutationLevels.TryGetValue(mutationId, out var lvl) ? lvl : 0)
                        : 0f;

                    var (effectLabel, avgEffects, totalEffects) = GetMutationEffectStats(mutationId, playerResults, mutationEffectFields, games);

                    Console.WriteLine("{0,8} | {1,-37} | {2,-32} | {3,10} | {4,10:F2} | {5,-32} | {6,24} | {7,24} | {8,12:F2}",
                        playerId,
                        Truncate(strategy, 37),
                        Truncate(MutationRegistry.GetById(mutationId)?.Name ?? $"[ID {mutationId}]", 32),
                        games,
                        avgLevel,
                        Truncate(effectLabel, 32),
                        avgEffects,
                        totalEffects,
                        avgAlive);
                }
            }

            Console.WriteLine(new string('-', 245));
        }

        private static (string label, string avgEffect, string totalEffect) GetMutationEffectStats(
            int mutationId,
            List<PlayerResult> playerResults,
            List<(int mutationId, string propertyName, string label)> mutationEffectFields,
            int games)
        {
            List<string> labels = new();
            List<string> avgEffects = new();
            List<string> totalEffects = new();

            // Special case for NecrohyphalInfiltration (compound effect)
            if (mutationId == MutationIds.NecrohyphalInfiltration)
            {
                long totalInf = playerResults.Sum(r => (long)r.NecrohyphalInfiltrations);
                long totalCas = playerResults.Sum(r => (long)r.NecrohyphalCascades);
                float avgInf = games > 0 ? (float)totalInf / games : 0f;
                float avgCas = games > 0 ? (float)totalCas / games : 0f;

                labels.Add("Infiltrations / Cascades");
                avgEffects.Add($"{avgInf:N2} / {avgCas:N2}");
                totalEffects.Add($"{totalInf:N0} / {totalCas:N0}");
            }
            else
            {
                foreach (var (effectMutationId, propertyName, label) in mutationEffectFields.Where(x => x.mutationId == mutationId))
                {
                    var perGameEffects = playerResults
                        .Select(pr =>
                        {
                            var val = typeof(PlayerResult).GetProperty(propertyName)?.GetValue(pr) ?? 0;
                            // Always unbox to int first, then convert to long, for safety
                            if (val is int i) return (long)i;
                            if (val is long l) return l;
                            if (val is float f) return (long)f;
                            if (val is double d) return (long)d;
                            return Convert.ToInt64(val);
                        })
                        .ToList();

                    long totalEffect = perGameEffects.Sum();
                    float avgEffectPerGame = games > 0 ? (float)totalEffect / games : 0f;

                    labels.Add(label);
                    avgEffects.Add(avgEffectPerGame.ToString("N2")); // thousands separator, 2 decimals
                    totalEffects.Add(totalEffect.ToString("N0"));     // thousands separator, no decimals
                }
            }

            string effectLabel = string.Join(" / ", labels);
            string avgEffectStr = string.Join(" / ", avgEffects);
            string totalEffectStr = string.Join(" / ", totalEffects);

            return (effectLabel, avgEffectStr, totalEffectStr);
        }

        private static List<(int mutationId, string propertyName, string label)> GetMutationEffectFields()
        {
            return new List<(int, string, string)>
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
                (MutationIds.HyphalVectoring, nameof(PlayerResult.HyphalVectoringGrowths), "Hyphal Vectoring Growths"),
                (MutationIds.TendrilNorthwest, nameof(PlayerResult.TendrilNorthwestGrownCells), "Grown Cells"),
                (MutationIds.TendrilNortheast, nameof(PlayerResult.TendrilNortheastGrownCells), "Grown Cells"),
                (MutationIds.TendrilSoutheast, nameof(PlayerResult.TendrilSoutheastGrownCells), "Grown Cells"),
                (MutationIds.TendrilSouthwest, nameof(PlayerResult.TendrilSouthwestGrownCells), "Grown Cells"),
            };
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
