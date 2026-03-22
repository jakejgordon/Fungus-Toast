using FungusToast.Core.Board;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Tests.Board;

public class FungalCellResistanceSourceTests
{
    [Fact]
    public void MakeResistant_without_explicit_source_uses_growth_source_name()
    {
        var cell = new FungalCell(ownerPlayerId: 0, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        cell.MakeResistant();

        Assert.True(cell.IsResistant);
        Assert.Equal("Initial Spore", cell.ResistanceSource);
    }

    [Fact]
    public void MakeResistant_with_explicit_source_uses_ability_name()
    {
        var cell = new FungalCell(ownerPlayerId: 0, tileId: 12, source: GrowthSource.HyphalOutgrowth, lastOwnerPlayerId: null);

        cell.MakeResistant("Mycelial Bastion");

        Assert.True(cell.IsResistant);
        Assert.Equal("Mycelial Bastion", cell.ResistanceSource);
    }

    [Fact]
    public void CloneForRelocation_preserves_resistance_source()
    {
        var cell = new FungalCell(ownerPlayerId: 0, tileId: 12, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.MakeResistant();

        var clone = cell.CloneForRelocation(newTileId: 30, GrowthSource.HyphalDraw);

        Assert.True(clone.IsResistant);
        Assert.Equal("Initial Spore", clone.ResistanceSource);
    }
}
