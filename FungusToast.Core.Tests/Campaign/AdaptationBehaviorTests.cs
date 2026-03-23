using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class AdaptationBehaviorTests
{
    [Fact]
    public void ApicalYield_awards_bonus_points_when_a_mutation_reaches_max_level()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var observer = new TestSimulationObserver();
        var mutation = RequireMutation(MutationIds.MycelialBloom);
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.ApicalYield));
        player.SetMutationLevel(mutation.Id, mutation.MaxLevel - 1, currentRound: 1);

        var upgraded = player.TryUpgradeMutation(mutation, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(10 - mutation.PointsPerUpgrade + AdaptationGameBalance.ApicalYieldMutationPointAward, player.MutationPoints);
        Assert.Equal(AdaptationGameBalance.ApicalYieldMutationPointAward, observer.LastApicalYieldBonus);
    }

    [Fact]
    public void HyphalEconomy_reduces_surge_activation_cost_by_configured_amount()
    {
        var player = CreatePlayer();
        var mutation = RequireMutation(MutationIds.HyphalSurge);
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalEconomy));

        var cost = player.GetMutationPointCost(mutation);

        Assert.Equal(
            Math.Max(0, mutation.GetSurgeActivationCost(currentLevel: 0) - AdaptationGameBalance.HyphalEconomySurgeCostReduction),
            cost);
    }

    [Fact]
    public void AegisHyphae_fortifies_only_the_first_new_living_cell_each_round()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.AegisHyphae));

        board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.HyphalOutgrowth);
        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 6, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        board.SpawnSporeForPlayer(player, tileId: 7, GrowthSource.HyphalOutgrowth);
        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 7, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.True(board.GetCell(6)!.IsResistant);
        Assert.False(board.GetCell(7)!.IsResistant);
        Assert.Equal(1, board.CurrentRoundContext.GetEffectCount(player.PlayerId, "adaptation_aegis_hyphae_growths"));
    }

    [Fact]
    public void CrustalCallus_fortifies_new_edge_cells_but_not_interior_cells()
    {
        var board = CreateBoardWithPlayer(out var player);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.CrustalCallus));

        board.SpawnSporeForPlayer(player, tileId: 5, GrowthSource.HyphalOutgrowth); // edge tile on 5x5 board
        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 5, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        board.SpawnSporeForPlayer(player, tileId: 12, GrowthSource.HyphalOutgrowth); // interior tile
        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 12, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.True(board.GetCell(5)!.IsResistant);
        Assert.False(board.GetCell(12)!.IsResistant);
    }

    private static Player CreatePlayer(int mutationPoints = 0)
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }

    private static GameBoard CreateBoardWithPlayer(out Player player)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        player = CreatePlayer();
        board.Players.Add(player);
        return board;
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
}
