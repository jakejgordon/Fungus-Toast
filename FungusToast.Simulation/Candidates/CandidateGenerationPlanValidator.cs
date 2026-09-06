using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Validates a generation plan before anything runs. Every operator that needs a declared value
/// list must have a non-empty one, and every operator that cannot need one must not carry it -
/// an ignored field in a search plan is a silent lie about what was searched.
/// </summary>
public static partial class CandidateGenerationPlanValidator
{
    public static IReadOnlyList<string> Validate(CandidateGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var errors = new List<string>();

        if (!string.Equals(plan.SchemaVersion, CandidateGenerationPlan.CurrentSchemaVersion, StringComparison.Ordinal))
            errors.Add($"schemaVersion must be '{CandidateGenerationPlan.CurrentSchemaVersion}'.");

        if (string.IsNullOrWhiteSpace(plan.PlanId)
            || plan.PlanId.Length > CandidateGenerationPlan.MaximumPlanIdLength
            || !PlanIdPattern().IsMatch(plan.PlanId))
        {
            errors.Add($"planId must be 1-{CandidateGenerationPlan.MaximumPlanIdLength} characters using only letters, numbers, '.', '_' or '-'.");
        }

        if (string.IsNullOrWhiteSpace(plan.Purpose))
            errors.Add("purpose is required.");

        if (plan.MaximumCandidates < 1 || plan.MaximumCandidates > CandidateGenerationPlan.CandidateCeiling)
            errors.Add($"maximumCandidates must be between 1 and {CandidateGenerationPlan.CandidateCeiling}.");

        ValidateOperators(plan, errors);
        ValidateValueLists(plan, errors);
        ValidateParent(plan, errors);
        return errors;
    }

    private static void ValidateOperators(CandidateGenerationPlan plan, ICollection<string> errors)
    {
        if (plan.Operators == null || plan.Operators.Count == 0)
        {
            errors.Add("operators must contain at least one operator.");
            return;
        }

        var seen = new HashSet<CandidateOperator>();
        foreach (var candidateOperator in plan.Operators)
        {
            if (!Enum.IsDefined(typeof(CandidateOperator), candidateOperator))
                errors.Add($"operators contains undefined operator '{candidateOperator}'.");
            else if (!seen.Add(candidateOperator))
                errors.Add($"operators repeats operator '{candidateOperator}'.");
        }
    }

    private static void ValidateValueLists(CandidateGenerationPlan plan, ICollection<string> errors)
    {
        var operators = plan.Operators ?? Array.Empty<CandidateOperator>();

        ValidateIntegerValues(
            "surgeAttemptTurnFrequencyValues",
            plan.SurgeAttemptTurnFrequencyValues,
            operators.Contains(CandidateOperator.SurgeAttemptTurnFrequencySweep),
            CandidateGenome.MinimumSurgeAttemptTurnFrequency,
            CandidateGenome.MaximumSurgeAttemptTurnFrequency,
            errors);

        ValidateIntegerValues(
            "startingSporeEdgeOffsetValues",
            plan.StartingSporeEdgeOffsetValues,
            operators.Contains(CandidateOperator.StartingSporeEdgeOffsetSweep),
            CandidateGenome.MinimumStartingSporeEdgeOffset,
            CandidateGenome.MaximumStartingSporeEdgeOffset,
            errors);

        var tierValues = plan.MaxTierValues ?? Array.Empty<MutationTier>();
        var tierSweepDeclared = operators.Contains(CandidateOperator.MaxTierSweep);
        if (tierSweepDeclared && tierValues.Count == 0)
            errors.Add("maxTierValues is required when operators includes MaxTierSweep.");
        if (!tierSweepDeclared && tierValues.Count > 0)
            errors.Add("maxTierValues must be empty unless operators includes MaxTierSweep.");

        var seenTiers = new HashSet<MutationTier>();
        foreach (var tier in tierValues)
        {
            if (!Enum.IsDefined(typeof(MutationTier), tier))
                errors.Add($"maxTierValues contains undefined tier '{tier}'.");
            else if (!seenTiers.Add(tier))
                errors.Add($"maxTierValues repeats tier '{tier}'.");
        }
    }

    private static void ValidateIntegerValues(
        string field,
        IReadOnlyList<int>? values,
        bool operatorDeclared,
        int minimum,
        int maximum,
        ICollection<string> errors)
    {
        var declaredValues = values ?? Array.Empty<int>();
        if (operatorDeclared && declaredValues.Count == 0)
            errors.Add($"{field} is required when its sweep operator is declared.");
        if (!operatorDeclared && declaredValues.Count > 0)
            errors.Add($"{field} must be empty unless its sweep operator is declared.");

        var seen = new HashSet<int>();
        foreach (var value in declaredValues)
        {
            if (value < minimum || value > maximum)
                errors.Add($"{field} contains {value}, which is outside {minimum}-{maximum}.");
            if (!seen.Add(value))
                errors.Add($"{field} repeats value {value}.");
        }
    }

    private static void ValidateParent(CandidateGenerationPlan plan, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(plan.ParentStrategyId))
        {
            errors.Add("parentStrategyId is required.");
            return;
        }

        if (!Enum.IsDefined(typeof(StrategySetEnum), plan.ParentStrategySet))
        {
            errors.Add($"parentStrategySet '{plan.ParentStrategySet}' is not a defined strategy set.");
            return;
        }

        var parent = StrategyRegistry.GetDefinitions(plan.ParentStrategySet)
            .FirstOrDefault(definition => string.Equals(definition.StrategyId, plan.ParentStrategyId, StringComparison.Ordinal));
        if (parent == null)
        {
            errors.Add($"parentStrategyId '{plan.ParentStrategyId}' is not registered in strategy set '{plan.ParentStrategySet}'.");
            return;
        }

        if (parent.Strategy is not ParameterizedSpendingStrategy)
            errors.Add($"parentStrategyId '{plan.ParentStrategyId}' is not a parameterized strategy and cannot seed a candidate search.");
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex PlanIdPattern();
}
