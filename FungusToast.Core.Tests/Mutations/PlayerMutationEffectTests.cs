using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerMutationEffectTests
{
    [Fact]
    public void GetDiagonalGrowthChance_returns_zero_when_player_has_no_matching_tendril_mutation()
    {
        var player = CreatePlayer();

        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Northwest));
        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Northeast));
        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Southeast));
        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Southwest));
    }

    [Fact]
    public void GetDiagonalGrowthChance_returns_effect_for_matching_direction_only()
    {
        var player = CreatePlayer();
        var northwest = RequireMutation(MutationIds.TendrilNorthwest);
        var southeast = RequireMutation(MutationIds.TendrilSoutheast);

        player.SetMutationLevel(northwest.Id, newLevel: 2, currentRound: 1);
        player.SetMutationLevel(southeast.Id, newLevel: 1, currentRound: 1);

        Assert.Equal(northwest.GetTotalEffect(2), player.GetDiagonalGrowthChance(DiagonalDirection.Northwest), precision: 6);
        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Northeast));
        Assert.Equal(southeast.GetTotalEffect(1), player.GetDiagonalGrowthChance(DiagonalDirection.Southeast), precision: 6);
        Assert.Equal(0f, player.GetDiagonalGrowthChance(DiagonalDirection.Southwest));
    }

    [Fact]
    public void GetEffectiveOrthogonalGrowthChance_applies_stacked_tendril_penalty()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNorthwest, newLevel: 2, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilSoutheast, newLevel: 3, currentRound: 1);

        var orthogonalGrowthChance = GrowthMutationProcessor.GetEffectiveOrthogonalGrowthChance(player);

        Assert.Equal(0.02f, orthogonalGrowthChance, precision: 6);
    }

    [Fact]
    public void GetEffectiveOrthogonalGrowthChance_clamps_to_tendril_floor()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNorthwest, newLevel: 5, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNortheast, newLevel: 5, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilSoutheast, newLevel: 5, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilSouthwest, newLevel: 5, currentRound: 1);

        var orthogonalGrowthChance = GrowthMutationProcessor.GetEffectiveOrthogonalGrowthChance(player);

        Assert.Equal(GameBalance.TendrilOrthogonalGrowthMinimumChance, orthogonalGrowthChance, precision: 6);
    }

    [Fact]
    public void GetEffectiveDirectionalDiagonalGrowthChance_applies_mycotropic_induction_multiplier()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.TendrilNorthwest, newLevel: 2, currentRound: 1);
        player.SetMutationLevel(MutationIds.MycotropicInduction, newLevel: 3, currentRound: 1);

        var diagonalGrowthChance = GrowthMutationProcessor.GetEffectiveDirectionalDiagonalGrowthChance(player, DiagonalDirection.Northwest);
        float expectedChance = RequireMutation(MutationIds.TendrilNorthwest).GetTotalEffect(2)
            * (1f + (RequireMutation(MutationIds.MycotropicInduction).EffectPerLevel * 3));

        Assert.Equal(expectedChance, diagonalGrowthChance, precision: 6);
    }

    [Fact]
    public void SetBaseMutationPoints_changes_base_and_current_mutation_point_income()
    {
        var player = CreatePlayer();

        player.SetBaseMutationPoints(7);

        Assert.Equal(7, player.GetBaseMutationPointIncome());
        Assert.Equal(7, player.GetMutationPointIncome());
    }

    [Fact]
    public void TryAutoUpgrade_records_apical_yield_bonus_when_auto_upgrade_reaches_max_level()
    {
        var player = CreatePlayer();
        var observer = new TestSimulationObserver();
        var adaptation = RequireAdaptation(FungusToast.Core.Campaign.AdaptationIds.ApicalYield);
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        player.TryAddAdaptation(adaptation);
        player.SetMutationLevel(mutation.Id, mutation.MaxLevel - 1, currentRound: 5);

        var upgraded = player.TryAutoUpgrade(mutation, currentRound: 6, observer);

        Assert.True(upgraded);
        Assert.Equal(mutation.MaxLevel, player.GetMutationLevel(mutation.Id));
        Assert.Equal(AdaptationGameBalance.ApicalYieldMutationPointAward, observer.LastApicalYieldBonus);
    }

    private static Player CreatePlayer()
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static FungusToast.Core.Campaign.AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = FungusToast.Core.Campaign.AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<FungusToast.Core.Campaign.AdaptationDefinition>(adaptation);
    }
}
