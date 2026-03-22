using FungusToast.Core.Board;
using System.Linq;

namespace FungusToast.Core.Tests.Board;

public class StartingSporeScaledLayoutTests
{
    [Theory]
    [InlineData(2, 80, 80)]
    [InlineData(3, 120, 90)]
    [InlineData(4, 200, 120)]
    [InlineData(5, 96, 144)]
    [InlineData(6, 220, 220)]
    [InlineData(7, 75, 130)]
    [InlineData(8, 240, 100)]
    public void GetStartingPositions_for_precomputed_player_counts_scales_to_unique_in_bounds_positions(int playerCount, int boardWidth, int boardHeight)
    {
        var positions = StartingSporeUtility.GetStartingPositions(boardWidth, boardHeight, playerCount);

        Assert.Equal(playerCount, positions.Count);
        Assert.Equal(playerCount, positions.Distinct().Count());
        Assert.All(positions, position =>
        {
            Assert.InRange(position.x, 0, boardWidth - 1);
            Assert.InRange(position.y, 0, boardHeight - 1);
        });
    }

    [Theory]
    [InlineData(2, 80, 80)]
    [InlineData(3, 120, 90)]
    [InlineData(4, 200, 120)]
    [InlineData(5, 96, 144)]
    [InlineData(6, 220, 220)]
    [InlineData(7, 75, 130)]
    [InlineData(8, 240, 100)]
    public void GetStartingPositions_for_precomputed_player_counts_matches_reference_layout_scaling(int playerCount, int boardWidth, int boardHeight)
    {
        var referencePositions = StartingSporeUtility.GetStartingPositions(160, 160, playerCount);
        var scaledPositions = StartingSporeUtility.GetStartingPositions(boardWidth, boardHeight, playerCount);

        var expectedPositions = referencePositions
            .Select(position =>
            (
                ScaleCoordinate(position.x, 160, boardWidth),
                ScaleCoordinate(position.y, 160, boardHeight)
            ))
            .ToArray();

        Assert.Equal(expectedPositions, scaledPositions);
    }

    private static int ScaleCoordinate(int coordinate, int referenceBoardSize, int targetBoardSize)
    {
        if (targetBoardSize <= 1)
        {
            return 0;
        }

        double referenceMax = Math.Max(1, referenceBoardSize - 1);
        double targetMax = targetBoardSize - 1;
        return Math.Clamp((int)Math.Round((coordinate / referenceMax) * targetMax), 0, targetBoardSize - 1);
    }
}
