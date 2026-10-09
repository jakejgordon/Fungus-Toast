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

        // Stage 12 alternate: authored against the 120x120 medium hotdog silhouette.
        public const int QuarantineBoardSize = 120;
        public const int QuarantineMaximumPlayers = 7;
        public const int QuarantineBeltStartX = 60;
        public const int QuarantinePocketStartX = 95;
        public const int QuarantineContourCurvature = 35;
        public const int QuarantineCrustSealWidth = 5;
        public const int QuarantineEntranceY = 60;
        public const double QuarantineMinimumPocketFraction = 0.14;
        public const double QuarantineMaximumPocketFraction = 0.16;
        public const int QuarantineStartingRotClearance = 10;
        public const int QuarantineStartingSeparation = 16;
        public const int QuarantineStartingEdgeClearance = 4;
        // Existing starting-position Adaptations remain active; these are post-modifier minima.
        public const int QuarantineEffectiveStartingRotClearance = 5;
        public const int QuarantineEffectiveStartingSeparation = 10;
    }
}
