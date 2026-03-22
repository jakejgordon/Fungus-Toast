using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class GameBoardCoordinateTests
{
    [Fact]
    public void GetXYFromTileId_round_trips_expected_coordinates()
    {
        var board = CreateBoard(width: 5, height: 4, playerCount: 0);

        Assert.Equal((0, 0), board.GetXYFromTileId(0));
        Assert.Equal((4, 0), board.GetXYFromTileId(4));
        Assert.Equal((0, 1), board.GetXYFromTileId(5));
        Assert.Equal((3, 2), board.GetXYFromTileId(13));
        Assert.Equal((4, 3), board.GetXYFromTileId(19));
    }

    [Fact]
    public void GetTile_returns_null_for_out_of_bounds_coordinates()
    {
        var board = CreateBoard(width: 5, height: 4, playerCount: 0);

        Assert.Null(board.GetTile(-1, 0));
        Assert.Null(board.GetTile(0, -1));
        Assert.Null(board.GetTile(5, 0));
        Assert.Null(board.GetTile(0, 4));
    }

    [Fact]
    public void GetTileById_returns_tile_with_expected_coordinates_and_id()
    {
        var board = CreateBoard(width: 5, height: 4, playerCount: 0);

        var tile = Assert.IsType<BoardTile>(board.GetTileById(13));

        Assert.Equal(13, tile.TileId);
        Assert.Equal(3, tile.X);
        Assert.Equal(2, tile.Y);
    }

    [Fact]
    public void AllTiles_returns_every_tile_on_the_board_once()
    {
        var board = CreateBoard(width: 5, height: 4, playerCount: 0);

        var tiles = board.AllTiles().ToArray();

        Assert.Equal(20, tiles.Length);
        Assert.Equal(20, tiles.Select(tile => tile.TileId).Distinct().Count());
        Assert.Equal(Enumerable.Range(0, 20), tiles.Select(tile => tile.TileId).OrderBy(id => id));
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
