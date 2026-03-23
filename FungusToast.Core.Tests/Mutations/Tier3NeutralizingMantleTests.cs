using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier3NeutralizingMantleTests
{
    [Fact]
    public void NeutralizingMantle_is_tier3_fungicide_and_requires_mycotoxin_potentiation_level_five()
    {
        var mutation = RequireMutation(MutationIds.NeutralizingMantle);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.ToxinNeutralization, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MycotoxinPotentiation, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void ApplyNeutralizingMantle_returns_zero_when_player_has_no_mantle_level()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 2, y: 2);
        ToxinHelper.ConvertToToxin(board, tileId: 11, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);
        var observer = new TestSimulationObserver();

        var neutralized = FungicideMutationProcessor.ApplyNeutralizingMantle(player, board, observer);

        Assert.Equal(0, neutralized);
        Assert.NotNull(board.GetCell(11));
    }

    [Fact]
    public void ApplyNeutralizingMantle_removes_adjacent_toxins_but_not_distant_ones()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        player.SetMutationLevel(MutationIds.NeutralizingMantle, newLevel: 1, currentRound: 1);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 2, y: 2); // tile 12
        ToxinHelper.ConvertToToxin(board, tileId: 11, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);
        ToxinHelper.ConvertToToxin(board, tileId: 13, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);
        ToxinHelper.ConvertToToxin(board, tileId: 0, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);
        var observer = new TestSimulationObserver();

        var neutralized = FungicideMutationProcessor.ApplyNeutralizingMantle(player, board, observer);

        Assert.Equal(2, neutralized);
        Assert.Null(board.GetCell(11));
        Assert.Null(board.GetCell(13));
        Assert.NotNull(board.GetCell(0));
    }

    [Fact]
    public void ApplyNeutralizingMantle_counts_each_adjacent_toxin_once_even_when_multiple_owned_cells_touch_it()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        player.SetMutationLevel(MutationIds.NeutralizingMantle, newLevel: 1, currentRound: 1);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 1, y: 2); // tile 11
        board.SpawnSporeForPlayer(player, tileId: 13, GrowthSource.HyphalOutgrowth); // tile 13, same toxin adjacent to both via 12
        ToxinHelper.ConvertToToxin(board, tileId: 12, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);
        var observer = new TestSimulationObserver();

        var neutralized = FungicideMutationProcessor.ApplyNeutralizingMantle(player, board, observer);

        Assert.Equal(1, neutralized);
        Assert.Null(board.GetCell(12));
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static GameBoard CreateBoardWithPlayers(out Player player, out Player enemy)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        player = new Player(0, "P0", PlayerTypeEnum.AI);
        enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        return board;
    }
}
