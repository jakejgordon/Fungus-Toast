using FungusToast.Core.Mutations;

namespace FungusToast.Core.Config
{
    public static class GameBalance
    {
        // Global Mechanics
        public const int MaxNumberOfRoundsBeforeGameEndTrigger = 75;
        public const float BaseGrowthChance = 0.015f;
        public const float BaseDeathChance = .032f; 
        public const float AgeDeathFactorPerGrowthCycle = 0.008f;
        public const int StartingMutationPoints = 5;
        public const float GameEndTileOccupancyThreshold = 0.90f;
        public const int TurnsAfterEndGameTileOccupancyThresholdMet = 3;
        public const int BaseAgeResetThreshold = 50;
        public const float MaxEnemyDecayPressurePerCell = 0.25f;
        public const float TimeBeforeDecayRender = 0.5f;
        public const float TimeAfterDecayRender = 0.5f;
        public const float NecrophyticBloomActivationThreshold = .20f;

        // Mutation Effects
        public const float MycelialBloomEffectPerLevel = 0.0025f;
        public const float HomeostaticHarmonyEffectPerLevel = 0.003f;
        public const float MycotoxinTracerFailedGrowthWeightPerLevel = 0.013f;
        public const float MycotoxinTracerFailureRateWeightPerLevel = 0.8f; // Percentage-based bonus for early game
        public const int MycotoxinTracerTileDuration = 21;
        public const int MycotoxinTracerMaxToxinsDivisor = 60;

        public const float TendrilDiagonalGrowthEffectPerLevel = 0.01f;
        public const float MutatorPhenotypeEffectPerLevel = 0.1f;
        public const float ChronoresilientCytoplasmEffectPerLevel = 4f;
        public const float HyphalSurgeEffectPerLevel = .009f;

        public const float NecrosporulationEffectPerLevel = 0.04f;
        public const int MycotoxinPotentiationGrowthCycleExtensionPerLevel = 1;
        public const float MycotoxinPotentiationKillChancePerLevel = 0.016f;
        public const float AdaptiveExpressionEffectPerLevel = 0.19f;
        public const float AdaptiveExpressionSecondPointChancePerLevel = 0.14f; // Chance per level for second point if first is awarded
        public const float MycotoxinCatabolismCleanupChancePerLevel = 0.025f;
        public const float MycotoxinCatabolismMutationPointChancePerLevel = 0.08f;
        public const int MycotoxinCatabolismMaxMutationPointsPerRound = 3;
        public const int MaxEconomyMutationPointsPerRound = 3; // Cap on total economy mutation points per round
        public const int HyphalVectoringBaseTiles = 3;
        public const int HyphalVectoringTilesPerLevel = 1;

        // Number of candidate cells to check for Hyphal Vectoring origin selection
        public const int HyphalVectoringCandidateCellsToCheck = 50;

        public const float MycotropicInductionEffectPerLevel = 0.25f;
        public const float PutrefactiveMycotoxinEffectPerLevel = 0.015f;
        public const int AgeResetReductionPerLevel = 5;
        public const int AnabolicInversionPointsPerUpgrade = 1; // Reduced from 2 → 1
        public const float AnabolicInversionGapBonusPerLevel = 0.30f;
        public const int AnabolicInversionMaxMutationPointsPerRound = 4; // Cap on Anabolic Inversion points per round
        public const float RegenerativeHyphaeReclaimChance = 0.021f;
        public const float CreepingMoldMoveChancePerLevel = .035f;
        public const float SporicialBloomEffectPerLevel = .08f;
        public const int SporocidalToxinTileDuration = 12;
        public const int NecrophyticBloomBaseSpores = 2;
        public const float NecrophyticBloomSporesPerDeathPerLevel = 40;
        public const float HyperadaptiveDriftHigherTierChancePerLevel = .28f;
        public const float HyperadaptiveDriftBonusTierOneMutationChancePerLevel = .3f;
        public const float NecrohyphalInfiltrationChancePerLevel = 0.004f;
        public const float NecrohyphalInfiltrationCascadeChancePerLevel = 0.019f;
        public const float NecrotoxicConversionReclaimChancePerLevel = .04f;
        public const float CatabolicRebirthResurrectionChancePerLevel = 0.12f;

        // Putrefactive Cascade (Tier 6 Fungicide)
        public const float PutrefactiveCascadeEffectivenessBonus = 0.004f; // X% per level boost to Putrefactive Mycotoxin
        public const float PutrefactiveCascadeCascadeChance = 0.22f; // Y% per level chance for cascade
        public const int PutrefactiveCascadeMaxCascadeDepth = 10; // Maximum cascade depth to prevent infinite recursion

