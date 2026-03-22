using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Players;

public class PlayerMycovariantTests
{
    [Fact]
    public void MarkTriggered_sets_has_triggered_true()
    {
        var mycovariant = CreatePlayerMycovariant();

        mycovariant.MarkTriggered();

        Assert.True(mycovariant.HasTriggered);
    }

    [Fact]
    public void IncrementEffectCount_initializes_and_accumulates_counts_by_effect_type()
    {
        var mycovariant = CreatePlayerMycovariant();

        mycovariant.IncrementEffectCount(MycovariantEffectType.Colonized, 2);
        mycovariant.IncrementEffectCount(MycovariantEffectType.Colonized, 3);
        mycovariant.IncrementEffectCount(MycovariantEffectType.Reclaimed, 1);

        Assert.Equal(5, mycovariant.EffectCounts[MycovariantEffectType.Colonized]);
        Assert.Equal(1, mycovariant.EffectCounts[MycovariantEffectType.Reclaimed]);
    }

    [Fact]
    public void AIScoreAtDraft_round_trips_as_mutable_metadata()
    {
        var mycovariant = CreatePlayerMycovariant();

        mycovariant.AIScoreAtDraft = 7.5f;

        Assert.Equal(7.5f, mycovariant.AIScoreAtDraft);
    }

    [Fact]
    public void ColonizeHandler_round_trips_as_assignable_delegate()
    {
        var mycovariant = CreatePlayerMycovariant();
        int lastFrom = -1;
        int lastTo = -1;
        mycovariant.ColonizeHandler = (from, to) =>
        {
            lastFrom = from;
            lastTo = to;
        };

        mycovariant.ColonizeHandler(4, 9);

        Assert.Equal(4, lastFrom);
        Assert.Equal(9, lastTo);
    }

    private static PlayerMycovariant CreatePlayerMycovariant()
    {
        var mycovariant = new Mycovariant
        {
            Id = 1234,
            Name = "Test Mycovariant"
        };

        return new PlayerMycovariant(playerId: 0, mycovariantId: mycovariant.Id, mycovariant: mycovariant);
    }
}
