using FungusToast.Core.AI;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Publishes generated candidates into <see cref="StrategySetEnum.Generated"/> so the existing
/// manifest, replay, and export pipeline can address them by set and name.
///
/// Registering rather than injecting is the point. Every reproducibility guarantee built in
/// Phase 2 and Phase 3 - resolved manifests, stable strategy identity in Parquet, replay outcome
/// fingerprints - keys off a registered definition. A candidate handed straight to the simulator
/// would run, but it would run outside all of that, which is exactly the confounding this
/// initiative exists to remove.
///
/// Nothing player-facing can reach these entries: every published candidate carries
/// <see cref="StrategyPool.None"/>, so pool-filtered campaign and single-player selection cannot
/// return one even by accident. Promotion out of this catalog stays a deliberate, reviewed step.
/// </summary>
public static class GeneratedCandidateCatalog
{
    /// <summary>
    /// Replaces the generated set with exactly these candidates. Publishing is wholesale rather
    /// than incremental so the catalog always reflects one search, never an accumulation of
    /// candidates from runs nobody is tracking any more.
    /// </summary>
    public static IReadOnlyList<StrategyDefinition> Publish(IEnumerable<CandidateGenome> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        // AIRoster's static constructor calls StrategyRegistry.Reset(). Touching it first means a
        // later first-use of the roster cannot silently wipe what we just published.
        EnsureRosterInitialized();

        var genomesByName = new Dictionary<string, CandidateGenome>(StringComparer.OrdinalIgnoreCase);
        var strategies = new List<IMutationSpendingStrategy>();
        foreach (var candidate in candidates)
        {
            var errors = CandidateGenomeValidator.Validate(candidate);
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"Candidate '{candidate.CandidateId}' is invalid and cannot be published:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                    nameof(candidates));
            }

            if (!genomesByName.TryAdd(candidate.DisplayName, candidate))
            {
                throw new ArgumentException(
                    $"Candidate display name '{candidate.DisplayName}' is published twice.",
                    nameof(candidates));
            }

            strategies.Add(CandidateGenomeFactory.Materialize(candidate));
        }

        StrategyRegistry.Register(
            StrategySetEnum.Generated,
            strategies,
            strategy => BuildCatalogEntry(genomesByName[strategy.StrategyName]),
            strategy => genomesByName[strategy.StrategyName].CandidateId);

        return StrategyRegistry.GetDefinitions(StrategySetEnum.Generated);
    }

    /// <summary>Empties the generated set, leaving every authored strategy set untouched.</summary>
    public static void Clear()
    {
        EnsureRosterInitialized();
        StrategyRegistry.Register(
            StrategySetEnum.Generated,
            Array.Empty<IMutationSpendingStrategy>(),
            _ => throw new InvalidOperationException("Clearing the generated catalog builds no entries."));
    }

    public static IReadOnlyList<StrategyDefinition> GetPublished()
    {
        EnsureRosterInitialized();
        return StrategyRegistry.GetDefinitions(StrategySetEnum.Generated);
    }

    /// <summary>
    /// Metadata for a candidate that has not been evaluated yet. Every measured field is set to
    /// its most conservative value: no pool, experimental role, draft lifecycle, no difficulty
    /// band, and no campaign difficulty. Phase 7 assigns real bands from evidence; asserting one
    /// here would be an authored guess masquerading as a measurement.
    /// </summary>
    private static StrategyCatalogEntry BuildCatalogEntry(CandidateGenome candidate)
    {
        return new StrategyCatalogEntry(
            strategyName: candidate.DisplayName,
            strategySet: StrategySetEnum.Generated,
            archetype: StrategyArchetype.Balanced,
            status: StrategyStatus.Testing,
            powerTier: StrategyPowerTier.Standard,
            role: StrategyRole.Experimental,
            lifecycle: StrategyLifecycle.Draft,
            difficultyBands: Array.Empty<DifficultyBand>(),
            campaignDifficulty: null,
            pools: StrategyPool.None,
            friendlyName: candidate.DisplayName,
            aiPlayerIntentions: string.Empty,
            intent: $"Generated candidate derived from {candidate.Lineage.ParentStrategyId}.",
            notes: candidate.Lineage.Notes);
    }

    private static void EnsureRosterInitialized() => _ = AIRoster.TestingStrategies.Count;
}
