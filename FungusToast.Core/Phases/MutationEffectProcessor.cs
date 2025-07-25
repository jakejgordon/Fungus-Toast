﻿// FungusToast.Core/Phases/MutationEffectProcessor.cs
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Centralizes all mutation-specific calculations and helpers.
    /// Phase orchestration lives in DeathEngine; this class never loops the full board.
    /// </summary>
    public static class MutationEffectProcessor
    {
        public static (float chance, DeathReason? reason, int? killerPlayerId) CalculateDeathChance(
            Player owner,
            FungalCell cell,
            GameBoard board,
            List<Player> allPlayers,
            double roll)
        {
            float harmonyReduction = owner.GetMutationEffect(MutationType.DefenseSurvival);
            float ageDelay = owner.GetMutationEffect(MutationType.SelfAgeResetThreshold);

            float baseChance = Math.Max(0f, GameBalance.BaseDeathChance - harmonyReduction);

            float ageComponent = cell.GrowthCycleAge > ageDelay
                ? (cell.GrowthCycleAge - ageDelay) * GameBalance.AgeDeathFactorPerGrowthCycle
                : 0f;

            float ageChance = Math.Max(0f, ageComponent - harmonyReduction);

            float totalFallbackChance = Math.Clamp(baseChance + ageChance, 0f, 1f);
            float thresholdRandom = baseChance;
            float thresholdAge = baseChance + ageChance;

            if (roll < totalFallbackChance)
            {
                if (roll < thresholdRandom)
                    return (totalFallbackChance, DeathReason.Randomness, null);
                return (totalFallbackChance, DeathReason.Age, null);
            }

            // PutrefactiveMycotoxin example — this needs to determine the killer
            if (CheckPutrefactiveMycotoxin(cell, board, allPlayers, roll, out float pmChance, out int? killerPlayerId) &&
                roll < pmChance)
            {
                return (pmChance, DeathReason.PutrefactiveMycotoxin, killerPlayerId);
            }

            return (totalFallbackChance, null, null);
        }


        public static void AdvanceOrResetCellAge(Player player, FungalCell cell)
        {
            int resetAt = player.GetSelfAgeResetThreshold();
            if (cell.GrowthCycleAge >= resetAt)
                cell.ResetGrowthCycleAge();
            else
                cell.IncrementGrowthAge();
        }

        public static void TryApplyMutatorPhenotype(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null
        )
        {
            float chance = player.GetMutationEffect(MutationType.AutoUpgradeRandom);
            if (chance <= 0f || rng.NextDouble() >= chance) return;

            // Check Hyperadaptive Drift levels and associated per-level effects
            int hyperadaptiveLevel = player.GetMutationLevel(MutationIds.HyperadaptiveDrift);
            bool hasHyperadaptive = hyperadaptiveLevel > 0;

            float higherTierChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftHigherTierChancePerLevel * hyperadaptiveLevel
                : 0f;

            float bonusTierOneChance = hasHyperadaptive
                ? GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel * hyperadaptiveLevel
                : 0f;

            // Gather upgradable mutations by tier - for auto-upgrades, we don't need mutation points
            var tier1 = allMutations.Where(m => m.Tier == MutationTier.Tier1 && CanAutoUpgrade(player, m)).ToList();
            var tier2 = allMutations.Where(m => m.Tier == MutationTier.Tier2 && CanAutoUpgrade(player, m)).ToList();
            var tier3 = allMutations.Where(m => m.Tier == MutationTier.Tier3 && CanAutoUpgrade(player, m)).ToList();
            var tier4 = allMutations.Where(m => m.Tier == MutationTier.Tier4 && CanAutoUpgrade(player, m)).ToList();

            List<Mutation> pool;
            MutationTier targetTier;

            // Hyperadaptive Drift: roll to see if we try for tier 2, 3, or 4 instead of 1
            if (hasHyperadaptive && rng.NextDouble() < higherTierChance)
            {
                // Try tier 2, 3, or 4 randomly, but only among those with upgradable mutations
                var availableHigherTiers = new List<(List<Mutation> mutations, MutationTier tier)>
        {
            (tier2, MutationTier.Tier2),
            (tier3, MutationTier.Tier3),
            (tier4, MutationTier.Tier4)
        }.Where(t => t.mutations.Count > 0).ToList();

                if (availableHigherTiers.Count > 0)
                {
                    var selected = availableHigherTiers[rng.Next(availableHigherTiers.Count)];
                    pool = selected.mutations;
                    targetTier = selected.tier;
                }
                else if (tier1.Count > 0)
                {
                    pool = tier1;
                    targetTier = MutationTier.Tier1;
                }
                else
                {
                    return;
                }
            }
            else if (tier1.Count > 0)
            {
                pool = tier1;
                targetTier = MutationTier.Tier1;
            }
            else
            {
                return;
            }

            // Pick a mutation to auto-upgrade
            var pick = pool[rng.Next(pool.Count)];
            int upgrades = 1;

            // Hyperadaptive Drift: Tier 1 can double-upgrade
            if (hasHyperadaptive && targetTier == MutationTier.Tier1 && rng.NextDouble() < bonusTierOneChance)
            {
                upgrades = 2;
            }

            // Actually perform the upgrades, attributing each point appropriately
            int mutatorPoints = 0;
            int hyperadaptivePoints = 0;

            for (int i = 0; i < upgrades; i++)
            {
                bool upgraded = player.TryAutoUpgrade(pick, currentRound);
                if (!upgraded) break;

                // Attribution logic:
                if (targetTier == MutationTier.Tier1)
                {
                    if (i == 0)
                    {
                        mutatorPoints += pick.PointsPerUpgrade;
                    }
                    else
                    {
                        hyperadaptivePoints += pick.PointsPerUpgrade;
                    }
                }
                else // Tier 2, 3, or 4
                {
                    hyperadaptivePoints += pick.PointsPerUpgrade;
                }
            }

            // Hyperadaptive Drift Max Level Effect: Automatically upgrade an additional Tier 1 mutation
            if (hasHyperadaptive && hyperadaptiveLevel >= GameBalance.HyperadaptiveDriftMaxLevel && tier1.Count > 0)
            {
                var additionalPick = tier1[rng.Next(tier1.Count)];
                bool additionalUpgraded = player.TryAutoUpgrade(additionalPick, currentRound);
                if (additionalUpgraded)
                {
                    hyperadaptivePoints += additionalPick.PointsPerUpgrade;
                }
            }

            // Notify observer
            if (observer != null)
            {
                if (mutatorPoints > 0)
                    observer.RecordMutatorPhenotypeMutationPointsEarned(player.PlayerId, mutatorPoints);

                if (hyperadaptivePoints > 0)
                    observer.RecordHyperadaptiveDriftMutationPointsEarned(player.PlayerId, hyperadaptivePoints);
            }
        }

        /// <summary>
        /// Checks if a mutation can be auto-upgraded (without requiring mutation points).
        /// This is different from CanUpgrade which requires the player to have mutation points.
        /// </summary>
        private static bool CanAutoUpgrade(Player player, Mutation mutation)
        {
            if (mutation == null) return false;

            // Surge: can't upgrade while active
            if (mutation.IsSurge && player.IsSurgeActive(mutation.Id))
                return false;

            // Check prerequisites
            foreach (var pre in mutation.Prerequisites)
                if (player.GetMutationLevel(pre.MutationId) < pre.RequiredLevel)
                    return false;

            // Check if not at max level
            int currentLevel = player.GetMutationLevel(mutation.Id);
            return currentLevel < mutation.MaxLevel;
        }




        /* ────────────────────────────────────────────────────────────────
         * 3 ▸  ENEMY-PRESSURE & MUTATION-SPECIFIC CHECKS
         * ────────────────────────────────────────────────────────────────*/

        /// <summary>
        /// Checks if any adjacent enemy cell applies a Putrefactive Mycotoxin effect.
        /// If so, accumulates their effects for total kill chance,
        /// and assigns killerPlayerId fairly using proportional interval attribution.
        /// </summary>
        /// <param name="target">The cell potentially being killed.</param>
        /// <param name="board">The current game board.</param>
        /// <param name="players">All players.</param>
        /// <param name="roll">The random roll for probabilistic death.</param>
        /// <param name="chance">[out] The total death chance from all adjacent enemies.</param>
        /// <param name="killerPlayerId">[out] The player responsible for the kill, if any.</param>
        /// <returns>True if at least one adjacent enemy applies Putrefactive Mycotoxin; false otherwise.</returns>
        private static bool CheckPutrefactiveMycotoxin(
            FungalCell target,
            GameBoard board,
            List<Player> players,
            double roll,
            out float chance,
            out int? killerPlayerId)
        {
            chance = 0f;
            killerPlayerId = null;

            // Build list of (playerId, effect) pairs for each orthogonally adjacent enemy with effect > 0
            var effects = new List<(int playerId, float effect)>();

            foreach (var neighborTile in board.GetOrthogonalNeighbors(target.TileId))
            {
                var neighbor = neighborTile.FungalCell;
                if (neighbor is null || !neighbor.IsAlive) continue; // Only living cells can apply Putrefactive Mycotoxin
                if (neighbor.OwnerPlayerId == target.OwnerPlayerId) continue;

                Player? enemy = players.FirstOrDefault(p => p.PlayerId == neighbor.OwnerPlayerId);
                if (enemy == null) continue;

                float effect = enemy.GetMutationEffect(MutationType.AdjacentFungicide);
                if (effect > 0f)
                {
                    effects.Add((enemy.PlayerId, effect));
                    chance += effect;
                }
            }

            if (chance <= 0f)
                return false;

            // Proportional interval assignment for fairness:
            // Each player's effect contributes to a "slice" of the total chance.
            float runningTotal = 0f;
            foreach (var (playerId, effect) in effects)
            {
                float start = runningTotal;
                float end = runningTotal + effect;

                // The roll is in [0, chance). If it falls within this player's slice, they get the kill.
                if (roll < end)
                {
                    killerPlayerId = playerId;
                    break;
                }
                runningTotal = end;
            }

            return true;
        }



        /* ────────────────────────────────────────────────────────────────
         * 4 ▸  MOVEMENT & GROWTH HELPERS
         * ────────────────────────────────────────────────────────────────*/

        public static float GetTendrilDiagonalGrowthMultiplier(Player player)
        {
            return 1f + player.GetMutationEffect(MutationType.TendrilDirectionalMultiplier);
        }

        /// <summary>
        /// Attempts to move a living fungal cell (Creeping Mold mutation effect) from <paramref name="sourceTile"/>
        /// to <paramref name="targetTile"/>. The source cell is removed and a new living cell is created in the target location,
        /// overwriting any existing cell or empty tile.
        /// Only succeeds if the player has Creeping Mold at level &gt; 0, and certain space/openness requirements are met.
        /// Returns true if the move was performed; otherwise, false.
        /// </summary>
        /// <param name="player">The player attempting the move.</param>
        /// <param name="sourceCell">The living fungal cell to move.</param>
        /// <param name="sourceTile">The tile containing the source cell.</param>
        /// <param name="targetTile">The target tile for movement.</param>
        /// <param name="rng">Random number generator.</param>
        /// <param name="board">The game board.</param>
        /// <param name="observer">The simulation observer.</param>
        /// <returns>True if the move succeeded; false otherwise.</returns>
        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board,
            ISimulationObserver? observer = null)
        {
            bool hasMaxCreepingMold = player.PlayerMutations.TryGetValue(MutationIds.CreepingMold, out var cm) &&
                                      cm.CurrentLevel == GameBalance.CreepingMoldMaxLevel;
            bool targetIsToxin = targetTile.FungalCell != null && targetTile.FungalCell.IsToxin;
            bool specialToxinJumpCase = hasMaxCreepingMold && targetIsToxin;

            // Only allow occupied tiles if it's the special toxin jump case
            if (targetTile.IsOccupied && !specialToxinJumpCase) return false;

            float moveChance =
                cm != null ? cm.CurrentLevel * GameBalance.CreepingMoldMoveChancePerLevel : 0f;

            // Handle the special toxin jump case
            if (specialToxinJumpCase) {
                // Only allow for cardinal directions
                int dx = targetTile.X - sourceTile.X;
                int dy = targetTile.Y - sourceTile.Y;
                bool isCardinal = (dx == 0 && Math.Abs(dy) == 1) || (dy == 0 && Math.Abs(dx) == 1);
                if (!isCardinal) return false;

                // Compute the tile beyond the toxin in the same direction
                int jumpX = targetTile.X + dx;
                int jumpY = targetTile.Y + dy;
                var jumpTile = board.GetTile(jumpX, jumpY);
                if (jumpTile != null && !jumpTile.IsOccupied && (jumpTile.FungalCell == null || !jumpTile.FungalCell.IsToxin)) {
                    if (rng.NextDouble() <= moveChance) {
                        int sourceOpen = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                                .Count(n => !n.IsOccupied);
                        int targetOpen = board.GetOrthogonalNeighbors(jumpTile.X, jumpTile.Y)
                                                .Count(n => !n.IsOccupied);
                        if (targetOpen >= sourceOpen && targetOpen >= 2) {
                            CreateCreepingMoldCell(player, sourceCell, sourceTile, jumpTile, board);
                            observer?.RecordCreepingMoldToxinJump(player.PlayerId);
                            return true;
                        }
                    }
                }
                return false;
            }

            // Standard Creeping Mold move
            if (rng.NextDouble() > moveChance) return false;

            // Count open (unoccupied) orthogonal neighbors for source and target
            int sourceOpenStandard = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                  .Count(n => !n.IsOccupied);
            int targetOpenStandard = board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                                  .Count(n => !n.IsOccupied);

            // Only allow the move if the target is at least as open as the source,
            // and the target has at least 2 open sides. This prevents mold from
            // sliding into dead ends or more enclosed spaces, encouraging spreading
            // into open areas and keeping the mutation balanced and fun.
            if (targetOpenStandard < sourceOpenStandard || targetOpenStandard < 2) return false;

            CreateCreepingMoldCell(player, sourceCell, sourceTile, targetTile, board);
            return true;
        }

        // Helper for creating and moving a Creeping Mold cell
        private static void CreateCreepingMoldCell(Player player, FungalCell sourceCell, BoardTile sourceTile, BoardTile targetTile, GameBoard board)
        {
            var newCell = new FungalCell(player.PlayerId, targetTile.TileId);
            board.PlaceFungalCell(newCell); // Event hooks will fire as appropriate
            sourceTile.RemoveFungalCell();
            player.RemoveControlledTile(sourceCell.TileId);
        }



        /* ────────────────────────────────────────────────────────────────
         * 5 ▸  SPOROCIDAL BLOOM HELPERS
         * ────────────────────────────────────────────────────────────────*/

        /** not used
        public static int GetSporocidalSporeDropCount(Player player,
                                                      int livingCellCount,
                                                      Mutation sporocidalBloom)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level == 0 || livingCellCount == 0) return 0;

            float chancePerCell = level * sporocidalBloom.EffectPerLevel;
            int spores = 0;

            // Deterministic per-player seeding for reproducibility in tests
            var rng = new Random(player.PlayerId + livingCellCount);

            for (int i = 0; i < livingCellCount; i++)
                if (rng.NextDouble() < chancePerCell) spores++;

            return spores;
        }

        public static int ComputeSporocidalBloomSporeDropCount(int level,
                                                               int livingCellCount,
                                                               float effectPerLevel)
        {
            if (livingCellCount == 0) return 0;
            float rate = level * effectPerLevel;
            float estimate = livingCellCount * rate;
            return Math.Max(1, (int)Math.Round(estimate));
        }

        */


        /* ────────────────────────────────────────────────────────────────
         * 6 ▸  NECROPHYTIC BLOOM HELPERS
         * ────────────────────────────────────────────────────────────────*/

        // Returns the damping factor based on occupied percent
        public static float GetNecrophyticBloomDamping(float occupiedPercent)
        {
            if (occupiedPercent <= 0.20f) return 1f;
            float raw = 1f - ((occupiedPercent - 0.20f) / 0.80f);
            return Math.Clamp(raw, 0f, 1f);
        }

        /// <summary>
        /// Handles the initial spore burst from Necrophytic Bloom when it first activates.
        /// Attempts to reclaim dead, non-toxin fungal cells on the board using event-safe logic.
        /// </summary>
        public static void TriggerNecrophyticBloomInitialBurst(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver? observer = null)
        {
            int level = player.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            var deadCells = board.GetAllCellsOwnedBy(player.PlayerId)
                                 .Where(cell => cell.IsDead && !cell.IsToxin)
                                 .ToList();

            float sporesPerDeadCell = level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel;
            int totalSpores = (int)Math.Floor(sporesPerDeadCell * deadCells.Count);

            if (totalSpores <= 0) return;

            var allTiles = board.AllTiles().ToList();
            int reclaims = 0;

            for (int i = 0; i < totalSpores; i++)
            {
                var targetTile = allTiles[rng.Next(allTiles.Count)];
                if (board.TryReclaimDeadCell(player.PlayerId, targetTile.TileId))
                {
                    reclaims++;
                }
            }

            if (reclaims > 0)
            {
                observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, totalSpores, reclaims);
            }
        }



        public static void TriggerNecrophyticBloomOnCellDeath(
           Player owner,
           GameBoard board,
           Random rng,
           float occupiedPercent,
           ISimulationObserver? observer = null)
        {
            int level = owner.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            float damping = GetNecrophyticBloomDamping(occupiedPercent);
            int spores = (int)Math.Floor(
                level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel * damping);

            if (spores <= 0) return;

            var allTileIds = board.AllTiles().Select(t => t.TileId).ToList();
            int reclaims = 0;

            for (int i = 0; i < spores; i++)
            {
                int randomTileId = allTileIds[rng.Next(allTileIds.Count)];
                bool success = board.TryReclaimDeadCell(owner.PlayerId, randomTileId);
                if (success) reclaims++;
            }

            observer?.ReportNecrophyticBloomSporeDrop(owner.PlayerId, spores, reclaims);
        }

        public static int ApplyMycotoxinTracer(
            Player player,
            GameBoard board,
            int failedGrowthsThisRound,
            Random rng,
            ISimulationObserver? observer = null)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinTracer);
            if (level == 0) return 0;

            int totalTiles = board.TotalTiles;
            int maxToxinsThisRound = totalTiles / GameBalance.MycotoxinTracerMaxToxinsDivisor;

            int livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive);

            // 1. Base toxin count with diminishing returns (square root scaling)
            int baseToxins = (int)Math.Floor(Math.Sqrt(level));
            int toxinsFromLevel = rng.Next(0, baseToxins + 1);

            // 2. Failed growth bonus with logarithmic scaling to prevent excessive scaling
            float logLevel = (float)Math.Log(level + 1, 2); // Log base 2 of (level + 1)
            float weightedFailures = failedGrowthsThisRound * logLevel * GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel;
            int toxinsFromFailures = rng.Next(0, (int)weightedFailures + 1);

            int totalToxins = toxinsFromLevel + toxinsFromFailures;
            totalToxins = Math.Min(totalToxins, maxToxinsThisRound);

            if (totalToxins == 0) return 0;

            // 3. Target tiles that are unoccupied, not toxic, and orthogonally adjacent to enemy mold
            List<BoardTile> candidateTiles = board.AllTiles()
                .Where(t => !t.IsOccupied)
                .Where(t =>
                    board.GetOrthogonalNeighbors(t.TileId)
                         .Any(n => n.FungalCell is { IsAlive: true } && n.FungalCell.OwnerPlayerId != player.PlayerId)
                )
                .ToList();

            int placed = 0;
            for (int i = 0; i < totalToxins && candidateTiles.Count > 0; i++)
            {
                int index = rng.Next(candidateTiles.Count);
                BoardTile chosen = candidateTiles[index];
                candidateTiles.RemoveAt(index);

                int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, GameBalance.MycotoxinTracerTileDuration);
                ToxinHelper.ConvertToToxin(board, chosen.TileId, toxinLifespan, player);
                placed++;
            }

            if (placed > 0)
            {
                observer?.ReportMycotoxinTracerSporeDrop(player.PlayerId, placed);
            }

            return placed;
        }


        public static void ApplyToxinAuraDeaths(GameBoard board,
                                         List<Player> players,
                                         Random rng,
                                         ISimulationObserver? simulationObserver = null)
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
                    simulationObserver?.RecordCellDeath(owner.PlayerId, DeathReason.MycotoxinPotentiation, killCount);
                }
            }
        }

        public static int ApplyMycotoxinCatabolism(
            Player player,
            GameBoard board,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null)
        {
            int level = player.GetMutationLevel(MutationIds.MycotoxinCatabolism);
            if (level <= 0) return 0;

            float cleanupChance = level * GameBalance.MycotoxinCatabolismCleanupChancePerLevel;
            int toxinsMetabolized = 0;
            var processedToxins = new HashSet<int>();

            int maxPointsPerRound = GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound;
            int pointsSoFar = roundContext.GetEffectCount(player.PlayerId, "CatabolizedMP");

            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive) continue;

                foreach (var neighborTile in board.GetOrthogonalNeighbors(cell.TileId))
                {
                    if (neighborTile.FungalCell is not { IsToxin: true }) continue;
                    if (!processedToxins.Add(neighborTile.TileId)) continue;

                    if (rng.NextDouble() < cleanupChance)
                    {
                        neighborTile.RemoveFungalCell();
                        toxinsMetabolized++;

                        if (pointsSoFar < maxPointsPerRound &&
                            rng.NextDouble() < GameBalance.MycotoxinCatabolismMutationPointChancePerCatabolism)
                        {
                            player.MutationPoints += 1;
                            roundContext.IncrementEffectCount(player.PlayerId, "CatabolizedMP");
                            pointsSoFar++;

                            if (pointsSoFar >= maxPointsPerRound)
                                break;
                        }
                    }
                }
                if (pointsSoFar >= maxPointsPerRound)
                    break;
            }

            if (toxinsMetabolized > 0)
            {
                observer?.RecordToxinCatabolism(player.PlayerId, toxinsMetabolized, pointsSoFar);
            }

            return toxinsMetabolized;
        }




        public static bool TryNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null)
        {
            int necroLevel = owner.GetMutationLevel(MutationIds.NecrohyphalInfiltration);
            if (necroLevel <= 0)
                return false;

            double baseChance = GameBalance.NecrohyphalInfiltrationChancePerLevel * necroLevel;
            double cascadeChance = GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel * necroLevel;

            // Find adjacent dead enemy cells
            var deadEnemyNeighbors = board
                .GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                .Where(tile =>
                    tile.IsOccupied &&
                    tile.FungalCell != null &&
                    tile.FungalCell.IsDead &&
                    tile.FungalCell.OwnerPlayerId.HasValue &&
                    tile.FungalCell.OwnerPlayerId.Value != owner.PlayerId)
                .ToList();

            Shuffle(deadEnemyNeighbors, rng);

            foreach (var deadTile in deadEnemyNeighbors)
            {
                            // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
            bool success = ReclaimCellHelper.TryReclaimDeadCell(
                board, owner, deadTile.TileId, (float)baseChance, rng, observer);
            if (success)
            {
                // Track which tiles have already been reclaimed
                var alreadyReclaimed = new HashSet<int> { deadTile.TileId };

                // Cascade infiltrations
                int cascadeCount = CascadeNecrohyphalInfiltration(
                    board, deadTile, owner, rng, (float)cascadeChance, alreadyReclaimed);

                // Record: 1 infiltration for the initial, cascades for the rest
                observer?.RecordNecrohyphalInfiltration(owner.PlayerId, 1);
                if (cascadeCount > 0)
                    observer?.RecordNecrohyphalInfiltrationCascade(owner.PlayerId, cascadeCount);

                return true;
            }
            }

            return false;
        }


        private static int CascadeNecrohyphalInfiltration(
           GameBoard board,
           BoardTile sourceTile,
           Player owner,
           Random rng,
           float cascadeChance,
           HashSet<int> alreadyReclaimed,
           ISimulationObserver? observer = null)
        {
            int cascadeCount = 0;
            var toCheck = new Queue<BoardTile>();
            toCheck.Enqueue(sourceTile);

            while (toCheck.Count > 0)
            {
                var currentTile = toCheck.Dequeue();

                var nextTargets = board
                    .GetOrthogonalNeighbors(currentTile.X, currentTile.Y)
                    .Where(tile =>
                        tile.IsOccupied &&
                        tile.FungalCell != null &&
                        tile.FungalCell.IsDead &&
                        tile.FungalCell.OwnerPlayerId.HasValue &&
                        tile.FungalCell.OwnerPlayerId.Value != owner.PlayerId &&
                        !alreadyReclaimed.Contains(tile.TileId))
                    .ToList();

                Shuffle(nextTargets, rng);

                foreach (var deadTile in nextTargets)
                {
                    // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
                    bool success = ReclaimCellHelper.TryReclaimDeadCell(
                        board, owner, deadTile.TileId, (float)cascadeChance, rng, observer);
                    if (success)
                    {
                        alreadyReclaimed.Add(deadTile.TileId);

                        cascadeCount++;
                        toCheck.Enqueue(deadTile);
                    }
                }
            }

            return cascadeCount;
        }

        /// <summary>
        /// Handles the Necrotoxic Conversion effect in response to a fungal cell death event.
        /// If the cell died to a toxin effect *created by a player* and the killer has Necrotoxic Conversion,
        /// the cell may be reclaimed by the killer.
        /// </summary>
        public static void OnCellDeath_NecrotoxicConversion(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            // Only applies to toxin-based deaths
            if (eventArgs.Reason != DeathReason.PutrefactiveMycotoxin &&
                eventArgs.Reason != DeathReason.SporocidalBloom &&
                eventArgs.Reason != DeathReason.MycotoxinPotentiation)
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

            // No adjacency check needed. Killer just needs to have the mutation.
            float chance = ntcLevel * GameBalance.NecrotoxicConversionReclaimChancePerLevel;
            if (rng.NextDouble() < chance)
            {
                deadCell.Reclaim(killerPlayerId);
                board.PlaceFungalCell(deadCell);

                observer?.RecordNecrotoxicConversionReclaim(killerPlayerId, 1);
            }
        }

        /// <summary>
        /// Handles the Regenerative Hyphae effect in response to the post-growth phase event.
        /// Players with this mutation have a chance to reclaim their own dead cells adjacent to living cells.
        /// </summary>
        public static void OnPostGrowthPhase_RegenerativeHyphae(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            var attempted = new HashSet<int>();
            foreach (Player p in players)
            {
                float reclaimChance = p.GetMutationEffect(MutationType.ReclaimOwnDeadCells);
                if (reclaimChance <= 0f) continue;
                foreach (FungalCell cell in board.GetAllCellsOwnedBy(p.PlayerId))
                {
                    foreach (BoardTile n in board.GetOrthogonalNeighbors(cell.TileId))
                    {
                        FungalCell? dead = n.FungalCell;
                        if (dead is null || dead.IsAlive || dead.IsToxin) continue;
                        if (dead.OwnerPlayerId != p.PlayerId) continue;
                        if (!attempted.Add(dead.TileId)) continue;
                        
                        // Try to reclaim the dead cell using the helper
                        bool success = ReclaimCellHelper.TryReclaimDeadCell(
                            board, p, dead.TileId, reclaimChance, rng, observer);
                        if (success)
                        {
                            observer?.RecordRegenerativeHyphaeReclaim(p.PlayerId);
                        }
                    }
                }
            }
        }

        public static (float baseChance, float surgeBonus) GetGrowthChancesWithHyphalSurge(Player player)
        {
            float baseChance = GameBalance.BaseGrowthChance + player.GetMutationEffect(MutationType.GrowthChance);

            int hyphalSurgeId = MutationIds.HyphalSurge;
            float surgeBonus = 0f;
            if (player.IsSurgeActive(hyphalSurgeId))
            {
                int surgeLevel = player.GetMutationLevel(hyphalSurgeId);
                surgeBonus = surgeLevel * GameBalance.HyphalSurgeEffectPerLevel;
            }
            return (baseChance, surgeBonus);
        }

        // Utility: Fisher-Yates shuffle (reuse existing or add to this class if needed)
        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        public static void ProcessHyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.HyphalVectoring);
                if (level <= 0 || !player.IsSurgeActive(MutationIds.HyphalVectoring))
                    continue;

                int centerX = board.Width / 2;
                int centerY = board.Height / 2;
                int totalTiles = GameBalance.HyphalVectoringBaseTiles +
                                 level * GameBalance.HyphalVectoringTilesPerLevel;

                var origin = HyphalVectoringHelper.TrySelectHyphalVectorOrigin(player, board, rng, centerX, centerY, totalTiles);

                if (origin == null)
                {
                    Console.WriteLine($"[HyphalVectoring] Player {player.PlayerId}: no valid origin found.");
                    continue;
                }

                // Outcome tallies
                int infested = 0;
                int reclaimed = 0;
                int catabolicGrowth = 0;
                int alreadyOwned = 0;
                int colonized = 0;
                int invalid = 0;

                int placed = 0;
                int currentTileId = origin.Value.tile.TileId;
                int dx = Math.Sign(centerX - origin.Value.tile.X);
                int dy = Math.Sign(centerY - origin.Value.tile.Y);

                for (int i = 0; i < totalTiles; i++)
                {
                    var (x, y) = board.GetXYFromTileId(currentTileId);
                    // Step towards the center
                    x += dx;
                    y += dy;
                    if (x < 0 || y < 0 || x >= board.Width || y >= board.Height)
                        break;
                    int targetTileId = y * board.Width + x;
                    var targetTile = board.GetTileById(targetTileId);
                    if (targetTile == null) { invalid++; continue; }

                    var prevCell = targetTile.FungalCell;
                    if (prevCell != null && prevCell.IsAlive && prevCell.OwnerPlayerId == player.PlayerId)
                    {
                        // Skip over friendly living mold
                        alreadyOwned++;
                        currentTileId = targetTileId;
                        continue;
                    }

                    FungalCellTakeoverResult takeoverResult;
                    if (prevCell != null)
                    {
                        // Use board.TakeoverCell to handle both cell state and board updates.
                        takeoverResult = board.TakeoverCell(targetTileId, player.PlayerId, allowToxin: true, players: board.Players, rng: rng, observer: observer);
                        switch (takeoverResult)
                        {
                            case FungalCellTakeoverResult.Infested: infested++; break;
                            case FungalCellTakeoverResult.Reclaimed: reclaimed++; break;
                            case FungalCellTakeoverResult.CatabolicGrowth: catabolicGrowth++; break;
                            case FungalCellTakeoverResult.AlreadyOwned: alreadyOwned++; break;
                            case FungalCellTakeoverResult.Invalid: invalid++; break;
                        }
                    }
                    else
                    {
                        // Place a new living cell if empty
                        var newCell = new FungalCell(player.PlayerId, targetTileId);
                        targetTile.PlaceFungalCell(newCell);
                        colonized++;
                    }

                    placed++;
                    currentTileId = targetTileId;
                }

                // Report results to simulation observer, if available
                if (observer != null)
                {
                    if (infested > 0) observer.ReportHyphalVectoringInfested(player.PlayerId, infested);
                    if (reclaimed > 0) observer.ReportHyphalVectoringReclaimed(player.PlayerId, reclaimed);
                    if (catabolicGrowth > 0) observer.ReportHyphalVectoringCatabolicGrowth(player.PlayerId, catabolicGrowth);
                    if (alreadyOwned > 0) observer.ReportHyphalVectoringAlreadyOwned(player.PlayerId, alreadyOwned);
                    if (colonized > 0) observer.ReportHyphalVectoringColonized(player.PlayerId, colonized);
                    if (invalid > 0) observer.ReportHyphalVectoringInvalid(player.PlayerId, invalid);
                }

                if (placed > 0)
                    observer?.RecordHyphalVectoringGrowth(player.PlayerId, placed);
            }
        }

        public static void OnPostGrowthPhase_HyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                if (player.GetMutationLevel(MutationIds.HyphalVectoring) > 0)
                {
                    ProcessHyphalVectoring(board, players, rng, observer);
                }
            }
        }

        public static void OnDecayPhase_SporocidalBloom(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
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

                // Take a snapshot of all tiles for fair sampling
                var allTiles = board.AllTiles().ToList();
                if (allTiles.Count == 0) continue;

                int kills = 0, toxified = 0;
                int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, GameBalance.DefaultToxinDuration);

                for (int i = 0; i < sporesToDrop; i++)
                {
                    var target = allTiles[rng.Next(allTiles.Count)];
                    var cell = target.FungalCell;

                    // Is this tile protected? (your own living cell or adjacent to one)
                    bool isOwnLiving = (cell?.IsAlive ?? false) && cell.OwnerPlayerId == player.PlayerId;
                    bool adjacentToOwn = board.GetOrthogonalNeighbors(target.TileId)
                        .Any(adj => adj.FungalCell?.IsAlive == true && adj.FungalCell.OwnerPlayerId == player.PlayerId);

                    if (isOwnLiving || adjacentToOwn)
                        continue; // Spore fizzles, nothing happens

                    if (cell != null && cell.IsAlive)
                    {
                        // Enemy cell: kill and toxify (use helper)
                        ToxinHelper.KillAndToxify(board, target.TileId, toxinLifespan, DeathReason.SporocidalBloom, player);
                        kills++;
                    }
                    else
                    {
                        // Empty or already toxin: place toxin
                        ToxinHelper.ConvertToToxin(board, target.TileId, toxinLifespan, player);
                        toxified++;
                    }
                }

                // Report total spores dropped for this player (once per player per round)
                if (sporesToDrop > 0)
                {
                    observer?.ReportSporocidalSporeDrop(player.PlayerId, sporesToDrop);
                }
            }
        }

        public static void OnPreGrowthPhase_MycotoxinCatabolism(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                ApplyMycotoxinCatabolism(player, board, rng, roundContext, observer);
            }
        }

        public static void OnDecayPhase_MycotoxinTracer(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                int failedGrowths = failedGrowthsByPlayerId.TryGetValue(player.PlayerId, out var v) ? v : 0;
                ApplyMycotoxinTracer(player, board, failedGrowths, rng, observer);
            }
        }

        public static void OnDecayPhase_MycotoxinPotentiation(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            ApplyToxinAuraDeaths(board, players, rng, observer);
        }

        public static void OnNecrophyticBloomActivated(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            foreach (var p in players)
            {
                if (p.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                {
                    TriggerNecrophyticBloomInitialBurst(p, board, rng, observer);
                }
            }
        }

        public static void OnMutationPhaseStart_MutatorPhenotype(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                TryApplyMutatorPhenotype(player, allMutations, rng, currentRound, observer);
            }
        }

        public static void OnToxinExpired_CatabolicRebirth(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            // Check adjacent tiles for dead cells
            var adjacentTiles = board.GetOrthogonalNeighbors(eventArgs.TileId);
            var resurrectionsByPlayer = new Dictionary<int, int>();

            foreach (var adjacentTile in adjacentTiles)
            {
                if (adjacentTile.FungalCell == null || !adjacentTile.FungalCell.IsDead)
                    continue;

                // Find the owner of the dead cell
                int? deadCellOwnerId = adjacentTile.FungalCell.OwnerPlayerId;
                if (!deadCellOwnerId.HasValue)
                    continue;

                var deadCellOwner = players.FirstOrDefault(p => p.PlayerId == deadCellOwnerId.Value);
                if (deadCellOwner == null)
                    continue;

                // Check if the dead cell's owner has Catabolic Rebirth
                int level = deadCellOwner.GetMutationLevel(MutationIds.CatabolicRebirth);
                if (level <= 0)
                    continue;

                float chance = level * GameBalance.CatabolicRebirthResurrectionChancePerLevel;
                
                // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
                bool success = ReclaimCellHelper.TryReclaimDeadCell(
                    board, deadCellOwner, adjacentTile.TileId, (float)chance, rng, observer);
                if (success)
                {
                    // Track resurrections by player
                    if (!resurrectionsByPlayer.ContainsKey(deadCellOwner.PlayerId))
                        resurrectionsByPlayer[deadCellOwner.PlayerId] = 0;
                    resurrectionsByPlayer[deadCellOwner.PlayerId]++;
                }
            }

            // Report resurrections for each player
            foreach (var (playerId, count) in resurrectionsByPlayer)
            {
                observer?.RecordCatabolicRebirthResurrection(playerId, count);
                
                // Fire the CatabolicRebirth event
                var (x, y) = board.GetXYFromTileId(eventArgs.TileId);
                var rebirthArgs = new CatabolicRebirthEventArgs(playerId, count, x, y);
                board.OnCatabolicRebirth(rebirthArgs);
            }
        }

        /// <summary>
        /// Handles Putrefactive Rejuvenation: when a cell is killed by Putrefactive Mycotoxin, rejuvenate nearby friendly cells.
        /// </summary>
        public static void OnCellDeath_PutrefactiveRejuvenation(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            ISimulationObserver? observer = null)
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
        /// Handles Chitin Fortification: At the start of each Growth Phase while the surge is active,
        /// randomly select X living cells per level to make resistant for the duration of the surge.
        /// </summary>
        public static void OnPreGrowthPhase_ChitinFortification(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.ChitinFortification);
                if (level <= 0 || !player.IsSurgeActive(MutationIds.ChitinFortification))
                    continue;

                // Get all living cells owned by this player
                var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(cell => cell.IsAlive && !cell.IsResistant) // Don't double-fortify already resistant cells
                    .ToList();

                if (livingCells.Count == 0)
                    continue;

                // Calculate how many cells to fortify based on level
                int cellsToFortify = level * GameBalance.ChitinFortificationCellsPerLevel;
                cellsToFortify = Math.Min(cellsToFortify, livingCells.Count); // Don't exceed available cells

                // Randomly select cells to make resistant
                var cellsToFortifyList = new List<FungalCell>();
                for (int i = 0; i < cellsToFortify; i++)
                {
                    int randomIndex = rng.Next(livingCells.Count);
                    cellsToFortifyList.Add(livingCells[randomIndex]);
                    livingCells.RemoveAt(randomIndex); // Ensure no duplicates
                }

                // Make selected cells resistant
                foreach (var cell in cellsToFortifyList)
                {
                    cell.MakeResistant();
                }

                // Track the effect for simulation
                if (cellsToFortify > 0)
                {
                    observer?.RecordChitinFortificationCellsFortified(player.PlayerId, cellsToFortify);
                }
            }
        }

    }


}
