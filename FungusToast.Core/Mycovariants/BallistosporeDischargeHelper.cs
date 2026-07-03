using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class BallistosporeDischargeHelper
    {
        private const int UnreachableDistanceBand = int.MaxValue;

        public static IReadOnlyList<int> SelectBallistosporeDischargeTargetTileIds(
            PlayerMycovariant playerMyco,
            GameBoard board,
            int sporesToDrop,
            Random rng,
            IReadOnlyCollection<int>? excludedTileIds = null)
        {
            var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
            if (player == null || sporesToDrop <= 0)
            {
                return Array.Empty<int>();
            }

            var excluded = excludedTileIds != null && excludedTileIds.Count > 0
                ? new HashSet<int>(excludedTileIds)
                : null;

            var emptyTiles = board.AllTiles()
                .Where(t => !t.IsOccupiedForSporePlacement && (excluded == null || !excluded.Contains(t.TileId)))
                .ToList();
            int maxSpores = Math.Min(sporesToDrop, emptyTiles.Count);
            if (maxSpores == 0)
            {
                return Array.Empty<int>();
            }

            var topPlayers = board.Players
                .Where(p => p.PlayerId != player.PlayerId)
                .OrderByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .Take(3)
                .Select(p => p.PlayerId)
                .ToHashSet();

            var livingEnemyTiles = board.AllTiles()
                .Where(tile =>
                    tile.FungalCell != null &&
                    tile.FungalCell.IsAlive &&
                    tile.FungalCell.OwnerPlayerId != player.PlayerId &&
                    topPlayers.Contains(tile.FungalCell.OwnerPlayerId ?? -1))
                .ToList();

            return emptyTiles
                .Select(tile => new
                {
                    TileId = tile.TileId,
                    DistanceBand = GetEnemyProximityBand(tile, livingEnemyTiles)
                })
                .OrderBy(entry => entry.DistanceBand)
                .ThenBy(_ => rng.Next())
                .Take(maxSpores)
                .Select(entry => entry.TileId)
                .ToList();
        }

        public static void ResolveBallistosporeDischarge(
            PlayerMycovariant playerMyco,
            GameBoard board,
            int sporesToDrop,
            Random rng,
            ISimulationObserver observer)
        {
            var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
            if (player == null) return;

            var selectedTileIds = SelectBallistosporeDischargeTargetTileIds(playerMyco, board, sporesToDrop, rng);
            if (selectedTileIds.Count == 0)
            {
                return;
            }

            foreach (int tileId in selectedTileIds)
            {
                int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, MycovariantGameBalance.BallistosporeDischargeToxinDuration);
                ToxinHelper.ConvertToToxin(board, tileId, toxinLifespan, GrowthSource.Ballistospore, player);
            }

            playerMyco.IncrementEffectCount(MycovariantEffectType.Drops, selectedTileIds.Count);
            observer.RecordBallistosporeDischarge(player.PlayerId, selectedTileIds.Count);
        }

        /// <summary>
        /// For human players: applies a single Ballistospore Discharge spore to the specified tile.
        /// </summary>
        public static void ResolveBallistosporeDischargeHuman(
            PlayerMycovariant playerMyco,
            GameBoard board,
            int tileId,
            ISimulationObserver observer)
        {
            var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
            if (player == null) return;
            var tile = board.GetTileById(tileId);
            if (tile == null || tile.IsOccupiedForSporePlacement)
            {
                return;
            }

            int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, MycovariantGameBalance.BallistosporeDischargeToxinDuration);
            ToxinHelper.ConvertToToxin(board, tileId, toxinLifespan, GrowthSource.Ballistospore, player);
            playerMyco.IncrementEffectCount(MycovariantEffectType.Drops, 1);
            observer.RecordBallistosporeDischarge(player.PlayerId, 1);
        }

        private static int GetEnemyProximityBand(BoardTile tile, IReadOnlyCollection<BoardTile> livingEnemyTiles)
        {
            if (livingEnemyTiles.Count == 0)
            {
                return UnreachableDistanceBand;
            }

            int closestBand = UnreachableDistanceBand;
            foreach (var enemyTile in livingEnemyTiles)
            {
                int dx = Math.Abs(tile.X - enemyTile.X);
                int dy = Math.Abs(tile.Y - enemyTile.Y);

                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int band = dx + dy == 1
                    ? 0
                    : dx == 1 && dy == 1
                        ? 1
                        : Math.Max(dx, dy);

                if (band < closestBand)
                {
                    closestBand = band;
                    if (closestBand == 0)
                    {
                        break;
                    }
                }
            }

            return closestBand;
        }
    }
}
