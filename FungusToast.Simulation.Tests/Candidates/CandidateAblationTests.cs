using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

/// <summary>
/// Ablation is the diagnostic use of the candidate machinery: remove one mutation from a strategy,
/// run it paired against the untouched original, and the difference is what that mutation was
/// contributing.
/// </summary>
public sealed class CandidateAblationTests
{
    private const string ControlName = "TST_BalancedGeneralistControl";

    [Fact]
    public void AnAblationSweep_ProducesOneCandidatePerDistinctBuildGoal()
    {
        var (parentGenes, result) = Ablate(ControlName);

        var distinctGoals = parentGenes.TargetMutationGoals.Select(goal => goal.MutationId).Distinct().Count();
        Assert.Equal(distinctGoals, result.GeneratedCount);
        Assert.Empty(result.Rejected);
    }

    /// <summary>
    /// A rising ladder is one intent expressed in stages, so ablating that mutation must remove
    /// every occurrence - leaving part of the ladder behind would ablate something else.
    /// </summary>
    [Fact]
    public void AblatingALadderedMutation_RemovesEveryOccurrence()
    {
        var (parentGenes, result) = Ablate("TST_Arch06_SurgeGrowth");

        var laddered = parentGenes.TargetMutationGoals
            .GroupBy(goal => goal.MutationId)
            .First(group => group.Count() > 1)
            .Key;
        var candidate = result.Accepted.Single(genome => genome.Genes.ExcludedMutationIds.Contains(laddered));

        Assert.DoesNotContain(candidate.Genes.TargetMutationGoals, goal => goal.MutationId == laddered);
    }

    /// <summary>
    /// The measurement only means anything if the block actually holds. Exclusions are honored on
    /// every acquisition path, including free upgrades, so the ablated mutation must never appear.
    /// </summary>
    [Fact]
    public void AnAblatedMutation_IsNeverBought()
    {
        var (parentGenes, result) = Ablate(ControlName);

        foreach (var candidate in result.Accepted)
        {
            var ablated = candidate.Genes.ExcludedMutationIds
                .Except(parentGenes.ExcludedMutationIds)
                .Single();
            var build = CandidateCharacterizationGate.ObserveBuild(CandidateGenomeFactory.Materialize(candidate));

            Assert.False(
                build.ContainsKey(ablated),
                $"{MutationRegistry.GetById(ablated)?.Name} was still bought despite being ablated.");
        }
    }

