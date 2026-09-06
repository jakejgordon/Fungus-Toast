using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class CandidateGeneratorTests
{
    private const string ControlStrategyId = "legacy.testing.tst-balancedgeneralistcontrol.v1";

    /// <summary>
    /// Generation takes no seed, so reproducibility is structural rather than recorded. Running
    /// the same plan twice must produce identical candidate IDs, display names, lineage notes,
    /// and rejections in identical order.
    /// </summary>
    [Fact]
    public void SamePlan_GeneratesIdenticalCandidatesAndRejectionsTwice()
    {
        var first = CandidateGenerator.Generate(CreateGoalOrderPlan());
        var second = CandidateGenerator.Generate(CreateGoalOrderPlan());

        Assert.Equal(
            first.Accepted.Select(candidate => candidate.CandidateId),
            second.Accepted.Select(candidate => candidate.CandidateId));
        Assert.Equal(
            first.Accepted.Select(candidate => candidate.DisplayName),
            second.Accepted.Select(candidate => candidate.DisplayName));
        Assert.Equal(
            first.Accepted.Select(candidate => candidate.Lineage.Notes),
            second.Accepted.Select(candidate => candidate.Lineage.Notes));
        Assert.Equal(first.Rejected, second.Rejected);
    }

    [Fact]
    public void EveryAcceptedCandidate_ValidatesAndVariesExactlyOneGene()
    {
        var result = CandidateGenerator.Generate(CreateBroadPlan());
        Assert.NotEmpty(result.Accepted);

        foreach (var candidate in result.Accepted)
        {
            Assert.Empty(CandidateGenomeValidator.Validate(candidate));
            // Every operator changes one gene, so a multi-gene candidate would mean an operator
            // leaked a second change and the comparison would no longer be single-variable.
            Assert.Single(candidate.VariedGenes);
        }
    }

    [Fact]
    public void AdjacentSwapOnFourGoals_YieldsThreeOrderingsAndKeepsTheTwoThatAreNew()
    {
        var result = CandidateGenerator.Generate(CreateGoalOrderPlan());

        // The control has four build goals, so adjacent swaps yield exactly three orderings.
        // Swapping the first pair rebuilds TST_BalancedControl_AnabolicFirst, which already
        // exists, so only the other two survive as candidates worth spending budget on.
        Assert.Equal(3, result.GeneratedCount);
        Assert.Equal(2, result.Accepted.Count);
        Assert.All(result.Accepted, candidate =>
            Assert.Equal(new[] { CandidateGene.TargetMutationGoals }, candidate.VariedGenes));
        Assert.Equal(2, result.Accepted.Select(candidate => candidate.CandidateId).Distinct().Count());

        var rejection = Assert.Single(result.Rejected);
        Assert.Equal(CandidateRejectionReason.DuplicateOfRegisteredStrategy, rejection.Reason);
        Assert.Equal(0, rejection.OperatorIndex);
    }

    /// <summary>
    /// The economy sweep deliberately includes the parent's own bias so the rejection is recorded
    /// rather than the value being quietly skipped.
    /// </summary>
    [Fact]
    public void EconomySweep_RejectsTheParentsOwnBiasAsADuplicate()
    {
        var result = CandidateGenerator.Generate(CreatePlan(
            "econsweep",
            new[] { CandidateOperator.EconomyBiasSweep }));

        var duplicate = Assert.Single(result.Rejected, rejection => rejection.Reason == CandidateRejectionReason.DuplicateOfParent);
        Assert.Equal(CandidateOperator.EconomyBiasSweep, duplicate.Operator);
        Assert.Equal(result.ParentGeneFingerprint, duplicate.GeneFingerprint);
        Assert.Equal(Enum.GetValues(typeof(EconomyBias)).Length, result.GeneratedCount);
    }

    /// <summary>
    /// Swapping the first pair and promoting the second goal produce the same ordering, so running
    /// both operators must collapse to one candidate rather than testing the same build twice.
    /// This uses the no-preferences control because Balanced Control's own convergence point is
    /// already a registered strategy, which is rejected for the more specific reason first.
    /// </summary>
    [Fact]
    public void ConvergentOperators_DeduplicateByBehaviorFingerprint()
    {
        var result = CandidateGenerator.Generate(CreatePlanForParent(
            "convergent",
            "TST_BalancedControl_NoPreferredMyco",
            new[] { CandidateOperator.GoalOrderAdjacentSwap, CandidateOperator.GoalOrderPromoteToFront }));

        var duplicate = Assert.Single(result.Rejected, rejection => rejection.Reason == CandidateRejectionReason.DuplicateOfEarlierCandidate);
        Assert.Equal(CandidateOperator.GoalOrderPromoteToFront, duplicate.Operator);
        Assert.Contains("Identical behavior to earlier candidate", duplicate.Detail, StringComparison.Ordinal);

        var fingerprints = result.Accepted.Select(candidate => CandidateGenomeFingerprint.Compute(candidate.Genes)).ToList();
        Assert.Equal(fingerprints.Count, fingerprints.Distinct().Count());
    }

    /// <summary>
    /// Swapping the first two Balanced Control goals reproduces TST_BalancedControl_AnabolicFirst,
    /// the build the Phase 6 holdout already confirmed. Regenerating it would spend batch budget
    /// re-proving a known result, so it is rejected against the registry.
    /// </summary>
    [Fact]
    public void CandidateMatchingARegisteredStrategy_IsRejected()
    {
        var result = CandidateGenerator.Generate(CreateGoalOrderPlanIncludingKnownBuild());

        var duplicate = Assert.Single(result.Rejected, rejection => rejection.Reason == CandidateRejectionReason.DuplicateOfRegisteredStrategy);
        Assert.Contains("TST_BalancedControl_AnabolicFirst", duplicate.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reordering a rising goal ladder inverts it, which the genome validator rejects as dead
    /// configuration. The generator must record that rather than emitting an unusable candidate.
    /// </summary>
    [Fact]
    public void OperatorThatInvertsAGoalLadder_IsRejectedAsInvalid()
    {
        var result = CandidateGenerator.Generate(CreatePlanForParent(
            "ladder",
            "TST_Arch06_SurgeGrowth",
            new[] { CandidateOperator.GoalOrderPromoteToFront }));

        var invalid = result.Rejected.Where(rejection => rejection.Reason == CandidateRejectionReason.FailedValidation).ToList();
        Assert.NotEmpty(invalid);
        Assert.All(invalid, rejection =>
            Assert.Contains("already targeted earlier", rejection.Detail, StringComparison.Ordinal));
        Assert.All(result.Accepted, candidate => Assert.Empty(CandidateGenomeValidator.Validate(candidate)));
    }

    [Fact]
    public void PlanExceedingItsCap_FailsBeforeGenerating()
    {
        var plan = CreatePlan("capped", new[] { CandidateOperator.GoalOrderAdjacentSwap }, maximumCandidates: 2);

        var exception = Assert.Throws<InvalidOperationException>(() => CandidateGenerator.Generate(plan));
        Assert.Contains("exceeds its maximumCandidates", exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not sample", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CountProposals_MatchesGeneratedCount()
    {
        var plan = CreateBroadPlan();
        Assert.Equal(CandidateGenerator.CountProposals(plan), CandidateGenerator.Generate(plan).GeneratedCount);
    }

    [Fact]
    public void DisplayNames_AreStableWhenOperatorOrderChanges()
    {
        var forward = CandidateGenerator.Generate(CreatePlan(
            "stable",
            new[] { CandidateOperator.EconomyBiasSweep, CandidateOperator.PrioritizeHighTierToggle }));
        var reversed = CandidateGenerator.Generate(CreatePlan(
            "stable",
            new[] { CandidateOperator.PrioritizeHighTierToggle, CandidateOperator.EconomyBiasSweep }));

        Assert.Equal(
            forward.Accepted.Select(candidate => candidate.DisplayName).OrderBy(name => name, StringComparer.Ordinal),
            reversed.Accepted.Select(candidate => candidate.DisplayName).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void SweepOperatorWithoutItsValueList_FailsPlanValidation()
    {
        var plan = CreatePlan("missingvalues", new[] { CandidateOperator.MaxTierSweep });

        Assert.Contains(
            CandidateGenerationPlanValidator.Validate(plan),
            error => error.Contains("maxTierValues is required", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => CandidateGenerator.Generate(plan));
    }

    /// <summary>
    /// A value list with no operator to consume it is a silent lie about what was searched.
    /// </summary>
    [Fact]
    public void ValueListWithoutItsOperator_FailsPlanValidation()
    {
        var plan = new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "orphanvalues",
            Purpose = "Prove an unused value list is refused.",
            ParentStrategyId = ControlStrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.PrioritizeHighTierToggle },
            MaximumCandidates = 8,
            StartingSporeEdgeOffsetValues = new[] { 2 }
        };

        Assert.Contains(
            CandidateGenerationPlanValidator.Validate(plan),
            error => error.Contains("startingSporeEdgeOffsetValues must be empty", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has spaces")]
    [InlineData("waaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaayTooLongToBeAPlanIdentifier")]
    public void MalformedPlanId_FailsValidation(string planId)
    {
        var plan = CreatePlan(planId, new[] { CandidateOperator.PrioritizeHighTierToggle });

        Assert.Contains(
            CandidateGenerationPlanValidator.Validate(plan),
            error => error.StartsWith("planId must be", StringComparison.Ordinal));
    }

    [Fact]
    public void UnregisteredParent_FailsValidation()
    {
        var plan = CreatePlan("unknownparent", new[] { CandidateOperator.PrioritizeHighTierToggle });
        var orphaned = new CandidateGenerationPlan
        {
            SchemaVersion = plan.SchemaVersion,
            PlanId = plan.PlanId,
            Purpose = plan.Purpose,
            ParentStrategyId = "legacy.testing.does-not-exist.v1",
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = plan.Operators,
            MaximumCandidates = plan.MaximumCandidates
        };

        Assert.Contains(
            CandidateGenerationPlanValidator.Validate(orphaned),
            error => error.Contains("is not registered in strategy set", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeatedOperator_FailsValidation()
    {
        var plan = CreatePlan(
            "repeated",
            new[] { CandidateOperator.PrioritizeHighTierToggle, CandidateOperator.PrioritizeHighTierToggle });

        Assert.Contains(
            CandidateGenerationPlanValidator.Validate(plan),
            error => error.Contains("repeats operator", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_RoundTripsThroughJson()
    {
        var plan = CreateBroadPlan();
        var roundTripped = CandidateGenerationPlanJson.Deserialize(CandidateGenerationPlanJson.Serialize(plan));

        Assert.Empty(CandidateGenerationPlanValidator.Validate(roundTripped));
        Assert.Equal(plan.Operators, roundTripped.Operators);
        Assert.Equal(plan.MaxTierValues, roundTripped.MaxTierValues);
        Assert.Equal(
            CandidateGenerator.Generate(plan).Accepted.Select(candidate => candidate.CandidateId),
            CandidateGenerator.Generate(roundTripped).Accepted.Select(candidate => candidate.CandidateId));
    }

    [Fact]
    public void Deserialize_RejectsUnknownFields()
    {
        var json = CandidateGenerationPlanJson.Serialize(CreateBroadPlan())
            .Replace("\"planId\":", "\"typoField\": true,\n  \"planId\":", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CandidateGenerationPlanJson.Deserialize(json));
    }

    [Fact]
    public void CheckedInExample_DeserializesGeneratesAndValidates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "candidate-plan.v1.example.json");
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(path));

        Assert.Empty(CandidateGenerationPlanValidator.Validate(plan));

        var result = CandidateGenerator.Generate(plan);
        Assert.NotEmpty(result.Accepted);
        Assert.All(result.Accepted, candidate => Assert.Empty(CandidateGenomeValidator.Validate(candidate)));
    }

    private static CandidateGenerationPlan CreateGoalOrderPlan() =>
        CreatePlan("openerorder", new[] { CandidateOperator.GoalOrderAdjacentSwap });

    /// <summary>
    /// Promotion from position 1 rebuilds the Anabolic-first opener that is already registered.
    /// </summary>
    private static CandidateGenerationPlan CreateGoalOrderPlanIncludingKnownBuild() =>
        CreatePlan("knownbuild", new[] { CandidateOperator.GoalOrderPromoteToFront });

    private static CandidateGenerationPlan CreateBroadPlan()
    {
        EnsureRosterInitialized();
        return CreateBroadPlanCore();
    }

    private static CandidateGenerationPlan CreateBroadPlanCore() => new()
    {
        SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
        PlanId = "broad",
        Purpose = "Exercise every operator family in one bounded plan.",
        ParentStrategyId = ControlStrategyId,
        ParentStrategySet = StrategySetEnum.Testing,
        Operators = new[]
        {
            CandidateOperator.GoalOrderAdjacentSwap,
            CandidateOperator.PrioritizeHighTierToggle,
            CandidateOperator.MaxTierSweep,
            CandidateOperator.StartingSporeEdgeOffsetSweep
        },
        MaximumCandidates = CandidateGenerationPlan.CandidateCeiling,
        MaxTierValues = new[] { MutationTier.Tier7, MutationTier.Tier10 },
        StartingSporeEdgeOffsetValues = new[] { -2, 0, 3 }
    };

    private static CandidateGenerationPlan CreatePlanForParent(
        string planId,
        string parentStrategyName,
        IReadOnlyList<CandidateOperator> operators)
    {
        EnsureRosterInitialized();
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, parentStrategyName)
            ?? throw new InvalidOperationException($"Strategy '{parentStrategyName}' is not registered.");

        return new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = planId,
            Purpose = $"Bounded candidate search over {parentStrategyName}.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = operators,
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        };
    }

    private static CandidateGenerationPlan CreatePlan(
        string planId,
        IReadOnlyList<CandidateOperator> operators,
        int maximumCandidates = CandidateGenerationPlan.CandidateCeiling)
    {
        EnsureRosterInitialized();
        return new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = planId,
            Purpose = "Bounded candidate search over Balanced Control.",
            ParentStrategyId = ControlStrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = operators,
            MaximumCandidates = maximumCandidates
        };
    }

    private static void EnsureRosterInitialized() => _ = AIRoster.TestingStrategies.Count;
}
