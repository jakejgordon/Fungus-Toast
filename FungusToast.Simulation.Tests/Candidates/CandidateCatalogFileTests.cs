using System.Text.Json;
using FungusToast.Core.AI;
using FungusToast.Simulation.Candidates;
using Xunit;

namespace FungusToast.Simulation.Tests.Candidates;

/// <summary>
/// Publishes into the process-wide registry, so it shares the generated-catalog collection.
/// </summary>
[Collection(GeneratedCatalogCollection.Name)]
public sealed class CandidateCatalogFileTests : IDisposable
{
    private const string ParentName = "TST_BalancedGeneralistControl";
    private const string OpponentName = "TST_RebirthAttrition";

    public void Dispose() => GeneratedCandidateCatalog.Clear();

    /// <summary>
    /// The reason this file exists: the simulator is a separate process, so a candidate must
    /// survive serialization and republish to the exact same behavior, or a measured artifact
    /// would describe something other than what was characterized.
    /// </summary>
    [Fact]
    public void ACatalogRoundTrip_RepublishesIdenticalIdentityAndBehavior()
    {
        var (candidates, parent, opponent) = GenerateCast();
        var before = GeneratedCandidateCatalog.Publish(candidates, new[] { parent, opponent })
            .ToDictionary(definition => definition.Strategy.StrategyName, StringComparer.Ordinal);

        var json = CandidateCatalogFileJson.Serialize(CandidateCatalogLoader.Build(candidates, new[] { parent, opponent }));
        GeneratedCandidateCatalog.Clear();
        var path = WriteTemporaryCatalog(json);

        try
        {
            var after = CandidateCatalogLoader.LoadAndPublish(path)
                .ToDictionary(definition => definition.Strategy.StrategyName, StringComparer.Ordinal);

            Assert.Equal(before.Keys.OrderBy(name => name, StringComparer.Ordinal), after.Keys.OrderBy(name => name, StringComparer.Ordinal));
            foreach (var (name, definition) in before)
            {
                Assert.Equal(definition.StrategyId, after[name].StrategyId);
                Assert.Equal(definition.DefinitionFingerprint, after[name].DefinitionFingerprint);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// References must record their authored set, not the generated one they were republished into,
    /// or a reloaded catalog would point at itself.
    /// </summary>
    [Fact]
    public void ReferencesRecordTheirAuthoredSet_EvenAfterBeingRepublished()
    {
        var (candidates, parent, opponent) = GenerateCast();
        GeneratedCandidateCatalog.Publish(candidates, new[] { parent, opponent });

        // Build from the already-republished definitions, the case that could resolve to Generated.
        var republishedParent = StrategyRegistry.GetDefinition(StrategySetEnum.Generated, ParentName)!;
        var file = CandidateCatalogLoader.Build(candidates, new[] { republishedParent });

        var reference = Assert.Single(file.References);
        Assert.Equal(StrategySetEnum.Testing, reference.StrategySet);
        Assert.Equal(ParentName, reference.StrategyName);
    }

    [Fact]
    public void ACatalogDeclaringAnotherSchemaVersion_IsRefused()
    {
        var (candidates, parent, _) = GenerateCast();
        var json = CandidateCatalogFileJson
            .Serialize(CandidateCatalogLoader.Build(candidates, new[] { parent }))
            .Replace(CandidateCatalogFile.CurrentSchemaVersion, "fungus-toast.ai-candidate-catalog.v0", StringComparison.Ordinal);
        var path = WriteTemporaryCatalog(json);

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => CandidateCatalogLoader.LoadAndPublish(path));
            Assert.Contains("expected", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ACatalogNamingAnUnknownReference_IsRefused()
    {
        var (candidates, parent, _) = GenerateCast();
        var json = CandidateCatalogFileJson
            .Serialize(CandidateCatalogLoader.Build(candidates, new[] { parent }))
            .Replace(ParentName, "TST_DoesNotExist", StringComparison.Ordinal);
        var path = WriteTemporaryCatalog(json);

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => CandidateCatalogLoader.LoadAndPublish(path));
            Assert.Contains("references unknown strategy", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AMissingCatalog_IsRefused()
    {
        Assert.Throws<FileNotFoundException>(
            () => CandidateCatalogLoader.LoadAndPublish(Path.Combine(Path.GetTempPath(), "no-such-candidate-catalog.json")));
    }

    [Fact]
    public void Deserialize_RejectsUnknownFields()
    {
        var (candidates, parent, _) = GenerateCast();
        var json = CandidateCatalogFileJson
            .Serialize(CandidateCatalogLoader.Build(candidates, new[] { parent }))
            .Replace("\"schemaVersion\":", "\"typoField\": true,\n  \"schemaVersion\":", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CandidateCatalogFileJson.Deserialize(json));
    }

    private static string WriteTemporaryCatalog(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"candidate-catalog-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static (IReadOnlyList<CandidateGenome> Candidates, StrategyDefinition Parent, StrategyDefinition Opponent) GenerateCast()
    {
        _ = AIRoster.TestingStrategies.Count;
        var parent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, ParentName)!;
        var opponent = StrategyRegistry.GetDefinition(StrategySetEnum.Testing, OpponentName)!;

        var candidates = CandidateGenerator.Generate(new CandidateGenerationPlan
        {
            SchemaVersion = CandidateGenerationPlan.CurrentSchemaVersion,
            PlanId = "catalogfile",
            Purpose = "Round-trip an evaluation cast through the catalog file.",
            ParentStrategyId = parent.StrategyId,
            ParentStrategySet = StrategySetEnum.Testing,
            Operators = new[] { CandidateOperator.GoalOrderAdjacentSwap },
            MaximumCandidates = CandidateGenerationPlan.CandidateCeiling
        }).Accepted;

        return (candidates, parent, opponent);
    }
}
