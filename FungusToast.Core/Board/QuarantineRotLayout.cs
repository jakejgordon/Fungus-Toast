using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;

namespace FungusToast.Core.Board
{
    /// <summary>Immutable placement instructions; building a plan never changes the board.</summary>
    public sealed class QuarantineRotPlan
    {
        internal QuarantineRotPlan(IEnumerable<int> rot, IEnumerable<int> pocket, IEnumerable<int> corridor,
            IEnumerable<(int x, int y)> starts, int entrance, int exit, int distance4, int distance8)
        {
            RotTileIds = Array.AsReadOnly(rot.OrderBy(id => id).ToArray());
            PocketTileIds = Array.AsReadOnly(pocket.OrderBy(id => id).ToArray());
            CorridorTileIds = Array.AsReadOnly(corridor.OrderBy(id => id).ToArray());
            StartingPositions = Array.AsReadOnly(starts.ToArray());
            EntranceTileId = entrance;
            ExitTileId = exit;
            CorridorOrthogonalDistance = distance4;
            CorridorDiagonalDistance = distance8;
        }

        public IReadOnlyList<int> RotTileIds { get; }
        public IReadOnlyList<int> PocketTileIds { get; }
        public IReadOnlyList<int> CorridorTileIds { get; }
        /// <summary>Stable player-slot order; slot zero is the human, not the closest mouth start.</summary>
        public IReadOnlyList<(int x, int y)> StartingPositions { get; }
        public int EntranceTileId { get; }
        public int ExitTileId { get; }
        /// <summary>Shortest mouth-to-exit distances in edges, restricted to the corridor.</summary>
        public int CorridorOrthogonalDistance { get; }
        public int CorridorDiagonalDistance { get; }
    }

    /// <summary>
    /// Fixed stage-12 alternate for the medium 120x120 hotdog. The right-hand cap is a
    /// roughly 15% pocket. A thin edge seal with a thick central bulge clips to playable terrain, not the
    /// empty board rectangle. Only the authored zigzag is carved out of that belt.
    /// Remote placement remains legal in the pocket; this is a growth barrier, not terrain.
    /// Unsupported masks fail explicitly; there is no generated fallback or RNG.
    /// </summary>
    public static class QuarantineRotLayout
    {
        // Inclusive rectangles are the authored corridor, NOT dilation of a polyline:
        // single-cell entrance/exit throats, two-cell lanes and 2x2 elbow overlaps.
        // The widely separated return legs prevent eight-neighbor cross-leg jumps.
        private static readonly (int left, int bottom, int right, int top)[] CorridorLanes =
        {
            (60, 60, 64, 60),
            (65, 60, 66, 70),
            (65, 69, 76, 70),
            (75, 48, 76, 70),
            (75, 48, 85, 49),
            (84, 48, 85, 60),
            (84, 59, 88, 60),
            (89, 60, 94, 60)
        };

        private static readonly (int x, int y)[] AuthoredStarts =
        {
            (47, 76), (49, 60), (28, 60), (47, 43), (27, 77), (27, 43), (10, 60)
        };

        public static QuarantineRotPlan Build(GameBoard board, int playerCount = 7)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.Width != RotBalance.QuarantineBoardSize || board.Height != RotBalance.QuarantineBoardSize)
                throw new ArgumentException("Quarantine requires the authored 120x120 medium hotdog board.", nameof(board));
            if (playerCount < 1 || playerCount > RotBalance.QuarantineMaximumPlayers)
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Quarantine supports one through seven player slots.");

            var playable = new HashSet<int>(board.AllTiles().Where(tile => !tile.IsBlocked).Select(tile => tile.TileId));
            var corridor = new HashSet<int>();
            foreach (var lane in CorridorLanes)
                for (int y = lane.bottom; y <= lane.top; y++)
                    for (int x = lane.left; x <= lane.right; x++)
                        corridor.Add(y * board.Width + x);
            Require(corridor.IsSubsetOf(playable), "The silhouette clips the authored corridor.");
            var pocket = new HashSet<int>(playable.Where(id => id % board.Width >= RotBalance.QuarantinePocketStartX));
            var belt = new HashSet<int>(playable.Where(id =>
            {
                int x = id % board.Width, y = id / board.Width;
                return x < RotBalance.QuarantinePocketStartX
                    && (x >= RotBalance.QuarantineOuterBeltStartX
                        || (x >= RotBalance.QuarantineBeltStartX
                            && y >= RotBalance.QuarantineBulgeBottomY && y <= RotBalance.QuarantineBulgeTopY));
            }));
            var main = new HashSet<int>(playable.Except(pocket).Except(belt));
            var rot = new HashSet<int>(belt.Except(corridor));
            double fraction = playable.Count == 0 ? 0 : pocket.Count / (double)playable.Count;
            Require(fraction >= RotBalance.QuarantineMinimumPocketFraction && fraction <= RotBalance.QuarantineMaximumPocketFraction,
                "The pocket must contain 14-16% of actual playable terrain; this is not the supported hotdog mask.");
            RequireConnected(playable, board.Width, false, "The playable silhouette is disconnected.");
            RequireConnected(main, board.Width, false, "The main starting region is disconnected.");
            RequireConnected(pocket, board.Width, false, "The pocket is disconnected.");
            RequireConnected(corridor, board.Width, false, "The corridor is disconnected.");

