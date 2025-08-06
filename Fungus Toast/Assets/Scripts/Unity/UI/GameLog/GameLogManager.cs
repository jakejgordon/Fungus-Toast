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
        
        // Growth cycle consolidation tracking
        private Dictionary<string, int> growthCycleColonizationCounts = new Dictionary<string, int>();
        private Dictionary<string, int> growthCycleInfestationCounts = new Dictionary<string, int>();
        private Dictionary<string, int> growthCycleReclamationCounts = new Dictionary<string, int>();
        private Dictionary<string, int> growthCycleToxificationCounts = new Dictionary<string, int>();
        private bool isTrackingGrowthCycle = false;
        
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
        
        // Track decay phase deaths by reason for summary
        private Dictionary<DeathReason, int> decayPhaseDeaths = new Dictionary<DeathReason, int>();
        private bool isTrackingDecayPhase = false;
        
        // Track resistance applications during growth phase for summary
        private int hypersystemicResistanceApplications = 0;
        private bool isTrackingResistanceApplications = false;
        
        // Track growth phase effects for summary
        private int regenerativeHyphaeReclaims = 0;
        private int hypersystemicDiagonalReclaims = 0;
        private bool isTrackingGrowthPhaseEffects = false;
        
        public event Action<GameLogEntry> OnNewLogEntry;
        
        private GameBoard board;
        private int humanPlayerId = 0; // Assuming human is always player 0
        
        // Reference to GameLogRouter to check silent mode
        private GameLogRouter gameLogRouter;
        
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
            
            // Subscribe to growth cycle events for consolidation
            board.PreGrowthCycle += OnPreGrowthCycle;
            board.PostGrowthPhase += OnPostGrowthPhase;
        }
        
        /// <summary>
        /// Sets the GameLogRouter reference to check for silent mode.
        /// Should be called after GameLogRouter is created.
        /// </summary>
        public void SetGameLogRouter(GameLogRouter router)
        {
            gameLogRouter = router;
        }
        
        /// <summary>
        /// Checks if logging should be suppressed due to silent mode.
        /// </summary>
        private bool IsSilentMode => gameLogRouter?.IsSilentMode ?? false;
        
        private void OnDestroy()
        {
            if (board != null)
            {
                board.CellPoisoned -= OnCellPoisoned;
                board.CellColonized -= OnCellColonized;
                board.CellInfested -= OnCellInfested;
                board.CellReclaimed -= OnCellReclaimed;
                board.CellToxified -= OnCellToxified;
                board.PreGrowthCycle -= OnPreGrowthCycle;
                board.PostGrowthPhase -= OnPostGrowthPhase;
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
            growthCycleColonizationCounts.Clear();
            growthCycleInfestationCounts.Clear();
            growthCycleReclamationCounts.Clear();
            growthCycleToxificationCounts.Clear();
            
            // Start tracking decay phase deaths for this round
            decayPhaseDeaths.Clear();
            isTrackingDecayPhase = true;
            
            // Start tracking resistance applications for this round
            hypersystemicResistanceApplications = 0;
            isTrackingResistanceApplications = true;
            
            // Start tracking growth phase effects for this round
            regenerativeHyphaeReclaims = 0;
            hypersystemicDiagonalReclaims = 0;
            isTrackingGrowthPhaseEffects = true;
            
            // Don't add round start messages here - that's for the global log
        }
        
        public void OnRoundComplete(int roundNumber)
        {
            // Show decay phase summary if we were tracking deaths (do this first, before round summary)
            if (isTrackingDecayPhase)
            {
                ShowDecayPhaseSummary();
                isTrackingDecayPhase = false;
            }
            
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
        
        private void ShowDecayPhaseSummary()
        {
            if (IsSilentMode) return;
            
            // Only show summary if at least one cell died
            int totalDeaths = decayPhaseDeaths.Values.Sum();
            if (totalDeaths == 0) return;
            
            var deathReasonParts = new List<string>();
            
            // Sort death reasons by count (descending) for consistent ordering
            var sortedDeaths = decayPhaseDeaths
                .Where(kvp => kvp.Value > 0)
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key.ToString()); // Secondary sort by name for consistency
            
            foreach (var kvp in sortedDeaths)
            {
                string reasonName = GetDeathReasonDisplayName(kvp.Key);
                string part = kvp.Value == 1 
                    ? $"1 cell killed by {reasonName}"
                    : $"{kvp.Value} cells killed by {reasonName}";
                deathReasonParts.Add(part);
            }
            
            string message = $"Decay Summary: {string.Join(", ", deathReasonParts)}";
            AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky, null, humanPlayerId));
        }
        
        private void ShowResistanceApplicationsSummary()
        {
            if (IsSilentMode) return;
            
            // Only show summary if at least one cell gained resistance
            if (hypersystemicResistanceApplications == 0) return;
            
            string message = hypersystemicResistanceApplications == 1 
                ? "1 cell gained resistance from Hypersystemic Regeneration!"
                : $"{hypersystemicResistanceApplications} cells gained resistance from Hypersystemic Regeneration!";
            
            AddEntry(new GameLogEntry(message, GameLogCategory.Lucky, null, humanPlayerId));
        }
        
        private void ShowGrowthPhaseSummary()
        {
            if (IsSilentMode) return;
            
            var summaryParts = new List<string>();
            
            // Regenerative Hyphae reclamations
            if (regenerativeHyphaeReclaims > 0)
            {
                string part = regenerativeHyphaeReclaims == 1 
                    ? "Regenerative Hyphae reclaimed 1 dead cell"
                    : $"Regenerative Hyphae reclaimed {regenerativeHyphaeReclaims} dead cells";
                summaryParts.Add(part);
            }
            
            // Hypersystemic Regeneration diagonal reclamations (subset of total reclamations)
            if (hypersystemicDiagonalReclaims > 0)
            {
                string part = hypersystemicDiagonalReclaims == 1 
                    ? "Hypersystemic Regeneration reclaimed 1 cell diagonally"
                    : $"Hypersystemic Regeneration reclaimed {hypersystemicDiagonalReclaims} cells diagonally";
                summaryParts.Add(part);
            }
            
            // Hypersystemic Regeneration resistance applications
            if (hypersystemicResistanceApplications > 0)
            {
                string part = hypersystemicResistanceApplications == 1 
                    ? "Hypersystemic Regeneration granted resistance to 1 cell"
                    : $"Hypersystemic Regeneration granted resistance to {hypersystemicResistanceApplications} cells";
                summaryParts.Add(part);
            }
            
            // Only show summary if there were any growth phase effects
            if (summaryParts.Count > 0)
            {
                string message = $"Growth Phase Summary: {string.Join(", ", summaryParts)}";
                AddEntry(new GameLogEntry(message, GameLogCategory.Lucky, null, humanPlayerId));
            }
        }
        
        private string GetDeathReasonDisplayName(DeathReason reason)
        {
            return reason switch
            {
                DeathReason.Age => "Old Age",
                DeathReason.Randomness => "Randomness",
                DeathReason.PutrefactiveMycotoxin => "Putrefactive Mycotoxin",
                DeathReason.SporicidalBloom => "Sporicidal Bloom",
                DeathReason.MycotoxinPotentiation => "Mycotoxin Potentiation",
                DeathReason.HyphalVectoring => "Hyphal Vectoring",
                DeathReason.JettingMycelium => "Jetting Mycelium",
                DeathReason.Infested => "Infestation",
                DeathReason.Poisoned => "Poisoning",
                DeathReason.PutrefactiveCascade => "Putrefactive Cascade",
                DeathReason.PutrefactiveCascadePoison => "Putrefactive Cascade Poison",
                DeathReason.CytolyticBurst => "Cytolytic Burst",
                DeathReason.Unknown => "Unknown Cause",
                _ => reason.ToString()
            };
        }
        
        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            if (IsSilentMode) return;
            
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
            if (IsSilentMode) return;
            
            if (playerId == humanPlayerId)
            {
                // Player colonized a tile - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                
                // During growth cycles, track for consolidation instead of immediate aggregation
                if (isTrackingGrowthCycle)
                {
                    if (!growthCycleColonizationCounts.ContainsKey(abilityKey))
                        growthCycleColonizationCounts[abilityKey] = 0;
                    growthCycleColonizationCounts[abilityKey]++;
                }
                else
                {
                    // Outside of growth cycles, use normal aggregation
                    IncrementAbilityEffect(abilityKey, "colonized", GameLogCategory.Lucky);
                }
            }
            // Note: There's no "enemy colonized our tiles" since colonization is only into empty tiles
        }
        
        private void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            if (IsSilentMode) return;
            
            if (playerId == humanPlayerId)
            {
                // Player infested enemy cells - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                
                // During growth cycles, track for consolidation instead of immediate aggregation
                if (isTrackingGrowthCycle)
                {
                    if (!growthCycleInfestationCounts.ContainsKey(abilityKey))
                        growthCycleInfestationCounts[abilityKey] = 0;
                    growthCycleInfestationCounts[abilityKey]++;
                }
                else
                {
                    // Outside of growth cycles, use normal aggregation
                    IncrementAbilityEffect(abilityKey, "infested", GameLogCategory.Lucky);
                }
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
            if (IsSilentMode) return;
            
            if (playerId == humanPlayerId)
            {
                // Player reclaimed their own dead cells
                string abilityKey = GetAbilityDisplayName(source);
                
                // RegenerativeHyphae goes to growth phase summary, not growth cycle summary
                if (source == GrowthSource.RegenerativeHyphae)
                {
                    // Don't track in growth cycle - this is handled by the growth phase summary
                    return;
                }
                
                // During growth cycles, track for consolidation instead of immediate aggregation
                if (isTrackingGrowthCycle)
                {
                    if (!growthCycleReclamationCounts.ContainsKey(abilityKey))
                        growthCycleReclamationCounts[abilityKey] = 0;
                    growthCycleReclamationCounts[abilityKey]++;
                }
                else
                {
                    // Outside of growth cycles, use normal aggregation
                    IncrementAbilityEffect(abilityKey, "reclaimed", GameLogCategory.Lucky);
                }
            }
            // Note: There's no "enemy reclaimed our dead cells" since reclamation is only for your own cells
        }
        
        private void OnCellToxified(int playerId, int tileId, GrowthSource source)
        {
            if (IsSilentMode) return;
            
            if (playerId == humanPlayerId)
            {
                // Player toxified empty/dead tiles - track for offensive aggregation
                string abilityKey = GetAbilityDisplayName(source);
                
                // During growth cycles, track for consolidation instead of immediate aggregation
                if (isTrackingGrowthCycle)
                {
                    if (!growthCycleToxificationCounts.ContainsKey(abilityKey))
                        growthCycleToxificationCounts[abilityKey] = 0;
                    growthCycleToxificationCounts[abilityKey]++;
                }
                else
                {
                    // Outside of growth cycles, use normal aggregation
                    IncrementAbilityEffect(abilityKey, "toxified", GameLogCategory.Lucky);
                }
            }
            // Note: There's no "enemy toxified our tiles" since toxification only affects empty/dead tiles
        }
        
        private void OnPreGrowthCycle()
        {
            if (IsSilentMode) return;
            
            // If we were already tracking, show the previous cycle's results
            if (isTrackingGrowthCycle)
            {
                ShowConsolidatedGrowthCycleSummary();
            }
            
            // Start tracking growth cycle events for this growth cycle
            isTrackingGrowthCycle = true;
            growthCycleColonizationCounts.Clear();
            growthCycleInfestationCounts.Clear();
            growthCycleReclamationCounts.Clear();
            growthCycleToxificationCounts.Clear();
        }
        
        private void OnPostGrowthPhase()
        {
            if (IsSilentMode) return;
            
            // Show growth phase summary with regenerative hyphae and hypersystemic regeneration effects
            if (isTrackingGrowthPhaseEffects)
            {
                ShowGrowthPhaseSummary();
                regenerativeHyphaeReclaims = 0;
                hypersystemicDiagonalReclaims = 0;
                isTrackingGrowthPhaseEffects = false;
            }
            
            // Show resistance applications summary separately (kept for compatibility)
            if (isTrackingResistanceApplications)
            {
                // Note: resistance applications are now included in growth phase summary above
                // but we keep this for backwards compatibility in case other systems use it
                hypersystemicResistanceApplications = 0;
                isTrackingResistanceApplications = false;
            }
            
            // Show final cycle's consolidated growth cycle summary after growth phase completes
            if (isTrackingGrowthCycle)
            {
                ShowConsolidatedGrowthCycleSummary();
                growthCycleColonizationCounts.Clear();
                growthCycleInfestationCounts.Clear();
                growthCycleReclamationCounts.Clear();
                growthCycleToxificationCounts.Clear();
            }
            isTrackingGrowthCycle = false;
        }
        
        private void ShowConsolidatedGrowthCycleSummary()
        {
            if (IsSilentMode) return;
            
            var allActivities = new List<(string action, Dictionary<string, int> counts)>
            {
                ("Colonized", growthCycleColonizationCounts),
                ("Killed", growthCycleInfestationCounts),
                ("Reclaimed", growthCycleReclamationCounts),
                ("Toxified", growthCycleToxificationCounts)
            };

            var summaries = new List<string>();

            foreach (var (action, counts) in allActivities)
            {
                var total = counts.Values.Sum();
                if (total == 0) continue;

                var breakdownParts = counts
                    .Where(kvp => kvp.Value > 0)
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => $"{kvp.Value} from {kvp.Key}")
                    .ToList();

                string breakdown = string.Join(", ", breakdownParts);
                string activity = total == 1
                    ? $"{action.ToLower()} 1 {GetTargetType(action)}: {breakdown}"
                    : $"{action.ToLower()} {total} {GetTargetTypePlural(action)}: {breakdown}";

                summaries.Add(activity);
            }

            if (summaries.Count > 0)
            {
                string message = $"Growth Cycle #{board.CurrentGrowthCycle} - {string.Join(", ", summaries)}";
                AddEntry(new GameLogEntry(message, GameLogCategory.Lucky, null, humanPlayerId));
            }
        }

        private string GetTargetType(string action)
        {
            return action switch
            {
                "Colonized" => "cell",
                "Killed" => "enemy cell",
                "Reclaimed" => "dead cell",
                "Toxified" => "tile",
                _ => "target"
            };
        }

        private string GetTargetTypePlural(string action)
        {
            return action switch
            {
                "Colonized" => "cells",
                "Killed" => "enemy cells",
                "Reclaimed" => "dead cells",
                "Toxified" => "tiles",
                _ => "targets"
            };
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
            
            // Check silent mode before showing the message
            if (IsSilentMode)
            {
                // Reset counters but don't show message
                playerPoisonedCounts.Clear();
                playerPoisonedCoroutine = null;
                yield break;
            }
            
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
            
            // Check silent mode before showing the message
            if (IsSilentMode)
            {
                // Reset counters but don't show message
                playerInfestedCounts.Clear();
                playerInfestedCoroutine = null;
                yield break;
            }
            
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
            
            // Check silent mode before showing the message
            if (IsSilentMode)
            {
                // Reset counters but don't show message
                playerToxifiedCounts.Clear();
                playerToxifiedCoroutine = null;
                yield break;
            }
            
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
                GrowthSource.RegenerativeHyphae => "Regenerative Hyphae",
                GrowthSource.Manual => "Manual placement",
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
            
            // Check silent mode before showing the message
            if (IsSilentMode)
            {
                // Reset counters but don't show message
                if (currentEventCounts.ContainsKey(eventKey))
                {
                    currentEventCounts[eventKey] = 0;
                }
                aggregationCoroutine = null;
                yield break;
            }
            
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
            // Suppress logging if in silent mode
            if (IsSilentMode) return;
            
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
            // Only track player deaths during decay phase for summary
            if (isTrackingDecayPhase && playerId == humanPlayerId)
            {
                if (!decayPhaseDeaths.ContainsKey(reason))
                    decayPhaseDeaths[reason] = 0;
                decayPhaseDeaths[reason] += deathCount;
            }
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
        public void RecordRegenerativeHyphaeReclaim(int playerId) 
        {
            // Track regenerative hyphae reclamations during growth phase for batching summary
            if (isTrackingGrowthPhaseEffects && playerId == humanPlayerId)
            {
                regenerativeHyphaeReclaims++;
            }
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
        public void RecordHypersystemicRegenerationResistance(int playerId) 
        {
            // Only track resistance applications during growth phase for batching summary
            if (isTrackingResistanceApplications && playerId == humanPlayerId)
            {
                hypersystemicResistanceApplications++;
            }
        }
        public void RecordHypersystemicDiagonalReclaim(int playerId) 
        {
            // Track diagonal reclamations during growth phase for batching summary
            if (isTrackingGrowthPhaseEffects && playerId == humanPlayerId)
            {
                hypersystemicDiagonalReclaims++;
            }
        }
    }
}