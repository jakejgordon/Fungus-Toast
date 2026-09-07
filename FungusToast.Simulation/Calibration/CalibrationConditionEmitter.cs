using System.Globalization;
using FungusToast.Simulation.Experiments;

namespace FungusToast.Simulation.Calibration;

/// <summary>One runnable unit of a calibration matrix: a context, and which repeat of it.</summary>
public sealed record CalibrationConditionKey(string ContextId, int RepeatIndex, bool IsHoldout)
{
    public override string ToString() => RepeatIndex == 0 ? ContextId : $"{ContextId}#r{RepeatIndex}";
}

/// <summary>
/// Turns a frozen matrix into runnable conditions.
///
/// Each condition is its own experiment, so its artifact stands alone and can be analyzed,
/// resumed, or discarded without touching the others. Experiment IDs are derived from the matrix
/// and context IDs rather than generated, which is what lets a re-run recognise work it has already
/// done instead of piling up near-duplicate artifacts.
/// </summary>
public static class CalibrationConditionEmitter
{
    /// <summary>
    /// Every condition the matrix will run, calibration contexts first, in a fixed order so a
    /// resumed run continues where it stopped.
    /// </summary>
    public static IReadOnlyList<CalibrationConditionKey> EnumerateConditions(CalibrationMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        var conditions = new List<CalibrationConditionKey>();

        foreach (var context in matrix.CalibrationContexts)
        foreach (var repeat in Enumerable.Range(0, Math.Max(1, context.Repeats)))
            conditions.Add(new CalibrationConditionKey(context.ContextId, repeat, IsHoldout: false));

        foreach (var context in matrix.HoldoutContexts)
        foreach (var repeat in Enumerable.Range(0, Math.Max(1, context.Repeats)))
            conditions.Add(new CalibrationConditionKey(context.ContextId, repeat, IsHoldout: true));

        return conditions;
    }

    public static CalibrationContext ResolveContext(CalibrationMatrix matrix, CalibrationConditionKey key)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(key);
        return matrix.AllContexts.FirstOrDefault(context =>
                   string.Equals(context.ContextId, key.ContextId, StringComparison.Ordinal))
               ?? throw new ArgumentException($"Matrix '{matrix.MatrixId}' has no context '{key.ContextId}'.", nameof(key));
    }

    /// <summary>
    /// The artifact identity for a condition. Derived so the same matrix always names the same
    /// artifacts, and so a repeat cannot collide with its siblings.
    /// </summary>
    public static string BuildExperimentId(CalibrationMatrix matrix, CalibrationConditionKey key)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(key);
        var context = ResolveContext(matrix, key);
        var suffix = context.Repeats > 1
            ? $"_r{key.RepeatIndex.ToString(CultureInfo.InvariantCulture)}"
            : string.Empty;
        return $"{matrix.MatrixId}__{Sanitize(key.ContextId)}{suffix}";
    }

    public static IReadOnlyList<string> RenderCommandLine(CalibrationMatrix matrix, CalibrationConditionKey key)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(key);

        var context = ResolveContext(matrix, key);
        var arguments = new List<string>
        {
            "--experiment-id", BuildExperimentId(matrix, key),
            "--purpose", $"P7 calibration of {matrix.MatrixId} in context {key.ContextId}.",
            "--games", context.GamesPerCondition.ToString(CultureInfo.InvariantCulture),
            "--seed", context.SeedForRepeat(key.RepeatIndex).ToString(CultureInfo.InvariantCulture),
            "--players", context.PlayerCount.ToString(CultureInfo.InvariantCulture),
            "--width", context.BoardWidth.ToString(CultureInfo.InvariantCulture),
            "--height", context.BoardHeight.ToString(CultureInfo.InvariantCulture),
            "--strategy-set", matrix.StrategySet.ToString(),
            "--selection-policy", matrix.SelectionPolicy.ToString(),
            // Calibration measures rather than decides, which is what lets a context run wider than
            // the staged promotion ceiling.
            "--analysis-version", CandidateAnalysisContract,
            "--evidence-stage", ExperimentEvidenceStage.Exploratory.ToString(),
            "--total-game-budget", matrix.TotalGameBudget.ToString(CultureInfo.InvariantCulture),
            "--runtime-budget-seconds", matrix.RuntimeBudgetSeconds.ToString(CultureInfo.InvariantCulture),
            "--parquet",
            "--no-keyboard"
        };

        if (matrix.StrategyNames.Count > 0)
        {
            arguments.Add("--strategy-names");
            arguments.Add(string.Join(",", matrix.StrategyNames));
        }

        arguments.Add(context.SlotAssignmentPolicy == Models.SlotAssignmentPolicy.RotateByGame
            ? "--rotate-slots"
            : "--fixed-slots");

        // Systems are named explicitly rather than left to defaults, so an artifact records what was
        // actually enabled instead of whatever the CLI happened to default to that week.
        if (!context.NutrientPatchesEnabled) arguments.Add("--no-nutrient-patches");
        if (!context.MycovariantDraftEnabled) arguments.Add("--no-mycovariants");
        if (!context.StartingAdaptationsEnabled) arguments.Add("--no-starting-adaptations");

        return arguments;
    }

    /// <summary>Must match the offline analyzer's version or its outputs are refused.</summary>
    public const string CandidateAnalysisContract = "fungus-toast.analysis.v2";

    private static string Sanitize(string value)
    {
        var characters = value.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_');
        return new string(characters.ToArray());
    }
}