            int entrance = RotBalance.QuarantineEntranceY * board.Width + RotBalance.QuarantineBeltStartX;
            int exit = RotBalance.QuarantineEntranceY * board.Width + RotBalance.QuarantinePocketStartX - 1;
            var open = new HashSet<int>(playable.Except(rot));
            RequireConnected(open, board.Width, true, "The open regions do not connect through the corridor.");
            foreach (int throat in new[] { entrance, exit })
            {
                var cut = new HashSet<int>(open);
                cut.Remove(throat);
                var reached = Distances(cut, main.Min(), board.Width, true);
                Require(!pocket.Any(reached.ContainsKey), "A diagonal route bypasses a single-cell corridor throat.");
            }

            // Validate all seven authored slots even when a smaller prefix is requested.
            var startIds = AuthoredStarts.Select(p => p.y * board.Width + p.x).ToArray();
            Require(startIds.All(main.Contains), "An authored starting slot is outside the main region.");
            foreach (int id in startIds)
            {
                var tile = board.GetTileById(id)!;
                Require(board.GetPlayableEdgeDistance(id) >= RotBalance.QuarantineStartingEdgeClearance,
                    "An authored starting slot lacks silhouette clearance.");
                Require(rot.All(r => tile.DistanceTo(board.GetTileById(r)!) >= RotBalance.QuarantineStartingRotClearance),
                    "An authored starting slot lacks rot clearance.");
            }
            for (int i = 0; i < startIds.Length; i++)
                for (int j = i + 1; j < startIds.Length; j++)
                    Require(board.GetTileById(startIds[i])!.DistanceTo(board.GetTileById(startIds[j])!) >= RotBalance.QuarantineStartingSeparation,
                        "Authored starting slots are too close together.");
            var mouthDistances = Distances(new HashSet<int>(main.Concat(new[] { entrance })), entrance, board.Width, true);
            Require(mouthDistances[startIds[0]] >= mouthDistances[startIds[1]], "The human must not be uniquely closest to the mouth.");

            return new QuarantineRotPlan(rot, pocket, corridor, AuthoredStarts.Take(playerCount), entrance, exit,
                Distances(corridor, entrance, board.Width, false)[exit],
                Distances(corridor, entrance, board.Width, true)[exit]);
        }

        /// <summary>Check real placements after starting-position perks, without suppressing those perks.</summary>
        public static void ValidateEffectiveStartingPositions(GameBoard board, QuarantineRotPlan plan)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            Require(board.Players.Count == plan.StartingPositions.Count, "The placed player count differs from the authored plan.");
            var excluded = new HashSet<int>(plan.RotTileIds.Concat(plan.PocketTileIds).Concat(plan.CorridorTileIds));
            var starts = board.Players.Select(player =>
            {
                int id = player.StartingTileId ?? throw new ArgumentException("A quarantine player has no starting spore.", nameof(board));
                return board.GetTileById(id) ?? throw new ArgumentException("A quarantine starting spore is outside the board.", nameof(board));
            }).ToArray();
            foreach (var tile in starts)
            {
                Require(!tile.IsBlocked && !excluded.Contains(tile.TileId), "A starting perk moved a spore outside the main region.");
                Require(board.GetPlayableEdgeDistance(tile.TileId) >= RotBalance.QuarantineStartingEdgeClearance,
                    "An effective starting slot lacks silhouette clearance.");
                Require(plan.RotTileIds.All(id => tile.DistanceTo(board.GetTileById(id)!) >= RotBalance.QuarantineEffectiveStartingRotClearance),
                    "An effective starting slot is too close to rot.");
            }
            for (int i = 0; i < starts.Length; i++)
                for (int j = i+1; j < starts.Length; j++)
                    Require(starts[i].DistanceTo(starts[j]) >= RotBalance.QuarantineEffectiveStartingSeparation,
                        "Starting perks moved quarantine spores too close together.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException("Invalid quarantine board: " + message, "board");
        }

        private static void RequireConnected(HashSet<int> tiles, int width, bool diagonal, string message)
        {
            Require(tiles.Count > 0 && Distances(tiles, tiles.Min(), width, diagonal).Count == tiles.Count, message);
        }

        private static Dictionary<int, int> Distances(HashSet<int> allowed, int seed, int width, bool diagonal)
        {
            var distances = new Dictionary<int, int> { [seed] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                int x = id % width, y = id / width;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx == 0 && dy == 0) || (!diagonal && Math.Abs(dx) + Math.Abs(dy) != 1)) continue;
                        if (x + dx < 0 || x + dx >= width || y + dy < 0) continue;
                        int neighbor = (y + dy) * width + x + dx;
                        if (!allowed.Contains(neighbor) || distances.ContainsKey(neighbor)) continue;
                        distances.Add(neighbor, distances[id] + 1);
                        queue.Enqueue(neighbor);
                    }
            }
            return distances;
        }
    }
}
