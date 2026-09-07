using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Simulation.Candidates;

namespace FungusToast.Simulation.Calibration;

public enum CalibrationConditionStatus
{
    Pending,
    Complete,
    Failed
}

public sealed class CalibrationConditionRecord
{
    public required string ContextId { get; set; }

    public required int RepeatIndex { get; set; }

    public required bool IsHoldout { get; set; }

    public required string ExperimentId { get; set; }

    public CalibrationConditionStatus Status { get; set; } = CalibrationConditionStatus.Pending;

    public int Attempts { get; set; }

    public int GamesRun { get; set; }

    public double RuntimeSeconds { get; set; }

    public string ArtifactPath { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// Durable state for one pass over a calibration matrix.
///
/// A calibration campaign is hours of simulation, so it has to survive being interrupted. State is
/// written after every condition, and a resumed run skips conditions already recorded complete
/// rather than re-running them - which is also what makes it safe to widen a matrix later and only
/// pay for the contexts that were added.
/// </summary>
public sealed class CalibrationRunState
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-calibration-run.v1";

    public required string SchemaVersion { get; set; }

    public required string MatrixId { get; set; }

    /// <summary>
    /// Conditions are keyed by matrix and context, so a resumed run recognises its own prior work.
    /// </summary>
    public required List<CalibrationConditionRecord> Conditions { get; set; }

    public int GamesRun { get; set; }

    public double RuntimeSeconds { get; set; }

    [JsonIgnore]
    public int RemainingConditions => Conditions.Count(record => record.Status != CalibrationConditionStatus.Complete);

    public static CalibrationRunState Create(CalibrationMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        return new CalibrationRunState
        {
            SchemaVersion = CurrentSchemaVersion,
            MatrixId = matrix.MatrixId,
            Conditions = CalibrationConditionEmitter.EnumerateConditions(matrix)
                .Select(key => new CalibrationConditionRecord
                {
                    ContextId = key.ContextId,
                    RepeatIndex = key.RepeatIndex,
                    IsHoldout = key.IsHoldout,
                    ExperimentId = CalibrationConditionEmitter.BuildExperimentId(matrix, key)
                })
                .ToList()
        };
    }

    /// <summary>
    /// Reconciles saved state against a matrix: conditions the matrix no longer has are dropped,
    /// and conditions it has gained are added as pending. Completed work is preserved, so widening
    /// a matrix costs only the new contexts.
    /// </summary>
    public void Reconcile(CalibrationMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (!string.Equals(MatrixId, matrix.MatrixId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Saved run state belongs to matrix '{MatrixId}', not '{matrix.MatrixId}'. "
                + "Resuming across matrices would mix evidence from different frozen designs.");
        }

        var expected = CalibrationConditionEmitter.EnumerateConditions(matrix)
            .ToDictionary(
                key => CalibrationConditionEmitter.BuildExperimentId(matrix, key),
                key => key,
                StringComparer.Ordinal);

        Conditions.RemoveAll(record => !expected.ContainsKey(record.ExperimentId));
        foreach (var (experimentId, key) in expected)
        {
            if (Conditions.Any(record => string.Equals(record.ExperimentId, experimentId, StringComparison.Ordinal))) continue;
            Conditions.Add(new CalibrationConditionRecord
            {
                ContextId = key.ContextId,
                RepeatIndex = key.RepeatIndex,
                IsHoldout = key.IsHoldout,
                ExperimentId = experimentId
            });
        }
    }
}

public static class CalibrationRunStateJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CalibrationRunState Deserialize(string json)
    {
        var state = JsonSerializer.Deserialize<CalibrationRunState>(json, SerializerOptions);
        if (state == null) throw new JsonException("Calibration run state must contain a JSON object.");
        if (!string.Equals(state.SchemaVersion, CalibrationRunState.CurrentSchemaVersion, StringComparison.Ordinal))
            throw new JsonException(
                $"Calibration run state declares schema '{state.SchemaVersion}'; expected '{CalibrationRunState.CurrentSchemaVersion}'.");
        return state;
    }

    public static string Serialize(CalibrationRunState state) => JsonSerializer.Serialize(state, SerializerOptions);

