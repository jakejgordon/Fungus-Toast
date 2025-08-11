using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Handles all mutation effects related to the Fungicide category.
    /// </summary>
    public static class FungicideMutationProcessor
    {
        /// <summary>
        /// Checks if any adjacent enemy cell applies a Putrefactive Mycotoxin effect.
        /// </summary>
        public static bool CheckPutrefactiveMycotoxin(
            FungalCell target,
            GameBoard board,
            List<Player> players,
            double roll,
            out float chance,
            out int? killerPlayerId,
            out int? attackerTileId,
            Random rng,
            ISimulationObserver observer)
        {
            chance = 0f;
            killerPlayerId = null;
            attackerTileId = null;

            // Build list of (playerId, effect, tileId) tuples for each orthogonally adjacent enemy with effect > 0
            var effects = new List<(int playerId, float effect, int tileId)>();

            foreach (var neighborTile in board.GetOrthogonalNeighbors(target.TileId))
            {
                var neighbor = neighborTile.FungalCell;
                if (neighbor is null || !neighbor.IsAlive) continue; // Only living cells can apply Putrefactive Mycotoxin
                if (neighbor.OwnerPlayerId == target.OwnerPlayerId) continue;

                Player? enemy = players.FirstOrDefault(p => p.PlayerId == neighbor.OwnerPlayerId);
                if (enemy == null) continue;

                float baseEffect = enemy.GetMutationEffect(MutationType.AdjacentFungicide);
                
                // Apply Putrefactive Cascade effectiveness bonus
                int cascadeLevel = enemy.GetMutationLevel(MutationIds.PutrefactiveCascade);
                float cascadeBonus = cascadeLevel * GameBalance.PutrefactiveCascadeEffectivenessBonus;
                float totalEffect = baseEffect + cascadeBonus;

                if (totalEffect > 0f)
                {
                    effects.Add((enemy.PlayerId, totalEffect, neighborTile.TileId));
                    chance += totalEffect;
                }
            }

            if (chance <= 0f)
                return false;

            // Proportional interval assignment for fairness:
            // Each player's effect contributes to a "slice" of the total chance.
            float runningTotal = 0f;
            foreach (var (playerId, effect, tileId) in effects)
            {
                float start = runningTotal;
                float end = runningTotal + effect;

                // The roll is in [0, chance). If it falls within this player's slice, they get the kill.
                if (roll < end)
                {
                    killerPlayerId = playerId;
                    attackerTileId = tileId;
                    break;
                }
                runningTotal = end;
            }

            return true;
        }

        /// <summary>
        /// Applies toxin aura deaths (Mycotoxin Potentiation effect).
        /// </summary>
        public static void ApplyToxinAuraDeaths(GameBoard board,
                                         List<Player> players,
                                         Random rng,
                                         ISimulationObserver simulationObserver)
        {
            foreach (var tile in board.AllToxinTiles())
            {
                var toxinCell = tile.FungalCell!;
                int? ownerId = toxinCell.OwnerPlayerId;
                Player? owner = players.FirstOrDefault(p => p.PlayerId == ownerId);
                if (owner == null)
                    continue;

                float killChance = owner.GetMutationEffect(MutationType.ToxinKillAura);
                if (killChance <= 0f)
                    continue;

                int killCount = 0;

                foreach (var neighborTile in board.GetAdjacentLivingTiles(tile.TileId, excludePlayerId: owner.PlayerId))
                {
                    if (rng.NextDouble() < killChance)
                    {
                        board.KillFungalCell(neighborTile.FungalCell!, DeathReason.MycotoxinPotentiation, owner.PlayerId);
                        killCount++;
                    }
                }

                if (killCount > 0)
                {
                    simulationObserver.RecordCellDeath(owner.PlayerId, DeathReason.MycotoxinPotentiation, killCount);
                }
            }
        }

        /// <summary>
        /// Handles Sporocidal Bloom spore drops during decay phase.
        /// </summary>
        public static void OnDecayPhase_SporocidalBloom(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            Mutation sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
                if (level <= 0) continue;

                // Count living cells for this player
                var yourLivingIds = board.AllTiles()
                    .Where(t => t.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                    .Select(t => t.TileId)
                    .ToHashSet();

                int livingCellCount = yourLivingIds.Count;
                int sporesToDrop = (int)Math.Floor(livingCellCount * level * GameBalance.SporicialBloomEffectPerLevel);
                if (sporesToDrop <= 0) continue;

                // Filter out tiles that contain the player's living or dead cells
                var availableTiles = board.AllTiles()
                    .Where(t => {
                        var cell = t.FungalCell;
                        // Exclude tiles with player's own living or dead cells
                        return !(cell != null && cell.OwnerPlayerId == player.PlayerId);
                    })
                    .ToList();

                if (availableTiles.Count == 0) continue;

                // Max level bonus: Remove 25% of empty tiles to increase enemy targeting
                bool isMaxLevel = level >= GameBalance.SporicidalBloomMaxLevel;
                if (isMaxLevel)
                {
                    // Separate empty tiles from enemy tiles
                    var emptyTiles = availableTiles.Where(t => t.FungalCell == null).ToList();
                    var nonEmptyTiles = availableTiles.Where(t => t.FungalCell != null).ToList();

                    // Remove 25% of empty tiles randomly
                    int emptyTilesToRemove = (int)Math.Floor(emptyTiles.Count * 0.25f);
                    for (int i = 0; i < emptyTilesToRemove && emptyTiles.Count > 0; i++)
                    {
                        int removeIndex = rng.Next(emptyTiles.Count);
                        emptyTiles.RemoveAt(removeIndex);
                    }

                    // Recombine the lists
                    availableTiles = nonEmptyTiles.Concat(emptyTiles).ToList();
                }

                int kills = 0, toxified = 0;
                int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, GameBalance.DefaultToxinDuration);

                for (int i = 0; i < sporesToDrop; i++)
                {
                    var target = availableTiles[rng.Next(availableTiles.Count)];
                    var cell = target.FungalCell;

                    if (cell != null && cell.IsAlive)
                    {
                        // Enemy cell: kill and toxify (use helper)
                        ToxinHelper.KillAndToxify(board, target.TileId, toxinLifespan, DeathReason.SporicidalBloom, GrowthSource.SporicidalBloom, player);
                        kills++;
                    }
                    else
                    {
                        // Empty tile or existing toxin: place/refresh toxin
                        ToxinHelper.ConvertToToxin(board, target.TileId, toxinLifespan, GrowthSource.SporicidalBloom, player);
                        toxified++;
                    }
                }

                // Report total spores dropped for this player (once per player per round)
                if (sporesToDrop > 0)
                {
                    observer.ReportSporicidalSporeDrop(player.PlayerId, sporesToDrop);
                }
            }
        }

        /// <summary>
        /// Handles Necrotoxic Conversion effect when cells die to toxin effects.
        /// </summary>
        public static void OnCellDeath_NecrotoxicConversion(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            // Only applies to toxin-based deaths (including cascade deaths)
            if (eventArgs.Reason != DeathReason.PutrefactiveMycotoxin &&
                eventArgs.Reason != DeathReason.SporicidalBloom &&
                eventArgs.Reason != DeathReason.MycotoxinPotentiation &&
                eventArgs.Reason != DeathReason.PutrefactiveCascade &&
                eventArgs.Reason != DeathReason.PutrefactiveCascadePoison)
                return;

            // Must know the killer (the player whose toxin killed this cell)
            if (!eventArgs.KillerPlayerId.HasValue)
                return;

            int killerPlayerId = eventArgs.KillerPlayerId.Value;
            var killerPlayer = players.FirstOrDefault(p => p.PlayerId == killerPlayerId);
            if (killerPlayer == null)
                return;

            int ntcLevel = killerPlayer.GetMutationLevel(MutationIds.NecrotoxicConversion);
            if (ntcLevel <= 0)
                return;

            var deadCell = eventArgs.Cell;
            if (deadCell == null)
                return;

            // Guard: Check if the cell is still reclaimable before attempting to reclaim
            // This prevents race conditions where the cell might have been converted to a toxin
            // or otherwise modified by other mutation effects that run in the same event cycle
            if (!deadCell.IsReclaimable)
                return;

            // No adjacency check needed. Killer just needs to have the mutation.
            float chance = ntcLevel * GameBalance.NecrotoxicConversionReclaimChancePerLevel;
            if (rng.NextDouble() < chance)
            {
                // Use the board's safe TryReclaimDeadCell method instead of calling FungalCell.Reclaim directly
                // This method handles all the validation, board state updates, and event firing properly
                bool success = board.TryReclaimDeadCell(killerPlayerId, deadCell.TileId, GrowthSource.NecrotoxicConversion);
                
                if (success)
                {
                    observer.RecordNecrotoxicConversionReclaim(killerPlayerId, 1);
                }
            }
        }

        /// <summary>
        /// Handles Putrefactive Rejuvenation effect when cells are killed by Putrefactive Mycotoxin.
        /// </summary>
        public static void OnCellDeath_PutrefactiveRejuvenation(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            ISimulationObserver observer)
        {
            if (eventArgs.Reason != DeathReason.PutrefactiveMycotoxin || eventArgs.KillerPlayerId == null)
                return;

            var killerPlayer = players.FirstOrDefault(p => p.PlayerId == eventArgs.KillerPlayerId.Value);
            if (killerPlayer == null)
                return;

            int level = killerPlayer.GetMutationLevel(MutationIds.PutrefactiveRejuvenation);
            if (level <= 0)
                return;

            int baseRadius = GameBalance.PutrefactiveRejuvenationEffectRadius;
            int radius = (level >= GameBalance.PutrefactiveRejuvenationMaxLevel)
                ? baseRadius * GameBalance.PutrefactiveRejuvenationMaxLevelRangeRadiusMultiplier
                : baseRadius;
            int ageReduction = GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel * level;

            // Find all friendly living cells within radius of the poisoned cell
            var centerTile = board.GetTileById(eventArgs.TileId);
            if (centerTile == null)
                return;

            var affectedCells = board.GetAllCellsOwnedBy(killerPlayer.PlayerId)
                .Where(cell => {
                    if (!cell.IsAlive) return false;
                    var tile = board.GetTileById(cell.TileId);
                    return tile != null && tile.DistanceTo(centerTile) <= radius;
                })
                .ToList();

            int totalCyclesReduced = 0;
            foreach (var cell in affectedCells)
            {
                totalCyclesReduced += cell.ReduceGrowthCycleAge(ageReduction);
            }
            if (observer != null && totalCyclesReduced > 0)
            {
                observer.RecordPutrefactiveRejuvenationGrowthCyclesReduced(killerPlayer.PlayerId, totalCyclesReduced);
            }
        }

        /// <summary>
        /// Handles Putrefactive Cascade effect when cells are killed by Putrefactive Mycotoxin.
        /// </summary>
        public static void OnCellDeath_PutrefactiveCascade(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            // Only applies to Putrefactive Mycotoxin deaths and cascade deaths for true recursion
            if (eventArgs.Reason != DeathReason.PutrefactiveMycotoxin &&
                eventArgs.Reason != DeathReason.PutrefactiveCascade &&
                eventArgs.Reason != DeathReason.PutrefactiveCascadePoison)
                return;

            // Must have a killer player
            if (!eventArgs.KillerPlayerId.HasValue)
                return;

            var killer = players.FirstOrDefault(p => p.PlayerId == eventArgs.KillerPlayerId.Value);
            if (killer == null)
                return;

            // Must have Putrefactive Cascade mutation
            int cascadeLevel = killer.GetMutationLevel(MutationIds.PutrefactiveCascade);
            if (cascadeLevel <= 0)
                return;

            // Must have attacker tile information for direction calculation
            if (!eventArgs.AttackerTileId.HasValue)
                return;

            var attackerTile = board.GetTileById(eventArgs.AttackerTileId.Value);
            if (attackerTile == null)
                return;

            // Now execute the cascade with the direct attacker tile info
            TryPutrefactiveCascade(killer, eventArgs.Cell, attackerTile, board, players, rng, observer);
        }

        /// <summary>
        /// Applies Mycotoxin Tracer spore drops during decay phase.
        /// </summary>
        public static int ApplyMycotoxinTracer(
            Player player,
            GameBoard board,
            int failedGrowthsThisRound,
            List<Player> allPlayers,
            Random rng,
            ISimulationObserver observer)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinTracer);
            if (level == 0) return 0;

            // Convert accumulated failed growths to average per growth cycle
            // failedGrowthsThisRound represents total across all growth cycles (typically 5)
            float averageFailedGrowthsPerCycle = (float)failedGrowthsThisRound / GameBalance.TotalGrowthCycles;
            int failedGrowthsThisRoundAdjusted = (int)Math.Round(averageFailedGrowthsPerCycle);

            int totalTiles = board.TotalTiles;
            int maxToxinsThisRound = totalTiles / GameBalance.MycotoxinTracerMaxToxinsDivisor;

            int livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive);

            // 1. Base toxin count with diminishing returns (square root scaling)
            int baseToxins = (int)Math.Floor(Math.Sqrt(level));
            int toxinsFromLevel = rng.Next(0, baseToxins + 1);

            // 2. Failed growth bonus with logarithmic scaling to prevent excessive scaling
            float logLevel = (float)Math.Log(level + 1, 2); // Log base 2 of (level + 1)
            float weightedFailures = failedGrowthsThisRoundAdjusted * logLevel * GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel;
            int toxinsFromFailures = rng.Next(0, (int)weightedFailures + 1);

            // 3. Percentage-based bonus for early game (when living cells are low)
            int toxinsFromPercentageBonus = 0;
            if (livingCells > 0 && failedGrowthsThisRoundAdjusted > 0)
            {
                // Calculate failure rate as percentage of living cells that failed to grow
                float failureRate = (float)failedGrowthsThisRoundAdjusted / livingCells;
                // Clamp failure rate to [0, 1] to handle edge cases where failures exceed living cells
                failureRate = Math.Clamp(failureRate, 0f, 1f);
                
                // Establish maximum possible bonus toxins: MIN(opponents, failed growths)
                int opponentCount = allPlayers.Count - 1; // Subtract 1 for this player
                int maxBonusToxins = Math.Min(opponentCount, failedGrowthsThisRoundAdjusted);
                
                // Calculate level multiplier: 10% per level, capped at 100% (level 10+)
                float levelMultiplier = Math.Min(level * 0.1f, 1.0f);
                
                // Apply percentage bonus scaling:
                // Level 1: 10% of (failureRate * maxBonusToxins)  
                // Level 5: 50% of (failureRate * maxBonusToxins)
                // Level 10+: 100% of (failureRate * maxBonusToxins)
                float baseBonusToxins = failureRate * maxBonusToxins;
                float scaledBonus = baseBonusToxins * levelMultiplier;
                toxinsFromPercentageBonus = (int)Math.Round(scaledBonus);
            }

            int totalToxins = toxinsFromLevel + toxinsFromFailures + toxinsFromPercentageBonus;
            totalToxins = Math.Min(totalToxins, maxToxinsThisRound);

            if (totalToxins == 0) return 0;

            // Use shared helper to find target tiles adjacent to enemy living cells
            List<BoardTile> candidateTiles = ToxinHelper.FindMycotoxinTargetTiles(board, player);

            int placed = 0;
            for (int i = 0; i < totalToxins && candidateTiles.Count > 0; i++)
            {
                int index = rng.Next(candidateTiles.Count);
                BoardTile chosen = candidateTiles[index];
                candidateTiles.RemoveAt(index);

                int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, GameBalance.MycotoxinTracerTileDuration);
                ToxinHelper.ConvertToToxin(board, chosen.TileId, toxinLifespan, GrowthSource.MycotoxinTracer, player);
                placed++;
            }

            if (placed > 0)
            {
                observer.ReportMycotoxinTracerSporeDrop(player.PlayerId, placed);
            }

            return placed;
        }

        /// <summary>
        /// Attempts to trigger a Putrefactive Cascade from the killed cell in the same direction as the attack.
        /// </summary>
        private static void TryPutrefactiveCascade(
            Player killer,
            FungalCell killedCell,
            BoardTile attackerTile,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer,
            int recursionDepth = 0)
        {
            int cascadeLevel = killer.GetMutationLevel(MutationIds.PutrefactiveCascade);
            if (cascadeLevel <= 0) return;

            // Prevent infinite recursion - limit cascade depth
            if (recursionDepth >= GameBalance.PutrefactiveCascadeMaxCascadeDepth) return;

            float cascadeChance = cascadeLevel * GameBalance.PutrefactiveCascadeCascadeChance;
            bool isMaxLevel = cascadeLevel >= GameBalance.PutrefactiveCascadeMaxLevel;

            // Determine cascade direction (from attacker to killed cell)
            var killedTile = board.GetTileById(killedCell.TileId);
            if (killedTile == null) return;

            int dx = killedTile.X - attackerTile.X;
            int dy = killedTile.Y - attackerTile.Y;

            // Start cascading from the killed cell position
            int cascadeKills = 0;
            int cascadeToxified = 0;
            var currentTile = killedTile;

            while (rng.NextDouble() < cascadeChance)
            {
                // Move to next tile in the same direction
                int nextX = currentTile.X + dx;
                int nextY = currentTile.Y + dy;
                var nextTile = board.GetTile(nextX, nextY);
                
                if (nextTile == null) break; // Hit board edge

                var nextCell = nextTile.FungalCell;
                if (nextCell == null) break; // Empty tile, cascade stops
                if (!nextCell.IsAlive) break; // Dead or toxin cell, cascade stops
                if (nextCell.OwnerPlayerId == killer.PlayerId) break; // Own cell, cascade stops

                // Kill the next cell in the cascade
                if (isMaxLevel)
                {
                    // At max level: convert to toxin (poison effect) with attacker tile info for potential cascade recursion
                    int toxinLifespan = ToxinHelper.GetToxinExpirationAge(killer, GameBalance.DefaultToxinDuration);
                    ToxinHelper.KillAndToxify(board, nextTile.TileId, toxinLifespan, DeathReason.PutrefactiveCascadePoison, GrowthSource.PutrefactiveCascade, killer, currentTile.TileId);
                    cascadeToxified++;
                }
                else
                {
                    // Below max level: just kill with attacker tile info for potential cascade recursion
                    board.KillFungalCell(nextCell, DeathReason.PutrefactiveCascade, killer.PlayerId, currentTile.TileId);
                }
                
                cascadeKills++;
                currentTile = nextTile;
            }

            // Record cascade effects
            if (observer != null)
            {
                if (cascadeKills > 0)
                    observer.RecordPutrefactiveCascadeKills(killer.PlayerId, cascadeKills);
                if (cascadeToxified > 0)
                    observer.RecordPutrefactiveCascadeToxified(killer.PlayerId, cascadeToxified);
            }
        }

        // Phase event handlers
        public static void OnDecayPhase_MycotoxinTracer(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver observer)
        {
            // Count living cells for each player
            var playerLivingCellCounts = players.ToDictionary(p => p.PlayerId, p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));
            
            // Order players by living cell count (fewest first, most last)
            var playersOrderedByLivingCells = players.OrderBy(p => playerLivingCellCounts[p.PlayerId]).ToList();

            foreach (var player in playersOrderedByLivingCells)
            {
                int failedGrowths = failedGrowthsByPlayerId.TryGetValue(player.PlayerId, out var v) ? v : 0;
                ApplyMycotoxinTracer(player, board, failedGrowths, players, rng, observer);
            }
        }

        public static void OnDecayPhase_MycotoxinPotentiation(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            ApplyToxinAuraDeaths(board, players, rng, observer);
        }
    }
}