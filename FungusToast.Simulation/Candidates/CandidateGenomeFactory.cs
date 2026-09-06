using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Converts between registered strategies and candidate genomes.
///
/// Extraction is lossless with respect to Core's definition fingerprint: materializing an
/// extracted gene set under the parent's own name reproduces the parent's fingerprint exactly.
/// That invariant is what makes the gene list a complete description of the search space rather
/// than a hopeful subset of it.
/// </summary>
public static class CandidateGenomeFactory
{
    /// <summary>Reads the full behavior surface off a parameterized strategy.</summary>
    public static CandidateGeneSet ExtractGenes(ParameterizedSpendingStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        return new CandidateGeneSet
        {
            PrioritizeHighTier = strategy.PrioritizeHighTier
                ?? throw new InvalidOperationException($"Strategy '{strategy.StrategyName}' has no prioritizeHighTier value."),
            MaxTier = strategy.MaxTier
                ?? throw new InvalidOperationException($"Strategy '{strategy.StrategyName}' has no maxTier value."),
            PriorityMutationCategories = strategy.PriorityMutationCategories?.ToList(),
            TargetMutationGoals = strategy.TargetMutationGoals
                .Select(goal => new CandidateMutationGoal { MutationId = goal.MutationId, TargetLevel = goal.TargetLevel })
                .ToList(),
            SurgePriorityIds = strategy.SurgePriorityIds.ToList(),
            SurgeAttemptTurnFrequency = strategy.SurgeAttemptTurnFrequency,
            EconomyBias = strategy.EconomyProfile,
            // GetMycovariantPreferences() returns Core's own evaluation order, which the genome preserves.
            MycovariantPreferences = strategy.GetMycovariantPreferences()
                .Select(preference => new CandidateMycovariantPreference
                {
                    MycovariantIds = preference.MycovariantIds.OrderBy(id => id).ToList(),
                    Priority = preference.Priority
                })
                .ToList(),
            ExcludedMutationIds = strategy.ExcludedMutationIds.OrderBy(id => id).ToList(),
            StartingSporeEdgeOffset = strategy.StartingSporeEdgeOffset
        };
    }

    /// <summary>
    /// Builds a genome from a parent definition and an explicit gene set. Passing the parent's own
    /// genes yields a pure clone with no varied genes, which is the control arm of a
    /// single-variable comparison.
    /// </summary>
    public static CandidateGenome CreateFromParent(
        StrategyDefinition parent,
        CandidateGeneSet genes,
        string displayNameSuffix,
        string notes = "")
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(genes);
        if (parent.Strategy is not ParameterizedSpendingStrategy parameterized)
            throw new ArgumentException(
                $"Strategy '{parent.Strategy.StrategyName}' is not parameterized and has no candidate genome.",
                nameof(parent));

        var parentGenes = ExtractGenes(parameterized);
        return new CandidateGenome
        {
            SchemaVersion = CandidateGenome.CurrentSchemaVersion,
            CandidateId = CandidateGenomeFingerprint.DeriveCandidateId(parent.StrategyId, genes),
            DisplayName = CandidateGenome.DisplayNamePrefix + displayNameSuffix,
            Lineage = new CandidateLineage
            {
                ParentStrategyId = parent.StrategyId,
                ParentDefinitionFingerprint = parent.DefinitionFingerprint,
                ParentStrategySet = ResolveStrategySet(parent),
                Notes = notes
            },
            Genes = genes,
            VariedGenes = CandidateGenomeFingerprint.FindDifferences(parentGenes, genes)
        };
    }

    /// <summary>
    /// Materializes a genome into a runnable strategy. The result is an ordinary
    /// <see cref="ParameterizedSpendingStrategy"/>; no candidate-specific behavior exists.
    /// </summary>
    public static ParameterizedSpendingStrategy Materialize(CandidateGenome genome)
    {
        ArgumentNullException.ThrowIfNull(genome);
        return Materialize(genome.Genes, genome.DisplayName);
    }

    /// <summary>
    /// Materializes a gene set under an explicit name. The name is a separate argument because
    /// Core's definition fingerprint includes it, so parity checks must be able to hold it constant.
    /// </summary>
    public static ParameterizedSpendingStrategy Materialize(CandidateGeneSet genes, string strategyName)
    {
        ArgumentNullException.ThrowIfNull(genes);
        if (string.IsNullOrWhiteSpace(strategyName))
            throw new ArgumentException("Strategy name is required.", nameof(strategyName));

        return new ParameterizedSpendingStrategy(
            strategyName: strategyName,
            prioritizeHighTier: genes.PrioritizeHighTier,
            priorityMutationCategories: genes.PriorityMutationCategories?.ToList(),
            maxTier: genes.MaxTier,
            targetMutationGoals: genes.TargetMutationGoals
                .Select(goal => new TargetMutationGoal(goal.MutationId, goal.TargetLevel))
                .ToList(),
            surgePriorityIds: genes.SurgePriorityIds.ToList(),
            surgeAttemptTurnFrequency: genes.SurgeAttemptTurnFrequency,
            economyBias: genes.EconomyBias,
            mycovariantPreferences: genes.MycovariantPreferences
                .Select(preference => new MycovariantPreference(preference.MycovariantIds, preference.Priority))
                .ToList(),
            // Deliberately unused: preferredMycovariantIds is constructor sugar that merges into
            // mycovariantPreferences, and the genome already stores the merged result.
            preferredMycovariantIds: null,
            excludedMutationIds: genes.ExcludedMutationIds.ToList(),
            startingSporeEdgeOffset: genes.StartingSporeEdgeOffset);
    }

    private static StrategySetEnum ResolveStrategySet(StrategyDefinition parent)
    {
        foreach (var strategySet in Enum.GetValues(typeof(StrategySetEnum)).Cast<StrategySetEnum>())
        {
            if (StrategyRegistry.GetDefinitions(strategySet)
                .Any(definition => string.Equals(definition.StrategyId, parent.StrategyId, StringComparison.Ordinal)))
            {
                return strategySet;
            }
        }

        throw new ArgumentException(
            $"Strategy definition '{parent.StrategyId}' is not registered in any strategy set.",
            nameof(parent));
    }
}
