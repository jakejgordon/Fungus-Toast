using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// Why a strategy owns a mutation.
/// </summary>
public enum MutationEntailment
{
    /// <summary>The strategy declares it as a build goal.</summary>
    Goal,

    /// <summary>Something the strategy targets requires it, so it had no choice.</summary>
    RequiredByGoal,

    /// <summary>Bought through the fallback path with nothing forcing it. The only free variation.</summary>
    Free
}

public sealed record MutationOwnership(
    int MutationId,
    string MutationName,
    MutationCategory Category,
    int Levels,
    MutationEntailment Entailment,
    IReadOnlyList<int> ForcedBy);

public enum ConfoundVerdict
{
    /// <summary>Every strategy that owns it was forced to. Co-occurrence carries no information about value.</summary>
    StructurallyEntailed,

    /// <summary>Some owners chose it freely, but enough were forced that a raw correlation is contaminated.</summary>
    PartiallyEntailed,

    /// <summary>Every owner chose it freely. A correlation here is at least about a real decision.</summary>
    FreeVariation
}

public sealed record MutationConfoundReport(
    int MutationId,
    string MutationName,
    MutationCategory Category,
    int Owners,
    int FreeOwners,
    int EntailedOwners,
    ConfoundVerdict Verdict)
{
    /// <summary>
    /// Whether a purchase-level correlation over this mutation may be reported as a lead at all.
    /// </summary>
    public bool IsReportableAsEvidence => Verdict == ConfoundVerdict.FreeVariation;
}

/// <summary>
/// Screens purchase-level correlations for structural confounding before they are believed.
///
/// A realized build is not a set of choices: it is the strategy's goals plus everything the
/// prerequisite tree forces along the way. Correlating strength against raw purchase counts
/// therefore measures the shape of the mutation tree at least as much as the value of any
/// mutation, and it does so most strongly for exactly the mutations that look most interesting -
/// low-tier roots that every successful build is required to own.
///
/// This is not hypothetical. A category-level correlation of `r = 0.792` between Fungicide
/// investment and measured strength turned out to be one Tier-1 mutation, `Mycotoxin Tracer`,
/// which is a prerequisite of `Necrosporulation` and so is bought by every strategy targeting that
/// line. It could not have been otherwise for any of its owners, and a variable that could not
/// have differed says nothing about value. The screen exists so that finding is made by a function
/// in milliseconds rather than by a person after an experiment.
///
/// The deeper remedy is to correlate over genes rather than builds - goals, ordering, and biases
/// are what an author actually chose - and this screen is the guard for the cases where a
/// purchase-level question is asked anyway.
/// </summary>
public static class StructuralConfoundScreen
{
    /// <summary>
    /// Explains every mutation in a strategy's observed build: chosen, forced, or free.
    /// </summary>
    public static IReadOnlyList<MutationOwnership> ExplainBuild(
        ParameterizedSpendingStrategy strategy,
        CandidateCharacterizationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        var build = CandidateCharacterizationGate.ObserveBuild(strategy, settings);
        var goalIds = strategy.TargetMutationGoals.Select(goal => goal.MutationId).ToHashSet();

        var ownerships = new List<MutationOwnership>();
        foreach (var (mutationId, levels) in build.OrderBy(entry => entry.Key))
        {
            var mutation = MutationRegistry.GetById(mutationId);
            if (mutation == null) continue;

            if (goalIds.Contains(mutationId))
            {
                ownerships.Add(new MutationOwnership(
                    mutationId, mutation.Name, mutation.Category, levels, MutationEntailment.Goal, Array.Empty<int>()));
                continue;
            }

            var forcedBy = goalIds.Where(goalId => RequiresTransitively(goalId, mutationId)).OrderBy(id => id).ToList();
            ownerships.Add(new MutationOwnership(
                mutationId,
                mutation.Name,
                mutation.Category,
                levels,
                forcedBy.Count > 0 ? MutationEntailment.RequiredByGoal : MutationEntailment.Free,
                forcedBy));
        }

        return ownerships;
    }

    /// <summary>
    /// Screens a whole panel, reporting for each mutation whether its owners had any choice.
    /// A purchase-level correlation may only be reported for mutations that come back
    /// <see cref="ConfoundVerdict.FreeVariation"/>.
    /// </summary>
    public static IReadOnlyList<MutationConfoundReport> ScreenPanel(
        IReadOnlyList<ParameterizedSpendingStrategy> panel,
        CandidateCharacterizationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var byMutation = new Dictionary<int, (int Free, int Entailed, string Name, MutationCategory Category)>();
        foreach (var strategy in panel)
        foreach (var ownership in ExplainBuild(strategy, settings))
        {
            byMutation.TryGetValue(ownership.MutationId, out var tally);
            tally.Name = ownership.MutationName;
            tally.Category = ownership.Category;
            // A declared goal is a choice; a forced prerequisite is not.
            if (ownership.Entailment == MutationEntailment.RequiredByGoal) tally.Entailed++;
            else tally.Free++;
            byMutation[ownership.MutationId] = tally;
        }

        return byMutation
            .Select(entry =>
            {
                var (free, entailed, name, category) = entry.Value;
                var verdict = entailed == 0
                    ? ConfoundVerdict.FreeVariation
                    : free == 0 ? ConfoundVerdict.StructurallyEntailed : ConfoundVerdict.PartiallyEntailed;
                return new MutationConfoundReport(entry.Key, name, category, free + entailed, free, entailed, verdict);
            })
            .OrderByDescending(report => report.Owners)
            .ThenBy(report => report.MutationId)
            .ToList();
    }

    /// <summary>
    /// The guard a purchase-level finding must pass. Returns the reason it is not evidence, or null
    /// when the variable genuinely varied by choice.
    /// </summary>
    public static string? ExplainWhyNotEvidence(MutationConfoundReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.Verdict switch
        {
            ConfoundVerdict.StructurallyEntailed =>
                $"{report.MutationName} is required by a goal in all {report.Owners} strategies that own it, "
                + "so no owner could have gone without it. Its co-occurrence with strength reflects which goals "
                + "were chosen, not the mutation's value.",
            ConfoundVerdict.PartiallyEntailed =>
                $"{report.MutationName} is forced by goals in {report.EntailedOwners} of {report.Owners} owners, "
                + $"so only {report.FreeOwners} chose it freely. A raw correlation mixes the choice with the "
                + "prerequisite tree; restrict it to the free owners or measure by ablation instead.",
            _ => null
        };
    }

    private static bool RequiresTransitively(int goalId, int mutationId)
    {
        var visited = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(goalId);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current)) continue;

            var mutation = MutationRegistry.GetById(current);
            if (mutation == null) continue;

            foreach (var prerequisite in mutation.Prerequisites)
            {
                if (prerequisite.MutationId == mutationId) return true;
                pending.Push(prerequisite.MutationId);
            }
        }

        return false;
    }
}
