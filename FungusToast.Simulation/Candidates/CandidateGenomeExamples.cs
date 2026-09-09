using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Builds the checked-in candidate genome examples from the live strategy registry.
///
/// The examples record their parent's definition fingerprint and the exact set of genes that
/// differ from that parent, so any change to a parent strategy invalidates them. Regenerating
/// them from here - rather than hand-editing the JSON - keeps the derived fields (candidateId,
/// parent fingerprint, variedGenes) consistent with each other and with the parent, which
/// hand-editing reliably does not: refreshing only the fingerprint leaves a gene set that no
/// longer matches the parent, and the validator then rejects the file for a second reason.
///
/// Both the regeneration command and the test that guards the checked-in copy go through this
/// class, so neither can drift from the other.
/// </summary>
public static class CandidateGenomeExamples
{
    /// <summary>Directory holding the checked-in examples, relative to the repository root.</summary>
    public const string ExamplesDirectoryRelativePath = "FungusToast.Simulation/Examples";

    /// <summary>Command that rewrites every example in place. Named in the guard test's failure message.</summary>
    public const string RegenerateCommand =
        "dotnet run --project FungusToast.Simulation -- --regenerate-examples";

    /// <summary>One checked-in example: the file name and the exact bytes it should contain.</summary>
    public readonly record struct GeneratedExample(string FileName, string Json);

    /// <summary>
    /// Builds every checked-in example. Requires the strategy registry to be populated, which
    /// touching <see cref="AIRoster"/> guarantees.
    /// </summary>
    public static IReadOnlyList<GeneratedExample> BuildAll() => new[]
    {
        new GeneratedExample("candidate-genome.v1.example.json", BuildOpenerOrderExample())
    };

    /// <summary>
    /// The opener-order candidate the frozen Phase 6 holdout confirmed: the parent's first two
    /// goals swapped, promoting Anabolic Inversion ahead of Creeping Mold. Expressed as an
    /// adjacent swap rather than a literal goal list so it keeps describing the same experiment
    /// if the parent's opener changes.
    /// </summary>
    private static string BuildOpenerOrderExample()
    {
        const string parentStrategyId = "legacy.testing.tst-balancedgeneralistcontrol.v1";

        var parent = ResolveParent(StrategySetEnum.Testing, parentStrategyId);
        var parentGenes = CandidateGenomeFactory.ExtractGenes((ParameterizedSpendingStrategy)parent.Strategy);

        var goals = parentGenes.TargetMutationGoals.ToList();
        if (goals.Count < 2)
            throw new InvalidOperationException(
                $"Parent '{parentStrategyId}' needs at least two goals for an opener-order swap; it has {goals.Count}.");
        (goals[0], goals[1]) = (goals[1], goals[0]);

        var genome = CandidateGenomeFactory.CreateFromParent(
            parent,
            WithGoals(parentGenes, goals),
            displayNameSuffix: "OpenerOrderAnabolicFirst",
            notes: "Balanced Control with Anabolic Inversion moved ahead of Creeping Mold; "
                 + "the contrast confirmed by the frozen Phase 6 opener-order holdout.");

        var errors = CandidateGenomeValidator.Validate(genome);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Regenerated example is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");

        return Normalize(CandidateGenomeJson.Serialize(genome));
    }

    private static StrategyDefinition ResolveParent(StrategySetEnum strategySet, string strategyId)
    {
        // AIRoster's static constructor populates StrategyRegistry; touch it before any lookup.
        _ = AIRoster.TestingStrategies.Count;

        return StrategyRegistry.GetDefinitions(strategySet)
                   .FirstOrDefault(definition => string.Equals(definition.StrategyId, strategyId, StringComparison.Ordinal))
               ?? throw new InvalidOperationException(
                   $"Example parent '{strategyId}' is not registered in strategy set '{strategySet}'.");
    }

    private static CandidateGeneSet WithGoals(CandidateGeneSet genes, List<CandidateMutationGoal> goals) => new()
    {
        PrioritizeHighTier = genes.PrioritizeHighTier,
        MaxTier = genes.MaxTier,
        PriorityMutationCategories = genes.PriorityMutationCategories,
        TargetMutationGoals = goals,
        SurgePriorityIds = genes.SurgePriorityIds,
        SurgeAttemptTurnFrequency = genes.SurgeAttemptTurnFrequency,
        EconomyBias = genes.EconomyBias,
        MycovariantPreferences = genes.MycovariantPreferences,
        ExcludedMutationIds = genes.ExcludedMutationIds,
        StartingSporeEdgeOffset = genes.StartingSporeEdgeOffset
    };

    /// <summary>
    /// The repository is canonical LF with a trailing newline, but the serializer indents with
    /// Environment.NewLine. Without this the generated text would match the checked-in file on
    /// Linux and differ from it on Windows.
    /// </summary>
    private static string Normalize(string json) => json.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
}
