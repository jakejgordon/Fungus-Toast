using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerSurgeStateTests
{
    [Fact]
    public void IsSurgeActive_returns_false_and_turns_remaining_zero_when_surge_is_missing()
    {
        var player = CreatePlayer();

        Assert.False(player.IsSurgeActive(MutationIds.HyphalSurge));
        Assert.Equal(0, player.GetSurgeTurnsRemaining(MutationIds.HyphalSurge));
    }

    [Fact]
    public void TickDownActiveSurges_decrements_turns_remaining_for_active_surges()
    {
        var player = CreatePlayer();
        player.ActiveSurges[MutationIds.HyphalSurge] = new Player.ActiveSurgeInfo(mutationId: MutationIds.HyphalSurge, level: 2, duration: 3);

        player.TickDownActiveSurges();

        Assert.True(player.IsSurgeActive(MutationIds.HyphalSurge));
        Assert.Equal(2, player.GetSurgeTurnsRemaining(MutationIds.HyphalSurge));
    }

    [Fact]
    public void TickDownActiveSurges_removes_expired_surges()
    {
        var player = CreatePlayer();
        player.ActiveSurges[MutationIds.HyphalSurge] = new Player.ActiveSurgeInfo(mutationId: MutationIds.HyphalSurge, level: 1, duration: 1);

        player.TickDownActiveSurges();

        Assert.False(player.IsSurgeActive(MutationIds.HyphalSurge));
        Assert.Equal(0, player.GetSurgeTurnsRemaining(MutationIds.HyphalSurge));
    }

    [Fact]
    public void RemoveActiveSurge_removes_only_the_requested_surge()
    {
        var player = CreatePlayer();
        player.ActiveSurges[MutationIds.HyphalSurge] = new Player.ActiveSurgeInfo(mutationId: MutationIds.HyphalSurge, level: 1, duration: 2);
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(mutationId: MutationIds.ChemotacticBeacon, level: 1, duration: 2);

        player.RemoveActiveSurge(MutationIds.HyphalSurge);

        Assert.False(player.IsSurgeActive(MutationIds.HyphalSurge));
        Assert.True(player.IsSurgeActive(MutationIds.ChemotacticBeacon));
        Assert.Equal(2, player.GetSurgeTurnsRemaining(MutationIds.ChemotacticBeacon));
    }

    private static Player CreatePlayer()
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
    }
}
