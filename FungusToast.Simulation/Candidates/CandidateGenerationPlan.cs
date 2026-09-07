using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Bounded, single-gene mutation operators. Each one derives a finite, ordered set of gene sets
/// from a parent, so a plan's whole output is enumerable before anything runs. No operator is
/// random: the search is exhaustive within its declared bounds, which is what makes generation
/// reproducible without a seed to record.
/// </summary>
public enum CandidateOperator
{
    /// <summary>Swaps each adjacent pair of build goals in turn. Yields goalCount-1 candidates.</summary>
    GoalOrderAdjacentSwap,

    /// <summary>Moves each non-opening goal to the front in turn. Yields goalCount-1 candidates.</summary>
    GoalOrderPromoteToFront,

    /// <summary>Every economy bias, including the parent's own (which dedup then rejects).</summary>
    EconomyBiasSweep,

    /// <summary>Flips the high-tier preference. Yields one candidate.</summary>
    PrioritizeHighTierToggle,

    /// <summary>Every value in the plan's surgeAttemptTurnFrequencyValues.</summary>
    SurgeAttemptTurnFrequencySweep,

    /// <summary>Every value in the plan's startingSporeEdgeOffsetValues.</summary>
    StartingSporeEdgeOffsetSweep,

    /// <summary>Every value in the plan's maxTierValues.</summary>
    MaxTierSweep,

    /// <summary>
    /// One candidate per distinct mutation in the parent's build plan, with that mutation removed
    /// from the plan and blocked outright.
    ///
    /// This is the diagnostic operator: paired against the unmodified parent, the difference is
    /// what that mutation was contributing. It varies two genes rather than one, because blocking a
    /// mutation the strategy is still told to buy would be incoherent - the goal has to go with it.
    /// Read an ablation sweep with a Decrease hypothesis, where "supported" means the mutation
    /// measurably mattered.
    /// </summary>
    AblateTargetGoal,

    /// <summary>
    /// One candidate per mutation the parent actually buys, whether or not it is a declared goal.
    ///
    /// Goal ablation cannot reach a mutation acquired through the fallback path, and some of the
    /// most-bought mutations are exactly that - a strategy's declared plan is not the whole of what
    /// it builds. This operator observes the parent's real build under the characterization script
    /// and blocks one purchase at a time, so a fallback staple can be measured like any goal.
    /// </summary>
    AblateObservedPurchase
}

/// <summary>
/// Versioned, serializable description of one bounded candidate search around a single parent.
///
/// A plan is fully enumerable before execution: every operator has a finite yield determined by
/// the parent's gene set and the plan's declared value lists, so the generator can refuse an
/// oversized search up front rather than discovering it mid-run.
/// </summary>
public sealed class CandidateGenerationPlan
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-candidate-plan.v1";

    /// <summary>Absolute ceiling on any one plan, independent of what the plan asks for.</summary>
    public const int CandidateCeiling = 64;

    public const int MaximumPlanIdLength = 40;

    public required string SchemaVersion { get; init; }

    /// <summary>Appears in every generated display name, so it must stay short and name-safe.</summary>
    public required string PlanId { get; init; }

    public required string Purpose { get; init; }

    public required string ParentStrategyId { get; init; }

    public required StrategySetEnum ParentStrategySet { get; init; }

    /// <summary>Applied in declared order; the order affects only which duplicate is kept first.</summary>
    public required IReadOnlyList<CandidateOperator> Operators { get; init; }

    /// <summary>Hard cap on generated candidates before deduplication and validation.</summary>
    public required int MaximumCandidates { get; init; }

    public IReadOnlyList<int> SurgeAttemptTurnFrequencyValues { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> StartingSporeEdgeOffsetValues { get; init; } = Array.Empty<int>();

    public IReadOnlyList<MutationTier> MaxTierValues { get; init; } = Array.Empty<MutationTier>();
}

/// <summary>
/// Why one generated gene set did not become a candidate. Rejections are returned rather than
/// silently dropped: they are the candidate-search half of the Static evidence stage, which
/// exists to reject invalid or confounded input visibly.
/// </summary>
public enum CandidateRejectionReason
{
    /// <summary>Behaviorally identical to the parent, so it would compare a strategy against itself.</summary>
    DuplicateOfParent,

    /// <summary>Behaviorally identical to a candidate an earlier operator already produced.</summary>
    DuplicateOfEarlierCandidate,

    /// <summary>Behaviorally identical to a strategy already in the registry, so testing it would re-prove a known build.</summary>
    DuplicateOfRegisteredStrategy,

    /// <summary>Structurally or semantically invalid, for example a goal ladder the operator inverted.</summary>
    FailedValidation
}

public sealed record CandidateRejection(
    CandidateOperator Operator,
    int OperatorIndex,
    string GeneFingerprint,
    CandidateRejectionReason Reason,
    string Detail);

/// <summary>
/// Everything one plan produced: the accepted candidates in generation order, and every rejection
/// with the operator that caused it.
/// </summary>
public sealed class CandidateGenerationResult
{
    public required string PlanId { get; init; }

    public required string ParentStrategyId { get; init; }

    /// <summary>The parent's own behavior fingerprint, recorded so a result can be re-checked later.</summary>
    public required string ParentGeneFingerprint { get; init; }

    public required IReadOnlyList<CandidateGenome> Accepted { get; init; }

    public required IReadOnlyList<CandidateRejection> Rejected { get; init; }

    public int GeneratedCount => Accepted.Count + Rejected.Count;
}
