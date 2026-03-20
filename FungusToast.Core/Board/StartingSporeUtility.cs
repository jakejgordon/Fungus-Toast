using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    /// <summary>
    /// Utility for computing and placing starting spores.
    /// Uses a board-aware layout search so starting positions are fairer across
    /// different board sizes and player counts than a fixed-radius circle.
    /// </summary>
    public static class StartingSporeUtility
    {
        private static readonly Dictionary<(int Width, int Height, int Players), (int x, int y)[]> CachedStartingPositions = new();
        private static readonly object CacheLock = new();

        private struct StartPosition
        {
            public int X { get; }
            public int Y { get; }

            public StartPosition(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private sealed class LayoutCandidate
        {
            public double Score { get; }
            public IReadOnlyList<StartPosition> Positions { get; }

            public LayoutCandidate(double score, IReadOnlyList<StartPosition> positions)
            {
                Score = score;
                Positions = positions;
            }
        }

        /// <summary>
        /// Returns the chosen starting positions for a board size and player count.
        /// Positions are ordered by player slot.
        /// </summary>
        public static IReadOnlyList<(int x, int y)> GetStartingPositions(int boardWidth, int boardHeight, int playerCount)
        {
            if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight <= 0) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (playerCount <= 0) return Array.Empty<(int x, int y)>();
            if (playerCount == 1)
            {
                return new[] { (boardWidth / 2, boardHeight / 2) };
            }

            var key = (Width: boardWidth, Height: boardHeight, Players: playerCount);
            lock (CacheLock)
            {
                if (CachedStartingPositions.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var best = FindBestLayout(boardWidth, boardHeight, playerCount);
            var computed = best.Positions.Select(p => (p.X, p.Y)).ToArray();

            lock (CacheLock)
            {
                if (!CachedStartingPositions.ContainsKey(key))
                {
                    CachedStartingPositions[key] = computed;
                }

                return CachedStartingPositions[key];
            }
        }

        /// <summary>
        /// Places starting spores for all players using the shared layout.
        /// </summary>
        public static void PlaceStartingSpores(GameBoard board, List<Player> players, Random rng, bool shufflePlayerOrder = true)
        {
            var positions = GetStartingPositions(board.Width, board.Height, players.Count);

            var playerIndices = Enumerable.Range(0, players.Count).ToList();
            if (shufflePlayerOrder)
            {
                playerIndices = playerIndices
                    .OrderBy(_ => rng.Next())
                    .ToList();
            }

            for (int i = 0; i < positions.Count; i++)
            {
                var (x, y) = positions[i];
                board.PlaceInitialSpore(playerIndices[i], x, y);
            }
        }

        private static LayoutCandidate FindBestLayout(int boardWidth, int boardHeight, int playerCount)
        {
            var candidates = new List<LayoutCandidate>();
            var radiusMin = 0.20;
            var radiusMax = 0.42;
            var radiusStep = 0.02;
            var angleOffsets = BuildAngleOffsets(playerCount);

            for (double uniformRadius = radiusMin; uniformRadius <= radiusMax + 0.0001; uniformRadius += radiusStep)
            {
                foreach (double angleOffset in angleOffsets)
                {
                    candidates.Add(BuildCandidate(boardWidth, boardHeight, playerCount, uniformRadius, uniformRadius, angleOffset));
                }
            }

            // Square 8-player boards benefit from letting cardinals and diagonals move independently.
            if (playerCount == 8)
            {
                for (double cardinalRadius = radiusMin; cardinalRadius <= radiusMax + 0.0001; cardinalRadius += radiusStep)
                {
                    for (double diagonalRadius = radiusMin; diagonalRadius <= radiusMax + 0.0001; diagonalRadius += radiusStep)
                    {
                        foreach (double angleOffset in angleOffsets)
                        {
                            candidates.Add(BuildCandidate(boardWidth, boardHeight, playerCount, cardinalRadius, diagonalRadius, angleOffset));
                        }
                    }
                }
            }

            return candidates
                .OrderBy(c => c.Score)
                .ThenBy(c => ComputeMinimumWallDistance(c.Positions, boardWidth, boardHeight))
                .First();
        }

        private static IEnumerable<double> BuildAngleOffsets(int playerCount)
        {
            int steps = Math.Max(8, playerCount * 4);
            for (int i = 0; i < steps; i++)
            {
                yield return (Math.PI * 2.0 / steps) * i;
            }
        }

        private static LayoutCandidate BuildCandidate(int boardWidth, int boardHeight, int playerCount, double cardinalRadiusFactor, double diagonalRadiusFactor, double angleOffset)
        {
            var positions = GenerateCircularPositions(boardWidth, boardHeight, playerCount, cardinalRadiusFactor, diagonalRadiusFactor, angleOffset);
            double score = ScoreLayout(boardWidth, boardHeight, positions);
            return new LayoutCandidate(score, positions);
        }

        private static List<StartPosition> GenerateCircularPositions(int boardWidth, int boardHeight, int playerCount, double cardinalRadiusFactor, double diagonalRadiusFactor, double angleOffset)
        {
            double baseRadius = Math.Min(boardWidth, boardHeight);
            double centerX = boardWidth / 2.0;
            double centerY = boardHeight / 2.0;
            var positions = new List<StartPosition>(playerCount);

            for (int i = 0; i < playerCount; i++)
            {
                double angle = angleOffset + i * Math.PI * 2.0 / playerCount;
                bool isCardinal = IsNearCardinal(angle);
                double radius = baseRadius * (isCardinal ? cardinalRadiusFactor : diagonalRadiusFactor);

                int x = Math.Clamp((int)Math.Round(centerX + radius * Math.Cos(angle)), 0, boardWidth - 1);
                int y = Math.Clamp((int)Math.Round(centerY + radius * Math.Sin(angle)), 0, boardHeight - 1);
                positions.Add(new StartPosition(x, y));
            }

            return ResolveDuplicatePositions(positions, boardWidth, boardHeight);
        }

        private static bool IsNearCardinal(double angle)
        {
            double normalized = NormalizeAngle(angle);
            double quarterTurn = Math.PI / 2.0;
            double nearestCardinal = Math.Round(normalized / quarterTurn) * quarterTurn;
            return Math.Abs(normalized - nearestCardinal) < 0.01;
        }

        private static double NormalizeAngle(double angle)
        {
            double twoPi = Math.PI * 2.0;
            double result = angle % twoPi;
            return result < 0 ? result + twoPi : result;
        }

        private static List<StartPosition> ResolveDuplicatePositions(List<StartPosition> positions, int boardWidth, int boardHeight)
        {
            var used = new HashSet<(int, int)>();
            var resolved = new List<StartPosition>(positions.Count);

            foreach (var position in positions)
            {
                if (used.Add((position.X, position.Y)))
                {
                    resolved.Add(position);
                    continue;
                }

                StartPosition replacement = FindNearestUnusedPosition(position, used, boardWidth, boardHeight);
                used.Add((replacement.X, replacement.Y));
                resolved.Add(replacement);
            }

            return resolved;
        }

        private static StartPosition FindNearestUnusedPosition(StartPosition origin, HashSet<(int, int)> used, int boardWidth, int boardHeight)
        {
            for (int radius = 1; radius < Math.Max(boardWidth, boardHeight); radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        {
                            continue;
                        }

                        int x = origin.X + dx;
                        int y = origin.Y + dy;
                        if (x < 0 || y < 0 || x >= boardWidth || y >= boardHeight)
                        {
                            continue;
                        }

                        if (!used.Contains((x, y)))
                        {
                            return new StartPosition(x, y);
                        }
                    }
                }
            }

            return origin;
        }

        private static double ScoreLayout(int boardWidth, int boardHeight, IReadOnlyList<StartPosition> positions)
        {
            int playerCount = positions.Count;
            var ownership = new int[playerCount];
            var ties = new int[playerCount];
            var earlyOwnership = new int[playerCount];
            var centerDistances = new double[playerCount];

            double centerX = boardWidth / 2.0;
            double centerY = boardHeight / 2.0;

            for (int i = 0; i < playerCount; i++)
            {
                centerDistances[i] = SquaredDistance(positions[i].X, positions[i].Y, centerX, centerY);
            }

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    double bestDistance = double.MaxValue;
                    int bestIndex = -1;
                    bool tied = false;

                    for (int i = 0; i < playerCount; i++)
                    {
                        double distance = SquaredDistance(positions[i].X, positions[i].Y, x, y);
                        if (distance + 0.000001 < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = i;
                            tied = false;
                        }
                        else if (Math.Abs(distance - bestDistance) < 0.000001)
                        {
                            tied = true;
                        }
                    }

                    if (bestIndex < 0)
                    {
                        continue;
                    }

                    if (tied)
                    {
                        for (int i = 0; i < playerCount; i++)
                        {
                            double distance = SquaredDistance(positions[i].X, positions[i].Y, x, y);
                            if (Math.Abs(distance - bestDistance) < 0.000001)
                            {
                                ties[i]++;
                            }
                        }
                    }
                    else
                    {
                        ownership[bestIndex]++;
                        if (bestDistance <= 100.0) // ~10 tile early-game radius
                        {
                            earlyOwnership[bestIndex]++;
                        }
                    }
                }
            }

            double varianceOwnership = Variance(ownership);
            double varianceEarlyOwnership = Variance(earlyOwnership);
            double varianceTies = Variance(ties);
            double varianceCenterDistance = Variance(centerDistances);
            double minimumSeparationPenalty = 1.0 / Math.Max(1.0, ComputeMinimumSeparationSquared(positions));

            return varianceOwnership
                + (varianceEarlyOwnership * 2.5)
                + (varianceTies * 0.25)
                + (varianceCenterDistance * 0.05)
                + (minimumSeparationPenalty * 500.0);
        }

        private static double ComputeMinimumSeparationSquared(IReadOnlyList<StartPosition> positions)
        {
            double minimum = double.MaxValue;
            for (int i = 0; i < positions.Count; i++)
            {
                for (int j = i + 1; j < positions.Count; j++)
                {
                    minimum = Math.Min(minimum, SquaredDistance(positions[i].X, positions[i].Y, positions[j].X, positions[j].Y));
                }
            }

            return minimum;
        }

        private static int ComputeMinimumWallDistance(IReadOnlyList<StartPosition> positions, int boardWidth, int boardHeight)
        {
            return positions.Min(p => Math.Min(Math.Min(p.X, boardWidth - 1 - p.X), Math.Min(p.Y, boardHeight - 1 - p.Y)));
        }

        private static double Variance(IReadOnlyList<int> values)
        {
            if (values.Count == 0) return 0;
            double mean = values.Average();
            return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        }

        private static double Variance(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return 0;
            double mean = values.Average();
            return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        }

        private static double SquaredDistance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return dx * dx + dy * dy;
        }
    }
}
