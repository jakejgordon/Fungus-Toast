using System;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Data driven progression settings for each campaign level.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/CampaignProgression", fileName = "CampaignProgression")]
    public class CampaignProgression : ScriptableObject
    {
        /// <summary>
        /// Ordered list of level specifications (index position defines progression order).
        /// </summary>
        public List<LevelSpec> levels = new();

        /// <summary>
        /// Defines parameters for a single campaign level.
        /// </summary>
        [Serializable]
        public class LevelSpec
        {
            /// <summary>Zero-based index for clarity / debugging (optional; may mirror list index).</summary>
            public int levelIndex; //0-based
            /// <summary>Identifier for the board preset (size/layout) used at this level.</summary>
            public string boardPresetId; // maps to board size/layout later
            /// <summary>Number of AI opponents (total players = aiCount +1 human currently).</summary>
            public int aiCount; // number of AI opponents
            /// <summary>Maximum mutation tier unlocked at this level.</summary>
            public int mutationTierMax; // maximum mutation tier unlocked at this level
        }

        /// <summary>
        /// Returns the level spec at <paramref name="idx"/> or throws if out of range.
        /// </summary>
        public LevelSpec Get(int idx)
        {
            if (idx < 0 || idx >= levels.Count)
                throw new IndexOutOfRangeException($"Invalid campaign level index {idx}. Count={levels.Count}");
            return levels[idx];
        }

        /// <summary>Total number of levels defined.</summary>
        public int MaxLevels => levels.Count;
    }
}
