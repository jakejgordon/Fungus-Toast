namespace FungusToast.Core.Board
{
    public enum NutrientRewardType
    {
        MutationPoints = 1
    }

    public sealed class NutrientPatch
    {
        public NutrientPatch(
            string displayName,
            string description,
            NutrientRewardType rewardType,
            int rewardAmount)
        {
            DisplayName = displayName;
            Description = description;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }

        public string DisplayName { get; }
        public string Description { get; }
        public NutrientRewardType RewardType { get; }
        public int RewardAmount { get; }

        public static NutrientPatch CreateDefaultMutationPointPatch()
        {
            return new NutrientPatch(
                "Nutrient Patch",
                "Rich in nutrients. When a living mold cell first grows orthogonally adjacent, the patch is consumed and grants +1 Mutation Point.",
                NutrientRewardType.MutationPoints,
                1);
        }
    }
}