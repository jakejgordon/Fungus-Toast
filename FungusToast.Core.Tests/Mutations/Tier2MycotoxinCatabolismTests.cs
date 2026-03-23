using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier2MycotoxinCatabolismTests
{
    [Fact]
    public void MycotoxinCatabolism_is_tier2_genetic_drift_and_requires_mutator_phenotype_level_two()
    {
        var mutation = RequireMutation(MutationIds.MycotoxinCatabolism);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.ToxinCleanupAndMPBonus, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MutatorPhenotype, prereq.MutationId);
        Assert.Equal(2, prereq.RequiredLevel);
    }

    [Fact]
    public void ApplyMycotoxinCatabolism_returns_zero_when_player_has_no_catabolism_level()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        var roundContext = new RoundContext();

        var metabolized = GeneticDriftMutationProcessor.ApplyMycotoxinCatabolism(player, board, new Random(123), roundContext, observer);

        Assert.Equal(0, metabolized);
    }

    [Fact]
    public void ApplyMycotoxinCatabolism_removes_adjacent_toxins_when_cleanup_roll_hits()
    {
        var board = CreateBoardWithPlayers(out var player, out var toxinOwner);
        var observer = new TestSimulationObserver();
        var roundContext = new RoundContext();
        player.SetMutationLevel(MutationIds.MycotoxinCatabolism, newLevel: 8, currentRound: 1);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 2, y: 2);
        ToxinHelper.ConvertToToxin(board, tileId: 11, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: toxinOwner);
        ToxinHelper.ConvertToToxin(board, tileId: 13, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: toxinOwner);

        var metabolized = GeneticDriftMutationProcessor.ApplyMycotoxinCatabolism(player, board, new AlwaysZeroRandom(), roundContext, observer);

        Assert.Equal(2, metabolized);
        Assert.Null(board.GetCell(11));
        Assert.Null(board.GetCell(13));
    }

    [Fact]
    public void ApplyMycotoxinCatabolism_awards_bonus_mutation_points_up_to_round_cap()
    {
        var board = CreateBoardWithPlayers(out var player, out var toxinOwner);
        var observer = new TestSimulationObserver();
        var roundContext = new RoundContext();
        player.SetMutationLevel(MutationIds.MycotoxinCatabolism, newLevel: 8, currentRound: 1);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 2, y: 2);

        foreach (var tileId in new[] { 7, 11, 13, 17 })
        {
            ToxinHelper.ConvertToToxin(board, tileId: tileId, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: toxinOwner);
        }

        var metabolized = GeneticDriftMutationProcessor.ApplyMycotoxinCatabolism(player, board, new AlwaysZeroRandom(), roundContext, observer);

        Assert.True(metabolized >= 3, "Expected enough toxin cleanup attempts to exercise the point cap.");
        Assert.Equal(GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound, roundContext.GetEffectCount(player.PlayerId, "CatabolizedMP"));
        Assert.Equal(GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound, player.MutationPoints);
    }

    [Fact]
    public void ApplyMycotoxinCatabolism_respects_existing_round_context_cap_before_awarding_more_points()
    {
        var board = CreateBoardWithPlayers(out var player, out var toxinOwner);
        var observer = new TestSimulationObserver();
        var roundContext = new RoundContext();
        roundContext.IncrementEffectCount(player.PlayerId, "CatabolizedMP", delta: GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound);
        player.SetMutationLevel(MutationIds.MycotoxinCatabolism, newLevel: 8, currentRound: 1);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 2, y: 2);
        ToxinHelper.ConvertToToxin(board, tileId: 11, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: toxinOwner);

        var metabolized = GeneticDriftMutationProcessor.ApplyMycotoxinCatabolism(player, board, new AlwaysZeroRandom(), roundContext, observer);

        Assert.Equal(1, metabolized);
        Assert.Equal(0, player.MutationPoints);
        Assert.Equal(GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound, roundContext.GetEffectCount(player.PlayerId, "CatabolizedMP"));
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static GameBoard CreateBoardWithPlayers(out Player player, out Player toxinOwner)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        player = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        toxinOwner = new Player(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(toxinOwner);
        return board;
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }
}
