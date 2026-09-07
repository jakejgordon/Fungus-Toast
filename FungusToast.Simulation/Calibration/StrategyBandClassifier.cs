using FungusToast.Core.AI;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// One strategy's measurement in one context, as the offline analyzer reports it.
/// </summary>
public sealed record StrategyContextMeasurement(
    string StrategyName,
    string ContextId,
    bool IsHoldout,
    int Games,
    double NormalizedBoardShare,
    double Ci95Low,
    double Ci95High);

public enum BandEvidence
{
    /// <summary>Enough games and a tight enough interval to place the strategy.</summary>
    Sufficient,

    /// <summary>Measured, but on too few games to place.</summary>
    TooFewGames,

    /// <summary>Measured on enough games, but the interval spans too many bands to choose one.</summary>
    IntervalTooWide,

    /// <summary>Never measured in this context.</summary>
    NotMeasured
}

public sealed record ContextBand(
    string ContextId,
    bool IsHoldout,
    DifficultyBand? Band,
    BandEvidence Evidence,
    int Games,
    double NormalizedBoardShare,
    double Ci95Low,
    double Ci95High);

public sealed class StrategyBandResult
{
    public required string StrategyName { get; init; }

    /// <summary>Null when the pooled evidence cannot place the strategy.</summary>
    public required DifficultyBand? OverallBand { get; init; }

    public required BandEvidence OverallEvidence { get; init; }

    /// <summary>Pooled conservative bound across calibration contexts, weighted by games.</summary>
    public required double PooledNormalizedBoardShare { get; init; }

    public required int TotalGames { get; init; }

    public required IReadOnlyList<ContextBand> ContextBands { get; init; }

    /// <summary>
    /// Contexts whose band differs from the overall one. A strategy with material contexts is a
    /// contextual specialist, and reporting only its overall band would hide that.
    /// </summary>
    public required IReadOnlyList<string> MaterialContexts { get; init; }

    public bool IsContextualSpecialist => MaterialContexts.Count > 0;
}

/// <summary>
/// Places strategies into difficulty bands from measured evidence.
///
/// **Thresholds are parity-relative and frozen, not fitted.** Normalized board share is already
/// parity-normalized: 1.0 means a strategy took exactly its equal share for the player count. The
/// boundaries below are stated as distances from parity and were chosen before the reference matrix
/// finished running, precisely so they could not be drawn around the gaps that happened to appear
/// in the data. Redrawing them afterwards is how a contextual specialist gets relabelled a
/// generalist, and how a disappointing result becomes a passing one.
///
/// Placement uses the *conservative* end of the interval rather than the point estimate, so a noisy
/// strategy is never promoted into a band its evidence cannot support. Insufficient evidence is
/// reported as such rather than guessed, which is what P7.4 requires.
/// </summary>
public static class StrategyBandClassifier
{
    /// <summary>Bump when a threshold or rule changes, so a classification always names the rules that produced it.</summary>
    public const string ClassifierVersion = "fungus-toast.ai-bands.v1";

    /// <summary>Confidently a quarter above parity or better.</summary>
    public const double EliteLowerBound = 1.25;

    /// <summary>Confidently above parity, allowing a small band of noise around it.</summary>
    public const double HardLowerBound = 1.05;

    /// <summary>Confidently below parity by the same margin.</summary>
    public const double EasyUpperBound = 0.95;

    /// <summary>Fewer games than this cannot place a strategy, however tight the interval looks.</summary>
    public const int MinimumGamesForBand = 25;

    /// <summary>
    /// An interval wider than this spans too much of the scale to choose a band. It is generous:
    /// the point is to catch evidence that is nearly useless, not to demand precision the game's
    /// variance cannot deliver.
    /// </summary>
    public const double MaximumIntervalWidth = 0.60;

    /// <summary>
    /// Classifies one measurement. Placement is by the interval bound that argues *against* the
    /// band: a strategy is Elite only if even its pessimistic bound clears the line.
    /// </summary>
    public static (DifficultyBand? Band, BandEvidence Evidence) ClassifyMeasurement(
        int games,
        double ci95Low,
        double ci95High)
    {
        if (games <= 0) return (null, BandEvidence.NotMeasured);
        if (games < MinimumGamesForBand) return (null, BandEvidence.TooFewGames);
        if (ci95High - ci95Low > MaximumIntervalWidth) return (null, BandEvidence.IntervalTooWide);

        if (ci95Low > EliteLowerBound) return (DifficultyBand.Elite, BandEvidence.Sufficient);
        if (ci95Low > HardLowerBound) return (DifficultyBand.Hard, BandEvidence.Sufficient);
        if (ci95High < EasyUpperBound) return (DifficultyBand.Easy, BandEvidence.Sufficient);
        return (DifficultyBand.Normal, BandEvidence.Sufficient);
    }

