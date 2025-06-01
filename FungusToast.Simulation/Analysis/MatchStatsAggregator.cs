using FungusToast.Core.AI;
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

            // Per-player stats map
            var playerStats = new Dictionary<int, (
                IMutationSpendingStrategy strategyObj,
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

            // Death reason summary
            var deathReasonCounts = new Dictionary<DeathReason, int>();

            foreach (var result in results)
            {
                foreach (var pr in result.PlayerResults)
                {
                    int id = pr.PlayerId;
                    bool isWinner = id == result.WinnerId;

                    if (!playerStats.ContainsKey(id))
                        playerStats[id] = (pr.Strategy, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f);

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
                    }
                }
            }

            // Ensure all defined death reasons appear
            foreach (DeathReason reason in Enum.GetValues(typeof(DeathReason)))
                if (!deathReasonCounts.ContainsKey(reason))
                    deathReasonCounts[reason] = 0;

            // Game-level stats
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

            // Death summary
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

            // Per-player table header
            Console.WriteLine("\nPer-Player Summary:");
            Console.WriteLine(
                $"{"Player",6} | {"Strategy",-40} | {"MaxTier",7} | {"High?",5} | {"Growth?",7} | {"Resist?",7} | {"Fungi?",6} | {"Drift?",6} | " +
                $"{"WinRate",7} | {"Avg Alive",10} | {"Avg Dead",9} | {"Avg Reclaims",14} | {"Avg Mold Moves",15} | " +
                $"{"Avg Bloom Spores",17} | {"Avg Necro Spores",17} | {"Avg Necrophytic",15} | {"Avg Reclaims NP",15} | " +
                $"{"Avg MP Spent",13} | {"Growth%",7} | {"SelfDeath%",11} | {"DecayMod",9}");

            Console.WriteLine(new string('-', 6) + "-|-" + new string('-', 40) + "-|-" + new string('-', 7) + "-|-" +
                              new string('-', 5) + "-|-" + new string('-', 7) + "-|-" + new string('-', 7) + "-|-" +
                              new string('-', 6) + "-|-" + new string('-', 6) + "-|-" +
                              new string('-', 7) + "-|-" + new string('-', 10) + "-|-" + new string('-', 9) + "-|-" +
                              new string('-', 14) + "-|-" + new string('-', 15) + "-|-" +
                              new string('-', 17) + "-|-" + new string('-', 17) + "-|-" +
                              new string('-', 15) + "-|-" + new string('-', 15) + "-|-" +
                              new string('-', 13) + "-|-" + new string('-', 7) + "-|-" + new string('-', 11) + "-|-" +
                              new string('-', 9));


            foreach (var (id, entry) in playerStats.OrderByDescending(kvp => (float)kvp.Value.wins / kvp.Value.appearances))
            {
                var (strategyObj, wins, appearances, living, dead, reclaims, moldMoves,
                     sporesBloom, sporesNecro, sporesNecrophytic, reclaimsNecrophytic,
                     mpSpent, growth, selfDeath, decayMod) = entry;

                float winRate = appearances > 0 ? (float)wins / appearances * 100f : 0f;
                float avgLiving = (float)living / appearances;
                float avgDead = (float)dead / appearances;
                float avgRecl = (float)reclaims / appearances;
                float avgMoves = (float)moldMoves / appearances;
                float avgSBloom = (float)sporesBloom / appearances;
                float avgSNecro = (float)sporesNecro / appearances;
                float avgSNeoP = (float)sporesNecrophytic / appearances;
                float avgRNeoP = (float)reclaimsNecrophytic / appearances;
                float avgMP = (float)mpSpent / appearances;
                float avgGrow = growth / appearances * 100f;
                float avgSelf = selfDeath / appearances * 100f;
                float avgDecay = decayMod / appearances * 100f;

                string maxTier = strategyObj.MaxTier?.ToString() ?? "-";
                string highTier = strategyObj.PrioritizeHighTier == true ? "Y" : "N";
                string usesGrowth = strategyObj.UsesGrowth == true ? "Y" : "N";
                string resist = strategyObj.UsesCellularResilience == true ? "Y" : "N";
                string fungi = strategyObj.UsesFungicide == true ? "Y" : "N";
                string drift = strategyObj.UsesGeneticDrift == true ? "Y" : "N";

                string displayName = strategyObj.StrategyName.Length > 40
                    ? strategyObj.StrategyName.Substring(0, 39) + "…"
                    : strategyObj.StrategyName;

                Console.WriteLine(
                    $"{id,6} | {displayName,-40} | {maxTier,7} | {highTier,5} | {usesGrowth,7} | {resist,7} | {fungi,6} | {drift,6} | " +
                    $"{winRate,6:F1}% | {avgLiving,10:F1} | {avgDead,9:F1} | {avgRecl,14:F1} | {avgMoves,15:F1} | " +
                    $"{avgSBloom,17:F1} | {avgSNecro,17:F1} | {avgSNeoP,15:F1} | {avgRNeoP,15:F1} | " +
                    $"{avgMP,13:F1} | {avgGrow,6:F2}% | {avgSelf,10:F2}% | {avgDecay,8:F2}%");
            }

            Console.WriteLine(new string('-', 230));
        }


    }
}
