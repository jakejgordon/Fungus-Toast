using FungusToast.Core.AI;
using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class StrategyBandClassifierTests
{
    /// <summary>
    /// Placement uses the interval bound that argues against the band, so a noisy strategy is never
    /// promoted into a band its evidence cannot support.
    /// </summary>
    [Fact]
    public void ANoisyStrategy_IsNotPromotedOnItsPointEstimate()
    {
        // Point estimate well into Elite territory, but the interval reaches down past parity.
        var (noisyBand, noisyEvidence) = StrategyBandClassifier.ClassifyMeasurement(games: 40, ci95Low: 0.95, ci95High: 1.50);
        // A steadier strategy with a lower estimate but a bound that clears the line.
        var (steadyBand, _) = StrategyBandClassifier.ClassifyMeasurement(games: 40, ci95Low: 1.30, ci95High: 1.40);

        Assert.Equal(BandEvidence.Sufficient, noisyEvidence);
        Assert.Equal(DifficultyBand.Normal, noisyBand);
        Assert.Equal(DifficultyBand.Elite, steadyBand);
    }

    [Theory]
    [InlineData(1.30, 1.45, DifficultyBand.Elite)]
    [InlineData(1.10, 1.20, DifficultyBand.Hard)]
    [InlineData(0.98, 1.03, DifficultyBand.Normal)]
    [InlineData(0.60, 0.80, DifficultyBand.Easy)]
    public void BandsAreMeasuredAsDistanceFromParity(double low, double high, DifficultyBand expected)
    {
        var (band, evidence) = StrategyBandClassifier.ClassifyMeasurement(games: 40, ci95Low: low, ci95High: high);

        Assert.Equal(BandEvidence.Sufficient, evidence);
        Assert.Equal(expected, band);
    }

    /// <summary>
    /// P7.4 requires flagging thin evidence rather than guessing, so both failure modes report
    /// themselves instead of producing a band.
    /// </summary>
    [Fact]
    public void ThinEvidenceIsReported_NotGuessed()
    {
        var tooFew = StrategyBandClassifier.ClassifyMeasurement(
            games: StrategyBandClassifier.MinimumGamesForBand - 1, ci95Low: 1.30, ci95High: 1.40);
        Assert.Equal(BandEvidence.TooFewGames, tooFew.Evidence);
        Assert.Null(tooFew.Band);

        var tooWide = StrategyBandClassifier.ClassifyMeasurement(games: 40, ci95Low: 0.60, ci95High: 1.60);
        Assert.Equal(BandEvidence.IntervalTooWide, tooWide.Evidence);
        Assert.Null(tooWide.Band);

        var unmeasured = StrategyBandClassifier.ClassifyMeasurement(games: 0, ci95Low: 0, ci95High: 0);
        Assert.Equal(BandEvidence.NotMeasured, unmeasured.Evidence);
    }

    /// <summary>
    /// A holdout exists to check a figure fitted on the calibration contexts, so folding it into
    /// that figure would have the check marking its own work.
    /// </summary>
    [Fact]
    public void HoldoutContextsAreReported_ButNeverPooledIntoTheOverallBand()
    {
        var result = StrategyBandClassifier.Classify("Panelist", new[]
        {
            Measure("calib.a", games: 40, share: 1.40, low: 1.30, high: 1.50),
            Measure("calib.b", games: 40, share: 1.40, low: 1.30, high: 1.50),
            // A holdout that flatly disagrees; it must not drag the pooled figure.
            Measure("hold.a", games: 40, share: 0.50, low: 0.40, high: 0.60, isHoldout: true)
        });

        Assert.Equal(DifficultyBand.Elite, result.OverallBand);
        Assert.Equal(80, result.TotalGames);
        Assert.Equal(1.40, result.PooledNormalizedBoardShare, 3);
        // But the disagreement is visible rather than hidden.
        Assert.Contains(result.ContextBands, band => band.IsHoldout && band.Band == DifficultyBand.Easy);
        Assert.Contains("hold.a", result.MaterialContexts);
    }

    /// <summary>
    /// A strategy whose band changes with the context is a contextual specialist, and a single
    /// global label would hide exactly the thing worth knowing about it.
    /// </summary>
    [Fact]
    public void AStrategyThatChangesBandByContext_IsFlaggedAsAContextualSpecialist()
    {
        var result = StrategyBandClassifier.Classify("Specialist", new[]
        {
            Measure("duel.small", games: 40, share: 0.60, low: 0.50, high: 0.70),
            Measure("crowded.large", games: 40, share: 1.45, low: 1.35, high: 1.55)
        });

        Assert.True(result.IsContextualSpecialist);
        Assert.NotEmpty(result.MaterialContexts);
    }

    [Fact]
    public void AConsistentStrategy_HasNoMaterialContexts()
    {
        var result = StrategyBandClassifier.Classify("Consistent", new[]
        {
            Measure("duel.small", games: 40, share: 1.12, low: 1.08, high: 1.16),
            Measure("crowded.large", games: 40, share: 1.14, low: 1.10, high: 1.18)
        });

        Assert.Equal(DifficultyBand.Hard, result.OverallBand);
        Assert.False(result.IsContextualSpecialist);
        Assert.Empty(result.MaterialContexts);
    }

    [Fact]
    public void PoolingWeightsContextsByTheGamesTheyContributed()
    {
        var result = StrategyBandClassifier.Classify("Weighted", new[]
        {
            Measure("a", games: 90, share: 1.00, low: 0.95, high: 1.05),
            Measure("b", games: 10, share: 2.00, low: 1.95, high: 2.05)
        });

        // 90 games at 1.00 and 10 at 2.00 pool to 1.10, not the unweighted 1.50.
        Assert.Equal(1.10, result.PooledNormalizedBoardShare, 3);
        Assert.Equal(100, result.TotalGames);
    }

    [Fact]
    public void AStrategyMeasuredOnlyInHoldouts_HasNoOverallBand()
    {
        var result = StrategyBandClassifier.Classify("HoldoutOnly", new[]
        {
            Measure("hold.a", games: 40, share: 1.40, low: 1.30, high: 1.50, isHoldout: true)
        });

        Assert.Null(result.OverallBand);
        Assert.Equal(BandEvidence.NotMeasured, result.OverallEvidence);
        Assert.Equal(0, result.TotalGames);
    }

    [Fact]
    public void ClassifyAll_GroupsByStrategyAndOrdersByPooledShare()
    {
        var results = StrategyBandClassifier.ClassifyAll(new[]
        {
            Measure("a", games: 40, share: 0.50, low: 0.40, high: 0.60, strategyName: "Weak"),
            Measure("a", games: 40, share: 1.40, low: 1.30, high: 1.50, strategyName: "Strong"),
            Measure("b", games: 40, share: 1.42, low: 1.32, high: 1.52, strategyName: "Strong")
        });

        Assert.Equal(new[] { "Strong", "Weak" }, results.Select(result => result.StrategyName));
        Assert.Equal(80, results[0].TotalGames);
    }

    /// <summary>
    /// The version travels with a classification so a band always names the rules that produced it.
    /// </summary>
    [Fact]
    public void TheClassifierIsVersioned()
        => Assert.Equal("fungus-toast.ai-bands.v1", StrategyBandClassifier.ClassifierVersion);

    private static StrategyContextMeasurement Measure(
        string contextId,
        int games,
        double share,
        double low,
        double high,
        bool isHoldout = false,
        string strategyName = "Panelist")
        => new(strategyName, contextId, isHoldout, games, share, low, high);
}
