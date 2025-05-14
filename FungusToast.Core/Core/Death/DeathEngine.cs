using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Death
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
                {
                    cell.CauseOfDeath = DeathReason.Protected;
                    continue;
                }

                float baseChance = GameBalance.BaseDeathChance;
                float ageMod = cell.GrowthCycleAge * GameBalance.AgeDeathFactorPerGrowthCycle;
                float defenseBonus = player.GetEffectiveSelfDeathChance();
                float pressure = GetEnemyPressure(players, player, cell, board);

                float finalChance = Math.Clamp(baseChance + ageMod + pressure - defenseBonus, 0f, 1f);

                bool died = false;

                if (rng.NextDouble() < baseChance)
                {
                    cell.CauseOfDeath = DeathReason.Randomness;
                    died = true;
                }
                else if (rng.NextDouble() < ageMod)
                {
                    cell.CauseOfDeath = DeathReason.Age;
                    died = true;
                }
                else if (rng.NextDouble() < pressure)
                {
                    cell.CauseOfDeath = DeathReason.EnemyDecayPressure;
                    died = true;
                }

                if (died)
                {
                    cell.Kill();
                    player.ControlledTileIds.Remove(cell.TileId);
                    TrySpawnSpore(player, board);
                }
                else
                {
                    cell.CauseOfDeath = null; // Survived this cycle
                    int resetAt = player.GetSelfAgeResetThreshold();
                    if (cell.GrowthCycleAge >= resetAt)
                        cell.ResetGrowthCycleAge();
                    else
                        cell.IncrementGrowthAge();
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

            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId == owner.PlayerId) continue;

                // Base offensive decay modifier
                float boost = enemy.GetOffensiveDecayModifierAgainst(targetCell, board);

                // Add adjacency stacking
                int adjacentOwnedByAttacker = board.GetAdjacentTileIds(targetCell.TileId)
                    .Select(id => board.GetCell(id))
                    .Where(cell => cell != null && cell.IsAlive && cell.OwnerPlayerId == enemy.PlayerId)
                    .Count();

                float toxinEffect = enemy.GetMutationEffect(MutationType.OpponentExtraDeathChance);
                float additionalBonus = adjacentOwnedByAttacker * toxinEffect;
                boost += additionalBonus;

                // Apply Encysted Spores multiplier if target is surrounded
                if (IsCellSurrounded(targetCell.TileId, board))
                {
                    float encystMultiplier = enemy.GetMutationEffect(MutationType.EncystedSporeMultiplier);
                    boost *= (1f + encystMultiplier);
                }

                if (boost < 0f) boost = 0f;
                total += boost;
            }

            return Math.Min(total, GameBalance.MaxEnemyDecayPressurePerCell);
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
