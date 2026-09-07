using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

/// <summary>
/// The screen exists because of a real wasted cycle: a category-level correlation of `r = 0.792`
/// between Fungicide investment and measured strength turned out to be one Tier-1 mutation,
/// `Mycotoxin Tracer`, that every strategy on the Necrosporulation line is required to buy. These
/// tests hold the line that such a variable is never reported as evidence again.
/// </summary>
public sealed class StructuralConfoundScreenTests
{
    private static IReadOnlyList<ParameterizedSpendingStrategy> ProvenPanel()
    {
        _ = AIRoster.TestingStrategies.Count; // forces registration
        return StrategyRegistry.GetDefinitions(StrategySetEnum.Proven)
            .Select(definition => definition.Strategy)
            .OfType<ParameterizedSpendingStrategy>()
            .ToList();
    }

    /// <summary>
    /// The regression this whole file is about. Whatever the panel looks like later, a mutation
    /// its owners were forced into must never come back reportable.
    /// </summary>
    [Fact]
    public void MycotoxinTracer_IsNeverReportableAsEvidence_AcrossTheProvenPanel()
    {
        var reports = StructuralConfoundScreen.ScreenPanel(ProvenPanel());
        var tracer = reports.Single(report => report.MutationId == MutationIds.MycotoxinTracer);

        Assert.False(tracer.IsReportableAsEvidence);
        Assert.NotNull(StructuralConfoundScreen.ExplainWhyNotEvidence(tracer));
        Assert.True(tracer.EntailedOwners > 0, "the tracer is reached through goals that require it");
    }

    /// <summary>
    /// A goal is a choice by definition, so it must never be filed as forced - otherwise the screen
    /// would suppress exactly the deliberate decisions we want to correlate over.
    /// </summary>
    [Fact]
    public void EveryDeclaredGoal_IsClassifiedAsAChoice()
    {
        foreach (var strategy in ProvenPanel())
        {
            var goalIds = strategy.TargetMutationGoals.Select(goal => goal.MutationId).ToHashSet();
            foreach (var ownership in StructuralConfoundScreen.ExplainBuild(strategy))
            {
                if (!goalIds.Contains(ownership.MutationId)) continue;
                Assert.Equal(MutationEntailment.Goal, ownership.Entailment);
            }
        }
    }

    /// <summary>
    /// A forced classification has to name a goal that genuinely requires the mutation, or the
    /// screen is suppressing free choices on a bad excuse.
    /// </summary>
    [Fact]
    public void AForcedPurchase_NamesAGoalThatTransitivelyRequiresIt()
    {
        var forced = ProvenPanel()
            .SelectMany(strategy => StructuralConfoundScreen.ExplainBuild(strategy))
            .Where(ownership => ownership.Entailment == MutationEntailment.RequiredByGoal)
            .ToList();

        Assert.NotEmpty(forced);
        foreach (var ownership in forced)
        {
            Assert.NotEmpty(ownership.ForcedBy);
            foreach (var goalId in ownership.ForcedBy)
                Assert.True(
                    ClosureContains(goalId, ownership.MutationId),
                    $"{ownership.MutationName} is not in the prerequisite closure of {goalId}");
        }
    }

    /// <summary>
    /// Free variation is the only thing a purchase-level correlation may speak about, so the
    /// reportable flag and the explanation must agree on every verdict.
    /// </summary>
    [Fact]
    public void OnlyFreeVariation_IsReportable()
    {
        foreach (var report in StructuralConfoundScreen.ScreenPanel(ProvenPanel()))
        {
            var explanation = StructuralConfoundScreen.ExplainWhyNotEvidence(report);
            Assert.Equal(report.Verdict == ConfoundVerdict.FreeVariation, explanation == null);
            Assert.Equal(report.Owners, report.FreeOwners + report.EntailedOwners);
        }
    }

    /// <summary>
    /// A root mutation everyone is dragged through is the archetype of a confound, so the panel had
    /// better contain at least one - if none does, the screen is not looking at real builds.
    /// </summary>
    [Fact]
    public void ThePanel_ContainsAtLeastOneStructurallyConfoundedMutation()
    {
        var reports = StructuralConfoundScreen.ScreenPanel(ProvenPanel());
        Assert.Contains(reports, report => !report.IsReportableAsEvidence);
    }

    private static bool ClosureContains(int goalId, int mutationId)
    {
        var visited = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(goalId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current)) continue;
            var mutation = MutationRegistry.GetById(current);
            if (mutation == null) continue;
            foreach (var prerequisite in mutation.Prerequisites)
            {
                if (prerequisite.MutationId == mutationId) return true;
                pending.Push(prerequisite.MutationId);
            }
        }
        return false;
    }
}
