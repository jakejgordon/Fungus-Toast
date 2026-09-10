using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class CalibrationReplayVerifierTests
{
    [Fact]
    public void Verify_PersistsSuccessAndFailureWithoutAbandoningLaterConditions()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fungus-toast-replay-{Guid.NewGuid():N}");
        try
        {
            foreach (var id in new[] { "first", "second" })
            {
                Directory.CreateDirectory(Path.Combine(root, id));
                File.WriteAllText(Path.Combine(root, id, "resolved-manifest.json"), "{}");
            }
            var state = new CalibrationRunState { SchemaVersion = CalibrationRunState.CurrentSchemaVersion, MatrixId = "matrix", Conditions = new List<CalibrationConditionRecord> { Complete("first"), Complete("second") } };

            var results = CalibrationReplayVerifier.Verify(state, root, (path, _) =>
            {
                if (path.Contains("first", StringComparison.Ordinal)) throw new InvalidOperationException("mismatch");
            });

            Assert.Equal(2, results.Count);
            Assert.False(results.Single(result => result.ExperimentId == "first").Passed);
            Assert.True(results.Single(result => result.ExperimentId == "second").Passed);
            Assert.Equal(2, CalibrationReplayVerifier.Load(Path.Combine(root, CalibrationReplayVerifier.ResultsFileName)).Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private static CalibrationConditionRecord Complete(string id) => new() { ExperimentId = id, ContextId = "context", RepeatIndex = 0, IsHoldout = false, Status = CalibrationConditionStatus.Complete };
}
