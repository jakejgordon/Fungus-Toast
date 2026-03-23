using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconHelperTests
{
    [Fact]
    public void TryGetActiveMarker_returns_false_when_beacon_is_inactive()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);

        bool found = ChemotacticBeaconHelper.TryGetActiveMarker(board, player, out var marker);

        Assert.False(found);
        Assert.Null(marker);
    }

    [Fact]
    public void TryGetActiveMarker_returns_true_when_beacon_is_active_and_marker_exists()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        player.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 5);
        bool placed = board.TryPlaceChemobeacon(player.PlayerId, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        bool found = ChemotacticBeaconHelper.TryGetActiveMarker(board, player, out var marker);

        Assert.True(placed);
        Assert.True(found);
        Assert.NotNull(marker);
        Assert.Equal(12, marker.TileId);
    }

    [Fact]
    public void TrySelectAITargetTile_returns_open_non_nutrient_tile()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 1);
        board.PlaceNutrientPatch(tileId: 6, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board);

        Assert.True(tileId.HasValue);
        Assert.True(board.IsTileOpenForChemobeacon(tileId.Value));
    }

    private static Player CreatePlayer()
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 99
        };
    }
}