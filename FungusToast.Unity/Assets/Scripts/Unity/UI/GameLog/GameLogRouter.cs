using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Board;
using FungusToast.Unity.UI.MutationTree;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Composite ISimulationObserver that routes events to appropriate log managers.
    /// Implements single-point-of-contact pattern for cleaner GameManager integration.
    /// Supports silent mode for fast-forward operations to skip UI logging.
    /// </summary>
    public class GameLogRouter : ISimulationObserver
    {
        private readonly GameLogManager playerActivityLogManager;
        private readonly GlobalGameLogManager globalEventsLogManager;
        private readonly UI_MutationTreeToastPresenter mutationTreeToastPresenter;

        /// <summary>
        /// When true, UI emission is suppressed. Some aggregation still occurs.
        /// </summary>
        public bool IsSilentMode { get; private set; } = false;

        public GameLogRouter(GameLogManager playerLog, GlobalGameLogManager globalLog, UI_MutationTreeToastPresenter mutationTreeToast)
        {
            playerActivityLogManager = playerLog;
            globalEventsLogManager = globalLog;
            mutationTreeToastPresenter = mutationTreeToast;
        }

        #region Silent Mode
        /// <summary>
        /// Enables silent mode to suppress all logging during fast-forward operations.
        /// </summary>
        public void EnableSilentMode()
        {
            IsSilentMode = true;
        }

        /// <summary>
        /// Disables silent mode to resume normal logging after fast-forward operations.
        /// </summary>
        public void DisableSilentMode()
        {
            IsSilentMode = false;
        }
        #endregion

        #region Free Point / Upgrade Routing (always aggregate)
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned)
            => playerActivityLogManager?.RecordMutatorPhenotypeMutationPointsEarned(playerId, freePointsEarned);

        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned)
            => playerActivityLogManager?.RecordHyperadaptiveDriftMutationPointsEarned(playerId, freePointsEarned);

        public void RecordAdaptiveExpressionBonus(int playerId, int bonus)
            => playerActivityLogManager?.RecordAdaptiveExpressionBonus(playerId, bonus);

        public void RecordAnabolicInversionBonus(int playerId, int bonus)
            => playerActivityLogManager?.RecordAnabolicInversionBonus(playerId, bonus);

        public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints)
        {
            playerActivityLogManager?.RecordApicalYieldBonus(playerId, mutationName, bonusPoints);

            if (IsSilentMode || bonusPoints <= 0)
            {
                return;
            }

            string pointLabel = bonusPoints == 1 ? "point" : "points";
            mutationTreeToastPresenter?.ShowIfTreeOpen($"Apical Yield grants {bonusPoints} mutation {pointLabel}!");
        }

        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints)
            => playerActivityLogManager?.RecordOntogenicRegressionFailureBonus(playerId, bonusPoints);

        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained)
            => playerActivityLogManager?.RecordOntogenicRegressionEffect(playerId, sourceMutationName, sourceLevelsLost, targetMutationName, targetLevelsGained);

        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName)
            => playerActivityLogManager?.RecordMutatorPhenotypeUpgrade(playerId, mutationName);

        public void RecordSpecificMutationUpgrade(int playerId, string mutationName)
            => playerActivityLogManager?.RecordSpecificMutationUpgrade(playerId, mutationName);

        public void RecordMutationUpgradeEvent(
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
            string upgradeSource)
        {
            // Intentionally no-op for now. This hook exists so gameplay telemetry
            // can consume ordered upgrade events in future iterations.
        }

        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset)
            => playerActivityLogManager?.RecordOntogenicRegressionSacrifices(playerId, cellsKilled, levelsOffset);
        #endregion

        #region Routed Only When Not Silent (UI oriented)
        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMutationPointIncome(playerId, newMutationPoints);
        }

        public void RecordNutrientPatchesPlaced(int count)
        {
        }

        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordNutrientPatchConsumed(playerId, nutrientTileId, patchType, rewardType, rewardAmount);
        }

        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordToxinCatabolism(playerId, toxinsCatabolized, catabolizedMutationPoints);
        }

        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordCellDeath(playerId, reason, deathCount);
        }

        public void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1)
        {
            if (IsSilentMode) return;
        }

        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordChemotacticMycotoxinsRelocations(playerId, relocations);
        }

        public void RecordConidialRelayRelocation(int playerId)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordConidialRelayRelocation(playerId);
        }

        public void RecordHyphalBridgeDeployment(int playerId, int cellsPlaced)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordHyphalBridgeDeployment(playerId, cellsPlaced);
        }

        public void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordVesicleBurstEffect(playerId, poisonedCells, toxifiedTiles);
        }

        public void RecordDistalSporeDeployment(int playerId)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordDistalSporeDeployment(playerId);
        }

        public void RecordAscusPrimacyDraftPriority(int playerId)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordAscusPrimacyDraftPriority(playerId);
        }

        public void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordRetrogradeBloomUpgrade(playerId, evolvedMutationName, devolvedMutationSummary, devolvedPoints);
        }

        public void RecordAegisHyphaeResistance(int playerId, int cellsFortified)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordAegisHyphaeResistance(playerId, cellsFortified);
        }

        public void RecordCrustalCallusResistance(int playerId, int cellsFortified)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordCrustalCallusResistance(playerId, cellsFortified);
        }

        public void RecordDirectedVectorSurge(int playerId, int cellsPlaced)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordDirectedVectorSurge(playerId, cellsPlaced);
        }

        public void RecordSaprophageRingConsumption(int playerId, int cellsConsumed)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordSaprophageRingConsumption(playerId, cellsConsumed);
        }

        public void RecordMycotoxicLashKills(int playerId, int cellsKilled)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMycotoxicLashKills(playerId, cellsKilled);
        }

        public void RecordMarginalClampKills(int playerId, int cellsKilled)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMarginalClampKills(playerId, cellsKilled);
        }

        public void RecordSporeSalvoLaunches(int playerId, int launches)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordSporeSalvoLaunches(playerId, launches);
        }

        public void RecordChemobeaconPlacement(int playerId, int tileX, int tileY)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordChemobeaconPlacement(playerId, tileX, tileY);
        }
        #endregion

        #region Phase / Round Routing (suppressed in silent mode)
        public void OnRoundStart(int roundNumber)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.OnRoundStart(roundNumber);
            globalEventsLogManager?.OnRoundStart(roundNumber);
        }

        public void OnRoundComplete(int roundNumber, GameBoard board)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.OnRoundComplete(roundNumber);
            globalEventsLogManager?.OnRoundComplete(roundNumber, board);
        }

        public void OnPhaseStart(string phaseName)
        {
            if (IsSilentMode) return;
            globalEventsLogManager?.OnPhaseStart(phaseName);
        }

        public void OnDraftPhaseStart(string mycovariantName = null)
        {
            if (IsSilentMode) return;
            globalEventsLogManager?.OnDraftPhaseStart(mycovariantName);
        }

        public void OnEndgameTriggered(int roundsRemaining)
        {
            if (IsSilentMode) return;
            globalEventsLogManager?.OnEndgameTriggered(roundsRemaining);
        }

        public void OnGameEnd(string winnerName)
        {
            if (IsSilentMode) return;
            globalEventsLogManager?.OnGameEnd(winnerName);
        }

        public void OnDraftPick(string playerName, string mycovariantName)
        {
            if (IsSilentMode) return;
            globalEventsLogManager?.OnDraftPick(playerName, mycovariantName);
        }
        #endregion

        #region Ignored / Simulation-only (no-op in Unity)
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordNecrophyticBloomPatchCreation(playerId, createdPatchCount);
        }

        public void RecordNecrophyticBloomPatchCreated(
            int playerId,
            int anchorTileId,
            int patchTileCount,
            NutrientPatchType patchType,
            NutrientRewardType rewardType,
            int rewardAmount)
        {
            if (IsSilentMode) return;

            playerActivityLogManager?.RecordNecrophyticBloomPatchCreated(
                playerId,
                anchorTileId,
                patchTileCount,
                patchType,
                rewardType,
                rewardAmount);
        }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordBankedPoints(int playerId, int pointsBanked) { }
        public void RecordHyphalSurgeGrowth(int playerId) { }
        public void RecordDirectedVectorGrowth(int playerId, int cellsPlaced) { }
        public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
        public void ReportJettingMyceliumColonized(int playerId, int colonized) { }
        public void ReportJettingMyceliumToxified(int playerId, int toxified) { }
        public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { }
        public void ReportJettingMyceliumInfested(int playerId, int infested) { }
        public void ReportDirectedVectorReclaimed(int playerId, int reclaimed) { }
        public void ReportDirectedVectorCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportDirectedVectorAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportDirectedVectorColonized(int playerId, int colonized) { }
        public void ReportDirectedVectorInvalid(int playerId, int invalid) { }
        public void ReportDirectedVectorInfested(int playerId, int infested) { }
        public void RecordStandardGrowth(int playerId) { }
        public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized) { }
        public void RecordBastionedCells(int playerId, int count)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordBastionedCells(playerId, count);
        }
        public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged) { }
        public void RecordSurgicalInoculationDrop(int playerId, int count)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordSurgicalInoculationDrop(playerId, count);
        }
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) { }
        public void RecordPerimeterProliferatorGrowth(int playerId) { }
        public void RecordHyphalResistanceTransfer(int playerId, int count)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordHyphalResistanceTransfer(playerId, count);
        }
        public void RecordSeptalAlarmResistance(int playerId, int count)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordSeptalAlarmResistance(playerId, count);
        }
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
        public void RecordHypersystemicRegenerationResistance(int playerId)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordHypersystemicRegenerationResistance(playerId);
        }
        public void RecordHypersystemicDiagonalReclaim(int playerId) { }
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }

        public void RecordMycelialCrescendoSurge(int playerId, string surgeName)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMycelialCrescendoSurge(playerId, surgeName);
        }
        #endregion
    }
}
