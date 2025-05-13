using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Simulation.GameSimulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MatchupStatsAggregator
    {
        public void PrintSummary(List<GameResult> results)
        {
            var strategyStats = new Dictionary<string, (int wins, int total, int totalLiving, int totalDead)>();

            foreach (var result in results)
            {
                var winner = result.PlayerResults.FirstOrDefault(p => p.PlayerId == result.WinnerId);
                if (winner == null) continue;

                foreach (var pr in result.PlayerResults)
                {
                    if (!strategyStats.ContainsKey(pr.StrategyName))
                        strategyStats[pr.StrategyName] = (0, 0, 0, 0);

                    var entry = strategyStats[pr.StrategyName];
                    entry.total++;
                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;

                    if (pr.StrategyName == winner.StrategyName)
                        entry.wins++;

                    strategyStats[pr.StrategyName] = entry;
                }
            }

            Console.WriteLine("\nStrategy Summary:");
            Console.WriteLine("Strategy                             | WinRate | Avg Alive | Avg Dead | Games");
            Console.WriteLine("-------------------------------------|---------|-----------|----------|-------");

            foreach (var kvp in strategyStats.OrderByDescending(kvp => kvp.Value.wins))
            {
                var (wins, total, living, dead) = kvp.Value;
                float winRate = (float)wins / total * 100;
                float avgLiving = (float)living / total;
                float avgDead = (float)dead / total;

                Console.WriteLine($"{kvp.Key,-37} | {winRate,6:F1}% | {avgLiving,9:F1} | {avgDead,8:F1} | {total,5}");
            }
        }
    }
}
