// FungusToast.Core/Phases/MutationEffectProcessor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Centralizes all mutation-specific calculations and helpers.
    /// Phase orchestration lives in DeathEngine; this class never loops the full board.
    /// </summary>
    public static class MutationEffectProcessor
    {
        /* ────────────────────────────────────────────────────────────────
         * 1 ▸  TURN-LEVEL HELPERS
         * ────────────────────────────────────────────────────────────────*/

        public static void ApplyStartOfTurnEffects(GameBoard board,
                                                   List<Player> players,
                                                   Random rng)
        {
            // Reserved for future start-of-turn effects
        }

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
                                                  ISporeDropObserver? observer = null)
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
                if (roll < thresholdRandom) return (totalFallbackChance, DeathReason.Randomness);
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

        public static void TryApplyMutatorPhenotype(Player player,
                                                    List<Mutation> allMutations,
                                                    Random rng)
        {
            float chance = player.GetMutationEffect(MutationType.AutoUpgradeRandom);
            if (chance <= 0f || rng.NextDouble() >= chance) return;

            var upgradable = allMutations
                .Where(m => m.Tier == MutationTier.Tier1)
                .Where(player.CanUpgrade)
                .ToList();

            if (upgradable.Count == 0) return;

            var pick = upgradable[rng.Next(upgradable.Count)];
            player.TryAutoUpgrade(pick);
        }

        /* ────────────────────────────────────────────────────────────────
         * 3 ▸  ENEMY-PRESSURE & MUTATION-SPECIFIC CHECKS
         * ────────────────────────────────────────────────────────────────*/

        private static bool CheckSilentBlight(FungalCell target,
                                              GameBoard board,
                                              List<Player> players,
                                              out float chance)
        {
            chance = 0f;

            foreach (Player enemy in players)
            {
                if (enemy.PlayerId == target.OwnerPlayerId) continue;

                chance += enemy.GetMutationEffect(MutationType.EnemyDecayChance);
            }

            return chance > 0f;
        }

        private static bool CheckPutrefactiveMycotoxin(FungalCell target,
                                                       GameBoard board,
                                                       List<Player> players,
                                                       out float chance)
        {
            chance = 0f;

            var adjacentTiles = board.GetAdjacentTileIds(target.TileId);

            foreach (int tileId in adjacentTiles)
            {
                FungalCell? neighbor = board.GetCell(tileId);
                if (neighbor is null || !neighbor.IsAlive) continue;
                if (neighbor.OwnerPlayerId == target.OwnerPlayerId) continue;

                Player? enemy = players.FirstOrDefault(p => p.PlayerId == neighbor.OwnerPlayerId);
                if (enemy == null) continue;

                float effect = enemy.GetMutationEffect(MutationType.AdjacentFungicide);
                chance += effect;
            }

            return chance > 0f;
        }

        /* appears to be unused
        private static float GetEnemyPressure(List<Player> allPlayers,
                                              Player owner,
                                              FungalCell target,
                                              GameBoard board)
        {
            float total = 0f;

            foreach (Player enemy in allPlayers)
            {
                if (enemy.PlayerId == owner.PlayerId) continue;

                float baseBoost =
                    enemy.GetOffensiveDecayModifierAgainst(target, board);

                int adjacentEnemy =
                    board.GetAdjacentTileIds(target.TileId)
                         .Select(board.GetCell)
                         .Count(c => c is { IsAlive: true } &&
                                     c.OwnerPlayerId == enemy.PlayerId);

                float toxinEffect =
                    enemy.GetMutationEffect(MutationType.AdjacentFungicide);

                baseBoost += adjacentEnemy * toxinEffect;

                if (DeathEngine.IsCellSurrounded(target.TileId, board))
                {
                    float encystMult =
                        enemy.GetMutationEffect(MutationType.EncystedSporeMultiplier);

                    baseBoost *= 1f + encystMult;
                }

                total += Math.Max(baseBoost, 0f);
            }

            return Math.Min(total, GameBalance.MaxEnemyDecayPressurePerCell);
        }
        */

        /* ────────────────────────────────────────────────────────────────
         * 4 ▸  MOVEMENT & GROWTH HELPERS
         * ────────────────────────────────────────────────────────────────*/

        public static float GetDiagonalGrowthMultiplier(Player player)
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

        public static int TryPlaceSporocidalSpores(Player player,
                                                   GameBoard board,
                                                   Random rng,
                                                   Mutation sporocidalBloom,
                                                   ISporeDropObserver? observer = null)
        {
            float dropChance = player.GetMutationEffect(MutationType.FungicideSporeDrop);
            if (dropChance <= 0f) return 0;

            int spores = 0;

            var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                                   .Where(c => c.IsAlive)
                                   .ToList();

            int expiration = board.CurrentGrowthCycle + GameBalance.SporocidalToxinTileDuration;

            foreach (FungalCell cell in livingCells)
            {
                if (rng.NextDouble() > dropChance) continue;

                foreach (var neighbor in board.GetOrthogonalNeighbors(cell.TileId))
                {
                    var targetCell = neighbor.FungalCell;

                    if (targetCell is { IsAlive: true } &&
                        targetCell.OwnerPlayerId != player.PlayerId)
                    {
                        ToxinHelper.KillAndToxify(
                            board,
                            neighbor.TileId,
                            expiration,
                            DeathReason.SporocidalBloom,
                            player);

                        spores++;
                        observer?.ReportSporocidalSporeDrop(player.PlayerId, 1);
                    }
                    else if (!neighbor.IsOccupied ||
                             (neighbor.FungalCell != null && !neighbor.FungalCell.IsAlive))
                    {
                        ToxinHelper.ConvertToToxin(
                            board,
                            neighbor.TileId,
                            expiration, 
                            player);

                        spores++;
                        observer?.ReportSporocidalSporeDrop(player.PlayerId, 1);
                    }
                }
            }

            return spores;
        }

        /* ────────────────────────────────────────────────────────────────
         * 6 ▸  NECROPHYTIC BLOOM HELPERS
         * ────────────────────────────────────────────────────────────────*/

        public static int GetBaseSporesForNecrophyticBloom(Player player)
        {
            int lvl = player.GetMutationLevel(MutationIds.NecrophyticBloom);
            return lvl > 0 ? GameBalance.NecrophyticBloomBaseSpores + lvl : 0;
        }

        public static float GetNecrophyticBloomDamping(float occupiedPercent)
        {
            if (occupiedPercent <= 0.20f) return 1f;
            float raw = 1f - ((occupiedPercent - 0.20f) / 0.80f);
            return Math.Clamp(raw, 0f, 1f);
        }

        public static int GetEffectiveSporesForNecrophyticBloom(Player player,
                                                                float occupiedPercent)
        {
            int baseSpores = GetBaseSporesForNecrophyticBloom(player);
            float damping = GetNecrophyticBloomDamping(occupiedPercent);
            return (int)Math.Floor(baseSpores * damping);
        }

        public static void HandleNecrophyticBloomSporeDrop(Player player,
                                                           GameBoard board,
                                                           Random rng,
                                                           float occupiedPercent,
                                                           ISporeDropObserver? observer = null)
        {
            int spores = GetEffectiveSporesForNecrophyticBloom(player, occupiedPercent);
            if (spores <= 0) return;

            var allTiles = board.AllTiles().ToList();
            int reclaims = 0;

            for (int i = 0; i < spores; i++)
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

            observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, spores, reclaims);
        }

        public static int ApplyMycotoxinTracer(Player player,
                                       GameBoard board,
                                       int failedGrowthsThisRound,
                                       Random rng,
                                       ISporeDropObserver? observer = null)
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
                {
                    return board.GetAdjacentTileIds(t.TileId)
                                .Select(board.GetCell)
                                .Any(c => c is { IsAlive: true } &&
                                          c.OwnerPlayerId != player.PlayerId);
                })
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
                                         ISporeDropObserver? observer = null)
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
                    observer?.ReportAuraKill(owner.PlayerId, killCount);
                }
            }
        }


    }
}
