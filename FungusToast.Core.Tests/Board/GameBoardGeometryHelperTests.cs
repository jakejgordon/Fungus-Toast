using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class GameBoardGeometryHelperTests
{
    [Fact]
    public void GetTileLine_returns_expected_straight_path_without_including_start_tile()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);

        var line = board.GetTileLine(startTileId: 12, direction: CardinalDirection.East, length: 3);

        Assert.Equal(new[] { 13, 14 }, line);
    }

    [Fact]
    public void GetTileLine_can_include_start_tile_and_stops_at_board_edge()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);

        var line = board.GetTileLine(startTileId: 12, direction: CardinalDirection.North, length: 4, includeStartingTile: true);

        Assert.Equal(new[] { 12, 17, 22 }, line);
    }

    [Theory]
    [InlineData(CardinalDirection.North)]
    [InlineData(CardinalDirection.East)]
    [InlineData(CardinalDirection.South)]
    [InlineData(CardinalDirection.West)]
    public void GetTileCone_returns_unique_in_bounds_tiles_for_each_direction(CardinalDirection direction)
    {
        var board = new GameBoard(width: 9, height: 9, playerCount: 0);

        var cone = board.GetTileCone(startTileId: 40, direction);

        Assert.Equal(cone.Distinct().Count(), cone.Count);
        Assert.All(cone, tileId => Assert.InRange(tileId, 0, 80));
    }

    [Fact]
    public void GetTileCone_for_north_points_only_to_tiles_with_greater_y_than_start()
    {
        var board = new GameBoard(width: 9, height: 9, playerCount: 0);
        var (startX, startY) = board.GetXYFromTileId(40);

        var cone = board.GetTileCone(startTileId: 40, direction: CardinalDirection.North)
            .Select(board.GetXYFromTileId)
            .ToArray();

        Assert.All(cone, position => Assert.True(position.y > startY, $"Expected north-facing cone tiles to have y > {startY}, but found ({position.x}, {position.y})."));
    }

    [Fact]
    public void GetTileCone_for_east_points_only_to_tiles_with_greater_x_than_start()
    {
        var board = new GameBoard(width: 9, height: 9, playerCount: 0);
        var (startX, startY) = board.GetXYFromTileId(40);

        var cone = board.GetTileCone(startTileId: 40, direction: CardinalDirection.East)
            .Select(board.GetXYFromTileId)
            .ToArray();

        Assert.All(cone, position => Assert.True(position.x > startX, $"Expected east-facing cone tiles to have x > {startX}, but found ({position.x}, {position.y})."));
    }
}
