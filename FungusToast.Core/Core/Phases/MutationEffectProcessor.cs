using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    public static class MutationEffectProcessor
    {
        public static void ApplyStartOfTurnEffects(GameBoard board, List<Player> players, Random rng)
        {
            ApplyRegenerativeHyphaeReclaims(board, players, rng);
        }

        public static void ApplyRegenerativeHyphaeReclaims(GameBoard board, List<Player> players, Random rng)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.RegenerativeHyphae);
                if (level <= 0)
                    continue;

                float reclaimChance = GameBalance.RegenerativeHyphaeReclaimChance * level;
                var playerCells = board.GetAllCellsOwnedBy(player.PlayerId);

                foreach (var cell in playerCells)
                {
                    var (x, y) = board.GetXYFromTileId(cell.TileId);
                    var neighbors = board.GetOrthogonalNeighbors(x, y);

                    foreach (var neighbor in neighbors)
                    {
                        var deadCell = neighbor.FungalCell;
                        if (deadCell == null || deadCell.IsAlive)
                            continue;

                        if (deadCell.OriginalOwnerPlayerId != player.PlayerId)
                            continue;

                        if (rng.NextDouble() < reclaimChance)
                        {
                            deadCell.Reclaim(player.PlayerId);
                            board.RegisterCell(deadCell);
                            player.AddControlledTile(deadCell.TileId);
                        }
                    }
                }
            }
        }

        public static void TryTriggerSporeOnDeath(
            Player player,
            GameBoard board,
            Random rng,
            ISporeDropObserver? observer = null)
        {
            float chance = player.GetMutationEffect(MutationType.SporeOnDeathChance);
            if (chance <= 0f || rng.NextDouble() > chance) return;

            var empty = board.AllTiles().Where(t => !t.IsOccupied).ToList();
            if (empty.Count == 0) return;

            var spawn = empty[rng.Next(0, empty.Count)];
            int tileId = spawn.TileId;

            board.SpawnSporeForPlayer(player, tileId);

            observer?.ReportNecrosporeDrop(player.PlayerId, 1);
        }

        public static (float chance, DeathReason? reason) CalculateDeathChance(
            Player player,
            FungalCell cell,
            GameBoard board,
            List<Player> players,
            double roll)
        {
            float baseChance = GameBalance.BaseDeathChance;
            float ageMod = cell.GrowthCycleAge * GameBalance.AgeDeathFactorPerGrowthCycle;
            float pressure = GetEnemyPressure(players, player, cell, board);
            float defenseBonus = player.GetEffectiveSelfDeathChance();

            float totalChance = baseChance + ageMod + pressure - defenseBonus;
            float clampedChance = Math.Clamp(totalChance, 0f, 1f);

            float thresholdRandom = baseChance;
            float thresholdAge = baseChance + ageMod;
            float thresholdEnemy = baseChance + ageMod + pressure;

            if (roll < clampedChance)
            {
                if (roll < thresholdRandom)
                    return (clampedChance, DeathReason.Randomness);
                else if (roll < thresholdAge)
                    return (clampedChance, DeathReason.Age);
                else
                    return (clampedChance, DeathReason.EnemyDecayPressure);
            }

            return (clampedChance, null);
        }

        public static void AdvanceOrResetCellAge(Player player, FungalCell cell)
        {
            int resetAt = player.GetSelfAgeResetThreshold();
            if (cell.GrowthCycleAge >= resetAt)
                cell.ResetGrowthCycleAge();
            else
                cell.IncrementGrowthAge();
        }

        private static float GetEnemyPressure(List<Player> allPlayers, Player owner, FungalCell targetCell, GameBoard board)
        {
            float total = 0f;

            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId == owner.PlayerId) continue;

                float boost = enemy.GetOffensiveDecayModifierAgainst(targetCell, board);

                int adjacentOwnedByAttacker = board.GetAdjacentTileIds(targetCell.TileId)
                    .Select(id => board.GetCell(id))
                    .Where(cell => cell != null && cell.IsAlive && cell.OwnerPlayerId == enemy.PlayerId)
                    .Count();

                float toxinEffect = enemy.GetMutationEffect(MutationType.OpponentExtraDeathChance);
                float additionalBonus = adjacentOwnedByAttacker * toxinEffect;
                boost += additionalBonus;

                if (DeathEngine.IsCellSurrounded(targetCell.TileId, board))
                {
                    float encystMultiplier = enemy.GetMutationEffect(MutationType.EncystedSporeMultiplier);
                    boost *= (1f + encystMultiplier);
                }

                if (boost < 0f) boost = 0f;
                total += boost;
            }

            return Math.Min(total, GameBalance.MaxEnemyDecayPressurePerCell);
        }

        public static float GetDiagonalGrowthMultiplier(Player player)
        {
            return 1f + player.GetMutationEffect(MutationType.TendrilDirectionalMultiplier);
        }

        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board)
        {
            if (!player.PlayerMutations.TryGetValue(MutationIds.CreepingMold, out var creepingMold) || creepingMold.CurrentLevel == 0)
                return false;

            if (player.ControlledTileIds.Count <= 1)
                return false;

            if (targetTile.IsOccupied)
                return false;

            float moveChance = creepingMold.CurrentLevel * GameBalance.CreepingMoldMoveChancePerLevel;
            if (rng.NextDouble() > moveChance)
                return false;

            int sourceOpen = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                  .Count(n => !n.IsOccupied);

            int targetOpen = board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                                  .Count(n => !n.IsOccupied);

            if (targetOpen < sourceOpen || targetOpen < 2)
                return false;

            var newCell = new FungalCell(player.PlayerId, targetTile.TileId);
            targetTile.PlaceFungalCell(newCell);
            player.AddControlledTile(targetTile.TileId);

            sourceTile.RemoveFungalCell();
            player.RemoveControlledTile(sourceCell.TileId);

            return true;
        }

        public static int GetSporocidalSporeDropCount(Player player, int livingCellCount, Mutation sporocidalBloomMutation)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level <= 0 || livingCellCount == 0) return 0;

            float effectPerLevel = sporocidalBloomMutation.EffectPerLevel;
            float chancePerCell = level * effectPerLevel;

            int totalSpores = 0;
            var rng = new Random(player.PlayerId + livingCellCount);

            for (int i = 0; i < livingCellCount; i++)
            {
                if (rng.NextDouble() < chancePerCell)
                    totalSpores++;
            }

            return totalSpores;
        }

        public static int ComputeSporocidalBloomSporeDropCount(int level, int livingCellCount, float effectPerLevel)
        {
            float dropRate = level * effectPerLevel;
            float estimatedSpores = livingCellCount * dropRate;
            return Math.Max(1, (int)Math.Round(estimatedSpores));
        }

        public static int TryPlaceSporocidalSpores(
            Player player,
            GameBoard board,
            Random rng,
            Mutation sporocidalBloom,
            ISporeDropObserver? tracking = null)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level <= 0) return 0;

            int sporesPlaced = 0;
            float dropChance = level * sporocidalBloom.EffectPerLevel;

            var allLivingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                                      .Where(c => c.IsAlive).ToList();

            foreach (var cell in allLivingCells)
            {
                if (rng.NextDouble() > dropChance)
                    continue;

                var (x, y) = board.GetXYFromTileId(cell.TileId);
                var neighbors = board.GetOrthogonalNeighbors(x, y)
                                     .Where(n => !n.IsOccupied).ToList();

                if (neighbors.Count == 0)
                    continue;

                var target = neighbors[rng.Next(0, neighbors.Count)];
                board.MarkAsToxinTile(target.TileId, player.PlayerId, GameBalance.ToxinTileDuration);

                sporesPlaced++;

                tracking?.ReportSporocidalSporeDrop(player.PlayerId, 1);
            }

            return sporesPlaced;
        }

        public static int GetBaseSporesForNecrophyticBloom(Player player)
        {
            int level = player.GetMutationLevel(MutationIds.NecrophyticBloom);
            return level > 0 ? GameBalance.NecrophyticBloomBaseSpores + level : 0;
        }

        public static float GetNecrophyticBloomDamping(float occupiedPercent)
        {
            if (occupiedPercent <= 0.2f) return 1f;
            float rawDamping = 1f - ((occupiedPercent - 0.2f) / 0.8f);
            return Math.Clamp(rawDamping, 0f, 1f);
        }

        public static int GetEffectiveSporesForNecrophyticBloom(Player player, float occupiedPercent)
        {
            int baseSpores = GetBaseSporesForNecrophyticBloom(player);
            float damping = GetNecrophyticBloomDamping(occupiedPercent);
            return (int)Math.Floor(baseSpores * damping);
        }

        public static void HandleNecrophyticBloomSporeDrop(
            Player player,
            GameBoard board,
            Random rng,
            float occupiedPercent,
            ISporeDropObserver? observer = null)
        {
            int spores = GetEffectiveSporesForNecrophyticBloom(player, occupiedPercent);
            if (spores <= 0) return;

            var deadTiles = board.GetDeadTiles();

            int reclaims = 0;

            for (int i = 0; i < spores; i++)
            {
                if (deadTiles.Count == 0) break;

                var targetTile = deadTiles[rng.Next(deadTiles.Count)];
                int targetTileId = targetTile.FungalCell!.TileId;
                var cell = board.GetCell(targetTileId);
                if (cell != null && !cell.IsAlive)
                {
                    // Reclaim the cell
                    cell.Reclaim(player.PlayerId);

                    // Track ownership and re-register cell
                    player.AddControlledTile(targetTileId);
                    board.RegisterCell(cell);

                    reclaims++;
                }
            }

            observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, spores, reclaims);
        }

    }
}