        // Chitin Fortification (Tier 2 MycelialSurges)
        public const int ChitinFortificationCellsPerLevel = 1; // X: cells fortified per level
        public const int ChitinFortificationDurationRounds = 3; // Y: rounds of protection

        // Max Levels
        public const int MycelialBloomMaxLevel = 150;
        public const int HomeostaticHarmonyMaxLevel = 100;
        public const int MycotoxinTracerMaxLevel = 50;
        public const int MutatorPhenotypeMaxLevel = 10;
        public const int DiagonalGrowthMaxLevel = 10;
        public const int HyphalVectoringMaxLevel = 5;
        public const int ChronoresilientCytoplasmMaxLevel = 15;
        public const int HyphalSurgeMaxLevel = 10;
        public const int NecrosporulationMaxLevel = 5;
        public const int MycotoxinPotentiationMaxLevel = 10;
        public const int AdaptiveExpressionMaxLevel = 5;
        public const int MycotoxinCatabolismMaxLevel = 10;
        public const int MycotropicInductionMaxLevel = 5;
        public const int PutrefactiveMycotoxinMaxLevel = 5;
        public const int AnabolicInversionMaxLevel = 3;
        public const int RegenerativeHyphaeMaxLevel = 5;
        public const int CreepingMoldMaxLevel = 4;
        public const int SporicidalBloomMaxLevel = 5;
        public const int NecrophyticBloomMaxLevel = 5;
        public const int HyperadaptiveDriftMaxLevel = 4;
        public const int NecrohyphalInfiltrationMaxLevel = 5;
        public const int NecrotoxicConversionMaxLevel = 5;
        public const int CatabolicRebirthMaxLevel = 3;
        public const int ChitinFortificationMaxLevel = 10;
        public const int PutrefactiveCascadeMaxLevel = 3;
        public const int MimeticResilienceMaxLevel = 3;

        // surge mutation points per activiation
        public const int HyphalSurgePointsPerActivation = 7;
        public const int HyphalVectoringPointsPerActivation = 9;
        public const int ChitinFortificationPointsPerActivation = 2;
        public const int MimeticResiliencePointsPerActivation = 8;

        // surge mutation durations
        public const int HyphalSurgeDurationRounds = 2;
        public const int HyphalVectoringSurgeDuration = 5;
        public const int ChitinFortificationSurgeDuration = 3;
        public const int MimeticResilienceSurgeDuration = 4;

        // surge mutation increase per level costs
        public const int HyphalSurgePointIncreasePerLevel = 1;
        public const int HyphalVectoringSurgePointIncreasePerLevel = 1;
        public const int ChitinFortificationPointIncreasePerLevel = 1;
        public const int MimeticResiliencePointIncreasePerLevel = 2;

        // AI mechanics
        public const int DefaultSurgeAIAttemptTurnFrequency = 5;

        // Phase Timing
        public const int TotalGrowthCycles = 5;
        public const float TimeBetweenGrowthCycles = 1f;

        // Board dimensions
        public const int BoardWidth = 160;
        public const int BoardHeight = 160;


        // time-based effects on Fungal Cells
        public const int DefaultToxinDuration = 6;

        // Putrefactive Rejuvenation (Tier 5 Fungicide)
        public const int PutrefactiveRejuvenationAgeReductionPerLevel = 4; // growth cycles
        public const int PutrefactiveRejuvenationEffectRadius = 3;
        public const float PutrefactiveRejuvenationMycotoxinBonusPerLevel = 0.003f;
        public const int PutrefactiveRejuvenationMaxLevel = 4;
        public const int PutrefactiveRejuvenationMaxLevelRangeRadiusMultiplier = 3;

        // Mimetic Resilience (Tier 3 MycelialSurges)
        public const float MimeticResilienceMinimumBoardControlThreshold = 0.01f; // 1%
        public const float MimeticResilienceMinimumCellAdvantageThreshold = 0.20f; // 20%

        public static class MutationCosts
        {
            public const int Tier1UpgradeCost = 1;
            public const int Tier2UpgradeCost = 2;
            public const int Tier3UpgradeCost = 4;
            public const int Tier4UpgradeCost = 5;
            public const int Tier5UpgradeCost = 6;
            public const int Tier6UpgradeCost = 7;

            public static int GetUpgradeCostByTier(MutationTier tier)
            {
                return tier switch
                {
                    MutationTier.Tier1 => Tier1UpgradeCost,
                    MutationTier.Tier2 => Tier2UpgradeCost,
                    MutationTier.Tier3 => Tier3UpgradeCost,
                    MutationTier.Tier4 => Tier4UpgradeCost,
                    MutationTier.Tier5 => Tier5UpgradeCost,
                    _ => Tier6UpgradeCost,
                };
            }
        }


    }
}
