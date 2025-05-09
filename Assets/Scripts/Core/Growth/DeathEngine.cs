using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Growth
{
    public static class DeathEngine
    {
        public static void ExecuteDeathCycle(GameBoard board, List<Player> players)
        {
            /* 1) Collect all living cells -------------------------------- */
            var livingTiles = new List<BoardTile>();
            foreach (var t in board.AllTiles())
                if (t.FungalCell != null && t.FungalCell.IsAlive)
                    livingTiles.Add(t);

            /* 2) Evaluate each cell -------------------------------------- */
            foreach (var tile in livingTiles)
            {
                var cell = tile.FungalCell;
                var player = players.Find(p => p.PlayerId == cell.OwnerPlayerId);
                if (player == null)
                {
                    Debug.LogWarning($"No player found for PlayerId {cell.OwnerPlayerId}");
                    continue;
                }

                // Never kill a player’s final cell
                if (player.ControlledTileIds.Count <= 1)
                    continue;

                float ageMod = cell.GrowthCycleAge * GameBalance.AgeDeathFactorPerGrowthCycle;
                float defenseBonus = player.GetEffectiveSelfDeathChance();
                float pressure = GetEnemyPressure(players, player, cell, board);

                float finalChance = Mathf.Clamp01(
                    GameBalance.BaseDeathChance + ageMod + pressure - defenseBonus);

                if (Random.value < finalChance)
                {
                    /* ---- Cell dies -------------------------------------- */
                    cell.Kill();
                    player.ControlledTileIds.Remove(cell.TileId);
                    Debug.Log($"💀 Cell ({tile.X},{tile.Y}) P{player.PlayerId} died. Age={cell.GrowthCycleAge}  Chance={finalChance:P2}");

                    TrySpawnSpore(player, board);   // Necrosporulation
                }
                else
                {
                    /* ---- Cell survives / rejuvenates ------------------- */
                    int resetAt = player.GetSelfAgeResetThreshold();
                    if (cell.GrowthCycleAge >= resetAt)
                    {
                        cell.ResetGrowthCycleAge();
                        Debug.Log($"♻️ Cell ({tile.X},{tile.Y}) rejuvenated (reset at {resetAt}).");
                    }
                    else
                    {
                        cell.IncrementGrowthAge();
                    }
                }
            }
        }

        /* ========== Necrosporulation helper ============================= */

        private static void TrySpawnSpore(Player player, GameBoard board)
        {
            float chance = player.GetMutationEffect(MutationType.SporeOnDeathChance);
            if (chance <= 0f || Random.value > chance) return;

            var empty = new List<BoardTile>();
            foreach (var t in board.AllTiles())
                if (!t.IsOccupied) empty.Add(t);
            if (empty.Count == 0) return;

            var spawn = empty[Random.Range(0, empty.Count)];
            int tileId = spawn.Y * board.Width + spawn.X;

            if (board.SpawnSporeForPlayer(player, tileId))
                Debug.Log($"🌱 Necrospore for P{player.PlayerId} at ({spawn.X},{spawn.Y})");
        }

        /* ========== Enemy-pressure calculation ========================== */

        private static float GetEnemyPressure(List<Player> allPlayers,
                                              Player owner,
                                              FungalCell targetCell,
                                              GameBoard board)
        {
            float total = 0f;

            /* 1) Precompute which enemies are actually adjacent ---------- */
            var adjacentEnemyIds = new HashSet<int>();
            foreach (int id in board.GetAdjacentTileIds(targetCell.TileId))
            {
                var n = board.GetCell(id);
                if (n != null && n.IsAlive && n.OwnerPlayerId != owner.PlayerId)
                    adjacentEnemyIds.Add(n.OwnerPlayerId);
            }

            /* 2) Sum each enemy’s modifiers ------------------------------ */
            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId == owner.PlayerId) continue;

                float boost = enemy.GetOffensiveDecayModifierAgainst(targetCell, board);

                // Remove toxin portion if the enemy is not adjacent
                if (!adjacentEnemyIds.Contains(enemy.PlayerId))
                    boost -= enemy.GetMutationEffect(MutationType.OpponentExtraDeathChance);

                if (boost < 0f) boost = 0f;
                total += boost;
            }

            return total;
        }

        /* ========== Utility ============================================ */

        public static bool IsCellSurrounded(int tileId, GameBoard board)
        {
            var cell = board.GetCell(tileId);
            if (cell == null) return false;

            foreach (int nId in board.GetAdjacentTileIds(tileId))
            {
                var n = board.GetCell(nId);
                if (n == null || !n.IsAlive) return false;
            }
            return true;
        }
    }
}
