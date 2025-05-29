using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var playerStats = new Dictionary<int, (
                string strategy,
                int wins,
                int appearances,
                int totalLiving,
                int totalDead,
                int totalReclaims,
                int totalMoldMoves,
                int sporesFromBloom,
                int sporesFromNecro,
                int mutationPointsSpent,
                float growthChance,
                float selfDeathChance,
                float decayMod)>();

            var deathReasonCounts = new Dictionary<DeathReason, int>();

            foreach (var result in results)
            {
                foreach (var pr in result.PlayerResults)
                {
                    int playerId = pr.PlayerId;
                    bool isWinner = pr.PlayerId == result.WinnerId;
                    if (!playerStats.ContainsKey(playerId))
                        playerStats[playerId] = (
                            pr.StrategyName, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f);

                    var entry = playerStats[playerId];
                    entry.appearances++;
                    if (isWinner) entry.wins++;
                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;
                    entry.totalReclaims += pr.ReclaimedCells;
                    entry.totalMoldMoves += pr.CreepingMoldMoves;
                    entry.sporesFromBloom += pr.SporocidalSpores;
                    entry.sporesFromNecro += pr.NecroSpores;
                    entry.mutationPointsSpent += pr.MutationLevels.Sum(kv =>
                        (MutationRegistry.GetById(kv.Key)?.PointsPerUpgrade ?? 0) * kv.Value);
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

            int boardTileCount = GameBalance.BoardWidth * GameBalance.BoardHeight;
            float avgLivingCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.LivingCells));
            float avgDeadCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.DeadCells));
            float avgEmptyCells = boardTileCount - avgLivingCells - avgDeadCells;
            float avgSporesDropped = (float)results.Sum(r => r.SporesFromSporocidalBloom.Values.Sum()) / results.Count;
            float avgToxicTiles = (float)results.Average(r => r.ToxicTileCount);

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");
            Console.WriteLine($"Avg Living Cells:   {avgLivingCells:F1}");
            Console.WriteLine($"Avg Dead Cells:     {avgDeadCells:F1}");
            Console.WriteLine($"Avg Empty Cells:    {avgEmptyCells:F1}");
            Console.WriteLine($"Avg Spores Dropped: {avgSporesDropped:F1}");
            Console.WriteLine($"Avg Lingering Toxic Tiles: {avgToxicTiles:F1}");

            Console.WriteLine("\nPer-Player Summary:");
            Console.WriteLine(
                $"{"Player",6} | {"Strategy",-44} | {"WinRate",7} | {"Avg Alive",10} | {"Avg Dead",9} | {"Avg Reclaims",14} | {"Avg Mold Moves",15} | {"Avg Bloom Spores",17} | {"Avg Necro Spores",17} | {"Avg MP Spent",13} | {"Growth%",7} | {"SelfDeath%",11} | {"DecayMod",9}"
            );
            Console.WriteLine(
                new string('-', 6) + "-|-" +
                new string('-', 44) + "-|-" +
                new string('-', 7) + "-|-" +
                new string('-', 10) + "-|-" +
                new string('-', 9) + "-|-" +
                new string('-', 14) + "-|-" +
                new string('-', 15) + "-|-" +
                new string('-', 17) + "-|-" +
                new string('-', 17) + "-|-" +
                new string('-', 13) + "-|-" +
                new string('-', 7) + "-|-" +
                new string('-', 11) + "-|-" +
                new string('-', 9)
            );

            float totalAppearances = playerStats.Values.Sum(v => v.appearances);
            float totalWins = playerStats.Values.Sum(v => v.wins);
            float sumLiving = playerStats.Values.Sum(v => v.totalLiving);
            float sumDead = playerStats.Values.Sum(v => v.totalDead);
            float sumReclaims = playerStats.Values.Sum(v => v.totalReclaims);
            float sumMoldMoves = playerStats.Values.Sum(v => v.totalMoldMoves);
            float sumBloom = playerStats.Values.Sum(v => v.sporesFromBloom);
            float sumNecro = playerStats.Values.Sum(v => v.sporesFromNecro);
            float sumMpSpent = playerStats.Values.Sum(v => v.mutationPointsSpent);
            float sumGrowth = playerStats.Values.Sum(v => v.growthChance);
            float sumSelfDeath = playerStats.Values.Sum(v => v.selfDeathChance);
            float sumDecayMod = playerStats.Values.Sum(v => v.decayMod);

            foreach (var kvp in playerStats
                .OrderByDescending(kvp => (float)kvp.Value.wins / kvp.Value.appearances))
            {
                int playerId = kvp.Key;
                var (strategy, wins, appearances, living, dead, reclaims, moldMoves, sporesBloom, sporesNecro, mpSpent, growth, selfDeath, decayMod) = kvp.Value;

                float winRate = (float)wins / appearances * 100;
                float avgLiving = (float)living / appearances;
                float avgDead = (float)dead / appearances;
                float avgReclaims = (float)reclaims / appearances;
                float avgMoldMoves = (float)moldMoves / appearances;
                float avgSporesBloom = (float)sporesBloom / appearances;
                float avgSporesNecro = (float)sporesNecro / appearances;
                float avgMpSpent = (float)mpSpent / appearances;
                float avgGrowth = growth / appearances * 100f;
                float avgSelfDeath = selfDeath / appearances * 100f;
                float avgDecay = decayMod / appearances * 100f;

                Console.WriteLine(
                    $"{playerId,6} | {strategy,-44} | {winRate,6:F1}% | {avgLiving,10:F1} | {avgDead,9:F1} | {avgReclaims,14:F1} | {avgMoldMoves,15:F1} | {avgSporesBloom,17:F1} | {avgSporesNecro,17:F1} | {avgMpSpent,13:F1} | {avgGrowth,6:F2}% | {avgSelfDeath,10:F2}% | {avgDecay,8:F2}%"
                );
            }

            Console.WriteLine(new string('-', 197));
            Console.WriteLine(
                $"{"Total",6} | {"(avg across all appearances)",-44} | {totalWins / totalAppearances * 100,6:F1}% | " +
                $"{sumLiving / totalAppearances,10:F1} | {sumDead / totalAppearances,9:F1} | {sumReclaims / totalAppearances,14:F1} | {sumMoldMoves / totalAppearances,15:F1} | " +
                $"{sumBloom / totalAppearances,17:F1} | {sumNecro / totalAppearances,17:F1} | {sumMpSpent / totalAppearances,13:F1} | " +
                $"{sumGrowth / totalAppearances * 100f,6:F2}% | {sumSelfDeath / totalAppearances * 100f,10:F2}% | {sumDecayMod / totalAppearances * 100f,8:F2}%"
            );

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
