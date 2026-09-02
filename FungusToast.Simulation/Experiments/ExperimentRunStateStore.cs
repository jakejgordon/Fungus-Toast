using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Experiments;

public sealed class ExperimentRunState
{
    public const string CurrentSchemaVersion = "fungus-toast.experiment-state.v1";

    public required string SchemaVersion { get; init; }
    public required string ExperimentId { get; init; }
    public required string ConditionId { get; init; }
    public required string ExecutionSha256 { get; init; }
    public required string Status { get; init; }
    public required DateTime UpdatedUtc { get; init; }
    public string ResolvedManifestSha256 { get; init; } = string.Empty;
    public string FailureType { get; init; } = string.Empty;
    public string FailureMessage { get; init; } = string.Empty;
    public string FailureStackTrace { get; init; } = string.Empty;
}

public static class ExperimentRunStateStore
{
    private const string ExportRootFolderName = "SimulationParquet";
    private const string StateFileName = "run-state.json";
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static string GetRunFolder(string experimentId) =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExportRootFolderName, experimentId);

    public static bool ShouldSkipCompleted(SimulationRunMetadata metadata)
    {
        var state = Read(metadata.ExperimentId);
        if (state == null || !string.Equals(state.Status, "complete", StringComparison.Ordinal)) return false;

        var expectedExecutionFingerprint = ExperimentFingerprint.ForExecution(metadata, CodeIdentityResolver.Resolve());
        if (!string.Equals(state.ExecutionSha256, expectedExecutionFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Completed artifact '{metadata.ExperimentId}' does not match the requested execution fingerprint; refusing to reuse it.");

        var resolvedManifestPath = Path.Combine(GetRunFolder(metadata.ExperimentId), "resolved-manifest.json");
        if (!File.Exists(resolvedManifestPath))
            throw new InvalidOperationException($"Completed artifact '{metadata.ExperimentId}' is missing resolved-manifest.json.");
        var actualManifestSha256 = ExperimentFingerprint.ForFile(resolvedManifestPath);
        if (!string.Equals(state.ResolvedManifestSha256, actualManifestSha256, StringComparison.Ordinal))
            throw new InvalidOperationException($"Completed artifact '{metadata.ExperimentId}' failed its recorded manifest checksum.");

        return true;
    }

    public static void MarkRunning(SimulationRunMetadata metadata) =>
        Write(metadata, "running");

    public static void MarkFinished(
        SimulationRunMetadata metadata,
        string completionStatus,
        string resolvedManifestSha256) =>
        Write(
            metadata,
            string.Equals(completionStatus, "complete", StringComparison.Ordinal) ? "complete" : "interrupted",
            resolvedManifestSha256: resolvedManifestSha256);

    public static void MarkFailed(SimulationRunMetadata metadata, Exception exception) =>
        Write(
            metadata,
            "failed",
            failureType: exception.GetType().FullName ?? exception.GetType().Name,
            failureMessage: exception.Message,
            failureStackTrace: exception.StackTrace ?? string.Empty);

    private static ExperimentRunState? Read(string experimentId)
    {
        var path = Path.Combine(GetRunFolder(experimentId), StateFileName);
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<ExperimentRunState>(File.ReadAllText(path), SerializerOptions)
            ?? throw new JsonException("Experiment run state must contain a JSON object.");
    }

    private static void Write(
        SimulationRunMetadata metadata,
        string status,
        string resolvedManifestSha256 = "",
        string failureType = "",
        string failureMessage = "",
        string failureStackTrace = "")
    {
        var folder = GetRunFolder(metadata.ExperimentId);
        Directory.CreateDirectory(folder);
        var code = CodeIdentityResolver.Resolve();
        var state = new ExperimentRunState
        {
            SchemaVersion = ExperimentRunState.CurrentSchemaVersion,
            ExperimentId = metadata.ExperimentId,
            ConditionId = metadata.Condition.ConditionId,
            ExecutionSha256 = ExperimentFingerprint.ForExecution(metadata, code),
            Status = status,
            UpdatedUtc = DateTime.UtcNow,
            ResolvedManifestSha256 = resolvedManifestSha256,
            FailureType = failureType,
            FailureMessage = failureMessage,
            FailureStackTrace = failureStackTrace
        };
        File.WriteAllText(
            Path.Combine(folder, StateFileName),
            JsonSerializer.Serialize(state, SerializerOptions));
    }

    private static JsonSerializerOptions CreateSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };
}
