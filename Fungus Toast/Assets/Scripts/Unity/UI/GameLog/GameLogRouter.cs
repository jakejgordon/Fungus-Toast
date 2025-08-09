using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Board;

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
        
        /// <summary>
        /// When true, all logging calls are ignored. Used during fast-forward mode.
        /// </summary>
        public bool IsSilentMode { get; set; } = false;
        
        public GameLogRouter(GameLogManager playerLog, GlobalGameLogManager globalLog)
        {
            playerActivityLogManager = playerLog;
            globalEventsLogManager = globalLog;
        }
        
        /// <summary>
        /// Enables silent mode to suppress all logging during fast-forward operations.
        /// </summary>
        public void EnableSilentMode()
        {
            IsSilentMode = true;
            UnityEngine.Debug.Log("[GameLogRouter] Silent mode ENABLED - logging suppressed during fast-forward");
        }
        
        /// <summary>
        /// Disables silent mode to resume normal logging after fast-forward operations.
        /// </summary>
        public void DisableSilentMode()
        {
            IsSilentMode = false;
            UnityEngine.Debug.Log("[GameLogRouter] Silent mode DISABLED - logging resumed");
        }
        
        // === AUTOMATIC ROUTING METHODS ===
        
        // Player-specific events (mutation points, abilities, etc.) → Player Log
        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMutationPointIncome(playerId, newMutationPoints);
        }
            
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordAdaptiveExpressionBonus(playerId, bonus);
        }
            
        public void RecordAnabolicInversionBonus(int playerId, int bonus)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordAnabolicInversionBonus(playerId, bonus);
        }
            
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordToxinCatabolism(playerId, toxinsCatabolized, catabolizedMutationPoints);
        }
            
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMutatorPhenotypeMutationPointsEarned(playerId, freePointsEarned);
        }
            
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordHyperadaptiveDriftMutationPointsEarned(playerId, freePointsEarned);
        }
        
        // === MANUAL ROUTING METHODS ===
        // These provide explicit control for events that need custom routing logic
        
        public void OnRoundStart(int roundNumber)
        {
            if (IsSilentMode) return;
            // Player gets notified for round summary tracking
            playerActivityLogManager?.OnRoundStart(roundNumber);
            // Global gets the main round start message
            globalEventsLogManager?.OnRoundStart(roundNumber);
        }
        
        public void OnRoundComplete(int roundNumber, GameBoard board)
        {
            if (IsSilentMode) return;
            // Both logs get round completion for their respective summaries
            playerActivityLogManager?.OnRoundComplete(roundNumber);
            globalEventsLogManager?.OnRoundComplete(roundNumber, board);
        }
        
        public void OnPhaseStart(string phaseName)
        {
            if (IsSilentMode) return;
            // Only global log shows phase transitions
            globalEventsLogManager?.OnPhaseStart(phaseName);
        }
        
        public void OnDraftPhaseStart(string mycovariantName = null)
        {
            if (IsSilentMode) return;
            // Only global log shows draft phase start
            globalEventsLogManager?.OnDraftPhaseStart(mycovariantName);
        }
        
        public void OnEndgameTriggered(int roundsRemaining)
        {
            if (IsSilentMode) return;
            // Only global log shows endgame countdown
            globalEventsLogManager?.OnEndgameTriggered(roundsRemaining);
        }
        
        public void OnGameEnd(string winnerName)
        {
            if (IsSilentMode) return;
            // Only global log shows game end
            globalEventsLogManager?.OnGameEnd(winnerName);
        }
        
        public void OnDraftPick(string playerName, string mycovariantName)
        {
            if (IsSilentMode) return;
            // Only global log shows draft picks
            globalEventsLogManager?.OnDraftPick(playerName, mycovariantName);
        }
        
        // === STUB IMPLEMENTATIONS ===
        // All other ISimulationObserver methods that we don't need in Unity
        // Silent mode still applies to prevent any potential future implementations from logging
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) 
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordCellDeath(playerId, reason, deathCount);
        }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) 
        {
            // Regenerative hyphae reclaims are now tracked via GameBoard.CellReclaimed event
            // No longer routed through ISimulationObserver - keeping for compatibility
        }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims) { }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordBankedPoints(int playerId, int pointsBanked) { }
        public void RecordHyphalSurgeGrowth(int playerId) { }
        public void RecordHyphalVectoringGrowth(int playerId, int cellsPlaced) { }
        public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
        public void ReportJettingMyceliumColonized(int playerId, int colonized) { }
        public void ReportJettingMyceliumToxified(int playerId, int toxified) { }
        public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { }
        public void ReportJettingMyceliumInfested(int playerId, int infested) { }
        public void ReportHyphalVectoringReclaimed(int playerId, int reclaimed) { }
        public void ReportHyphalVectoringCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportHyphalVectoringAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportHyphalVectoringColonized(int playerId, int colonized) { }
        public void ReportHyphalVectoringInvalid(int playerId, int invalid) { }
        public void ReportHyphalVectoringInfested(int playerId, int infested) { }
        public void RecordStandardGrowth(int playerId) { }
        public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized) { }
        public void RecordBastionedCells(int playerId, int count) { }
        public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged) { }
        public void RecordSurgicalInoculationDrop(int playerId, int count) { }
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) { }
        public void RecordPerimeterProliferatorGrowth(int playerId) { }
        public void RecordHyphalResistanceTransfer(int playerId, int count) { }
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
        public void RecordHypersystemicRegenerationResistance(int playerId) { }
        public void RecordHypersystemicDiagonalReclaim(int playerId) { }
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordMutatorPhenotypeUpgrade(playerId, mutationName);
        }
        public void RecordSpecificMutationUpgrade(int playerId, string mutationName)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordSpecificMutationUpgrade(playerId, mutationName);
        }
        
        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordChemotacticMycotoxinsRelocations(playerId, relocations);
        }

        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained)
        {
            if (IsSilentMode) return;
            playerActivityLogManager?.RecordOntogenicRegressionEffect(playerId, sourceMutationName, sourceLevelsLost, targetMutationName, targetLevelsGained);
        }
    }
}