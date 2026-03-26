using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class NutrientPatchFactoryTests
{
    [Fact]
    public void CreateAdaptogenCluster_sets_expected_patch_metadata()
    {
        var patch = NutrientPatch.CreateAdaptogenCluster(clusterId: 7, clusterTileCount: 3);

        Assert.Equal(7, patch.ClusterId);
        Assert.Equal(3, patch.ClusterTileCount);
        Assert.Equal(NutrientPatchType.Adaptogen, patch.PatchType);
        Assert.Equal(NutrientRewardType.MutationPoints, patch.RewardType);
        Assert.Equal(3, patch.RewardAmount);
        Assert.Equal("Adaptogen Patch", patch.DisplayName);
        Assert.Contains("+3 Mutation Points", patch.Description);
    }

    [Fact]
    public void CreateSporemealCluster_sets_expected_patch_metadata()
    {
        var patch = NutrientPatch.CreateSporemealCluster(clusterId: 8, clusterTileCount: 4);

        Assert.Equal(8, patch.ClusterId);
        Assert.Equal(4, patch.ClusterTileCount);
        Assert.Equal(NutrientPatchType.Sporemeal, patch.PatchType);
        Assert.Equal(NutrientRewardType.FreeGrowth, patch.RewardType);
        Assert.Equal(3, patch.RewardAmount);
        Assert.Equal("Sporemeal Patch", patch.DisplayName);
        Assert.Contains("3 free growth tiles", patch.Description);
    }

    [Fact]
    public void CreateHypervariationCluster_sets_expected_patch_metadata()
    {
        var patch = NutrientPatch.CreateHypervariationCluster(clusterId: 9, clusterTileCount: 6);

        Assert.Equal(9, patch.ClusterId);
        Assert.Equal(6, patch.ClusterTileCount);
        Assert.Equal(NutrientPatchType.Hypervariation, patch.PatchType);
        Assert.Equal(NutrientRewardType.MycovariantDraft, patch.RewardType);
        Assert.Equal(1, patch.RewardAmount);
        Assert.Equal("Hypervariation Patch", patch.DisplayName);
        Assert.Contains("Hypervariation draft", patch.Description);
    }
}
