using FungusToast.Core.Campaign;

namespace FungusToast.Core.Tests.Campaign;

public class MoldinessProgressionTests
{
    [Fact]
    public void Final_campaign_victory_reward_is_at_least_double_previous_level_reward()
    {
        int reward = MoldinessProgression.GetRewardForClearedLevel(clearedLevelDisplay: 15, isFinalCampaignVictory: true);

        Assert.Equal(14, reward);
    }

    [Fact]
    public void Non_final_clear_uses_authored_reward_curve()
    {
        int reward = MoldinessProgression.GetRewardForClearedLevel(clearedLevelDisplay: 15, isFinalCampaignVictory: false);

        Assert.Equal(8, reward);
    }

    [Fact]
    public void Apply_award_rolls_multiple_thresholds_and_preserves_overflow()
    {
        var state = new MoldinessProgressionState
        {
            currentProgress = 5,
            currentTierIndex = 0,
            lifetimeEarned = 5
        };

        var result = MoldinessProgression.ApplyAward(state, 10);

        Assert.Equal(10, result.AmountAwarded);
        Assert.Equal(5, result.PreviousProgress);
        Assert.Equal(0, result.NewProgress);
        Assert.Equal(0, result.PreviousTierIndex);
        Assert.Equal(2, result.NewTierIndex);
        Assert.Equal(12, result.CurrentThreshold);
        Assert.Equal(15, result.LifetimeEarned);
        Assert.Equal(2, result.UnlockTriggers.Count);
        Assert.Equal(new[] { 6, 9 }, result.UnlockTriggers.Select(trigger => trigger.threshold).ToArray());
        Assert.Equal(2, state.pendingUnlockTriggers.Count);
    }

    [Fact]
    public void Thresholds_continue_with_plus_four_growth_after_authored_table()
    {
        Assert.Equal(34, MoldinessProgression.GetThresholdForTier(9));
        Assert.Equal(38, MoldinessProgression.GetThresholdForTier(10));
        Assert.Equal(42, MoldinessProgression.GetThresholdForTier(11));
    }
}
