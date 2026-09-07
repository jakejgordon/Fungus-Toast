using System.Globalization;
using System.Text;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Renders classified bands for human review, and surfaces the two things a table of numbers
/// hides: where a measured band contradicts the strategy's authored metadata, and where a single
/// global label would misrepresent a contextual specialist.
/// </summary>
public static class StrategyBandReport
{
    public static string Render(
        CalibrationMatrix matrix,
        IReadOnlyList<StrategyBandResult> results,
        IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(results);

        var builder = new StringBuilder();
        builder.AppendLine($"# Measured performance bands — {matrix.MatrixId}");
        builder.AppendLine();
        builder.AppendLine($"- Classifier: `{StrategyBandClassifier.ClassifierVersion}`");
        builder.AppendLine($"- Panel: {matrix.StrategySet} ({results.Count} strategies)");
        builder.AppendLine($"- Bands are distances from parity: Elite above `{StrategyBandClassifier.EliteLowerBound:0.00}`, "
            + $"Hard above `{StrategyBandClassifier.HardLowerBound:0.00}`, Easy below `{StrategyBandClassifier.EasyUpperBound:0.00}`, "
            + "placed on the interval bound that argues against the band.");
        builder.AppendLine("- Thresholds were frozen before the matrix finished running, so they could not be drawn around the results.");
        builder.AppendLine();

        builder.AppendLine("## Overall");
        builder.AppendLine();
        builder.AppendLine("| Strategy | Band | Pooled share | Games | Evidence | Contextual |");
        builder.AppendLine("|---|---|---:|---:|---|---|");
        foreach (var result in results)
        {
            builder.AppendLine(
                $"| {result.StrategyName} "
                + $"| {DescribeBand(result.OverallBand)} "
                + $"| {result.PooledNormalizedBoardShare.ToString("0.000", CultureInfo.InvariantCulture)} "
                + $"| {result.TotalGames} "
                + $"| {result.OverallEvidence} "
                + $"| {(result.IsContextualSpecialist ? string.Join(", ", result.MaterialContexts) : "—")} |");
        }

        AppendLabelMismatches(builder, matrix, results);
        AppendContextDetail(builder, results);

        builder.AppendLine();
        builder.AppendLine("## Evidence gaps");
        builder.AppendLine();
        var gaps = results.Where(result => result.OverallEvidence != BandEvidence.Sufficient).ToList();
        // Per-context gaps matter even when the pooled figure is sound: a strategy can be placeable
        // overall while one context contributed too little to say anything about it, and a
        // contextual claim about that context would then be unsupported.
        var contextGaps = results
            .SelectMany(result => result.ContextBands
                .Where(band => band.Evidence != BandEvidence.Sufficient)
                .Select(band => $"- {result.StrategyName} in {band.ContextId}: {band.Evidence} on {band.Games} games."))
            .ToList();

        if (gaps.Count == 0 && contextGaps.Count == 0 && warnings.Count == 0)
        {
            builder.AppendLine("None. Every strategy has enough evidence to place, in every context.");
        }
        else
        {
            foreach (var gap in gaps)
                builder.AppendLine($"- {gap.StrategyName}: {gap.OverallEvidence} on {gap.TotalGames} games overall.");
            foreach (var contextGap in contextGaps)
                builder.AppendLine(contextGap);
            foreach (var warning in warnings)
                builder.AppendLine($"- {warning}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Where measurement disagrees with the authored power tier. This is the point of calibrating
    /// at all: a label nobody measured is a guess, and a boss that loses is a broken promise to the
    /// player rather than a rounding error.
    /// </summary>
    private static void AppendLabelMismatches(
        StringBuilder builder,
        CalibrationMatrix matrix,
        IReadOnlyList<StrategyBandResult> results)
    {
        var mismatches = new List<string>();
        foreach (var result in results)
        {
            if (result.OverallBand is not { } band) continue;
            var entry = AIRoster.GetStrategyCatalogEntry(matrix.StrategySet, result.StrategyName);
            if (entry == null) continue;

            var expected = ExpectedBandForTier(entry.PowerTier);
            if (expected == null || expected == band) continue;

            mismatches.Add(
                $"- **{result.StrategyName}** is authored `{entry.PowerTier}`"
                + $"{(entry.Role == StrategyRole.Boss ? " and used as a **Boss**" : string.Empty)}"
                + $", but measures **{band}** at {result.PooledNormalizedBoardShare.ToString("0.000", CultureInfo.InvariantCulture)}.");
        }

        builder.AppendLine();
        builder.AppendLine("## Authored label vs measurement");
        builder.AppendLine();
        if (mismatches.Count == 0) builder.AppendLine("No strategy contradicts its authored power tier.");
        else foreach (var mismatch in mismatches) builder.AppendLine(mismatch);
    }

    private static void AppendContextDetail(StringBuilder builder, IReadOnlyList<StrategyBandResult> results)
    {
        builder.AppendLine();
        builder.AppendLine("## By context");
        builder.AppendLine();
        builder.AppendLine("Holdout contexts are measured and shown but never pooled into the overall band, "
            + "since they exist to check it.");
        builder.AppendLine();
        builder.AppendLine("| Strategy | Context | Band | Share | 95% CI | Games |");
        builder.AppendLine("|---|---|---|---:|---|---:|");
        foreach (var result in results)
        foreach (var context in result.ContextBands)
        {
            builder.AppendLine(
                $"| {result.StrategyName} "
                + $"| {context.ContextId}{(context.IsHoldout ? " *(holdout)*" : string.Empty)} "
                + $"| {DescribeBand(context.Band)} "
                + $"| {context.NormalizedBoardShare.ToString("0.000", CultureInfo.InvariantCulture)} "
                + $"| {context.Ci95Low.ToString("0.000", CultureInfo.InvariantCulture)}–{context.Ci95High.ToString("0.000", CultureInfo.InvariantCulture)} "
                + $"| {context.Games} |");
        }
    }

    /// <summary>
    /// The band an authored power tier implies. Standard covers the middle, so it is compatible
    /// with either side of parity and never counted as a contradiction.
    /// </summary>
    private static DifficultyBand? ExpectedBandForTier(StrategyPowerTier tier) => tier switch
    {
        StrategyPowerTier.Weak => DifficultyBand.Easy,
        StrategyPowerTier.Strong => DifficultyBand.Hard,
        StrategyPowerTier.Spike => DifficultyBand.Hard,
        _ => null
    };

    private static string DescribeBand(DifficultyBand? band) => band?.ToString() ?? "—";
}
