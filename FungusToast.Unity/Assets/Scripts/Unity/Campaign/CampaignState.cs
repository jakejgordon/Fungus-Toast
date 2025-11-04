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
        public List<TraitStack> traitStacks = new(); // placeholder for future Genetic Traits
        public int unlockedMutationTierMax; // enforced ceiling on mutation tiers
        public string boardPresetId; // preset identifier for board size/layout
        public int seed; // RNG seed for reproducibility

        [Serializable]
        public class TraitStack
        {
            public string id; // trait id
            public int level; // stack level
        }
    }
}
