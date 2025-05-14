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

            var strategyStats = new Dictionary<string, (int wins, int games, int totalLiving, int totalDead, int mutationPointsSpent, float growthChance, float selfDeathChance, float decayMod)>();
            var mutationTotalsByStrategy = new Dictionary<string, Dictionary<int, (int totalLevel, int count)>>();
            var mutationImpact = new Dictionary<int, (int winsWithMutation, int uses, int totalLevel)>();

            foreach (var result in results)
            {
                var winner = result.PlayerResults.FirstOrDefault(p => p.PlayerId == result.WinnerId);
                if (winner == null) continue;

                string winnerStrategy = winner.StrategyName;
                var uniqueStrategies = new HashSet<string>();

                foreach (var pr in result.PlayerResults)
                {
                    string strategy = pr.StrategyName;

                    if (!strategyStats.ContainsKey(strategy))
                        strategyStats[strategy] = (0, 0, 0, 0, 0, 0f, 0f, 0f);

                    var entry = strategyStats[strategy];

                    if (!uniqueStrategies.Contains(strategy))
                    {
                        entry.games++;
                        uniqueStrategies.Add(strategy);
                    }

                    if (strategy == winnerStrategy)
                        entry.wins++;

                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;
                    entry.mutationPointsSpent += pr.MutationLevels.Sum(kv => (MutationRegistry.GetById(kv.Key)?.PointsPerUpgrade ?? 0) * kv.Value);
                    entry.growthChance += pr.EffectiveGrowthChance;
                    entry.selfDeathChance += pr.EffectiveSelfDeathChance;
                    entry.decayMod += pr.OffensiveDecayModifier;
                    strategyStats[strategy] = entry;

                    // Mutation totals by strategy
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

                        // Mutation impact tracking
                        if (!mutationImpact.ContainsKey(kv.Key))
                            mutationImpact[kv.Key] = (0, 0, 0);
                        var mi = mutationImpact[kv.Key];
                        if (strategy == winnerStrategy) mi.winsWithMutation++;
                        mi.uses++;
                        mi.totalLevel += kv.Value;
                        mutationImpact[kv.Key] = mi;
                    }
                }
            }

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");

            Console.WriteLine("\nStrategy Summary:");
            Console.WriteLine("Strategy                             | WinRate | Avg Alive | Avg Dead | Games | Avg MP Spent | Growth% | SelfDeath% | DecayMod");
            Console.WriteLine("-------------------------------------|---------|-----------|----------|-------|--------------|---------|------------|----------");

            foreach (var kvp in strategyStats.OrderByDescending(kvp => kvp.Value.wins))
            {
                var (wins, games, living, dead, mpSpent, growth, selfDeath, decayMod) = kvp.Value;
                float winRate = (float)wins / games * 100;
                float avgLiving = (float)living / games;
                float avgDead = (float)dead / games;
                float avgMpSpent = (float)mpSpent / games;
                float avgGrowth = growth / games * 100f;
                float avgSelfDeath = selfDeath / games * 100f;
                float avgDecay = decayMod / games * 100f;

                Console.WriteLine($"{kvp.Key,-37} | {winRate,6:F1}% | {avgLiving,9:F1} | {avgDead,8:F1} | {games,5} | {avgMpSpent,12:F1} | {avgGrowth,7:F2}% | {avgSelfDeath,10:F2}% | {avgDecay,8:F2}%");
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

            Console.WriteLine("\nMutation Impact Analysis:");
            Console.WriteLine("Mutation Name                   | WinRate | Uses | Avg Level in Wins");
            Console.WriteLine("--------------------------------|---------|------|-------------------");
            foreach (var kv in mutationImpact.OrderByDescending(kv => kv.Value.winsWithMutation))
            {
                var mutation = MutationRegistry.GetById(kv.Key);
                string name = mutation?.Name ?? $"[ID {kv.Key}]";
                float winRate = (float)kv.Value.winsWithMutation / totalGames * 100;
                float avgLevel = kv.Value.winsWithMutation > 0 ? (float)kv.Value.totalLevel / kv.Value.winsWithMutation : 0f;
                Console.WriteLine($"{name,-32} | {winRate,6:F1}% | {kv.Value.uses,4} | {avgLevel,17:F2}");
            }
        }
    }
}