    public static void Save(CalibrationRunState state, string path) => File.WriteAllText(path, Serialize(state));

    public static CalibrationRunState Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Calibration run state '{path}' does not exist.", path);
        return Deserialize(File.ReadAllText(path));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}

public sealed record CalibrationRunReport(
    int ConditionsRun,
    int ConditionsSkipped,
    int ConditionsFailed,
    int GamesRun,
    double RuntimeSeconds,
    string StopReason);

/// <summary>
/// Runs a frozen matrix to completion, one condition at a time.
///
/// Condition execution is injected for the same reason the candidate driver injects it: the part
/// worth testing is the sequencing, resumption, and budget behaviour, not the ability to start a
/// process. The real implementation is <see cref="ProcessCandidateExecution.RunArm"/>, since a
/// calibration condition and a candidate arm are both just a simulator invocation.
/// </summary>
public sealed class CalibrationDriver
{
    private readonly Func<IReadOnlyList<string>, CandidateArmOutcome> runCondition;

    public CalibrationDriver(Func<IReadOnlyList<string>, CandidateArmOutcome> runCondition)
        => this.runCondition = runCondition ?? throw new ArgumentNullException(nameof(runCondition));

    public CalibrationRunReport Run(
        CalibrationMatrix matrix,
        CalibrationRunState state,
        string? statePath = null,
        int maximumAttemptsPerCondition = 2)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(state);

        var errors = CalibrationMatrixValidator.Validate(matrix);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Calibration matrix is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                nameof(matrix));
        }

        state.Reconcile(matrix);

        int run = 0, skipped = 0, failed = 0;
        var stopReason = "All conditions complete.";

        foreach (var record in state.Conditions)
        {
            if (record.Status == CalibrationConditionStatus.Complete)
            {
                skipped++;
                continue;
            }

            if (record.Attempts >= maximumAttemptsPerCondition)
            {
                failed++;
                continue;
            }

            var context = CalibrationConditionEmitter.ResolveContext(matrix, ToKey(record));
            if (state.GamesRun + context.GamesPerCondition > matrix.TotalGameBudget)
            {
                stopReason = "Remaining budget cannot fund the next condition.";
                break;
            }

            if (state.RuntimeSeconds >= matrix.RuntimeBudgetSeconds)
            {
                stopReason = "Runtime budget is exhausted.";
                break;
            }

            var arguments = CalibrationConditionEmitter.RenderCommandLine(matrix, ToKey(record));

            // Retry inside this pass rather than leaving a hole for someone to notice later. An
            // unattended campaign that abandons a condition on one transient failure produces a
            // matrix with a gap in it, and the gap is only discovered at analysis time.
            while (record.Attempts < maximumAttemptsPerCondition
                   && record.Status != CalibrationConditionStatus.Complete)
            {
                var outcome = runCondition(arguments);
                record.Attempts++;
                record.RuntimeSeconds += outcome.RuntimeSeconds;
                record.ArtifactPath = outcome.ArtifactPath;
                record.Detail = outcome.Detail;
                state.RuntimeSeconds += outcome.RuntimeSeconds;

                if (outcome.Succeeded)
                {
                    record.Status = CalibrationConditionStatus.Complete;
                    record.GamesRun = context.GamesPerCondition;
                    state.GamesRun += context.GamesPerCondition;
                    run++;
                }
                else if (record.Attempts >= maximumAttemptsPerCondition)
                {
                    record.Status = CalibrationConditionStatus.Failed;
                    failed++;
                }
            }

            // Saving after every condition is what makes hours of simulation resumable rather than
            // restartable.
            if (!string.IsNullOrWhiteSpace(statePath)) CalibrationRunStateJson.Save(state, statePath);
        }

        if (!string.IsNullOrWhiteSpace(statePath)) CalibrationRunStateJson.Save(state, statePath);
        return new CalibrationRunReport(run, skipped, failed, state.GamesRun, state.RuntimeSeconds, stopReason);
    }

    private static CalibrationConditionKey ToKey(CalibrationConditionRecord record)
        => new(record.ContextId, record.RepeatIndex, record.IsHoldout);
}
