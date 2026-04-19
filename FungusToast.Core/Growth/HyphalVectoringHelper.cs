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
    public static class DirectedVectorHelper
    {
        public sealed class VectorLineOutcome
        {
            public int Infested { get; set; }
            public int Reclaimed { get; set; }
            public int CatabolicGrowth { get; set; }
            public int AlreadyOwned { get; set; }
            public int Colonized { get; set; }
            public int Invalid { get; set; }
            public List<int> AffectedTileIds { get; } = new();
            public int PlacedCount => Infested + Reclaimed + CatabolicGrowth + Colonized;
        }

        //TODO make it select the tile closest to the requested target that is not blocked by friendly mold AND has the longest available straight path toward (and possibly through) that target.
        /// <summary>
        /// Attempts to select a valid directed-vector origin cell and tile for the given player and board.
        /// Prioritizes selection based on: 1) Fewest friendly living cells in path, 2) Most enemy cells to infest, 3) Closest to target.
        /// Outputs debug info as appropriate.
        /// </summary>
        public static (FungalCell cell, BoardTile tile)? TrySelectDirectedVectorOrigin(
            Player player,
            GameBoard board,
            Random rng,
            int centerX,
            int centerY,
            int totalTiles,
            int maxDebugLines = 5)
            => TrySelectVectorOrigin(player, board, rng, centerX, centerY, totalTiles, maxDebugLines);

        public static (FungalCell cell, BoardTile tile)? TrySelectVectorOrigin(
            Player player,
            GameBoard board,
            Random rng,
            int targetX,
            int targetY,
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
                var evaluation = EvaluateCellForVectoring(tile, board, player.PlayerId, targetX, targetY, totalTiles);
                evaluatedCells.Add((cell, tile, evaluation));
            }

            // Sort by our prioritization criteria:
            // 1. Fewest friendly living cells in path (ascending)
            // 2. Most enemy cells to infest (descending) 
            // 3. Closest to target (ascending distance)
            var sortedCells = evaluatedCells
                .OrderBy(x => x.eval.FriendlyLivingInPath)     // Priority 1: Fewest friendly
                .ThenByDescending(x => x.eval.EnemyLivingInPath) // Priority 2: Most enemies
                .ThenBy(x => x.eval.DistanceToTarget)          // Priority 3: Closest to target
                .ToList();

            // First, try to find cells with completely unblocked paths (original behavior for optimal cases)
            var unblockedCells = sortedCells.Where(x => x.eval.FriendlyLivingInPath == 0).ToList();
            
            if (unblockedCells.Count > 0)
            {
                var chosen = unblockedCells[0]; // Take the best one based on our sorting
                debugLines.Add($"  ✓ Selected unblocked origin at distance {chosen.eval.DistanceToTarget:F1} with {chosen.eval.EnemyLivingInPath} enemy targets");
                return (chosen.cell, chosen.tile);
            }

            // Fallback: Allow paths with friendly cells, but prefer the best options
            int maxCandidateCells = Math.Min(GameBalance.DirectedVectorCandidateCellsToCheck, sortedCells.Count);
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
                debugLines.Add($"  ✓ Selected fallback origin at distance {chosen.eval.DistanceToTarget:F1} with {chosen.eval.FriendlyLivingInPath} friendly and {chosen.eval.EnemyLivingInPath} enemy in path");
                return (chosen.cell, chosen.tile);
            }

            return null;
        }

        /// <summary>
        /// Evaluates a cell's suitability for directed-vector origin selection.
        /// </summary>
        private static CellEvaluation EvaluateCellForVectoring(
            BoardTile tile, 
            GameBoard board, 
            int playerId, 
            int targetX, 
            int targetY, 
            int totalTiles)
        {
            double distance = GetDistance(tile.X, tile.Y, targetX, targetY);
            var path = GetLineToTarget(tile.X, tile.Y, targetX, targetY, totalTiles);
            
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
                DistanceToTarget = distance,
                FriendlyLivingInPath = friendlyLiving,
                EnemyLivingInPath = enemyLiving
            };
        }

        /// <summary>
        /// Represents the evaluation metrics for a cell being considered as a directed-vector origin.
        /// </summary>
        private struct CellEvaluation
        {
            public double DistanceToTarget;
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
        /// Returns a straight line of (x, y) points from (fromX, fromY) to (toX, toY), length up to maxLength.
        /// </summary>
        public static List<(int x, int y)> GetLineToCenter(int fromX, int fromY, int toX, int toY, int maxLength)
            => GetLineToTarget(fromX, fromY, toX, toY, maxLength);

        public static List<(int x, int y)> GetLineToTarget(int fromX, int fromY, int toX, int toY, int maxLength)
        {
            var line = new List<(int x, int y)>();
            int dx = toX - fromX;
            int dy = toY - fromY;

            if (dx == 0 && dy == 0)
            {
                return line;
            }

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
        /// Projects a line of growth from the specified start coordinates toward the requested target.
        /// Friendly living cells remain part of the projected line and count toward its visible reach.
        /// Friendly dead cells are skipped entirely so they do not spend the vector's quota.
        /// Empty tiles, nutrient patches, enemy living cells, enemy dead cells, and toxin tiles remain valid targets.
        /// </summary>
        public static VectorLineOutcome ApplyDirectedVectorLine(
            Player player,
            GameBoard board,
            Random rng,
            int startX,
            int startY,
            int targetX,
            int targetY,
            int totalTiles,
            ISimulationObserver observer,
            GrowthSource source,
            DeathReason deathReason,
            bool stopAtTargetTile)
        {
            var outcome = new VectorLineOutcome();
            if (totalTiles <= 0)
            {
                return outcome;
            }

            int completedLineTiles = 0;
            int maxPathLength = board.Width * board.Height;
            var path = GetLineToTarget(startX, startY, targetX, targetY, maxPathLength);

            foreach (var (x, y) in path)
            {
                if (completedLineTiles >= totalTiles)
                {
                    break;
                }

                var tile = board.GetTile(x, y);
                if (tile == null)
                    break;

                if (stopAtTargetTile && tile.TileId == (targetY * board.Width + targetX))
                {
                    break;
                }

                var cell = tile.FungalCell;

                // If the tile has a friendly living cell, skip overwriting, but still count it
                if (cell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                {
                    outcome.AlreadyOwned++;
                    completedLineTiles++;
                    continue;
                }

                // Friendly dead cells should not consume vector quota.
                if (cell is { IsDead: true, OwnerPlayerId: var deadOwnerId } && deadOwnerId == player.PlayerId)
                {
                    outcome.Invalid++;
                    continue;
                }

                FungalCellTakeoverResult takeoverResult;
                if (cell != null)
                {
                    takeoverResult = board.TakeoverCell(tile.TileId, player.PlayerId, allowToxin: true, source, players: board.Players, rng: rng, observer: observer);
                    switch (takeoverResult)
                    {
                        case FungalCellTakeoverResult.Infested:
                            outcome.Infested++;
                            outcome.AffectedTileIds.Add(tile.TileId);
                            completedLineTiles++;
                            observer.RecordAttributedKill(player.PlayerId, deathReason, 1);
                            break;
                        case FungalCellTakeoverResult.Reclaimed:
                            outcome.Reclaimed++;
                            outcome.AffectedTileIds.Add(tile.TileId);
                            completedLineTiles++;
                            break;
                        case FungalCellTakeoverResult.Overgrown:
                            outcome.CatabolicGrowth++;
                            outcome.AffectedTileIds.Add(tile.TileId);
                            completedLineTiles++;
                            break;
                        case FungalCellTakeoverResult.AlreadyOwned:
                            outcome.AlreadyOwned++;
                            completedLineTiles++;
                            break;
                        case FungalCellTakeoverResult.Invalid:
                            outcome.Invalid++;
                            break;
                    }
                }
                else if (!board.IsTileBlockedForOccupation(tile.TileId))
                {
                    var newCell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tile.TileId, source: source, lastOwnerPlayerId: null);
                    board.PlaceFungalCell(newCell);
                    outcome.Colonized++;
                    outcome.AffectedTileIds.Add(tile.TileId);
                    completedLineTiles++;
                }
                else
                {
                    outcome.Invalid++;
                }

                if (stopAtTargetTile && (x == targetX && y == targetY))
                {
                    break;
                }
            }

            return outcome;
        }

        /// <summary>
        /// Projects Chemotactic Beacon growth from the player's starting spore toward the beacon marker.
        /// Only valid growth targets consume quota, and those targets are assigned in earliest-to-latest path order.
        /// </summary>
        public static VectorLineOutcome ApplyChemotacticBeaconPathGrowth(
            Player player,
            GameBoard board,
            Random rng,
            int startTileId,
            int targetTileId,
            int totalTiles,
            ISimulationObserver observer,
            GrowthSource source,
            DeathReason deathReason)
        {
            var outcome = new VectorLineOutcome();
            if (totalTiles <= 0 || startTileId < 0 || targetTileId < 0)
            {
                return outcome;
            }

            var startTile = board.GetTileById(startTileId);
            var targetTile = board.GetTileById(targetTileId);
            if (startTile == null || targetTile == null)
            {
                return outcome;
            }

            int selectedGrowthTargets = 0;
            int maxPathLength = board.Width * board.Height;
            var path = GetLineToTarget(startTile.X, startTile.Y, targetTile.X, targetTile.Y, maxPathLength);

            foreach (var (x, y) in path)
            {
                if (selectedGrowthTargets >= totalTiles)
                {
                    break;
                }

                var tile = board.GetTile(x, y);
                if (tile == null)
                {
                    break;
                }

                if (tile.TileId == targetTileId)
                {
                    break;
                }

                if (!IsChemotacticBeaconGrowthTarget(tile, board, player.PlayerId, out bool alreadyOwned))
                {
                    if (alreadyOwned)
                    {
                        outcome.AlreadyOwned++;
                    }
                    else
                    {
                        outcome.Invalid++;
                    }

                    continue;
                }

                selectedGrowthTargets++;
                ApplyDirectedVectorGrowthToTile(player, board, rng, tile, observer, source, deathReason, outcome);
            }

            return outcome;
        }

        public static IReadOnlyList<int> GetChemotacticBeaconPathTargetTileIds(
            Player player,
            GameBoard board,
            int startTileId,
            int targetTileId,
            int totalTiles)
        {
            if (player == null || board == null || totalTiles <= 0 || startTileId < 0 || targetTileId < 0)
            {
                return Array.Empty<int>();
            }

            var startTile = board.GetTileById(startTileId);
            var targetTile = board.GetTileById(targetTileId);
            if (startTile == null || targetTile == null)
            {
                return Array.Empty<int>();
            }

            int selectedGrowthTargets = 0;
            int maxPathLength = board.Width * board.Height;
            var path = GetLineToTarget(startTile.X, startTile.Y, targetTile.X, targetTile.Y, maxPathLength);
            var affectedTileIds = new List<int>();

            foreach (var (x, y) in path)
            {
                if (selectedGrowthTargets >= totalTiles)
                {
                    break;
                }

                var tile = board.GetTile(x, y);
                if (tile == null)
                {
                    break;
                }

                if (tile.TileId == targetTileId)
                {
                    break;
                }

                if (!IsChemotacticBeaconGrowthTarget(tile, board, player.PlayerId, out _))
                {
                    continue;
                }

                selectedGrowthTargets++;
                affectedTileIds.Add(tile.TileId);
            }

            return affectedTileIds;
        }

        private static bool IsChemotacticBeaconGrowthTarget(BoardTile tile, GameBoard board, int playerId, out bool alreadyOwned)
        {
            alreadyOwned = false;
            if (tile == null)
            {
                return false;
            }

            var cell = tile.FungalCell;
            if (cell == null)
            {
                return !board.IsTileBlockedForOccupation(tile.TileId);
            }

            if (cell.OwnerPlayerId == playerId)
            {
                alreadyOwned = cell.IsAlive;
                return false;
            }

            if (cell.IsResistant)
            {
                return false;
            }

            return cell.IsAlive || cell.IsDead || cell.IsToxin;
        }

        private static void ApplyDirectedVectorGrowthToTile(
            Player player,
            GameBoard board,
            Random rng,
            BoardTile tile,
            ISimulationObserver observer,
            GrowthSource source,
            DeathReason deathReason,
            VectorLineOutcome outcome)
        {
            var cell = tile.FungalCell;
            if (cell != null)
            {
                FungalCellTakeoverResult takeoverResult = board.TakeoverCell(tile.TileId, player.PlayerId, allowToxin: true, source, players: board.Players, rng: rng, observer: observer);
                switch (takeoverResult)
                {
                    case FungalCellTakeoverResult.Infested:
                        outcome.Infested++;
                        outcome.AffectedTileIds.Add(tile.TileId);
                        observer.RecordAttributedKill(player.PlayerId, deathReason, 1);
                        break;
                    case FungalCellTakeoverResult.Reclaimed:
                        outcome.Reclaimed++;
                        outcome.AffectedTileIds.Add(tile.TileId);
                        break;
                    case FungalCellTakeoverResult.Overgrown:
                        outcome.CatabolicGrowth++;
                        outcome.AffectedTileIds.Add(tile.TileId);
                        break;
                    case FungalCellTakeoverResult.AlreadyOwned:
                        outcome.AlreadyOwned++;
                        break;
                    case FungalCellTakeoverResult.Invalid:
                        outcome.Invalid++;
                        break;
                }

                return;
            }

            if (board.IsTileBlockedForOccupation(tile.TileId))
            {
                outcome.Invalid++;
                return;
            }

            var newCell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tile.TileId, source: source, lastOwnerPlayerId: null);
            board.PlaceFungalCell(newCell);
            outcome.Colonized++;
            outcome.AffectedTileIds.Add(tile.TileId);
        }

    }
}
