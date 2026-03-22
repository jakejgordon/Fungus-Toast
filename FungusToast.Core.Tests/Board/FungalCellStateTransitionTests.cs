using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class FungalCellStateTransitionTests
{
    [Fact]
    public void Kill_turns_living_non_resistant_cell_into_dead_cell()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.Kill(DeathReason.Age);

        Assert.True(cell.IsDead, "Expected Kill to turn a living cell into a dead cell.");
        Assert.False(cell.IsAlive, "Expected killed cell not to remain alive.");
        Assert.Equal(DeathReason.Age, cell.CauseOfDeath);
        Assert.Equal(1, cell.LastOwnerPlayerId);
        Assert.False(cell.IsResistant, "Expected death to clear resistance.");
    }

    [Fact]
    public void Kill_does_not_affect_resistant_cell()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();

        cell.Kill(DeathReason.Age);

        Assert.True(cell.IsAlive, "Expected resistant cell to ignore Kill.");
        Assert.True(cell.IsResistant, "Expected resistant cell to remain resistant after ignored Kill.");
        Assert.Null(cell.CauseOfDeath);
    }

    [Fact]
    public void Reclaim_turns_dead_cell_alive_for_new_owner_and_increments_reclaim_count()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.Kill(DeathReason.Age);

        cell.Reclaim(newOwnerPlayerId: 2, source: GrowthSource.RegenerativeHyphae);

        Assert.True(cell.IsAlive, "Expected reclaim to revive a dead cell.");
        Assert.Equal(2, cell.OwnerPlayerId);
        Assert.Equal(1, cell.LastOwnerPlayerId);
        Assert.Equal(1, cell.ReclaimCount);
        Assert.Equal(GrowthSource.RegenerativeHyphae, cell.SourceOfGrowth);
        Assert.Null(cell.CauseOfDeath);
    }

    [Fact]
    public void Takeover_on_enemy_living_cell_returns_infested_and_changes_owner()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        var result = cell.Takeover(newOwnerPlayerId: 2, source: GrowthSource.HyphalOutgrowth);

        Assert.Equal(FungalCellTakeoverResult.Infested, result);
        Assert.True(cell.IsAlive, "Expected infested cell to remain alive under the new owner.");
        Assert.Equal(2, cell.OwnerPlayerId);
        Assert.Equal(1, cell.LastOwnerPlayerId);
        Assert.Equal(GrowthSource.HyphalOutgrowth, cell.SourceOfGrowth);
        Assert.Null(cell.CauseOfDeath);
    }

    [Fact]
    public void Takeover_on_dead_cell_returns_reclaimed_and_increments_reclaim_count()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.Kill(DeathReason.Age);

        var result = cell.Takeover(newOwnerPlayerId: 2, source: GrowthSource.NecrophyticBloom);

        Assert.Equal(FungalCellTakeoverResult.Reclaimed, result);
        Assert.True(cell.IsAlive, "Expected reclaimed cell to be alive.");
        Assert.Equal(2, cell.OwnerPlayerId);
        Assert.Equal(1, cell.LastOwnerPlayerId);
        Assert.Equal(1, cell.ReclaimCount);
        Assert.Equal(GrowthSource.NecrophyticBloom, cell.SourceOfGrowth);
    }

    [Fact]
    public void Takeover_on_toxin_requires_allow_toxin_to_overgrow_it()
    {
        var toxinCell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 3, lastOwnerPlayerId: null);

        var withoutPermission = toxinCell.Takeover(newOwnerPlayerId: 2, source: GrowthSource.CreepingMold, allowToxin: false);
        var withPermission = toxinCell.Takeover(newOwnerPlayerId: 2, source: GrowthSource.CreepingMold, allowToxin: true);

        Assert.Equal(FungalCellTakeoverResult.Invalid, withoutPermission);
        Assert.Equal(FungalCellTakeoverResult.Overgrown, withPermission);
        Assert.True(toxinCell.IsAlive, "Expected allowed toxin takeover to convert the toxin into a living cell.");
        Assert.Equal(2, toxinCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.CreepingMold, toxinCell.SourceOfGrowth);
    }

    [Fact]
    public void ConvertToToxin_turns_living_cell_into_toxin_and_assigns_new_owner_when_provided()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        var owner = new Player(playerId: 2, playerName: "Player 2", playerType: PlayerTypeEnum.AI);

        cell.ConvertToToxin(toxinLifespan: 4, growthSource: GrowthSource.CytolyticBurst, owner: owner, reason: DeathReason.CytolyticBurst);

        Assert.True(cell.IsToxin, "Expected ConvertToToxin to produce a toxin cell.");
        Assert.Equal(2, cell.OwnerPlayerId);
        Assert.Equal(1, cell.LastOwnerPlayerId);
        Assert.Equal(GrowthSource.CytolyticBurst, cell.SourceOfGrowth);
        Assert.Equal(4, cell.ToxinExpirationAge);
        Assert.False(cell.IsResistant, "Expected toxin conversion to clear resistance.");
    }

    [Fact]
    public void ConvertToToxin_does_not_affect_resistant_cell()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();

        cell.ConvertToToxin(toxinLifespan: 4, growthSource: GrowthSource.CytolyticBurst);

        Assert.True(cell.IsAlive, "Expected resistant cell to ignore toxin conversion.");
        Assert.False(cell.IsToxin, "Expected resistant cell not to become toxin.");
        Assert.True(cell.IsResistant, "Expected resistance to remain when toxin conversion is ignored.");
    }

    [Fact]
    public void HasToxinExpired_returns_true_when_toxin_age_reaches_expiration_age()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 3, lastOwnerPlayerId: null);
        cell.SetGrowthCycleAge(2);
        Assert.False(cell.HasToxinExpired());

        cell.IncrementGrowthAge();

        Assert.True(cell.HasToxinExpired());
    }
}
