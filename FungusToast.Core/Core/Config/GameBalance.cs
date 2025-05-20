using FungusToast.Core.Core.Mutations;

namespace FungusToast.Core.Config
{
    public static class GameBalance
    {
        // Global Mechanics
        public const float BaseGrowthChance = 0.015f;
        public const float BaseDeathChance = 0.01f;
        public const float AgeDeathFactorPerGrowthCycle = 0.007f;
        public const int StartingMutationPoints = 5;
        public const float GameEndTileOccupancyThreshold = 0.999f;
        public const int TurnsAfterEndGameTileOccupancyThresholdMet = 3;
        public const int BaseAgeResetThreshold = 50;
        public const float MaxEnemyDecayPressurePerCell = 0.25f;
        public const float TimeBeforeDecayRender = 0.5f;
        public const float TimeAfterDecayRender = 0.5f;

        // Mutation Effects
        public const float MycelialBloomEffectPerLevel = 0.003f;
        public const float HomeostaticHarmonyEffectPerLevel = 0.0025f;
        public const float SilentBlightEffectPerLevel = 0.001f;
        public const float AdaptiveExpressionEffectPerLevel = 0.09f;
        public const float ChronoresilientCytoplasmEffectPerLevel = 5f;
        public const float NecrosporulationEffectPerLevel = 0.075f;
        public const float EncystedSporesEffectPerLevel = 0.01f;
        public const float MutatorPhenotypeEffectPerLevel = 0.075f;
        public const float DiagonalGrowthEffectPerLevel = 0.01f;
        public const float MycotropicInductionEffectPerLevel = 0.45f;
        public const float PutrefactiveMycotoxinEffectPerLevel = 0.09f;
        public const int AgeResetReductionPerLevel = 5;
        public const int AnabolicInversionPointsPerUpgrade = 1; // Reduced from 2 → 1
        public const float AnabolicInversionGapBonusPerLevel = 0.10f;
        public const float RegenerativeHyphaeReclaimChance = 0.02f; 

        // Max Levels
        public const int MycelialBloomMaxLevel = 100;
        public const int HomeostaticHarmonyMaxLevel = 100;
        public const int SilentBlightMaxLevel = 100;
        public const int AdaptiveExpressionMaxLevel = 10;
        public const int ChronoresilientCytoplasmMaxLevel = 10;
        public const int NecrosporulationMaxLevel = 5;
        public const int EncystedSporesMaxLevel = 5;
        public const int MutatorPhenotypeMaxLevel = 10; // Consider reducing to 7 if late-game inflation persists
        public const int DiagonalGrowthMaxLevel = 10;
        public const int MycotropicInductionMaxLevel = 3;
        public const int PutrefactiveMycotoxinMaxLevel = 5;
        public const int AnabolicInversionMaxLevel = 3;
        public const int RegenerativeHyphaeMaxLevel = 5;

        // Phase Timing
        public const int TotalGrowthCycles = 5;
        public const float TimeBetweenGrowthCycles = 1f;

        // Board dimensions
        public const int BoardWidth = 50;
        public const int BoardHeight = 50;

        
        public static class MutationCosts
        {
            public const int Tier1UpgradeCost = 1;
            public const int Tier2UpgradeCost = 2;
            public const int Tier3UpgradeCost = 3;

            public static int GetUpgradeCostByTier(MutationTier tier)
            {
                return tier switch
                {
                    MutationTier.Tier1 => Tier1UpgradeCost,
                    MutationTier.Tier2 => Tier2UpgradeCost,
                    MutationTier.Tier3 => Tier3UpgradeCost,
                    _ => Tier1UpgradeCost,
                };
            }
        }
    }
}
