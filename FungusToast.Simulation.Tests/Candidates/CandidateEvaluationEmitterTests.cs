using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

/// <summary>
/// Publishes into the process-wide registry, so it shares the generated-catalog collection.
/// </summary>
[Collection(GeneratedCatalogCollection.Name)]
public sealed class CandidateEvaluationEmitterTests : IDisposable
{
    private const string ParentName = "TST_BalancedGeneralistControl";
    private const string OpponentName = "TST_RebirthAttrition";

    private static readonly CandidateEvaluationStage[] AllGameStages =
    {
        CandidateEvaluationStage.Smoke,
        CandidateEvaluationStage.Calibration,
        CandidateEvaluationStage.Comparison,
        CandidateEvaluationStage.Holdout
    };

    public void Dispose() => GeneratedCandidateCatalog.Clear();

    /// <summary>
    /// The point of the emitter: every stage it produces must satisfy the same manifest contract
    /// that governs hand-authored experiments, including the per-stage game counts the frozen
    /// evidence gates enforce.
    /// </summary>
    [Theory]
    [InlineData(CandidateEvaluationStage.Smoke, 5)]
    [InlineData(CandidateEvaluationStage.Calibration, 20)]
    [InlineData(CandidateEvaluationStage.Comparison, 50)]
    [InlineData(CandidateEvaluationStage.Holdout, 100)]
    public void EveryStage_EmitsAValidManifestWithTheGatedGameCount(CandidateEvaluationStage stage, int expectedGames)
    {
        var plan = PublishCastAndCreatePlan();

        var stageManifests = CandidateEvaluationEmitter.Emit(plan, stage, PriorStages(stage));

        foreach (var manifest in new[] { stageManifests.Control, stageManifests.Treatment })
        {
            Assert.Empty(ExperimentManifestValidator.Validate(manifest));
            Assert.Equal(expectedGames, manifest.GamesPerCondition);
            // Both arms declare the combined budget, which the analyzer requires to match.
            Assert.Equal(expectedGames * 2, manifest.TotalGameBudget);
            Assert.Single(manifest.Conditions);
        }
    }

    /// <summary>
    /// A paired swap is only interpretable if the two arms differ in exactly one strategy. Anything
    /// else that changed would be attributed to the candidate.
    /// </summary>
    [Theory]
    [InlineData(CandidateEvaluationStage.Smoke)]
    [InlineData(CandidateEvaluationStage.Holdout)]
    public void ControlAndTreatment_DifferOnlyInTheSwappedStrategy(CandidateEvaluationStage stage)
    {
        var plan = PublishCastAndCreatePlan();

        var stageManifests = CandidateEvaluationEmitter.Emit(plan, stage, PriorStages(stage));
        var control = stageManifests.Control.Conditions[0];
        var treatment = stageManifests.Treatment.Conditions[0];

        Assert.Equal(control.PairingGroupId, treatment.PairingGroupId);
        Assert.Equal(control.PlayerCount, treatment.PlayerCount);
        Assert.Equal(control.Board.Width, treatment.Board.Width);
        Assert.Equal(control.Board.Height, treatment.Board.Height);
        Assert.Equal(control.SlotAssignmentPolicy, treatment.SlotAssignmentPolicy);
        Assert.Equal(control.Strategies.StrategySet, treatment.Strategies.StrategySet);
        Assert.Equal(
            new[] { ParentName, OpponentName },
            control.Strategies.ExplicitStrategyNames);
        Assert.Equal(
            new[] { plan.CandidateDisplayName, OpponentName },
            treatment.Strategies.ExplicitStrategyNames);

        Assert.Equal(stageManifests.Control.BaseSeed, stageManifests.Treatment.BaseSeed);
        // Each arm is its own single-condition run, so their artifact folders differ by experiment
        // ID alone - which is what gives the analyzer two folders to pair.
        Assert.NotEqual(
            ExperimentArtifactId.Derive(stageManifests.Control.ExperimentId, control.ConditionId, isBatchMode: false),
            ExperimentArtifactId.Derive(stageManifests.Treatment.ExperimentId, treatment.ConditionId, isBatchMode: false));
    }

