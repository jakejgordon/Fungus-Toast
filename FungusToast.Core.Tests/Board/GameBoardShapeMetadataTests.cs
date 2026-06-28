using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class GameBoardShapeMetadataTests
{
    [Fact]
    public void Constructor_builds_playable_edge_distance_and_corner_metadata_from_irregular_block_mask()
    {
        var board = new GameBoard(
            width: 5,
            height: 5,
            playerCount: 1,
            permanentlyBlockedTileIds: new[] { 0, 1, 4, 20, 24 });

        Assert.True(board.IsPlayableEdgeTile(6));
        Assert.False(board.IsPlayableEdgeTile(12));

        Assert.True(board.IsWithinPlayableEdgeDistance(11, 2));
        Assert.False(board.IsWithinPlayableEdgeDistance(11, 1));
        Assert.Equal(2, board.GetPlayableEdgeDistance(11));

        Assert.Equal(5, board.GetCornerTileId(GameBoard.BoardCorner.TopLeft));
        Assert.Equal(3, board.GetCornerTileId(GameBoard.BoardCorner.TopRight));
        Assert.Equal(19, board.GetCornerTileId(GameBoard.BoardCorner.BottomRight));
        Assert.Equal(15, board.GetCornerTileId(GameBoard.BoardCorner.BottomLeft));
    }
}
