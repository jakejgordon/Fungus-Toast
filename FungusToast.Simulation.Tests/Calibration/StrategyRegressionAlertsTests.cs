using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class StrategyRegressionAlertsTests
{
    [Fact]
    public void Compare_ReportsTheFiveRegressionClasses_WhenEvidenceSupportsThem()
    {
        var baseline = Snapshot(Band("Alpha", DifficultyBand.Hard, contexts: new[] { "duel" }), health: Health("Alpha", 100, 10, 0, 0), profile: Profile((MutationCategory.Growth, 10)));
        var current = Snapshot(Band("Alpha", DifficultyBand.Easy, contexts: new[] { "crowded" }), health: Health("Alpha", 100, 20, 8, 1), profile: Profile((MutationCategory.Fungicide, 10)));

        var alerts = StrategyRegressionAlerts.Compare(baseline, current);

        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.BandMoved);
        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.RobustnessLost);
        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.ArchetypeDrift);
        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.ReplayParityFailure);
        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.AbnormalFallbackRate);
        Assert.Contains(alerts, alert => alert.Kind == StrategyRegressionAlertKind.AbnormalDecisionFailureRate);
    }

    [Fact]
    public void Compare_ReportsMissingTelemetry_AsAnEvidenceGap()
    {
        var baseline = Snapshot(Band("Alpha", DifficultyBand.Normal));
        var current = Snapshot(Band("Alpha", DifficultyBand.Normal));

        var alerts = StrategyRegressionAlerts.Compare(baseline, current);

        Assert.Equal(2, alerts.Count(alert => alert.Kind == StrategyRegressionAlertKind.EvidenceGap));
    }

    [Fact]
    public void Compare_RefusesToCompareAcrossClassifierVersions()
    {
        var baseline = Snapshot(Band("Alpha", DifficultyBand.Normal), version: "v1");
        var current = Snapshot(Band("Alpha", DifficultyBand.Easy), version: "v2");

        var alert = Assert.Single(StrategyRegressionAlerts.Compare(baseline, current));

        Assert.Equal(StrategyRegressionAlertKind.ClassifierVersionChanged, alert.Kind);
    }

    private static StrategyRegressionSnapshot Snapshot(StrategyBandResult band, StrategyExecutionHealth? health = null,
        IReadOnlyDictionary<MutationCategory, int>? profile = null, string version = "fungus-toast.ai-bands.v1")
        => new()
        {
            ClassifierVersion = version,
            Bands = new[] { band },
            CategoryProfiles = profile == null
                ? new Dictionary<string, IReadOnlyDictionary<MutationCategory, int>>()
                : new Dictionary<string, IReadOnlyDictionary<MutationCategory, int>> { [band.StrategyName] = profile },
            ExecutionHealth = health == null
                ? new Dictionary<string, StrategyExecutionHealth>()
                : new Dictionary<string, StrategyExecutionHealth> { [band.StrategyName] = health }
        };

    private static StrategyBandResult Band(string name, DifficultyBand band, IReadOnlyList<string>? contexts = null)
        => new()
        {
            StrategyName = name,
            OverallBand = band,
            OverallEvidence = BandEvidence.Sufficient,
            PooledNormalizedBoardShare = 1,
            TotalGames = 100,
            ContextBands = Array.Empty<ContextBand>(),
            MaterialContexts = contexts ?? Array.Empty<string>()
        };

    private static StrategyExecutionHealth Health(string name, int decisions, int fallback, int failures, int parity)
        => new(name, decisions, fallback, failures, parity);

    private static IReadOnlyDictionary<MutationCategory, int> Profile(params (MutationCategory Category, int Levels)[] values)
        => values.ToDictionary(value => value.Category, value => value.Levels);
}
