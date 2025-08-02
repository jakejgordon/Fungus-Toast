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
        
        public void Initialize(GameBoard gameBoard)
        {
            board = gameBoard;
            
            // Add initial game start message
            AddEntry(new GameLogEntry("Game started!", GameLogCategory.Normal));
        }
        
        public void OnRoundStart(int roundNumber)
        {
            if (roundNumber > 1) // Don't show for the first round
            {
                AddEntry(new GameLogEntry($"Round {roundNumber} begins", GameLogCategory.Normal));
            }
        }
        
        public void OnPhaseStart(string phaseName)
        {
            AddEntry(new GameLogEntry($"{phaseName} phase begins", GameLogCategory.Normal));
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