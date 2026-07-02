using System;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.AI; // for strategy names (referencing AIRoster)
using FungusToast.Unity.Grid;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Authoritative definition of a campaign board configuration including size and AI lineup.
    /// Existing presets can keep using <see cref="aiPlayers"/> for a fixed ordered lineup.
    /// Newer presets may instead specify an AI pool plus <see cref="pooledAiPlayerCount"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/BoardPreset", fileName = "BoardPreset")]
    public class BoardPreset : ScriptableObject
    {
        [Header("Identity")]
        public string presetId; // unique string id for lookup / save

        [Header("Board Dimensions")]
        public int boardWidth = 160;
        public int boardHeight = 160;

        [Header("Board Visuals")]
        public BoardMediumConfig boardMedium;

        [Header("Human Starting Position Overrides")]
        public List<Vector2Int> humanStartingCoordinatePool = new();

        [Header("Fixed AI Lineup")]
        public List<AIPlayerSpec> aiPlayers = new(); // ordered AI specifications; preferred when populated

        [Header("Optional AI Pool")]
        [Min(0)] public int pooledAiPlayerCount = 0; // active AI count when using aiStrategyPool instead of aiPlayers
        public List<string> aiStrategyPool = new(); // eligible strategies for pooled campaign selection

        public bool UsesFixedAiLineup => aiPlayers != null && aiPlayers.Count > 0;
        public bool UsesAiPool => !UsesFixedAiLineup && aiStrategyPool != null && aiStrategyPool.Count > 0 && pooledAiPlayerCount > 0;

        public int GetConfiguredAiPlayerCount()
        {
            if (UsesFixedAiLineup)
            {
                return aiPlayers.Count;
            }

            return Mathf.Max(0, pooledAiPlayerCount);
        }

        [Header("AI Pool Adaptation Overrides")]
        public List<AiPoolAdaptationEntry> poolAdaptationOverrides = new();

        /// <summary>Specification for one AI player in the preset.</summary>
        [Serializable]
        public class AIPlayerSpec
        {
            public string strategyName; // must match AIRoster strategy name; resolved at runtime
            public OptionalVector2Int startingCoordinate; // optional forced starting tile (unset => auto placement)
            public List<string> startingAdaptationIds = new();
        }

        /// <summary>Maps a pool strategy name to the adaptations that strategy starts with.</summary>
        [Serializable]
        public class AiPoolAdaptationEntry
        {
            public string strategyName; // must match a name in aiStrategyPool
            public List<string> startingAdaptationIds = new();
        }

        /// <summary>Unity-serializable optional Vector2Int.</summary>
        [Serializable]
        public struct OptionalVector2Int
        {
            [SerializeField] private bool hasValue;
            [SerializeField] private Vector2Int value;

            public bool HasValue => hasValue;

            public Vector2Int Value
            {
                get
                {
                    if (!hasValue)
                    {
                        throw new InvalidOperationException("OptionalVector2Int has no value.");
                    }

                    return value;
                }
            }

            public OptionalVector2Int(Vector2Int value)
            {
                hasValue = true;
                this.value = value;
            }

            public static implicit operator OptionalVector2Int(Vector2Int value) => new(value);
        }
    }
}
