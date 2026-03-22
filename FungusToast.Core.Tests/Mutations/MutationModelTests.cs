using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class MutationModelTests
{
    [Fact]
    public void Mutation_CanUpgrade_returns_false_for_active_surge_even_below_max_level()
    {
        var surge = new Mutation(
            id: 9001,
            name: "Test Surge",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.HyphalSurge,
            effectPerLevel: 0.1f,
            maxLevel: 3,
            isSurge: true,
            surgeDuration: 2,
            pointsPerActivation: 2,
            pointIncreasePerLevel: 1);

        Assert.False(surge.CanUpgrade(currentLevel: 1, isSurgeActive: true));
        Assert.True(surge.CanUpgrade(currentLevel: 1, isSurgeActive: false));
    }

    [Fact]
    public void Mutation_CanUpgrade_returns_false_at_max_level()
    {
        var mutation = new Mutation(
            id: 9002,
            name: "Test Mutation",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.GrowthChance,
            effectPerLevel: 0.1f,
            maxLevel: 3);

        Assert.False(mutation.CanUpgrade(currentLevel: 3));
        Assert.True(mutation.CanUpgrade(currentLevel: 2));
    }

    [Fact]
    public void Mutation_GetTotalEffect_scales_linearly_by_level()
    {
        var mutation = new Mutation(
            id: 9003,
            name: "Scaling Mutation",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.GrowthChance,
            effectPerLevel: 0.125f);

        Assert.Equal(0f, mutation.GetTotalEffect(0), precision: 6);
        Assert.Equal(0.25f, mutation.GetTotalEffect(2), precision: 6);
        Assert.Equal(0.5f, mutation.GetTotalEffect(4), precision: 6);
    }

    [Fact]
    public void Mutation_GetSurgeActivationCost_increases_with_current_level()
    {
        var surge = new Mutation(
            id: 9004,
            name: "Cost Surge",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.HyphalSurge,
            effectPerLevel: 0.1f,
            isSurge: true,
            surgeDuration: 2,
            pointsPerActivation: 3,
            pointIncreasePerLevel: 2);

        Assert.Equal(3, surge.GetSurgeActivationCost(currentLevel: 0));
        Assert.Equal(5, surge.GetSurgeActivationCost(currentLevel: 1));
        Assert.Equal(9, surge.GetSurgeActivationCost(currentLevel: 3));
    }

    [Fact]
    public void PlayerMutation_Upgrade_sets_first_upgrade_round_once_and_stops_at_max_level()
    {
        var mutation = new Mutation(
            id: 9005,
            name: "Player Mutation",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.GrowthChance,
            effectPerLevel: 0.1f,
            maxLevel: 2);
        var playerMutation = new PlayerMutation(playerId: 0, mutationId: mutation.Id, mutation: mutation);

        playerMutation.Upgrade(currentRound: 4);
        playerMutation.Upgrade(currentRound: 5);
        playerMutation.Upgrade(currentRound: 6);

        Assert.Equal(2, playerMutation.CurrentLevel);
        Assert.Equal(4, playerMutation.FirstUpgradeRound);
        Assert.True(playerMutation.IsMaxedOut);
    }

    [Fact]
    public void PlayerMutation_GetEffect_returns_mutation_total_effect_at_current_level()
    {
        var mutation = new Mutation(
            id: 9006,
            name: "Effect Mutation",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.GrowthChance,
            effectPerLevel: 0.2f);
        var playerMutation = new PlayerMutation(playerId: 0, mutationId: mutation.Id, mutation: mutation)
        {
            CurrentLevel = 3
        };

        Assert.Equal(0.6f, playerMutation.GetEffect(), precision: 6);
    }

    [Fact]
    public void PlayerMutation_CanAutoUpgrade_returns_false_at_max_level()
    {
        var mutation = new Mutation(
            id: 9007,
            name: "Auto Upgrade Mutation",
            description: string.Empty,
            flavorText: string.Empty,
            type: MutationType.GrowthChance,
            effectPerLevel: 0.2f,
            maxLevel: 1);
        var playerMutation = new PlayerMutation(playerId: 0, mutationId: mutation.Id, mutation: mutation)
        {
            CurrentLevel = 1
        };

        Assert.False(playerMutation.CanAutoUpgrade());
    }
}
