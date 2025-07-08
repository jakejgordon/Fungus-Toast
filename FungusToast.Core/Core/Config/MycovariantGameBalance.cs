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

        public const float HyphalResistanceTransferChance = 0.10f;

        public const float PerimeterProliferatorEdgeMultiplier = 2.0f;

        public const int EnduringToxaphoresNewToxinExtension = 7; // X: cycles added to new toxins
        public const int EnduringToxaphoresExistingToxinExtension = 3; // Y: cycles added to existing toxins at acquisition

        public const float ReclamationRhizomorphsSecondAttemptChance = 0.25f; // 25% chance for second reclamation attempt

        // AI scoring for Mycovariants
        public const float MycelialBastionIBaseAIScore = 4f;
        public const float MycelialBastionIIBaseAIScore = 5f;
        public const float MycelialBastionIIIBaseAIScore = 6f;
        public const float MycelialBastionSynergyBonusAIScore = 6f;
        public const float ReclamationRhizomorphsBaseAIScoreEarly = 6f;
        public const float ReclamationRhizomorphsBaseAIScoreLate = 3f;
        public const float ReclamationRhizomorphsBonusAIScore = 6f;

        public const float AIDraftModeratePriority = 6f;
        public const float HyphalResistanceTransferBaseAIScoreEarly = 5f;
        public const float HyphalResistanceTransferBaseAIScoreLate = 3f;
        public const float MycovariantSynergyBonus = 3f;
    }
}
