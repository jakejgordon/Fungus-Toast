using FungusToast.Core.Board;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Tests.Board;

public class FungalCellHelperTests
{
    [Fact]
    public void CloneForRelocation_preserves_key_living_state_and_moves_tile_id()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.SetBirthRound(5);
        cell.SetGrowthCycleAge(3);
        cell.MarkAsNewlyGrown();
        cell.MakeResistant("Initial Spore");

        var clone = cell.CloneForRelocation(newTileId: 27, source: GrowthSource.HyphalDraw);

        Assert.NotSame(cell, clone);
        Assert.True(clone.IsAlive, "Expected relocated clone of a living cell to remain alive.");
        Assert.Equal(27, clone.TileId);
        Assert.Equal(1, clone.OwnerPlayerId);
        Assert.Equal(cell.OriginalOwnerPlayerId, clone.OriginalOwnerPlayerId);
        Assert.Equal(cell.LastOwnerPlayerId, clone.LastOwnerPlayerId);
        Assert.Equal(5, clone.BirthRound);
        Assert.Equal(3, clone.GrowthCycleAge);
        Assert.True(clone.IsResistant, "Expected relocated clone to preserve resistance.");
        Assert.Equal("Initial Spore", clone.ResistanceSource);
        Assert.False(clone.IsNewlyGrown, "Expected relocated clone not to carry transient newly-grown animation state.");
        Assert.Equal(GrowthSource.HyphalDraw, clone.SourceOfGrowth);
    }

    [Fact]
    public void Newly_grown_flag_can_be_set_and_cleared()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.MarkAsNewlyGrown();
        Assert.True(cell.IsNewlyGrown);

        cell.ClearNewlyGrownFlag();
        Assert.False(cell.IsNewlyGrown);
    }

    [Fact]
    public void Dying_flag_can_be_set_and_cleared()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.MarkAsDying();
        Assert.True(cell.IsDying);

        cell.ClearDyingFlag();
        Assert.False(cell.IsDying);
    }

    [Fact]
    public void Toxin_drop_flag_can_be_set_and_cleared()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.MarkAsReceivingToxinDrop();
        Assert.True(cell.IsReceivingToxinDrop);

        cell.ClearToxinDropFlag();
        Assert.False(cell.IsReceivingToxinDrop);
    }

    [Fact]
    public void Growth_age_helpers_increment_reset_and_reduce_age_without_going_below_zero()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.IncrementGrowthAge();
        cell.IncrementGrowthAge();
        Assert.Equal(2, cell.GrowthCycleAge);

        var reducedBy = cell.ReduceGrowthCycleAge(amount: 5);
        Assert.Equal(2, reducedBy);
        Assert.Equal(0, cell.GrowthCycleAge);

        cell.IncrementGrowthAge();
        cell.ResetGrowthCycleAge();
        Assert.Equal(0, cell.GrowthCycleAge);
    }

    [Fact]
    public void GrewThisRound_matches_birth_round()
    {
        var cell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.SetBirthRound(4);

        Assert.True(cell.GrewThisRound(4));
        Assert.False(cell.GrewThisRound(5));
    }
}
