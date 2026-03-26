using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class NutrientPatchPlacementUtilityTests
{
    [Fact]
    public void CreateClusterPatch_returns_hypervariation_only_for_eligible_cluster_sizes()
    {
        var eligiblePatch = NutrientPatchPlacementUtility.CreateClusterPatch(
            clusterId: 1,
            clusterTileCount: 4,
            patchRoll: 0d,
            fallbackRewardRoll: 0.9d);
        var tooLargePatch = NutrientPatchPlacementUtility.CreateClusterPatch(
            clusterId: 2,
            clusterTileCount: 7,
            patchRoll: 0d,
            fallbackRewardRoll: 0.9d);

        Assert.Equal(NutrientPatchType.Hypervariation, eligiblePatch.PatchType);
        Assert.NotEqual(NutrientPatchType.Hypervariation, tooLargePatch.PatchType);
    }
}
