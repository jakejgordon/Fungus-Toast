using System.Globalization;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Deterministic, exhaustive candidate generation around one parent strategy.
///
/// There is no seed and no sampling. Every operator enumerates a finite, ordered set of gene sets
/// derived from the parent, so the same plan against the same registry always produces the same
/// candidates in the same order, with the same rejections. That is a stronger reproducibility
/// guarantee than a recorded seed, and it is affordable because Phase 6 batches are capped at 100
/// games: a search wide enough to need sampling could not be evaluated anyway.
///
/// Deduplication is by behavior fingerprint, not by construction path, so two operators that
/// happen to converge on the same build collapse to one candidate. Rejections are returned rather
/// than dropped - they are the candidate-search half of the Static evidence stage.
/// </summary>
public static class CandidateGenerator
{
    public static CandidateGenerationResult Generate(CandidateGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var planErrors = CandidateGenerationPlanValidator.Validate(plan);
        if (planErrors.Count > 0)
        {
            throw new ArgumentException(
                $"Candidate generation plan is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, planErrors)}",
                nameof(plan));
        }

        var parent = StrategyRegistry.GetDefinitions(plan.ParentStrategySet)
            .First(definition => string.Equals(definition.StrategyId, plan.ParentStrategyId, StringComparison.Ordinal));
        var parentStrategy = (ParameterizedSpendingStrategy)parent.Strategy;
        var parentGenes = CandidateGenomeFactory.ExtractGenes(parentStrategy);
        var parentFingerprint = CandidateGenomeFingerprint.Compute(parentGenes);

        // Observing the parent's real build costs a scripted run, so it happens only when an
        // operator needs it. The script is deterministic, so generation stays reproducible.
        var observedBuild = plan.Operators.Contains(CandidateOperator.AblateObservedPurchase)
            ? CandidateCharacterizationGate.ObserveBuild(parentStrategy)
            : null;

        var proposals = EnumerateProposals(plan, parentGenes, observedBuild).ToList();
        if (proposals.Count > plan.MaximumCandidates)
        {
            throw new InvalidOperationException(
                $"Plan '{plan.PlanId}' enumerates {proposals.Count} candidates, which exceeds its maximumCandidates of {plan.MaximumCandidates}. "
                + "Narrow the operators or raise the cap deliberately; generation does not sample.");
        }

        // Behaviorally identical roster strategies are worth rejecting: re-testing a build that
        // already exists spends batch budget to re-prove a known result.
        var registeredFingerprints = BuildRegisteredFingerprintIndex();

        var accepted = new List<CandidateGenome>();
        var rejected = new List<CandidateRejection>();
        var seenFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var proposal in proposals)
        {
            var fingerprint = CandidateGenomeFingerprint.Compute(proposal.Genes);

            if (string.Equals(fingerprint, parentFingerprint, StringComparison.Ordinal))
            {
                rejected.Add(Reject(proposal, fingerprint, CandidateRejectionReason.DuplicateOfParent,
                    $"Identical behavior to parent '{parentStrategy.StrategyName}'."));
                continue;
            }

            if (seenFingerprints.TryGetValue(fingerprint, out var earlierCandidateId))
            {
                rejected.Add(Reject(proposal, fingerprint, CandidateRejectionReason.DuplicateOfEarlierCandidate,
                    $"Identical behavior to earlier candidate '{earlierCandidateId}'."));
                continue;
            }

            if (registeredFingerprints.TryGetValue(fingerprint, out var registeredName))
            {
                rejected.Add(Reject(proposal, fingerprint, CandidateRejectionReason.DuplicateOfRegisteredStrategy,
                    $"Identical behavior to registered strategy '{registeredName}'."));
                continue;
            }

            var genome = CandidateGenomeFactory.CreateFromParent(
                parent,
                proposal.Genes,
                BuildDisplayNameSuffix(plan, proposal, fingerprint),
                proposal.LineageNote);

            var genomeErrors = CandidateGenomeValidator.Validate(genome);
            if (genomeErrors.Count > 0)
            {
                rejected.Add(Reject(proposal, fingerprint, CandidateRejectionReason.FailedValidation,
                    string.Join(" ", genomeErrors)));
                continue;
            }

            seenFingerprints[fingerprint] = genome.CandidateId;
            accepted.Add(genome);
        }

