using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconHelperTests
{
    [Fact]
    public void GetNonTargetPenalty_starts_at_base_damper()
    {
        float penalty = ChemotacticBeaconHelper.GetNonTargetPenalty(level: 0);

        Assert.Equal(GameBalance.ChemotacticBeaconBaseNonTargetPenalty, penalty, precision: 3);
    }

    [Fact]
    public void GetNonTargetPenalty_reduces_by_level_and_clamps_at_zero()
    {
        float levelOnePenalty = ChemotacticBeaconHelper.GetNonTargetPenalty(level: 1);
        float levelSixPenalty = ChemotacticBeaconHelper.GetNonTargetPenalty(level: 6);

        Assert.Equal(0.05f, levelOnePenalty, precision: 3);
        Assert.Equal(0f, levelSixPenalty, precision: 3);
    }

    [Fact]
    public void DoesMoveStayAsCloseOrCloserToMarker_returns_true_for_neutral_distance()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(2, 1));
        var markerTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));

        bool result = ChemotacticBeaconHelper.DoesMoveStayAsCloseOrCloserToMarker(sourceTile, targetTile, board, markerTile.TileId);

        Assert.True(result);
    }

    [Fact]
    public void DoesMoveStayAsCloseOrCloserToMarker_returns_false_for_farther_distance()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var sourceTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));
        var targetTile = Assert.IsType<BoardTile>(board.GetTile(0, 2));
        var markerTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));

        bool result = ChemotacticBeaconHelper.DoesMoveStayAsCloseOrCloserToMarker(sourceTile, targetTile, board, markerTile.TileId);

        Assert.False(result);
    }
}