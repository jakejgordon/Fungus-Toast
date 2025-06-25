// FungusToast.Core/Phases/MutationEffectProcessor.cs
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
        public static void ApplyRegenerativeHyphaeReclaims(GameBoard board,
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
                        if (dead.OriginalOwnerPlayerId != p.PlayerId) continue;
                        if (!attempted.Add(dead.TileId)) continue;
                        if (rng.NextDouble() < reclaimChance)
                        {
                            // Try to reclaim the dead cell using board API
                            bool success = board.TryGrowFungalCell(
                                p.PlayerId,
                                cell.TileId,   // Source tile: living cell
                                dead.TileId,   // Target: dead cell tile
                                out GrowthFailureReason reason, canReclaimDeadCell: true
                            );
                            if (success)
                            {
                                observer?.RecordRegenerativeHyphaeReclaim(p.PlayerId);
                            }
                        }
                    }
                }
            }
        }


        public static (float chance, DeathReason? reason) CalculateDeathChance(
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
                    return (totalFallbackChance, DeathReason.Randomness);
                return (totalFallbackChance, DeathReason.Age);
            }

            if (CheckPutrefactiveMycotoxin(cell, board, allPlayers, out float pmChance) &&
                roll < pmChance)
            {
                return (pmChance, DeathReason.PutrefactiveMycotoxin);
            }

            return (totalFallbackChance, null);
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

            // Gather upgradable mutations by tier
            var tier1 = allMutations.Where(m => m.Tier == MutationTier.Tier1 && player.CanUpgrade(m)).ToList();
            var tier2 = allMutations.Where(m => m.Tier == MutationTier.Tier2 && player.CanUpgrade(m)).ToList();
            var tier3 = allMutations.Where(m => m.Tier == MutationTier.Tier3 && player.CanUpgrade(m)).ToList();
            var tier4 = allMutations.Where(m => m.Tier == MutationTier.Tier4 && player.CanUpgrade(m)).ToList();

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
                bool upgraded = player.TryAutoUpgrade(pick);
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

            // Notify observer
            if (observer != null)
            {
                if (mutatorPoints > 0)
                    observer.RecordMutatorPhenotypeMutationPointsEarned(player.PlayerId, mutatorPoints);

                if (hyperadaptivePoints > 0)
                    observer.RecordHyperadaptiveDriftMutationPointsEarned(player.PlayerId, hyperadaptivePoints);
            }
        }




        /* ────────────────────────────────────────────────────────────────
         * 3 ▸  ENEMY-PRESSURE & MUTATION-SPECIFIC CHECKS
         * ────────────────────────────────────────────────────────────────*/

        private static bool CheckPutrefactiveMycotoxin(FungalCell target,
                                                       GameBoard board,
                                                       List<Player> players,
                                                       out float chance)
        {
            chance = 0f;

            var adjacentTiles = board.GetAdjacentTileIds(target.TileId);

            foreach (var neighborTile in board.GetAdjacentTiles(target.TileId))
            {
                var neighbor = neighborTile.FungalCell;
                if (neighbor is null || !neighbor.IsAlive) continue;
                if (neighbor.OwnerPlayerId == target.OwnerPlayerId) continue;

                Player? enemy = players.FirstOrDefault(p => p.PlayerId == neighbor.OwnerPlayerId);
                if (enemy == null) continue;

                float effect = enemy.GetMutationEffect(MutationType.AdjacentFungicide);
                chance += effect;
            }

            return chance > 0f;
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
        /// <returns>True if the move succeeded; false otherwise.</returns>
        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board)
        {
            if (!player.PlayerMutations.TryGetValue(MutationIds.CreepingMold, out var cm) ||
                cm.CurrentLevel == 0)
                return false;

            if (player.ControlledTileIds.Count <= 1) return false;
            if (targetTile.IsOccupied) return false;

            float moveChance =
                cm.CurrentLevel * GameBalance.CreepingMoldMoveChancePerLevel;

            if (rng.NextDouble() > moveChance) return false;

            int sourceOpen = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                  .Count(n => !n.IsOccupied);

            int targetOpen = board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                                  .Count(n => !n.IsOccupied);

            if (targetOpen < sourceOpen || targetOpen < 2) return false;

            // Create new cell in target location
            var newCell = new FungalCell(player.PlayerId, targetTile.TileId);
            board.PlaceFungalCell(newCell); // Event hooks will fire as appropriate

            // Remove source cell
            sourceTile.RemoveFungalCell();
            player.RemoveControlledTile(sourceCell.TileId);

            return true;
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

            // 1. Randomized base toxin count based on level
            int toxinsFromLevel = rng.Next(0, (level + 1) / 2);

            // 2. Failed growth bonus scales with both fails and level
            float weightedFailures = failedGrowthsThisRound * level * GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel;
            int toxinsFromFailures = rng.Next(0, (int)weightedFailures + 1);

            int totalToxins = toxinsFromLevel + toxinsFromFailures;
            totalToxins = Math.Min(totalToxins, maxToxinsThisRound);

            if (totalToxins == 0) return 0;

            // 3. Target tiles that are unoccupied, not toxic, and adjacent to enemy mold
            List<BoardTile> candidateTiles = board.AllTiles()
                .Where(t => !t.IsOccupied)
                .Where(t =>
                    board.GetAdjacentTiles(t.TileId)
                         .Any(n => n.FungalCell is { IsAlive: true } && n.FungalCell.OwnerPlayerId != player.PlayerId)
                )
                .ToList();

            int placed = 0;
            for (int i = 0; i < totalToxins && candidateTiles.Count > 0; i++)
            {
                int index = rng.Next(candidateTiles.Count);
                BoardTile chosen = candidateTiles[index];
                candidateTiles.RemoveAt(index);

                int expiration = board.CurrentGrowthCycle + GameBalance.MycotoxinTracerTileDuration;
                ToxinHelper.ConvertToToxin(board, chosen.TileId, expiration, player);
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
                        neighborTile.FungalCell!.Kill(DeathReason.MycotoxinPotentiation);
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

                foreach (var neighborTile in board.GetAdjacentTiles(cell.TileId))
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
                if (rng.NextDouble() <= baseChance)
                {
                    // Initial infiltration (reclaim as living)
                    var reclaimedCell = deadTile.FungalCell!;
                    reclaimedCell.Reclaim(owner.PlayerId);
                    board.PlaceFungalCell(reclaimedCell); // Use board method for events!

                    // Track which tiles have already been reclaimed
                    var alreadyReclaimed = new HashSet<int> { deadTile.TileId };

                    // Cascade infiltrations
                    int cascadeCount = CascadeNecrohyphalInfiltration(
                        board, deadTile, owner, rng, cascadeChance, alreadyReclaimed);

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
           double cascadeChance,
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
                    if (rng.NextDouble() <= cascadeChance)
                    {
                        var reclaimedCell = deadTile.FungalCell!;
                        reclaimedCell.Reclaim(owner.PlayerId);
                        board.PlaceFungalCell(reclaimedCell); // Use board method for events!
                        alreadyReclaimed.Add(deadTile.TileId);

                        cascadeCount++;
                        toCheck.Enqueue(deadTile);
                    }
                }
            }

            return cascadeCount;
        }


        public static void TryNecrotoxicConversion(
           FungalCell deadCell,
           GameBoard board,
           List<Player> players,
           Random rng,
           ISimulationObserver? growthAndDecayObserver = null)
        {
            // Only applies to toxin-based deaths
            if (deadCell.CauseOfDeath != DeathReason.PutrefactiveMycotoxin &&
                deadCell.CauseOfDeath != DeathReason.SporocidalBloom &&
                deadCell.CauseOfDeath != DeathReason.MycotoxinPotentiation)
                return;

            foreach (var neighbor in board.GetAdjacentTiles(deadCell.TileId))
            {
                var neighborCell = neighbor.FungalCell;
                if (neighborCell == null || !neighborCell.IsAlive) continue;
                int neighborOwnerId = neighborCell.OwnerPlayerId ?? -1;
                if (neighborOwnerId == deadCell.OwnerPlayerId) continue;

                var enemyPlayer = players.FirstOrDefault(p => p.PlayerId == neighborOwnerId);
                if (enemyPlayer == null) continue;

                int ntcLevel = enemyPlayer.GetMutationLevel(MutationIds.NecrotoxicConversion);
                if (ntcLevel <= 0) continue;

                float chance = ntcLevel * GameBalance.NecrotoxicConversionReclaimChancePerLevel;
                if (rng.NextDouble() < chance)
                {
                    deadCell.Reclaim(enemyPlayer.PlayerId);
                    board.PlaceFungalCell(deadCell);

                    // Log the reclaim if tracking
                    growthAndDecayObserver?.RecordNecrotoxicConversionReclaim(enemyPlayer.PlayerId, 1);
                    break; // Only one player can claim it
                }
            }
        }

        /// <summary>
        /// Handles the Necrotoxic Conversion effect in response to a fungal cell death event.
        /// If the cell died to a toxin effect and an adjacent enemy has the mutation,
        /// the cell may be reclaimed for that enemy player.
        /// </summary>
        /// <param name="eventArgs">Cell death event arguments.</param>
        /// <param name="board">Game board instance.</param>
        /// <param name="players">List of all players.</param>
        /// <param name="rng">RNG instance.</param>
        /// <param name="observer">Optional simulation observer.</param>
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

            // Get the (now dead) cell
            var deadCell = eventArgs.Cell;
            if (deadCell == null)
                return;

            // Find adjacent living enemy cells
            foreach (var neighbor in board.GetAdjacentTiles(deadCell.TileId))
            {
                var neighborCell = neighbor.FungalCell;
                if (neighborCell == null || !neighborCell.IsAlive)
                    continue;

                int neighborOwnerId = neighborCell.OwnerPlayerId ?? -1;
                // Only consider enemy players
                if (neighborOwnerId == deadCell.OwnerPlayerId)
                    continue;

                var enemyPlayer = players.FirstOrDefault(p => p.PlayerId == neighborOwnerId);
                if (enemyPlayer == null)
                    continue;

                int ntcLevel = enemyPlayer.GetMutationLevel(MutationIds.NecrotoxicConversion);
                if (ntcLevel <= 0)
                    continue;

                float chance = ntcLevel * GameBalance.NecrotoxicConversionReclaimChancePerLevel;
                if (rng.NextDouble() < chance)
                {
                    deadCell.Reclaim(enemyPlayer.PlayerId);
                    board.PlaceFungalCell(deadCell); // This will fire appropriate board events

                    observer?.RecordNecrotoxicConversionReclaim(enemyPlayer.PlayerId, 1);
                    break; // Only one player can claim the cell
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

                // Replace the helper with one that uses TryGrowFungalCell for each growth:
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
                    // Only grow if not already occupied (optional, based on rule)
                    if (board.GetTileById(targetTileId)?.IsOccupied == true)
                        break;

                    if (board.TryGrowFungalCell(player.PlayerId, currentTileId, targetTileId, out GrowthFailureReason reason))
                        placed++;

                    currentTileId = targetTileId;
                }

                if (placed > 0)
                    observer?.RecordHyphalVectoringGrowth(player.PlayerId, placed);
            }
        }


    }


}
