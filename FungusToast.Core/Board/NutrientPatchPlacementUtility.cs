using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    public static class NutrientPatchPlacementUtility
    {
        public static int PlaceStartingNutrientPatches(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            if (board == null || players == null || rng == null)
            {
                return 0;
            }

            int targetCount = Math.Max(
                GameBalance.NutrientPatchMinimumCount,
                (int)Math.Round(board.TotalTiles * GameBalance.NutrientPatchDensity, MidpointRounding.AwayFromZero));

            int minimumDistanceFromStartingSpores = Math.Max(
                1,
                (int)Math.Ceiling(board.Width * GameBalance.NutrientPatchMinimumDistanceFromStartingSporeWidthFactor));

            var startingTileIds = players
                .Where(player => player.StartingTileId.HasValue)
                .Select(player => player.StartingTileId!.Value)
                .ToList();

            var candidateTileIds = board.AllTiles()
                .Where(tile => !tile.IsOccupied && !tile.HasNutrientPatch)
                .Select(tile => tile.TileId)
                .ToList();

            Shuffle(candidateTileIds, rng);

            int placedCount = 0;
            foreach (int tileId in candidateTileIds)
            {
                if (placedCount >= targetCount)
                {
                    break;
                }

                if (!IsFarEnoughFromStartingSpores(board, tileId, startingTileIds, minimumDistanceFromStartingSpores))
                {
                    continue;
                }

                if (board.PlaceNutrientPatch(tileId, NutrientPatch.CreateDefaultMutationPointPatch()))
                {
                    placedCount++;
                }
            }

            observer?.RecordNutrientPatchesPlaced(placedCount);
            return placedCount;
        }

        private static bool IsFarEnoughFromStartingSpores(
            GameBoard board,
            int candidateTileId,
            IReadOnlyList<int> startingTileIds,
            int minimumDistance)
        {
            foreach (int startingTileId in startingTileIds)
            {
                var candidateTile = board.GetTileById(candidateTileId);
                var startingTile = board.GetTileById(startingTileId);
                if (candidateTile == null || startingTile == null)
                {
                    continue;
                }

                if (candidateTile.DistanceTo(startingTile) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}