using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class NutrientPatchQueryTests
{
    [Fact]
    public void AllNutrientPatchTiles_and_GetNutrientPatchCount_return_only_tiles_with_patches()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 0);

        board.PlaceNutrientPatch(tileId: 3, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));
        board.PlaceNutrientPatch(tileId: 8, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));
        board.PlaceNutrientPatch(tileId: 20, NutrientPatch.CreateSporemealCluster(clusterId: 11, clusterTileCount: 1));

        var patchTileIds = board.AllNutrientPatchTiles().Select(tile => tile.TileId).OrderBy(id => id).ToArray();

        Assert.Equal(new[] { 3, 8, 20 }, patchTileIds);
        Assert.Equal(3, board.GetNutrientPatchCount());
    }

    [Fact]
    public void GetNutrientClusterTileIds_returns_all_tiles_in_the_matching_cluster()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 0);

        board.PlaceNutrientPatch(tileId: 3, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));
        board.PlaceNutrientPatch(tileId: 8, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));
        board.PlaceNutrientPatch(tileId: 20, NutrientPatch.CreateSporemealCluster(clusterId: 11, clusterTileCount: 1));

        var clusterTileIds = board.GetNutrientClusterTileIds(3).OrderBy(id => id).ToArray();
        var unrelatedClusterTileIds = board.GetNutrientClusterTileIds(20).ToArray();
        var noPatchTileIds = board.GetNutrientClusterTileIds(0);

        Assert.Equal(new[] { 3, 8 }, clusterTileIds);
        Assert.Equal(new[] { 20 }, unrelatedClusterTileIds);
        Assert.Empty(noPatchTileIds);
    }

    [Fact]
    public void GetNutrientClusterTileCount_returns_cluster_tile_count_for_tile_patch_and_zero_otherwise()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 0);

        board.PlaceNutrientPatch(tileId: 3, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));
        board.PlaceNutrientPatch(tileId: 8, NutrientPatch.CreateAdaptogenCluster(clusterId: 10, clusterTileCount: 2));

        Assert.Equal(2, board.GetNutrientClusterTileCount(3));
        Assert.Equal(2, board.GetNutrientClusterTileCount(8));
        Assert.Equal(0, board.GetNutrientClusterTileCount(0));
    }

    private static GameBoard CreateBoard(int width, int height, int playerCount)
    {
        var board = new GameBoard(width, height, playerCount);
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            board.Players.Add(new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI));
        }

        return board;
    }
}
