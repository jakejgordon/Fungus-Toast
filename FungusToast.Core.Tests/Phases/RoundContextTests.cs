using FungusToast.Core.Phases;

namespace FungusToast.Core.Tests.Phases;

public class RoundContextTests
{
    [Fact]
    public void GetEffectCount_returns_zero_for_missing_player_effect_pair()
    {
        var context = new RoundContext();

        var count = context.GetEffectCount(playerId: 7, effect: "CatabolizedMP");

        Assert.Equal(0, count);
    }

    [Fact]
    public void IncrementEffectCount_initializes_and_accumulates_per_player_and_effect()
    {
        var context = new RoundContext();

        context.IncrementEffectCount(playerId: 1, effect: "CatabolizedMP");
        context.IncrementEffectCount(playerId: 1, effect: "CatabolizedMP", delta: 2);
        context.IncrementEffectCount(playerId: 2, effect: "CatabolizedMP", delta: 5);
        context.IncrementEffectCount(playerId: 1, effect: "BankedMP", delta: 4);

        Assert.Equal(3, context.GetEffectCount(playerId: 1, effect: "CatabolizedMP"));
        Assert.Equal(5, context.GetEffectCount(playerId: 2, effect: "CatabolizedMP"));
        Assert.Equal(4, context.GetEffectCount(playerId: 1, effect: "BankedMP"));
    }

    [Fact]
    public void Reset_clears_all_effect_counts()
    {
        var context = new RoundContext();
        context.IncrementEffectCount(playerId: 1, effect: "CatabolizedMP", delta: 3);
        context.IncrementEffectCount(playerId: 2, effect: "BankedMP", delta: 4);

        context.Reset();

        Assert.Equal(0, context.GetEffectCount(playerId: 1, effect: "CatabolizedMP"));
        Assert.Equal(0, context.GetEffectCount(playerId: 2, effect: "BankedMP"));
    }

    [Fact]
    public void ToString_includes_one_line_per_recorded_effect_count()
    {
        var context = new RoundContext();
        context.IncrementEffectCount(playerId: 1, effect: "CatabolizedMP", delta: 3);
        context.IncrementEffectCount(playerId: 2, effect: "BankedMP", delta: 4);

        var text = context.ToString();

        Assert.Contains("Player 1, Effect CatabolizedMP: 3", text);
        Assert.Contains("Player 2, Effect BankedMP: 4", text);
    }
}
