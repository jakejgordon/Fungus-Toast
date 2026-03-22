using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Phases;

public class DecayPhaseContextTests
{
    [Fact]
    public void IsCompetitiveTargetingNeeded_returns_false_when_no_player_has_competitive_antagonism_active()
    {
        var players = new List<Player>
        {
            new(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI),
            new(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI)
        };
        var context = new DecayPhaseContext(new GameBoard(width: 5, height: 5, playerCount: 2), players);

        Assert.False(context.IsCompetitiveTargetingNeeded());
    }

    [Fact]
    public void IsCompetitiveTargetingNeeded_returns_false_when_competitive_antagonism_is_active_without_supported_mutations()
    {
        var player = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        player.ActiveSurges[MutationIds.CompetitiveAntagonism] = new Player.ActiveSurgeInfo(MutationIds.CompetitiveAntagonism, level: 1, duration: 2);
        var players = new List<Player> { player };
        var context = new DecayPhaseContext(new GameBoard(width: 5, height: 5, playerCount: 1), players);

        Assert.False(context.IsCompetitiveTargetingNeeded());
    }

    [Fact]
    public void IsCompetitiveTargetingNeeded_returns_true_when_competitive_antagonism_is_active_with_supported_mutation()
    {
        var player = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        player.ActiveSurges[MutationIds.CompetitiveAntagonism] = new Player.ActiveSurgeInfo(MutationIds.CompetitiveAntagonism, level: 1, duration: 2);
        player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 1, currentRound: 1);
        var players = new List<Player> { player };
        var context = new DecayPhaseContext(new GameBoard(width: 5, height: 5, playerCount: 1), players);

        Assert.True(context.IsCompetitiveTargetingNeeded());
    }

    [Fact]
    public void GetColonySizeCategorization_returns_cached_result_for_repeated_calls()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var p0 = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        var p1 = new Player(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI);
        board.Players.Add(p0);
        board.Players.Add(p1);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 3);
        board.SpawnSporeForPlayer(p1, tileId: 17, FungusToast.Core.Growth.GrowthSource.HyphalOutgrowth);

        var context = new DecayPhaseContext(board, new List<Player> { p0, p1 });
        var first = context.GetColonySizeCategorization(p0);
        var second = context.GetColonySizeCategorization(p0);

        Assert.Single(first.largerColonies);
        Assert.Empty(first.smallerColonies);
        Assert.Same(first.largerColonies, second.largerColonies);
        Assert.Same(first.smallerColonies, second.smallerColonies);
        Assert.Equal(1, first.largerColonies[0].PlayerId);
    }
}
