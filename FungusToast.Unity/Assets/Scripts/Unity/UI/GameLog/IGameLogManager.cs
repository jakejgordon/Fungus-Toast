using System;
using System.Collections.Generic;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Interface for game log managers to work with the generic UI_GameLogPanel
    /// </summary>
    public interface IGameLogManager
    {
        event Action<GameLogEntry> OnNewLogEntry;
        IEnumerable<GameLogEntry> GetRecentEntries(int count = 20);
        void ClearLog();
    }
}