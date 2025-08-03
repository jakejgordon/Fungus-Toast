using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Growth;

namespace FungusToast.Unity.UI.GameLog
{
    public class GameLogManager : MonoBehaviour, ISimulationObserver, IGameLogManager
    {
        private Queue<GameLogEntry> logEntries = new Queue<GameLogEntry>();
        private const int MAX_ENTRIES = 50;
        
        // Round summary tracking for the human player using snapshots (like GlobalGameLogManager)
        private PlayerSnapshot roundStartSnapshot;
        
        // Real-time event aggregation tracking
        private Dictionary<string, int> currentEventCounts = new Dictionary<string, int>();
        private Coroutine aggregationCoroutine;
        
        // Track poisoning attacks against the player by ability
        private Dictionary<string, int> playerPoisonedCounts = new Dictionary<string, int>();
        private Coroutine playerPoisonedCoroutine;
        
        public event Action<GameLogEntry> OnNewLogEntry;
        
        private GameBoard board;
        private int humanPlayerId = 0; // Assuming human is always player 0
        
        private struct PlayerSnapshot
        {
            public int LivingCells;
            public int DeadCells;
            public int ToxinCells;
        }
        
        private PlayerSnapshot TakePlayerSnapshot(GameBoard gameBoard, int playerId)
        {
            var playerCells = gameBoard.GetAllCellsOwnedBy(playerId);
            var livingCount = playerCells.Count(c => c.IsAlive);
            var deadCount = playerCells.Count(c => c.IsDead);
            var toxinCount = playerCells.Count(c => c.IsToxin);
            
            return new PlayerSnapshot
            {
                LivingCells = livingCount,
                DeadCells = deadCount,
                ToxinCells = toxinCount
            };
        }
        
        public void Initialize(GameBoard gameBoard)
        {
            board = gameBoard;
            
            // Take initial snapshot BEFORE any spores are placed (for accurate Round 1 tracking)
            if (board != null)
            {
                roundStartSnapshot = TakePlayerSnapshot(board, humanPlayerId);
            }
            
            // Subscribe to relevant board events for immediate feedback
            board.CellPoisoned += OnCellPoisoned;
            
            // Don't add initial game start message here - that's for the global log
        }
        
        private void OnDestroy()
        {
            if (board != null)
            {
                board.CellPoisoned -= OnCellPoisoned;
            }
            
            // Clean up any running aggregation coroutines
            if (aggregationCoroutine != null)
            {
                StopCoroutine(aggregationCoroutine);
                aggregationCoroutine = null;
            }
            
            if (playerPoisonedCoroutine != null)
            {
                StopCoroutine(playerPoisonedCoroutine);
                playerPoisonedCoroutine = null;
            }
        }
        
        public void OnRoundStart(int roundNumber)
        {
            // For Round 1, we already took the snapshot in Initialize() before spores were placed
            // For subsequent rounds, take a fresh snapshot at the start
            if (roundNumber > 1 && board != null)
            {
                roundStartSnapshot = TakePlayerSnapshot(board, humanPlayerId);
            }
            
            // Clear event aggregation counters for the new round
            currentEventCounts.Clear();
            playerPoisonedCounts.Clear();
            
            // Don't add round start messages here - that's for the global log
        }
        
        public void OnRoundComplete(int roundNumber)
        {
            // Take snapshot at end of round and calculate deltas for the human player
            var roundEndSnapshot = TakePlayerSnapshot(board, humanPlayerId);
            
            int cellsGrown = roundEndSnapshot.LivingCells - roundStartSnapshot.LivingCells;
            int cellsDied = roundStartSnapshot.LivingCells - roundEndSnapshot.LivingCells + cellsGrown; // Account for growth and death
            int toxinChange = roundEndSnapshot.ToxinCells - roundStartSnapshot.ToxinCells;
            int deadCellChange = roundEndSnapshot.DeadCells - roundStartSnapshot.DeadCells;
            
            // Only show summary if there were changes or the player has dead cells
            if (cellsGrown != 0 || cellsDied > 0 || toxinChange != 0 || deadCellChange != 0)
            {
                // Use shared formatter for consistent messaging
                string summary = RoundSummaryFormatter.FormatRoundSummary(
                    roundNumber,
                    cellsGrown,
                    cellsDied,
                    toxinChange,
                    deadCellChange, // Pass the change in dead cells, not the total
                    roundEndSnapshot.LivingCells,
                    roundEndSnapshot.DeadCells,
                    roundEndSnapshot.ToxinCells,
                    0f, // occupancy not needed for player-specific format
                    isPlayerSpecific: true);
                
                AddEntry(new GameLogEntry(summary, GameLogCategory.Normal, null, humanPlayerId));
            }
        }
        
