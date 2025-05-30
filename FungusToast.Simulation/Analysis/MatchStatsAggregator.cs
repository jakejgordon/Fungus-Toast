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
        // MatchupStatsAggregator.cs  –  class MatchupStatsAggregator
        // -----------------------------------------------------------------------------
        // REPLACES the entire PrintSummary method. No other changes required.

        public void PrintSummary(List<GameResult> results)
        {
            // Board-size header
            int boardWidth = GameBalance.BoardWidth;
            int boardHeight = GameBalance.BoardHeight;
            int totalCells = boardWidth * boardHeight;

            Console.WriteLine($"\nBoard Width:        {boardWidth}");
            Console.WriteLine($"Board Height:       {boardHeight}");
            Console.WriteLine($"Total Cells:        {totalCells}");

            // Aggregate game-level metrics
            int totalGames = results.Count;
            int totalTurns = results.Sum(r => r.TurnsPlayed);
            float avgTurns = totalGames > 0 ? (float)totalTurns / totalGames : 0;
            double sumSquaredDiffs = results.Sum(r => Math.Pow(r.TurnsPlayed - avgTurns, 2));
            double stdDevTurns = totalGames > 1 ? Math.Sqrt(sumSquaredDiffs / (totalGames - 1)) : 0;

            // Per-player accumulation
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
                int sporesFromNecrophytic,
                int reclaimsFromNecrophytic,
                int mutationPointsSpent,
                float growthChance,
                float selfDeathChance,
                float decayMod)>();

            // Death-reason counts across all games
            var deathReasonCounts = new Dictionary<DeathReason, int>();

            foreach (var result in results)
            {
                foreach (var pr in result.PlayerResults)
                {
                    int id = pr.PlayerId;
                    bool isWinner = id == result.WinnerId;

                    if (!playerStats.ContainsKey(id))
                        playerStats[id] = (pr.StrategyName, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f);

                    var entry = playerStats[id];
                    entry.appearances++;
                    if (isWinner) entry.wins++;

                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;
                    entry.totalReclaims += pr.ReclaimedCells;
                    entry.totalMoldMoves += pr.CreepingMoldMoves;
                    entry.sporesFromBloom += pr.SporocidalSpores;
                    entry.sporesFromNecro += pr.NecroSpores;
                    entry.sporesFromNecrophytic += pr.NecrophyticSpores;
                    entry.reclaimsFromNecrophytic += pr.NecrophyticReclaims;
                    entry.mutationPointsSpent += pr.MutationLevels.Sum(kv =>
                                                  (MutationRegistry.GetById(kv.Key)?.PointsPerUpgrade ?? 0) * kv.Value);
                    entry.growthChance += pr.EffectiveGrowthChance;
                    entry.selfDeathChance += pr.EffectiveSelfDeathChance;
                    entry.decayMod += pr.OffensiveDecayModifier;
                    playerStats[id] = entry;

                    if (pr.DeadCellDeathReasons != null)
                    {
                        foreach (DeathReason reason in pr.DeadCellDeathReasons)
                        {
                            if (!deathReasonCounts.ContainsKey(reason))
                                deathReasonCounts[reason] = 0;
                            deathReasonCounts[reason]++;
                        }

                        // Ensure all defined DeathReasons are represented
                        foreach (DeathReason reason in Enum.GetValues(typeof(DeathReason)))
                        {
                            if (!deathReasonCounts.ContainsKey(reason))
                                deathReasonCounts[reason] = 0;
                        }

                    }
                }
            }

            // Game-level averages
            float avgLivingCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.LivingCells));
            float avgDeadCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.DeadCells));
            float avgEmptyCells = totalCells - avgLivingCells - avgDeadCells;
            float avgSporesDropped = (float)results.Sum(r => r.SporesFromSporocidalBloom.Values.Sum()) / totalGames;
            float avgToxicTiles = (float)results.Average(r => r.ToxicTileCount);

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");
            Console.WriteLine($"Avg Living Cells:   {avgLivingCells:F1}");
            Console.WriteLine($"Avg Dead Cells:     {avgDeadCells:F1}");
            Console.WriteLine($"Avg Empty Cells:    {avgEmptyCells:F1}");
            Console.WriteLine($"Avg Spores Dropped: {avgSporesDropped:F1}");
            Console.WriteLine($"Avg Lingering Toxic Tiles: {avgToxicTiles:F1}");

            // -------- Death-reason summary with percentage of total ---------------
            int totalDeaths = deathReasonCounts.Values.Sum();
            Console.WriteLine("\nDeath Reason Summary:");
            Console.WriteLine($"Total Cells that Died: {totalDeaths}");
            Console.WriteLine("Cause                        | Count | Percent");
            Console.WriteLine("-----------------------------|-------|--------");
            foreach (var kv in deathReasonCounts.OrderByDescending(kv => kv.Value))
            {
                float pct = totalDeaths > 0 ? kv.Value * 100f / totalDeaths : 0f;
                Console.WriteLine($"{kv.Key,-28} | {kv.Value,5} | {pct,6:F1}%");
            }

            // ------------------------- Per-player table ---------------------------
            Console.WriteLine("\nPer-Player Summary:");
            Console.WriteLine(
                $"{"Player",6} | {"Strategy",-44} | {"WinRate",7} | {"Avg Alive",10} | {"Avg Dead",9} | {"Avg Reclaims",14} | {"Avg Mold Moves",15} | {"Avg Bloom Spores",17} | {"Avg Necro Spores",17} | {"Avg Necrophytic",15} | {"Avg Reclaims NP",15} | {"Avg MP Spent",13} | {"Growth%",7} | {"SelfDeath%",11} | {"DecayMod",9}");
            Console.WriteLine(new string('-', 6) + "-|-" + new string('-', 44) + "-|-" + new string('-', 7) + "-|-" +
                              new string('-', 10) + "-|-" + new string('-', 9) + "-|-" + new string('-', 14) + "-|-" +
                              new string('-', 15) + "-|-" + new string('-', 17) + "-|-" + new string('-', 17) + "-|-" +
                              new string('-', 15) + "-|-" + new string('-', 15) + "-|-" + new string('-', 13) + "-|-" +
                              new string('-', 7) + "-|-" + new string('-', 11) + "-|-" + new string('-', 9));

            foreach (var kvp in playerStats
                     .OrderByDescending(kvp => (float)kvp.Value.wins / kvp.Value.appearances))
            {
                int id = kvp.Key;
                var (strategy, wins, appearances, living, dead, reclaims, moldMoves,
                     sporesBloom, sporesNecro, sporesNecrophytic, reclaimsNecrophytic,
                     mpSpent, growth, selfDeath, decayMod) = kvp.Value;

                float winRate = appearances > 0 ? (float)wins / appearances * 100f : 0f;
                float avgLiving = appearances > 0 ? (float)living / appearances : 0f;
                float avgDead = appearances > 0 ? (float)dead / appearances : 0f;
                float avgRecl = appearances > 0 ? (float)reclaims / appearances : 0f;
                float avgMoves = appearances > 0 ? (float)moldMoves / appearances : 0f;
                float avgSBloom = appearances > 0 ? (float)sporesBloom / appearances : 0f;
                float avgSNecro = appearances > 0 ? (float)sporesNecro / appearances : 0f;
                float avgSNeoP = appearances > 0 ? (float)sporesNecrophytic / appearances : 0f;
                float avgRNeoP = appearances > 0 ? (float)reclaimsNecrophytic / appearances : 0f;
                float avgMP = appearances > 0 ? (float)mpSpent / appearances : 0f;
                float avgGrow = appearances > 0 ? growth / appearances * 100f : 0f;
                float avgSelf = appearances > 0 ? selfDeath / appearances * 100f : 0f;
                float avgDecay = appearances > 0 ? decayMod / appearances * 100f : 0f;

                Console.WriteLine(
                    $"{id,6} | {strategy,-44} | {winRate,6:F1}% | {avgLiving,10:F1} | {avgDead,9:F1} | {avgRecl,14:F1} | {avgMoves,15:F1} | {avgSBloom,17:F1} | {avgSNecro,17:F1} | {avgSNeoP,15:F1} | {avgRNeoP,15:F1} | {avgMP,13:F1} | {avgGrow,6:F2}% | {avgSelf,10:F2}% | {avgDecay,8:F2}%");
            }

            Console.WriteLine(new string('-', 230));
        }

    }
}
