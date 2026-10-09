using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;

namespace FungusToast.Core.Board
{
    /// <summary>RNG-free central organic island for the level-8 Kaiser bun alternate.</summary>
    public static class CentralRotLayout
    {
        /// <summary>Read-only plan, sized against playable tiles rather than the rectangle.</summary>
        public static IReadOnlyList<int> Build(GameBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.Width != RotBalance.CentralIslandBoardSize || board.Height != RotBalance.CentralIslandBoardSize)
                throw new ArgumentException("Central rot requires the 90x90 Kaiser bun.", nameof(board));
            var playable = board.AllTiles().Where(t => !t.IsBlocked).ToArray();
            int count = (int)Math.Round(playable.Length * RotBalance.CentralIslandPlayableFraction);
            var rot = playable.OrderBy(t => ContourDistance(t.X, t.Y, board.Width, board.Height))
                .ThenBy(t => t.TileId).Take(count).Select(t => t.TileId).ToHashSet();
            var open = playable.Select(t => t.TileId).Except(rot).ToHashSet();
            if (rot.Count == 0 || open.Count == 0 || rot.Any(board.IsPlayableEdgeTile)
                || !Connected(board, rot) || !Connected(board, open)
                || !rot.Contains((board.Height / 2) * board.Width + board.Width / 2))
                throw new InvalidOperationException("Central rot needs a connected interior island and an unbroken surrounding growth route.");
            return Array.AsReadOnly(rot.OrderBy(id => id).ToArray());
        }

        private static double ContourDistance(int x, int y, int width, int height)
        {
            double dx = x + 0.5 - width / 2d, dy = y + 0.5 - height / 2d;
            double angle = Math.Atan2(dy, dx);
            double contour = 1 + RotBalance.CentralIslandBroadLobeAmplitude * Math.Sin(5 * angle + 0.7)
                + RotBalance.CentralIslandFineLobeAmplitude * Math.Sin(11 * angle - 0.4);
            return Math.Sqrt(dx * dx + dy * dy) / contour;
        }

        private static bool Connected(GameBoard board, HashSet<int> tiles)
        {
            var reached = new HashSet<int>(); var queue = new Queue<int>();
            int seed = tiles.Min(); reached.Add(seed); queue.Enqueue(seed);
            while (queue.Count > 0)
                foreach (var neighbor in board.GetOrthogonalNeighbors(queue.Dequeue()))
                    if (tiles.Contains(neighbor.TileId) && reached.Add(neighbor.TileId)) queue.Enqueue(neighbor.TileId);
            return reached.Count == tiles.Count;
        }

        public static void Place(GameBoard board)
        {
            var plan = Build(board);
            if (plan.Any(id => board.GetTileById(id)!.IsOccupiedForSporePlacement))
                throw new InvalidOperationException("Central rot must be placed before starting spores/resources.");
            board.ConfigureRot(RotBalance.AdjacentDeathChance);
            foreach (int id in plan)
                if (!board.TryPlaceRot(id)) throw new InvalidOperationException($"Unable to place central rot at {id}.");
        }
    }
}
