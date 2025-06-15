// FungusToast.Core/Phases/MutationEffectProcessor.cs
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Phases;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
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
                                                   Random rng)
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
                            dead.Reclaim(p.PlayerId);
                            board.PlaceFungalCell(dead);
                            p.AddControlledTile(dead.TileId);
                        }
                    }
                }
            }
        }


        public static void TryTriggerSporeOnDeath(Player player,
                                                  GameBoard board,
                                                  Random rng,
                                                  ISimulationObserver? observer = null)
        {
            float chance = player.GetMutationEffect(MutationType.SporeOnDeathChance);
            if (chance <= 0f || rng.NextDouble() > chance) return;

            var empty = board.AllTiles().Where(t => !t.IsOccupied).ToList();
            if (empty.Count == 0) return;

            BoardTile spawn = empty[rng.Next(empty.Count)];
            board.SpawnSporeForPlayer(player, spawn.TileId);

            observer?.ReportNecrosporeDrop(player.PlayerId, 1);
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

            List<Mutation> pool;
            MutationTier targetTier;

            // Hyperadaptive Drift: roll to see if we try for tier 2 or 3 instead of 1
            if (hasHyperadaptive && rng.NextDouble() < higherTierChance)
            {
                // Try tier 2 or 3 randomly
                var higherTiers = tier2.Concat(tier3).ToList();
                if (higherTiers.Count > 0)
                {
                    pool = higherTiers;
                    targetTier = rng.Next(2) == 0 ? MutationTier.Tier2 : MutationTier.Tier3;
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
                else // Tier 2 or 3
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

        public static bool TryCreepingMoldMove(Player player,
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

            var newCell = new FungalCell(player.PlayerId, targetTile.TileId);
            targetTile.PlaceFungalCell(newCell);
            player.AddControlledTile(targetTile.TileId);

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

        public static int TryPlaceSporocidalSpores(
            Player player,
            GameBoard board,
            Random rng,
            Mutation sporocidalBloom,
            ISimulationObserver? simulationObserver = null)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level <= 0) return 0;

            int livingCellCount = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive);
            if (livingCellCount == 0) return 0;

            // 1. Calculate spores to drop (matches prior game balance: linear scaling)
            float sporesPerCell = level * sporocidalBloom.EffectPerLevel;
            int sporesToDrop = (int)Math.Round(livingCellCount * sporesPerCell);
            if (sporesToDrop <= 0) return 0;

            int expiration = board.CurrentGrowthCycle + GameBalance.SporocidalToxinTileDuration;

            // Avoid double-hitting the same tile
            var allTiles = board.AllTiles().ToList();
            HashSet<int> alreadyTargeted = new HashSet<int>();
            int totalToxinsPlaced = 0;
            int totalKills = 0;

            for (int i = 0; i < sporesToDrop && alreadyTargeted.Count < allTiles.Count; i++)
            {
                // Pick a random tile that hasn't been targeted yet
                BoardTile tile;
                do
                {
                    tile = allTiles[rng.Next(allTiles.Count)];
                } while (!alreadyTargeted.Add(tile.TileId));

                // 1. If it's an enemy living cell, kill and toxify it
                if (tile.IsOccupied && tile.FungalCell != null && tile.FungalCell.IsAlive && tile.FungalCell.OwnerPlayerId != player.PlayerId)
                {
                    ToxinHelper.KillAndToxify(
                        board,
                        tile.TileId,
                        expiration,
                        DeathReason.SporocidalBloom,
                        player);

                    totalKills++;
                    totalToxinsPlaced++;
                    continue;
                }

                // 2. If it's a friendly cell (alive or dead) or adjacent to a friendly, do nothing
                bool isFriendly = tile.IsOccupied && tile.FungalCell != null && tile.FungalCell.OwnerPlayerId == player.PlayerId;
                if (isFriendly)
                    continue;

                bool adjacentToFriendly = board.GetOrthogonalNeighbors(tile.X, tile.Y)
                    .Any(n => n.IsOccupied && n.FungalCell != null && n.FungalCell.OwnerPlayerId == player.PlayerId);
                if (adjacentToFriendly)
                    continue;

                // 3. If it's empty or has a dead cell (not owned), and not next to a friendly, toxify it
                if (!tile.IsOccupied || (tile.FungalCell != null && !tile.FungalCell.IsAlive))
                {
                    ToxinHelper.ConvertToToxin(
                        board,
                        tile.TileId,
                        expiration,
                        player);

                    totalToxinsPlaced++;
                }
            }

            // Reporting (matches your new requirements)
            if (totalToxinsPlaced > 0)
                simulationObserver?.ReportSporocidalSporeDrop(player.PlayerId, totalToxinsPlaced);

            if (totalKills > 0)
                simulationObserver?.RecordCellDeath(player.PlayerId, DeathReason.SporocidalBloom, totalKills);

            return totalToxinsPlaced;
        }


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

        // Handles initial burst: call once when 20% occupancy is reached
        public static void TriggerNecrophyticBloomInitialBurst(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver? observer = null)
        {
            int level = player.GetMutationLevel(MutationIds.NecrophyticBloom);
            if (level <= 0) return;

            // Get all dead, non-toxin, non-empty cells owned by this player
            var deadCells = board.GetAllCellsOwnedBy(player.PlayerId)
                                 .Where(cell => !cell.IsAlive && !cell.IsToxin)
                                 .ToList();

            float sporesPerDeadCell = level * GameBalance.NecrophyticBloomSporesPerDeathPerLevel;
            float damping = 1f; // Initial burst: NO damping

            int totalSpores = (int)Math.Floor(sporesPerDeadCell * deadCells.Count * damping);
            if (totalSpores <= 0) return;

            var allTiles = board.AllTiles().ToList();
            int reclaims = 0;

            for (int i = 0; i < totalSpores; i++)
            {
                BoardTile target = allTiles[rng.Next(allTiles.Count)];

                if (target.FungalCell is { IsAlive: false, IsToxin: false })
                {
                    target.FungalCell.Reclaim(player.PlayerId);
                    player.AddControlledTile(target.TileId);
                    board.PlaceFungalCell(target.FungalCell);
                    reclaims++;
                }
            }

            observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, totalSpores, reclaims);
        }

        // Handles *per-death* spore drop AFTER activation
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

            var allTiles = board.AllTiles().ToList();
            int reclaims = 0;

            for (int i = 0; i < spores; i++)
            {
                BoardTile target = allTiles[rng.Next(allTiles.Count)];

                if (target.FungalCell is { IsAlive: false, IsToxin: false })
                {
                    target.FungalCell.Reclaim(owner.PlayerId);
                    owner.AddControlledTile(target.TileId);
                    board.PlaceFungalCell(target.FungalCell);
                    reclaims++;
                }
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
                    // Initial infiltration
                    ReclaimDeadCellAsLiving(deadTile, owner, board);

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
                        ReclaimDeadCellAsLiving(deadTile, owner, board);
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
                    enemyPlayer.AddControlledTile(deadCell.TileId);

                    // Log the reclaim if tracking
                    growthAndDecayObserver?.RecordNecrotoxicConversionReclaim(enemyPlayer.PlayerId, 1);
                    break; // Only one player can claim it
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


        private static void ReclaimDeadCellAsLiving(
            BoardTile tile,
            Player newOwner,
            GameBoard board)
        {
            // Remove old cell, create new one as living, assign ownership
            var newCell = new FungalCell(newOwner.PlayerId, tile.TileId);
            tile.PlaceFungalCell(newCell);
            board.PlaceFungalCell(newCell);
            newOwner.AddControlledTile(tile.TileId);
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
                                 (int)Math.Round(level * (double)GameBalance.HyphalVectoringTilesPerLevel);

                var origin = TrySelectHyphalVectorOrigin(player, board, rng, centerX, centerY, totalTiles);
                if (origin == null)
                {
                    Console.WriteLine($"[HyphalVectoring] Player {player.PlayerId}: no valid origin found.");
                    continue;
                }

                //Console.WriteLine($"[HyphalVectoring] Player {player.PlayerId} origin: {origin.Value.tile.TileId}");

                int placed = ApplyHyphalVectorLine(player, board, rng, origin.Value.tile.X, origin.Value.tile.Y, centerX, centerY, totalTiles, observer);
                if (placed > 0)
                    observer?.RecordHyphalVectoringGrowth(player.PlayerId, placed);
            }
        }

        private static (FungalCell cell, BoardTile tile)? TrySelectHyphalVectorOrigin(
            Player player,
            GameBoard board,
            Random rng,
            int centerX,
            int centerY,
            int totalTiles)
        {
            var candidates = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive)
                .Select(c =>
                {
                    var t = board.GetTileById(c.TileId)!;
                    int dx = centerX - t.X;
                    int dy = centerY - t.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    return new { cell = c, tile = t, dist };
                })
                .Where(entry =>
                {
                    if (entry.dist < totalTiles) return false;

                    var path = GetLineToCenter(entry.tile.X, entry.tile.Y, centerX, centerY, totalTiles);
                    foreach (var (x, y) in path)
                    {
                        var t = board.GetTile(x, y)!;
                        if (t.IsOccupied && t.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                            return false;
                    }
                    return true;
                })
                .OrderBy(e => e.dist)
                .ToList();

            return candidates.Count == 0
                ? null
                : (candidates[rng.Next(candidates.Count)].cell, candidates[rng.Next(candidates.Count)].tile);
        }

        private static int ApplyHyphalVectorLine(
            Player player,
            GameBoard board,
            Random rng,
            int startX,
            int startY,
            int centerX,
            int centerY,
            int totalTiles,
            ISimulationObserver? observer)
        {
            var path = GetLineToCenter(startX, startY, centerX, centerY, totalTiles);
            int placed = 0;

            foreach (var (x, y) in path)
            {
                var tile = board.GetTile(x, y)!;

                if (tile.IsOccupied && tile.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                {
                    throw new InvalidOperationException(
                        $"Hyphal Vectoring path encountered unexpected friendly cell at {tile.TileId}");
                }

                if (tile.IsOccupied && tile.FungalCell is { IsAlive: true } fc)
                {
                    fc.Kill(DeathReason.HyphalVectoring);
                    observer?.RecordCellDeath(player.PlayerId, DeathReason.HyphalVectoring, 1);
                }

                var newCell = new FungalCell(player.PlayerId, tile.TileId);
                tile.PlaceFungalCell(newCell);
                board.PlaceFungalCell(newCell);
                player.AddControlledTile(tile.TileId);
                placed++;
            }

            return placed;
        }



        private static List<(int x, int y)> GetLineToCenter(int fromX, int fromY, int toX, int toY, int maxLength)
        {
            var line = new List<(int x, int y)>();
            int dx = toX - fromX;
            int dy = toY - fromY;

            float stepX = dx / (float)Math.Max(Math.Abs(dx), Math.Abs(dy));
            float stepY = dy / (float)Math.Max(Math.Abs(dx), Math.Abs(dy));

            float cx = fromX + 0.5f;
            float cy = fromY + 0.5f;

            for (int i = 0; i < maxLength; i++)
            {
                cx += stepX;
                cy += stepY;

                int ix = (int)Math.Floor(cx);
                int iy = (int)Math.Floor(cy);

                line.Add((ix, iy));
            }

            return line;
        }



    }


}
