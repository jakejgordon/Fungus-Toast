using FungusToast.Core.Players;

namespace FungusToast.Core.Config
{
    public static class MycovariantGameBalance
    {
        public const int MycovariantSelectionTriggerRound = 15; 
        public const int MycovariantSelectionDraftSize = 3;

        // Shared balance parameters
        public const int JettingMyceliumNumberOfLivingCellTiles = 4;
        public const int JettingMyceliumNumberOfToxinTiles = 10;
        public const int DefaultJettingMyceliumToxinGrowthCycleDuration = 16;

        public const int PlasmidBountyMutationPointAward = 7;
        public const int PlasmidBountyIIMutationPointAward = 9;
        public const int PlasmidBountyIIIMutationPointAward = 11;

        public const float NeutralizingMantleNeutralizeChance = 0.20f;

        public const int MycelialBastionIMaxResistantCells = 5;
        public const int MycelialBastionIIMaxResistantCells = 8;
        public const int MycelialBastionIIIMaxResistantCells = 12;

        public const float PerimeterProliferatorEdgeMultiplier = 2.0f;
    }
}
