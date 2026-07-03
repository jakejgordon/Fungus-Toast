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

            var targetTiles = emptyTiles
                .Where(tile => board.GetOrthogonalNeighbors(tile.X, tile.Y)
                    .Any(n => n.FungalCell != null
                        && n.FungalCell.IsAlive
                        && n.FungalCell.OwnerPlayerId != player.PlayerId
                        && topPlayers.Contains(n.FungalCell.OwnerPlayerId ?? -1)))
                .ToList();

            if (targetTiles.Count < maxSpores)
            {
                var extra = emptyTiles
                    .Except(targetTiles)
                    .OrderBy(_ => rng.Next())
                    .Take(maxSpores - targetTiles.Count)
                    .Select(tile => tile.TileId);
                return targetTiles.Select(tile => tile.TileId).Concat(extra).ToList();
            }

            return targetTiles
                .OrderBy(_ => rng.Next())
                .Take(maxSpores)
                .Select(tile => tile.TileId)
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
            // Use custom duration for Ballistospore Discharge, with all bonuses
            int toxinLifespan = ToxinHelper.GetToxinExpirationAge(player, MycovariantGameBalance.BallistosporeDischargeToxinDuration);
            ToxinHelper.ConvertToToxin(board, tileId, toxinLifespan, GrowthSource.Ballistospore, player);
            playerMyco.IncrementEffectCount(MycovariantEffectType.Drops, 1);
            observer.RecordBallistosporeDischarge(player.PlayerId, 1);
        }
    }
}
