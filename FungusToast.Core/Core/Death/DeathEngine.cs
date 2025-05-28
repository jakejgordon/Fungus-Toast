using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
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
            foreach (var player in players)
            {
                RunSporeDropIfApplicable(board, player);
            }

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
                    continue; // Skip killing player's last cell
                }

                double roll = rng.NextDouble();
                var (deathChance, reason) = MutationEffectProcessor.CalculateDeathChance(
                    player, cell, board, players, roll);

                if (reason.HasValue && roll < deathChance)
                {
                    cell.Kill(reason.Value);
                    player.ControlledTileIds.Remove(cell.TileId);
                    MutationEffectProcessor.TryTriggerSporeOnDeath(player, board, rng);
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(player, cell);
                }
            }
        }

        private static void RunSporeDropIfApplicable(GameBoard board, Player player)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level <= 0) return;

            int livingCells = player.ControlledTileIds.Count;
            int sporesToDrop = level * (int)Math.Floor(Math.Log(livingCells + 1, 2));

            var allTileIds = Enumerable.Range(0, board.Width * board.Height).ToList();
            var sporeRng = new Random(player.PlayerId + livingCells); // deterministic

            for (int i = 0; i < sporesToDrop; i++)
            {
                int targetId = allTileIds[sporeRng.Next(allTileIds.Count)];
                var targetTile = board.GetTileById(targetId);
                var target = targetTile?.FungalCell;

                bool isEnemy = target != null && target.OwnerPlayerId != player.PlayerId;

                if (target != null && target.IsAlive && isEnemy)
                {
                    target.Kill(DeathReason.Fungicide);
                    board.MarkAsToxinTile(targetId, player.PlayerId, GameBalance.ToxinTileDuration);
                }
                else if ((target == null || !target.IsAlive) && (target == null || isEnemy))
                {
                    board.MarkAsToxinTile(targetId, player.PlayerId, GameBalance.ToxinTileDuration);
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
