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
        private static readonly Dictionary<(int Width, int Height, int Players), StartingPositionAnalysis> CachedAnalysis = new();
        private static readonly object CacheLock = new();
        private const int ReferenceBoardSize = 160;
        private static readonly Dictionary<int, (int x, int y)[]> PrecomputedReferenceLayouts = new()
        {
            [2] = new[]
            {
                (128, 80),
                (32, 80),
            },
            [3] = new[]
            {
                (141, 80),
                (50, 133),
                (50, 27),
            },
            [4] = new[]
            {
                (128, 128),
                (32, 128),
                (32, 32),
                (128, 32),
            },
            [5] = new[]
            {
                (114, 104),
                (67, 120),
                (38, 80),
                (67, 40),
                (114, 56),
            },
            [6] = new[]
            {
                (136, 95),
                (92, 126),
                (37, 123),
                (24, 65),
                (68, 34),
                (123, 37),
            },
            [7] = new[]
            {
                (139, 94),
                (106, 135),
                (54, 135),
                (21, 94),
                (32, 42),
                (80, 19),
                (128, 42),
            },
            [8] = new[]
            {
                (142, 106),
                (106, 142),
                (54, 142),
                (18, 106),
                (18, 54),
                (54, 18),
                (106, 18),
                (142, 54),
            },
        };

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

        public sealed class StartingPositionAnalysisEntry
        {
            public int SlotIndex { get; }
            public int X { get; }
            public int Y { get; }
            public int UncontestedTileCount { get; }
            public int EarlyUncontestedTileCount { get; }
            public int TieTileCount { get; }
            public int FavorRank { get; }

            public StartingPositionAnalysisEntry(int slotIndex, int x, int y, int uncontestedTileCount, int earlyUncontestedTileCount, int tieTileCount, int favorRank)
            {
                SlotIndex = slotIndex;
                X = x;
                Y = y;
                UncontestedTileCount = uncontestedTileCount;
                EarlyUncontestedTileCount = earlyUncontestedTileCount;
                TieTileCount = tieTileCount;
                FavorRank = favorRank;
            }
        }

        public sealed class StartingPositionAnalysis
        {
            public double LayoutScore { get; }
            public IReadOnlyList<StartingPositionAnalysisEntry> Entries { get; }

            public StartingPositionAnalysis(double layoutScore, IReadOnlyList<StartingPositionAnalysisEntry> entries)
            {
                LayoutScore = layoutScore;
                Entries = entries;
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
        public static IReadOnlyList<(int x, int y)> GetStartingPositions(int boardWidth, int boardHeight, int playerCount, IReadOnlyList<(int x, int y)>? overridePositions = null)
        {
            if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight <= 0) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (playerCount <= 0) return Array.Empty<(int x, int y)>();
            if (playerCount == 1)
            {
                return new[] { (boardWidth / 2, boardHeight / 2) };
            }

            if (overridePositions is { Count: > 0 })
            {
                return NormalizeOverridePositions(boardWidth, boardHeight, playerCount, overridePositions)
                    .Select(p => (p.X, p.Y))
                    .ToArray();
            }

            var key = (Width: boardWidth, Height: boardHeight, Players: playerCount);
            lock (CacheLock)
            {
                if (CachedStartingPositions.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var analysis = GetStartingPositionAnalysis(boardWidth, boardHeight, playerCount);
            return analysis.Entries
                .OrderBy(e => e.SlotIndex)
                .Select(e => (e.X, e.Y))
                .ToArray();
        }

        public static StartingPositionAnalysis GetStartingPositionAnalysis(int boardWidth, int boardHeight, int playerCount, IReadOnlyList<(int x, int y)>? overridePositions = null)
        {
            if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight <= 0) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (playerCount <= 0) return new StartingPositionAnalysis(0, Array.Empty<StartingPositionAnalysisEntry>());
            if (playerCount == 1)
            {
                return new StartingPositionAnalysis(0, new[]
                {
                    new StartingPositionAnalysisEntry(0, boardWidth / 2, boardHeight / 2, boardWidth * boardHeight, boardWidth * boardHeight, 0, 1)
                });
            }

            if (overridePositions is { Count: > 0 })
            {
                var normalizedOverride = NormalizeOverridePositions(boardWidth, boardHeight, playerCount, overridePositions);
                return BuildAnalysis(boardWidth, boardHeight, 0, normalizedOverride);
            }

            var key = (Width: boardWidth, Height: boardHeight, Players: playerCount);
            if (TryGetPrecomputedPositions(boardWidth, boardHeight, playerCount, out var precomputedPositions))
            {
                var precomputedAnalysis = BuildAnalysis(boardWidth, boardHeight, 0, precomputedPositions.Select(p => new StartPosition(p.x, p.y)).ToArray());
                lock (CacheLock)
                {
                    if (!CachedAnalysis.ContainsKey(key))
                    {
                        CachedAnalysis[key] = precomputedAnalysis;
                    }

                    if (!CachedStartingPositions.ContainsKey(key))
                    {
                        CachedStartingPositions[key] = precomputedPositions;
                    }

                    return CachedAnalysis[key];
                }
            }

            lock (CacheLock)
            {
                if (CachedAnalysis.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var best = FindBestLayout(boardWidth, boardHeight, playerCount);
            var analysis = BuildAnalysis(boardWidth, boardHeight, best.Score, best.Positions);

            lock (CacheLock)
            {
                if (!CachedAnalysis.ContainsKey(key))
                {
                    CachedAnalysis[key] = analysis;
                }

                if (!CachedStartingPositions.ContainsKey(key))
                {
                    CachedStartingPositions[key] = best.Positions.Select(p => (p.X, p.Y)).ToArray();
                }

                return CachedAnalysis[key];
            }
        }

        /// <summary>
        /// Places starting spores for all players using the shared layout.
        /// </summary>
        public static void PlaceStartingSpores(GameBoard board, List<Player> players, Random rng, bool shufflePlayerOrder = true, IReadOnlyList<(int x, int y)>? overridePositions = null)
        {
            var positions = GetStartingPositions(board.Width, board.Height, players.Count, overridePositions);

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

            // Square 6-player boards can need per-opposite-pair radii to avoid one obviously favored arc.
            if (playerCount == 6)
            {
                for (double radiusA = radiusMin; radiusA <= radiusMax + 0.0001; radiusA += radiusStep)
                {
                    for (double radiusB = radiusMin; radiusB <= radiusMax + 0.0001; radiusB += radiusStep)
                    {
                        for (double radiusC = radiusMin; radiusC <= radiusMax + 0.0001; radiusC += radiusStep)
                        {
                            foreach (double angleOffset in angleOffsets)
                            {
                                candidates.Add(BuildCandidateSixPlayer(boardWidth, boardHeight, radiusA, radiusB, radiusC, angleOffset));
                            }
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

        private static bool TryGetPrecomputedPositions(int boardWidth, int boardHeight, int playerCount, out (int x, int y)[] positions)
        {
            if (!PrecomputedReferenceLayouts.TryGetValue(playerCount, out var referencePositions))
            {
                positions = Array.Empty<(int x, int y)>();
                return false;
            }

            positions = ScaleReferencePositions(referencePositions, boardWidth, boardHeight)
                .Select(p => (p.X, p.Y))
                .ToArray();

            return true;
        }

        private static IReadOnlyList<StartPosition> NormalizeOverridePositions(int boardWidth, int boardHeight, int playerCount, IReadOnlyList<(int x, int y)> overridePositions)
        {
            if (overridePositions.Count != playerCount)
            {
                throw new ArgumentException($"Override positions count ({overridePositions.Count}) must match player count ({playerCount}).", nameof(overridePositions));
            }

            var normalized = overridePositions
                .Select(p => new StartPosition(
                    Math.Clamp(p.x, 0, boardWidth - 1),
                    Math.Clamp(p.y, 0, boardHeight - 1)))
                .ToList();

            return ResolveDuplicatePositions(normalized, boardWidth, boardHeight);
        }

        private static IReadOnlyList<StartPosition> ScaleReferencePositions(IReadOnlyList<(int x, int y)> referencePositions, int boardWidth, int boardHeight)
        {
            var scaled = referencePositions
                .Select(p => new StartPosition(
                    ScaleCoordinate(p.x, ReferenceBoardSize, boardWidth),
                    ScaleCoordinate(p.y, ReferenceBoardSize, boardHeight)))
                .ToList();

            return ResolveDuplicatePositions(scaled, boardWidth, boardHeight);
        }

        private static int ScaleCoordinate(int coordinate, int referenceBoardSize, int targetBoardSize)
        {
            if (targetBoardSize <= 1)
            {
                return 0;
            }

            double referenceMax = Math.Max(1, referenceBoardSize - 1);
            double targetMax = targetBoardSize - 1;
            return Math.Clamp((int)Math.Round((coordinate / referenceMax) * targetMax), 0, targetBoardSize - 1);
        }

        private static LayoutCandidate BuildCandidate(int boardWidth, int boardHeight, int playerCount, double cardinalRadiusFactor, double diagonalRadiusFactor, double angleOffset)
        {
            var positions = GenerateCircularPositions(boardWidth, boardHeight, playerCount, cardinalRadiusFactor, diagonalRadiusFactor, angleOffset);
            double score = ScoreLayout(boardWidth, boardHeight, positions);
            return new LayoutCandidate(score, positions);
        }

        private static LayoutCandidate BuildCandidateSixPlayer(int boardWidth, int boardHeight, double radiusA, double radiusB, double radiusC, double angleOffset)
        {
            var positions = GenerateSixPlayerPositions(boardWidth, boardHeight, radiusA, radiusB, radiusC, angleOffset);
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

        private static List<StartPosition> GenerateSixPlayerPositions(int boardWidth, int boardHeight, double radiusA, double radiusB, double radiusC, double angleOffset)
        {
            double baseRadius = Math.Min(boardWidth, boardHeight);
            double centerX = boardWidth / 2.0;
            double centerY = boardHeight / 2.0;
            var radiusFactors = new[] { radiusA, radiusB, radiusC, radiusA, radiusB, radiusC };
            var positions = new List<StartPosition>(6);

            for (int i = 0; i < 6; i++)
            {
                double angle = angleOffset + i * Math.PI * 2.0 / 6.0;
                double radius = baseRadius * radiusFactors[i];
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

        private static StartingPositionAnalysis BuildAnalysis(int boardWidth, int boardHeight, double layoutScore, IReadOnlyList<StartPosition> positions)
        {
            int playerCount = positions.Count;
            var ownership = new int[playerCount];
            var ties = new int[playerCount];
            var earlyOwnership = new int[playerCount];

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
                        if (bestDistance <= 100.0)
                        {
                            earlyOwnership[bestIndex]++;
                        }
                    }
                }
            }

            var rankOrder = Enumerable.Range(0, playerCount)
                .OrderByDescending(i => earlyOwnership[i])
                .ThenByDescending(i => ownership[i])
                .ThenBy(i => ties[i])
                .ToList();

            var ranks = new int[playerCount];
            for (int rank = 0; rank < rankOrder.Count; rank++)
            {
                ranks[rankOrder[rank]] = rank + 1;
            }

            var entries = Enumerable.Range(0, playerCount)
                .Select(i => new StartingPositionAnalysisEntry(
                    slotIndex: i,
                    x: positions[i].X,
                    y: positions[i].Y,
                    uncontestedTileCount: ownership[i],
                    earlyUncontestedTileCount: earlyOwnership[i],
                    tieTileCount: ties[i],
                    favorRank: ranks[i]))
                .ToArray();

            return new StartingPositionAnalysis(layoutScore, entries);
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
