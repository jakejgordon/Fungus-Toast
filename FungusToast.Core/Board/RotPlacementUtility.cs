using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;

namespace FungusToast.Core.Board
{
    public static class RotPlacementUtility
    {
        /// <summary>A static, tapered tongue from the left edge toward the middle.
        /// Clips to the silhouette, protects starting spores, and consumes no RNG.</summary>
        public static int PlaceIntroductoryPatch(GameBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            board.ConfigureRot(RotBalance.AdjacentDeathChance);
            int reach = Math.Max(1, (int)Math.Round(board.Width * RotBalance.IntroductoryReachWidthFactor));
            int halfThickness = Math.Max(1, (int)Math.Round(board.Height * RotBalance.IntroductoryHalfThicknessHeightFactor));
            int clearance = Math.Max(RotBalance.MinimumStartingSporeClearance,
                (int)Math.Ceiling(Math.Min(board.Width, board.Height) * RotBalance.StartingSporeClearanceSizeFactor));
            var startingTiles = board.Players.Where(p => p.StartingTileId.HasValue)
                .Select(p => board.GetTileById(p.StartingTileId!.Value)).Where(t => t != null).ToList();
            var best = new List<int>();
            int offset = halfThickness + clearance + 1;
            // If a protected start bisects the middle tongue, shift it up/down rather
            // than amputating the obstacle. Prefer the central authored layout.
            foreach (int shift in Enumerable.Range(-offset, offset * 2 + 1)
                .OrderBy(Math.Abs).ThenByDescending(value => value))
            {
                int centerY = board.Height / 2 + shift;
                if (centerY - halfThickness < 1 || centerY + halfThickness >= board.Height - 1) continue;
                var candidates = new HashSet<int>();
                foreach (var tile in board.AllTiles())
                {
                    if (tile.X >= reach || tile.IsOccupiedForSporePlacement || board.IsTileBlockedForOccupation(tile.TileId)) continue;
                    int thickness = Math.Max(1, halfThickness * (reach - tile.X) / reach);
                    int bend = (tile.X / Math.Max(1, reach / 5)) % 3 - 1;
                    if (Math.Abs(tile.Y - centerY - bend) > thickness) continue;
                    if (startingTiles.Any(start => tile.DistanceTo(start!) <= clearance)) continue;
                    candidates.Add(tile.TileId);
                }
                // Keep a connected edge-attached patch, never detached mask scraps.
                while (candidates.Count > 0)
                {
                    int seed = candidates.Min();
                    candidates.Remove(seed);
                    var component = new List<int>();
                    var queue = new Queue<int>();
                    queue.Enqueue(seed);
                    bool touchesEdge = false;
                    while (queue.Count > 0)
                    {
                        int id = queue.Dequeue();
                        component.Add(id);
                        touchesEdge |= board.IsPlayableEdgeTile(id);
                        foreach (var neighbor in board.GetOrthogonalNeighbors(id))
                            if (candidates.Remove(neighbor.TileId)) queue.Enqueue(neighbor.TileId);
                    }
                    if (touchesEdge && component.Count > best.Count) best = component;
                    if (touchesEdge && component.Any(id => board.GetTileById(id)!.X >= board.Width / 2))
                        return Place(board, component);
                }
            }
            return Place(board, best);
        }

        private static int Place(GameBoard board, IEnumerable<int> tileIds)
        {
            int placed = 0;
            foreach (int id in tileIds.OrderBy(id => id)) if (board.TryPlaceRot(id)) placed++;
            return placed;
        }
    }
}
