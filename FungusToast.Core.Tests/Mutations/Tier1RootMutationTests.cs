using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier1RootMutationTests
{
    [Fact]
    public void MycelialBloom_is_a_root_growth_mutation_with_no_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        Assert.Equal(MutationCategory.Growth, mutation.Category);
        Assert.Equal(MutationTier.Tier1, mutation.Tier);
        Assert.Equal(MutationType.GrowthChance, mutation.Type);
        Assert.Empty(mutation.Prerequisites);
        Assert.Contains(mutation.Id, MutationRegistry.Roots.Keys);
    }

    [Fact]
    public void HomeostaticHarmony_is_a_root_cellular_resilience_mutation_with_no_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.HomeostaticHarmony);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier1, mutation.Tier);
        Assert.Equal(MutationType.DefenseSurvival, mutation.Type);
        Assert.Empty(mutation.Prerequisites);
        Assert.Contains(mutation.Id, MutationRegistry.Roots.Keys);
    }

    [Fact]
    public void MycelialBloom_effect_scales_effective_growth_chance_by_level()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        player.SetMutationLevel(mutation.Id, newLevel: 4, currentRound: 1);

        var effectiveGrowthChance = player.GetEffectiveGrowthChance();

        Assert.Equal(
            GameBalance.BaseGrowthChance + (4 * GameBalance.MycelialBloomEffectPerLevel),
            effectiveGrowthChance,
            precision: 6);
    }

    [Fact]
    public void HomeostaticHarmony_effect_scales_effective_self_death_chance_by_level()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.HomeostaticHarmony);

        player.SetMutationLevel(mutation.Id, newLevel: 5, currentRound: 1);

        var effectiveSelfDeathChance = player.GetEffectiveSelfDeathChance();

        Assert.Equal(
            5 * GameBalance.HomeostaticHarmonyEffectPerLevel,
            effectiveSelfDeathChance,
            precision: 6);
    }

    [Fact]
    public void MycelialBloom_root_upgrade_is_allowed_on_round_one_when_points_are_available()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 1);

        Assert.True(canUpgrade, $"Expected root mutation {mutation.Name} to be upgradable without prerequisites.");
    }

    [Fact]
    public void HomeostaticHarmony_root_upgrade_is_allowed_on_round_one_when_points_are_available()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = RequireMutation(MutationIds.HomeostaticHarmony);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 1);

        Assert.True(canUpgrade, $"Expected root mutation {mutation.Name} to be upgradable without prerequisites.");
    }

    [Fact]
    public void Upgrading_mycelial_bloom_sets_prereq_met_round_for_hyphal_surge_when_threshold_is_reached()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var mycelialBloom = RequireMutation(MutationIds.MycelialBloom);

        player.SetMutationLevel(mycelialBloom.Id, newLevel: 4, currentRound: 1);
        var upgraded = player.TryUpgradeMutation(mycelialBloom, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(5, player.GetMutationLevel(mycelialBloom.Id));
        Assert.Equal(2, Assert.IsType<PlayerMutation>(player.PlayerMutations[MutationIds.HyphalSurge]).PrereqMetRound);
    }

    [Fact]
    public void Upgrading_homeostatic_harmony_sets_prereq_met_round_for_chronoresilient_cytoplasm_when_threshold_is_reached()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var harmony = RequireMutation(MutationIds.HomeostaticHarmony);

        player.SetMutationLevel(harmony.Id, newLevel: 4, currentRound: 1);
        var upgraded = player.TryUpgradeMutation(harmony, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(5, player.GetMutationLevel(harmony.Id));
        Assert.Equal(2, Assert.IsType<PlayerMutation>(player.PlayerMutations[MutationIds.ChronoresilientCytoplasm]).PrereqMetRound);
    }

    private static Player CreatePlayer(int mutationPoints = 0)
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }
}
