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
        
        // Track various attacks against the player by ability
        private Dictionary<string, int> playerInfestedCounts = new Dictionary<string, int>();
        private Coroutine playerInfestedCoroutine;
        
        private Dictionary<string, int> playerColonizedCounts = new Dictionary<string, int>();
        private Coroutine playerColonizedCoroutine;
        
        private Dictionary<string, int> playerReclaimedCounts = new Dictionary<string, int>();
        private Coroutine playerReclaimedCoroutine;
        
        private Dictionary<string, int> playerToxifiedCounts = new Dictionary<string, int>();
        private Coroutine playerToxifiedCoroutine;
        
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
            board.CellColonized += OnCellColonized;
            board.CellInfested += OnCellInfested;
            board.CellReclaimed += OnCellReclaimed;
            board.CellToxified += OnCellToxified;
            
            // Don't add initial game start message here - that's for the global log
        }
        
        private void OnDestroy()
        {
            if (board != null)
            {
                board.CellPoisoned -= OnCellPoisoned;
                board.CellColonized -= OnCellColonized;
                board.CellInfested -= OnCellInfested;
                board.CellReclaimed -= OnCellReclaimed;
                board.CellToxified -= OnCellToxified;
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
            
            if (playerInfestedCoroutine != null)
            {
                StopCoroutine(playerInfestedCoroutine);
                playerInfestedCoroutine = null;
            }
            
            if (playerColonizedCoroutine != null)
            {
                StopCoroutine(playerColonizedCoroutine);
                playerColonizedCoroutine = null;
            }
            
            if (playerReclaimedCoroutine != null)
            {
                StopCoroutine(playerReclaimedCoroutine);
                playerReclaimedCoroutine = null;
            }
            
            if (playerToxifiedCoroutine != null)
            {
                StopCoroutine(playerToxifiedCoroutine);
                playerToxifiedCoroutine = null;
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
            playerInfestedCounts.Clear();
            playerColonizedCounts.Clear();
            playerReclaimedCounts.Clear();
            playerToxifiedCounts.Clear();
            
            // Don't add round start messages here - that's for the global log
        }
        
        public void OnRoundComplete(int roundNumber)
        {
            // Take snapshot at end of round and calculate deltas for the human player
            var roundEndSnapshot = TakePlayerSnapshot(board, humanPlayerId);
            
            int livingCellChange = roundEndSnapshot.LivingCells - roundStartSnapshot.LivingCells;
            int deadCellChange = roundEndSnapshot.DeadCells - roundStartSnapshot.DeadCells;
            int toxinChange = roundEndSnapshot.ToxinCells - roundStartSnapshot.ToxinCells;
            
            // Only show summary if there were changes
            if (livingCellChange != 0 || deadCellChange != 0 || toxinChange != 0)
            {
                // Use shared formatter for consistent messaging
                string summary = RoundSummaryFormatter.FormatRoundSummary(
                    roundNumber,
                    livingCellChange,
                    deadCellChange,
                    toxinChange,
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
        
        private void OnCellColonized(int playerId, int tileId, GrowthSource source)
        {
            if (playerId == humanPlayerId)
            {
                // Player colonized a tile - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                
                // Additional debug logging to identify the "reclaim colonized" issue
                if (source == GrowthSource.Reclaim)
                {
                    UnityEngine.Debug.LogError($"[GameLogManager] BUG: OnCellColonized called with GrowthSource.Reclaim! This should be OnCellReclaimed. Tile: {tileId}, Ability: {abilityKey}");
                }
                
                UnityEngine.Debug.Log($"[GameLogManager] OnCellColonized: {abilityKey} on tile {tileId}");
                IncrementAbilityEffect(abilityKey, "colonized", GameLogCategory.Lucky);
            }
            // Note: There's no "enemy colonized our tiles" since colonization is only into empty tiles
        }
        
        private void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            if (playerId == humanPlayerId)
            {
                // Player infested enemy cells - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                UnityEngine.Debug.Log($"[GameLogManager] OnCellInfested: {abilityKey} on tile {tileId} (old owner: {oldOwnerId})");
                IncrementAbilityEffect(abilityKey, "infested", GameLogCategory.Lucky);
            }
            else if (oldOwnerId == humanPlayerId)
            {
                // Player's cell was infested - track by ability
                string abilityKey = GetAbilityDisplayName(source);
                IncrementPlayerInfestedEffect(abilityKey);
            }
        }
        
        private void OnCellReclaimed(int playerId, int tileId, GrowthSource source)
        {
            if (playerId == humanPlayerId)
            {
                // Player reclaimed their own dead cells - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                UnityEngine.Debug.Log($"[GameLogManager] OnCellReclaimed: {abilityKey} on tile {tileId}");
                IncrementAbilityEffect(abilityKey, "reclaimed", GameLogCategory.Lucky);
            }
            // Note: There's no "enemy reclaimed our dead cells" since reclamation is only for your own cells
        }
        
        private void OnCellToxified(int playerId, int tileId, GrowthSource source)
        {
            if (playerId == humanPlayerId)
            {
                // Player toxified empty/dead tiles - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                IncrementAbilityEffect(abilityKey, "toxified", GameLogCategory.Lucky);
            }
            // Note: There's no "enemy toxified our tiles" since toxification only affects empty/dead tiles
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
        
        private void IncrementPlayerInfestedEffect(string abilityKey)
        {
            if (!playerInfestedCounts.ContainsKey(abilityKey))
                playerInfestedCounts[abilityKey] = 0;
            playerInfestedCounts[abilityKey]++;
            
            // Stop any existing player infested coroutine and start a new one
            if (playerInfestedCoroutine != null)
                StopCoroutine(playerInfestedCoroutine);
            playerInfestedCoroutine = StartCoroutine(ShowAggregatedPlayerInfestedAfterDelay());
        }
        
        private System.Collections.IEnumerator ShowAggregatedPlayerInfestedAfterDelay()
        {
            // Wait a short time to allow multiple infestation events to aggregate
            yield return new WaitForSeconds(0.5f);
            
            // Calculate total infested cells and build breakdown message
            int totalInfested = playerInfestedCounts.Values.Sum();
            if (totalInfested > 0)
            {
                var breakdownParts = playerInfestedCounts
                    .Where(kvp => kvp.Value > 0)
                    .Select(kvp => $"{kvp.Value} by {kvp.Key}")
                    .ToList();
                
                string breakdown = string.Join(", ", breakdownParts);
                string message = totalInfested == 1 
                    ? $"1 of your cells was killed: {breakdown}"
                    : $"{totalInfested} of your cells were killed: {breakdown}";
                
                AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky, null, humanPlayerId));
                
                // Reset the counters
                playerInfestedCounts.Clear();
            }
            
            playerInfestedCoroutine = null;
        }
        
        private void IncrementPlayerToxifiedEffect(string abilityKey)
        {
            if (!playerToxifiedCounts.ContainsKey(abilityKey))
                playerToxifiedCounts[abilityKey] = 0;
            playerToxifiedCounts[abilityKey]++;
            
            // Stop any existing player toxified coroutine and start a new one
            if (playerToxifiedCoroutine != null)
                StopCoroutine(playerToxifiedCoroutine);
            playerToxifiedCoroutine = StartCoroutine(ShowAggregatedPlayerToxifiedAfterDelay());
        }
        
        private System.Collections.IEnumerator ShowAggregatedPlayerToxifiedAfterDelay()
        {
            // Wait a short time to allow multiple toxification events to aggregate
            yield return new WaitForSeconds(0.5f);
            
            // Calculate total toxified cells and build breakdown message
            int totalToxified = playerToxifiedCounts.Values.Sum();
            if (totalToxified > 0)
            {
                var breakdownParts = playerToxifiedCounts
                    .Where(kvp => kvp.Value > 0)
                    .Select(kvp => $"{kvp.Value} by {kvp.Key}")
                    .ToList();
                
                string breakdown = string.Join(", ", breakdownParts);
                string message = totalToxified == 1 
                    ? $"1 of your cells was toxified: {breakdown}"
                    : $"{totalToxified} of your cells were toxified: {breakdown}";
                
                AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky, null, humanPlayerId));
                
                // Reset the counters
                playerToxifiedCounts.Clear();
            }
            
            playerToxifiedCoroutine = null;
        }
        
        private string GetAbilityDisplayName(GrowthSource source)
        {
            return source switch
            {
                GrowthSource.JettingMycelium => "Jetting Mycelium",
                GrowthSource.CytolyticBurst => "Cytolytic Burst",
                GrowthSource.SporicidalBloom => "Sporicidal Bloom",
                GrowthSource.MycotoxinTracer => "Mycotoxin Tracer",
                GrowthSource.PutrefactiveCascade => "Putrefactive Cascade",
                GrowthSource.HyphalVectoring => "Hyphal Vectoring",
                GrowthSource.MimeticResilience => "Mimetic Resilience",
                GrowthSource.SurgicalInoculation => "Surgical Inoculation",
                GrowthSource.Ballistospore => "Ballistospore Discharge",
                GrowthSource.HyphalOutgrowth => "Hyphal Outgrowth",
                GrowthSource.TendrilOutgrowth => "Tendril Outgrowth",
                GrowthSource.Manual => "Manual placement",
                _ => source.ToString()
            };
        }
        
        private void IncrementAbilityEffect(string abilityKey, string effectType, GameLogCategory category)
        {
            string eventKey = $"{abilityKey}_{effectType}";
            
            // Debug logging to help track the source of malformed messages
            if (effectType == "reclaimed" || effectType == "colonized")
            {
                UnityEngine.Debug.Log($"[GameLogManager] {abilityKey} -> {effectType} (event key: {eventKey})");
            }
            
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
                    "poisoned" => count == 1 ? $"{abilityKey} poisoned 1 enemy cell" : $"{abilityKey} poisoned {count} enemy cells",
                    "colonized" => count == 1 ? $"{abilityKey} colonized 1 empty tile" : $"{abilityKey} colonized {count} empty tiles",
                    "infested" => count == 1 ? $"{abilityKey} killed 1 enemy cell" : $"{abilityKey} killed {count} enemy cells",
                    "reclaimed" => count == 1 ? $"{abilityKey} reclaimed 1 dead cell" : $"{abilityKey} revived {count} dead cells",
                    "toxified" => count == 1 ? $"{abilityKey} toxified 1 empty tile" : $"{abilityKey} toxified {count} empty tiles",
                    _ => $"{abilityKey}: unknown effect {effectType} ({count})"
                };
                
                // Defensive validation to prevent malformed messages
                if (string.IsNullOrEmpty(message) || message.Contains("colonized") && message.Contains("reclaim"))
                {
                    UnityEngine.Debug.LogError($"Malformed message detected for {abilityKey} with effect type '{effectType}' and count {count}. Generated message: '{message}'");
                    message = $"{abilityKey}: {effectType} {count} tiles/cells"; // Fallback message
                }
                
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
        public void RecordMutationPointIncome(int playerId, int totalMutationPoints)
        {
            // Note: This method receives the total points (base + bonuses) from Player.AssignMutationPoints
            // Since bonuses are reported separately via RecordAdaptiveExpressionBonus, RecordAnabolicInversionBonus, etc.
            // we don't show the total here to avoid confusion.
            
            // If we wanted to show base income only, we'd need a separate method since this gets total.
            // For now, we rely on the bonus-specific messages and round summaries to show point changes.
            
            // Uncomment below if you want to show total mutation points earned each round:
            // if (playerId == humanPlayerId && totalMutationPoints > 0)
            // {
            //     AddNormalEntry($"Earned {totalMutationPoints} mutation points", playerId);
            // }
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
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) 
        {
            if (playerId == humanPlayerId && bonus > 0)
            {
                string message = bonus == 1 
                    ? "Earned 1 free mutation point from Adaptive Expression!" 
                    : $"Earned {bonus} free mutation points from Adaptive Expression!";
                AddLuckyEntry(message, playerId);
            }
        }
        
        public void RecordAnabolicInversionBonus(int playerId, int bonus) 
        {
            if (playerId == humanPlayerId && bonus > 0)
            {
                string message = bonus == 1 
                    ? "Earned 1 free mutation point from Anabolic Inversion!" 
                    : $"Earned {bonus} free mutation points from Anabolic Inversion!";
                AddLuckyEntry(message, playerId);
            }
        }
        
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) 
        {
            if (playerId == humanPlayerId && catabolizedMutationPoints > 0)
            {
                string message = catabolizedMutationPoints == 1 
                    ? $"Earned 1 free mutation point from Mycotoxin Catabolism (catabolized {toxinsCatabolized} toxin{(toxinsCatabolized == 1 ? "" : "s")})!" 
                    : $"Earned {catabolizedMutationPoints} free mutation points from Mycotoxin Catabolism (catabolized {toxinsCatabolized} toxin{(toxinsCatabolized == 1 ? "" : "s")})!";
                AddLuckyEntry(message, playerId);
            }
        }
        
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
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