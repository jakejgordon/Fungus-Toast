using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.GameLog
{
    public class GameLogManager : MonoBehaviour, ISimulationObserver, IGameLogManager
    {
        private Queue<GameLogEntry> logEntries = new Queue<GameLogEntry>();
        private const int MAX_ENTRIES = 50;
        
        // Round summary tracking for the human player (PlayerId 0)
        private Dictionary<int, int> currentRoundColonized = new Dictionary<int, int>();
        private Dictionary<int, int> currentRoundToxins = new Dictionary<int, int>();
        private Dictionary<int, int> currentRoundDeaths = new Dictionary<int, int>();
        private Dictionary<int, int> currentRoundReclaimed = new Dictionary<int, int>();
        
        public event Action<GameLogEntry> OnNewLogEntry;
        
        private GameBoard board;
        private int humanPlayerId = 0; // Assuming human is always player 0
        
        public void Initialize(GameBoard gameBoard)
        {
            board = gameBoard;
            
            // Subscribe to relevant board events
            board.CellColonized += OnCellColonized;
            board.CellToxified += OnCellToxified;
            board.CellPoisoned += OnCellPoisoned;
            board.CellReclaimed += OnCellReclaimed;
            
            // Add initial game start message
            AddEntry(new GameLogEntry("Game started!", GameLogCategory.Normal));
        }
        
        private void OnDestroy()
        {
            if (board != null)
            {
                board.CellColonized -= OnCellColonized;
                board.CellToxified -= OnCellToxified;
                board.CellPoisoned -= OnCellPoisoned;
                board.CellReclaimed -= OnCellReclaimed;
            }
        }
        
        public void OnRoundStart(int roundNumber)
        {
            // Reset round tracking
            currentRoundColonized.Clear();
            currentRoundToxins.Clear();
            currentRoundDeaths.Clear();
            currentRoundReclaimed.Clear();
            
            // Don't add round start messages here - that's for the global log
        }
        
        public void OnRoundComplete(int roundNumber)
        {
            // Add round summary for human player
            var summaryParts = new List<string>();
            
            if (currentRoundColonized.TryGetValue(humanPlayerId, out int colonized) && colonized > 0)
                summaryParts.Add($"Grew {colonized} new cells");
            if (currentRoundToxins.TryGetValue(humanPlayerId, out int toxins) && toxins > 0)
                summaryParts.Add($"Dropped {toxins} toxins");
            if (currentRoundReclaimed.TryGetValue(humanPlayerId, out int reclaimed) && reclaimed > 0)
                summaryParts.Add($"Reclaimed {reclaimed} cells");
            if (currentRoundDeaths.TryGetValue(humanPlayerId, out int deaths) && deaths > 0)
                summaryParts.Add($"{deaths} cells died");
            
            if (summaryParts.Any())
            {
                string summary = $"Round {roundNumber} summary:\n{string.Join("\n", summaryParts)}";
                AddEntry(new GameLogEntry(summary, GameLogCategory.Normal, null, humanPlayerId));
            }
        }
        
        public void OnPhaseStart(string phaseName)
        {
            // Don't add phase start messages here - that's for the global log
            // Only add player-specific phase messages if needed
        }
        
        private void OnCellColonized(int playerId, int tileId)
        {
            if (playerId == humanPlayerId)
            {
                IncrementRoundCount(currentRoundColonized, playerId);
                // Don't add individual colonization messages to avoid spam
            }
        }
        
        private void OnCellToxified(int playerId, int tileId)
        {
            if (playerId == humanPlayerId)
            {
                IncrementRoundCount(currentRoundToxins, playerId);
            }
        }
        
        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId)
        {
            if (playerId == humanPlayerId)
            {
                IncrementRoundCount(currentRoundToxins, playerId);
                AddEntry(new GameLogEntry($"Poisoned enemy cell", GameLogCategory.Lucky, null, playerId));
            }
            else if (oldOwnerId == humanPlayerId)
            {
                AddEntry(new GameLogEntry($"Your cell was poisoned!", GameLogCategory.Unlucky, null, humanPlayerId));
            }
        }
        
        private void OnCellReclaimed(int playerId, int tileId)
        {
            if (playerId == humanPlayerId)
            {
                IncrementRoundCount(currentRoundReclaimed, playerId);
            }
        }
        
        private void IncrementRoundCount(Dictionary<int, int> counter, int playerId)
        {
            if (!counter.ContainsKey(playerId))
                counter[playerId] = 0;
            counter[playerId]++;
        }
        
        private void AddEntry(GameLogEntry entry)
        {
            logEntries.Enqueue(entry);
            
            // Remove old entries if over limit
            while (logEntries.Count > MAX_ENTRIES)
            {
                logEntries.Dequeue();
            }
            
            OnNewLogEntry?.Invoke(entry);
        }
        
        public IEnumerable<GameLogEntry> GetRecentEntries(int count = 20)
        {
            return logEntries.TakeLast(count);
        }
        
        public void ClearLog()
        {
            logEntries.Clear();
            AddEntry(new GameLogEntry("Log cleared", GameLogCategory.Normal));
        }
        
        // Helper methods for adding specific types of log entries
        public void AddNormalEntry(string message, int? playerId = null)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Normal, null, playerId));
        }
        
        public void AddLuckyEntry(string message, int? playerId = null)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Lucky, null, playerId));
        }
        
        public void AddUnluckyEntry(string message, int? playerId = null)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky, null, playerId));
        }
        
        // Implement ISimulationObserver methods we care about
        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (playerId == humanPlayerId && newMutationPoints > 0)
            {
                AddNormalEntry($"Earned {newMutationPoints} mutation points", playerId);
            }
        }
        
        // Enhanced ISimulationObserver methods for interesting events
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) 
        {
            if (playerId == humanPlayerId && freePointsEarned > 0)
            {
                AddLuckyEntry($"Mutator Phenotype earned {freePointsEarned} free mutation points!", playerId);
            }
        }
        
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) 
        {
            if (playerId == humanPlayerId && freePointsEarned > 0)
            {
                AddLuckyEntry($"Hyperadaptive Drift earned {freePointsEarned} free mutation points!", playerId);
            }
        }
        
        public void ReportJettingMyceliumInfested(int playerId, int infested) 
        {
            if (playerId != humanPlayerId && infested > 0)
            {
                // Enemy player used Jetting Mycelium against us
                AddUnluckyEntry($"Player {playerId + 1} killed {infested} of your cells with Jetting Mycelium", humanPlayerId);
            }
            else if (playerId == humanPlayerId && infested > 0)
            {
                // We used Jetting Mycelium successfully
                AddLuckyEntry($"Jetting Mycelium killed {infested} enemy cells", playerId);
            }
        }
        
        public void ReportHyphalVectoringInfested(int playerId, int infested) 
        {
            if (playerId != humanPlayerId && infested > 0)
            {
                AddUnluckyEntry($"Player {playerId + 1} killed {infested} of your cells with Hyphal Vectoring", humanPlayerId);
            }
            else if (playerId == humanPlayerId && infested > 0)
            {
                AddLuckyEntry($"Hyphal Vectoring killed {infested} enemy cells", playerId);
            }
        }
        
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1)
        {
            if (playerId == humanPlayerId)
            {
                IncrementRoundCount(currentRoundDeaths, playerId);
            }
        }
        
        // Stub implementations for other ISimulationObserver methods that we don't need detailed logging for
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporocidalSporeDrop(int playerId, int count) { }
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
        public void ReportHyphalVectoringReclaimed(int playerId, int reclaimed) { }
        public void ReportHyphalVectoringCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportHyphalVectoringAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportHyphalVectoringColonized(int playerId, int colonized) { }
        public void ReportHyphalVectoringInvalid(int playerId, int invalid) { }
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
    }
}