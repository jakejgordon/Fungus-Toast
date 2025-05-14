using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.GameSimulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MatchupStatsAggregator
    {
        public void PrintSummary(List<GameResult> results)
        {
            int totalGames = results.Count;
            int totalTurns = results.Sum(r => r.TurnsPlayed);
            float avgTurns = totalGames > 0 ? (float)totalTurns / totalGames : 0;
            double sumSquaredDiffs = results.Sum(r => Math.Pow(r.TurnsPlayed - avgTurns, 2));
            double stdDevTurns = totalGames > 1 ? Math.Sqrt(sumSquaredDiffs / (totalGames - 1)) : 0;

            // wins, games, alive, dead, mpSpent
            var strategyStats = new Dictionary<string, (int wins, int games, int totalLiving, int totalDead, int mutationPointsSpent)>();
            var mutationTotalsByStrategy = new Dictionary<string, Dictionary<int, (int totalLevel, int count)>>();

            foreach (var result in results)
            {
                var winner = result.PlayerResults.FirstOrDefault(p => p.PlayerId == result.WinnerId);
                if (winner == null) continue;

                // Count 1 win per game for the winner's strategy
                var winnerStrategy = winner.StrategyName;
                if (!string.IsNullOrWhiteSpace(winnerStrategy))
                {
                    if (!strategyStats.ContainsKey(winnerStrategy))
                        strategyStats[winnerStrategy] = (0, 0, 0, 0, 0);

                    var winEntry = strategyStats[winnerStrategy];
                    winEntry.wins++;
                    strategyStats[winnerStrategy] = winEntry;
                }

                var uniqueStrategies = new HashSet<string>();

                foreach (var pr in result.PlayerResults)
                {
                    string strategy = pr.StrategyName;

                    if (!strategyStats.ContainsKey(strategy))
                        strategyStats[strategy] = (0, 0, 0, 0, 0);

                    var entry = strategyStats[strategy];

                    if (!uniqueStrategies.Contains(strategy))
                    {
                        entry.games++;
                        uniqueStrategies.Add(strategy);
                    }

                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;

                    // 🔢 Total mutation points spent = sum of all mutation levels * cost per level
                    int mpSpent = pr.MutationLevels.Sum(kv =>
                    {
                        var m = MutationRegistry.GetById(kv.Key);
                        return (m != null) ? m.PointsPerUpgrade * kv.Value : 0;
                    });

                    entry.mutationPointsSpent += mpSpent;
                    strategyStats[strategy] = entry;

                    // Mutation levels
                    if (!mutationTotalsByStrategy.ContainsKey(strategy))
                        mutationTotalsByStrategy[strategy] = new();

                    foreach (var kv in pr.MutationLevels)
                    {
                        if (kv.Value == 0) continue;

                        var mtDict = mutationTotalsByStrategy[strategy];
                        if (!mtDict.ContainsKey(kv.Key))
                            mtDict[kv.Key] = (0, 0);

                        var current = mtDict[kv.Key];
                        mtDict[kv.Key] = (current.totalLevel + kv.Value, current.count + 1);
                    }
                }
            }

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");

            Console.WriteLine("\nStrategy Summary:");
            Console.WriteLine("Strategy                             | WinRate | Avg Alive | Avg Dead | Games | Avg MP Spent");
            Console.WriteLine("-------------------------------------|---------|-----------|----------|-------|--------------");

            foreach (var kvp in strategyStats.OrderByDescending(kvp => kvp.Value.wins))
            {
                var (wins, games, living, dead, mpSpent) = kvp.Value;
                float winRate = (float)wins / games * 100;
                float avgLiving = (float)living / games;
                float avgDead = (float)dead / games;
                float avgMpSpent = (float)mpSpent / games;

                Console.WriteLine($"{kvp.Key,-37} | {winRate,6:F1}% | {avgLiving,9:F1} | {avgDead,8:F1} | {games,5} | {avgMpSpent,12:F1}");
            }

            Console.WriteLine("\nMutation Usage Per Strategy:");

            foreach (var strat in mutationTotalsByStrategy.Keys.OrderBy(k => k))
            {
                Console.WriteLine($"\nStrategy: {strat}");
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine("Mutation Name                   | Avg Level");
                Console.WriteLine("--------------------------------------------");

                var mutationTotals = mutationTotalsByStrategy[strat];

                foreach (var kv in mutationTotals.OrderBy(kv => kv.Key))
                {
                    var mutation = MutationRegistry.GetById(kv.Key);
                    string name = mutation?.Name ?? $"[ID {kv.Key}]";
                    float avg = (float)kv.Value.totalLevel / kv.Value.count;
                    Console.WriteLine($"{name,-30} | {avg,9:F2}");
                }
            }
        }
    }
}
