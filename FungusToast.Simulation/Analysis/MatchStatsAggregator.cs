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

            var deathReasonCounts = new Dictionary<DeathReason, int>();

            //var mutationUsageByStrategy = new Dictionary<string, Dictionary<int, List<(int level, bool isWinner)>>>();

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

                    // Track mutation usage per strategy
                    string strategyKey = pr.Strategy.StrategyName;
                    /*
                    if (!mutationUsageByStrategy.ContainsKey(strategyKey))
                        mutationUsageByStrategy[strategyKey] = new Dictionary<int, List<(int, bool)>>();

                    foreach (var kv in pr.MutationLevels)
                    {
                        if (!mutationUsageByStrategy[strategyKey].ContainsKey(kv.Key))
                            mutationUsageByStrategy[strategyKey][kv.Key] = new List<(int, bool)>();

                        mutationUsageByStrategy[strategyKey][kv.Key].Add((kv.Value, isWinner));
                    }*/
                }
            }

            foreach (DeathReason reason in Enum.GetValues(typeof(DeathReason)))
                if (!deathReasonCounts.ContainsKey(reason))
                    deathReasonCounts[reason] = 0;

            PrintGameLevelStats(results, totalCells);
            PrintDeathReasonSummary(deathReasonCounts);
            PrintPlayerSummaryTable(playerStats);
            //PrintMutationUsageByStrategy(mutationUsageByStrategy);
        }


        private void PrintGameLevelStats(List<GameResult> results, int totalCells)
        {
            int totalGames = results.Count;
            float avgLivingCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.LivingCells));
            float avgDeadCells = (float)results.Average(r => r.PlayerResults.Sum(p => p.DeadCells));
            float avgEmptyCells = totalCells - avgLivingCells - avgDeadCells;
            float avgSporesDropped = results.Sum(r => r.SporesFromSporocidalBloom.Values.Sum()) / totalGames;
            float avgToxicTiles = (float)results.Average(r => r.ToxicTileCount);
            float avgTurns = (float)results.Average(r => r.TurnsPlayed);
            double stdDevTurns = Math.Sqrt(results.Sum(r => Math.Pow(r.TurnsPlayed - avgTurns, 2)) / Math.Max(1, totalGames - 1));

            Console.WriteLine($"\nTotal Games Played: {totalGames}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:F1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:F2}");
            Console.WriteLine($"Avg Living Cells:   {avgLivingCells:F1}");
            Console.WriteLine($"Avg Dead Cells:     {avgDeadCells:F1}");
            Console.WriteLine($"Avg Empty Cells:    {avgEmptyCells:F1}");
            Console.WriteLine($"Avg Spores Dropped: {avgSporesDropped:F1}");
            Console.WriteLine($"Avg Lingering Toxic Tiles: {avgToxicTiles:F1}");
        }

        private void PrintPlayerSummaryTable(Dictionary<int, (
            IMutationSpendingStrategy strategyObj, int wins, int appearances,
            int living, int dead, int reclaims, int moldMoves,
            int sporesBloom, int sporesNecro, int sporesNecrophytic,
            int reclaimsNecrophytic, int mpSpent,
            float growthChance, float selfDeathChance, float decayMod)> playerStats)
        {
            Console.WriteLine("\nPer-Player Summary:");
            Console.WriteLine(
                $"{"Player",6} | {"Strategy",-40} | {"MaxTier",7} | {"High?",5} | {"Growth?",7} | {"Resist?",7} | {"Fungi?",6} | {"Drift?",6} | " +
                $"{"WinRate",7} | {"Avg Alive",10} | {"Avg Dead",9} | {"Avg MP Spent",13} | " +
                $"{"Growth%",7} | {"SelfDeath%",11} | {"DecayMod",9}");

            Console.WriteLine(new string('-', 150));

            foreach (var (id, entry) in playerStats.OrderByDescending(kvp => (float)kvp.Value.wins / kvp.Value.appearances))
            {
                var (strategyObj, wins, appearances, living, dead, _, _, _, _, _, _, mpSpent, growth, selfDeath, decayMod) = entry;

                float winRate = appearances > 0 ? (float)wins / appearances * 100f : 0f;

                Console.WriteLine(
                    $"{id,6} | {Truncate(strategyObj.StrategyName, 40),-40} | {strategyObj.MaxTier?.ToString() ?? "-",7} | " +
                    $"{BoolFlag(strategyObj.PrioritizeHighTier),5} | {BoolFlag(strategyObj.UsesGrowth),7} | {BoolFlag(strategyObj.UsesCellularResilience),7} | " +
                    $"{BoolFlag(strategyObj.UsesFungicide),6} | {BoolFlag(strategyObj.UsesGeneticDrift),6} | " +
                    $"{winRate,6:F1}% | {(float)living / appearances,10:F1} | {(float)dead / appearances,9:F1} | " +
                    $"{(float)mpSpent / appearances,13:F1} | {growth / appearances * 100f,6:F2}% | {selfDeath / appearances * 100f,10:F2}% | {decayMod / appearances * 100f,8:F2}%");
            }

            Console.WriteLine(new string('-', 150));
        }



        private void PrintDeathReasonSummary(Dictionary<DeathReason, int> deathReasonCounts)
        {
            int totalDeaths = deathReasonCounts.Values.Sum();

            Console.WriteLine("\nDeath Reason Summary:");
            Console.WriteLine($"Total Cells that Died: {totalDeaths}");
            Console.WriteLine($"{"Cause",-30} | {"Count",5} | {"Percent",7}");
            Console.WriteLine(new string('-', 45));

            foreach (var kv in deathReasonCounts.OrderByDescending(kv => kv.Value))
            {
                string cause = kv.Key.ToString();
                int count = kv.Value;
                float percent = totalDeaths > 0 ? (float)count / totalDeaths * 100f : 0f;
                Console.WriteLine($"{cause,-30} | {count,5} | {percent,6:F1}%");
            }
        }


        private string BoolFlag(bool? val) => val == true ? "Y" : "N";

        private string Truncate(string s, int max) =>
            s.Length > max ? s.Substring(0, max - 1) + "…" : s;


    }
}
