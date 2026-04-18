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
        public string boardPresetId; // preset identifier for board size/layout
        public int seed; // RNG seed for reproducibility
        public int boardWidth; // persisted board width for current level
        public int boardHeight; // persisted board height for current level
        public int humanMoldIndex = 0; // selected mold icon for the single campaign human player
        public bool pendingAdaptationSelection; // true when player must pick adaptation before continuing
        public bool campaignCompleted; // true after final victory
        public CampaignVictorySnapshot pendingVictorySnapshot; // serialized scoreboard snapshot for pending adaptation resumes
        public bool pendingDefeatCarryoverSelection; // true when player must choose preserved adaptations before resetting the run
        public List<string> pendingDefeatCarryoverOptions = new(); // adaptation ids available to preserve from the failed run
        public List<string> pendingNextRunCarryoverAdaptationIds = new(); // applied automatically to the next fresh run
        public List<string> resolvedAiStrategyNames = new(); // active AI lineup for current level; persisted so pooled levels resume consistently
        public MoldinessProgressionState moldiness = new(); // persistent moldiness progression toward permanent unlocks
    }

    [Serializable]
    public class CampaignVictorySnapshot
    {
        public int clearedLevelDisplay;
        public int moldinessAwarded;
        public int moldinessProgressBeforeAward;
        public int moldinessProgressAfterAward;
        public int moldinessThresholdAfterAward;
        public int moldinessTierBeforeAward;
        public int moldinessTierAfterAward;
        public int pendingMoldinessUnlockCount;
        public List<CampaignVictoryPlayerRow> rows = new();
    }

    [Serializable]
    public class CampaignVictoryPlayerRow
    {
        public int rank;
        public int playerId;
        public string playerName;
        public int livingCells;
        public int resistantCells;
        public int deadCells;
        public int toxinCells;
    }
}
