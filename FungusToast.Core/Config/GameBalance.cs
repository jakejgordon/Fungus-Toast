using FungusToast.Core.Mutations;

namespace FungusToast.Core.Config
{
    public static class GameBalance
    {
        // ==================== GLOBAL GAME MECHANICS ====================
        public const int BoardWidth = 160;
        public const int BoardHeight = 160;
        public const int MaxNumberOfRoundsBeforeGameEndTrigger = 75;
        public const float BaseGrowthChance = 0.015f;
        public const float BaseDeathChance = .032f; 
        public const float AgeDeathFactorPerGrowthCycle = 0.008f;
        public const int StartingMutationPoints = 5;
        public const float GameEndTileOccupancyThreshold = 0.90f;
        public const int TurnsAfterEndGameTileOccupancyThresholdMet = 3;
        public const int BaseAgeResetThreshold = 50;
        public const float MaxEnemyDecayPressurePerCell = 0.25f;
        public const int DefaultSurgeAIAttemptTurnFrequency = 5;
        public const int TotalGrowthCycles = 5;
        public const int DefaultToxinDuration = 6;

        // ==================== MUTATION-SPECIFIC CONSTANTS ====================
        
        // Mycelial Bloom (Tier 1 Growth)
        public const float MycelialBloomEffectPerLevel = 0.0025f;
        public const int MycelialBloomMaxLevel = 150;

        // Homeostatic Harmony (Tier 1 CellularResilience)
        public const float HomeostaticHarmonyEffectPerLevel = 0.003f;
        public const int HomeostaticHarmonyMaxLevel = 100;

        // Mycotoxin Tracer (Tier 1 Fungicide)
        public const float MycotoxinTracerFailedGrowthWeightPerLevel = 0.013f;
        public const float MycotoxinTracerFailureRateWeightPerLevel = 0.8f; // Percentage-based bonus for early game
        public const int MycotoxinTracerTileDuration = 21;
        public const int MycotoxinTracerMaxToxinsDivisor = 60;
        public const int MycotoxinTracerMaxLevel = 50;

        // Mutator Phenotype (Tier 1 GeneticDrift)
        public const float MutatorPhenotypeEffectPerLevel = 0.1f;
        public const int MutatorPhenotypeMaxLevel = 10;

        // Tendrils (Tier 2 Growth)
        public const float TendrilDiagonalGrowthEffectPerLevel = 0.01f;
        public const int TendrilDiagonalGrowthMaxLevel = 10;

        // Chronoresilient Cytoplasm (Tier 2 CellularResilience)
        public const float ChronoresilientCytoplasmEffectPerLevel = 4f;
        public const int ChronoresilientCytoplasmMaxLevel = 15;

        // Mycotoxin Potentiation (Tier 2 Fungicide)
        public const int MycotoxinPotentiationGrowthCycleExtensionPerLevel = 1;
        public const float MycotoxinPotentiationKillChancePerLevel = 0.016f;
        public const int MycotoxinPotentiationMaxLevel = 10;

        // Adaptive Expression (Tier 2 GeneticDrift)
        public const float AdaptiveExpressionEffectPerLevel = 0.19f;
        public const float AdaptiveExpressionSecondPointChancePerLevel = 0.14f; // Chance per level for second point if first is awarded
        public const int AdaptiveExpressionMaxLevel = 5;

        // Mycotoxin Catabolism (Tier 2 GeneticDrift)
        public const float MycotoxinCatabolismCleanupChancePerLevel = 0.025f;
        public const float MycotoxinCatabolismMutationPointChancePerLevel = 0.08f;
        public const int MycotoxinCatabolismMaxMutationPointsPerRound = 3;
        public const int MycotoxinCatabolismMaxLevel = 10;

        // Hyphal Surge (Tier 2 MycelialSurges)
        public const float HyphalSurgeEffectPerLevel = .009f;
        public const int HyphalSurgeMaxLevel = 10;
        public const int HyphalSurgePointsPerActivation = 7;
        public const int HyphalSurgeDurationRounds = 2;
        public const int HyphalSurgePointIncreasePerLevel = 1;

