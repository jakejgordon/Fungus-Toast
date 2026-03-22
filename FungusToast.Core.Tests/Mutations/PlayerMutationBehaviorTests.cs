using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerMutationBehaviorTests
{
    [Fact]
    public void GetMutationLevel_returns_zero_when_player_does_not_have_the_mutation()
    {
        var player = CreatePlayer();

        var level = player.GetMutationLevel(MutationIds.HyphalSurge);

        Assert.Equal(0, level);
    }

    [Fact]
    public void GetMutationEffect_returns_zero_when_player_has_no_mutations_of_that_type()
    {
        var player = CreatePlayer();

        var effect = player.GetMutationEffect(MutationType.HyphalSurge);

        Assert.Equal(0f, effect);
    }

    [Fact]
    public void GetMutationPointCost_returns_points_per_upgrade_for_standard_mutation()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        var cost = player.GetMutationPointCost(mutation);

        Assert.Equal(mutation.PointsPerUpgrade, cost);
    }

    [Fact]
    public void GetMutationPointCost_returns_surge_activation_cost_based_on_current_level()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        player.SetMutationLevel(mutation.Id, newLevel: 2);

        var cost = player.GetMutationPointCost(mutation);

        Assert.Equal(mutation.GetSurgeActivationCost(currentLevel: 2), cost);
    }

    [Fact]
    public void GetMutationPointCost_applies_hyphal_economy_discount_to_mycelial_surges()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        var adaptation = RequireAdaptation(AdaptationIds.HyphalEconomy);
        player.TryAddAdaptation(adaptation);

        var cost = player.GetMutationPointCost(mutation);

        Assert.Equal(
            Math.Max(0, mutation.GetSurgeActivationCost(currentLevel: 0) - AdaptationGameBalance.HyphalEconomySurgeCostReduction),
            cost);
    }

    [Fact]
    public void CanUpgrade_returns_false_when_prerequisites_are_missing()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var mutation = RequireMutation(MutationIds.HyphalSurge);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 5);

        Assert.False(canUpgrade, $"Expected {mutation.Name} not to be upgradable when prerequisites are missing.");
    }

    [Fact]
    public void CanUpgrade_returns_false_when_mutation_points_are_insufficient()
    {
        var player = CreatePlayer(mutationPoints: 0);
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 5);

        Assert.False(canUpgrade, $"Expected {mutation.Name} not to be upgradable with insufficient mutation points.");
    }

    [Fact]
    public void CanUpgrade_returns_false_when_mutation_is_already_at_max_level()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        player.SetMutationLevel(mutation.Id, mutation.MaxLevel);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 5);

        Assert.False(canUpgrade, $"Expected {mutation.Name} not to be upgradable when already maxed.");
    }

    [Fact]
    public void CanUpgrade_returns_false_when_surge_is_currently_active()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        SatisfyPrerequisites(player, mutation);
        player.TryUpgradeMutation(mutation, new TestSimulationObserver(), currentRound: 2);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 3);

        Assert.False(canUpgrade, $"Expected active surge {mutation.Name} not to be upgradable while active.");
    }

    [Fact]
    public void CanUpgrade_returns_true_when_requirements_are_met()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        SatisfyPrerequisites(player, mutation);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 5);

        Assert.True(canUpgrade, $"Expected {mutation.Name} to be upgradable when prerequisites and mutation points are satisfied.");
    }

    [Fact]
    public void TryUpgradeMutation_for_standard_mutation_spends_points_increases_level_and_records_manual_upgrade()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.MycelialBloom);

        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 1);

        Assert.True(upgraded, $"Expected standard mutation {mutation.Name} to upgrade successfully.");
        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
        Assert.Equal(10 - mutation.PointsPerUpgrade, player.MutationPoints);
        Assert.Equal(mutation.PointsPerUpgrade, observer.LastMutationPointsSpent);
        Assert.Equal("manual", observer.LastUpgradeSource);
        Assert.Equal(1, observer.UpgradeEventCount);
    }

    [Fact]
    public void TryUpgradeMutation_for_surge_spends_points_activates_surge_and_records_surge_upgrade()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        SatisfyPrerequisites(player, mutation);
        int expectedCost = player.GetMutationPointCost(mutation);

        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 2);

        Assert.True(upgraded, $"Expected surge mutation {mutation.Name} to activate successfully.");
        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
        Assert.True(player.IsSurgeActive(mutation.Id), $"Expected surge mutation {mutation.Name} to be active after upgrade.");
        Assert.Equal(mutation.SurgeDuration, player.GetSurgeTurnsRemaining(mutation.Id));
        Assert.Equal(99 - expectedCost, player.MutationPoints);
        Assert.Equal(expectedCost, observer.LastMutationPointsSpent);
        Assert.Equal("surge", observer.LastUpgradeSource);
    }

    [Fact]
    public void TryUpgradeMutation_enforces_one_round_delay_after_prerequisites_are_first_met()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var prerequisite = RequireMutation(MutationIds.MycelialBloom);
        var mutation = RequireMutation(MutationIds.HyphalSurge);

        player.SetMutationLevel(prerequisite.Id, newLevel: 4, currentRound: 3);

        var prereqUpgraded = player.TryUpgradeMutation(prerequisite, observer, currentRound: 4);
        var sameRoundUpgrade = player.TryUpgradeMutation(mutation, observer, currentRound: 4);
        var nextRoundUpgrade = player.TryUpgradeMutation(mutation, observer, currentRound: 5);

        Assert.True(prereqUpgraded, $"Expected prerequisite mutation {prerequisite.Name} to upgrade successfully.");
        Assert.False(sameRoundUpgrade, $"Expected {mutation.Name} to be blocked in the same round its prerequisites were first satisfied.");
        Assert.True(nextRoundUpgrade, $"Expected {mutation.Name} to become upgradable on the following round.");
    }

    [Fact]
    public void TryAutoUpgrade_can_bypass_missing_prerequisites_for_special_effects()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.HyphalSurge);

        var upgraded = player.TryAutoUpgrade(mutation, currentRound: 6, observer);

        Assert.True(upgraded, $"Expected TryAutoUpgrade to bypass prerequisites for {mutation.Name}.");
        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
        Assert.Equal("auto", observer.LastUpgradeSource);
        Assert.Equal(1, observer.UpgradeEventCount);
    }

    [Fact]
    public void SetMutationLevel_can_raise_mutation_level_without_prerequisites_for_special_effects()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.OntogenicRegression);

        player.SetMutationLevel(mutation.Id, newLevel: 1, currentRound: 6);

        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
    }

    [Fact]
    public void TryUpgradeMutation_awards_apical_yield_bonus_when_mutation_reaches_max_level()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var adaptation = RequireAdaptation(AdaptationIds.ApicalYield);
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        player.TryAddAdaptation(adaptation);
        player.SetMutationLevel(mutation.Id, mutation.MaxLevel - 1);
        int startingPoints = player.MutationPoints;

        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 6);

        Assert.True(upgraded, $"Expected {mutation.Name} to reach max level successfully.");
        Assert.Equal(mutation.MaxLevel, player.GetMutationLevel(mutation.Id));
        Assert.Equal(startingPoints - mutation.PointsPerUpgrade + AdaptationGameBalance.ApicalYieldMutationPointAward, player.MutationPoints);
        Assert.Equal(AdaptationGameBalance.ApicalYieldMutationPointAward, observer.LastApicalYieldBonus);
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

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }

    private static void SatisfyPrerequisites(Player player, Mutation mutation)
    {
        foreach (var prerequisite in mutation.Prerequisites)
        {
            player.SetMutationLevel(prerequisite.MutationId, prerequisite.RequiredLevel, currentRound: 1);
        }
    }
}
