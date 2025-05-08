namespace FungusToast.Core.Config
{
    public static class GameBalance
    {
        // Global Mechanics
        public const float BaseGrowthChance = 0.05f; 
        public const float BaseDeathChance = 0.02f;
        public const float AgeDeathFactorPerGrowthCycle = 0.005f;
        public const int StartingMutationPoints = 5;
        public const float GameEndTileOccupancyThreshold = 0.001f; //TESTING!!!
        public const int TurnsAfterEndGameTileOccupancyThresholdMet = 1; //TESTING!!!

        // Mutation Effects
        public const float MycelialBloomEffectPerLevel = 0.005f;
        public const float HomeostaticHarmonyEffectPerLevel = 0.0025f;
        public const float SilentBlightEffectPerLevel = 0.0025f;
        public const float AdaptiveExpressionEffectPerLevel = 0.10f;
        public const float ChronoresilientCytoplasmEffectPerLevel = 5f;
        public const float NecrosporulationEffectPerLevel = 0.15f;
        public const float EncystedSporesEffectPerLevel = 0.05f;
        public const float MutatorPhenotypeEffectPerLevel = 0.075f;
        public const float DiagonalGrowthEffectPerLevel = 0.01f;

        // Max Levels
        public const int MycelialBloomMaxLevel = 100;
        public const int HomeostaticHarmonyMaxLevel = 100;
        public const int SilentBlightMaxLevel = 100;
        public const int AdaptiveExpressionMaxLevel = 10;
        public const int ChronoresilientCytoplasmMaxLevel = 10;
        public const int NecrosporulationMaxLevel = 5;
        public const int EncystedSporesMaxLevel = 5;
        public const int MutatorPhenotypeMaxLevel = 10;
        public const int DiagonalGrowthMaxLevel = 10;
    }
}
