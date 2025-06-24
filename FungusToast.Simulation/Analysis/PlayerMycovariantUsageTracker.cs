using FungusToast.Core.Config;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.Analysis
{
    public class PlayerMycovariantUsageTracker
    {
        // Each record stores effect counts as a dictionary by effect type (e.g., "Colonized", "Infested", "Toxified", etc.)
        private readonly List<(int PlayerId, string Strategy, string MycovariantName, string Type, string EffectType, int EffectValue, bool Triggered)> _records = new();

        public void TrackGameResult(GameResult result)
        {
            foreach (var pr in result.PlayerResults)
            {
                foreach (var myco in pr.Mycovariants)
                {
                    if (myco.EffectCounts != null && myco.EffectCounts.Count > 0)
                    {
                        foreach (var kvp in myco.EffectCounts)
                        {
                            _records.Add((
                                pr.PlayerId,
                                pr.StrategyName ?? "None",
                                myco.MycovariantName,
                                myco.MycovariantType,
                                kvp.Key,
                                kvp.Value,
                                myco.Triggered
                            ));
                        }
                    }
                    else
                    {
                        // Log mycovariant with "-" effect type if no effects
                        _records.Add((
                            pr.PlayerId,
                            pr.StrategyName ?? "None",
                            myco.MycovariantName,
                            myco.MycovariantType,
                            "-",
                            0,
                            myco.Triggered
                        ));
                    }
                }
            }
        }

        public void PrintReport(List<(int PlayerId, string StrategyName)> rankedPlayers)
        {
            Console.WriteLine("\nPlayer-Mycovariant Usage Summary (per Player, all games):");
            Console.WriteLine("{0,8} | {1,-25} | {2,-28} | {3,-12} | {4,-12} | {5,-8} | {6,-8} | {7,-10} | {8,-12}",
                "PlayerId", "Strategy", "Mycovariant Name", "Type", "Effect", "Games", "Trig.", "Avg Eff", "Tot Eff");
            Console.WriteLine(new string('-', 8) + "-|-" +
                                new string('-', 25) + "-|-" +
                                new string('-', 28) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 8) + "-|-" +
                                new string('-', 8) + "-|-" +
                                new string('-', 10) + "-|-" +
                                new string('-', 12));

            // Group by player/strategy/mycovariant/effect/type
            var grouped = _records
                .GroupBy(r => (r.PlayerId, r.Strategy, r.MycovariantName, r.Type, r.EffectType))
                .Select(g => new
                {
                    g.Key.PlayerId,
                    g.Key.Strategy,
                    g.Key.MycovariantName,
                    g.Key.Type,
                    g.Key.EffectType,
                    Games = g.Count(),
                    Triggered = g.Count(x => x.Triggered),
                    TotalEffect = g.Sum(x => x.EffectValue),
                    AvgEffect = g.Count() > 0 ? g.Average(x => x.EffectValue) : 0.0
                })
                .ToList();

            foreach (var (playerId, strategyName) in rankedPlayers)
            {
                var playerRecords = grouped.Where(x =>
                    x.PlayerId == playerId &&
                    string.Equals(x.Strategy, strategyName, StringComparison.OrdinalIgnoreCase));

                foreach (var r in playerRecords.OrderBy(x => x.MycovariantName).ThenBy(x => x.EffectType))
                {
                    Console.WriteLine("{0,8} | {1,-25} | {2,-28} | {3,-12} | {4,-12} | {5,-8} | {6,-8} | {7,-10:N2} | {8,-12}",
                        r.PlayerId,
                        Truncate(r.Strategy, 25),
                        Truncate(r.MycovariantName, 28),
                        Truncate(r.Type, 12),
                        Truncate(r.EffectType, 12),
                        r.Games,
                        r.Triggered,
                        r.AvgEffect,
                        r.TotalEffect);
                }
            }
            Console.WriteLine(new string('-', 125));
        }

        private static string Truncate(string value, int maxLength) =>
            value == null ? "" : (value.Length <= maxLength ? value : value[..maxLength]);
    }
}
