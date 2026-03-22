namespace FungusToast.Core.Config;

public sealed class DefaultCoreBalance : ICoreBalance
{
    public float AdaptiveExpressionEffectPerLevel => GameBalance.AdaptiveExpressionEffectPerLevel;
    public float AdaptiveExpressionSecondPointChancePerLevel => GameBalance.AdaptiveExpressionSecondPointChancePerLevel;

    public float AnabolicInversionGapBonusPerLevel => GameBalance.AnabolicInversionGapBonusPerLevel;
    public float AnabolicInversionHighRewardCutoff => GameBalance.AnabolicInversionHighRewardCutoff;
    public float AnabolicInversionMidRewardCutoff => GameBalance.AnabolicInversionMidRewardCutoff;
    public float AnabolicInversionLowRewardCutoff => GameBalance.AnabolicInversionLowRewardCutoff;
    public int AnabolicInversionMaxMutationPointsPerRound => GameBalance.AnabolicInversionMaxMutationPointsPerRound;
}