    /// <summary>
    /// Classifies a strategy overall and per context.
    ///
    /// The overall band pools calibration contexts only. Holdout contexts are measured and reported
    /// but never fold into the figure they exist to check - otherwise the check is being marked by
    /// the thing it is checking.
    /// </summary>
    public static StrategyBandResult Classify(
        string strategyName,
        IReadOnlyList<StrategyContextMeasurement> measurements)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
            throw new ArgumentException("Strategy name is required.", nameof(strategyName));
        ArgumentNullException.ThrowIfNull(measurements);

        var contextBands = measurements
            .Select(measurement =>
            {
                var (band, evidence) = ClassifyMeasurement(measurement.Games, measurement.Ci95Low, measurement.Ci95High);
                return new ContextBand(
                    measurement.ContextId,
                    measurement.IsHoldout,
                    band,
                    evidence,
                    measurement.Games,
                    measurement.NormalizedBoardShare,
                    measurement.Ci95Low,
                    measurement.Ci95High);
            })
            .OrderBy(band => band.IsHoldout)
            .ThenBy(band => band.ContextId, StringComparer.Ordinal)
            .ToList();

        var calibration = measurements.Where(measurement => !measurement.IsHoldout).ToList();
        var totalGames = calibration.Sum(measurement => measurement.Games);
        if (totalGames == 0)
        {
            return new StrategyBandResult
            {
                StrategyName = strategyName,
                OverallBand = null,
                OverallEvidence = BandEvidence.NotMeasured,
                PooledNormalizedBoardShare = 0,
                TotalGames = 0,
                ContextBands = contextBands,
                MaterialContexts = Array.Empty<string>()
            };
        }

        // Pooling weights by games, so a context that contributed more evidence counts for more.
        //
        // The interval is pooled the same way, which deliberately does not narrow it. Inverse-
        // variance pooling would give a tighter interval, but it assumes every context measures one
        // underlying quantity - and the whole premise of a contextual matrix is that a strategy's
        // strength genuinely differs between a duel and a crowded table. Treating that variation as
        // if it were noise to be averaged away would report more confidence than the design earns.
        // Averaging the bounds keeps roughly the typical context's width, which errs toward
        // under-placing a strategy rather than over-placing it, and heterogeneity is surfaced
        // separately as material contexts rather than hidden inside one number.
        var pooledShare = calibration.Sum(m => m.NormalizedBoardShare * m.Games) / totalGames;
        var pooledLow = calibration.Sum(m => m.Ci95Low * m.Games) / totalGames;
        var pooledHigh = calibration.Sum(m => m.Ci95High * m.Games) / totalGames;
        var (overallBand, overallEvidence) = ClassifyMeasurement(totalGames, pooledLow, pooledHigh);

        // A context whose band differs from the overall one is where a single global label would
        // mislead, so it is named rather than averaged away.
        var materialContexts = contextBands
            .Where(band => band.Evidence == BandEvidence.Sufficient && band.Band != overallBand)
            .Select(band => band.ContextId)
            .ToList();

        return new StrategyBandResult
        {
            StrategyName = strategyName,
            OverallBand = overallBand,
            OverallEvidence = overallEvidence,
            PooledNormalizedBoardShare = pooledShare,
            TotalGames = totalGames,
            ContextBands = contextBands,
            MaterialContexts = materialContexts
        };
    }

    /// <summary>Classifies every strategy present in the measurements.</summary>
    public static IReadOnlyList<StrategyBandResult> ClassifyAll(
        IReadOnlyList<StrategyContextMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        return measurements
            .GroupBy(measurement => measurement.StrategyName, StringComparer.Ordinal)
            .Select(group => Classify(group.Key, group.ToList()))
            .OrderByDescending(result => result.PooledNormalizedBoardShare)
            .ToList();
    }
}
