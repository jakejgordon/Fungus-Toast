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
                    (int x, int y) = board.GetXYFromTileId(cell.TileId);

                    foreach (BoardTile n in board.GetOrthogonalNeighbors(x, y))
                    {
                        FungalCell? dead = n.FungalCell;
                        if (dead is null || dead.IsAlive) continue;
                        if (dead.OriginalOwnerPlayerId != p.PlayerId) continue;
                        if (!attempted.Add(dead.TileId)) continue;

                        if (rng.NextDouble() < reclaimChance)
                        {
                            dead.Reclaim(p.PlayerId);
                            board.RegisterCell(dead);
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

        /* ────────────────────────────────────────────────────────────────
         * 2 ▸  DEATH-CHANCE CALCULATION
         * ────────────────────────────────────────────────────────────────*/

        public static (float chance, DeathReason? reason) CalculateDeathChance(
            Player owner,
            FungalCell cell,
            GameBoard board,
            List<Player> allPlayers,
            double roll)
        {
            // A. Mutation-specific lethal checks (short-circuit on success)

            // A-2  Putrefactive Mycotoxin
            if (CheckPutrefactiveMycotoxin(cell, board, allPlayers, out float pmChance) &&
                roll < pmChance)
            {
                return (pmChance, DeathReason.PutrefactiveMycotoxin);
            }

            // A-3  Encysted Spores
            if (CheckEncystedSpores(cell, board, allPlayers, out float esChance) &&
                roll < esChance)
            {
                return (esChance, DeathReason.EncystedSpores);
            }

            // A-4  Silent Blight
            if (CheckSilentBlight(cell, board, allPlayers, out float sbChance) &&
                roll < sbChance)
            {
                return (sbChance, DeathReason.SilentBlight);
            }

            // B. Fallback: randomness, age, generic pressure

            float baseChance = GameBalance.BaseDeathChance;
            float ageMod = cell.GrowthCycleAge *
                               GameBalance.AgeDeathFactorPerGrowthCycle;
            float pressure = GetEnemyPressure(allPlayers, owner, cell, board);
            float defense = owner.GetEffectiveSelfDeathChance();

            float total = Math.Clamp(baseChance + ageMod + pressure - defense, 0f, 1f);

            float thrRnd = baseChance;
            float thrAge = baseChance + ageMod;

            if (roll < total)
            {
                if (roll < thrRnd) return (total, DeathReason.Randomness);
                return (total, DeathReason.Age);
            }

            return (total, null);
        }

        public static void AdvanceOrResetCellAge(Player player, FungalCell cell)
        {
            int resetAt = player.GetSelfAgeResetThreshold();
            if (cell.GrowthCycleAge >= resetAt)
                cell.ResetGrowthCycleAge();
            else
                cell.IncrementGrowthAge();
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



        private static bool CheckEncystedSpores(FungalCell target,
                                        GameBoard board,
                                        List<Player> players,
                                        out float chance)
        {
            chance = 0f;

            // Only applies if this cell is fully surrounded
            if (!DeathEngine.IsCellSurrounded(target.TileId, board))
                return false;

            // Sum the actual Encysted Spore effect across all enemies
            foreach (Player enemy in players)
            {
                if (enemy.PlayerId == target.OwnerPlayerId) continue;

                chance += enemy.GetMutationEffect(MutationType.EncystedSporeMultiplier);
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

        /* ────────────────────────────────────────────────────────────────
         * 4 ▸  MOVEMENT & GROWTH HELPERS (unchanged logic)
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
            if (!player.PlayerMutations.TryGetValue(MutationIds.CreepingMold,
                                                    out var cm) ||
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
            var rng = new Random(player.PlayerId + livingCellCount);

            for (int i = 0; i < livingCellCount; i++)
                if (rng.NextDouble() < chancePerCell) spores++;

            return spores;
        }

        public static int ComputeSporocidalBloomSporeDropCount(int level,
                                                               int livingCellCount,
                                                               float effectPerLevel)
        {
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

            foreach (FungalCell cell in livingCells)
            {
                if (rng.NextDouble() > dropChance) continue;

                (int x, int y) = board.GetXYFromTileId(cell.TileId);
                var neighbours = board.GetOrthogonalNeighbors(x, y).ToList();
                if (neighbours.Count == 0) continue;

                BoardTile target = neighbours[rng.Next(neighbours.Count)];

                var cellAtTarget = board.GetCell(target.TileId);
                if (cellAtTarget?.IsAlive == true)
                {
                    ToxinHelper.KillAndToxify(
                        board,
                        target.TileId,
                        GameBalance.ToxinTileDuration,
                        DeathReason.SporocidalBloom);
                }
                else
                {
                    ToxinHelper.ConvertToToxin(
                        board,
                        target.TileId,
                        GameBalance.ToxinTileDuration);
                }

                spores++;
                observer?.ReportSporocidalSporeDrop(player.PlayerId, 1);
            }

            return spores;
        }



        /* ────────────────────────────────────────────────────────────────
         * 6 ▸  NECROPHYTIC BLOOM HELPERS (unchanged)
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

                if (target.FungalCell != null && !target.FungalCell.IsAlive)
                {
                    target.FungalCell.Reclaim(player.PlayerId);
                    player.AddControlledTile(target.TileId);
                    board.RegisterCell(target.FungalCell);
                    reclaims++;
                }
            }

            observer?.ReportNecrophyticBloomSporeDrop(player.PlayerId, spores, reclaims);
        }

    }
}
