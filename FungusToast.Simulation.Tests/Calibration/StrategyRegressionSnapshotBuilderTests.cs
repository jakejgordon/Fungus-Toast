using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class StrategyRegressionSnapshotBuilderTests
{
    [Fact]
    public void Build_AggregatesAnalyzerHealthAcrossCompletedCalibrationArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fungus-toast-regression-{Guid.NewGuid():N}");
        try
        {
            WriteArtifact(root, "condition-a", games: 20, decisions: 12, fallbackDecisions: 2);
            WriteArtifact(root, "condition-b", games: 20, decisions: 8, fallbackDecisions: 1);
            var state = new CalibrationRunState
            {
                SchemaVersion = CalibrationRunState.CurrentSchemaVersion,
                MatrixId = "matrix.test",
                Conditions = new List<CalibrationConditionRecord>
                {
                    Completed("condition-a", "duel.small.square"),
                    Completed("condition-b", "duel.medium.square")
                }
            };

            var snapshot = StrategyRegressionSnapshotBuilder.Build(
                state, root, "snapshot.test", new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc), out var warnings);

            var health = Assert.Single(snapshot.ExecutionHealth);
            Assert.Equal("Alpha", health.Key);
            Assert.Equal(20, health.Value.Decisions);
            Assert.Equal(3, health.Value.FallbackDecisions);
            Assert.Empty(warnings);
            Assert.Equal("matrix.test", snapshot.MatrixId);
            Assert.Single(snapshot.Bands);
            Assert.Equal("Alpha", snapshot.Bands[0].StrategyName);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Build_LeavesMissingHealthAsAnEvidenceGap()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fungus-toast-regression-{Guid.NewGuid():N}");
        try
        {
            var artifact = Path.Combine(root, "condition-a");
            Directory.CreateDirectory(artifact);
            File.WriteAllText(Path.Combine(artifact, CalibrationMeasurementReader.PlayerSummaryFileName),
                "player,games,avg_normalized_board_share,normalized_board_share_ci95_low,normalized_board_share_ci95_high\nAlpha,20,1,0.9,1.1\n");
            var state = new CalibrationRunState
            {
                SchemaVersion = CalibrationRunState.CurrentSchemaVersion,
                MatrixId = "matrix.test",
                Conditions = new List<CalibrationConditionRecord> { Completed("condition-a", "duel.small.square") }
            };

            var snapshot = StrategyRegressionSnapshotBuilder.Build(
                state, root, "snapshot.test", DateTime.UtcNow, out var warnings);

            Assert.Empty(snapshot.ExecutionHealth);
            Assert.Contains(warnings, warning => warning.Contains(CalibrationMeasurementReader.ExecutionHealthFileName, StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static CalibrationConditionRecord Completed(string experimentId, string contextId) => new()
    {
        ExperimentId = experimentId,
        ContextId = contextId,
        IsHoldout = false,
        RepeatIndex = 0,
        Status = CalibrationConditionStatus.Complete
    };

    private static void WriteArtifact(string root, string experimentId, int games, int decisions, int fallbackDecisions)
    {
        var artifact = Path.Combine(root, experimentId);
        Directory.CreateDirectory(artifact);
        File.WriteAllText(Path.Combine(artifact, CalibrationMeasurementReader.PlayerSummaryFileName),
            $"player,games,avg_normalized_board_share,normalized_board_share_ci95_low,normalized_board_share_ci95_high\nAlpha,{games},1,0.9,1.1\n");
        File.WriteAllText(Path.Combine(artifact, CalibrationMeasurementReader.ExecutionHealthFileName),
            $"strategy_name,decisions,fallback_decisions\nAlpha,{decisions},{fallbackDecisions}\n");
    }
}
