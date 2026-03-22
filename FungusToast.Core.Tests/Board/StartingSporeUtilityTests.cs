using FungusToast.Core.Board;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Tests.Board;

public class StartingSporeUtilityTests
{
    [Fact]
    public void GetStartingPositions_for_one_player_returns_board_center()
    {
        var positions = StartingSporeUtility.GetStartingPositions(boardWidth: 11, boardHeight: 9, playerCount: 1);

        var position = Assert.Single(positions);
        Assert.Equal((5, 4), position);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void GetStartingPositions_for_precomputed_player_counts_returns_unique_in_bounds_positions(int playerCount)
    {
        const int boardWidth = 160;
        const int boardHeight = 160;

        var positions = StartingSporeUtility.GetStartingPositions(boardWidth, boardHeight, playerCount);

        Assert.Equal(playerCount, positions.Count);
        Assert.Equal(playerCount, positions.Distinct().Count());
        Assert.All(positions, position =>
        {
            Assert.InRange(position.x, 0, boardWidth - 1);
            Assert.InRange(position.y, 0, boardHeight - 1);
        });
    }

    [Fact]
    public void GetStartingPositions_with_override_positions_clamps_coordinates_into_board_bounds()
    {
        var overridePositions = new List<(int x, int y)>
        {
            (-10, -20),
            (999, 500),
            (3, 4),
        };

        var positions = StartingSporeUtility.GetStartingPositions(boardWidth: 10, boardHeight: 8, playerCount: 3, overridePositions);

        Assert.Equal(new[] { (0, 0), (9, 7), (3, 4) }, positions.ToArray());
    }

    [Fact]
    public void GetStartingPositions_with_duplicate_override_positions_resolves_to_unique_positions()
    {
        var overridePositions = new List<(int x, int y)>
        {
            (2, 2),
            (2, 2),
            (2, 2),
        };

        var positions = StartingSporeUtility.GetStartingPositions(boardWidth: 5, boardHeight: 5, playerCount: 3, overridePositions);

        Assert.Equal(3, positions.Count);
        Assert.Equal(3, positions.Distinct().Count());
        Assert.Contains((2, 2), positions);
        Assert.All(positions, position =>
        {
            Assert.InRange(position.x, 0, 4);
            Assert.InRange(position.y, 0, 4);
        });
    }

    [Fact]
    public void GetStartingPositionAnalysis_returns_one_entry_per_slot_in_slot_order()
    {
        var analysis = StartingSporeUtility.GetStartingPositionAnalysis(boardWidth: 160, boardHeight: 160, playerCount: 5);

        Assert.Equal(5, analysis.Entries.Count);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, analysis.Entries.Select(entry => entry.SlotIndex).ToArray());
        Assert.All(analysis.Entries, entry =>
        {
            Assert.InRange(entry.X, 0, 159);
            Assert.InRange(entry.Y, 0, 159);
            Assert.True(entry.UncontestedTileCount >= 0);
            Assert.True(entry.EarlyUncontestedTileCount >= 0);
            Assert.True(entry.TieTileCount >= 0);
            Assert.InRange(entry.FavorRank, 1, 5);
        });
    }
}
