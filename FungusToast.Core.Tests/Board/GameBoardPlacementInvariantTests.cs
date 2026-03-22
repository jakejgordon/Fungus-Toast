using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class GameBoardPlacementInvariantTests
{
    [Fact]
    public void PlaceInitialSpore_does_not_overwrite_an_occupied_tile()
    {
        var board = CreateBoard(width: 6, height: 6, playerCount: 2);

        board.PlaceInitialSpore(playerId: 0, x: 2, y: 2);
        board.PlaceInitialSpore(playerId: 1, x: 2, y: 2);

        var tile = Assert.IsType<BoardTile>(board.GetTile(2, 2));
        var fungalCell = Assert.IsType<FungalCell>(tile.FungalCell);

        Assert.Equal(0, fungalCell.OwnerPlayerId);
        Assert.Single(board.Players[0].ControlledTileIds);
        Assert.Empty(board.Players[1].ControlledTileIds);
        Assert.Null(board.Players[1].StartingTileId);
    }

    [Fact]
    public void SpawnSporeForPlayer_rejects_tiles_with_existing_nutrient_patch()
    {
        var board = CreateBoard(width: 6, height: 6, playerCount: 1);
        var player = board.Players[0];
        const int tileId = 8;

        var placedPatch = board.PlaceNutrientPatch(tileId, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));
        var spawned = board.SpawnSporeForPlayer(player, tileId, GrowthSource.HyphalOutgrowth);

        Assert.True(placedPatch, "Expected nutrient patch placement to succeed on an empty tile.");
        Assert.False(spawned, "Expected spore placement to fail when a nutrient patch already occupies the tile.");
        Assert.Empty(player.ControlledTileIds);
        Assert.Null(board.GetCell(tileId));
    }

    [Fact]
    public void SpawnSporeForPlayer_places_a_newly_grown_cell_and_updates_player_control()
    {
        var board = CreateBoard(width: 6, height: 6, playerCount: 1);
        var player = board.Players[0];
        const int tileId = 8;

        var spawned = board.SpawnSporeForPlayer(player, tileId, GrowthSource.HyphalOutgrowth);

        var cell = Assert.IsType<FungalCell>(board.GetCell(tileId));
        Assert.True(spawned, "Expected spore placement on an empty tile to succeed.");
        Assert.Equal(player.PlayerId, cell.OwnerPlayerId);
        Assert.Equal(GrowthSource.HyphalOutgrowth, cell.SourceOfGrowth);
        Assert.True(cell.IsNewlyGrown, "Expected non-initial spores placed through SpawnSporeForPlayer to be marked newly grown.");
        Assert.Contains(tileId, player.ControlledTileIds);
    }

    [Fact]
    public void TryRelocateStartingSpore_moves_the_resistant_starting_cell_and_updates_bookkeeping()
    {
        var board = CreateBoard(width: 6, height: 6, playerCount: 1);
        var player = board.Players[0];

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        int originalTileId = Assert.IsType<int>(player.StartingTileId);
        int targetTileId = 3 + (4 * board.Width);

        var relocated = board.TryRelocateStartingSpore(player, targetTileId);

        Assert.True(relocated, "Expected relocation of a starting spore to an empty target tile to succeed.");
        Assert.Null(board.GetCell(originalTileId));

        var relocatedCell = Assert.IsType<FungalCell>(board.GetCell(targetTileId));
        Assert.True(relocatedCell.IsResistant, "Expected relocated starting spores to remain resistant.");
        Assert.Equal(targetTileId, player.StartingTileId);
        Assert.DoesNotContain(originalTileId, player.ControlledTileIds);
        Assert.Contains(targetTileId, player.ControlledTileIds);
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
