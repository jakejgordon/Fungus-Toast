namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Builds the durable regression-comparison input from one completed calibration pass. Analyzer
/// artifacts remain the source for observations; this only joins them back to the frozen run.
/// </summary>
public static class StrategyRegressionSnapshotBuilder
{
    public static StrategyRegressionSnapshot Build(
        CalibrationRunState state,
        string exportRoot,
        string snapshotId,
        DateTime createdUtc,
        out IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(exportRoot)) throw new ArgumentException("Export root is required.", nameof(exportRoot));
        if (string.IsNullOrWhiteSpace(snapshotId)) throw new ArgumentException("Snapshot ID is required.", nameof(snapshotId));

        var measurements = CalibrationMeasurementReader.ReadAll(state, exportRoot, out var measurementWarnings);
        var executionHealth = CalibrationMeasurementReader.ReadExecutionHealth(state, exportRoot, out var healthWarnings);
        var categoryProfiles = CalibrationMeasurementReader.ReadCategoryProfiles(state, exportRoot, out var profileWarnings);
        warnings = measurementWarnings.Concat(healthWarnings).Concat(profileWarnings).Distinct(StringComparer.Ordinal).ToList();

        return new StrategyRegressionSnapshot
        {
            SchemaVersion = StrategyRegressionSnapshot.CurrentSchemaVersion,
            SnapshotId = snapshotId,
            MatrixId = state.MatrixId,
            CreatedUtc = createdUtc.ToUniversalTime(),
            ClassifierVersion = StrategyBandClassifier.ClassifierVersion,
            Bands = StrategyBandClassifier.ClassifyAll(measurements),
            CategoryProfiles = categoryProfiles,
            ExecutionHealth = executionHealth
        };
    }
}
