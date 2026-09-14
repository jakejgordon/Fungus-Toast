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
        public sealed class BeaconPlacementCandidate
        {
            public BeaconPlacementCandidate(
                BoardTile tile,
                int pathLength,
                int expectedPlacements,
                int nutrientValue,
                int enemyLivingTilesCrossed,
                int enemyToxinsCrossed,
                int distancePenalty)
            {
                Tile = tile;
                PathLength = pathLength;
                ExpectedPlacements = expectedPlacements;
                NutrientValue = nutrientValue;
                EnemyLivingTilesCrossed = enemyLivingTilesCrossed;
                EnemyToxinsCrossed = enemyToxinsCrossed;
                DistancePenalty = distancePenalty;
            }

            public BoardTile Tile { get; }
            public int PathLength { get; }
            public int ExpectedPlacements { get; }
            public int NutrientValue { get; }
            public int EnemyLivingTilesCrossed { get; }
            public int EnemyToxinsCrossed { get; }
            public int DistancePenalty { get; }
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

        public static IReadOnlyList<int> GetProjectedGrowthTileIds(Player player, GameBoard board, int targetTileId, int projectedLevel, int surgeDuration = GameBalance.ChemotacticBeaconSurgeDuration)
            => GetProjectedGrowthPath(player, board, targetTileId, projectedLevel, surgeDuration).GrowthTileIds;

        /// <summary>
        /// Full preview of the beacon line for a candidate marker tile: the tiles the line crosses before growth
        /// begins, the tile growth begins from, and every tile growth would eventually be assigned to. The line is
        /// shown in full; the spiral past the marker is shown only as far as the whole surge could grow.
        /// </summary>
        public static DirectedVectorHelper.ChemotacticBeaconPathProjection GetProjectedGrowthPath(Player player, GameBoard board, int targetTileId, int projectedLevel, int surgeDuration = GameBalance.ChemotacticBeaconSurgeDuration)
        {
            if (player == null || board == null || !player.StartingTileId.HasValue)
            {
                return DirectedVectorHelper.ChemotacticBeaconPathProjection.Empty;
            }

            int wholeSurgeBudget = GetTilesPerRound(projectedLevel) * Math.Max(0, surgeDuration);
            return DirectedVectorHelper.GetChemotacticBeaconPathProjection(
                player,
                board,
                player.StartingTileId.Value,
                targetTileId,
                lineTileLimit: int.MaxValue,
                spiralBudget: wholeSurgeBudget);
        }

        public static int GetTilesPerRound(int level)
            => GameBalance.ChemotacticBeaconBaseTiles + Math.Max(0, level) * GameBalance.ChemotacticBeaconTilesPerLevel;

        public static int? TrySelectAITargetTile(Player player, GameBoard board, int projectedLevel, int surgeDuration)
            => TrySelectAITargetCandidate(player, board, projectedLevel, surgeDuration)?.Tile.TileId;

        /// <summary>
        /// The marker the AI would place right now, with the path statistics that ranked it, so callers
        /// can judge whether the activation is worth its cost before committing.
        /// </summary>
        public static BeaconPlacementCandidate? TrySelectAITargetCandidate(Player player, GameBoard board, int projectedLevel, int surgeDuration)
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
                // No colony to project from: any legal marker is as good as another and grows nothing.
                var fallbackTile = validTiles.OrderBy(tile => tile.TileId).First();
                return new BeaconPlacementCandidate(fallbackTile, 0, 0, 0, 0, 0, 0);
            }

            int idealDistance = CalculateIdealDistance(projectedLevel, surgeDuration);
            int maxPlacements = Math.Max(0, idealDistance);
            int colonyReach = GetColonyReach(player, board, anchor);
            var pathBuffer = new (int x, int y)[Math.Max(board.Width, board.Height)];
            var candidates = validTiles
                .Select(tile => EvaluateCandidateTile(tile, anchor, player.PlayerId, board, idealDistance, maxPlacements, colonyReach, pathBuffer))
                .Where(candidate => candidate != null)
                .ToList();

            var bestCandidate = candidates
                .OrderByDescending(candidate => candidate!.EnemyLivingTilesCrossed)
                .ThenByDescending(candidate => candidate!.EnemyToxinsCrossed)
                .ThenByDescending(candidate => candidate!.NutrientValue)
                .ThenByDescending(candidate => candidate!.ExpectedPlacements)
                .ThenByDescending(candidate => candidate!.PathLength)
                .ThenBy(candidate => candidate!.DistancePenalty)
                .ThenBy(candidate => candidate!.Tile.TileId)
                .FirstOrDefault();

            return bestCandidate;
        }

        private static int CalculateIdealDistance(int projectedLevel, int surgeDuration)
        {
            int clampedLevel = Math.Max(0, projectedLevel);
            int clampedDuration = Math.Max(0, surgeDuration);
            int cellsPerRound = GetTilesPerRound(clampedLevel);
            return (clampedDuration * cellsPerRound) + GameBalance.ChemotacticBeaconAiBridgeBufferTiles;
        }

        private static BeaconPlacementCandidate? EvaluateCandidateTile(
            BoardTile candidateTile,
            BoardTile anchor,
            int playerId,
            GameBoard board,
            int idealDistance,
            int maxPlacements,
            int colonyReach,
            (int x, int y)[] pathBuffer)
        {
            int dx = candidateTile.X - anchor.X;
            int dy = candidateTile.Y - anchor.Y;
            int pathLength = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (pathLength <= 0)
            {
                return null;
            }

            // Same stepping as DirectedVectorHelper.GetLineToTarget, generated lazily: step i sits at
            // Chebyshev distance i + 1 from the anchor, so the growth origin (the furthest friendly
            // living cell on the line, never the marker itself) cannot lie past the colony's reach and
            // the scored window never needs the rest of the line. Scanning every board tile this way
            // is what keeps AI marker selection affordable on a 160x160 board.
            float stepX = dx / (float)pathLength;
            float stepY = dy / (float)pathLength;
            float cx = anchor.X + 0.5f;
            float cy = anchor.Y + 0.5f;
            int generated = 0;

            int originScanEnd = Math.Min(pathLength - 1, colonyReach);
            int originIndex = -1;
            for (int index = 0; index < originScanEnd; index++)
            {
                var (x, y) = NextPathStep(pathBuffer, ref generated, ref cx, ref cy, stepX, stepY);
                var tile = board.GetTile(x, y);
                if (tile == null)
                {
                    break;
                }

                if (tile.FungalCell is { IsAlive: true } cell && cell.OwnerPlayerId == playerId)
                {
                    originIndex = index;
                }
            }

            // Growth begins just past the furthest friendly living cell on the line, so only the remainder counts.
            // The path's final step is the marker tile itself, which never receives growth.
            int firstGrowthIndex = originIndex + 1;
            int remainingPathLength = pathLength - firstGrowthIndex;
            if (remainingPathLength <= 1)
            {
                return null;
            }

            int expectedPlacements = Math.Min(remainingPathLength, maxPlacements);
            int nutrientValue = 0;
            int enemyLivingTilesCrossed = 0;
            int enemyToxinsCrossed = 0;
            int lastIndexToEvaluate = Math.Min(firstGrowthIndex + expectedPlacements, Math.Max(0, pathLength - 1));
            for (int index = firstGrowthIndex; index < lastIndexToEvaluate; index++)
            {
                var (x, y) = index < generated
                    ? pathBuffer[index]
                    : NextPathStep(pathBuffer, ref generated, ref cx, ref cy, stepX, stepY);
                var pathTile = board.GetTile(x, y);
                if (pathTile == null)
                {
                    break;
                }

                if (pathTile.HasNutrientPatch)
                {
                    nutrientValue += pathTile.NutrientPatch?.ClusterTileCount ?? 1;
                }

                if (pathTile.FungalCell is { OwnerPlayerId: var ownerId } cell && ownerId != playerId)
                {
                    if (cell.IsAlive)
                    {
                        enemyLivingTilesCrossed++;
                    }
                    else if (cell.IsToxin)
                    {
                        enemyToxinsCrossed++;
                    }
                }
            }

            return new BeaconPlacementCandidate(
                candidateTile,
                remainingPathLength,
                expectedPlacements,
                nutrientValue,
                enemyLivingTilesCrossed,
                enemyToxinsCrossed,
                Math.Abs(remainingPathLength - idealDistance));
        }

        private static (int x, int y) NextPathStep(
            (int x, int y)[] pathBuffer,
            ref int generated,
            ref float cx,
            ref float cy,
            float stepX,
            float stepY)
        {
            cx += stepX;
            cy += stepY;
            var step = ((int)Math.Floor(cx), (int)Math.Floor(cy));
            pathBuffer[generated++] = step;
            return step;
        }

        /// <summary>Chebyshev distance from the anchor to the player's furthest living cell.</summary>
        private static int GetColonyReach(Player player, GameBoard board, BoardTile anchor)
        {
            int reach = 0;
            foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
            {
                if (!cell.IsAlive)
                {
                    continue;
                }

                var tile = board.GetTileById(cell.TileId);
                if (tile == null)
                {
                    continue;
                }

                reach = Math.Max(reach, Math.Max(Math.Abs(tile.X - anchor.X), Math.Abs(tile.Y - anchor.Y)));
            }

            return reach;
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