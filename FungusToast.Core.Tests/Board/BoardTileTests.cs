using FungusToast.Core.Board;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Tests.Board;

public class BoardTileTests
{
    [Fact]
    public void DistanceTo_returns_manhattan_distance()
    {
        var source = new BoardTile(x: 1, y: 2, boardWidth: 10);
        var target = new BoardTile(x: 4, y: 6, boardWidth: 10);

        var distance = source.DistanceTo(target);

        Assert.Equal(7, distance);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(4, 0, true)]
    [InlineData(0, 3, true)]
    [InlineData(4, 3, true)]
    [InlineData(2, 1, false)]
    public void IsOnBorder_reports_whether_tile_touches_the_board_edge(int x, int y, bool expected)
    {
        var tile = new BoardTile(x, y, boardWidth: 5);

        var isOnBorder = tile.IsOnBorder(boardWidth: 5, boardHeight: 4);

        Assert.Equal(expected, isOnBorder);
    }

    [Fact]
    public void Proxy_properties_reflect_the_underlying_fungal_cell_state()
    {
        var board = new GameBoard(width: 10, height: 10, playerCount: 1);
        var tile = board.GetTile(2, 3);
        var cell = new FungalCell(ownerPlayerId: 0, tileId: 32, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();
        cell.SetBirthRound(2);
        cell.SetGrowthCycleAge(4);

        board.PlaceFungalCell(cell);

        var occupiedTile = Assert.IsType<BoardTile>(tile);
        Assert.True(occupiedTile.IsOccupied, "Expected tile to report occupied after a fungal cell is placed.");
        Assert.True(occupiedTile.IsAlive, "Expected tile to report alive when its fungal cell is alive.");
        Assert.False(occupiedTile.IsDead, "Expected tile not to report dead when its fungal cell is alive.");
        Assert.False(occupiedTile.IsToxin, "Expected tile not to report toxin when its fungal cell is a normal spore.");
        Assert.True(occupiedTile.IsResistant, "Expected tile to report resistant when its fungal cell is resistant.");
        Assert.Equal(FungalCellType.Alive, occupiedTile.CellType);
        Assert.Equal(0, occupiedTile.OriginalOwnerPlayerId);
        Assert.Equal(4, occupiedTile.GrowthCycleAge);
    }
}
