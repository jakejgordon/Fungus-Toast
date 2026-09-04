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
    public void Registered_strategy_ids_are_unique_across_all_rosters()
    {
        var ids = Enum.GetValues<StrategySetEnum>()
            .SelectMany(strategySet => AIRoster.GetStrategiesByFilter(strategySet, new StrategyCatalogFilter())
                .Select(strategy => StrategyIdentity.GetStableId(strategySet, strategy)))
            .ToList();

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    private static ParameterizedSpendingStrategy CreateParameterized(int startingOffset) => new(
        "Fingerprint Test",
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
