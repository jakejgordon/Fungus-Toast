using FungusToast.Core.Board;

namespace FungusToast.Core.Tests.Board;

public class PlayerBoardSummaryTests
{
    [Fact]
    public void Constructor_sets_all_summary_fields()
    {
        var summary = new PlayerBoardSummary(playerId: 7, livingCells: 11, resistantCells: 4, deadCells: 3, toxinCells: 5);

        Assert.Equal(7, summary.PlayerId);
        Assert.Equal(11, summary.LivingCells);
        Assert.Equal(4, summary.ResistantCells);
        Assert.Equal(3, summary.DeadCells);
        Assert.Equal(5, summary.ToxinCells);
    }
}
