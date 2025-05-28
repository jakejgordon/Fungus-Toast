using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MutationImpactTracker
    {
        private readonly Dictionary<int, MutationImpactStats> mutationStats = new();

        public void TrackGameResult(GameResult result)
        {
            foreach (var player in result.PlayerResults)
            {
                bool isWinner = player.PlayerId == result.WinnerId;

                foreach (var kv in player.MutationLevels)
                {
                    int mutationId = kv.Key;
                    int level = kv.Value;
                    if (level == 0) continue;

                    if (!mutationStats.TryGetValue(mutationId, out var stats))
                    {
                        var mutation = MutationRegistry.GetById(mutationId);
                        stats = new MutationImpactStats
                        {
                            MutationId = mutationId,
                            MutationName = mutation?.Name ?? $"[ID {mutationId}]"
                        };
                        mutationStats[mutationId] = stats;
                    }

                    stats.TotalAppearances++;
                    if (isWinner)
                    {
                        stats.WinsWith++;
                        stats.TotalLevelsInWins += level;
                    }
                }
            }
        }

        public void PrintReport()
        {
            Console.WriteLine("\nMutation Impact Analysis:");
            Console.WriteLine("Mutation Name                   | WinRate | Uses | Avg Level in Wins");
            Console.WriteLine("--------------------------------|---------|------|-------------------");

            foreach (var stat in mutationStats.Values
                         .OrderByDescending(s => s.WinRateWhenPresent))
            {
                Console.WriteLine(
                    $"{stat.MutationName,-32} | {stat.WinRateWhenPresent,6:F1}% | {stat.TotalAppearances,4} | {stat.AvgLevelInWins,17:F2}");
            }
        }

        private class MutationImpactStats
        {
            public int MutationId;
            public string MutationName = "";
            public int WinsWith = 0;
            public int TotalAppearances = 0;
            public int TotalLevelsInWins = 0;

            public float WinRateWhenPresent => TotalAppearances == 0 ? 0f : (float)WinsWith / TotalAppearances * 100f;
            public float AvgLevelInWins => WinsWith == 0 ? 0f : (float)TotalLevelsInWins / WinsWith;
        }
    }
}
