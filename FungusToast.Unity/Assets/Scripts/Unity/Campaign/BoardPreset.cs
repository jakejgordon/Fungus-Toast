using System;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.AI; // for strategy names (referencing AIRoster)

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Authoritative definition of a campaign board configuration including size, mutation tier cap, and AI lineup.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/BoardPreset", fileName = "BoardPreset")]
    public class BoardPreset : ScriptableObject
    {
        [Header("Identity")]
        public string presetId; // unique string id for lookup / save

        [Header("Board Dimensions")]
        public int boardWidth = 160;
        public int boardHeight = 160;

        [Header("Mutation Tier Cap")]
        public int mutationTierMax = 2; // highest tier unlocked when using this preset

        [Header("AI Lineup")]
        public List<AIPlayerSpec> aiPlayers = new(); // ordered AI specifications

        /// <summary>Specification for one AI player in the preset.</summary>
        [Serializable]
        public class AIPlayerSpec
        {
            public string strategyName; // must match AIRoster strategy name; resolved at runtime
            public Vector2Int? startingCoordinate; // optional forced starting tile (null => auto placement)
        }
    }
}
