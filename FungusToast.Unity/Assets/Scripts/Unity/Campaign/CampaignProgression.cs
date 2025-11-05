using System;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Data driven progression settings for each campaign level (preset authoritative for board + AI layout).
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
            public int levelIndex;
            /// <summary>Board preset containing authoritative board size, AI roster, mutation tier cap.</summary>
            public BoardPreset boardPreset;
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
