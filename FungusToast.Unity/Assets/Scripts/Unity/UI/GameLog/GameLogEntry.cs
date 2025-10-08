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

        public GameLogEntry(string message, GameLogCategory category, Color? color = null, int? playerId = null, int? explicitRound = null)
        {
            Message = message;
            Category = category;
            if (color.HasValue)
            {
                TextColor = color.Value;
            }
            else
            {
                TextColor = category switch
                {
                    GameLogCategory.Normal => Color.white,
                    GameLogCategory.Lucky => new Color(0.6f, 1f, 0.6f),
                    GameLogCategory.Unlucky => new Color(1f, 0.6f, 0.6f),
                    _ => Color.white
                };
            }
            PlayerId = playerId;
            Timestamp = DateTime.Now;
            Round = explicitRound ?? (GameManager.Instance?.Board?.CurrentRound ?? 0);
        }
    }

    public enum GameLogCategory
    {
        Normal,
        Lucky,
        Unlucky
    }
}
