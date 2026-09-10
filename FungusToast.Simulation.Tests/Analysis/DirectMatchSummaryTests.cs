using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Export;
using Xunit;

namespace FungusToast.Simulation.Tests.Analysis;

public sealed class DirectMatchSummaryTests
{
    [Fact]
    public void Build_UsesFractionalCreditAndNormalizesBoardShare()
    {
        var rows = new[]
        {
            Row("alpha", credit: 1, living: 75, total: 100),
            Row("alpha", credit: 0.5, living: 50, total: 100),
            Row("bravo", credit: 0, living: 25, total: 100),
            Row("bravo", credit: 0.5, living: 50, total: 100)
        };

        var summaries = DirectMatchSummary.Build(rows);

        var alpha = summaries[0];
        Assert.Equal("alpha", alpha.StrategyName);
        Assert.Equal(1.5, alpha.WinCredit);
        Assert.Equal(0.75, alpha.WinRate);
        Assert.Equal(1.25, alpha.MeanNormalizedBoardShare);
    }

    private static PlayerExportRow Row(string name, double credit, int living, int total) => new()
    {
        StrategyName = name,
        WinCredit = credit,
        LivingCells = living,
        TotalLivingCells = total,
        PlayerCount = 2
    };
}
