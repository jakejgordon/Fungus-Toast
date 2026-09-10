using FungusToast.Simulation.Export;

namespace FungusToast.Simulation.Analysis;

public sealed record DirectMatchStrategySummary(
    string StrategyName,
    int Games,
    double WinCredit,
    double WinRate,
    double MeanNormalizedBoardShare);

public static class DirectMatchSummary
{
    public static IReadOnlyList<DirectMatchStrategySummary> Build(IEnumerable<PlayerExportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows
            .GroupBy(row => row.StrategyName, StringComparer.Ordinal)
            .Select(group => new DirectMatchStrategySummary(
                group.Key,
                group.Count(),
                group.Sum(row => row.WinCredit),
                group.Average(row => row.WinCredit),
                group.Average(row => row.TotalLivingCells > 0
                    ? row.LivingCells * (double)row.PlayerCount / row.TotalLivingCells
                    : 0)))
            .OrderByDescending(summary => summary.WinCredit)
            .ThenBy(summary => summary.StrategyName, StringComparer.Ordinal)
            .ToList();
    }
}
