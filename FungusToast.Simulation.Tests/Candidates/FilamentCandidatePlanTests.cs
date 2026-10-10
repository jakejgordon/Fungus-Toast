using FungusToast.Core.AI;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class FilamentCandidatePlanTests
{
    [Fact]
    public void Putrid_extension_preserves_the_complete_engine_and_only_appends_filament()
    {
        _ = AIRoster.CampaignStrategies.Count;
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures",
            "candidate-plan.filament-putrid-tendrils.v1.json");
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(path));
        Assert.Empty(CandidateGenerationPlanValidator.Validate(plan));
        var candidate = Assert.Single(CandidateGenerator.Generate(plan).Accepted);
        Assert.Empty(CandidateGenomeValidator.Validate(candidate));
        var parent = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.CampaignStrategiesByName["CMP_Growth_PutridTendrils_Medium"]);
        var strategy = CandidateGenomeFactory.Materialize(candidate);
        Assert.Equal(parent.TargetMutationGoals.Select(g => (g.MutationId, g.TargetLevel)),
            strategy.TargetMutationGoals.Take(parent.TargetMutationGoals.Count)
                .Select(g => (g.MutationId, g.TargetLevel)));
        Assert.Equal(parent.TargetMutationGoals.Count + 1, strategy.TargetMutationGoals.Count);
        Assert.Equal(MutationIds.FilamentOverdrive, strategy.TargetMutationGoals.Last().MutationId);
        Assert.Equal(GameBalance.FilamentOverdriveMaxLevel, strategy.TargetMutationGoals.Last().TargetLevel);
        Assert.Single(candidate.VariedGenes);
        var characterization = CandidateCharacterizationGate.Run(new[] { candidate });
        Assert.Empty(characterization.Findings);
        Assert.Single(characterization.Passed);
        // No player-facing registration or edits are part of generating an evaluation genome.
        Assert.DoesNotContain(parent.TargetMutationGoals, g => g.MutationId == MutationIds.FilamentOverdrive);
        Assert.DoesNotContain(AIRoster.CampaignStrategies, s => s.StrategyName == candidate.DisplayName);
        Assert.DoesNotContain(AIRoster.ProvenStrategies, s => s.StrategyName == candidate.DisplayName);
    }
}
