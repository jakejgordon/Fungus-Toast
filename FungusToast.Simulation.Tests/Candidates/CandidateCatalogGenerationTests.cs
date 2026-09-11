using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class CandidateCatalogGenerationTests
{
    [Theory]
    [InlineData("candidate-plan.p8-hard-economy-control.v1.json")]
    [InlineData("candidate-plan.p8-hard-surge-tempo.v1.json")]
    [InlineData("candidate-plan.p8-elite-large-board.v1.json")]
    public void P8Plan_BuildsGeneratedOnlyCatalogWithParentReference(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(path));

        var result = CandidateCatalogGeneration.Build(plan);

        Assert.NotEmpty(result.Catalog.Candidates);
        Assert.Equal(result.Generation.Accepted.Select(candidate => candidate.CandidateId), result.Catalog.Candidates.Select(candidate => candidate.CandidateId));
        var parent = Assert.Single(result.Catalog.References);
        Assert.Equal(plan.ParentStrategySet, parent.StrategySet);
        Assert.Equal(plan.ParentStrategyId, StrategyRegistry.GetDefinition(parent.StrategySet, parent.StrategyName)!.StrategyId);
    }

    [Fact]
    public void AdditionalReference_IsIncludedOnceAlongsideParent()
    {
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "candidate-plan.p8-hard-economy-control.v1.json")));

        var result = CandidateCatalogGeneration.Build(plan, new[]
        {
            new CandidateCatalogReference
            {
                StrategySet = StrategySetEnum.Proven,
                StrategyName = "Grow>Kill>Reclaim(Econ/Reclaim)"
            },
            new CandidateCatalogReference
            {
                StrategySet = StrategySetEnum.Proven,
                StrategyName = "Grow>Kill>Reclaim(Econ/Reclaim)"
            }
        });

        Assert.Equal(2, result.Catalog.References.Count);
    }
}