        return new CandidateGenerationResult
        {
            PlanId = plan.PlanId,
            ParentStrategyId = plan.ParentStrategyId,
            ParentGeneFingerprint = parentFingerprint,
            Accepted = accepted,
            Rejected = rejected
        };
    }

    /// <summary>
    /// The candidate count a plan would generate before deduplication and validation. Callers can
    /// size a batch from this without running the search.
    /// </summary>
    public static int CountProposals(CandidateGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var parent = StrategyRegistry.GetDefinitions(plan.ParentStrategySet)
            .FirstOrDefault(definition => string.Equals(definition.StrategyId, plan.ParentStrategyId, StringComparison.Ordinal));
        if (parent?.Strategy is not ParameterizedSpendingStrategy parameterized)
            throw new ArgumentException($"Plan parent '{plan.ParentStrategyId}' is not a registered parameterized strategy.", nameof(plan));

        var observedBuild = plan.Operators.Contains(CandidateOperator.AblateObservedPurchase)
            ? CandidateCharacterizationGate.ObserveBuild(parameterized)
            : null;
        return EnumerateProposals(plan, CandidateGenomeFactory.ExtractGenes(parameterized), observedBuild).Count();
    }

    private static CandidateRejection Reject(
        CandidateProposal proposal,
        string fingerprint,
        CandidateRejectionReason reason,
        string detail)
        => new(proposal.Operator, proposal.OperatorIndex, fingerprint, reason, detail);

    private static Dictionary<string, string> BuildRegisteredFingerprintIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var strategySet in Enum.GetValues(typeof(StrategySetEnum)).Cast<StrategySetEnum>())
        foreach (var definition in StrategyRegistry.GetDefinitions(strategySet))
        {
            if (definition.Strategy is not ParameterizedSpendingStrategy parameterized) continue;
            var fingerprint = CandidateGenomeFingerprint.Compute(CandidateGenomeFactory.ExtractGenes(parameterized));
            // First registration wins; the name is only used to explain a rejection.
            index.TryAdd(fingerprint, parameterized.StrategyName);
        }

        return index;
    }

    /// <summary>
    /// Names stay stable when a plan's operator list is reordered because the suffix ends in the
    /// candidate's own behavior fingerprint rather than its position in the run.
    /// </summary>
    private static string BuildDisplayNameSuffix(CandidateGenerationPlan plan, CandidateProposal proposal, string fingerprint)
        => $"{plan.PlanId}_{proposal.Operator}_{fingerprint[..8]}";

    private static IEnumerable<CandidateProposal> EnumerateProposals(
        CandidateGenerationPlan plan,
        CandidateGeneSet parentGenes,
        IReadOnlyDictionary<int, int>? observedBuild)
    {
        foreach (var candidateOperator in plan.Operators)
        {
            var proposals = candidateOperator switch
            {
                CandidateOperator.GoalOrderAdjacentSwap => EnumerateGoalOrderAdjacentSwaps(parentGenes),
                CandidateOperator.GoalOrderPromoteToFront => EnumerateGoalOrderPromotions(parentGenes),
                CandidateOperator.EconomyBiasSweep => EnumerateEconomyBiasSweep(parentGenes),
                CandidateOperator.PrioritizeHighTierToggle => EnumerateHighTierToggle(parentGenes),
                CandidateOperator.SurgeAttemptTurnFrequencySweep => EnumerateSurgeFrequencySweep(parentGenes, plan.SurgeAttemptTurnFrequencyValues),
                CandidateOperator.StartingSporeEdgeOffsetSweep => EnumerateEdgeOffsetSweep(parentGenes, plan.StartingSporeEdgeOffsetValues),
                CandidateOperator.MaxTierSweep => EnumerateMaxTierSweep(parentGenes, plan.MaxTierValues),
                CandidateOperator.AblateTargetGoal => EnumerateGoalAblations(parentGenes),
                CandidateOperator.AblateObservedPurchase => EnumerateObservedAblations(parentGenes, observedBuild
                    ?? throw new InvalidOperationException("AblateObservedPurchase needs the parent's observed build.")),
                _ => throw new NotSupportedException($"Operator '{candidateOperator}' has no enumeration.")
            };

            var index = 0;
            foreach (var (genes, note) in proposals)
                yield return new CandidateProposal(candidateOperator, index++, genes, note);
        }
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateGoalOrderAdjacentSwaps(CandidateGeneSet parentGenes)
    {
        var goals = parentGenes.TargetMutationGoals;
        for (var position = 0; position + 1 < goals.Count; position++)
        {
            var reordered = goals.ToList();
            (reordered[position], reordered[position + 1]) = (reordered[position + 1], reordered[position]);
            yield return (
                WithGoals(parentGenes, reordered),
                $"Swapped build goals at positions {position} and {position + 1}.");
        }
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateGoalOrderPromotions(CandidateGeneSet parentGenes)
    {
        var goals = parentGenes.TargetMutationGoals;
        for (var position = 1; position < goals.Count; position++)
        {
            var reordered = goals.ToList();
            var promoted = reordered[position];
            reordered.RemoveAt(position);
            reordered.Insert(0, promoted);
            yield return (
                WithGoals(parentGenes, reordered),
                $"Promoted the build goal at position {position} to the opening.");
        }
    }

    /// <summary>
    /// One ablation per distinct mutation in the build plan: the goal is removed and the mutation
    /// blocked, so the paired difference against the parent is what that mutation contributed.
    ///
    /// Every occurrence of a repeated mutation goes, because a rising ladder is one intent
    /// expressed in stages and leaving part of it behind would ablate something else.
    /// </summary>
    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateGoalAblations(CandidateGeneSet parentGenes)
    {
        var goals = parentGenes.TargetMutationGoals;
        foreach (var mutationId in goals.Select(goal => goal.MutationId).Distinct())
        {
            var remaining = goals.Where(goal => goal.MutationId != mutationId).ToList();
            var exclusions = parentGenes.ExcludedMutationIds.Concat(new[] { mutationId }).Distinct().OrderBy(id => id).ToList();
            var name = MutationRegistry.GetById(mutationId)?.Name ?? mutationId.ToString(CultureInfo.InvariantCulture);

            // A mutation that gates other goals cannot be ablated alone: blocking it blocks
            // everything behind it, and the measured effect covers all of them. Saying so here is
            // what stops the result being read as that one mutation's contribution.
            var gated = FindGoalsGatedBehind(mutationId, remaining.Select(goal => goal.MutationId).Distinct().ToList());
            var note = $"Ablates {name}: removed from the build plan and blocked outright.";
            if (gated.Count > 0)
            {
                var gatedNames = gated.Select(id => MutationRegistry.GetById(id)?.Name ?? id.ToString(CultureInfo.InvariantCulture));
                note += $" It also gates {string.Join(", ", gatedNames)}, so the measured effect covers "
                    + (gated.Count == 1 ? "that too." : "those too.");
            }

            yield return (Clone(parentGenes, targetMutationGoals: remaining, excludedMutationIds: exclusions), note);
        }
    }

    /// <summary>
    /// One ablation per mutation the parent actually buys, ordered by mutation ID so the sweep is
    /// stable. A purchase that is also a goal has the goal removed with it, exactly as goal
    /// ablation does, because blocking a mutation the strategy is still told to buy is incoherent.
    /// </summary>
    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateObservedAblations(
        CandidateGeneSet parentGenes,
        IReadOnlyDictionary<int, int> observedBuild)
    {
        var alreadyExcluded = parentGenes.ExcludedMutationIds.ToHashSet();
        foreach (var mutationId in observedBuild.Keys.Where(id => !alreadyExcluded.Contains(id)).OrderBy(id => id))
        {
            var remaining = parentGenes.TargetMutationGoals.Where(goal => goal.MutationId != mutationId).ToList();
            var exclusions = parentGenes.ExcludedMutationIds.Concat(new[] { mutationId }).Distinct().OrderBy(id => id).ToList();
            var mutation = MutationRegistry.GetById(mutationId);
            var name = mutation?.Name ?? mutationId.ToString(CultureInfo.InvariantCulture);
            var wasGoal = parentGenes.TargetMutationGoals.Any(goal => goal.MutationId == mutationId);

            var note = $"Ablates {name} ({mutation?.Category.ToString() ?? "unknown"}, {observedBuild[mutationId]} levels observed), "
                + $"which the parent reaches {(wasGoal ? "as a declared goal" : "through the fallback path only")}.";

            var gated = FindGoalsGatedBehind(mutationId, remaining.Select(goal => goal.MutationId).Distinct().ToList());
            if (gated.Count > 0)
            {
                var gatedNames = gated.Select(id => MutationRegistry.GetById(id)?.Name ?? id.ToString(CultureInfo.InvariantCulture));
                note += $" It also gates {string.Join(", ", gatedNames)}, so the measured effect covers "
                    + (gated.Count == 1 ? "that too." : "those too.");
                // Excluding it cannot actually work: the strategy still has goals behind it, and the
                // prerequisite path buys what a goal needs regardless of the exclusion list. The
                // candidate is still emitted so the conflict is visible, and characterization
                // rejects it as ViolatedExclusions rather than it passing as a real measurement.
                note += " The exclusion cannot hold while those goals remain, so this ablation is not achievable.";
            }

            yield return (Clone(parentGenes, targetMutationGoals: remaining, excludedMutationIds: exclusions), note);
        }
    }

    /// <summary>
    /// Goals that transitively require <paramref name="mutationId"/>, and so become unreachable
    /// when it is blocked.
    ///
    /// The walk has to follow the whole prerequisite chain, not just goal-to-goal links: a root
    /// mutation usually gates a goal through intermediates that are not themselves goals, and
    /// stopping at goals would report such an ablation as clean when the strategy will simply buy
    /// the mutation anyway to reach what depends on it.
    /// </summary>
    private static IReadOnlyList<int> FindGoalsGatedBehind(int mutationId, IReadOnlyList<int> remainingGoalIds)
    {
        return remainingGoalIds
            .Where(goalId => goalId != mutationId && RequiresTransitively(goalId, mutationId))
            .OrderBy(goalId => goalId)
            .ToList();
    }

    /// <summary>Whether <paramref name="goalId"/> needs <paramref name="mutationId"/> anywhere in its prerequisite chain.</summary>
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

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateEconomyBiasSweep(CandidateGeneSet parentGenes)
    {
        foreach (var bias in Enum.GetValues(typeof(EconomyBias)).Cast<EconomyBias>())
            yield return (Clone(parentGenes, economyBias: bias), $"Set economy bias to {bias}.");
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateHighTierToggle(CandidateGeneSet parentGenes)
    {
        var toggled = !parentGenes.PrioritizeHighTier;
        yield return (Clone(parentGenes, prioritizeHighTier: toggled), $"Set high-tier prioritization to {toggled}.");
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateSurgeFrequencySweep(
        CandidateGeneSet parentGenes,
        IReadOnlyList<int> values)
    {
        foreach (var value in values)
            yield return (Clone(parentGenes, surgeAttemptTurnFrequency: value), $"Set surge attempt turn frequency to {value}.");
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateEdgeOffsetSweep(
        CandidateGeneSet parentGenes,
        IReadOnlyList<int> values)
    {
        foreach (var value in values)
            yield return (Clone(parentGenes, startingSporeEdgeOffset: value), $"Set starting spore edge offset to {value}.");
    }

    private static IEnumerable<(CandidateGeneSet Genes, string Note)> EnumerateMaxTierSweep(
        CandidateGeneSet parentGenes,
        IReadOnlyList<MutationTier> values)
    {
        foreach (var value in values)
            yield return (Clone(parentGenes, maxTier: value), $"Set max tier to {value}.");
    }

    private static CandidateGeneSet WithGoals(CandidateGeneSet parentGenes, IReadOnlyList<CandidateMutationGoal> goals)
        => Clone(parentGenes, targetMutationGoals: goals);

    private static CandidateGeneSet Clone(
        CandidateGeneSet source,
        bool? prioritizeHighTier = null,
        MutationTier? maxTier = null,
        IReadOnlyList<CandidateMutationGoal>? targetMutationGoals = null,
        int? surgeAttemptTurnFrequency = null,
        EconomyBias? economyBias = null,
        int? startingSporeEdgeOffset = null,
        IReadOnlyList<int>? excludedMutationIds = null)
    {
        return new CandidateGeneSet
        {
            PrioritizeHighTier = prioritizeHighTier ?? source.PrioritizeHighTier,
            MaxTier = maxTier ?? source.MaxTier,
            PriorityMutationCategories = source.PriorityMutationCategories,
            TargetMutationGoals = targetMutationGoals ?? source.TargetMutationGoals,
            SurgePriorityIds = source.SurgePriorityIds,
            SurgeAttemptTurnFrequency = surgeAttemptTurnFrequency ?? source.SurgeAttemptTurnFrequency,
            EconomyBias = economyBias ?? source.EconomyBias,
            MycovariantPreferences = source.MycovariantPreferences,
            ExcludedMutationIds = excludedMutationIds ?? source.ExcludedMutationIds,
            StartingSporeEdgeOffset = startingSporeEdgeOffset ?? source.StartingSporeEdgeOffset
        };
    }

    private sealed record CandidateProposal(
        CandidateOperator Operator,
        int OperatorIndex,
        CandidateGeneSet Genes,
        string LineageNote);
}
