using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Growth
{
    public static class DeathEngine
    {
        private static readonly Random rng = new();

        public static void ExecuteDeathCycle(GameBoard board, List<Player> players)
        {
            var livingTiles = new List<BoardTile>();
            foreach (var t in board.AllTiles())
                if (t.FungalCell != null && t.FungalCell.IsAlive)
                    livingTiles.Add(t);

            foreach (var tile in livingTiles)
            {
                var cell = tile.FungalCell;
                var player = players.Find(p => p.PlayerId == cell.OwnerPlayerId);
                if (player == null)
                {
                    Console.WriteLine($"[Warning] No player found for PlayerId {cell.OwnerPlayerId}");
                    continue;
                }

                if (player.ControlledTileIds.Count <= 1)
                    continue;

                float ageMod = cell.GrowthCycleAge * GameBalance.AgeDeathFactorPerGrowthCycle;
                float defenseBonus = player.GetEffectiveSelfDeathChance();
                float pressure = GetEnemyPressure(players, player, cell, board);

                float finalChance = Math.Clamp(
                    GameBalance.BaseDeathChance + ageMod + pressure - defenseBonus, 0f, 1f);

                if (rng.NextDouble() < finalChance)
                {
                    cell.Kill();
                    player.ControlledTileIds.Remove(cell.TileId);

                    TrySpawnSpore(player, board);
                }
                else
                {
                    int resetAt = player.GetSelfAgeResetThreshold();
                    if (cell.GrowthCycleAge >= resetAt)
                    {
                        cell.ResetGrowthCycleAge();
                    }
                    else
                    {
                        cell.IncrementGrowthAge();
                    }
                }
            }
        }

        private static void TrySpawnSpore(Player player, GameBoard board)
        {
            float chance = player.GetMutationEffect(MutationType.SporeOnDeathChance);
            if (chance <= 0f || rng.NextDouble() > chance) return;

            var empty = board.AllTiles().Where(t => !t.IsOccupied).ToList();
            if (empty.Count == 0) return;

            var spawn = empty[rng.Next(0, empty.Count)];
            int tileId = spawn.Y * board.Width + spawn.X;

            board.SpawnSporeForPlayer(player, tileId);
        }

        private static float GetEnemyPressure(List<Player> allPlayers,
                                              Player owner,
                                              FungalCell targetCell,
                                              GameBoard board)
        {
            float total = 0f;
            var adjacentEnemyIds = new HashSet<int>();

            foreach (int id in board.GetAdjacentTileIds(targetCell.TileId))
            {
                var n = board.GetCell(id);
                if (n != null && n.IsAlive && n.OwnerPlayerId != owner.PlayerId)
                    adjacentEnemyIds.Add(n.OwnerPlayerId);
            }

            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId == owner.PlayerId) continue;

                float boost = enemy.GetOffensiveDecayModifierAgainst(targetCell, board);

                if (!adjacentEnemyIds.Contains(enemy.PlayerId))
                    boost -= enemy.GetMutationEffect(MutationType.OpponentExtraDeathChance);

                if (boost < 0f) boost = 0f;
                total += boost;
            }

            return total;
        }

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
