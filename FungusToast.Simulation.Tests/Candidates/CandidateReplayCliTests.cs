using System.Reflection;
using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

[Collection(GeneratedCatalogCollection.Name)]
public sealed class CandidateReplayCliTests : IDisposable
{
    public void Dispose() => GeneratedCandidateCatalog.Clear();

    [Fact]
    public void Replay_publishes_catalog_before_resolving_the_manifest()
    {
        _ = AIRoster.CampaignStrategies.Count;
        var plan = CandidateGenerationPlanJson.Deserialize(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "candidate-plan.filament-putrid-tendrils.v2.json")));
        var candidate = Assert.Single(CandidateGenerator.Generate(plan).Accepted);
        var catalog = CandidateCatalogLoader.Build(new[] { candidate }, Array.Empty<StrategyDefinition>());
        var path = Path.Combine(Path.GetTempPath(), $"replay-catalog-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, CandidateCatalogFileJson.Serialize(catalog));
        GeneratedCandidateCatalog.Clear();
        try
        {
            var error = RunMain("--replay-manifest", path + ".missing", "--candidate-catalog", path);
            Assert.NotNull(StrategyRegistry.GetDefinition(StrategySetEnum.Generated, candidate.DisplayName));
            Assert.Contains("Replay failed: Resolved experiment manifest was not found.", error);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Replay_refuses_invalid_catalog_before_entering_replay()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invalid-replay-catalog-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{}");
        GeneratedCandidateCatalog.Clear();
        try
        {
            var error = RunMain("--replay-manifest", path + ".missing", "--candidate-catalog", path);
            Assert.Contains("Failed to load candidate catalog", error);
            Assert.DoesNotContain("Replay failed:", error);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Replay_without_catalog_keeps_the_existing_manifest_validation()
    {
        var error = RunMain("--replay-manifest", Path.Combine(Path.GetTempPath(), $"missing-replay-{Guid.NewGuid():N}.json"));
        Assert.Contains("Replay failed: Resolved experiment manifest was not found.", error);
        Assert.DoesNotContain("candidate catalog", error);
    }

    private static string RunMain(params string[] args)
    {
        var program = typeof(CandidateCatalogLoader).Assembly.GetType("FungusToast.Simulation.Program", throwOnError: true)!;
        var main = program.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalError = Console.Error;
        var originalExitCode = Environment.ExitCode;
        using var error = new StringWriter();
        try
        {
            Environment.ExitCode = 0;
            Console.SetError(error);
            main.Invoke(null, new object[] { args });
            Assert.Equal(1, Environment.ExitCode);
            return error.ToString();
        }
        finally
        {
            Console.SetError(originalError);
            Environment.ExitCode = originalExitCode;
        }
    }
}