    /// <summary>
    /// The verdict resolves its target row by matching the control and treatment strategy IDs, so
    /// a swap must name both. Naming only the candidate would resolve to no rows.
    /// </summary>
    [Theory]
    [InlineData(CandidateEvaluationStage.Comparison)]
    [InlineData(CandidateEvaluationStage.Holdout)]
    public void DecisionBearingStages_PreregisterBothSidesOfTheSwap(CandidateEvaluationStage stage)
    {
        var plan = PublishCastAndCreatePlan();

        var stageManifests = CandidateEvaluationEmitter.Emit(plan, stage, PriorStages(stage));
        var hypothesis = Assert.IsType<ExperimentHypothesis>(stageManifests.Control.Analysis.Hypothesis);
        Assert.Same(stageManifests.Control.Analysis.Hypothesis, stageManifests.Treatment.Analysis.Hypothesis);

        var candidateId = StrategyRegistry.GetDefinition(StrategySetEnum.Generated, plan.CandidateDisplayName)!.StrategyId;
        var parentId = StrategyRegistry.GetDefinition(StrategySetEnum.Generated, ParentName)!.StrategyId;
        Assert.Equal(candidateId, hypothesis.TargetStrategyId);
        Assert.Equal(parentId, hypothesis.ControlStrategyId);
        Assert.Equal(ExperimentEstimand.PairedMeanDifference, hypothesis.Estimand);
        Assert.Equal(plan.Margin, hypothesis.Margin);
        // The analyzer refuses a verdict whose declared context does not equal the pairing group.
        Assert.Equal(stageManifests.PairingGroupId, hypothesis.PrimaryContextId);
    }

    /// <summary>
    /// Smoke and calibration exist to catch integrity failures cheaply; the frozen gates forbid a
    /// decision-bearing plan below comparison.
    /// </summary>
    [Theory]
    [InlineData(CandidateEvaluationStage.Smoke)]
    [InlineData(CandidateEvaluationStage.Calibration)]
    public void ScreeningStages_CarryNoHypothesis(CandidateEvaluationStage stage)
    {
        var plan = PublishCastAndCreatePlan();

        var stageManifests = CandidateEvaluationEmitter.Emit(plan, stage, PriorStages(stage));

        Assert.Null(stageManifests.Control.Analysis.Hypothesis);
        Assert.Null(stageManifests.Treatment.Analysis.Hypothesis);
        Assert.Empty(ExperimentManifestValidator.Validate(stageManifests.Control));
        Assert.Empty(ExperimentManifestValidator.Validate(stageManifests.Treatment));
    }

    [Fact]
    public void HoldoutRunsInTheUnseenContext_AndScreeningStagesDoNot()
    {
        var plan = PublishCastAndCreatePlan();

        var comparison = CandidateEvaluationEmitter.Emit(plan, CandidateEvaluationStage.Comparison, PriorStages(CandidateEvaluationStage.Comparison));
        var holdout = CandidateEvaluationEmitter.Emit(plan, CandidateEvaluationStage.Holdout, PriorStages(CandidateEvaluationStage.Holdout));

        Assert.Equal(plan.ScreeningContext.BaseSeed, comparison.Control.BaseSeed);
        Assert.Equal(plan.ScreeningContext.BoardWidth, comparison.Control.Conditions[0].Board.Width);
        Assert.Equal(plan.HoldoutContext.BaseSeed, holdout.Control.BaseSeed);
        Assert.Equal(plan.HoldoutContext.BoardWidth, holdout.Control.Conditions[0].Board.Width);
        Assert.NotEqual(comparison.Control.BaseSeed, holdout.Control.BaseSeed);
    }

