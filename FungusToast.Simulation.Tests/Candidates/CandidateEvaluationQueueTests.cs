using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class CandidateEvaluationQueueTests
{
    /// <summary>
    /// Every candidate clears a cheap stage before any candidate starts an expensive one, so a
    /// field is thinned at 10 games a head rather than 200.
    /// </summary>
    [Fact]
    public void TheQueue_FinishesAStageAcrossTheFieldBeforeAdvancing()
    {
        var queue = CreateQueue(candidateCount: 3);

        var firstPass = new List<CandidateWorkItem>();
        for (var i = 0; i < 3; i++)
        {
            var item = queue.NextWorkItem()!;
            firstPass.Add(item);
            queue.RecordStageResult(item.CandidateId, item.Stage, CandidateStageResult.Passed, item.GameCost, 1);
        }

        Assert.All(firstPass, item => Assert.Equal(CandidateEvaluationStage.Smoke, item.Stage));
        Assert.Equal(3, firstPass.Select(item => item.CandidateId).Distinct().Count());
        Assert.Equal(CandidateEvaluationStage.Calibration, queue.NextWorkItem()!.Stage);
    }

    [Fact]
    public void StageCosts_CoverBothArmsAndNeverExceedTheBatchCeiling()
    {
        Assert.Equal(10, CandidateEvaluationQueue.StageGameCost(CandidateEvaluationStage.Smoke));
        Assert.Equal(200, CandidateEvaluationQueue.StageGameCost(CandidateEvaluationStage.Holdout));

        // The ceiling is per batch, and each arm is its own batch, so a holdout is two runs of 100.
        foreach (var stage in new[]
                 {
                     CandidateEvaluationStage.Smoke, CandidateEvaluationStage.Calibration,
                     CandidateEvaluationStage.Comparison, CandidateEvaluationStage.Holdout
                 })
        {
            Assert.True(CandidateEvaluationQueue.GamesPerArm(stage) <= CandidateEvaluationQueue.MaximumGamesPerBatch);
        }

        Assert.Equal(ExperimentManifest.MaximumGamesPerCondition, CandidateEvaluationQueue.MaximumGamesPerBatch);
    }

    /// <summary>
    /// A stage that cannot fit in the remaining budget is never started, so the queue never leaves
    /// a half-funded comparison behind.
    /// </summary>
    [Fact]
    public void AStageThatCannotFitTheRemainingBudget_IsNotDispatched()
    {
        var queue = CreateQueue(candidateCount: 1, totalGameBudget: 15);

        var smoke = queue.NextWorkItem()!;
        queue.RecordStageResult(smoke.CandidateId, smoke.Stage, CandidateStageResult.Passed, smoke.GameCost, 1);

        // 5 games remain; calibration needs 40.
        Assert.Equal(5, queue.RemainingGameBudget);
        Assert.Null(queue.NextWorkItem());
    }

    /// <summary>
    /// An integrity failure says nothing about the candidate, so it may be retried up to the cap.
    /// </summary>
    [Fact]
    public void IntegrityFailures_AreRetriedUpToTheCapThenStopTheCandidate()
    {
        var queue = CreateQueue(candidateCount: 1, maximumAttemptsPerStage: 2);
        var candidateId = queue.Entries[0].CandidateId;

        queue.RecordStageResult(candidateId, CandidateEvaluationStage.Smoke, CandidateStageResult.FailedIntegrity, 10, 1, "parity failure");
        Assert.Equal(CandidateQueueStatus.Pending, queue.Entries[0].Status);
        Assert.Equal(CandidateEvaluationStage.Smoke, queue.NextWorkItem()!.Stage);

        queue.RecordStageResult(candidateId, CandidateEvaluationStage.Smoke, CandidateStageResult.FailedIntegrity, 10, 1, "parity failure");
        Assert.Equal(CandidateQueueStatus.StoppedOnIntegrity, queue.Entries[0].Status);
        Assert.Contains("Exhausted 2 attempts", queue.Entries[0].Detail, StringComparison.Ordinal);
        Assert.Null(queue.NextWorkItem());
    }

    /// <summary>
    /// The safeguard that matters most: a preregistered question that was answered is never asked
    /// again. Retrying until it passes would turn the staged gates into a search for a favorable
    /// sample.
    /// </summary>
    [Fact]
    public void AnEvidenceFailure_StopsTheCandidateImmediatelyWithNoRetry()
    {
        var queue = CreateQueue(candidateCount: 1, maximumAttemptsPerStage: 5);
        var candidateId = queue.Entries[0].CandidateId;

        queue.RecordStageResult(candidateId, CandidateEvaluationStage.Comparison, CandidateStageResult.FailedEvidence, 100, 30, "not_supported");

        Assert.Equal(CandidateQueueStatus.StoppedOnEvidence, queue.Entries[0].Status);
        Assert.Equal(1, queue.Entries[0].Attempts[CandidateEvaluationStage.Comparison]);
        Assert.Null(queue.NextWorkItem());
    }

    [Fact]
    public void PassingTheHoldout_CompletesTheCandidate()
    {
        var queue = CreateQueue(candidateCount: 1);
        var candidateId = queue.Entries[0].CandidateId;

        foreach (var stage in new[]
                 {
                     CandidateEvaluationStage.Smoke, CandidateEvaluationStage.Calibration,
                     CandidateEvaluationStage.Comparison, CandidateEvaluationStage.Holdout
                 })
        {
            queue.RecordStageResult(candidateId, stage, CandidateStageResult.Passed, CandidateEvaluationQueue.StageGameCost(stage), 5);
        }

        Assert.Equal(CandidateQueueStatus.Passed, queue.Entries[0].Status);
        Assert.Null(queue.NextWorkItem());
    }

    /// <summary>
    /// A candidate whose optimistic bound is under the margin cannot reach the threshold, so more
    /// games can only spend budget.
    /// </summary>
    [Fact]
    public void FutileCandidates_ArePrunedBeforeSpendingMoreBudget()
    {
        var queue = CreateQueue(candidateCount: 2);
        queue.RecordStageResult(
            queue.Entries[0].CandidateId, CandidateEvaluationStage.Calibration, CandidateStageResult.Passed,
            40, 10, estimate: -0.02, ci95Low: -0.09, ci95High: 0.01);
        queue.RecordStageResult(
            queue.Entries[1].CandidateId, CandidateEvaluationStage.Calibration, CandidateStageResult.Passed,
            40, 10, estimate: 0.09, ci95Low: 0.01, ci95High: 0.17);

        var pruned = queue.PruneFutileCandidates(margin: 0.05, ExperimentDirection.Increase);

        Assert.Equal(1, pruned);
        Assert.Equal(CandidateQueueStatus.Pruned, queue.Entries[0].Status);
        Assert.Contains("cannot reach the 0.05 threshold", queue.Entries[0].Detail, StringComparison.Ordinal);
        Assert.Equal(CandidateQueueStatus.Pending, queue.Entries[1].Status);
    }

    [Fact]
    public void DominatedCandidates_ArePrunedWhenAnotherIntervalSitsEntirelyAbove()
    {
        var queue = CreateQueue(candidateCount: 2);
        queue.RecordStageResult(
            queue.Entries[0].CandidateId, CandidateEvaluationStage.Calibration, CandidateStageResult.Passed,
            40, 10, estimate: 0.02, ci95Low: 0.00, ci95High: 0.04);
        queue.RecordStageResult(
            queue.Entries[1].CandidateId, CandidateEvaluationStage.Calibration, CandidateStageResult.Passed,
            40, 10, estimate: 0.20, ci95Low: 0.15, ci95High: 0.25);

        var pruned = queue.PruneDominatedCandidates(ExperimentDirection.Increase);

        Assert.Equal(1, pruned);
        Assert.Equal(CandidateQueueStatus.Pruned, queue.Entries[0].Status);
        Assert.Contains("Dominated by", queue.Entries[0].Detail, StringComparison.Ordinal);
        Assert.Equal(CandidateQueueStatus.Pending, queue.Entries[1].Status);
    }

    /// <summary>
    /// Intervals from different game counts are not comparable, so domination only applies within
    /// one stage.
    /// </summary>
    [Fact]
    public void DominationIsNotAppliedAcrossDifferentStages()
    {
        var queue = CreateQueue(candidateCount: 2);
        queue.RecordStageResult(
            queue.Entries[0].CandidateId, CandidateEvaluationStage.Smoke, CandidateStageResult.Passed,
            10, 2, estimate: 0.02, ci95Low: 0.00, ci95High: 0.04);
        queue.RecordStageResult(
            queue.Entries[1].CandidateId, CandidateEvaluationStage.Calibration, CandidateStageResult.Passed,
            40, 10, estimate: 0.20, ci95Low: 0.15, ci95High: 0.25);

        Assert.Equal(0, queue.PruneDominatedCandidates(ExperimentDirection.Increase));
        Assert.All(queue.Entries, entry => Assert.Equal(CandidateQueueStatus.Pending, entry.Status));
    }

    [Fact]
    public void PruningNeverTouchesACandidateThatAlreadyStopped()
    {
        var queue = CreateQueue(candidateCount: 1);
        queue.RecordStageResult(
            queue.Entries[0].CandidateId, CandidateEvaluationStage.Comparison, CandidateStageResult.FailedEvidence,
            100, 30, "not_supported", estimate: -0.2, ci95Low: -0.3, ci95High: -0.1);

        Assert.Equal(0, queue.PruneFutileCandidates(margin: 0.05, ExperimentDirection.Increase));
        Assert.Equal(CandidateQueueStatus.StoppedOnEvidence, queue.Entries[0].Status);
    }

    /// <summary>
    /// The runtime budget is a real gate, not a recorded number, and it projects from the queue's
    /// own measured throughput rather than an authored guess.
    /// </summary>
    [Fact]
    public void AStageProjectedToExceedTheRuntimeBudget_IsNotDispatched()
    {
        var queue = CandidateEvaluationQueue.Create(
            "runtime-test",
            GenerateCandidates().Take(1),
            totalGameBudget: 5000,
            totalRuntimeBudgetSeconds: 60);
        var candidateId = queue.Entries[0].CandidateId;

        // Ten smoke games took 50 seconds, so calibration's forty games project to 200.
        queue.RecordStageResult(candidateId, CandidateEvaluationStage.Smoke, CandidateStageResult.Passed, 10, 50);

        Assert.Equal(200, queue.ProjectedRuntimeSeconds(40));
        Assert.Equal(10, queue.RemainingRuntimeBudgetSeconds);
        Assert.Null(queue.NextWorkItem());
    }

    [Fact]
    public void WithNoMeasuredThroughputYet_TheFirstStageIsStillDispatched()
    {
        var queue = CreateQueue(candidateCount: 1);

        Assert.Equal(0, queue.ProjectedRuntimeSeconds(200));
        Assert.Equal(CandidateEvaluationStage.Smoke, queue.NextWorkItem()!.Stage);
    }

    /// <summary>
    /// Resumability is the point of persisting: a reloaded queue must hand out the same next work
    /// item and carry the same consumed budget.
    /// </summary>
    [Fact]
    public void AQueue_ResumesFromDiskAtTheSameWorkItem()
    {
        var queue = CreateQueue(candidateCount: 2);
        var first = queue.NextWorkItem()!;
        queue.RecordStageResult(first.CandidateId, first.Stage, CandidateStageResult.Passed, first.GameCost, 7.5);

        var resumed = CandidateEvaluationQueueJson.Deserialize(CandidateEvaluationQueueJson.Serialize(queue));

        Assert.Equal(queue.GamesConsumed, resumed.GamesConsumed);
        Assert.Equal(queue.RuntimeSecondsConsumed, resumed.RuntimeSecondsConsumed);
        Assert.Equal(queue.NextWorkItem(), resumed.NextWorkItem());
        Assert.Equal(
            CandidateEvaluationStage.Smoke,
            resumed.Entries.Single(entry => entry.CandidateId == first.CandidateId).HighestPassedStage);
    }

    [Fact]
    public void AQueue_RoundTripsThroughAFile()
    {
        var queue = CreateQueue(candidateCount: 1);
        var path = Path.Combine(Path.GetTempPath(), $"candidate-queue-{Guid.NewGuid():N}.json");

        try
        {
            CandidateEvaluationQueueJson.Save(queue, path);
            var loaded = CandidateEvaluationQueueJson.Load(path);
            Assert.Equal(queue.QueueId, loaded.QueueId);
            Assert.Equal(queue.Entries[0].CandidateId, loaded.Entries[0].CandidateId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AQueueFileFromAnotherSchemaVersion_IsRefused()
    {
        var json = CandidateEvaluationQueueJson.Serialize(CreateQueue(candidateCount: 1))
            .Replace(CandidateEvaluationQueue.CurrentSchemaVersion, "fungus-toast.ai-candidate-queue.v0", StringComparison.Ordinal);

        Assert.Throws<System.Text.Json.JsonException>(() => CandidateEvaluationQueueJson.Deserialize(json));
    }

    [Fact]
    public void RecordingAnUnknownCandidate_Throws()
    {
        var queue = CreateQueue(candidateCount: 1);

        Assert.Throws<ArgumentException>(() => queue.RecordStageResult(
            "candidate.not-in-this-queue.000000000000",
            CandidateEvaluationStage.Smoke,
            CandidateStageResult.Passed,
            10,
            1));
    }

    private static IReadOnlyList<CandidateGenome> GenerateCandidates()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, "TST_BalancedGeneralistControl")!;
        return CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "queue",
            Purpose = "Queue a bounded opener-order and economy search.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap, CandidateOperator.EconomyBiasSweep },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;
    }

    private static CandidateEvaluationQueue CreateQueue(
        int candidateCount,
        int totalGameBudget = 5000,
        int maximumAttemptsPerStage = 2)
    {
        var candidates = GenerateCandidates();
        Assert.True(candidates.Count >= candidateCount, "Not enough candidates generated for this test.");
        return CandidateEvaluationQueue.Create(
            "queue-test",
            candidates.Take(candidateCount),
            totalGameBudget,
            totalRuntimeBudgetSeconds: 10_000,
            maximumAttemptsPerStage: maximumAttemptsPerStage);
    }
}
