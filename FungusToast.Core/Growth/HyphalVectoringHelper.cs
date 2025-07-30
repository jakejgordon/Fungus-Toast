using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FungusToast.Core.Growth
{
    public static class HyphalVectoringHelper
    {
        //TODO make it select the ile closest to the center that is not blocked by friendly mold AND has the longest available straight path toward (and possibly through) the center.
        /// <summary>
        /// Attempts to select a valid Hyphal Vector origin cell and tile for the given player and board.
        /// Prioritizes selection based on: 1) Fewest friendly living cells in path, 2) Most enemy cells to infest, 3) Closest to center.
        /// Outputs debug info as appropriate.
        /// </summary>
        public static (FungalCell cell, BoardTile tile)? TrySelectHyphalVectorOrigin(
            Player player,
            GameBoard board,
            Random rng,
            int centerX,
            int centerY,
            int totalTiles,
            int maxDebugLines = 5)
        {
            var livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Where(c => c.IsAlive).ToList();
            if (livingCells.Count == 0)
                return null;

            var evaluatedCells = new List<(FungalCell cell, BoardTile tile, CellEvaluation eval)>();
            var debugLines = new List<string>();

            // Evaluate all living cells
            foreach (var cell in livingCells)
            {
                var tile = board.GetTileById(cell.TileId)!;
                var evaluation = EvaluateCellForHyphalVectoring(tile, board, player.PlayerId, centerX, centerY, totalTiles);
                evaluatedCells.Add((cell, tile, evaluation));
            }

            // Sort by our prioritization criteria:
            // 1. Fewest friendly living cells in path (ascending)
            // 2. Most enemy cells to infest (descending) 
            // 3. Closest to center (ascending distance)
            var sortedCells = evaluatedCells
                .OrderBy(x => x.eval.FriendlyLivingInPath)     // Priority 1: Fewest friendly
                .ThenByDescending(x => x.eval.EnemyLivingInPath) // Priority 2: Most enemies
                .ThenBy(x => x.eval.DistanceToCenter)          // Priority 3: Closest to center
                .ToList();

            // First, try to find cells with completely unblocked paths (original behavior for optimal cases)
            var unblockedCells = sortedCells.Where(x => x.eval.FriendlyLivingInPath == 0).ToList();
            
            if (unblockedCells.Count > 0)
            {
                var chosen = unblockedCells[0]; // Take the best one based on our sorting
                debugLines.Add($"  ✓ Selected unblocked origin at distance {chosen.eval.DistanceToCenter:F1} with {chosen.eval.EnemyLivingInPath} enemy targets");
                return (chosen.cell, chosen.tile);
            }

            // Fallback: Allow paths with friendly cells, but prefer the best options
            int maxCandidateCells = Math.Min(GameBalance.HyphalVectoringCandidateCellsToCheck, sortedCells.Count);
            var bestCandidates = sortedCells.Take(maxCandidateCells).ToList();

            if (bestCandidates.Count > 0)
            {
                // Among the top candidates, select randomly from those tied for the best score
                var bestEval = bestCandidates[0].eval;
                var tiedForBest = bestCandidates
                    .Where(x => x.eval.FriendlyLivingInPath == bestEval.FriendlyLivingInPath &&
                               x.eval.EnemyLivingInPath == bestEval.EnemyLivingInPath)
                    .ToList();

                var chosen = tiedForBest[rng.Next(tiedForBest.Count)];
                debugLines.Add($"  ✓ Selected fallback origin at distance {chosen.eval.DistanceToCenter:F1} with {chosen.eval.FriendlyLivingInPath} friendly and {chosen.eval.EnemyLivingInPath} enemy in path");
                return (chosen.cell, chosen.tile);
            }

            return null;
        }

        /// <summary>
        /// Evaluates a cell's suitability for Hyphal Vectoring origin selection.
        /// </summary>
        private static CellEvaluation EvaluateCellForHyphalVectoring(
            BoardTile tile, 
            GameBoard board, 
            int playerId, 
            int centerX, 
            int centerY, 
            int totalTiles)
        {
            double distance = GetDistance(tile.X, tile.Y, centerX, centerY);
            var path = GetLineToCenter(tile.X, tile.Y, centerX, centerY, totalTiles);
            
            int friendlyLiving = 0;
            int enemyLiving = 0;
            
            foreach (var (x, y) in path)
            {
                var pathTile = board.GetTile(x, y);
                if (pathTile?.FungalCell is { IsAlive: true } cell)
                {
                    if (cell.OwnerPlayerId == playerId)
                        friendlyLiving++;
                    else
                        enemyLiving++;
                }
            }
            
            return new CellEvaluation
            {
                DistanceToCenter = distance,
                FriendlyLivingInPath = friendlyLiving,
                EnemyLivingInPath = enemyLiving
            };
        }

        /// <summary>
        /// Represents the evaluation metrics for a cell being considered as Hyphal Vectoring origin.
        /// </summary>
        private struct CellEvaluation
        {
            public double DistanceToCenter;
            public int FriendlyLivingInPath;
            public int EnemyLivingInPath;
        }

        // --- Helper Methods ---

        private static double GetDistance(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Returns true if the path is blocked by a friendly cell, otherwise false. Also returns how many unblocked tiles there were.
        /// </summary>
        private static bool PathBlockedByFriendly(List<(int x, int y)> path, GameBoard board, int playerId, out int unblockedTiles)
        {
            unblockedTiles = 0;
            foreach (var (x, y) in path)
            {
                var pathTile = board.GetTile(x, y);
                if (pathTile == null)
                    continue;

                if (pathTile.IsOccupied &&
                    pathTile.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } &&
                    oid == playerId)
                {
                    return true;
                }

                unblockedTiles++;
            }
            return false;
        }

        /// <summary>
        /// Returns a straight line of (x, y) points from (fromX, fromY) to (toX, toY), length up to maxLength.
        /// </summary>
        public static List<(int x, int y)> GetLineToCenter(int fromX, int fromY, int toX, int toY, int maxLength)
        {
            var line = new List<(int x, int y)>();
            int dx = toX - fromX;
            int dy = toY - fromY;

            float stepX = dx / (float)Math.Max(Math.Abs(dx), Math.Abs(dy));
            float stepY = dy / (float)Math.Max(Math.Abs(dx), Math.Abs(dy));

            float cx = fromX + 0.5f;
            float cy = fromY + 0.5f;

            for (int i = 0; i < maxLength; i++)
            {
                cx += stepX;
                cy += stepY;

                int ix = (int)Math.Floor(cx);
                int iy = (int)Math.Floor(cy);

                line.Add((ix, iy));
            }

            return line;
        }

        /// <summary>
        /// Projects a line of hyphal growth from the specified start coordinates toward the center,
        /// overwriting all cells (dead, toxin, enemy living, empty) in its path with new living cells belonging to the player.
        /// If a friendly living cell is encountered, it is left untouched and counted toward the total,
        /// and the projection continues to the next tile. Always creates the requested number of cells,
        /// skipping but not stopping for friendly living cells.
        /// Returns the number of tiles processed (i.e., the number of living cells created or skipped).
        /// </summary>
        public static int ApplyHyphalVectorLine(
            Player player,
            GameBoard board,
            Random rng,
            int startX,
            int startY,
            int centerX,
            int centerY,
            int totalTiles,
            ISimulationObserver? observer)
        {
            var path = GetLineToCenter(startX, startY, centerX, centerY, totalTiles);
            int processed = 0;

            foreach (var (x, y) in path)
            {
                var tile = board.GetTile(x, y);
                if (tile == null)
                    continue;

                var cell = tile.FungalCell;

                // If the tile has a friendly living cell, skip overwriting, but still count it
                if (cell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                {
                    processed++;
                    continue;
                }

                // If it's an enemy living cell, kill it (this will fire board events and remove control)
                if (cell is { IsAlive: true })
                {
                    board.KillFungalCell(cell, DeathReason.HyphalVectoring, player.PlayerId);
                    observer?.RecordCellDeath(player.PlayerId, DeathReason.HyphalVectoring, 1);
                }

                // Overwrite whatever was there with a new living cell (if it's not already your living cell)
                var newCell = new FungalCell(player.PlayerId, tile.TileId, GrowthSource.HyphalVectoring);
                board.PlaceFungalCell(newCell);

                processed++;
            }

            return processed;
        }

    }
}