        // Hyphal Vectoring (Tier 2 MycelialSurges)
        public const int HyphalVectoringBaseTiles = 3;
        public const int HyphalVectoringTilesPerLevel = 1;
        public const int HyphalVectoringCandidateCellsToCheck = 50; // Number of candidate cells to check for origin selection
        public const int HyphalVectoringMaxLevel = 5;
        public const int HyphalVectoringPointsPerActivation = 9;
        public const int HyphalVectoringSurgeDuration = 4;
        public const int HyphalVectoringSurgePointIncreasePerLevel = 1;

        // Chitin Fortification (Tier 2 MycelialSurges)
        public const int ChitinFortificationCellsPerLevel = 1; // X: cells fortified per level
        public const int ChitinFortificationDurationRounds = 3; // Y: rounds of protection
        public const int ChitinFortificationMaxLevel = 10;
        public const int ChitinFortificationPointsPerActivation = 2;
        public const int ChitinFortificationSurgeDuration = 3;
        public const int ChitinFortificationPointIncreasePerLevel = 1;

        // Necrosporulation (Tier 3 CellularResilience)
        public const float NecrosporulationEffectPerLevel = 0.04f;
        public const int NecrosporulationMaxLevel = 5;

        // Putrefactive Mycotoxin (Tier 3 Fungicide)
        public const float PutrefactiveMycotoxinEffectPerLevel = 0.015f;
        public const int PutrefactiveMycotoxinMaxLevel = 5;

        // Anabolic Inversion (Tier 3 GeneticDrift)
        public const int AnabolicInversionPointsPerUpgrade = 1; // Reduced from 2 → 1
        public const float AnabolicInversionGapBonusPerLevel = 0.30f;
        public const int AnabolicInversionMaxMutationPointsPerRound = 4; // Cap on Anabolic Inversion points per round
        public const int AnabolicInversionMaxLevel = 3;

        // Mycotropic Induction (Tier 3 Growth)
        public const float MycotropicInductionEffectPerLevel = 0.25f;
        public const int MycotropicInductionMaxLevel = 5;

        // Mimetic Resilience (Tier 3 MycelialSurges)
        public const float MimeticResilienceMinimumBoardControlThreshold = 0.01f; // 1%
        public const float MimeticResilienceMinimumCellAdvantageThreshold = 0.20f; // 20%
        public const int MimeticResilienceMaxLevel = 3;
        public const int MimeticResiliencePointsPerActivation = 8;
        public const int MimeticResilienceSurgeDuration = 4;
        public const int MimeticResiliencePointIncreasePerLevel = 2;

        // Competitive Antagonism (Tier 3 MycelialSurge) 
        public const int CompetitiveAntagonismPointsPerActivation = 7;
        public const int CompetitiveAntagonismSurgeDuration = 4;
        public const int CompetitiveAntagonismPointIncreasePerLevel = 1;
        public const int CompetitiveAntagonismMaxLevel = 5;
        public const float CompetitiveAntagonismSporicidalBloomEmptyTileReduction = 0.25f; // Additional 25% empty tile reduction
        public const float CompetitiveAntagonismSporicidalBloomSmallerColonyReduction = 0.75f; // 75% smaller colony tile reduction
        public const float CompetitiveAntagonismNecrophyticBloomSmallerColonyReduction = 0.75f; // 75% smaller colony dead cell reduction

        // Regenerative Hyphae (Tier 4 Growth)
        public const float RegenerativeHyphaeReclaimChance = 0.021f;
        public const int RegenerativeHyphaeMaxLevel = 5;

        // Creeping Mold (Tier 4 Growth)
        public const float CreepingMoldMoveChancePerLevel = .035f;
        public const int CreepingMoldMaxLevel = 4;

        // Sporocidal Bloom (Tier 4 Fungicide)
        public const float SporicialBloomEffectPerLevel = .08f;
        public const int SporocidalToxinTileDuration = 12;
        public const int SporicidalBloomMaxLevel = 5;

        // Necrophytic Bloom (Tier 4 GeneticDrift)
        public const float NecrophyticBloomActivationThreshold = .20f;
        public const int NecrophyticBloomBaseSpores = 2;
        public const float NecrophyticBloomSporesPerDeathPerLevel = 40;
        public const int NecrophyticBloomMaxLevel = 5;

