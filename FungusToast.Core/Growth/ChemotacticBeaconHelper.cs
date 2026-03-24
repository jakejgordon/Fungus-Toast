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
        private sealed class BeaconPlacementCandidate
        {
            public BeaconPlacementCandidate(
                BoardTile tile,
                int pathLength,
                int nutrientTilesCrossed,
                int enemyLivingTilesCrossed,
                int distancePenalty)
            {
                Tile = tile;
                PathLength = pathLength;
                NutrientTilesCrossed = nutrientTilesCrossed;
                EnemyLivingTilesCrossed = enemyLivingTilesCrossed;
                DistancePenalty = distancePenalty;
            }

            public BoardTile Tile { get; }
            public int PathLength { get; }
            public int NutrientTilesCrossed { get; }
            public int EnemyLivingTilesCrossed { get; }
            public int DistancePenalty { get; }
            public int Score => (NutrientTilesCrossed * GameBalance.ChemotacticBeaconAiNutrientPathScore)
                + (EnemyLivingTilesCrossed * GameBalance.ChemotacticBeaconAiEnemyLivingPathScore)
                - (DistancePenalty * GameBalance.ChemotacticBeaconAiDistancePenaltyPerTile);
        }

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
            => TrySelectAITargetTile(player, board, player?.GetMutationLevel(MutationIds.ChemotacticBeacon) ?? 0, GameBalance.ChemotacticBeaconSurgeDuration);

        public static int? TrySelectAITargetTile(Player player, GameBoard board, int projectedLevel, int surgeDuration)
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
                    .ThenBy(tile => tile.TileId)
                    .Select(tile => (int?)tile.TileId)
                    .FirstOrDefault();
            }

            int idealDistance = CalculateIdealDistance(projectedLevel, surgeDuration);
            var bestCandidate = validTiles
                .Select(tile => EvaluateCandidateTile(tile, anchor, player.PlayerId, board, idealDistance))
                .Where(candidate => candidate != null)
                .OrderByDescending(candidate => candidate!.Score)
                .ThenBy(candidate => candidate!.DistancePenalty)
                .ThenByDescending(candidate => candidate!.PathLength)
                .ThenByDescending(candidate => candidate!.NutrientTilesCrossed)
                .ThenByDescending(candidate => candidate!.EnemyLivingTilesCrossed)
                .ThenBy(candidate => candidate!.Tile.TileId)
                .FirstOrDefault();

            return bestCandidate?.Tile.TileId;
        }

        private static int CalculateIdealDistance(int projectedLevel, int surgeDuration)
        {
            int clampedLevel = Math.Max(0, projectedLevel);
            int clampedDuration = Math.Max(0, surgeDuration);
            int cellsPerRound = GameBalance.ChemotacticBeaconBaseTiles + (clampedLevel * GameBalance.ChemotacticBeaconTilesPerLevel);
            return (clampedDuration * cellsPerRound) + GameBalance.ChemotacticBeaconAiBridgeBufferTiles;
        }

        private static BeaconPlacementCandidate? EvaluateCandidateTile(
            BoardTile candidateTile,
            BoardTile anchor,
            int playerId,
            GameBoard board,
            int idealDistance)
        {
            int dx = candidateTile.X - anchor.X;
            int dy = candidateTile.Y - anchor.Y;
            int pathLength = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (pathLength <= 0)
            {
                return null;
            }

            var path = DirectedVectorHelper.GetLineToTarget(anchor.X, anchor.Y, candidateTile.X, candidateTile.Y, pathLength);
            if (path.Count == 0)
            {
                return null;
            }

            int nutrientTilesCrossed = 0;
            int enemyLivingTilesCrossed = 0;
            for (int index = 0; index < path.Count - 1; index++)
            {
                var (x, y) = path[index];
                var pathTile = board.GetTile(x, y);
                if (pathTile == null)
                {
                    break;
                }

                if (pathTile.HasNutrientPatch)
                {
                    nutrientTilesCrossed++;
                }

                if (pathTile.FungalCell is { IsAlive: true, OwnerPlayerId: var ownerId } && ownerId != playerId)
                {
                    enemyLivingTilesCrossed++;
                }
            }

            return new BeaconPlacementCandidate(
                candidateTile,
                pathLength,
                nutrientTilesCrossed,
                enemyLivingTilesCrossed,
                Math.Abs(pathLength - idealDistance));
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
    }
}