using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

public sealed class P8CandidateGenerationPlanTests
{
    [Theory]
    [InlineData("candidate-plan.p8-hard-economy-control.v1.json")]
    [InlineData("candidate-plan.p8-hard-surge-tempo.v1.json")]
    [InlineData("candidate-plan.p8-elite-large-board.v1.json")]
    public void CheckedInPlan_ValidatesAndProducesCandidates(string fileName)
    {
        _ = AIRoster.ProvenStrategies.Count;
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(path));

        Assert.Empty(CandidateGenerationPlanValidator.Validate(plan));
        var result = CandidateGenerator.Generate(plan);
        Assert.NotEmpty(result.Accepted);
        Assert.All(result.Accepted, candidate => Assert.Empty(CandidateGenomeValidator.Validate(candidate)));
    }
}
