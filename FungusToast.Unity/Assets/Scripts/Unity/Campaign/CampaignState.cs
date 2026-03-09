using System;
using System.Collections.Generic;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Runtime / persisted campaign state. Only a single active campaign is supported initially.
    /// </summary>
    [Serializable]
    public class CampaignState
    {
        public string runId; // GUID string
        public int levelIndex; // current level (0-based)
        public List<string> selectedAdaptationIds = new(); // unique adaptation ids picked during this run
        public int unlockedMutationTierMax; // enforced ceiling on mutation tiers
        public string boardPresetId; // preset identifier for board size/layout
        public int seed; // RNG seed for reproducibility
        public int boardWidth; // persisted board width for current level
        public int boardHeight; // persisted board height for current level
        public bool pendingAdaptationSelection; // true when player must pick adaptation before continuing
        public bool campaignCompleted; // true after final victory
        public CampaignVictorySnapshot pendingVictorySnapshot; // serialized scoreboard snapshot for pending adaptation resumes
    }

    [Serializable]
    public class CampaignVictorySnapshot
    {
        public int clearedLevelDisplay;
        public List<CampaignVictoryPlayerRow> rows = new();
    }

    [Serializable]
    public class CampaignVictoryPlayerRow
    {
        public int rank;
        public int playerId;
        public string playerName;
        public int livingCells;
        public int deadCells;
        public int toxinCells;
    }
}