        // Necrohyphal Infiltration (Tier 5 CellularResilience)
        public const float NecrohyphalInfiltrationChancePerLevel = 0.004f;
        public const float NecrohyphalInfiltrationCascadeChancePerLevel = 0.019f;
        public const int NecrohyphalInfiltrationMaxLevel = 5;

        // Necrotoxic Conversion (Tier 5 Fungicide)
        public const float NecrotoxicConversionReclaimChancePerLevel = .04f;
        public const int NecrotoxicConversionMaxLevel = 5;

        // Putrefactive Rejuvenation (Tier 5 Fungicide)
        public const int PutrefactiveRejuvenationAgeReductionPerLevel = 4; // growth cycles
        public const int PutrefactiveRejuvenationEffectRadius = 3;
        public const float PutrefactiveRejuvenationMycotoxinBonusPerLevel = 0.003f;
        public const int PutrefactiveRejuvenationMaxLevel = 4;
        public const int PutrefactiveRejuvenationMaxLevelRangeRadiusMultiplier = 3;

        // Hyperadaptive Drift (Tier 5 GeneticDrift)
        public const float HyperadaptiveDriftHigherTierChancePerLevel = .28f;
        public const float HyperadaptiveDriftBonusTierOneMutationChancePerLevel = .3f;
        public const int HyperadaptiveDriftMaxLevel = 4;

        // Catabolic Rebirth (Tier 6 CellularResilience)
        public const float CatabolicRebirthResurrectionChancePerLevel = 0.12f;
        public const int CatabolicRebirthMaxLevel = 3;

        // Putrefactive Cascade (Tier 6 Fungicide)
        public const float PutrefactiveCascadeEffectivenessBonus = 0.004f; // X% per level boost to Putrefactive Mycotoxin
        public const float PutrefactiveCascadeCascadeChance = 0.22f; // Y% per level chance for cascade
        public const int PutrefactiveCascadeMaxCascadeDepth = 10; // Maximum cascade depth to prevent infinite recursion
        public const int PutrefactiveCascadeMaxLevel = 3;

        // Ontogenic Regression (Tier 6 GeneticDrift)
        public const float OntogenicRegressionChancePerLevel = 0.30f;
        public const int OntogenicRegressionTier1LevelsToConsume = 3;
        public const int OntogenicRegressionMaxLevel = 3;
        public const int OntogenicRegressionFailureConsolationPoints = 2; // Points awarded when regression fails

        // Hypersystemic Regeneration (Tier 7 CellularResilience)
        public const float HypersystemicRegenerationEffectivenessBonus = 0.01f; // X% per level boost to Regenerative Hyphae
        public const float HypersystemicRegenerationResistanceChance = 0.15f; // Y% per level chance for resistant cells
        public const int HypersystemicRegenerationMaxLevel = 3;

        // Legacy constants (keeping for backward compatibility, can be removed later if not needed)
        public const int AgeResetReductionPerLevel = 5;

        public static class MutationCosts
        {
            public const int Tier1UpgradeCost = 1;
            public const int Tier2UpgradeCost = 2;
            public const int Tier3UpgradeCost = 4;
            public const int Tier4UpgradeCost = 5;
            public const int Tier5UpgradeCost = 6;
            public const int Tier6UpgradeCost = 7;
            public const int Tier7UpgradeCost = 8;

            public static int GetUpgradeCostByTier(MutationTier tier)
            {
                return tier switch
                {
                    MutationTier.Tier1 => Tier1UpgradeCost,
                    MutationTier.Tier2 => Tier2UpgradeCost,
                    MutationTier.Tier3 => Tier3UpgradeCost,
                    MutationTier.Tier4 => Tier4UpgradeCost,
                    MutationTier.Tier5 => Tier5UpgradeCost,
                    MutationTier.Tier6 => Tier6UpgradeCost,
                    MutationTier.Tier7 => Tier7UpgradeCost,
                    _ => Tier6UpgradeCost,
                };
            }
        }


    }
}
