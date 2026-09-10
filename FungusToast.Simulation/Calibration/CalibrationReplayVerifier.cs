using System.Text.Json;
using FungusToast.Simulation.Experiments;

namespace FungusToast.Simulation.Calibration;

public sealed record CalibrationReplayResult(string ExperimentId, bool Passed, string Detail, DateTime VerifiedUtc);

/// <summary>Durable, per-condition replay evidence for one calibration run.</summary>
public static class CalibrationReplayVerifier
{
    public const string ResultsFileName = "calibration-replay-parity.json";

    public static IReadOnlyList<CalibrationReplayResult> Verify(
        CalibrationRunState state,
        string exportRoot,
        Action<string, string?> replay,
        string? resultsPath = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(replay);
        if (string.IsNullOrWhiteSpace(exportRoot)) throw new ArgumentException("Export root is required.", nameof(exportRoot));

        resultsPath ??= Path.Combine(exportRoot, ResultsFileName);
        var results = Load(resultsPath).ToDictionary(result => result.ExperimentId, StringComparer.Ordinal);
        foreach (var condition in state.Conditions.Where(condition => condition.Status == CalibrationConditionStatus.Complete))
        {
            var manifestPath = Path.Combine(exportRoot, condition.ExperimentId, "resolved-manifest.json");
            try
            {
                if (!File.Exists(manifestPath)) throw new FileNotFoundException("Resolved manifest was not found.", manifestPath);
                replay(manifestPath, $"{condition.ExperimentId}__parity");
                results[condition.ExperimentId] = new CalibrationReplayResult(condition.ExperimentId, true, "Replay outcome verified.", DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                results[condition.ExperimentId] = new CalibrationReplayResult(condition.ExperimentId, false, exception.Message, DateTime.UtcNow);
            }

            Save(results.Values, resultsPath);
        }

        return results.Values.OrderBy(result => result.ExperimentId, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<CalibrationReplayResult> Load(string path)
        => !File.Exists(path)
            ? Array.Empty<CalibrationReplayResult>()
            : JsonSerializer.Deserialize<List<CalibrationReplayResult>>(File.ReadAllText(path))
              ?? throw new JsonException("Calibration replay results must contain an array.");

    private static void Save(IEnumerable<CalibrationReplayResult> results, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(results.OrderBy(result => result.ExperimentId), new JsonSerializerOptions { WriteIndented = true }));
    }
}
