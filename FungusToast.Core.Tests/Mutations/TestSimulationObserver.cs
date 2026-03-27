using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Tests.Mutations;

internal sealed class TestSimulationObserver : ISimulationObserver
{
    public int? LastMutationPointsSpent { get; private set; }
    public int? LastMutationPointIncome { get; private set; }
    public int? LastApicalYieldBonus { get; private set; }
    public string? LastUpgradeSource { get; private set; }
    public int UpgradeEventCount { get; private set; }
    public int NecrophyticBloomReportCount { get; private set; }
    public int LastNecrophyticBloomPatchCount { get; private set; }
    public Dictionary<int, int> NecrophyticBloomPatchesByPlayer { get; } = new();

    public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) => LastMutationPointsSpent = pointsPerUpgrade;
    public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints) => LastApicalYieldBonus = bonusPoints;
    public void RecordMutationUpgradeEvent(int playerId, int mutationId, string mutationName, MutationTier mutationTier, int oldLevel, int newLevel, int round, int mutationPointsBefore, int mutationPointsAfter, int pointsSpent, string upgradeSource)
    {
        UpgradeEventCount++;
        LastUpgradeSource = upgradeSource;
    }

    public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { }
    public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) { }
    public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { }
    public void RecordAnabolicInversionBonus(int playerId, int bonus) { }
    public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) { }
    public void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1) { }
    public void RecordCreepingMoldMove(int playerId) { }
    public void RecordCreepingMoldToxinJump(int playerId) { }
    public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
    public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
    public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
    public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
    public void RecordNutrientPatchesPlaced(int count) { }
    public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount) { }
    public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
    public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
    public void RecordRegenerativeHyphaeReclaim(int playerId) { }
    public void ReportSporicidalSporeDrop(int playerId, int count) { }
    public void ReportNecrosporeDrop(int playerId, int count) { }
    public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount)
    {
        NecrophyticBloomReportCount++;
        LastNecrophyticBloomPatchCount = createdPatchCount;

        if (!NecrophyticBloomPatchesByPlayer.ContainsKey(playerId))
            NecrophyticBloomPatchesByPlayer[playerId] = 0;
        NecrophyticBloomPatchesByPlayer[playerId] += createdPatchCount;
    }
    public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
    public void RecordMutationPointIncome(int playerId, int newMutationPoints) => LastMutationPointIncome = newMutationPoints;
    public void RecordBankedPoints(int playerId, int pointsBanked) { }
    public void RecordHyphalSurgeGrowth(int playerId) { }
    public void RecordDirectedVectorGrowth(int playerId, int cellsPlaced) { }
    public void ReportJettingMyceliumInfested(int playerId, int infested) { }
    public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { }
    public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
    public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
    public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
    public void ReportJettingMyceliumColonized(int playerId, int colonized) { }
    public void ReportJettingMyceliumToxified(int playerId, int toxified) { }
    public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { }
    public void ReportDirectedVectorInfested(int playerId, int infested) { }
    public void ReportDirectedVectorReclaimed(int playerId, int reclaimed) { }
    public void ReportDirectedVectorCatabolicGrowth(int playerId, int catabolicGrowth) { }
    public void ReportDirectedVectorAlreadyOwned(int playerId, int alreadyOwned) { }
    public void ReportDirectedVectorColonized(int playerId, int colonized) { }
    public void ReportDirectedVectorInvalid(int playerId, int invalid) { }
    public void RecordStandardGrowth(int playerId) { }
    public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized) { }
    public void RecordBastionedCells(int playerId, int count) { }
    public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged) { }
    public void RecordSurgicalInoculationDrop(int playerId, int count) { }
    public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) { }
    public void RecordPerimeterProliferatorGrowth(int playerId) { }
    public void RecordHyphalResistanceTransfer(int playerId, int count) { }
    public void RecordSeptalAlarmResistance(int playerId, int count) { }
    public void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles) { }
    public void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles) { }
    public void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count) { }
    public void RecordNecrophoricAdaptationReclamation(int playerId, int count) { }
    public void RecordBallistosporeDischarge(int playerId, int count) { }
    public void RecordChitinFortificationCellsFortified(int playerId, int count) { }
    public void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills) { }
    public void RecordPutrefactiveCascadeToxified(int playerId, int toxified) { }
    public void RecordMimeticResilienceInfestations(int playerId, int infestations) { }
    public void RecordMimeticResilienceDrops(int playerId, int drops) { }
    public void RecordCytolyticBurstToxins(int playerId, int toxinsCreated) { }
    public void RecordCytolyticBurstKills(int playerId, int cellsKilled) { }
    public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations) { }
    public void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles) { }
    public void RecordHypersystemicRegenerationResistance(int playerId) { }
    public void RecordHypersystemicDiagonalReclaim(int playerId) { }
    public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName) { }
    public void RecordSpecificMutationUpgrade(int playerId, string mutationName) { }
    public void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints) { }
    public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained) { }
    public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) { }
    public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
    public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset) { }
}
