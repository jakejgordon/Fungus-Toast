using FungusToast.Core.Players;

namespace FungusToast.Core.Config
{
    public static class MycovariantGameBalance
    {
        public static readonly int[] MycovariantSelectionTriggerRounds = new[] { 15, 20, 25, 30 };
        public const int MycovariantSelectionDraftSize = 3;

        // Shared balance parameters
        public const int DefaultJettingMyceliumToxinGrowthCycleDuration = 17;

        // Jetting Mycelium tier patterns
        public const int JettingMyceliumILivingCellTiles = 3;
        public const int JettingMyceliumIILivingCellTiles = 3;
        public const int JettingMyceliumIIILivingCellTiles = 4;
        public static readonly int[] JettingMyceliumIToxinRowWidths = new[] { 3, 3, 5, 7 };
        public static readonly int[] JettingMyceliumIIToxinRowWidths = new[] { 3, 3, 5, 7, 9 };
        public static readonly int[] JettingMyceliumIIIToxinRowWidths = new[] { 3, 3, 5, 7, 9, 11 };

        public const int PlasmidBountyMutationPointAward = 7;
        public const int PlasmidBountyIIMutationPointAward = 11;
        public const int PlasmidBountyIIIMutationPointAward = 15;
        public const int AscusWagerTier5LevelsGranted = 1;
        public const int AscusBaitMutationPointAward = 8;
        public const float AscusBaitSelfCullPercentage = 0.10f;
        public const float AscusBaitPreferredAIScore = 99f;
        public const float AscusBaitFallbackAIScore = 0f;
        public const int SporalSnareMutationPointAward = 10;
        public const float SporalSnarePreferredAIScore = 99f;
        public const float SporalSnareFallbackAIScore = 0f;
        public const int SporalSnareDenseLineMaxBoardDimension = 18;
        public const int SporalSnareMediumLineMaxBoardDimension = 36;
        public const int SporalSnareDenseLineStride = 1;
        public const int SporalSnareMediumLineStride = 2;
        public const int SporalSnareSparseLineStride = 3;

        public const float NeutralizingMantleNeutralizeChance = 0.20f;

        public const int MycelialBastionIMaxResistantCells = 8;
        public const int MycelialBastionIIMaxResistantCells = 11;
        public const int MycelialBastionIIIMaxResistantCells = 16;

        public const float HyphalResistanceTransferChance = 0.12f;
        public const float SeptalAlarmResistanceChance = 0.15f;
        public const float SeptalSealResistancePortionNumerator = 0.30f;
        public const float SeptalSealAIScore = 2f;

        public const float PerimeterProliferatorEdgeMultiplier = 2.5f;
        public const int PerimeterProliferatorEdgeDistance = 2; // Cells within this many tiles of the edge get the bonus

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

        // Chemotactic Mycotoxins parameters
        public const float ChemotacticMycotoxinsMycotoxinTracerMultiplier = 3.0f; // Y value: 3.0% per Mycotoxin Tracer level

        // Corner Conduit balance constants (per growth phase replacements)
        public const int CornerConduitIReplacementsPerPhase = 2;
        public const int CornerConduitIIReplacementsPerPhase = 3; // reserved
        public const int CornerConduitIIIReplacementsPerPhase = 4; // reserved

        // Aggressotropic Conduit balance constants (enemy-tracking conduit) replacements per phase
        public const int AggressotropicConduitIReplacementsPerPhase = 1;
        public const int AggressotropicConduitIIReplacementsPerPhase = 2;
        public const int AggressotropicConduitIIIReplacementsPerPhase = 3;

        // Hyphal Draw AI scoring constants
        public const float HyphalDrawBaseAIScore = 4f;
        public const float HyphalDrawForwardAdvanceAIScorePerTile = 0.45f;
        public const float HyphalDrawEnemyCaptureAIScore = 1.25f;

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
        public const float AscusWagerAIScore = 8f;
        public const float AIDraftAlwaysPickScoreThreshold = 99f;

        public const float CytolyticBurstBaseAIScore = 5f;

        public const float AIDraftModeratePriority = 6f;
        public const float HyphalResistanceTransferBaseAIScoreEarly = 5f;
        public const float HyphalResistanceTransferBaseAIScoreLate = 3f;
        public const float SeptalAlarmBaseAIScore = 5f;
        public const float MycovariantSynergyBonus = 3f;
    }
}
