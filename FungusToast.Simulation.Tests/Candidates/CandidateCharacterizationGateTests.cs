using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class CandidateCharacterizationGateTests
{
    private const string ControlStrategyId = "legacy.testing.tst-balancedgeneralistcontrol.v1";

    /// <summary>
    /// Real generated candidates must clear the gate. This is the no-false-positive check: a gate
    /// that rejected healthy candidates would quietly starve the search.
    /// </summary>
    [Fact]
    public void GeneratedCandidates_AllPassCharacterization()
    {
        var candidates = GenerateCandidates();
        Assert.NotEmpty(candidates);

        var report = CandidateCharacterizationGate.Run(candidates);

        Assert.Equal(string.Empty, string.Join("; ", report.Findings.Select(f => $"{f.DisplayName} {f.Failure}: {f.Detail}")));
        Assert.Equal(candidates.Count, report.Passed.Count);
    }

    /// <summary>
    /// Every passing candidate must actually have bought something, and the recorded build is what
    /// a later stage would reason about.
    /// </summary>
    [Fact]
    public void PassingCandidates_RecordANonEmptyObservedBuild()
    {
        var report = CandidateCharacterizationGate.Run(GenerateCandidates());

        foreach (var candidate in report.Passed)
        {
            var build = Assert.Contains(candidate.CandidateId, report.ObservedBuilds);
            Assert.NotEmpty(build);
            Assert.All(build.Values, level => Assert.True(level > 0));
        }
    }

    /// <summary>
    /// Paired inference compares a candidate against its control under matched seeds, so a
    /// candidate whose own decisions drifted under a fixed seed would corrupt every downstream
    /// interval. The gate must therefore observe identical builds across repeated runs.
    /// </summary>
    [Fact]
    public void RepeatedRuns_ProduceIdenticalObservedBuilds()
    {
        var candidates = GenerateCandidates();
        var first = CandidateCharacterizationGate.Run(candidates);
        var second = CandidateCharacterizationGate.Run(candidates);

        Assert.Equal(first.Passed.Select(candidate => candidate.CandidateId), second.Passed.Select(candidate => candidate.CandidateId));
        foreach (var candidate in first.Passed)
        {
            Assert.Equal(
                first.ObservedBuilds[candidate.CandidateId].OrderBy(entry => entry.Key),
                second.ObservedBuilds[candidate.CandidateId].OrderBy(entry => entry.Key));
        }
    }

    [Fact]
    public void StrategyThatThrowsWhileSpending_IsReportedNotPropagated()
    {
        var report = RunWithFake(new FakeStrategy { ThrowOnSpend = true });

        var finding = Assert.Single(report.Findings);
        Assert.Equal(CandidateCharacterizationFailure.SpendingThrew, finding.Failure);
        Assert.Empty(report.Passed);
    }

    [Fact]
    public void StrategyThatBuysNothing_IsReportedAsNeverSpent()
    {
        var report = RunWithFake(new FakeStrategy { BuyNothing = true });

        Assert.Equal(CandidateCharacterizationFailure.NeverSpent, Assert.Single(report.Findings).Failure);
    }

    [Fact]
    public void StrategyWhoseBuildDriftsUnderAFixedSeed_IsReportedAsNondeterministic()
    {
        var report = RunWithFake(new FakeStrategy { DriftBetweenRuns = true });

        var finding = Assert.Single(report.Findings);
        Assert.Equal(CandidateCharacterizationFailure.NondeterministicSpending, finding.Failure);
        Assert.Contains("produced", finding.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void StrategyThatBuysAnExcludedMutation_IsReported()
    {
        var report = RunWithFake(
            new FakeStrategy { ForcedPurchaseId = MutationIds.MycelialBloom },
            excludedMutationIds: new[] { MutationIds.MycelialBloom });

        var finding = Assert.Single(report.Findings);
        Assert.Equal(CandidateCharacterizationFailure.ViolatedExclusions, finding.Failure);
        Assert.Contains(MutationIds.MycelialBloom.ToString(), finding.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void StrategyThatDraftsAnUnofferedMycovariant_IsReported()
    {
        var report = RunWithFake(new FakeStrategy { DraftOutsideChoices = true });

        var finding = Assert.Single(report.Findings);
        Assert.Equal(CandidateCharacterizationFailure.DraftFailed, finding.Failure);
        Assert.Contains("not among the offered choices", finding.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void StrategyWhoseDraftDriftsUnderAFixedSeed_IsReported()
    {
        var report = RunWithFake(new FakeStrategy { DriftDraftBetweenRuns = true });

        Assert.Equal(CandidateCharacterizationFailure.NondeterministicDraft, Assert.Single(report.Findings).Failure);
    }

    [Fact]
    public void MaterializationFailure_IsReportedNotPropagated()
    {
        var report = CandidateCharacterizationGate.Run(
            new[] { CreateCandidate() },
            materializer: _ => throw new InvalidOperationException("cannot build"));

        var finding = Assert.Single(report.Findings);
        Assert.Equal(CandidateCharacterizationFailure.ConstructionFailed, finding.Failure);
        Assert.Contains("cannot build", finding.Detail, StringComparison.Ordinal);
    }

    private static CandidateCharacterizationReport RunWithFake(
        FakeStrategy strategy,
        IReadOnlyList<int>? excludedMutationIds = null)
    {
        return CandidateCharacterizationGate.Run(
            new[] { CreateCandidate(excludedMutationIds) },
            new CandidateCharacterizationSettings { Rounds = 3 },
            _ => strategy);
    }

    private static IReadOnlyList<CandidateGenome> GenerateCandidates()
    {
        EnsureRosterInitialized();
        return CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "chargate",
            Purpose = "Characterize a bounded opener-order and economy search.",
            ParentStrategyId = ControlStrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap, CandidateOperator.EconomyBiasSweep },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;
    }

    /// <summary>
    /// A genome whose genes are irrelevant to the fake-strategy tests; only its declared
    /// exclusions are read, so the gate can be aimed at one behavior at a time.
    /// </summary>
    private static CandidateGenome CreateCandidate(IReadOnlyList<int>? excludedMutationIds = null)
    {
        EnsureRosterInitialized();
        var definition = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, "TST_BalancedGeneralistControl")!;
        var genes = CandidateGenomeFactory.ExtractGenes((ParameterizedSpendingStrategy)definition.Strategy);
        var withExclusions = new CandidateGeneSet
        {
            PrioritizeHighTier = genes.PrioritizeHighTier,
            MaxTier = genes.MaxTier,
            PriorityMutationCategories = genes.PriorityMutationCategories,
            TargetMutationGoals = genes.TargetMutationGoals,
            SurgePriorityIds = genes.SurgePriorityIds,
            SurgeAttemptTurnFrequency = genes.SurgeAttemptTurnFrequency,
            EconomyBias = genes.EconomyBias,
            MycovariantPreferences = genes.MycovariantPreferences,
            ExcludedMutationIds = excludedMutationIds ?? genes.ExcludedMutationIds,
            StartingSporeEdgeOffset = genes.StartingSporeEdgeOffset
        };

        return CandidateGenomeFactory.CreateFromParent(definition, withExclusions, "GateProbe");
    }

    private static void EnsureRosterInitialized() => _ = AIRoster.TestingStrategies.Count;

    /// <summary>
    /// Stands in for a strategy that misbehaves in a way no real ParameterizedSpendingStrategy
    /// currently can, so each gate check can be shown to fire.
    /// </summary>
    private sealed class FakeStrategy : IMutationSpendingStrategy
    {
        private int spendRunCount;
        private int draftRunCount;

        public bool ThrowOnSpend { get; init; }
        public bool BuyNothing { get; init; }
        public bool DriftBetweenRuns { get; init; }
        public bool DraftOutsideChoices { get; init; }
        public bool DriftDraftBetweenRuns { get; init; }
        public int? ForcedPurchaseId { get; init; }

        public string StrategyName => "FakeCharacterizationStrategy";
        public MutationTier? MaxTier => MutationTier.Tier10;
        public bool? PrioritizeHighTier => true;
        public bool? UsesGrowth => true;
        public bool? UsesCellularResilience => false;
        public bool? UsesFungicide => false;
        public bool? UsesGeneticDrift => false;

        public Mycovariant SelectMycovariantFromChoices(Player player, List<Mycovariant> choices, GameBoard board, Random rnd)
        {
            if (DraftOutsideChoices)
                return MycovariantRepository.All.First(mycovariant => choices.All(choice => choice.Id != mycovariant.Id));

            // Each Run() call constructs the gate fresh but reuses this instance, so a per-call
            // counter is what lets one fake differ between the two repeat runs.
            var index = DriftDraftBetweenRuns ? draftRunCount++ % choices.Count : 0;
            return choices[index];
        }

        public void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board, Random rnd, ISimulationObserver simulationObserver)
        {
            if (ThrowOnSpend) throw new InvalidOperationException("deliberate spending failure");
            if (BuyNothing) return;

            var purchaseId = ForcedPurchaseId ?? MutationIds.MycelialBloom;
            if (DriftBetweenRuns && spendRunCount++ >= 3)
                purchaseId = MutationIds.HomeostaticHarmony;

            var mutation = MutationRegistry.GetById(purchaseId)!;
            player.TryUpgradeMutation(mutation, simulationObserver, board.CurrentRound, board);
        }
    }
}
