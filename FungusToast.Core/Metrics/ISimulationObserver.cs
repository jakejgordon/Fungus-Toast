using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Metrics
{
    public interface ISimulationObserver
    {
        void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordAdaptiveExpressionBonus(int playerId, int bonus);
        void RecordAnabolicInversionBonus(int playerId, int bonus);
        void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints);

        void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1);
        void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1);
        void RecordCreepingMoldMove(int playerId);
        void RecordCreepingMoldToxinJump(int playerId);
        void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount);
        void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount);
        void RecordTendrilGrowth(int playerId, DiagonalDirection value);
        void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints);
        void RecordNutrientPatchesPlaced(int count);
        void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount);
        void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions);
        void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells);
        void RecordRegenerativeHyphaeReclaim(int playerId);

        void ReportSporicidalSporeDrop(int playerId, int count);
        void ReportNecrosporeDrop(int playerId, int count);
        void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount);
        void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped);
        void RecordMutationPointIncome(int playerId, int newMutationPoints);
        void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade);
        void RecordBankedPoints(int playerId, int pointsBanked);
        void RecordHyphalSurgeGrowth(int playerId);
        void RecordDirectedVectorGrowth(int playerId, int cellsPlaced);
        void ReportJettingMyceliumInfested(int playerId, int infested);
        void ReportJettingMyceliumReclaimed(int playerId, int reclaimed);
        void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth);
        void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned);
        void ReportJettingMyceliumInvalid(int playerId, int invalid);
        void ReportJettingMyceliumColonized(int playerId, int colonized);
        void ReportJettingMyceliumToxified(int playerId, int toxified);
        void ReportJettingMyceliumPoisoned(int playerId, int poisoned);
        void ReportDirectedVectorInfested(int playerId, int infested);
        void ReportDirectedVectorReclaimed(int playerId, int reclaimed);
        void ReportDirectedVectorCatabolicGrowth(int playerId, int catabolicGrowth);
        void ReportDirectedVectorAlreadyOwned(int playerId, int alreadyOwned);
        void ReportDirectedVectorColonized(int playerId, int colonized);
        void ReportDirectedVectorInvalid(int playerId, int invalid);
        void RecordStandardGrowth(int playerId);
        void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized);
        void RecordBastionedCells(int playerId, int count);
        void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged);
        void RecordSurgicalInoculationDrop(int playerId, int count);
        void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced);
        void RecordPerimeterProliferatorGrowth(int playerId);
        void RecordHyphalResistanceTransfer(int playerId, int count);
        void RecordSeptalAlarmResistance(int playerId, int count);
        void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles);
        void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles);
        void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count);
        void RecordNecrophoricAdaptationReclamation(int playerId, int count);
        void RecordBallistosporeDischarge(int playerId, int count);
        void RecordChitinFortificationCellsFortified(int playerId, int count);
        void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills);
        void RecordPutrefactiveCascadeToxified(int playerId, int toxified);
        void RecordMimeticResilienceInfestations(int playerId, int infestations);
        void RecordMimeticResilienceDrops(int playerId, int drops);
        void RecordCytolyticBurstToxins(int playerId, int toxinsCreated);
        void RecordCytolyticBurstKills(int playerId, int cellsKilled);
        void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations);
        void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles);
        void RecordHypersystemicRegenerationResistance(int playerId);
        void RecordHypersystemicDiagonalReclaim(int playerId);
        void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName);
        void RecordSpecificMutationUpgrade(int playerId, string mutationName);
        void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints);
        void RecordMutationUpgradeEvent(
            int playerId,
            int mutationId,
            string mutationName,
            MutationTier mutationTier,
            int oldLevel,
            int newLevel,
            int round,
            int mutationPointsBefore,
            int mutationPointsAfter,
            int pointsSpent,
            string upgradeSource);
        void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained);
        void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints);
        void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected);
        void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset);
    }
}