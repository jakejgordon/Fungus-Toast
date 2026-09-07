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

        var proposals = EnumerateProposals(plan, parentGenes).ToList();
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

        return EnumerateProposals(plan, CandidateGenomeFactory.ExtractGenes(parameterized)).Count();
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

    private static IEnumerable<CandidateProposal> EnumerateProposals(CandidateGenerationPlan plan, CandidateGeneSet parentGenes)
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
    /// Goals that transitively require <paramref name="mutationId"/>, and so become unreachable
    /// when it is blocked.
    /// </summary>
    private static IReadOnlyList<int> FindGoalsGatedBehind(int mutationId, IReadOnlyList<int> remainingGoalIds)
    {
        var blocked = new HashSet<int> { mutationId };
        // Repeat until nothing new is blocked: a goal behind a blocked goal is blocked as well.
        bool grew;
        do
        {
            grew = false;
            foreach (var goalId in remainingGoalIds)
            {
                if (blocked.Contains(goalId)) continue;
                var mutation = MutationRegistry.GetById(goalId);
                if (mutation == null) continue;
                if (!mutation.Prerequisites.Any(prerequisite => blocked.Contains(prerequisite.MutationId))) continue;
                blocked.Add(goalId);
                grew = true;
            }
        } while (grew);

        return blocked.Where(id => id != mutationId).OrderBy(id => id).ToList();
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
