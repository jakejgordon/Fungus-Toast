using FungusToast.Simulation.Experiments;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// The staged ladder a candidate climbs. Static and Characterization cost no games and are run by
/// the generator and <see cref="CandidateCharacterizationGate"/>; the rest each emit a paired
/// experiment manifest.
/// </summary>
public enum CandidateEvaluationStage
{
    Static,
    Characterization,
    Smoke,
    Calibration,
    Comparison,
    Holdout
}

/// <summary>
/// One board and seed schedule a stage runs in.
/// </summary>
public sealed class CandidateEvaluationContext
{
    public required int BoardWidth { get; init; }

    public required int BoardHeight { get; init; }

    public required int BaseSeed { get; init; }
}

/// <summary>
/// Versioned description of how one published candidate is evaluated against its parent.
///
/// The shape is fixed rather than configurable: a paired two-artifact swap, two players against
/// one fixed opponent. The control arm runs the parent and the treatment arm runs the candidate in
/// the same slot, matched on seeds, slots, board, and RNG controls and sharing one pairing group,
/// so the difference between arms is exactly one strategy.
/// </summary>
public sealed class CandidateEvaluationPlan
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-candidate-evaluation.v1";

    /// <summary>Smoke allows 3-5 games; the rest are fixed by the staged evidence gates.</summary>
    public const int DefaultSmokeGames = 5;

    public required string SchemaVersion { get; init; }

    public required string EvaluationId { get; init; }

    public required string Purpose { get; init; }

    /// <summary>Display name of a candidate already published in the generated catalog.</summary>
    public required string CandidateDisplayName { get; init; }

    /// <summary>The parent control, published as a reference in the generated catalog.</summary>
    public required string ParentStrategyName { get; init; }

    /// <summary>The fixed opponent, present unchanged in both arms.</summary>
    public required string OpponentStrategyName { get; init; }

    /// <summary>Board and seed for smoke, calibration, and comparison.</summary>
    public required CandidateEvaluationContext ScreeningContext { get; init; }

    /// <summary>
    /// Board and seed for the holdout, which must differ from the screening context in both
    /// geometry and seed so a passing candidate cannot have been selected on the context it is
    /// finally judged in.
    /// </summary>
    public required CandidateEvaluationContext HoldoutContext { get; init; }

    /// <summary>
    /// The advancement threshold for decision-bearing stages, as a paired mean difference in
    /// normalized board share. Frozen before the run, never fitted afterwards.
    /// </summary>
    public required double Margin { get; init; }

    public int SmokeGames { get; init; } = DefaultSmokeGames;

    /// <summary>
    /// Seconds allowed for a stage's two arms combined. The analyzer refuses a verdict whose
    /// measured runtime exceeded this, so it is a real gate rather than a comment.
    /// </summary>
    public required double RuntimeBudgetSecondsPerStage { get; init; }

    public ExperimentDirection Direction { get; init; } = ExperimentDirection.Increase;
}
