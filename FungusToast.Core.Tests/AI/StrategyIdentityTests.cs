using System.Text.RegularExpressions;
using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Tests.AI;

public sealed class StrategyIdentityTests
{
    [Fact]
    public void Stable_id_is_deterministic_and_keeps_the_roster_namespace()
    {
        var strategy = new RandomMutationSpendingStrategy("Legacy Random #1");

        var first = StrategyIdentity.GetStableId(StrategySetEnum.Testing, strategy);
        var second = StrategyIdentity.GetStableId(StrategySetEnum.Testing, strategy);

        Assert.Equal("legacy.testing.legacy-random-1.v1", first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Definition_fingerprint_changes_when_behavior_configuration_changes()
    {
        var baseline = CreateParameterized(startingOffset: 0);
        var equivalent = CreateParameterized(startingOffset: 0);
        var changed = CreateParameterized(startingOffset: 2);

        Assert.Equal(
            StrategyIdentity.GetDefinitionFingerprint(baseline),
            StrategyIdentity.GetDefinitionFingerprint(equivalent));
        Assert.NotEqual(
            StrategyIdentity.GetDefinitionFingerprint(baseline),
            StrategyIdentity.GetDefinitionFingerprint(changed));
    }

    [Fact]
    public void Promoted_strategy_uses_its_durable_non_roster_identity()
    {
        var strategy = Assert.IsType<ParameterizedSpendingStrategy>(
            AIRoster.ProvenStrategiesByName["VerdantReclaimer"]);

        Assert.Equal("ai.growth.verdant-reclaimer.v1", StrategyIdentity.GetStableId(StrategySetEnum.Proven, strategy));
    }

    [Fact]
    public void Registered_strategy_ids_are_unique_across_all_rosters()
    {
        // Generated is excluded: the simulation's candidate catalog populates it at run time with
        // identities of its own, so it is empty here and its IDs are not minted by GetStableId.
        var ids = AIRoster.AuthoredStrategySets
            .SelectMany(strategySet => AIRoster.GetStrategiesByFilter(strategySet, new StrategyCatalogFilter())
                .Select(strategy => StrategyIdentity.GetStableId(strategySet, strategy)))
            .ToList();

        Assert.NotEmpty(ids);
        Assert.Empty(StrategyRegistry.GetDefinitions(StrategySetEnum.Generated));
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Registered_stable_ids_match_the_frozen_snapshot()
    {
        // Stable IDs key every simulation comparison, so they are pinned rather than trusted to
        // derivation. Set FUNGUS_UPDATE_STRATEGY_IDS=1 to rewrite the snapshot after adding a strategy.
        var registered = AIRoster.AuthoredStrategySets
            .SelectMany(strategySet => StrategyRegistry.GetDefinitions(strategySet)
                .Select(definition => $"{strategySet}\t{definition.StrategyId}"))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToList();
        var snapshotPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../AI/StrategyStableIds.txt"));
        if (Environment.GetEnvironmentVariable("FUNGUS_UPDATE_STRATEGY_IDS") == "1")
        {
            File.WriteAllLines(snapshotPath, registered);
        }

        var frozen = File.ReadAllLines(snapshotPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        var missing = frozen.Except(registered, StringComparer.Ordinal).ToList();
        var unpinned = registered.Except(frozen, StringComparer.Ordinal).ToList();

        Assert.True(missing.Count == 0,
            "Pinned stable IDs are no longer registered. Renaming a strategy must add a StrategyIdentity rename "
            + "entry instead of changing its identity; delete a line only when retiring that strategy:\n"
            + string.Join("\n", missing));
        Assert.True(unpinned.Count == 0,
            "New strategies need their stable IDs pinned. Add these lines to FungusToast.Core.Tests/AI/StrategyStableIds.txt:\n"
            + string.Join("\n", unpinned));
    }

    [Fact]
    public void Renamed_strategies_keep_their_identity_and_resolve_by_legacy_name()
    {
        Assert.NotEmpty(AIRoster.ProvenStrategies); // registers the authored sets
        Assert.NotEmpty(StrategyIdentity.RenamedStrategies);
        foreach (var rename in StrategyIdentity.RenamedStrategies)
        {
            var current = StrategyRegistry.GetDefinition(rename.StrategySet, rename.CurrentName);
            Assert.True(current != null, $"Renamed strategy '{rename.CurrentName}' is not registered in {rename.StrategySet}.");
            Assert.Equal(rename.CurrentName, current!.Strategy.StrategyName);
            Assert.Same(current, StrategyRegistry.GetDefinition(rename.StrategySet, rename.LegacyName));
            Assert.Same(
                current.Strategy,
                StrategyRegistry.GetStrategyDictionary(rename.StrategySet)[rename.LegacyName]);
            Assert.Equal(rename.LegacyName, StrategyIdentity.GetIdentityName(current.Strategy));
        }
    }

    [Fact]
    public void Measured_bands_key_registered_strategies()
    {
        var registeredIds = AIRoster.AuthoredStrategySets
            .SelectMany(StrategyRegistry.GetDefinitions)
            .Select(definition => definition.StrategyId)
            .ToHashSet(StringComparer.Ordinal);

        var orphaned = StrategyMeasuredBands.All.Keys.Where(id => !registeredIds.Contains(id)).ToList();

        Assert.True(orphaned.Count == 0,
            "Measured bands reference unregistered stable IDs; evidence must follow its strategy's identity: "
            + string.Join(", ", orphaned));
        Assert.All(StrategyMeasuredBands.All.Values, band =>
        {
            Assert.Matches("^\\d{4}-\\d{2}-\\d{2}$", band.EvidenceDate);
            Assert.Equal(band.Band.HasValue, band.Evidence == BandEvidence.Sufficient);
        });
    }

    [Fact]
    public void Rename_preserves_stable_id_and_fingerprint()
    {
        var legacy = CreateParameterized(startingOffset: 0, name: "Grow>Kill>Reclaim(Econ)");
        var renamed = CreateParameterized(startingOffset: 0, name: "SporeLedger");

        Assert.Equal(
            StrategyIdentity.GetStableId(StrategySetEnum.Proven, legacy),
            StrategyIdentity.GetStableId(StrategySetEnum.Proven, renamed));
        Assert.Equal(
            StrategyIdentity.GetDefinitionFingerprint(legacy),
            StrategyIdentity.GetDefinitionFingerprint(renamed));
    }

    [Fact]
    public void Player_facing_roster_names_are_machine_safe()
    {
        // Testing entries keep their technical identities while they are evaluated; everything a
        // player can meet must be safe in CSV columns, file paths, and CLI arguments.
        Assert.NotEmpty(AIRoster.ProvenStrategies); // registers the authored sets
        var unsafeNames = new[] { StrategySetEnum.Proven, StrategySetEnum.Campaign }
            .SelectMany(strategySet => StrategyRegistry.GetDefinitions(strategySet))
            .Select(definition => definition.Strategy.StrategyName)
            .Where(name => !name.StartsWith("TST_", StringComparison.Ordinal))
            .Where(name => !Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(unsafeNames.Count == 0,
            "Strategy names may only use letters, digits, and underscores; rename via StrategyIdentity: "
            + string.Join(", ", unsafeNames));
    }

    private static ParameterizedSpendingStrategy CreateParameterized(int startingOffset, string name = "Fingerprint Test") => new(
        name,
        prioritizeHighTier: true,
        priorityMutationCategories: new List<MutationCategory> { MutationCategory.Growth },
        targetMutationGoals: new List<TargetMutationGoal>
        {
            new(MutationIds.MycelialBloom, 3)
        },
        surgeAttemptTurnFrequency: 4,
        economyBias: EconomyBias.MaxEconomy,
        startingSporeEdgeOffset: startingOffset);
}
