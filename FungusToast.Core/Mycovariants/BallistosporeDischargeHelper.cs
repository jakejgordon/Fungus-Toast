using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Mycovariants
{
    public static class BallistosporeDischargeHelper
    {
        public static void ResolveBallistosporeDischarge(
            PlayerMycovariant playerMyco,
            GameBoard board,
            int sporesToDrop,
            Random rng,
            ISimulationObserver? observer)
        {
            var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
            if (player == null) return;

            // Get all empty tiles
            var emptyTiles = board.AllTiles().Where(t => t.FungalCell == null).ToList();
            int maxSpores = Math.Min(sporesToDrop, emptyTiles.Count);
            if (maxSpores == 0) return;

            // AI: target top 2 ENEMY players with most living cells
            if (player.PlayerType != PlayerTypeEnum.Human)
            {
                var topPlayers = board.Players
                    .Where(p => p.PlayerId != player.PlayerId)
                    .OrderByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .Take(3)
                    .Select(p => p.PlayerId)
                    .ToHashSet();

                // Find empty tiles adjacent to top players' living cells (enemy only)
                var targetTiles = emptyTiles
                    .Where(tile => board.GetOrthogonalNeighbors(tile.X, tile.Y)
                        .Any(n => n.FungalCell != null && n.FungalCell.IsAlive && n.FungalCell.OwnerPlayerId != player.PlayerId && topPlayers.Contains(n.FungalCell.OwnerPlayerId ?? -1)))
                    .ToList();

                // If not enough, fill with random empty tiles
                if (targetTiles.Count < maxSpores)
                {
                    var extra = emptyTiles.Except(targetTiles).OrderBy(_ => rng.Next()).Take(maxSpores - targetTiles.Count).ToList();
                    targetTiles.AddRange(extra);
                }
                else
                {
                    targetTiles = targetTiles.OrderBy(_ => rng.Next()).Take(maxSpores).ToList();
                }

                foreach (var tile in targetTiles)
                {
                    int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, MycovariantGameBalance.BallistosporeDischargeToxinDuration);
                    ToxinHelper.ConvertToToxin(board, tile.TileId, toxinLifespan, GrowthSource.Ballistospore, player);
                }
                playerMyco.IncrementEffectCount(MycovariantEffectType.Drops, targetTiles.Count);
                observer?.RecordBallistosporeDischarge(player.PlayerId, targetTiles.Count);
            }
            // Human: UI will handle selection and call effect per tile
        }

        /// <summary>
        /// For human players: applies a single Ballistospore Discharge spore to the specified tile.
        /// </summary>
        public static void ResolveBallistosporeDischargeHuman(
            PlayerMycovariant playerMyco,
            GameBoard board,
            int tileId,
            ISimulationObserver? observer)
        {
            var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
            if (player == null) return;
            // Use custom duration for Ballistospore Discharge, with all bonuses
            int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, MycovariantGameBalance.BallistosporeDischargeToxinDuration);
            ToxinHelper.ConvertToToxin(board, tileId, toxinLifespan, GrowthSource.Ballistospore, player);
            playerMyco.IncrementEffectCount(MycovariantEffectType.Drops, 1);
            observer?.RecordBallistosporeDischarge(player.PlayerId, 1);
        }
    }
}
