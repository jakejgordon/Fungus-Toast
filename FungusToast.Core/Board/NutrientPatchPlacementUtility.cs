using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    public static class NutrientPatchPlacementUtility
    {
        public static int PlaceStartingNutrientPatches(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            if (board == null || players == null || rng == null)
            {
                return 0;
            }

            int baseTargetCount = Math.Max(
                GameBalance.NutrientPatchMinimumCount,
                (int)Math.Round(board.TotalTiles * GameBalance.NutrientPatchDensity, MidpointRounding.AwayFromZero));
            int targetCount = Math.Max(
                GameBalance.NutrientPatchMinimumCount,
                (int)Math.Floor(baseTargetCount * GameBalance.NutrientPatchTotalTileMultiplier));

            int minimumDistanceFromStartingSpores = Math.Max(
                1,
                (int)Math.Ceiling(board.Width * GameBalance.NutrientPatchMinimumDistanceFromStartingSporeWidthFactor));

            var startingTileIds = players
                .Where(player => player.StartingTileId.HasValue)
                .Select(player => player.StartingTileId!.Value)
                .ToList();

            var candidateTileIds = board.AllTiles()
                .Where(tile => !tile.IsOccupied && !tile.HasNutrientPatch)
                .Select(tile => tile.TileId)
                .ToList();

            var candidateRegionIndicesByTileId = BuildNearestStartingRegionIndices(board, candidateTileIds, startingTileIds);
            int[] placedTileCountsByRegion = new int[startingTileIds.Count];

            Shuffle(candidateTileIds, rng);

            int placedTileCount = 0;
            int nextClusterId = 1;

            while (targetCount - placedTileCount >= GameBalance.NutrientPatchClusterMinimumSize)
            {
                int remainingTileBudget = targetCount - placedTileCount;
                int maxClusterSize = Math.Min(GameBalance.NutrientPatchClusterMaximumSize, remainingTileBudget);
                if (maxClusterSize < GameBalance.NutrientPatchClusterMinimumSize)
                {
                    break;
                }

                List<int> sizeOptions = BuildWeightedClusterSizeOptions(maxClusterSize);
                Shuffle(sizeOptions, rng);

                bool clusterPlaced = false;
                foreach (int desiredClusterSize in sizeOptions)
                {
                    var clusterTileIds = TryBuildCluster(
                        board,
                        candidateTileIds,
                        startingTileIds,
                        minimumDistanceFromStartingSpores,
                        desiredClusterSize,
                        rng,
                        candidateRegionIndicesByTileId,
                        placedTileCountsByRegion);
                    if (clusterTileIds.Count < GameBalance.NutrientPatchClusterMinimumSize)
                    {
                        continue;
                    }

                    NutrientPatch clusterPatch = CreateClusterPatch(
                        nextClusterId++,
                        clusterTileIds.Count,
                        rng.NextDouble(),
                        rng.NextDouble());
                    foreach (int clusterTileId in clusterTileIds)
                    {
                        if (board.PlaceNutrientPatch(clusterTileId, clusterPatch))
                        {
                            placedTileCount++;
                            if (candidateRegionIndicesByTileId.TryGetValue(clusterTileId, out int regionIndex))
                            {
                                placedTileCountsByRegion[regionIndex]++;
                            }
                        }
                    }

                    clusterPlaced = true;
                    break;
                }

                if (!clusterPlaced)
                {
                    break;
                }
            }

            observer?.RecordNutrientPatchesPlaced(placedTileCount);
            return placedTileCount;
        }

        internal static NutrientPatch CreateClusterPatch(
            int clusterId,
            int clusterTileCount,
            double patchRoll,
            double fallbackRewardRoll,
            NutrientPatchSource source = NutrientPatchSource.StartingBoard,
            bool allowHypervariation = true)
        {
            if (allowHypervariation
                && clusterTileCount >= GameBalance.HypervariationPatchClusterMinimumSize
                && clusterTileCount <= GameBalance.HypervariationPatchClusterMaximumSize
                && patchRoll < GameBalance.HypervariationPatchChance)
            {
                return NutrientPatch.CreateHypervariationCluster(clusterId, clusterTileCount, source);
            }

            return fallbackRewardRoll < 0.5d
                ? NutrientPatch.CreateAdaptogenCluster(clusterId, clusterTileCount, source)
                : NutrientPatch.CreateSporemealCluster(clusterId, clusterTileCount, source);
        }

        private static List<int> TryBuildCluster(
            GameBoard board,
            IReadOnlyList<int> candidateTileIds,
            IReadOnlyList<int> startingTileIds,
            int minimumDistanceFromStartingSpores,
            int desiredClusterSize,
            Random rng,
            IReadOnlyDictionary<int, int> candidateRegionIndicesByTileId,
            IReadOnlyList<int> placedTileCountsByRegion)
        {
            foreach (int regionIndex in BuildRegionPlacementOrder(placedTileCountsByRegion))
            {
                var regionalSeedTileIds = candidateTileIds
                    .Where(tileId => candidateRegionIndicesByTileId.TryGetValue(tileId, out int tileRegionIndex) && tileRegionIndex == regionIndex)
                    .ToList();

                List<int> clusterTileIds = TryBuildClusterFromSeedPool(
                    board,
                    regionalSeedTileIds,
                    startingTileIds,
                    minimumDistanceFromStartingSpores,
                    desiredClusterSize,
                    rng);
                if (clusterTileIds.Count >= GameBalance.NutrientPatchClusterMinimumSize)
                {
                    return clusterTileIds;
                }
            }

            return TryBuildClusterFromSeedPool(
                board,
                candidateTileIds,
                startingTileIds,
                minimumDistanceFromStartingSpores,
                desiredClusterSize,
                rng);
        }

        private static List<int> TryBuildClusterFromSeedPool(
            GameBoard board,
            IReadOnlyList<int> seedCandidateTileIds,
            IReadOnlyList<int> startingTileIds,
            int minimumDistanceFromStartingSpores,
            int desiredClusterSize,
            Random rng)
        {
            var seedTileIds = seedCandidateTileIds
                .Where(tileId => IsEligibleClusterTile(
                    board,
                    tileId,
                    startingTileIds,
                    minimumDistanceFromStartingSpores,
                    Array.Empty<int>()))
                .ToList();

            Shuffle(seedTileIds, rng);

            foreach (int seedTileId in seedTileIds)
            {
                var clusterTileIds = new HashSet<int> { seedTileId };
                var frontierTileIds = new List<int> { seedTileId };

                while (clusterTileIds.Count < desiredClusterSize && frontierTileIds.Count > 0)
                {
                    int frontierIndex = rng.Next(frontierTileIds.Count);
                    int frontierTileId = frontierTileIds[frontierIndex];

                    var growthOptions = board.GetOrthogonalNeighbors(frontierTileId)
                        .Select(tile => tile.TileId)
                        .Where(tileId => !clusterTileIds.Contains(tileId))
                        .Where(tileId => IsEligibleClusterTile(
                            board,
                            tileId,
                            startingTileIds,
                            minimumDistanceFromStartingSpores,
                            clusterTileIds))
                        .ToList();

                    if (growthOptions.Count == 0)
                    {
                        frontierTileIds.RemoveAt(frontierIndex);
                        continue;
                    }

                    int nextTileId = growthOptions[rng.Next(growthOptions.Count)];
                    if (clusterTileIds.Add(nextTileId))
                    {
                        frontierTileIds.Add(nextTileId);
                    }
                }

                if (clusterTileIds.Count >= GameBalance.NutrientPatchClusterMinimumSize)
                {
                    return clusterTileIds.ToList();
                }
            }

            return new List<int>();
        }

        private static List<int> BuildWeightedClusterSizeOptions(int maxClusterSize)
        {
            var sizeOptions = new List<int>();
            for (int size = GameBalance.NutrientPatchClusterMinimumSize; size <= maxClusterSize; size++)
            {
                int weight = maxClusterSize - size + 1;
                for (int i = 0; i < weight; i++)
                {
                    sizeOptions.Add(size);
                }
            }

            return sizeOptions;
        }

        private static bool IsEligibleClusterTile(
            GameBoard board,
            int candidateTileId,
            IReadOnlyList<int> startingTileIds,
            int minimumDistanceFromStartingSpores,
            IEnumerable<int> currentClusterTileIds)
        {
            BoardTile? candidateTile = board.GetTileById(candidateTileId);
            if (candidateTile == null || candidateTile.IsOccupied || candidateTile.HasNutrientPatch)
            {
                return false;
            }

            if (!IsFarEnoughFromStartingSpores(board, candidateTileId, startingTileIds, minimumDistanceFromStartingSpores))
            {
                return false;
            }

            HashSet<int> currentClusterTileSet = currentClusterTileIds as HashSet<int> ?? new HashSet<int>(currentClusterTileIds);
            return board.GetOrthogonalNeighbors(candidateTileId)
                .All(orthogonalNeighbor => !orthogonalNeighbor.HasNutrientPatch || currentClusterTileSet.Contains(orthogonalNeighbor.TileId));
        }

        private static bool IsFarEnoughFromStartingSpores(
            GameBoard board,
            int candidateTileId,
            IReadOnlyList<int> startingTileIds,
            int minimumDistance)
        {
            foreach (int startingTileId in startingTileIds)
            {
                var candidateTile = board.GetTileById(candidateTileId);
                var startingTile = board.GetTileById(startingTileId);
                if (candidateTile == null || startingTile == null)
                {
                    continue;
                }

                if (candidateTile.DistanceTo(startingTile) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<int, int> BuildNearestStartingRegionIndices(
            GameBoard board,
            IReadOnlyList<int> candidateTileIds,
            IReadOnlyList<int> startingTileIds)
        {
            var tileRegionIndices = new Dictionary<int, int>();
            if (startingTileIds.Count == 0)
            {
                return tileRegionIndices;
            }

            foreach (int candidateTileId in candidateTileIds)
            {
                tileRegionIndices[candidateTileId] = GetNearestStartingRegionIndex(board, candidateTileId, startingTileIds);
            }

            return tileRegionIndices;
        }

        private static int GetNearestStartingRegionIndex(
            GameBoard board,
            int candidateTileId,
            IReadOnlyList<int> startingTileIds)
        {
            BoardTile? candidateTile = board.GetTileById(candidateTileId);
            if (candidateTile == null)
            {
                return 0;
            }

            int nearestRegionIndex = 0;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < startingTileIds.Count; i++)
            {
                BoardTile? startingTile = board.GetTileById(startingTileIds[i]);
                if (startingTile == null)
                {
                    continue;
                }

                int distance = candidateTile.DistanceTo(startingTile);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestRegionIndex = i;
                }
            }

            return nearestRegionIndex;
        }

        private static IEnumerable<int> BuildRegionPlacementOrder(IReadOnlyList<int> placedTileCountsByRegion)
        {
            return Enumerable.Range(0, placedTileCountsByRegion.Count)
                .OrderBy(regionIndex => placedTileCountsByRegion[regionIndex])
                .ThenBy(regionIndex => regionIndex);
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
