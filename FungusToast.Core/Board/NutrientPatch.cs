namespace FungusToast.Core.Board
{
    public enum NutrientRewardType
    {
        MutationPoints = 1
    }

    public sealed class NutrientPatch
    {
        public NutrientPatch(
            int clusterId,
            int clusterTileCount,
            string displayName,
            string description,
            NutrientRewardType rewardType,
            int rewardAmount)
        {
            ClusterId = clusterId;
            ClusterTileCount = clusterTileCount;
            DisplayName = displayName;
            Description = description;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }

        public int ClusterId { get; }
        public int ClusterTileCount { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public NutrientRewardType RewardType { get; }
        public int RewardAmount { get; }

        public static NutrientPatch CreateMutationPointCluster(int clusterId, int clusterTileCount)
        {
            string pointLabel = clusterTileCount == 1 ? "Point" : "Points";
            return new NutrientPatch(
                clusterId,
                clusterTileCount,
                "Nutrient Cluster",
                $"Rich in nutrients. The first living mold cell to grow onto any tile in this cluster claims all {clusterTileCount} nutrients for +{clusterTileCount} Mutation {pointLabel}.",
                NutrientRewardType.MutationPoints,
                clusterTileCount);
        }
    }
}