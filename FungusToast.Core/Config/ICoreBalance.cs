namespace FungusToast.Core.Config;

public interface ICoreBalance
{
    float AdaptiveExpressionEffectPerLevel { get; }
    float AdaptiveExpressionSecondPointChancePerLevel { get; }

    float AnabolicInversionGapBonusPerLevel { get; }
    float AnabolicInversionHighRewardCutoff { get; }
    float AnabolicInversionMidRewardCutoff { get; }
    float AnabolicInversionLowRewardCutoff { get; }
    int AnabolicInversionMaxMutationPointsPerRound { get; }
}
