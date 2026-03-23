using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Growth
{
    public static class ChemotacticBeaconHelper
    {
        public static bool TryGetActiveMarker(GameBoard board, Player player, out GameBoard.ChemobeaconMarker? marker)
        {
            marker = null;
            if (board == null || player == null || !player.IsSurgeActive(MutationIds.ChemotacticBeacon))
            {
                return false;
            }

            marker = board.GetChemobeacon(player.PlayerId);
            return marker != null;
        }

        public static int? TrySelectAITargetTile(Player player, GameBoard board)
        {
            if (player == null || board == null)
            {
                return null;
            }

            var validTiles = board.AllTiles()
                .Where(tile => board.IsTileOpenForChemobeacon(tile.TileId))
                .ToList();
            if (validTiles.Count == 0)
            {
                return null;
            }

            var anchor = GetAnchorTile(player, board);
            if (anchor == null)
            {
                return validTiles
                    .OrderBy(tile => tile.TileId)
                    .Select(tile => (int?)tile.TileId)
                    .FirstOrDefault();
            }

            var nutrientTiles = board.AllNutrientPatchTiles().ToList();
            if (nutrientTiles.Count == 0)
            {
                return validTiles
                    .OrderBy(tile => tile.DistanceTo(anchor))
                    .ThenBy(tile => tile.TileId)
                    .Select(tile => (int?)tile.TileId)
                    .FirstOrDefault();
            }

            var nearestNutrient = nutrientTiles
                .OrderBy(tile => tile.DistanceTo(anchor))
                .ThenBy(tile => tile.TileId)
                .First();

            int reflectedX = Math.Clamp(nearestNutrient.X + (nearestNutrient.X - anchor.X), 0, board.Width - 1);
            int reflectedY = Math.Clamp(nearestNutrient.Y + (nearestNutrient.Y - anchor.Y), 0, board.Height - 1);

            int dxAway = nearestNutrient.X - anchor.X;
            int dyAway = nearestNutrient.Y - anchor.Y;

            var oppositeSideCandidates = validTiles
                .Where(tile => IsOnOppositeSideOfNutrient(tile, nearestNutrient, dxAway, dyAway))
                .ToList();

            var scoredCandidates = (oppositeSideCandidates.Count > 0 ? oppositeSideCandidates : validTiles)
                .OrderBy(tile => Math.Abs(tile.X - reflectedX) + Math.Abs(tile.Y - reflectedY))
                .ThenByDescending(tile => Math.Abs(tile.X - anchor.X) + Math.Abs(tile.Y - anchor.Y))
                .ThenBy(tile => tile.TileId)
                .ToList();

            return scoredCandidates.Count > 0 ? scoredCandidates[0].TileId : null;
        }

        private static BoardTile? GetAnchorTile(Player player, GameBoard board)
        {
            if (player.StartingTileId.HasValue)
            {
                var startingTile = board.GetTileById(player.StartingTileId.Value);
                if (startingTile != null)
                {
                    return startingTile;
                }
            }

            var livingTiles = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(cell => cell.IsAlive)
                .Select(cell => board.GetTileById(cell.TileId))
                .OfType<BoardTile>()
                .ToList();
            if (livingTiles.Count == 0)
            {
                return null;
            }

            int avgX = (int)Math.Round(livingTiles.Average(tile => tile.X), MidpointRounding.AwayFromZero);
            int avgY = (int)Math.Round(livingTiles.Average(tile => tile.Y), MidpointRounding.AwayFromZero);
            return board.GetTile(avgX, avgY) ?? livingTiles.OrderBy(tile => tile.TileId).First();
        }

        private static bool IsOnOppositeSideOfNutrient(BoardTile candidate, BoardTile nutrientTile, int dxAway, int dyAway)
        {
            int candidateDx = candidate.X - nutrientTile.X;
            int candidateDy = candidate.Y - nutrientTile.Y;
            int dot = candidateDx * dxAway + candidateDy * dyAway;
            return dot >= 0;
        }
    }
}