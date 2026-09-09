using System.Globalization;
using System.Text;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Reads per-strategy measurements out of the offline analyzer's summaries and pairs them back to
/// the contexts that produced them.
///
/// The analyzer writes one summary per artifact and knows nothing about the matrix; the run state
/// knows which artifact came from which context. Joining them here is what turns a pile of
/// per-artifact CSVs into a contextual picture of one panel.
/// </summary>
public static class CalibrationMeasurementReader
{
    public const string PlayerSummaryFileName = "post_simulation_player_summary.csv";
    public const string ExecutionHealthFileName = "strategy_execution_health.csv";

    /// <summary>
    /// Collects every completed condition's measurements. Conditions that did not complete are
    /// skipped rather than treated as zeroes, because a missing run is missing evidence, not
    /// evidence of nothing.
    /// </summary>
    public static IReadOnlyList<StrategyContextMeasurement> ReadAll(
        CalibrationRunState state,
        string exportRoot,
        out IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(state);
        var measurements = new List<StrategyContextMeasurement>();
        var problems = new List<string>();

        foreach (var record in state.Conditions)
        {
            if (record.Status != CalibrationConditionStatus.Complete)
            {
                problems.Add($"{record.ExperimentId}: {record.Status}, so its context has no evidence from this run.");
                continue;
            }

            var path = Path.Combine(exportRoot, record.ExperimentId, PlayerSummaryFileName);
            if (!File.Exists(path))
            {
                problems.Add($"{record.ExperimentId}: no {PlayerSummaryFileName}; run the analyzer over its artifact.");
                continue;
            }

            measurements.AddRange(ReadSummary(path, record.ContextId, record.IsHoldout));
        }

        warnings = problems;
        return measurements;
    }

    /// <summary>
    /// Parses one analyzer summary. Columns are addressed by name rather than position, so a change
    /// to the analyzer's column order cannot silently shift which number is read as which.
    /// </summary>
    public static IReadOnlyList<StrategyContextMeasurement> ReadSummary(string path, string contextId, bool isHoldout)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return Array.Empty<StrategyContextMeasurement>();

        var headers = SplitCsvLine(lines[0]);
        var columns = headers
            .Select((header, index) => (header, index))
            .ToDictionary(entry => entry.header.Trim(), entry => entry.index, StringComparer.OrdinalIgnoreCase);

        foreach (var required in new[]
                 {
                     "player", "games", "avg_normalized_board_share",
                     "normalized_board_share_ci95_low", "normalized_board_share_ci95_high"
                 })
        {
            if (!columns.ContainsKey(required))
                throw new InvalidOperationException($"'{path}' is missing the '{required}' column.");
        }

        var measurements = new List<StrategyContextMeasurement>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = SplitCsvLine(line);

            var name = Value(values, columns, "player");
            if (string.IsNullOrWhiteSpace(name)) continue;

            measurements.Add(new StrategyContextMeasurement(
                name,
                contextId,
                isHoldout,
                (int)ParseDouble(values, columns, "games"),
                ParseDouble(values, columns, "avg_normalized_board_share"),
                ParseDouble(values, columns, "normalized_board_share_ci95_low"),
                ParseDouble(values, columns, "normalized_board_share_ci95_high")));
        }

        return measurements;
    }

    /// <summary>
    /// Collects the analyzer's decision-health rows across completed conditions. Missing files are
    /// reported as evidence gaps rather than converted to healthy zero-rate telemetry.
    /// </summary>
    public static IReadOnlyDictionary<string, StrategyExecutionHealth> ReadExecutionHealth(
        CalibrationRunState state,
        string exportRoot,
        out IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(state);
        var totals = new Dictionary<string, (int Decisions, int FallbackDecisions)>(StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (var record in state.Conditions)
        {
            if (record.Status != CalibrationConditionStatus.Complete)
            {
                problems.Add($"{record.ExperimentId}: {record.Status}, so its execution health is missing from this run.");
                continue;
            }

            var path = Path.Combine(exportRoot, record.ExperimentId, ExecutionHealthFileName);
            if (!File.Exists(path))
            {
                problems.Add($"{record.ExperimentId}: no {ExecutionHealthFileName}; run the analyzer over its artifact.");
                continue;
            }

            foreach (var health in ReadExecutionHealthSummary(path))
            {
                totals.TryGetValue(health.StrategyName, out var total);
                totals[health.StrategyName] = (
                    total.Decisions + health.Decisions,
                    total.FallbackDecisions + health.FallbackDecisions);
            }
        }

        warnings = problems;
        return totals.ToDictionary(
            entry => entry.Key,
            entry => new StrategyExecutionHealth(entry.Key, entry.Value.Decisions, entry.Value.FallbackDecisions, 0, 0),
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<StrategyExecutionHealth> ReadExecutionHealthSummary(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return Array.Empty<StrategyExecutionHealth>();

        var headers = SplitCsvLine(lines[0]);
        var columns = headers
            .Select((header, index) => (header, index))
            .ToDictionary(entry => entry.header.Trim(), entry => entry.index, StringComparer.OrdinalIgnoreCase);
        foreach (var required in new[] { "strategy_name", "decisions", "fallback_decisions" })
        {
            if (!columns.ContainsKey(required))
                throw new InvalidOperationException($"'{path}' is missing the '{required}' column.");
        }

        var health = new List<StrategyExecutionHealth>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = SplitCsvLine(line);
            var name = Value(values, columns, "strategy_name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            health.Add(new StrategyExecutionHealth(
                name,
                (int)ParseDouble(values, columns, "decisions"),
                (int)ParseDouble(values, columns, "fallback_decisions"),
                0,
                0));
        }

        return health;
    }

    private static string Value(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> columns, string column)
    {
        var index = columns[column];
        return index < values.Count ? values[index].Trim() : string.Empty;
    }

    private static double ParseDouble(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> columns, string column)
        => double.TryParse(Value(values, columns, column), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    /// <summary>
    /// Minimal RFC-4180 splitting. Strategy names are authored and can contain commas, so naive
    /// splitting would shift every column after the name.
    /// </summary>
    public static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (inQuotes)
            {
                if (character != '"') { current.Append(character); continue; }
                if (index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; continue; }
                inQuotes = false;
                continue;
            }

            switch (character)
            {
                case '"': inQuotes = true; break;
                case ',': values.Add(current.ToString()); current.Clear(); break;
                default: current.Append(character); break;
            }
        }

        values.Add(current.ToString());
        return values;
    }
}