        public void OnPhaseStart(string phaseName)
        {
            // Don't add phase start messages here - that's for the global log
            // Only add player-specific phase messages if needed
        }
        
        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            if (playerId == humanPlayerId)
            {
                // Create ability-specific key for aggregation
                string abilityKey = GetAbilityDisplayName(source);
                IncrementAbilityEffect(abilityKey, "poisoned", GameLogCategory.Lucky);
            }
            else if (oldOwnerId == humanPlayerId)
            {
                // Player's cell was poisoned - track by ability
                string abilityKey = GetAbilityDisplayName(source);
                IncrementPlayerPoisonedEffect(abilityKey);
            }
        }
        
        private void IncrementPlayerPoisonedEffect(string abilityKey)
        {
            if (!playerPoisonedCounts.ContainsKey(abilityKey))
                playerPoisonedCounts[abilityKey] = 0;
            playerPoisonedCounts[abilityKey]++;
            
            // Stop any existing player poisoned coroutine and start a new one
            if (playerPoisonedCoroutine != null)
                StopCoroutine(playerPoisonedCoroutine);
            playerPoisonedCoroutine = StartCoroutine(ShowAggregatedPlayerPoisonedAfterDelay());
        }
        
        private System.Collections.IEnumerator ShowAggregatedPlayerPoisonedAfterDelay()
        {
            // Wait a short time to allow multiple poisoning events to aggregate
            yield return new WaitForSeconds(0.5f);
            
            // Calculate total poisoned cells and build breakdown message
            int totalPoisoned = playerPoisonedCounts.Values.Sum();
            if (totalPoisoned > 0)
            {
                var breakdownParts = playerPoisonedCounts
                    .Where(kvp => kvp.Value > 0)
                    .Select(kvp => $"{kvp.Value} by {kvp.Key}")
                    .ToList();
                
                string breakdown = string.Join(", ", breakdownParts);
                string message = totalPoisoned == 1 
                    ? $"1 of your cells was poisoned: {breakdown}"
                    : $"{totalPoisoned} of your cells were poisoned: {breakdown}";
                
                AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky, null, humanPlayerId));
                
                // Reset the counters
                playerPoisonedCounts.Clear();
            }
            
            playerPoisonedCoroutine = null;
        }
        
        private string GetAbilityDisplayName(GrowthSource source)
        {
            return source switch
            {
                GrowthSource.JettingMycelium => "Jetting Mycelium",
                GrowthSource.CytolyticBurst => "Cytolytic Burst",
                GrowthSource.SporicidalBloom => "Sporicidal Bloom",
                GrowthSource.Manual => "Manual toxin placement",
                _ => source.ToString()
            };
        }
        
        private void IncrementAbilityEffect(string abilityKey, string effectType, GameLogCategory category)
        {
            string eventKey = $"{abilityKey}_{effectType}";
            
            if (!currentEventCounts.ContainsKey(eventKey))
                currentEventCounts[eventKey] = 0;
            currentEventCounts[eventKey]++;
            
            // Stop any existing aggregation coroutine and start a new one
            if (aggregationCoroutine != null)
                StopCoroutine(aggregationCoroutine);
            aggregationCoroutine = StartCoroutine(ShowAggregatedAbilityEffectAfterDelay(abilityKey, effectType, category));
        }
        
        private System.Collections.IEnumerator ShowAggregatedAbilityEffectAfterDelay(string abilityKey, string effectType, GameLogCategory category)
        {
            // Wait a short time to allow multiple events to aggregate
            yield return new WaitForSeconds(0.5f);
            
            string eventKey = $"{abilityKey}_{effectType}";
            
            // Show the aggregated message
            if (currentEventCounts.TryGetValue(eventKey, out int count) && count > 0)
            {
                string message = effectType switch
                {
                    "poisoned" => count == 1 ? $"{abilityKey} poisoned enemy cell" : $"{abilityKey} poisoned {count} enemy cells",
                    _ => $"{abilityKey}: {effectType} {count}"
                };
                
                AddEntry(new GameLogEntry(message, category, null, humanPlayerId));
                
                // Reset the counter for this event
                currentEventCounts[eventKey] = 0;
            }
            
            aggregationCoroutine = null;
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
            // Death tracking is now handled by snapshots in OnRoundComplete
            // This method is kept for ISimulationObserver interface compatibility
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