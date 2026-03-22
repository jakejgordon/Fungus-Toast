using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class GameBoardNeighborTests
{
    [Fact]
    public void GetOrthogonalNeighbors_for_center_tile_returns_four_cardinal_neighbors()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighbors = board.GetOrthogonalNeighbors(1, 1)
            .Select(tile => tile.TileId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(new[] { 1, 4, 6, 9 }, neighbors);
    }

    [Fact]
    public void GetOrthogonalNeighbors_for_corner_tile_returns_two_neighbors()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighbors = board.GetOrthogonalNeighbors(0, 0)
            .Select(tile => tile.TileId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(new[] { 1, 4 }, neighbors);
    }

    [Fact]
    public void GetDiagonalNeighbors_for_center_tile_returns_four_diagonal_neighbors()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighbors = board.GetDiagonalNeighbors(1, 1)
            .Select(tile => tile.TileId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(new[] { 0, 2, 8, 10 }, neighbors);
    }

    [Fact]
    public void GetDiagonalNeighbors_for_corner_tile_returns_single_neighbor()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighbors = board.GetDiagonalNeighbors(0, 0)
            .Select(tile => tile.TileId)
            .ToArray();

        Assert.Equal(new[] { 5 }, neighbors);
    }

    [Fact]
    public void GetAdjacentTileIds_for_center_tile_returns_all_eight_neighbors()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighborIds = board.GetAdjacentTileIds(5).OrderBy(id => id).ToArray();

        Assert.Equal(new[] { 0, 1, 2, 4, 6, 8, 9, 10 }, neighborIds);
    }

    [Fact]
    public void GetAdjacentTileIds_for_corner_tile_returns_three_neighbors()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var neighborIds = board.GetAdjacentTileIds(0).OrderBy(id => id).ToArray();

        Assert.Equal(new[] { 1, 4, 5 }, neighborIds);
    }

    [Fact]
    public void GetAdjacentTiles_matches_adjacent_tile_ids()
    {
        var board = CreateBoard(width: 4, height: 4, playerCount: 0);

        var adjacentTiles = board.GetAdjacentTiles(5).Select(tile => tile.TileId).OrderBy(id => id).ToArray();
        var adjacentIds = board.GetAdjacentTileIds(5).OrderBy(id => id).ToArray();

        Assert.Equal(adjacentIds, adjacentTiles);
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