    /// <summary>
    /// Blocking a mutation blocks everything gated behind it, so the measured effect is not that
    /// one mutation's contribution. Saying so on the candidate is what stops the number being
    /// misread later.
    /// </summary>
    [Fact]
    public void AblatingAMutationThatGatesAnother_SaysSoOnTheCandidate()
    {
        var (_, result) = Ablate(ControlName);

        // Necrosporulation is a prerequisite of Catabolic Rebirth in this build plan.
        var gating = result.Accepted.Single(genome =>
            genome.Genes.ExcludedMutationIds.Contains(MutationIds.Necrosporulation));

        Assert.Contains("also gates", gating.Lineage.Notes, StringComparison.Ordinal);
        Assert.Contains("Catabolic Rebirth", gating.Lineage.Notes, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAblationWithNoDownstreamGoals_CarriesNoConfoundNote()
    {
        var (_, result) = Ablate(ControlName);

        var independent = result.Accepted.Single(genome =>
            genome.Genes.ExcludedMutationIds.Contains(MutationIds.CreepingMold));

        Assert.DoesNotContain("also gates", independent.Lineage.Notes, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ablation necessarily varies two genes: blocking a mutation the strategy is still told to buy
    /// would be incoherent, so the goal goes with it.
    /// </summary>
    [Fact]
    public void EveryAblation_DeclaresBothGenesItChanges()
    {
        var (_, result) = Ablate(ControlName);

        Assert.All(result.Accepted, candidate =>
        {
            Assert.Equal(
                new[] { CandidateGene.TargetMutationGoals, CandidateGene.ExcludedMutationIds },
                candidate.VariedGenes.OrderBy(gene => (int)gene));
            Assert.Empty(CandidateGenomeValidator.Validate(candidate));
        });
    }

    /// <summary>
    /// The trap that made pruning direction-aware. An ablation expects the candidate to be worse,
    /// so a large negative difference is the result it is hunting - and an increase-shaped futility
    /// test would prune exactly those away first.
    /// </summary>
    [Fact]
    public void ADecreaseSweep_KeepsLargeDropsAndPrunesOnlyTheHarmlessOnes()
    {
        var (_, result) = Ablate(ControlName);
        var queue = CandidateEvaluationQueue.Create("ablation", result.Accepted, 5000, 100_000);

        // The first ablation hurt a lot: that mutation matters, and it must survive.
        Measure(queue, 0, -0.40, -0.50, -0.30);
        // The second barely moved anything: that mutation does not matter, and it can go.
        Measure(queue, 1, -0.01, -0.03, 0.01);

        var pruned = queue.PruneFutileCandidates(margin: 0.05, ExperimentDirection.Decrease);

        Assert.Equal(1, pruned);
        Assert.Equal(CandidateQueueStatus.Pending, queue.Entries[0].Status);
        Assert.Equal(CandidateQueueStatus.Pruned, queue.Entries[1].Status);

        // The same data under an increase hypothesis prunes the opposite candidate, which is why
        // the direction cannot be defaulted.
        var mirrored = CandidateEvaluationQueue.Create("mirror", result.Accepted, 5000, 100_000);
        Measure(mirrored, 0, -0.40, -0.50, -0.30);
        Measure(mirrored, 1, -0.01, -0.03, 0.01);
        Assert.Equal(2, mirrored.PruneFutileCandidates(margin: 0.05, ExperimentDirection.Increase));
    }

    /// <summary>
    /// Under a decrease hypothesis the better candidate is the one that dropped further, so
    /// domination has to compare the other way round.
    /// </summary>
    [Fact]
    public void ADecreaseSweep_TreatsTheLargerDropAsDominant()
    {
        var (_, result) = Ablate(ControlName);
        var queue = CandidateEvaluationQueue.Create("ablation", result.Accepted, 5000, 100_000);
        Measure(queue, 0, -0.40, -0.50, -0.30);
        Measure(queue, 1, -0.10, -0.15, -0.05);

        Assert.Equal(1, queue.PruneDominatedCandidates(ExperimentDirection.Decrease));
        Assert.Equal(CandidateQueueStatus.Pending, queue.Entries[0].Status);
        Assert.Equal(CandidateQueueStatus.Pruned, queue.Entries[1].Status);
    }

    [Fact]
    public void NonInferiorityPrunesNothing_BecauseNoOneSidedBoundRulesItOutEarly()
    {
        var (_, result) = Ablate(ControlName);
        var queue = CandidateEvaluationQueue.Create("ablation", result.Accepted, 5000, 100_000);
        Measure(queue, 0, -0.40, -0.50, -0.30);

        Assert.Equal(0, queue.PruneFutileCandidates(0.05, ExperimentDirection.NonInferiority));
        Assert.Equal(0, queue.PruneDominatedCandidates(ExperimentDirection.NonInferiority));
    }

    [Fact]
    public void TheCheckedInAblationExample_DeserializesAndGenerates()
    {
        _ = AIRoster.TestingStrategies.Count;
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "candidate-plan.ablation.example.json");
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(path));

        Assert.Empty(CandidateGenerationPlanValidator.Validate(plan));

        var result = CandidateGenerator.Generate(plan);
        Assert.NotEmpty(result.Accepted);
        Assert.All(result.Accepted, candidate =>
        {
            Assert.Empty(CandidateGenomeValidator.Validate(candidate));
            Assert.Contains("Ablates", candidate.Lineage.Notes, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// The gap goal ablation cannot reach. The panel's strongest strategies buy seven levels of
    /// Mycotoxin Tracer through the fallback path and never declare it as a goal, so a goal-only
    /// sweep would report that their most-bought Fungicide mutation does not exist.
    /// </summary>
    [Fact]
    public void ObservedAblation_ReachesFallbackPurchasesThatGoalAblationCannot()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Proven, "TST_CampaignMirror_AI13_AnabolicFirst")!;
        var parameterized = (ParameterizedSpendingStrategy)parent.Strategy;
        var goalIds = parameterized.TargetMutationGoals.Select(goal => goal.MutationId).ToHashSet();

        var goalSweep = Generate(parent, CandidateOperator.AblateTargetGoal);
        var observedSweep = Generate(parent, CandidateOperator.AblateObservedPurchase);

        int Ablated(CandidateGenome genome) => genome.Genes.ExcludedMutationIds
            .Except(CandidateGenomeFactory.ExtractGenes(parameterized).ExcludedMutationIds).Single();

        Assert.DoesNotContain(goalSweep.Accepted, genome => Ablated(genome) == MutationIds.MycotoxinTracer);
        var tracer = Assert.Single(observedSweep.Accepted, genome => Ablated(genome) == MutationIds.MycotoxinTracer);
        Assert.Contains("fallback path only", tracer.Lineage.Notes, StringComparison.Ordinal);
        Assert.DoesNotContain(goalIds, id => id == MutationIds.MycotoxinTracer);
    }

    /// <summary>
    /// A mutation a remaining goal depends on cannot be ablated: the prerequisite path buys what a
    /// goal needs whatever the exclusion list says. Those ablations are still emitted so the
    /// conflict is visible, and the characterization gate rejects them as ViolatedExclusions rather
    /// than letting them pass as real measurements of nothing.
    /// </summary>
    [Fact]
    public void ObservedAblation_CoversEveryPurchase_AndUnachievableOnesAreCaughtNotBelieved()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Proven, "TST_CampaignMirror_AI13_AnabolicFirst")!;
        var parameterized = (ParameterizedSpendingStrategy)parent.Strategy;
        var parentGenes = CandidateGenomeFactory.ExtractGenes(parameterized);
        var observed = CandidateCharacterizationGate.ObserveBuild(parameterized);

        var sweep = Generate(parent, CandidateOperator.AblateObservedPurchase);
        Assert.Equal(observed.Count, sweep.GeneratedCount);

        var report = CandidateCharacterizationGate.Run(sweep.Accepted);

        foreach (var genome in report.Passed)
        {
            var ablated = genome.Genes.ExcludedMutationIds.Except(parentGenes.ExcludedMutationIds).Single();
            var rebuilt = CandidateCharacterizationGate.ObserveBuild(CandidateGenomeFactory.Materialize(genome));
            Assert.False(rebuilt.ContainsKey(ablated), $"Mutation {ablated} passed the gate but survived its own ablation.");
        }

        // Whatever could not be ablated was caught rather than reported as a measurement.
        Assert.All(report.Findings, finding =>
            Assert.Equal(CandidateCharacterizationFailure.ViolatedExclusions, finding.Failure));
    }

    /// <summary>
    /// The unachievable ablations are exactly the ones whose mutation still gates a goal, and the
    /// candidate says so before anything is run.
    /// </summary>
    [Fact]
    public void AnUnachievableAblation_SaysSoOnTheCandidate()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Proven, "TST_CampaignMirror_AI13_AnabolicFirst")!;
        var sweep = Generate(parent, CandidateOperator.AblateObservedPurchase);
        var report = CandidateCharacterizationGate.Run(sweep.Accepted);

        foreach (var finding in report.Findings)
        {
            var genome = sweep.Accepted.Single(candidate => candidate.CandidateId == finding.CandidateId);
            Assert.Contains("not achievable", genome.Lineage.Notes, StringComparison.Ordinal);
        }
    }

    private static CandidateGenerationResult Generate(StrategyDefinition parent, CandidateOperator candidateOperator)
        => CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "obsablate",
            Purpose = "Measure what each purchase contributes.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Proven,
            Operators = new[] { candidateOperator },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        });

    private static void Measure(CandidateEvaluationQueue queue, int index, double estimate, double low, double high)
        => queue.RecordStageResult(
            queue.Entries[index].CandidateId,
            CandidateEvaluationStage.Calibration,
            CandidateStageResult.Passed,
            CandidateEvaluationQueue.StageGameCost(CandidateEvaluationStage.Calibration),
            10,
            estimate: estimate,
            ci95Low: low,
            ci95High: high);

    private static (CandidateGeneSet ParentGenes, CandidateGenerationResult Result) Ablate(string parentName)
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, parentName)!;
        var parentGenes = CandidateGenomeFactory.ExtractGenes((ParameterizedSpendingStrategy)parent.Strategy);

        var result = CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "ablate",
            Purpose = "Measure what each build goal contributes.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.AblateTargetGoal },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        });

        return (parentGenes, result);
    }
}
