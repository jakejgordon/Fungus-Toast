using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Board;
using FungusToast.Core.AI;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class MatchupStatsAggregator
    {
        private void PrintDeathReasonSummaryFloat(
            Dictionary<DeathReason, float> avgDeathReasons,
            string label,
            int gameCount)
        {
            float totalDeaths = avgDeathReasons.Values.Sum();
            float avgTotalDeaths = gameCount > 0 ? totalDeaths / gameCount : 0f;

            Console.WriteLine($"\n=== {label} ===");
            Console.WriteLine($"Avg Cells that Died per Game: {avgTotalDeaths:N1}");
            Console.WriteLine($"{"Cause",-30} | {"Avg Count",13} | {"Percent",9}");
            Console.WriteLine(new string('-', 57));

            foreach (var kv in avgDeathReasons.OrderByDescending(kv => kv.Value))
            {
                string cause = kv.Key.ToString();
                float avgCount = kv.Value;
                float percent = totalDeaths > 0 ? avgCount / totalDeaths * 100f : 0f;
                Console.WriteLine($"{cause,-30} | {avgCount,13:N1} | {percent,8:N1}%");
            }
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

            Console.WriteLine($"\n=== Game-Level Stats ===");
            Console.WriteLine($"Total Games Played: {totalGames:N0}");
            Console.WriteLine($"Avg Turns per Game: {avgTurns:N1}");
            Console.WriteLine($"Std Dev of Turns:   {stdDevTurns:N2}");
            Console.WriteLine($"Avg Living Cells:   {avgLivingCells:N1}");
            Console.WriteLine($"Avg Dead Cells:     {avgDeadCells:N1}");
            Console.WriteLine($"Avg Empty Cells:    {avgEmptyCells:N1}");
            Console.WriteLine($"Avg Spores Dropped: {avgSporesDropped:N1}");
            Console.WriteLine($"Avg Lingering Toxic Tiles: {avgToxicTiles:N1}");
        }


        public void PrintSummary(
            List<GameResult> results,
            Dictionary<DeathReason, int> cumulativeDeathReasons // only for end-state legacy
        )
        {
            int boardWidth = GameBalance.BoardWidth;
            int boardHeight = GameBalance.BoardHeight;
            int totalCells = boardWidth * boardHeight;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            Console.WriteLine($"\n=== Fungus Toast Simulation Summary ===");
            Console.WriteLine($"Simulation Date/Time: {timestamp}");
            Console.WriteLine($"Board Width:        {boardWidth:N0}");
            Console.WriteLine($"Board Height:       {boardHeight:N0}");
            Console.WriteLine($"Total Cells:        {totalCells:N0}");

            var playerStats = new Dictionary<int, (
                IMutationSpendingStrategy strategyObj,
                int wins,
                int appearances,
                int totalLiving,
                int totalDead,
                int mutationPointsSpent,
                float growthChance,
                float selfDeathChance,
                float decayMod)>();

            var endStateDeathReasonCounts = new Dictionary<DeathReason, int>();

            foreach (var result in results)
            {
                foreach (var pr in result.PlayerResults)
                {
                    int id = pr.PlayerId;
                    bool isWinner = id == result.WinnerId;

                    if (!playerStats.ContainsKey(id))
                    {
                        playerStats[id] = (
                            strategyObj: pr.Strategy,
                            wins: 0,
                            appearances: 0,
                            totalLiving: 0,
                            totalDead: 0,
                            mutationPointsSpent: 0,
                            growthChance: 0f,
                            selfDeathChance: 0f,
                            decayMod: 0f
                        );
                    }

                    var entry = playerStats[id];
                    entry.appearances++;
                    if (isWinner) entry.wins++;

                    entry.totalLiving += pr.LivingCells;
                    entry.totalDead += pr.DeadCells;
                    entry.growthChance += pr.EffectiveGrowthChance;
                    entry.selfDeathChance += pr.EffectiveSelfDeathChance;
                    entry.decayMod += pr.OffensiveDecayModifier;
                    playerStats[id] = entry;

                    if (pr.DeadCellDeathReasons != null)
                    {
                        foreach (DeathReason reason in pr.DeadCellDeathReasons)
                        {
                            if (!endStateDeathReasonCounts.ContainsKey(reason))
                                endStateDeathReasonCounts[reason] = 0;
                            endStateDeathReasonCounts[reason]++;
                        }
                    }
                }
            }

            foreach (DeathReason reason in Enum.GetValues(typeof(DeathReason)))
            {
                if (!endStateDeathReasonCounts.ContainsKey(reason))
                    endStateDeathReasonCounts[reason] = 0;
                if (!cumulativeDeathReasons.ContainsKey(reason))
                    cumulativeDeathReasons[reason] = 0;
            }

            PrintGameLevelStats(results, totalCells);

            int numGames = results.Count;

            // -- NEW: Calculate cumulative death reasons as per-game averages --
            var deathReasonTotalsByGame = new List<Dictionary<DeathReason, int>>();
            foreach (var result in results)
            {
                var reasonTotals = new Dictionary<DeathReason, int>();
                foreach (var pr in result.PlayerResults)
                {
                    if (pr.DeathsByReason == null) continue;
                    foreach (var kvp in pr.DeathsByReason)
                    {
                        if (!reasonTotals.ContainsKey(kvp.Key))
                            reasonTotals[kvp.Key] = 0;
                        reasonTotals[kvp.Key] += kvp.Value;
                    }
                }
                deathReasonTotalsByGame.Add(reasonTotals);
            }
            // Average across games
            var avgDeathReasons = new Dictionary<DeathReason, float>();
            foreach (DeathReason reason in Enum.GetValues(typeof(DeathReason)))
            {
                float total = deathReasonTotalsByGame.Sum(dict => dict.ContainsKey(reason) ? dict[reason] : 0);
                avgDeathReasons[reason] = numGames > 0 ? total / numGames : 0f;
            }

            // Print the summary using our per-game averages
            PrintDeathReasonSummaryFloat(avgDeathReasons, "Cumulative Death Reason Summary", 1); // gameCount=1 to avoid dividing again
            PrintDeathReasonSummary(endStateDeathReasonCounts, "End-State Death Reason Summary (At Game End)", numGames);

            PrintPlayerSummaryTable(playerStats, results);
        }

        private void PrintPlayerSummaryTable(
            Dictionary<int, (
                IMutationSpendingStrategy strategyObj, int wins, int appearances,
                int living, int dead, int mpSpent,
                float growthChance, float selfDeathChance, float decayMod)> playerStats,
            List<GameResult> gameResults
        )
        {
            var rankedPlayerList = GetRankedPlayerList(gameResults);

            var totalMpSpentByPlayer = new Dictionary<int, int>();
            var totalMpEarnedByPlayer = new Dictionary<int, int>();
            var totalAutoupgradeMpByPlayer = new Dictionary<int, int>();

            foreach (var game in gameResults)
            {
                var tracking = game.TrackingContext;
                if (tracking == null) continue;

                foreach (var kvp in tracking.GetAllMutationPointsSpentByTier())
                {
                    int playerId = kvp.Key;
                    int sum = kvp.Value.Values.Sum();
                    if (!totalMpSpentByPlayer.ContainsKey(playerId))
                        totalMpSpentByPlayer[playerId] = 0;
                    totalMpSpentByPlayer[playerId] += sum;
                }
                foreach (var kvp in tracking.GetAllMutationPointIncome())
                {
                    int playerId = kvp.Key;
                    if (!totalMpEarnedByPlayer.ContainsKey(playerId))
                        totalMpEarnedByPlayer[playerId] = 0;
                    totalMpEarnedByPlayer[playerId] += kvp.Value;
                }
                
                // Calculate total autoupgrade mutation points
                foreach (var player in game.PlayerResults)
                {
                    int playerId = player.PlayerId;
                    int autoupgradeMp = player.MutatorPhenotypePointsEarned + player.HyperadaptiveDriftPointsEarned;
                    
                    if (!totalAutoupgradeMpByPlayer.ContainsKey(playerId))
                        totalAutoupgradeMpByPlayer[playerId] = 0;
                    totalAutoupgradeMpByPlayer[playerId] += autoupgradeMp;
                }
            }

            Console.WriteLine("\n=== Per-Player Summary ===");
            Console.WriteLine(
                $"{"Player",6} | {"Strategy",-40} | {"WinRate",7} | {"Avg Alive",13} | {"Avg Dead",13} | " +
                $"{"Avg MP Spent",16} | {"Avg MP Earned",16} | {"Avg Autoupgrade MP",20} | " +
                $"{"Growth%",11} | {"SelfDeath%",13} | {"DecayMod",10}");
            Console.WriteLine(new string('-', 211));

            foreach (var (id, strategyName) in rankedPlayerList)
            {
                if (!playerStats.TryGetValue(id, out var entry))
                    continue;

                var (
                    strategyObj, wins, appearances, living, dead,
                    mpSpent, growth, selfDeath, decayMod
                ) = entry;

                float winRate = appearances > 0 ? (float)wins / appearances * 100f : 0f;

                int totalMpSpent = totalMpSpentByPlayer.TryGetValue(id, out var v1) ? v1 : 0;
                int totalMpEarned = totalMpEarnedByPlayer.TryGetValue(id, out var v2) ? v2 : 0;
                int totalAutoupgradeMp = totalAutoupgradeMpByPlayer.TryGetValue(id, out var v3) ? v3 : 0;

                float avgMpSpent = appearances > 0 ? (float)totalMpSpent / appearances : 0f;
                float avgMpEarned = appearances > 0 ? (float)totalMpEarned / appearances : 0f;
                float avgAutoupgradeMp = appearances > 0 ? (float)totalAutoupgradeMp / appearances : 0f;

                Console.WriteLine(
                    $"{id,6} | {Truncate(strategyObj.StrategyName, 40),-40} | {winRate,6:N1}% | " +
                    $"{(float)living / appearances,13:N1} | {(float)dead / appearances,13:N1} | " +
                    $"{avgMpSpent,16:N1} | {avgMpEarned,16:N1} | {avgAutoupgradeMp,20:N1} | " +
                    $"{growth / appearances * 100f,10:N2}% | {selfDeath / appearances * 100f,12:N2}% | {decayMod / appearances,9:N2}%");
            }

            Console.WriteLine(new string('-', 211));
        }

        private void PrintDeathReasonSummary(
            Dictionary<DeathReason, int> deathReasonCounts,
            string label,
            int gameCount)
        {
            int totalDeaths = deathReasonCounts.Values.Sum();
            float avgTotalDeaths = gameCount > 0 ? (float)totalDeaths / gameCount : 0f;

            Console.WriteLine($"\n=== {label} ===");
            Console.WriteLine($"Avg Cells that Died per Game: {avgTotalDeaths:N1}");
            Console.WriteLine($"{"Cause",-30} | {"Avg Count",13} | {"Percent",9}");
            Console.WriteLine(new string('-', 57));

            foreach (var kv in deathReasonCounts.OrderByDescending(kv => kv.Value))
            {
                string cause = kv.Key.ToString();
                float avgCount = gameCount > 0 ? (float)kv.Value / gameCount : 0f;
                float percent = totalDeaths > 0 ? (float)kv.Value / totalDeaths * 100f : 0f;
                Console.WriteLine($"{cause,-30} | {avgCount,13:N1} | {percent,8:N1}%");
            }
        }

        private string Truncate(string s, int max) =>
            s.Length > max ? s.Substring(0, max - 1) + "…" : s;

        public static List<(int PlayerId, string StrategyName)> GetRankedPlayerList(List<GameResult> gameResults)
        {
            var playerGroups = gameResults
                .SelectMany(r => r.PlayerResults.Select(pr => new
                {
                    pr.PlayerId,
                    StrategyName = pr.StrategyName ?? "None",
                    pr.LivingCells,
                    WinnerId = r.WinnerId
                }))
                .GroupBy(x => (x.PlayerId, x.StrategyName))
                .Select(g =>
                {
                    int wins = g.Count(x => x.PlayerId == x.WinnerId);
                    double avgAlive = g.Average(x => x.LivingCells);
                    return new
                    {
                        PlayerId = g.Key.PlayerId,
                        StrategyName = g.Key.StrategyName,
                        Wins = wins,
                        AvgAlive = avgAlive
                    };
                })
                .OrderByDescending(x => x.Wins)
                .ThenByDescending(x => x.AvgAlive)
                .Select(x => (x.PlayerId, x.StrategyName))
                .ToList();

            return playerGroups;
        }
    }
}
