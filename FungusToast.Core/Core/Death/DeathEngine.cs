using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Death
{
    public static class DeathEngine
    {
        private static readonly Random rng = new();

        public static void ExecuteDeathCycle(GameBoard board, List<Player> players)
        {
            var livingTiles = board.AllTiles()
                                   .Where(t => t.FungalCell != null && t.FungalCell.IsAlive)
                                   .ToList();

            foreach (var tile in livingTiles)
            {
                var cell = tile.FungalCell!;
                var player = players.FirstOrDefault(p => p.PlayerId == cell.OwnerPlayerId);

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

                float deathChance = MutationEffectProcessor.CalculateDeathChance(player, cell, board, players);

                if (rng.NextDouble() < deathChance)
                {
                    cell.Kill(); // Sets CauseOfDeath internally
                    player.ControlledTileIds.Remove(cell.TileId);

                    MutationEffectProcessor.TryTriggerSporeOnDeath(player, board, rng);
                }
                else
                {
                    cell.CauseOfDeath = null;
                    MutationEffectProcessor.AdvanceOrResetCellAge(player, cell);
                }
            }
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
