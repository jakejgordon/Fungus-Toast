using System;
using UnityEngine;

namespace FungusToast.Unity.UI.GameLog
{
    [System.Serializable]
    public class GameLogEntry
    {
        public int Round { get; set; }
        public string Message { get; set; }
        public Color TextColor { get; set; }
        public DateTime Timestamp { get; set; }
        public GameLogCategory Category { get; set; }
        public int? PlayerId { get; set; } // For player-specific entries

        public GameLogEntry(string message, GameLogCategory category, Color? color = null, int? playerId = null)
        {
            Message = message;
            Category = category;
            
            // Auto-assign color based on category if not provided
            if (color.HasValue)
            {
                TextColor = color.Value;
            }
            else
            {
                TextColor = category switch
                {
                    GameLogCategory.Normal => Color.white,
                    // Improved colors for better readability and accessibility
                    GameLogCategory.Lucky => new Color(0.6f, 1f, 0.6f), // Brighter, more readable green
                    GameLogCategory.Unlucky => new Color(1f, 0.6f, 0.6f), // Softer, more readable red
                    _ => Color.white
                };
            }
            
            PlayerId = playerId;
            Timestamp = DateTime.Now;
            Round = GameManager.Instance?.Board?.CurrentRound ?? 0;
        }
    }

    public enum GameLogCategory
    {
        Normal,   // Normal white text for informational messages
        Lucky,    // Green text for positive/fortunate events
        Unlucky   // Red text for negative/unfortunate events
    }
}