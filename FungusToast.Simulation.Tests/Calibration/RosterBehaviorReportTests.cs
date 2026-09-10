using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Calibration;
using FungusToast.Simulation.Candidates;
using FungusToast.Simulation.Experiments;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class RosterBehaviorReportTests
{
    [Fact]
    public void Render_StampsIdentityAndPreservesBothSimilarityMeasures()
    {
        _ = AIRoster.ProvenStrategies.Count;
        var profile = new StrategyBehaviorProfile(
            AIRoster.ProvenStrategies[0].StrategyName,
            new Dictionary<int, int> { [MutationIds.MycelialBloom] = 2 },
            new Dictionary<MutationCategory, int> { [MutationCategory.Growth] = 2 });
        var pair = new StrategyBehaviorPair(profile.StrategyName, "Other", 0.75, 1);
        var settings = new CandidateCharacterizationSettings();
        var code = new ResolvedCodeIdentity
        {
            Commit = "abc1234",
            SimulationAssemblyVersion = "test",
            SimulationAssemblySha256 = "simulation-sha",
            CoreAssemblySha256 = "core-sha"
        };

        var report = RosterBehaviorReport.Render(
            StrategySetEnum.Proven, new[] { profile }, new[] { pair }, settings, code);

        Assert.Contains(RosterBehaviorReport.SchemaVersion, report);
        Assert.Contains("`abc1234`", report);
        Assert.Contains("`simulation-sha`", report);
        Assert.Contains("`core-sha`", report);
        Assert.Contains("0.750", report);
        Assert.Contains("1.000", report);
        Assert.Contains("redundancy-review candidate", report);
    }
}
