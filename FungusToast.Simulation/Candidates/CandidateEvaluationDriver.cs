using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>How running one arm of a stage ended.</summary>
public sealed record CandidateArmOutcome(
    bool Succeeded,
    string ArtifactPath,
    double RuntimeSeconds,
    string Detail);

/// <summary>What the paired analyzer reported for one stage.</summary>
public sealed record CandidateStageAnalysis(
    bool Succeeded,
    double? Estimate,
    double? Ci95Low,
    double? Ci95High,
    string? Verdict,
    string Detail);

public sealed record CandidateDriverStep(
    string CandidateId,
    CandidateEvaluationStage Stage,
    CandidateStageResult Result,
    string Detail);

public sealed class CandidateDriverReport
{
    public required IReadOnlyList<CandidateDriverStep> Steps { get; init; }

    public required string StopReason { get; init; }

    public required int GamesConsumed { get; init; }

    public required double RuntimeSecondsConsumed { get; init; }
}

/// <summary>
/// Runs a queue to completion without hand-driving: emit a stage, run both arms, analyze the pair,
/// record the result, prune, persist, repeat.
///
/// Arm execution and analysis are injected rather than hard-wired. The default implementations
/// shell out to the simulator and the Python analyzer, but the part worth testing is the decision
/// logic - what counts as passing, what may be retried, when to stop - and that should not require
/// spawning processes to exercise.
///
/// The mapping from outcome to result is where the frozen evidence gates live:
/// a failed arm or failed analysis is always an integrity failure, because it says nothing about
/// the candidate. Comparison and holdout pass or fail on their preregistered verdict. Calibration
/// has no verdict and exists to catch a clear regression, so it fails only when the whole interval
/// sits below the negative margin. Smoke checks integrity alone and never judges a candidate.
/// </summary>
public sealed class CandidateEvaluationDriver
{
    private readonly Func<IReadOnlyList<string>, CandidateArmOutcome> runArm;
    private readonly Func<string, string, CandidateStageAnalysis> analyzePair;

    public CandidateEvaluationDriver(
        Func<IReadOnlyList<string>, CandidateArmOutcome> runArm,
        Func<string, string, CandidateStageAnalysis> analyzePair)
    {
        this.runArm = runArm ?? throw new ArgumentNullException(nameof(runArm));
        this.analyzePair = analyzePair ?? throw new ArgumentNullException(nameof(analyzePair));
    }

    /// <summary>
    /// Drives the queue until nothing is left, the budget cannot fund the next stage, or
    /// <paramref name="maximumSteps"/> is reached. Persisting after every step is what makes an
    /// interrupted run resumable rather than restartable.
    /// </summary>
    public CandidateDriverReport Run(
        CandidateEvaluationPlan plan,
        CandidateEvaluationQueue queue,
        string candidateCatalogPath,
        string? queueStatePath = null,
        int maximumSteps = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(queue);

        var steps = new List<CandidateDriverStep>();
        var stopReason = "No work remains.";

        for (var step = 0; step < maximumSteps; step++)
        {
            var workItem = queue.NextWorkItem();
            if (workItem == null)
            {
                stopReason = queue.Entries.Any(entry => entry.Status == CandidateQueueStatus.Pending)
                    ? "Remaining budget cannot fund the next stage."
                    : "No work remains.";
                break;
            }

            var stagePlan = PlanForCandidate(plan, queue, workItem.CandidateId);
            var manifests = CandidateEvaluationEmitter.Emit(
                stagePlan,
                workItem.Stage,
                CandidateEvaluationEmitter.RequiredPriorStages(workItem.Stage));

            var control = runArm(CandidateEvaluationEmitter.RenderCommandLine(manifests, isTreatment: false, candidateCatalogPath));
            var treatment = runArm(CandidateEvaluationEmitter.RenderCommandLine(manifests, isTreatment: true, candidateCatalogPath));
            var runtimeSeconds = control.RuntimeSeconds + treatment.RuntimeSeconds;

            CandidateStageResult result;
            string detail;
            CandidateStageAnalysis? analysis = null;

            if (!control.Succeeded || !treatment.Succeeded)
            {
                // A run that did not finish tells us nothing about the candidate.
                result = CandidateStageResult.FailedIntegrity;
                detail = $"Arm failed: {(control.Succeeded ? treatment.Detail : control.Detail)}";
            }
            else
            {
                analysis = analyzePair(control.ArtifactPath, treatment.ArtifactPath);
                (result, detail) = Interpret(workItem.Stage, analysis, stagePlan.Margin);
            }

            queue.RecordStageResult(
                workItem.CandidateId,
                workItem.Stage,
                result,
                workItem.GameCost,
                runtimeSeconds,
                detail,
                analysis?.Estimate,
                analysis?.Ci95Low,
                analysis?.Ci95High);

            steps.Add(new CandidateDriverStep(workItem.CandidateId, workItem.Stage, result, detail));

            // Pruning after each step is what keeps a doomed candidate from consuming the next,
            // more expensive stage.
            queue.PruneFutileCandidates(stagePlan.Margin);
            queue.PruneDominatedCandidates();

            if (!string.IsNullOrWhiteSpace(queueStatePath)) CandidateEvaluationQueueJson.Save(queue, queueStatePath);

            if (step == maximumSteps - 1) stopReason = "Reached the step limit.";
        }

        return new CandidateDriverReport
        {
            Steps = steps,
            StopReason = stopReason,
            GamesConsumed = queue.GamesConsumed,
            RuntimeSecondsConsumed = queue.RuntimeSecondsConsumed
        };
    }

