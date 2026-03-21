namespace FungusToast.Core.Board
{
    public enum NutrientPatchType
    {
        Adaptogen = 1,
        Sporemeal = 2
    }

    public enum NutrientRewardType
    {
        MutationPoints = 1,
        FreeGrowth = 2
    }

    public sealed class NutrientPatch
    {
        public NutrientPatch(
            int clusterId,
            int clusterTileCount,
            NutrientPatchType patchType,
            string displayName,
            string description,
            NutrientRewardType rewardType,
            int rewardAmount)
        {
            ClusterId = clusterId;
            ClusterTileCount = clusterTileCount;
            PatchType = patchType;
            DisplayName = displayName;
            Description = description;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }

        public int ClusterId { get; }
        public int ClusterTileCount { get; }
        public NutrientPatchType PatchType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public NutrientRewardType RewardType { get; }
        public int RewardAmount { get; }

        public static NutrientPatch CreateAdaptogenCluster(int clusterId, int clusterTileCount)
        {
            string pointLabel = clusterTileCount == 1 ? "Point" : "Points";
            return new NutrientPatch(
                clusterId,
                clusterTileCount,
                NutrientPatchType.Adaptogen,
                "Adaptogen Patch",
                $"A mutagen-rich fungal feast. The first living mold cell to grow onto any tile in this cluster claims the whole patch for +{clusterTileCount} Mutation {pointLabel}.",
                NutrientRewardType.MutationPoints,
                clusterTileCount);
        }

        public static NutrientPatch CreateSporemealCluster(int clusterId, int clusterTileCount)
        {
            int freeGrowthCount = Math.Max(0, clusterTileCount - 1);
            string growthLabel = freeGrowthCount == 1 ? "tile" : "tiles";
            return new NutrientPatch(
                clusterId,
                clusterTileCount,
                NutrientPatchType.Sporemeal,
                "Sporemeal Patch",
                $"A growth-packed fungal banquet. The first living mold cell to grow onto any tile in this cluster spreads through the rest of the patch for {freeGrowthCount} free growth {growthLabel}.",
                NutrientRewardType.FreeGrowth,
                freeGrowthCount);
        }
    }
}