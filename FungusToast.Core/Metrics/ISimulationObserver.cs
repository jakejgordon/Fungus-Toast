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

        void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1);
        void RecordCreepingMoldMove(int playerId);
        void RecordCreepingMoldToxinJump(int playerId);
        void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount);
        void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount);
        void RecordTendrilGrowth(int playerId, DiagonalDirection value);
        void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints);
        void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions);
        void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells);
        void RecordRegenerativeHyphaeReclaim(int playerId);

        void ReportSporocidalSporeDrop(int playerId, int count);
        void ReportNecrosporeDrop(int playerId, int count);
        void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims);
        void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped);
        void RecordMutationPointIncome(int playerId, int newMutationPoints);
        void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade);
        void RecordBankedPoints(int playerId, int pointsBanked);
        void RecordHyphalSurgeGrowth(int playerId);
        void RecordHyphalVectoringGrowth(int playerId, int cellsPlaced);
        void ReportJettingMyceliumInfested(int playerId, int infested);
        void ReportJettingMyceliumReclaimed(int playerId, int reclaimed);
        void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth);
        void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned);
        void ReportJettingMyceliumInvalid(int playerId, int invalid);
        void ReportJettingMyceliumColonized(int playerId, int colonized);
        void ReportJettingMyceliumToxified(int playerId, int toxified);
        void ReportJettingMyceliumPoisoned(int playerId, int poisoned);
        void ReportHyphalVectoringInfested(int playerId, int infested);
        void ReportHyphalVectoringReclaimed(int playerId, int reclaimed);
        void ReportHyphalVectoringCatabolicGrowth(int playerId, int catabolicGrowth);
        void ReportHyphalVectoringAlreadyOwned(int playerId, int alreadyOwned);
        void ReportHyphalVectoringColonized(int playerId, int colonized);
        void ReportHyphalVectoringInvalid(int playerId, int invalid);
        void RecordStandardGrowth(int playerId);
        void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized);
        void RecordBastionedCells(int playerId, int count);
        void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged);
        void RecordSurgicalInoculationDrop(int playerId, int count);
        void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced);
        void RecordPerimeterProliferatorGrowth(int playerId);
        void RecordHyphalResistanceTransfer(int playerId, int count);
        void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles);
        void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles);
        void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count);
        void RecordNecrophoricAdaptationReclamation(int playerId, int count);
        void RecordBallistosporeDischarge(int playerId, int count);
    }
}