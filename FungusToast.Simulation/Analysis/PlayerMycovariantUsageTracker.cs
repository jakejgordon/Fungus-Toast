using FungusToast.Core.Config;
using FungusToast.Simulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.Analysis
{
    public class PlayerMycovariantUsageTracker
    {
        private readonly List<(int PlayerId, string Strategy, string MycovariantName, string Type, bool Triggered, string Effect)> _records = new();

        public void TrackGameResult(GameResult result)
        {
            foreach (var pr in result.PlayerResults)
            {
                foreach (var myco in pr.Mycovariants)
                {
                    string effect = myco.EffectSummary ?? "-";
                    _records.Add((
                        pr.PlayerId,
                        pr.StrategyName ?? "None",
                        myco.MycovariantName,
                        myco.MycovariantType,
                        myco.Triggered,
                        effect
                    ));
                }
            }
        }

        public void PrintReport(List<(int PlayerId, string StrategyName)> rankedPlayers)
        {
            Console.WriteLine("\nPlayer-Mycovariant Usage Summary (per Player, all games):");
            Console.WriteLine("{0,8} | {1,-25} | {2,-28} | {3,-12} | {4,-10} | {5,-8} | {6,-60}",
                "PlayerId", "Strategy", "Mycovariant Name", "Type", "Triggered", "Games", "Effect");
            Console.WriteLine(new string('-', 8) + "-|-" +
                                new string('-', 25) + "-|-" +
                                new string('-', 28) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 10) + "-|-" +
                                new string('-', 8) + "-|-" +
                                new string('-', 60));

            // Group by PlayerId + Strategy + Mycovariant for game count and aggregate effects
            var grouped = _records
                .GroupBy(r => (r.PlayerId, r.Strategy, r.MycovariantName, r.Type))
                .Select(g => new
                {
                    g.Key.PlayerId,
                    g.Key.Strategy,
                    g.Key.MycovariantName,
                    g.Key.Type,
                    Games = g.Count(),
                    Triggered = g.Count(x => x.Triggered), // Count times triggered
                    Effect = AggregateEffectSummaries(g.Select(x => x.Effect).ToList())
                })
                .ToList();

            foreach (var (playerId, strategyName) in rankedPlayers)
            {
                var playerRecords = grouped.Where(x =>
                    x.PlayerId == playerId &&
                    string.Equals(x.Strategy, strategyName, StringComparison.OrdinalIgnoreCase));

                foreach (var r in playerRecords)
                {
                    Console.WriteLine("{0,8} | {1,-25} | {2,-28} | {3,-12} | {4,-10} | {5,-8} | {6,-60}",
                        r.PlayerId,
                        Truncate(r.Strategy, 25),
                        Truncate(r.MycovariantName, 28),
                        Truncate(r.Type, 12),
                        r.Triggered,
                        r.Games,
                        Truncate(r.Effect, 60));
                }
            }
            Console.WriteLine(new string('-', 159));
        }

        private static string Truncate(string value, int maxLength) =>
            value == null ? "" : (value.Length <= maxLength ? value : value[..maxLength]);

        /// <summary>
        /// Aggregates effect summaries from multiple games.
        /// If all summaries are the same, shows one; otherwise, shows a comma-separated list.
        /// </summary>
        private static string AggregateEffectSummaries(List<string> effects)
        {
            var nonEmpty = effects.Where(e => !string.IsNullOrWhiteSpace(e) && e != "-").ToList();
            if (nonEmpty.Count == 0) return "";
            if (nonEmpty.All(e => e == nonEmpty[0])) return nonEmpty[0];
            return string.Join(", ", nonEmpty.Distinct());
        }
    }
}
