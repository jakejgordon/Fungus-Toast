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
    /// Replaces the generated set with exactly this evaluation cast. Publishing is wholesale
    /// rather than incremental so the catalog always reflects one search, never an accumulation of
    /// candidates from runs nobody is tracking any more.
    /// </summary>
    /// <param name="references">
    /// Authored strategies the candidates are measured against - typically the parent control and
    /// the fixed opponent. They join the generated set so a paired comparison's control and
    /// treatment conditions can name one strategy set each.
    ///
    /// The alternative was per-name set qualification in the manifest, but Parquet records the
    /// strategy set once per run, so that would turn a run-level invariant into a per-player one
    /// and ripple through the input schema, resolved manifest, replay runner, export schema, and
    /// every analytic that groups by set. Republishing an authored strategy under a second set
    /// label is much cheaper, and it costs nothing that matters: the reference keeps its authored
    /// stable ID and definition fingerprint, which are what identify behavior in an artifact. Read
    /// <c>strategy_id</c>, not <c>strategy_set</c>, to know what a control arm actually was.
    /// </param>
    public static IReadOnlyList<StrategyDefinition> Publish(
        IEnumerable<CandidateGenome> candidates,
        IEnumerable<StrategyDefinition>? references = null)
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

        var referencesByName = new Dictionary<string, StrategyDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references ?? Array.Empty<StrategyDefinition>())
        {
            ArgumentNullException.ThrowIfNull(reference);
            if (genomesByName.ContainsKey(reference.Strategy.StrategyName))
                throw new ArgumentException(
                    $"Reference strategy '{reference.Strategy.StrategyName}' collides with a candidate display name.",
                    nameof(references));
            if (!referencesByName.TryAdd(reference.Strategy.StrategyName, reference))
                throw new ArgumentException(
                    $"Reference strategy '{reference.Strategy.StrategyName}' is published twice.",
                    nameof(references));

            strategies.Add(reference.Strategy);
        }

        StrategyRegistry.Register(
            StrategySetEnum.Generated,
            strategies,
            strategy => referencesByName.TryGetValue(strategy.StrategyName, out var reference)
                ? BuildReferenceEntry(reference)
                : BuildCatalogEntry(genomesByName[strategy.StrategyName]),
            strategy => referencesByName.TryGetValue(strategy.StrategyName, out var reference)
                ? reference.StrategyId
                : genomesByName[strategy.StrategyName].CandidateId);

        return StrategyRegistry.GetDefinitions(StrategySetEnum.Generated);
    }

    /// <summary>
    /// Copies an authored strategy's metadata verbatim, overriding only the set label and pool
    /// membership.
    ///
    /// The copy has to be faithful. <c>StrategyRegistry.GetDefinition(IMutationSpendingStrategy)</c>
    /// resolves by reference across every set, so once an authored strategy is registered twice, a
    /// caller like <c>AIRoster.GetThemeForStrategy</c> may read either copy's metadata. Keeping
    /// every measured field identical makes which copy it finds irrelevant; clearing pools is safe
    /// precisely because pool membership is the one thing a generated-set entry must never assert.
    /// </summary>
    private static StrategyCatalogEntry BuildReferenceEntry(StrategyDefinition reference)
    {
        var authored = reference.Metadata;
        return new StrategyCatalogEntry(
            strategyName: authored.StrategyName,
            strategySet: StrategySetEnum.Generated,
            archetype: authored.Archetype,
            status: authored.Status,
            powerTier: authored.PowerTier,
            role: authored.Role,
            lifecycle: authored.Lifecycle,
            difficultyBands: authored.DifficultyBands,
            campaignDifficulty: authored.CampaignDifficulty,
            pools: StrategyPool.None,
            friendlyName: authored.FriendlyName,
            aiPlayerIntentions: authored.AIPlayerIntentions,
            intent: authored.Intent,
            notes: authored.Notes,
            favoredAgainst: authored.FavoredAgainst,
            weakAgainst: authored.WeakAgainst,
            suggestedAdaptationSets: authored.SuggestedAdaptationSets,
            mutationPlan: authored.MutationPlan,
            mycovariantPlan: authored.MycovariantPlan);
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
