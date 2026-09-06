using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// The bounded set of behavior dimensions a Phase 6 candidate may vary.
/// Every value in <see cref="CandidateGeneSet"/> belongs to exactly one gene so a
/// candidate can declare, and validation can verify, which single dimension it changes.
/// </summary>
public enum CandidateGene
{
    PrioritizeHighTier,
    MaxTier,
    PriorityMutationCategories,
    TargetMutationGoals,
    SurgePriorityIds,
    SurgeAttemptTurnFrequency,
    EconomyBias,
    MycovariantPreferences,
    ExcludedMutationIds,
    StartingSporeEdgeOffset
}

/// <summary>
/// Versioned, serializable description of one generated AI candidate.
///
/// The gene set is stated in full rather than as a sparse diff against the parent, so a
/// genome materializes without consulting the roster and two genomes describing the same
/// behavior always share one fingerprint. Lineage and <see cref="VariedGenes"/> record the
/// relationship to the parent; <see cref="CandidateGenomeValidator"/> enforces that the
/// declared genes are exactly the genes that actually differ, mirroring the declared-difference
/// contract already used by <c>--compare-manifests</c>.
///
/// This is search tooling, not gameplay: nothing here changes Core AI behavior, and a
/// materialized candidate is a plain <see cref="ParameterizedSpendingStrategy"/>.
/// </summary>
public sealed class CandidateGenome
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-candidate-genome.v1";

    /// <summary>Generated candidates are namespaced so they can never collide with a roster name.</summary>
    public const string DisplayNamePrefix = "CAND_";
    public const string CandidateIdPrefix = "candidate.";

    public const int MaximumTargetMutationGoals = 16;
    public const int MaximumSurgePriorityIds = 8;
    public const int MaximumExcludedMutationIds = 12;
    public const int MinimumSurgeAttemptTurnFrequency = 1;
    public const int MaximumSurgeAttemptTurnFrequency = 20;
    // Bounds sit just outside the widest offset any registered strategy uses (-15..+8),
    // so the search space stays finite without rejecting existing roster behavior.
    public const int MinimumStartingSporeEdgeOffset = -20;
    public const int MaximumStartingSporeEdgeOffset = 20;
    public const int MinimumMycovariantPreferencePriority = 0;
    public const int MaximumMycovariantPreferencePriority = 10_000;

    public required string SchemaVersion { get; init; }

    /// <summary>Derived identity: <c>candidate.&lt;parent-slug&gt;.&lt;gene-fingerprint-prefix&gt;</c>.</summary>
    public required string CandidateId { get; init; }

    /// <summary>Strategy name used when this genome is materialized. Must start with <see cref="DisplayNamePrefix"/>.</summary>
    public required string DisplayName { get; init; }

    public required CandidateLineage Lineage { get; init; }

    public required CandidateGeneSet Genes { get; init; }

    /// <summary>Genes this candidate claims to change relative to its parent's gene set.</summary>
    public IReadOnlyList<CandidateGene> VariedGenes { get; init; } = Array.Empty<CandidateGene>();
}

public sealed class CandidateLineage
{
    /// <summary>Stable ID of the parent strategy, e.g. <c>legacy.testing.tst-balancedgeneralistcontrol.v1</c>.</summary>
    public required string ParentStrategyId { get; init; }

    /// <summary>Core's definition fingerprint for the parent at extraction time; a changed parent invalidates the lineage claim.</summary>
    public required string ParentDefinitionFingerprint { get; init; }

    public required StrategySetEnum ParentStrategySet { get; init; }

    /// <summary>Optional human note describing the search step that produced this candidate.</summary>
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// The complete behavior surface of a <see cref="ParameterizedSpendingStrategy"/> excluding its
/// name. Every constructor parameter that Core's definition fingerprint reads has a gene here,
/// which the round-trip test proves against the whole registered roster.
/// </summary>
public sealed class CandidateGeneSet
{
    public required bool PrioritizeHighTier { get; init; }

    public required MutationTier MaxTier { get; init; }

    /// <summary>
    /// Null means the strategy did not restrict categories, which Core expands to every category.
    /// An empty list is a different, deliberate value: no category priority at all. Order is
    /// preserved because it seeds Core's stable category shuffle.
    /// </summary>
    public IReadOnlyList<MutationCategory>? PriorityMutationCategories { get; init; }

    /// <summary>Ordered build plan. Position is the proven-safe search dimension from the Phase 6 opener-order holdout.</summary>
    public IReadOnlyList<CandidateMutationGoal> TargetMutationGoals { get; init; } = Array.Empty<CandidateMutationGoal>();

    /// <summary>Ordered surge activation preference. Every ID must be a <see cref="MutationCategory.MycelialSurges"/> mutation.</summary>
    public IReadOnlyList<int> SurgePriorityIds { get; init; } = Array.Empty<int>();

    public required int SurgeAttemptTurnFrequency { get; init; }

    public required EconomyBias EconomyBias { get; init; }

    /// <summary>Draft preferences in descending priority, which is also Core's evaluation order.</summary>
    public IReadOnlyList<CandidateMycovariantPreference> MycovariantPreferences { get; init; } = Array.Empty<CandidateMycovariantPreference>();

    /// <summary>Mutations this candidate may never buy, including through free upgrades. Order is not behavioral.</summary>
    public IReadOnlyList<int> ExcludedMutationIds { get; init; } = Array.Empty<int>();

    public required int StartingSporeEdgeOffset { get; init; }
}

public sealed class CandidateMutationGoal
{
    public required int MutationId { get; init; }

    /// <summary>Null targets the mutation's maximum level.</summary>
    public int? TargetLevel { get; init; }
}

public sealed class CandidateMycovariantPreference
{
    /// <summary>Interchangeable mycovariants at one priority. Core only membership-tests these, so order is not behavioral.</summary>
    public required IReadOnlyList<int> MycovariantIds { get; init; }

    public required int Priority { get; init; }
}
