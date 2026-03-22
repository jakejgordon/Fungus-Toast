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
        var tile = new BoardTile(x: 2, y: 3, boardWidth: 10);
        var cell = new FungalCell(ownerPlayerId: 1, tileId: tile.TileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();
        cell.SetBirthRound(2);
        cell.SetGrowthCycleAge(4);

        typeof(BoardTile)
            .GetMethod("PlaceFungalCell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(tile, new object[] { cell });

        Assert.True(tile.IsOccupied, "Expected tile to report occupied after a fungal cell is placed.");
        Assert.True(tile.IsAlive, "Expected tile to report alive when its fungal cell is alive.");
        Assert.False(tile.IsDead, "Expected tile not to report dead when its fungal cell is alive.");
        Assert.False(tile.IsToxin, "Expected tile not to report toxin when its fungal cell is a normal spore.");
        Assert.True(tile.IsResistant, "Expected tile to report resistant when its fungal cell is resistant.");
        Assert.Equal(FungalCellType.Alive, tile.CellType);
        Assert.Equal(1, tile.OriginalOwnerPlayerId);
        Assert.Equal(4, tile.GrowthCycleAge);
    }
}
