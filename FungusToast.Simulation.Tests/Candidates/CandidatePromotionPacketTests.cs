using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

[Collection(GeneratedCatalogCollection.Name)]
public sealed class CandidatePromotionPacketTests : IDisposable
{
    private const string ParentName = "TST_BalancedGeneralistControl";
    private const string OpponentName = "TST_RebirthAttrition";

    public void Dispose() => GeneratedCandidateCatalog.Clear();

    /// <summary>
    /// A candidate that cleared every stage in two consistent contexts has nothing mechanical in
    /// its way - which the packet states without ever recommending promotion.
    /// </summary>
    [Fact]
    public void AFullyClearedCandidate_IsMechanicallyEligibleButNotRecommended()
    {
        var packet = BuildPacket(RunToHoldout);

        Assert.True(packet.IsMechanicallyEligible);
        Assert.Empty(packet.Blockers);

        var markdown = CandidatePromotionPacketMarkdown.Render(packet);
        Assert.Contains("This is not a recommendation to promote", markdown, StringComparison.Ordinal);
        Assert.Contains("Promotion remains a reviewed decision", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void ACandidateThatNeverReachedTheHoldout_IsBlocked()
    {
        var packet = BuildPacket((queue, candidateId) =>
            Measure(queue, candidateId, CandidateEvaluationStage.Comparison, 0.20, 0.15, 0.25));

        Assert.False(packet.IsMechanicallyEligible);
        Assert.Contains(packet.Blockers, blocker => blocker.Contains("holdout stage has not passed", StringComparison.Ordinal));
        Assert.Contains(packet.Blockers, blocker => blocker.Contains("Robustness is not demonstrated", StringComparison.Ordinal));
    }

    /// <summary>
    /// An advantage that reverses between contexts did not transfer, and that must block eligibility
    /// even though every stage technically passed.
    /// </summary>
    [Fact]
    public void AnAdvantageThatReversedBetweenContexts_BlocksEligibility()
    {
        var packet = BuildPacket((queue, candidateId) =>
        {
            Measure(queue, candidateId, CandidateEvaluationStage.Smoke, 0.10, 0.05, 0.15);
            Measure(queue, candidateId, CandidateEvaluationStage.Calibration, 0.10, 0.05, 0.15);
            Measure(queue, candidateId, CandidateEvaluationStage.Comparison, 0.20, 0.15, 0.25);
            Measure(queue, candidateId, CandidateEvaluationStage.Holdout, -0.18, -0.24, -0.12);
        });

        Assert.False(packet.IsMechanicallyEligible);
        Assert.Contains(packet.Blockers, blocker => blocker.Contains("reversed sign between contexts", StringComparison.Ordinal));
    }

    [Fact]
    public void ThePacket_DiffsExactlyTheGenesThatChanged()
    {
        var packet = BuildPacket(RunToHoldout);

        var difference = Assert.Single(packet.GeneDiff);
        Assert.Equal(CandidateGene.TargetMutationGoals, difference.Gene);
        Assert.NotEqual(difference.ParentValue, difference.CandidateValue);
    }

    /// <summary>
    /// The packet must name the artifacts behind each stage, or a reviewer cannot check the claim.
    /// </summary>
    [Fact]
    public void ThePacket_NamesBothArtifactsAndTheContextForEveryAttemptedStage()
    {
        var packet = BuildPacket(RunToHoldout);

        Assert.Equal(4, packet.Stages.Count);
        var holdout = packet.Stages.Single(stage => stage.Stage == CandidateEvaluationStage.Holdout);
        Assert.Equal("eval-packet_holdout_control", holdout.ControlExperimentId);
        Assert.Equal("eval-packet_holdout_treatment", holdout.TreatmentExperimentId);
        Assert.Equal(100, holdout.GamesPerArm);
        // The holdout runs in the unseen context, not the screening one.
        Assert.Equal("140x100 seed 2026090631", holdout.Context);
        Assert.Equal("120x120 seed 2026090630",
            packet.Stages.Single(stage => stage.Stage == CandidateEvaluationStage.Comparison).Context);
    }

    [Fact]
    public void RetriedStages_AreRecordedAsFailures()
    {
        var packet = BuildPacket((queue, candidateId) =>
        {
            queue.RecordStageResult(candidateId, CandidateEvaluationStage.Smoke, CandidateStageResult.FailedIntegrity, 10, 1, "parity failure");
            RunToHoldout(queue, candidateId);
        });

        Assert.Contains(packet.Failures, failure => failure.Contains("Smoke needed 2 attempts", StringComparison.Ordinal));
    }

    [Fact]
    public void TheRenderedPacket_CarriesTheMeasuresAndTheDiff()
    {
        var markdown = CandidatePromotionPacketMarkdown.Render(BuildPacket(RunToHoldout));

        Assert.Contains("## Definition diff", markdown, StringComparison.Ordinal);
        Assert.Contains("TargetMutationGoals", markdown, StringComparison.Ordinal);
        Assert.Contains("Lineage fidelity", markdown, StringComparison.Ordinal);
        Assert.Contains("Behavioral diversity", markdown, StringComparison.Ordinal);
        Assert.Contains("## Stages and artifacts", markdown, StringComparison.Ordinal);
        Assert.Contains("eval-packet_holdout_treatment", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildingAPacketForACandidateOutsideTheQueue_Throws()
    {
        var (plan, queue, genomes, parent) = CreateEvaluation();
        var stranger = genomes.Values.First();
        var emptyQueue = CandidateEvaluationQueue.Create("empty", Array.Empty<CandidateGenome>(), 100, 100);
        var ranking = CandidateRanking.Rank(queue, genomes, parent.Strategy)[0];

        Assert.Throws<ArgumentException>(() => CandidatePromotionPacket.Build(plan, emptyQueue, stranger, parent, ranking));
    }

    private static void RunToHoldout(CandidateEvaluationQueue queue, string candidateId)
    {
        Measure(queue, candidateId, CandidateEvaluationStage.Smoke, 0.10, 0.04, 0.16);
        Measure(queue, candidateId, CandidateEvaluationStage.Calibration, 0.12, 0.06, 0.18);
        Measure(queue, candidateId, CandidateEvaluationStage.Comparison, 0.14, 0.09, 0.19);
        Measure(queue, candidateId, CandidateEvaluationStage.Holdout, 0.11, 0.07, 0.15);
    }

    private static void Measure(
        CandidateEvaluationQueue queue,
        string candidateId,
        CandidateEvaluationStage stage,
        double estimate,
        double low,
        double high)
    {
        queue.RecordStageResult(
            candidateId, stage, CandidateStageResult.Passed,
            CandidateEvaluationQueue.StageGameCost(stage), 5,
            estimate: estimate, ci95Low: low, ci95High: high);
    }

    private static CandidatePromotionPacket BuildPacket(Action<CandidateEvaluationQueue, string> run)
    {
        var (plan, queue, genomes, parent) = CreateEvaluation();
        var candidateId = queue.Entries[0].CandidateId;
        run(queue, candidateId);

        var ranking = CandidateRanking.Rank(queue, genomes, parent.Strategy)
            .Single(row => row.CandidateId == candidateId);
        return CandidatePromotionPacket.Build(plan, queue, genomes[candidateId], parent, ranking);
    }

    private static (CandidateEvaluationPlan Plan,
        CandidateEvaluationQueue Queue,
        IReadOnlyDictionary<string, CandidateGenome> Genomes,
        StrategyDefinition Parent) CreateEvaluation()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, ParentName)!;
        var opponent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, OpponentName)!;

        var candidates = CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "packet",
            Purpose = "Assemble a promotion packet for an opener-order candidate.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;

        GeneratedCandidateCatalog.Publish(candidates, new[] { parent, opponent });

        var plan = new CandidateEvaluationPlan
        {
            SchemaVersion = CandidateEvaluationPlan.CurrentSchemaVersion,
            EvaluationId = "eval-packet",
            Purpose = "Does the reordered opener beat Balanced Control?",
            CandidateDisplayName = candidates[0].DisplayName,
            ParentStrategyName = ParentName,
            OpponentStrategyName = OpponentName,
            ScreeningContext = new CandidateEvaluationContext { BoardWidth = 120, BoardHeight = 120, BaseSeed = 2026090630 },
            HoldoutContext = new CandidateEvaluationContext { BoardWidth = 140, BoardHeight = 100, BaseSeed = 2026090631 },
            Margin = 0.05,
            RuntimeBudgetSecondsPerStage = 900
        };

        var queue = CandidateEvaluationQueue.Create("packet-test", candidates, 5000, 10_000);
        return (plan, queue, candidates.ToDictionary(candidate => candidate.CandidateId, StringComparer.Ordinal), parent);
    }
}
