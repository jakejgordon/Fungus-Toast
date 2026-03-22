using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class ChemobeaconBoardTests
{
    [Fact]
    public void TryPlaceChemobeacon_adds_marker_and_raises_placed_event()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        int? eventPlayerId = null;
        int? eventTileId = null;
        board.ChemobeaconPlaced += (playerId, tileId) =>
        {
            eventPlayerId = playerId;
            eventTileId = tileId;
        };

        var placed = board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        Assert.True(placed);
        var marker = Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeacon(0));
        Assert.Equal(12, marker.TileId);
        Assert.Equal(0, eventPlayerId);
        Assert.Equal(12, eventTileId);
        Assert.True(board.IsChemobeaconTile(12));
        Assert.True(board.IsTileBlockedForOccupation(12));
    }

    [Fact]
    public void TryPlaceChemobeacon_rejects_second_marker_for_same_player()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        var placedAgain = board.TryPlaceChemobeacon(playerId: 0, tileId: 13, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        Assert.False(placedAgain);
        Assert.Equal(12, Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeacon(0)).TileId);
    }

    [Fact]
    public void TryPlaceChemobeacon_rejects_occupied_or_nutrient_tiles()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.PlaceInitialSpore(playerId: 0, x: 2, y: 2);
        board.PlaceNutrientPatch(tileId: 6, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));

        var placedOnOccupied = board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);
        var placedOnNutrient = board.TryPlaceChemobeacon(playerId: 0, tileId: 6, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        Assert.False(placedOnOccupied);
        Assert.False(placedOnNutrient);
        Assert.Null(board.GetChemobeacon(0));
    }

    [Fact]
    public void GetChemobeaconAtTile_and_GetActiveChemobeacons_reflect_current_markers()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);
        board.TryPlaceChemobeacon(playerId: 1, tileId: 18, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 4);

        var markerAt12 = Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeaconAtTile(12));
        var activeMarkers = board.GetActiveChemobeacons().OrderBy(marker => marker.PlayerId).ToArray();

        Assert.Equal(0, markerAt12.PlayerId);
        Assert.Equal(new[] { 0, 1 }, activeMarkers.Select(marker => marker.PlayerId).ToArray());
        Assert.Equal(new[] { 12, 18 }, activeMarkers.Select(marker => marker.TileId).ToArray());
    }

    [Fact]
    public void SynchronizeChemobeaconsWithSurges_updates_turns_remaining_from_player_state()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 5);
        board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 1);

        player.TickDownActiveSurges();
        board.SynchronizeChemobeaconsWithSurges(new[] { player });

        Assert.Equal(4, Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeacon(0)).TurnsRemaining);
    }

    [Fact]
    public void SynchronizeChemobeaconsWithSurges_removes_marker_and_raises_expired_event_when_surge_is_missing()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        int? expiredPlayerId = null;
        int? expiredTileId = null;
        board.ChemobeaconExpired += (playerId, tileId) =>
        {
            expiredPlayerId = playerId;
            expiredTileId = tileId;
        };
        board.TryPlaceChemobeacon(playerId: 0, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        board.SynchronizeChemobeaconsWithSurges(new[] { player });

        Assert.False(board.HasChemobeacon(0));
        Assert.Equal(0, expiredPlayerId);
        Assert.Equal(12, expiredTileId);
    }
}
