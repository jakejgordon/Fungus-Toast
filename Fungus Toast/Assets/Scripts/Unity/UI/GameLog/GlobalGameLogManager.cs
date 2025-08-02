using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Board;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Manages global game events that all players should see
    /// (round starts, phase changes, system messages, etc.)
    /// </summary>
    public class GlobalGameLogManager : MonoBehaviour, IGameLogManager
    {
        private Queue<GameLogEntry> logEntries = new Queue<GameLogEntry>();
        private const int MAX_ENTRIES = 30; // Fewer entries for global log
        
        public event Action<GameLogEntry> OnNewLogEntry;
        
        private GameBoard board;
        
        // Board state snapshots for round summaries
        private BoardSnapshot roundStartSnapshot;
        
        private struct BoardSnapshot
        {
            public int LivingCells;
            public int DeadCells;
            public int ToxinCells;
            public float Occupancy;
        }
        
        private BoardSnapshot TakeSnapshot(GameBoard gameBoard)
        {
            var allCells = gameBoard.GetAllCells();
            var livingCount = allCells.Count(c => c.IsAlive);
            var deadCount = allCells.Count(c => c.IsDead);
            var toxinCount = allCells.Count(c => c.IsToxin);
            var occupancy = gameBoard.GetOccupiedTileRatio() * 100f;
            
            return new BoardSnapshot
            {
                LivingCells = livingCount,
                DeadCells = deadCount,
                ToxinCells = toxinCount,
                Occupancy = occupancy
            };
        }
        
        public void Initialize(GameBoard gameBoard)
        {
            board = gameBoard;
            
            // Don't add initial game start message - Round 1 begins will be shown instead
        }
        
        public void OnRoundStart(int roundNumber)
        {
            // Take snapshot at start of round
            if (board != null)
            {
                roundStartSnapshot = TakeSnapshot(board);
            }
            
            // Always show round start messages, including Round 1
            AddEntry(new GameLogEntry($"Round {roundNumber} begins", GameLogCategory.Normal));
        }
        
        public void OnRoundComplete(int roundNumber, GameBoard gameBoard)
        {
            // Take snapshot at end of round and calculate deltas
            var roundEndSnapshot = TakeSnapshot(gameBoard);
            
            int cellsGrown = roundEndSnapshot.LivingCells - roundStartSnapshot.LivingCells;
            int cellsDied = roundStartSnapshot.LivingCells - roundEndSnapshot.LivingCells + cellsGrown; // Account for growth and death
            int toxinChange = roundEndSnapshot.ToxinCells - roundStartSnapshot.ToxinCells;
            int deadCellChange = roundEndSnapshot.DeadCells - roundStartSnapshot.DeadCells;
            
            // Create global round summary with net changes
            var summaryParts = new List<string>();
            
            if (cellsGrown != 0)
                summaryParts.Add($"{Math.Abs(cellsGrown)} cells {(cellsGrown > 0 ? "grown" : "lost")}");
            
            if (cellsDied > 0)
                summaryParts.Add($"{cellsDied} cell{(cellsDied == 1 ? "" : "s")} died");
                
            if (toxinChange != 0)
                summaryParts.Add($"{Math.Abs(toxinChange)} toxins {(toxinChange > 0 ? "added" : "removed")}");
                
            string changes = summaryParts.Any() ? string.Join(", ", summaryParts) : "no net changes";
            
            string summary = $"Round {roundNumber} summary: {changes}, " +
                           $"board now {roundEndSnapshot.Occupancy:F1}% occupied " +
                           $"({roundEndSnapshot.LivingCells} living, {roundEndSnapshot.DeadCells} dead, {roundEndSnapshot.ToxinCells} toxins)";
            
            AddEntry(new GameLogEntry(summary, GameLogCategory.Normal));
        }
        
        public void OnPhaseStart(string phaseName)
        {
            // Don't add basic phase messages as they're redundant with UI_PhaseBanner and UI_PhaseProgressTracker
            // This method is kept for potential future non-redundant phase messages
        }
        
        public void OnDraftPhaseStart(string mycovariantName = null)
        {
            string message = string.IsNullOrEmpty(mycovariantName) 
                ? "Mycovariant draft phase begins" 
                : $"Testing: {mycovariantName}";
            AddEntry(new GameLogEntry(message, GameLogCategory.Normal));
        }
        
        public void OnEndgameTriggered(int roundsRemaining)
        {
            string message = roundsRemaining == 1 
                ? "Final Round!" 
                : $"Endgame in {roundsRemaining} rounds";
            AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky));
        }
        
        public void OnGameEnd(string winnerName)
        {
            AddEntry(new GameLogEntry($"Game Over - {winnerName} wins!", GameLogCategory.Lucky));
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
        
        public IEnumerable<GameLogEntry> GetRecentEntries(int count = 15)
        {
            return logEntries.TakeLast(count);
        }
        
        public void ClearLog()
        {
            logEntries.Clear();
            AddEntry(new GameLogEntry("Global log cleared", GameLogCategory.Normal));
        }
        
        // Helper methods for adding specific types of log entries
        public void AddNormalEntry(string message)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Normal));
        }
        
        public void AddLuckyEntry(string message)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Lucky));
        }
        
        public void AddUnluckyEntry(string message)
        {
            AddEntry(new GameLogEntry(message, GameLogCategory.Unlucky));
        }
    }
}