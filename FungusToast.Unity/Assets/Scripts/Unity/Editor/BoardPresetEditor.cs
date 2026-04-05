#if UNITY_EDITOR
using System;
using FungusToast.Core.AI;
using FungusToast.Unity.Campaign;
using UnityEditor;
using UnityEngine;

namespace FungusToast.Unity.Editor
{
    [CustomEditor(typeof(BoardPreset))]
    public class BoardPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var preset = (BoardPreset)target;

            EditorGUILayout.Space(12f);
            DrawAiConfigurationSummary(preset);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawAiConfigurationSummary(BoardPreset preset)
        {
            EditorGUILayout.LabelField("Campaign AI Summary", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mode", GetModeLabel(preset));
                EditorGUILayout.LabelField("Configured AI Count", preset.GetConfiguredAiPlayerCount().ToString());

                if (preset.UsesFixedAiLineup)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Fixed lineup", EditorStyles.miniBoldLabel);
                    for (int i = 0; i < preset.aiPlayers.Count; i++)
                    {
                        var spec = preset.aiPlayers[i];
                        DrawStrategyEntry(i, spec?.strategyName, includeSlot: true);
                    }
                }
                else if (preset.UsesAiPool)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Eligible pool", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(
                        $"This preset resolves {preset.pooledAiPlayerCount} unique opponents from the pool below at campaign runtime.",
                        MessageType.Info);

                    for (int i = 0; i < preset.aiStrategyPool.Count; i++)
                    {
                        DrawStrategyEntry(i, preset.aiStrategyPool[i], includeSlot: false);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "No fixed AI lineup or active AI pool is configured on this BoardPreset.",
                        MessageType.Warning);
                }
            }
        }

        private static string GetModeLabel(BoardPreset preset)
        {
            if (preset.UsesFixedAiLineup)
            {
                return "Fixed AI Lineup";
            }

            if (preset.UsesAiPool)
            {
                return "Pooled AI Selection";
            }

            return "No AI configuration";
        }

        private static void DrawStrategyEntry(int index, string strategyName, bool includeSlot)
        {
            string trimmedName = string.IsNullOrWhiteSpace(strategyName) ? "<empty>" : strategyName.Trim();
            var catalog = AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Campaign, trimmedName)
                       ?? AIRoster.GetStrategyCatalogEntry(StrategySetEnum.Proven, trimmedName);
            var profile = AIRoster.GetStrategyProfile(StrategySetEnum.Campaign, trimmedName)
                       ?? AIRoster.GetStrategyProfile(StrategySetEnum.Proven, trimmedName);

            string prefix = includeSlot ? $"Slot {index + 1}" : $"Pool {index + 1}";
            string title = catalog != null ? $"{prefix}: {trimmedName}" : $"{prefix}: {trimmedName} (unresolved)";
            EditorGUILayout.LabelField(title, EditorStyles.label);

            using (new EditorGUI.IndentLevelScope())
            {
                if (catalog == null && profile == null)
                {
                    EditorGUILayout.HelpBox("Strategy id not found in Campaign/Proven roster.", MessageType.Warning);
                    return;
                }

                if (catalog != null)
                {
                    EditorGUILayout.LabelField("Display", catalog.DisplayName);
                }

                if (profile != null)
                {
                    EditorGUILayout.LabelField("Theme", profile.Theme.ToString());
                    EditorGUILayout.LabelField("Power", profile.PowerTier.ToString());
                    EditorGUILayout.LabelField("Role", profile.Role.ToString());
                    EditorGUILayout.LabelField("Campaign Difficulty", profile.CampaignDifficulty?.ToString() ?? "—");
                    if (!string.IsNullOrWhiteSpace(profile.Intent))
                    {
                        EditorGUILayout.LabelField("Intent", profile.Intent, EditorStyles.wordWrappedLabel);
                    }
                }

                EditorGUILayout.Space(2f);
            }
        }
    }
}
#endif
