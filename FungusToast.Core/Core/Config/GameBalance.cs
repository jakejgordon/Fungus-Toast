using FungusToast.Core.Mutations;

namespace FungusToast.Core.Config
{
    public static class GameBalance
    {
        // Global Mechanics
        public const int MaxNumberOfRoundsBeforeGameEndTrigger = 100;
        public const float BaseGrowthChance = 0.015f;
        public const float BaseDeathChance = .032f; //0.015f;
        public const float AgeDeathFactorPerGrowthCycle = 0.01f;
        public const int StartingMutationPoints = 5;
        public const float GameEndTileOccupancyThreshold = 0.95f;
        public const int TurnsAfterEndGameTileOccupancyThresholdMet = 3;
        public const int BaseAgeResetThreshold = 50;
        public const float MaxEnemyDecayPressurePerCell = 0.25f;
        public const float TimeBeforeDecayRender = 0.5f;
        public const float TimeAfterDecayRender = 0.5f;
        public const float NecrophyticBloomActivationThreshold = .20f;

        // Mutation Effects
        public const float MycelialBloomEffectPerLevel = 0.0025f;
        public const float HomeostaticHarmonyEffectPerLevel = 0.003f;
        public const float MycotoxinTracerFailedGrowthWeightPerLevel = 0.0024f;
        public const int MycotoxinTracerTileDuration = 20;
        public const int MycotoxinTracerMaxToxinsDivisor = 50;

        public const float TendrilDiagonalGrowthEffectPerLevel = 0.01f;
        public const float MutatorPhenotypeEffectPerLevel = 0.065f;
        public const float ChronoresilientCytoplasmEffectPerLevel = 4f;
        public const float HyphalSurgeEffectPerLevel = .1f;

        public const float NecrosporulationEffectPerLevel = 0.04f;
        public const int MycotoxinPotentiationGrowthCycleExtensionPerLevel = 1;
        public const float MycotoxinPotentiationKillChancePerLevel = 0.012f;
        public const float AdaptiveExpressionEffectPerLevel = 0.15f;
        public const float MycotoxinCatabolismCleanupChancePerLevel = 0.025f;
        public const float MycotoxinCatabolismMutationPointChancePerCatabolism = 0.075f;
        public const int MycotoxinCatabolismMaxMutationPointsPerRound = 3;
        public const int HyphalVectoringBaseTiles = 2;
        public const int HyphalVectoringTilesPerLevel = 1;

        public const float MycotropicInductionEffectPerLevel = 0.25f;
        public const float PutrefactiveMycotoxinEffectPerLevel = 0.013f;
        public const int AgeResetReductionPerLevel = 5;
        public const int AnabolicInversionPointsPerUpgrade = 1; // Reduced from 2 → 1
        public const float AnabolicInversionGapBonusPerLevel = 0.10f;
        public const float RegenerativeHyphaeReclaimChance = 0.01f;
        public const float CreepingMoldMoveChancePerLevel = .035f;
        public const float SporicialBloomEffectPerLevel = .065f;
        public const int SporocidalToxinTileDuration = 11;
        public const int NecrophyticBloomBaseSpores = 2;
        public const float NecrophyticBloomSporesPerDeathPerLevel = 40;
        public const float HyperadaptiveDriftHigherTierChancePerLevel = .25f;
        public const float HyperadaptiveDriftBonusTierOneMutationChancePerLevel = .25f;
        public const float NecrohyphalInfiltrationChancePerLevel = 0.004f;
        public const float NecrohyphalInfiltrationCascadeChancePerLevel = 0.019f;
        public const float NecrotoxicConversionReclaimChancePerLevel = .035f;

        // Max Levels
        public const int MycelialBloomMaxLevel = 150;
        public const int HomeostaticHarmonyMaxLevel = 100;
        public const int MycotoxinTracerMaxLevel = 50;
        public const int MutatorPhenotypeMaxLevel = 15;
        public const int DiagonalGrowthMaxLevel = 10;
        public const int HyphalVectoringMaxLevel = 5;
        public const int ChronoresilientCytoplasmMaxLevel = 15;
        public const int HyphalSurgeMaxLevel = 10;
        public const int NecrosporulationMaxLevel = 5;
        public const int MycotoxinPotentiationMaxLevel = 10;
        public const int AdaptiveExpressionMaxLevel = 6;
        public const int MycotoxinCatabolismMaxLevel = 10;
        public const int MycotropicInductionMaxLevel = 5;
        public const int PutrefactiveMycotoxinMaxLevel = 5;
        public const int AnabolicInversionMaxLevel = 3;
        public const int RegenerativeHyphaeMaxLevel = 5;
        public const int CreepingMoldMaxLevel = 4;
        public const int SporocidalBloomMaxLevel = 5;
        public const int NecrophyticBloomMaxLevel = 5;
        public const int HyperadaptiveDriftMaxLevel = 4;
        public const int NecrohyphalInfiltrationMaxLevel = 5;
        public const int NecrotoxicConversionMaxLevel = 5;

        // surge mutation points per activiation
        public const int HyphalSurgePointsPerActivation = 7;
        public const int HyphalVectoringPointsPerActivation = 9;

        // surge mutation durations
        public const int HyphalSurgeDurationRounds = 2;
        public const int HyphalVectoringSurgeDuration = 5;

        // surge mutation increase per level costs
        public const int HyphalSurgePointIncreasePerLevel = 1;
        public const int HyphalVectoringSurgePointIncreasePerLevel = 1;

        // AI mechanics
        public const int DefaultSurgeAIAttemptTurnFrequency = 5;

        // Phase Timing
        public const int TotalGrowthCycles = 5;
        public const float TimeBetweenGrowthCycles = 1f;

        // Board dimensions
        public const int BoardWidth = 100;
        public const int BoardHeight = 100;


        // time-based effects on Fungal Cells
        public const int DefaultToxinDuration = 5;
        

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
