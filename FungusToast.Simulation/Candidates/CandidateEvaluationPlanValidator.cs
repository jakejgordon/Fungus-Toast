using System.Text.RegularExpressions;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Validates an evaluation plan before any stage is emitted.
///
/// The load-bearing rule is the holdout contrast: a holdout that reused the screening board or
/// seed would not be a holdout at all, it would re-measure the context the candidate was selected
/// on and report the result as independent confirmation.
/// </summary>
public static partial class CandidateEvaluationPlanValidator
{
    public const int MaximumEvaluationIdLength = 48;

    public static IReadOnlyList<string> Validate(CandidateEvaluationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var errors = new List<string>();

        if (!string.Equals(plan.SchemaVersion, CandidateEvaluationPlan.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add($"schemaVersion must be '{CandidateEvaluationPlan.CurrentSchemaVersion}'.");

        if (string.IsNullOrWhiteSpace(plan.EvaluationId)
            || plan.EvaluationId.Length > MaximumEvaluationIdLength
            || !EvaluationIdPattern().IsMatch(plan.EvaluationId))
        {
            errors.Add($"evaluationId must be 1-{MaximumEvaluationIdLength} characters using only letters, numbers, '.', '_' or '-'.");
        }

        if (string.IsNullOrWhiteSpace(plan.Purpose))
            errors.Add("purpose is required.");

        if (plan.SmokeGames is < 3 or > 5)
            errors.Add("smokeGames must be between 3 and 5.");

        if (!double.IsFinite(plan.Margin) || plan.Margin < 0)
            errors.Add("margin must be finite and non-negative.");

        if (!double.IsFinite(plan.RuntimeBudgetSecondsPerStage) || plan.RuntimeBudgetSecondsPerStage <= 0)
            errors.Add("runtimeBudgetSecondsPerStage must be finite and positive.");

        if (!Enum.IsDefined(typeof(Experiments.ExperimentDirection), plan.Direction))
            errors.Add($"direction '{plan.Direction}' is not a defined direction.");

        ValidateCast(plan, errors);
        ValidateContexts(plan, errors);
        return errors;
    }

    /// <summary>
    /// Every named strategy must already be published in the generated catalog, because both arms
    /// name that one set and the emitter reads stable IDs from it.
    /// </summary>
    private static void ValidateCast(CandidateEvaluationPlan plan, ICollection<string> errors)
    {
        var names = new[]
        {
            ("candidateDisplayName", plan.CandidateDisplayName),
            ("parentStrategyName", plan.ParentStrategyName),
            ("opponentStrategyName", plan.OpponentStrategyName)
        };

        foreach (var (field, name) in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"{field} is required.");
                continue;
            }

            if (StrategyRegistry.GetDefinition(StrategySetEnum.Generated, name) == null)
                errors.Add($"{field} '{name}' is not published in the generated catalog.");
        }

        var distinct = names.Select(entry => entry.Item2)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (distinct != names.Count(entry => !string.IsNullOrWhiteSpace(entry.Item2)))
            errors.Add("candidate, parent, and opponent must be three different strategies.");
    }

    private static void ValidateContexts(CandidateEvaluationPlan plan, ICollection<string> errors)
    {
        ValidateContext("screeningContext", plan.ScreeningContext, errors);
        ValidateContext("holdoutContext", plan.HoldoutContext, errors);
        if (plan.ScreeningContext == null || plan.HoldoutContext == null) return;

        var sameBoard = plan.ScreeningContext.BoardWidth == plan.HoldoutContext.BoardWidth
            && plan.ScreeningContext.BoardHeight == plan.HoldoutContext.BoardHeight;
        if (sameBoard)
            errors.Add("holdoutContext must use a different board than screeningContext; a holdout on the selection board is not an unseen context.");

        if (plan.ScreeningContext.BaseSeed == plan.HoldoutContext.BaseSeed)
            errors.Add("holdoutContext must use a different base seed than screeningContext.");
    }

    private static void ValidateContext(string field, CandidateEvaluationContext? context, ICollection<string> errors)
    {
        if (context == null)
        {
            errors.Add($"{field} is required.");
            return;
        }

        if (context.BoardWidth < 1 || context.BoardHeight < 1)
            errors.Add($"{field} board width and height must both be positive.");
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex EvaluationIdPattern();
}
