using System.Text.Json;
using System.Text.Json.Serialization;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// A serialized evaluation cast: the candidates to publish plus the authored strategies they are
/// measured against.
///
/// This exists because the simulator runs as its own process. A candidate published in the memory
/// of the process that generated it does not exist for the runner, which resolves lineups through
/// the registry - so without a file to load, generated candidates could be designed and screened
/// but never actually played. Genomes are deterministic to materialize, so the loaded catalog is
/// byte-for-byte the same behavior the generating process characterized.
/// </summary>
public sealed class CandidateCatalogFile
{
    public const string CurrentSchemaVersion = "fungus-toast.ai-candidate-catalog.v1";

    public required string SchemaVersion { get; init; }

    public required IReadOnlyList<CandidateGenome> Candidates { get; init; }

    /// <summary>Authored strategies to republish alongside the candidates, by set and name.</summary>
    public IReadOnlyList<CandidateCatalogReference> References { get; init; } = Array.Empty<CandidateCatalogReference>();
}

public sealed class CandidateCatalogReference
{
    public required StrategySetEnum StrategySet { get; init; }

    public required string StrategyName { get; init; }
}

public static class CandidateCatalogFileJson
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static CandidateCatalogFile Deserialize(string json)
    {
        var catalog = JsonSerializer.Deserialize<CandidateCatalogFile>(json, SerializerOptions);
        return catalog ?? throw new JsonException("Candidate catalog must contain a JSON object.");
    }

    public static string Serialize(CandidateCatalogFile catalog) => JsonSerializer.Serialize(catalog, SerializerOptions);

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}

/// <summary>
/// Loads a catalog file into the process-wide generated set. The runner calls this before it
/// resolves any lineup, so a candidate named on the command line resolves like any other strategy.
/// </summary>
public static class CandidateCatalogLoader
{
    public static IReadOnlyList<StrategyDefinition> LoadAndPublish(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Candidate catalog path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Candidate catalog '{path}' does not exist.", path);

        // In a freshly started runner nothing has touched the roster yet, so its static
        // constructor has not registered the authored sets the references resolve against.
        _ = AIRoster.TestingStrategies.Count;

        var catalog = CandidateCatalogFileJson.Deserialize(File.ReadAllText(path));
        if (!string.Equals(catalog.SchemaVersion, CandidateCatalogFile.CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Candidate catalog '{path}' declares schema '{catalog.SchemaVersion}'; expected '{CandidateCatalogFile.CurrentSchemaVersion}'.");

        var references = new List<StrategyDefinition>();
        foreach (var reference in catalog.References)
        {
            var definition = StrategyRegistry.GetDefinition(reference.StrategySet, reference.StrategyName)
                ?? throw new InvalidOperationException(
                    $"Candidate catalog '{path}' references unknown strategy '{reference.StrategyName}' in set '{reference.StrategySet}'.");
            references.Add(definition);
        }

        return GeneratedCandidateCatalog.Publish(catalog.Candidates, references);
    }

    /// <summary>
    /// Builds the catalog file for an evaluation cast. Reference sets are recorded so the loading
    /// process resolves the same authored definitions the generating process used.
    /// </summary>
    public static CandidateCatalogFile Build(
        IEnumerable<CandidateGenome> candidates,
        IEnumerable<StrategyDefinition> references)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(references);

        return new CandidateCatalogFile
        {
            SchemaVersion = CandidateCatalogFile.CurrentSchemaVersion,
            Candidates = candidates.ToList(),
            References = references
                .Select(reference => new CandidateCatalogReference
                {
                    StrategySet = ResolveAuthoredSet(reference),
                    StrategyName = reference.Strategy.StrategyName
                })
                .ToList()
        };
    }

    /// <summary>
    /// A reference may already be republished in the generated set, so the authored set is found by
    /// skipping that one; otherwise a reloaded catalog would point at itself.
    /// </summary>
    private static StrategySetEnum ResolveAuthoredSet(StrategyDefinition reference)
    {
        foreach (var strategySet in AIRoster.AuthoredStrategySets)
        {
            if (StrategyRegistry.GetDefinitions(strategySet)
                .Any(definition => string.Equals(definition.StrategyId, reference.StrategyId, StringComparison.Ordinal)))
            {
                return strategySet;
            }
        }

        throw new ArgumentException(
            $"Reference '{reference.Strategy.StrategyName}' is not registered in any authored strategy set.",
            nameof(reference));
    }
}