    private static (CandidateStageResult Result, string Detail) Interpret(
        CandidateEvaluationStage stage,
        CandidateStageAnalysis analysis,
        double margin)
    {
        if (!analysis.Succeeded)
            return (CandidateStageResult.FailedIntegrity, $"Analysis failed: {analysis.Detail}");

        switch (stage)
        {
            case CandidateEvaluationStage.Smoke:
                // Smoke proves the pair runs and pairs cleanly. It never judges a candidate.
                return (CandidateStageResult.Passed, "Smoke completed and paired cleanly.");

            case CandidateEvaluationStage.Calibration:
                // No verdict at this stage; it exists to reject a clear regression, which means the
                // entire interval sitting below the negative margin rather than a weak point estimate.
                if (analysis.Ci95High is { } upper && upper < -margin)
                    return (CandidateStageResult.FailedEvidence,
                        $"Clear regression at calibration: interval upper bound {upper:0.####} is below -{margin:0.####}.");
                return (CandidateStageResult.Passed, "Calibration showed no clear regression.");

            case CandidateEvaluationStage.Comparison:
            case CandidateEvaluationStage.Holdout:
                if (string.IsNullOrWhiteSpace(analysis.Verdict))
                    return (CandidateStageResult.FailedIntegrity,
                        $"{stage} requires a preregistered verdict and none was produced.");
                return string.Equals(analysis.Verdict, "supported", StringComparison.OrdinalIgnoreCase)
                    ? (CandidateStageResult.Passed, $"{stage} verdict supported.")
                    : (CandidateStageResult.FailedEvidence, $"{stage} verdict {analysis.Verdict}.");

            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage consumes no games.");
        }
    }

    /// <summary>
    /// Each candidate needs its own plan, because the plan names the candidate whose arms are being
    /// emitted. Everything else - contexts, margin, opponent, budgets - is shared.
    /// </summary>
    private static CandidateEvaluationPlan PlanForCandidate(
        CandidateEvaluationPlan plan,
        CandidateEvaluationQueue queue,
        string candidateId)
    {
        var entry = queue.Entries.First(candidate => string.Equals(candidate.CandidateId, candidateId, StringComparison.Ordinal));
        if (string.Equals(entry.DisplayName, plan.CandidateDisplayName, StringComparison.Ordinal)) return plan;

        return new CandidateEvaluationPlan
        {
            SchemaVersion = plan.SchemaVersion,
            // Evaluation IDs must differ per candidate or two candidates' artifacts would collide.
            EvaluationId = BuildEvaluationId(plan.EvaluationId, entry),
            Purpose = plan.Purpose,
            CandidateDisplayName = entry.DisplayName,
            ParentStrategyName = plan.ParentStrategyName,
            OpponentStrategyName = plan.OpponentStrategyName,
            ScreeningContext = plan.ScreeningContext,
            HoldoutContext = plan.HoldoutContext,
            Margin = plan.Margin,
            SmokeGames = plan.SmokeGames,
            RuntimeBudgetSecondsPerStage = plan.RuntimeBudgetSecondsPerStage,
            Direction = plan.Direction
        };
    }

    private static string BuildEvaluationId(string baseEvaluationId, CandidateQueueEntry entry)
    {
        // The candidate fingerprint suffix is already unique and short; reuse it rather than
        // inventing a second identifier for the same candidate.
        var suffix = entry.CandidateId.Split('.').Last();
        var trimmed = baseEvaluationId.Length > CandidateEvaluationPlanValidator.MaximumEvaluationIdLength - suffix.Length - 1
            ? baseEvaluationId[..(CandidateEvaluationPlanValidator.MaximumEvaluationIdLength - suffix.Length - 1)]
            : baseEvaluationId;
        return $"{trimmed}-{suffix}";
    }
}