    [Fact]
    public void SkippingAnEarlierStage_IsRefused()
    {
        var plan = PublishCastAndCreatePlan();

        var exception = Assert.Throws<InvalidOperationException>(() => CandidateEvaluationEmitter.Emit(
            plan,
            CandidateEvaluationStage.Holdout,
            new[] { CandidateEvaluationStage.Static, CandidateEvaluationStage.Characterization, CandidateEvaluationStage.Smoke }));

        Assert.Contains("before Calibration, Comparison has passed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmittingWithoutCharacterizationHavingPassed_IsRefused()
    {
        var plan = PublishCastAndCreatePlan();

        var exception = Assert.Throws<InvalidOperationException>(() => CandidateEvaluationEmitter.Emit(
            plan,
            CandidateEvaluationStage.Smoke,
            new[] { CandidateEvaluationStage.Static }));

        Assert.Contains("Characterization", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CandidateEvaluationStage.Static)]
    [InlineData(CandidateEvaluationStage.Characterization)]
    public void ZeroGameStages_EmitNoManifest(CandidateEvaluationStage stage)
    {
        var plan = PublishCastAndCreatePlan();

        var exception = Assert.Throws<ArgumentException>(
            () => CandidateEvaluationEmitter.Emit(plan, stage, AllGameStages));
        Assert.Contains("costs no games", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A holdout that reuses the selection board or seed is not an unseen context; it re-measures
    /// what the candidate was chosen on and reports it as independent confirmation.
    /// </summary>
    [Fact]
    public void HoldoutReusingTheScreeningBoard_FailsValidation()
    {
        var plan = PublishCastAndCreatePlan();
        var reused = ClonePlan(plan, holdout: new CandidateEvaluationContext
        {
            BoardWidth = plan.ScreeningContext.BoardWidth,
            BoardHeight = plan.ScreeningContext.BoardHeight,
            BaseSeed = plan.HoldoutContext.BaseSeed
        });

        Assert.Contains(
            CandidateEvaluationPlanValidator.Validate(reused),
            error => error.Contains("must use a different board", StringComparison.Ordinal));
    }

    [Fact]
    public void HoldoutReusingTheScreeningSeed_FailsValidation()
    {
        var plan = PublishCastAndCreatePlan();
        var reused = ClonePlan(plan, holdout: new CandidateEvaluationContext
        {
            BoardWidth = plan.HoldoutContext.BoardWidth,
            BoardHeight = plan.HoldoutContext.BoardHeight,
            BaseSeed = plan.ScreeningContext.BaseSeed
        });

        Assert.Contains(
            CandidateEvaluationPlanValidator.Validate(reused),
            error => error.Contains("different base seed", StringComparison.Ordinal));
    }

    [Fact]
    public void APlanNamingAnUnpublishedCandidate_FailsValidation()
    {
        var plan = PublishCastAndCreatePlan();
        GeneratedCandidateCatalog.Clear();

        Assert.Contains(
            CandidateEvaluationPlanValidator.Validate(plan),
            error => error.Contains("is not published in the generated catalog", StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<CandidateEvaluationStage> PriorStages(CandidateEvaluationStage stage)
        => CandidateEvaluationEmitter.RequiredPriorStages(stage).ToList();

    private static CandidateEvaluationPlan PublishCastAndCreatePlan()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, ParentName)!;
        var opponent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, OpponentName)!;

        var candidates = CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "evalemit",
            Purpose = "Emit a staged evaluation for an opener-order candidate.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;

        GeneratedCandidateCatalog.Publish(candidates, new[] { parent, opponent });

        return new CandidateEvaluationPlan
        {
            SchemaVersion = CandidateEvaluationPlan.CurrentSchemaVersion,
            EvaluationId = "eval-opener-01",
            Purpose = "Does the reordered opener beat Balanced Control against a fixed opponent?",
            CandidateDisplayName = candidates[0].DisplayName,
            ParentStrategyName = ParentName,
            OpponentStrategyName = OpponentName,
            ScreeningContext = new CandidateEvaluationContext { BoardWidth = 120, BoardHeight = 120, BaseSeed = 2026090610 },
            HoldoutContext = new CandidateEvaluationContext { BoardWidth = 140, BoardHeight = 100, BaseSeed = 2026090611 },
            Margin = 0.05,
            RuntimeBudgetSecondsPerStage = 900
        };
    }

    private static CandidateEvaluationPlan ClonePlan(CandidateEvaluationPlan plan, CandidateEvaluationContext holdout) => new()
    {
        SchemaVersion = plan.SchemaVersion,
        EvaluationId = plan.EvaluationId,
        Purpose = plan.Purpose,
        CandidateDisplayName = plan.CandidateDisplayName,
        ParentStrategyName = plan.ParentStrategyName,
        OpponentStrategyName = plan.OpponentStrategyName,
        ScreeningContext = plan.ScreeningContext,
        HoldoutContext = holdout,
        Margin = plan.Margin,
        RuntimeBudgetSecondsPerStage = plan.RuntimeBudgetSecondsPerStage
    };
}
