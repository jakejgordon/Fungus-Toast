namespace FungusToast.Core.Config
{
    public static class RotBalance
    {
        // Provisional introductory tuning: five percentage points per Decay Phase.
        public const float AdjacentDeathChance = 0.05f;
        public const float IntroductoryReachWidthFactor = 0.55f;
        public const float IntroductoryHalfThicknessHeightFactor = 0.10f;
        public const float StartingSporeClearanceSizeFactor = 0.10f;
        public const int MinimumStartingSporeClearance = 4;
    }
}
