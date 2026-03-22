using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class StartingSporeReferenceLayoutTests
{
    public static TheoryData<int, (int x, int y)[]> ReferenceLayoutData => new()
    {
        { 2, new[] { (128, 80), (32, 80) } },
        { 3, new[] { (141, 80), (50, 133), (50, 27) } },
        { 4, new[] { (128, 128), (32, 128), (32, 32), (128, 32) } },
        { 5, new[] { (114, 104), (67, 120), (38, 80), (67, 40), (114, 56) } },
        { 6, new[] { (136, 95), (92, 126), (37, 123), (24, 65), (68, 34), (123, 37) } },
        { 7, new[] { (139, 94), (106, 135), (54, 135), (21, 94), (32, 42), (80, 19), (128, 42) } },
        { 8, new[] { (142, 106), (106, 142), (54, 142), (18, 106), (18, 54), (54, 18), (106, 18), (142, 54) } },
    };

    [Theory]
    [MemberData(nameof(ReferenceLayoutData))]
    public void GetStartingPositions_on_reference_board_returns_the_documented_precomputed_layout(int playerCount, (int x, int y)[] expectedPositions)
    {
        var positions = StartingSporeUtility.GetStartingPositions(boardWidth: 160, boardHeight: 160, playerCount);

        Assert.Equal(expectedPositions, positions);
    }
}
