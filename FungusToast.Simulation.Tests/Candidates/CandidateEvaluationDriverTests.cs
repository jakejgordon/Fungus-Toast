using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

[Collection(GeneratedCatalogCollection.Name)]
public sealed class CandidateEvaluationDriverTests : IDisposable
{
    private const string ParentName = "TST_BalancedGeneralistControl";
    private const string OpponentName = "TST_RebirthAttrition";

    public void Dispose() => GeneratedCandidateCatalog.Clear();

    /// <summary>
    /// The whole point of the driver: a candidate that keeps winning climbs every stage without
    /// anyone touching it.
    /// </summary>
    [Fact]
    public void ACandidateThatKeepsWinning_ClimbsToTheHoldoutUnattended()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var driver = CreateDriver(_ => Arm(ok: true), Analysis(0.12, 0.08, 0.16, "supported"));

        var report = driver.Run(plan, queue, "catalog.json");

        Assert.Equal(
            new[]
            {
                CandidateEvaluationStage.Smoke, CandidateEvaluationStage.Calibration,
                CandidateEvaluationStage.Comparison, CandidateEvaluationStage.Holdout
            },
            report.Steps.Select(step => step.Stage));
        Assert.All(report.Steps, step => Assert.Equal(CandidateStageResult.Passed, step.Result));
        Assert.Equal(CandidateQueueStatus.Passed, queue.Entries[0].Status);
        // 10 + 40 + 100 + 200 across both arms of each stage.
        Assert.Equal(350, report.GamesConsumed);
    }

    /// <summary>
    /// A failed run says nothing about the candidate, so it must be an integrity failure and be
    /// retried rather than counted against it.
    /// </summary>
    [Fact]
    public void AFailedArm_IsAnIntegrityFailureAndIsRetried()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var attempts = 0;
        var driver = CreateDriver(
            _ => Arm(ok: ++attempts > 2),
            Analysis(0.12, 0.08, 0.16, "supported"));

        var report = driver.Run(plan, queue, "catalog.json", maximumSteps: 2);

        Assert.Equal(CandidateStageResult.FailedIntegrity, report.Steps[0].Result);
        Assert.Contains("Arm failed", report.Steps[0].Detail, StringComparison.Ordinal);
        Assert.Equal(CandidateStageResult.Passed, report.Steps[1].Result);
        Assert.Equal(CandidateEvaluationStage.Smoke, report.Steps[1].Stage);
    }

    /// <summary>
    /// A lost verdict is an answer, not an accident, so the candidate stops immediately.
    /// </summary>
    [Fact]
    public void AnUnsupportedVerdict_StopsTheCandidateOnEvidence()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        // The interval stays wide enough to survive futility pruning, so the candidate reaches
        // comparison and is stopped by the verdict rather than by the interval.
        var driver = CreateDriver(
            _ => Arm(ok: true),
            Analysis(0.02, -0.06, 0.20, "not_supported"));

        var report = driver.Run(plan, queue, "catalog.json");

        Assert.Equal(CandidateEvaluationStage.Comparison, report.Steps.Last().Stage);
        Assert.Equal(CandidateStageResult.FailedEvidence, report.Steps.Last().Result);
        Assert.Equal(CandidateQueueStatus.StoppedOnEvidence, queue.Entries[0].Status);
        // Smoke and calibration passed first; the holdout was never reached.
        Assert.Equal(3, report.Steps.Count);
    }

    /// <summary>
    /// A candidate whose calibration interval cannot reach the margin is pruned there rather than
    /// being carried into a hundred-game comparison to learn the same thing. Pruning deliberately
    /// ignores smoke, whose few games per arm make its interval noise.
    /// </summary>
    [Fact]
    public void ACandidateThatCannotReachTheMargin_IsPrunedAtCalibrationNotAtSmoke()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var driver = CreateDriver(_ => Arm(ok: true), Analysis(-0.02, -0.06, 0.02, verdict: null));

        var report = driver.Run(plan, queue, "catalog.json");

        Assert.Equal(
            new[] { CandidateEvaluationStage.Smoke, CandidateEvaluationStage.Calibration },
            report.Steps.Select(step => step.Stage));
        Assert.All(report.Steps, step => Assert.Equal(CandidateStageResult.Passed, step.Result));
        Assert.Equal(CandidateQueueStatus.Pruned, queue.Entries[0].Status);
        Assert.Contains("at Calibration", queue.Entries[0].Detail, StringComparison.Ordinal);
        // Smoke plus calibration only; the 100-game comparison was never funded.
        Assert.Equal(50, report.GamesConsumed);
    }

    /// <summary>
    /// Calibration has no verdict. It exists to catch a clear regression, which means the entire
    /// interval below the negative margin - not merely a weak point estimate.
    /// </summary>
    [Fact]
    public void CalibrationStopsAClearRegression_ButToleratesAWeakResult()
    {
        var (regressionPlan, regressionQueue) = CreateEvaluation(candidateLimit: 1);
        var regressionDriver = CreateDriver(_ => Arm(ok: true), Analysis(-0.30, -0.40, -0.20, verdict: null));
        var regression = regressionDriver.Run(regressionPlan, regressionQueue, "catalog.json");

        Assert.Equal(CandidateEvaluationStage.Calibration, regression.Steps.Last().Stage);
        Assert.Equal(CandidateStageResult.FailedEvidence, regression.Steps.Last().Result);
        Assert.Contains("Clear regression", regression.Steps.Last().Detail, StringComparison.Ordinal);

        var (weakPlan, weakQueue) = CreateEvaluation(candidateLimit: 1);
        var weakDriver = CreateDriver(_ => Arm(ok: true), Analysis(-0.01, -0.06, 0.04, verdict: null));
        var weak = weakDriver.Run(weakPlan, weakQueue, "catalog.json", maximumSteps: 2);

        Assert.All(weak.Steps, step => Assert.Equal(CandidateStageResult.Passed, step.Result));
    }

    /// <summary>
    /// Comparison and holdout are decision-bearing. If no verdict came back, the stage did not
    /// answer its question, which is an integrity problem rather than a loss.
    /// </summary>
    [Fact]
    public void AMissingVerdictAtADecisionStage_IsAnIntegrityFailure()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var driver = CreateDriver(_ => Arm(ok: true), Analysis(0.12, 0.08, 0.16, verdict: null));

        var report = driver.Run(plan, queue, "catalog.json", maximumSteps: 3);

        var comparison = report.Steps.Last();
        Assert.Equal(CandidateEvaluationStage.Comparison, comparison.Stage);
        Assert.Equal(CandidateStageResult.FailedIntegrity, comparison.Result);
        Assert.Contains("requires a preregistered verdict", comparison.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AFailedAnalysis_IsAnIntegrityFailure()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var driver = CreateDriver(
            _ => Arm(ok: true),
            (_, _) => new CandidateStageAnalysis(false, null, null, null, null, "pandas exploded"));

        var report = driver.Run(plan, queue, "catalog.json", maximumSteps: 1);

        Assert.Equal(CandidateStageResult.FailedIntegrity, report.Steps[0].Result);
        Assert.Contains("pandas exploded", report.Steps[0].Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Each candidate needs its own evaluation ID, or two candidates' artifacts would overwrite
    /// each other.
    /// </summary>
    [Fact]
    public void EachCandidateGetsItsOwnArtifactIdentity()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 2);
        var experimentIds = new List<string>();
        var driver = CreateDriver(
            arguments =>
            {
                experimentIds.Add(arguments[arguments.ToList().IndexOf("--experiment-id") + 1]);
                return Arm(ok: true);
            },
            Analysis(0.12, 0.08, 0.16, "supported"));

        driver.Run(plan, queue, "catalog.json", maximumSteps: 2);

        // Two candidates, two arms each, all four artifact identities distinct.
        Assert.Equal(4, experimentIds.Count);
        Assert.Equal(4, experimentIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheDriverStops_WhenTheBudgetCannotFundTheNextStage()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1, totalGameBudget: 15);
        var driver = CreateDriver(_ => Arm(ok: true), Analysis(0.12, 0.08, 0.16, "supported"));

        var report = driver.Run(plan, queue, "catalog.json");

        Assert.Single(report.Steps);
        Assert.Equal("Remaining budget cannot fund the next stage.", report.StopReason);
    }

    /// <summary>
    /// Persisting after every step is what makes an interrupted run resumable.
    /// </summary>
    [Fact]
    public void TheQueueIsPersistedAfterEveryStep_SoAnInterruptedRunResumes()
    {
        var (plan, queue) = CreateEvaluation(candidateLimit: 1);
        var path = Path.Combine(Path.GetTempPath(), $"driver-queue-{Guid.NewGuid():N}.json");
        var driver = CreateDriver(_ => Arm(ok: true), Analysis(0.12, 0.08, 0.16, "supported"));

        try
        {
            driver.Run(plan, queue, "catalog.json", path, maximumSteps: 2);

            var resumed = CandidateEvaluationQueueJson.Load(path);
            Assert.Equal(CandidateEvaluationStage.Calibration, resumed.Entries[0].HighestPassedStage);
            Assert.Equal(CandidateEvaluationStage.Comparison, resumed.NextWorkItem()!.Stage);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A swap produces a paired row for the changed slot and another for the unchanged opponent.
    /// Reading whichever came first would sometimes report the opponent's difference.
    /// </summary>
    [Fact]
    public void ThePairedSummaryReader_PicksTheRowWhereTheStrategyActuallyChanged()
    {
        var headers = new[] { "strategy_id_control", "strategy_id_treatment", "paired_difference_normalized_board_share" };
        var rows = new IReadOnlyList<string>[]
        {
            new[] { "opponent", "opponent", "0.0" },
            new[] { "parent", "candidate", "0.42" }
        };

        var selected = ProcessCandidateExecution.SelectSwappedRow(headers, rows);

        Assert.Equal("0.42", selected![2]);
    }

    private static CandidateEvaluationDriver CreateDriver(
        Func<IReadOnlyList<string>, CandidateArmOutcome> runArm,
        Func<string, string, CandidateStageAnalysis> analyze) => new(runArm, analyze);

    private static CandidateArmOutcome Arm(bool ok)
        => new(ok, "artifact", 1.0, ok ? "Completed." : "exit code 1");

    private static Func<string, string, CandidateStageAnalysis> Analysis(
        double estimate,
        double low,
        double high,
        string? verdict)
        => (_, _) => new CandidateStageAnalysis(true, estimate, low, high, verdict, "ok");

    private static (CandidateEvaluationPlan Plan, CandidateEvaluationQueue Queue) CreateEvaluation(
        int candidateLimit,
        int totalGameBudget = 5000)
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, ParentName)!;
        var opponent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, OpponentName)!;

        var candidates = CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "driver",
            Purpose = "Drive a bounded opener-order search unattended.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted.Take(candidateLimit).ToList();

        GeneratedCandidateCatalog.Publish(candidates, new[] { parent, opponent });

        var plan = new CandidateEvaluationPlan
        {
            SchemaVersion = CandidateEvaluationPlan.CurrentSchemaVersion,
            EvaluationId = "eval-driver",
            Purpose = "Does the reordered opener beat Balanced Control?",
            CandidateDisplayName = candidates[0].DisplayName,
            ParentStrategyName = ParentName,
            OpponentStrategyName = OpponentName,
            ScreeningContext = new CandidateEvaluationContext { BoardWidth = 120, BoardHeight = 120, BaseSeed = 2026090640 },
            HoldoutContext = new CandidateEvaluationContext { BoardWidth = 140, BoardHeight = 100, BaseSeed = 2026090641 },
            Margin = 0.05,
            RuntimeBudgetSecondsPerStage = 900
        };

        var queue = CandidateEvaluationQueue.Create("driver-test", candidates, totalGameBudget, 100_000);
        return (plan, queue);
    }
}
