using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Death;
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

            var playerStats = new Dictionary<int, (string strategy, int wins, int appearances, int totalLiving, int totalDead, int totalReclaims, int mutationPointsSpent, float growthChance, float selfDeathChance, float decayMod)>();
            var deathReasonCounts = new Dictionary<DeathReason, int>();

            foreach (var result in results)
            {
                foreach (var pr in result.PlayerResults)
                {
                    int playerId = pr.PlayerId;
                    bool isWinner = pr.PlayerId == result.WinnerId;
                    if (!playerStats.ContainsKey(playerId))
                        playerStats[playerId] = (pr.StrategyName, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f);

                    var entry = playerStats[playerId];
                    entry.appearances++;
                    if (isWinner) entry.wins++;
                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;
                    entry.totalReclaims += pr.ReclaimedCells;
                    entry.mutationPointsSpent += pr.MutationLevels.Sum(kv => (MutationRegistry.GetById(kv.Key)?.PointsPerUpgrade ?? 0) * kv.Value);
                    entry.growthChance += pr.EffectiveGrowthChance;
                    entry.selfDeathChance += pr.EffectiveSelfDeathChance;
                    entry.decayMod += pr.OffensiveDecayModifier;
                    playerStats[playerId] = entry;

                    if (pr.DeadCellDeathReasons != null)
                    {
                        foreach (var reason in pr.DeadCellDeathReasons)
                        {
                            if (!deathReasonCounts.ContainsKey(reason))
                                deathReasonCounts[reason] = 0;
                            deathReasonCounts[reason]++;
                        }
                    }
                }
            }

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");

            Console.WriteLine("\nPer-Player Summary:");
            Console.WriteLine("Player | Strategy                             | WinRate | Avg Alive | Avg Dead | Avg Reclaims | Avg MP Spent | Growth% | SelfDeath% | DecayMod");
            Console.WriteLine("-------|--------------------------------------|---------|-----------|----------|--------------|---------------|---------|------------|----------");

            foreach (var kvp in playerStats.OrderBy(k => k.Key))
            {
                int playerId = kvp.Key;
                var (strategy, wins, appearances, living, dead, reclaims, mpSpent, growth, selfDeath, decayMod) = kvp.Value;

                float winRate = (float)wins / appearances * 100;
                float avgLiving = (float)living / appearances;
                float avgDead = (float)dead / appearances;
                float avgReclaims = (float)reclaims / appearances;
                float avgMpSpent = (float)mpSpent / appearances;
                float avgGrowth = growth / appearances * 100f;
                float avgSelfDeath = selfDeath / appearances * 100f;
                float avgDecay = decayMod / appearances * 100f;

                Console.WriteLine($"{playerId,6} | {strategy,-38} | {winRate,6:F1}% | {avgLiving,9:F1} | {avgDead,8:F1} | {avgReclaims,12:F1} | {avgMpSpent,13:F1} | {avgGrowth,7:F2}% | {avgSelfDeath,10:F2}% | {avgDecay,8:F2}%");
            }

            Console.WriteLine("\nDeath Reason Summary:");
            Console.WriteLine("Cause                        | Count");
            Console.WriteLine("-----------------------------|--------");
            foreach (var kv in deathReasonCounts.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"{kv.Key,-28} | {kv.Value,6}");
            }
        }
    }
}
