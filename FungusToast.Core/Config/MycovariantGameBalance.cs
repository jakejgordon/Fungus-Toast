using FungusToast.Core.Players;

namespace FungusToast.Core.Config
{
    public static class MycovariantGameBalance
    {
        public static readonly int[] MycovariantSelectionTriggerRounds = new[] { 15, 20, 25 };
        public const int MycovariantSelectionDraftSize = 3;

        // Shared balance parameters
        public const int JettingMyceliumNumberOfLivingCellTiles = 4;
        public const int DefaultJettingMyceliumToxinGrowthCycleDuration = 17;

        // Jetting Mycelium cone effect parameters
        public const int JettingMyceliumConeNarrowLength = 4;  // First 4 toxins: 1 tile wide
        public const int JettingMyceliumConeMediumLength = 3;  // Next 3 toxins: 3 tiles wide
        public const int JettingMyceliumConeWideLength = 3;    // Last 3 toxins: 5 tiles wide
        public const int JettingMyceliumConeNarrowWidth = 1;
        public const int JettingMyceliumConeMediumWidth = 3;
        public const int JettingMyceliumConeWideWidth = 5;

        public const int PlasmidBountyMutationPointAward = 7;
        public const int PlasmidBountyIIMutationPointAward = 11;
        public const int PlasmidBountyIIIMutationPointAward = 15;

        public const float NeutralizingMantleNeutralizeChance = 0.20f;

        public const int MycelialBastionIMaxResistantCells = 8;
        public const int MycelialBastionIIMaxResistantCells = 11;
        public const int MycelialBastionIIIMaxResistantCells = 16;

        public const float HyphalResistanceTransferChance = 0.12f;

        public const float PerimeterProliferatorEdgeMultiplier = 2.5f;

        public const int EnduringToxaphoresNewToxinExtension = 8; // X: cycles added to new toxins
        public const int EnduringToxaphoresExistingToxinExtension = 4; // Y: cycles added to existing toxins at acquisition

        public const float ReclamationRhizomorphsSecondAttemptChance = 0.30f;

        public const int BallistosporeDischargeISpores = 12;
        public const int BallistosporeDischargeIISpores = 17;
        public const int BallistosporeDischargeIIISpores = 22;

        // Ballistospore Discharge toxin duration (in growth cycles)
        public const int BallistosporeDischargeToxinDuration = 18;

        // Cytolytic Burst parameters
        public const int CytolyticBurstRadius = 4; // 4-tile radius explosion
        public const float CytolyticBurstToxinChance = 0.65f; // 65% chance to drop toxin per tile
        public const int CytolyticBurstToxinDuration = 15; // Duration of newly created toxins

        public const float NecrophoricAdaptationReclamationChance = .15f;

        // AI scoring for Mycovariants
        public const float MycelialBastionIBaseAIScore = 4f;
        public const float MycelialBastionIIBaseAIScore = 5f;
        public const float MycelialBastionIIIBaseAIScore = 6f;
        public const float MycelialBastionSynergyBonusAIScore = 6f;
        public const float ReclamationRhizomorphsBaseAIScoreEarly = 6f;
        public const float ReclamationRhizomorphsBaseAIScoreLate = 3f;
        public const float ReclamationRhizomorphsBonusAIScore = 6f;

        public const float BallistosporeDischargeIAIScore = 3f;
        public const float BallistosporeDischargeIIAIScore = 4f;
        public const float BallistosporeDischargeIIIAIScore = 6f;

        public const float CytolyticBurstBaseAIScore = 5f;

        public const float AIDraftModeratePriority = 6f;
        public const float HyphalResistanceTransferBaseAIScoreEarly = 5f;
        public const float HyphalResistanceTransferBaseAIScoreLate = 3f;
        public const float MycovariantSynergyBonus = 3f;
    }
}
