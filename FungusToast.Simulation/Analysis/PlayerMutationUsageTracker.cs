using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Analysis
{
    public class PlayerMutationUsageTracker
    {
        // Each record stores one mutation effect for one player in one game
        private readonly List<(int PlayerId, string Strategy, int MutationId, string MutationName, MutationTier Tier, MutationCategory Category, int Level, string EffectType, int EffectValue)> _records = new();

        public void TrackGameResult(GameResult result)
        {
            foreach (var player in result.PlayerResults)
            {
                string strategy = player.StrategyName ?? "None";

                // Track only mutations the player actually acquired (level > 0)
                var acquiredMutationIds = player.MutationLevels.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                
                foreach (var mutationId in acquiredMutationIds)
                {
                    var mutation = MutationRegistry.GetById(mutationId);
                    if (mutation == null) continue;

                    int level = player.MutationLevels[mutationId];
                    
                    // Get all effects for this mutation
                    var effects = GetMutationEffects(mutationId, player);
                    
                    // Always create exactly one record per mutation per game
                    if (effects.Count > 0)
                    {
                        // If there are effects, create a record for each effect type
                        foreach (var (effectType, effectValue) in effects)
                        {
                            _records.Add((
                                player.PlayerId,
                                strategy,
                                mutationId,
                                mutation.Name,
                                mutation.Tier,
                                mutation.Category,
                                level,
                                effectType,
                                effectValue
                            ));
                        }
                    }
                    else
                    {
                        // If there are no effects, create a single record with "-" effect type
                        _records.Add((
                            player.PlayerId,
                            strategy,
                            mutationId,
                            mutation.Name,
                            mutation.Tier,
                            mutation.Category,
                            level,
                            "-",
                            0
                        ));
                    }
                }
            }
        }

        private static Dictionary<string, int> GetMutationEffects(int mutationId, PlayerResult player)
        {
            var effects = new Dictionary<string, int>();

            switch (mutationId)
            {
                case MutationIds.RegenerativeHyphae:
                    if (player.RegenerativeHyphaeReclaims > 0)
                        effects["Reclaims"] = player.RegenerativeHyphaeReclaims;
                    break;

                case MutationIds.CreepingMold:
                    if (player.CreepingMoldMoves > 0)
                        effects["Mold Movements"] = player.CreepingMoldMoves;
                    if (player.CreepingMoldToxinJumps > 0)
                        effects["Toxin Jumps"] = player.CreepingMoldToxinJumps;
                    break;

                case MutationIds.Necrosporulation:
                    if (player.NecrosporulationSpores > 0)
                        effects["Necro Spores"] = player.NecrosporulationSpores;
                    break;

                case MutationIds.SporocidalBloom:
                    if (player.SporocidalSpores > 0)
                        effects["Spore Drops"] = player.SporocidalSpores;
                    if (player.SporocidalKills > 0)
                        effects["Spore Kills"] = player.SporocidalKills;
                    break;

                case MutationIds.NecrophyticBloom:
                    if (player.NecrophyticSpores > 0)
                        effects["Spore Drops"] = player.NecrophyticSpores;
                    if (player.NecrophyticReclaims > 0)
                        effects["Reclaims"] = player.NecrophyticReclaims;
                    break;

                case MutationIds.MycotoxinTracer:
                    if (player.MycotoxinTracerSpores > 0)
                        effects["Toxin Drops"] = player.MycotoxinTracerSpores;
                    break;

                case MutationIds.MycotoxinPotentiation:
                    if (player.ToxinAuraKills > 0)
                        effects["Toxin Aura Kills"] = player.ToxinAuraKills;
                    break;

                case MutationIds.MycotoxinCatabolism:
                    if (player.MycotoxinCatabolisms > 0)
                        effects["Toxin Catabolisms"] = player.MycotoxinCatabolisms;
                    if (player.CatabolizedMutationPoints > 0)
                        effects["Catabolized MP"] = player.CatabolizedMutationPoints;
                    break;

                case MutationIds.AdaptiveExpression:
                    if (player.AdaptiveExpressionPointsEarned > 0)
                        effects["Bonus MP"] = player.AdaptiveExpressionPointsEarned;
                    break;

                case MutationIds.MutatorPhenotype:
                    if (player.MutatorPhenotypePointsEarned > 0)
                        effects["Mutator Free MP"] = player.MutatorPhenotypePointsEarned;
                    break;

                case MutationIds.HyperadaptiveDrift:
                    if (player.HyperadaptiveDriftPointsEarned > 0)
                        effects["Hyperadaptive Free MP"] = player.HyperadaptiveDriftPointsEarned;
                    break;

                case MutationIds.AnabolicInversion:
                    if (player.AnabolicInversionPointsEarned > 0)
                        effects["Anabolic MP"] = player.AnabolicInversionPointsEarned;
                    break;

                case MutationIds.NecrohyphalInfiltration:
                    if (player.NecrohyphalInfiltrations > 0)
                        effects["Infiltrations"] = player.NecrohyphalInfiltrations;
                    if (player.NecrohyphalCascades > 0)
                        effects["Cascades"] = player.NecrohyphalCascades;
                    break;

                case MutationIds.PutrefactiveMycotoxin:
                    if (player.PutrefactiveMycotoxinKills > 0)
                        effects["PM Kills"] = player.PutrefactiveMycotoxinKills;
                    break;

                case MutationIds.NecrotoxicConversion:
                    if (player.NecrotoxicConversionReclaims > 0)
                        effects["Necrotoxic Reclaims"] = player.NecrotoxicConversionReclaims;
                    break;

                case MutationIds.CatabolicRebirth:
                    if (player.CatabolicRebirthResurrections > 0)
                        effects["Rebirths"] = player.CatabolicRebirthResurrections;
                    if (player.CatabolicRebirthAgedToxins > 0)
                        effects["Aged Toxins"] = player.CatabolicRebirthAgedToxins;
                    break;

                case MutationIds.HyphalSurge:
                    if (player.HyphalSurgeGrowths > 0)
                        effects["Hyphal Surge Growths"] = player.HyphalSurgeGrowths;
                    break;

                case MutationIds.HyphalVectoring:
                    if (player.HyphalVectoringGrowths > 0)
                        effects["Growths"] = player.HyphalVectoringGrowths;
                    if (player.HyphalVectoringInfested > 0)
                        effects["Infested"] = player.HyphalVectoringInfested;
                    if (player.HyphalVectoringReclaimed > 0)
                        effects["Reclaimed"] = player.HyphalVectoringReclaimed;
                    if (player.HyphalVectoringCatabolicGrowth > 0)
                        effects["Catabolic Growth"] = player.HyphalVectoringCatabolicGrowth;
                    if (player.HyphalVectoringAlreadyOwned > 0)
                        effects["Already Owned"] = player.HyphalVectoringAlreadyOwned;
                    if (player.HyphalVectoringColonized > 0)
                        effects["Colonized"] = player.HyphalVectoringColonized;
                    if (player.HyphalVectoringInvalid > 0)
                        effects["Invalid"] = player.HyphalVectoringInvalid;
                    break;
            }

            return effects;
        }

        public void PrintReport(List<(int PlayerId, string StrategyName)> rankedPlayers, SimulationTrackingContext tracking)
        {
            // Prepare category sort order
            var categorySortOrder = new Dictionary<MutationCategory, int>
            {
                { MutationCategory.Growth, 0 },
                { MutationCategory.CellularResilience, 1 },
                { MutationCategory.Fungicide, 2 },
                { MutationCategory.GeneticDrift, 3 },
                { MutationCategory.MycelialSurges, 4 }
            };

            Console.WriteLine("\nPlayer-Mutation Usage Summary (per Player, all games):");
            Console.WriteLine("{0,8} | {1,-25} | {2,-6} | {3,-28} | {4,-12} | {5,-8} | {6,-8} | {7,-10} | {8,-12} | {9,12} | {10,6} | {11,6} | {12,4}",
                "PlayerId", "Strategy", "Tier", "Mutation Name", "Effect", "Games", "AvgLvl", "Avg Eff", "Tot Eff", "AvgFirstRound", "Min", "Max", "N");
            Console.WriteLine(new string('-', 8) + "-|-" +
                                new string('-', 25) + "-|-" +
                                new string('-', 6) + "-|-" +
                                new string('-', 28) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 8) + "-|-" +
                                new string('-', 8) + "-|-" +
                                new string('-', 10) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 12) + "-|-" +
                                new string('-', 6) + "-|-" +
                                new string('-', 6) + "-|-" +
                                new string('-', 4));

            // Group by player/strategy/mutation/effect type
            var grouped = _records
                .GroupBy(r => (r.PlayerId, r.Strategy, r.MutationId, r.MutationName, r.Tier, r.Category, r.EffectType))
                .Select(g => new
                {
                    g.Key.PlayerId,
                    g.Key.Strategy,
                    g.Key.MutationId,
                    g.Key.MutationName,
                    g.Key.Tier,
                    g.Key.Category,
                    g.Key.EffectType,
                    Games = g.Count(),
                    AvgLevel = g.Average(x => x.Level),
                    TotalEffect = g.Sum(x => x.EffectValue),
                    AvgEffect = g.Count() > 0 ? g.Average(x => x.EffectValue) : 0.0
                })
                .ToList();

            foreach (var (playerId, strategyName) in rankedPlayers)
            {
                var playerRecords = grouped.Where(x =>
                    x.PlayerId == playerId &&
                    string.Equals(x.Strategy, strategyName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Tier)
                    .ThenBy(x => categorySortOrder.TryGetValue(x.Category, out var idx) ? idx : 99)
                    .ThenBy(x => x.MutationName)
                    .ThenBy(x => x.EffectType);

                foreach (var r in playerRecords)
                {
                    var (avgFirst, minFirst, maxFirst, nFirst) = tracking.GetFirstUpgradeStats(r.PlayerId, r.MutationId);
                    Console.WriteLine("{0,8} | {1,-25} | {2,-6} | {3,-28} | {4,-12} | {5,-8} | {6,-8} | {7,-10:N2} | {8,-12} | {9,12} | {10,6} | {11,6} | {12,4}",
                        r.PlayerId,
                        Truncate(r.Strategy, 25),
                        r.Tier.ToString(),
                        Truncate(r.MutationName, 28),
                        Truncate(r.EffectType, 12),
                        r.Games,
                        r.AvgLevel.ToString("F1"),
                        r.AvgEffect,
                        r.TotalEffect,
                        avgFirst.HasValue ? avgFirst.Value.ToString("F2") : "-",
                        minFirst.HasValue ? minFirst.Value.ToString() : "-",
                        maxFirst.HasValue ? maxFirst.Value.ToString() : "-",
                        nFirst);
                }
            }
            Console.WriteLine(new string('-', 160));
        }

        private static string Truncate(string value, int maxLength) =>
            value == null ? "" : (value.Length <= maxLength ? value : value[..maxLength]);
    }
}
