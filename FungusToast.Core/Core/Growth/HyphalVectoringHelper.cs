using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;

namespace FungusToast.Core.Growth
{
    public static class HyphalVectoringHelper
    {
        /// <summary>
        /// Attempts to select a valid Hyphal Vector origin cell and tile for the given player and board.
        /// The origin is valid if its projected vector toward the center is not blocked by friendly mold.
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
            var candidates = new List<(FungalCell cell, BoardTile tile, double dist)>();
            var debugLines = new List<string>();

            foreach (var cell in livingCells)
            {
                var tile = board.GetTileById(cell.TileId)!;
                double dist = GetDistance(tile.X, tile.Y, centerX, centerY);

                var path = GetLineToCenter(tile.X, tile.Y, centerX, centerY, totalTiles);

                if (PathBlockedByFriendly(path, board, player.PlayerId, out int unblockedTiles))
                {
                    debugLines.Add($"  ⤷ Rejected: blocked by friendly mold after {unblockedTiles} tiles (origin dist = {dist:F1})");
                    continue;
                }

                candidates.Add((cell, tile, dist));
            }

            if (candidates.Count == 0)
            {
                int maxLength = livingCells
                    .Select(c =>
                    {
                        var t = board.GetTileById(c.TileId)!;
                        var line = GetLineToCenter(t.X, t.Y, centerX, centerY, totalTiles);
                        int count = 0;
                        foreach (var (x, y) in line)
                        {
                            var pathTile = board.GetTile(x, y);
                            if (pathTile == null)
                                continue;

                            if (pathTile.IsOccupied &&
                                pathTile.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } &&
                                oid == player.PlayerId)
                                break;

                            count++;
                        }
                        return count;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                float occupancyPercent = board.GetOccupiedTileRatio() * 100f;

                Console.WriteLine(
                    $"[HyphalVectoring] Player {player.PlayerId} has {livingCells.Count} living cells: no valid origin found (Round {board.CurrentRound})\n" +
                    $"  ? Wanted to place {totalTiles} tiles\n" +
                    $"  ? Longest available straight-line path was {maxLength} tiles\n" +
                    $"  ? Board occupancy: {occupancyPercent:F1}%\n" +
                    $"  ? Debug detail:");

                if (debugLines.Count == 0)
                {
                    Console.WriteLine("  ⤷ No living mold cells found for player.");
                }
                else
                {
                    for (int i = 0; i < Math.Min(debugLines.Count, maxDebugLines); i++)
                        Console.WriteLine(debugLines[i]);
                    if (debugLines.Count > maxDebugLines)
                        Console.WriteLine($"  ⤷ ...and {debugLines.Count - maxDebugLines} more rejection reasons not shown.");
                }

                return null;
            }

            var chosen = candidates[rng.Next(candidates.Count)];
            return (chosen.cell, chosen.tile);
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
            int placed = 0;

            foreach (var (x, y) in path)
            {
                var tile = board.GetTile(x, y);
                if (tile == null)
                    continue;

                if (tile.IsOccupied && tile.FungalCell is { IsAlive: true, OwnerPlayerId: var oid } && oid == player.PlayerId)
                {
                    // Defensive: should never happen, but don't crash
                    // Console.WriteLine($"[HyphalVectoring] Path encountered friendly cell at {tile.TileId}");
                    break;
                }

                if (tile.IsOccupied && tile.FungalCell is { IsAlive: true } fc)
                {
                    fc.Kill(DeathReason.HyphalVectoring);
                    observer?.RecordCellDeath(player.PlayerId, DeathReason.HyphalVectoring, 1);
                }

                var newCell = new FungalCell(player.PlayerId, tile.TileId);
                tile.PlaceFungalCell(newCell);
                board.PlaceFungalCell(newCell);
                player.AddControlledTile(tile.TileId);
                placed++;
            }

            return placed;
        }
    }
}
