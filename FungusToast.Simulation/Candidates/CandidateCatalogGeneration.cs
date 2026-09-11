using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Materializes a bounded generation plan into the generated-only catalog consumed by a separate
/// simulator process. This is deliberately a file-ready evaluation cast, never a player-facing
/// roster mutation.
/// </summary>
public static class CandidateCatalogGeneration
{
    public static CandidateCatalogGenerationResult Build(
        CandidateGenerationPlan plan,
        IEnumerable<CandidateCatalogReference>? additionalReferences = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        // The registry is populated by AIRoster's static initializer. Command-line generation
        // starts in a fresh process, so touching it here keeps this API safe outside test fixtures.
        _ = AIRoster.ProvenStrategies.Count;

        var generation = CandidateGenerator.Generate(plan);
        var references = new List<StrategyDefinition>
        {
            RequireReference(plan.ParentStrategySet, plan.ParentStrategyId)
        };

        foreach (var reference in additionalReferences ?? Array.Empty<CandidateCatalogReference>())
            references.Add(RequireReference(reference.StrategySet, reference.StrategyName));

        var distinctReferences = references
            .GroupBy(reference => reference.StrategyId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        return new CandidateCatalogGenerationResult(
            generation,
            CandidateCatalogLoader.Build(generation.Accepted, distinctReferences));
    }

    private static StrategyDefinition RequireReference(StrategySetEnum strategySet, string nameOrId)
    {
        var definition = StrategyRegistry.GetDefinitions(strategySet).FirstOrDefault(definition =>
            string.Equals(definition.StrategyId, nameOrId, StringComparison.Ordinal)
            || string.Equals(definition.Strategy.StrategyName, nameOrId, StringComparison.Ordinal));
        return definition ?? throw new ArgumentException(
            $"Unknown candidate-catalog reference '{nameOrId}' in strategy set '{strategySet}'.",
            nameof(nameOrId));
    }
}

public sealed record CandidateCatalogGenerationResult(
    CandidateGenerationResult Generation,
    CandidateCatalogFile Catalog);
