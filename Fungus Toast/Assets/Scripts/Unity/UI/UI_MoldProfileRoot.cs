using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using FungusToast.Core.Config;
using FungusToast.Core.Phases; // GrowthMutationProcessor lives here

namespace FungusToast.Unity.UI
{
    public class UI_MoldProfileRoot : MonoBehaviour
    {
        [Header("Growth Preview (Orthogonals Only)")] 
        [Tooltip("Parent containers for the 4 orthogonal growth preview cells (N,E,S,W). You only need to assign the parent objects. Child objects are auto-located by name.")]
        [SerializeField] private GrowthDirectionCell[] orthogonalCells = Array.Empty<GrowthDirectionCell>();
        [SerializeField] private Image centerPlayerIcon;

        [Header("Stat Text References")] 
        [SerializeField] private TextMeshProUGUI statsAgeProtectionText;          // UI_StatsRootAgeDecayReductionChance
        [SerializeField] private TextMeshProUGUI statsRandomDecayBreakdownText;    // UI_StatsRootRandomDecayChance
        [SerializeField] private TextMeshProUGUI statsAgeThresholdText;            // UI_StatsRootStartDecayAge

        [Header("Formatting / Visuals")] 
        [SerializeField] private Color zeroChanceColor = new Color(1f,1f,1f,0.35f);
        [SerializeField] private Color finalZeroGreen = new Color(0.4f, 1f, 0.4f, 1f);

        private Player trackedPlayer;
        private List<Player> allPlayers;

        private const float SurgeDisplayEpsilon = 1e-6f;

        // Child name constants
        private const string ArrowName = "UI_GrowthPreviewCellArrowImage";
        private const string PercentName = "UI_GrowthPreviewCellPercentText";
        private const string SurgeName = "UI_GrowthPreviewCellSurgeText";

        private bool cellsResolved = false;

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            EnsureCellsResolved();
            Refresh();
        }

        public void Refresh()
        {
            if (trackedPlayer == null) return;
            EnsureCellsResolved();
            UpdateGrowthChances();
            UpdateStats();
        }

        private void EnsureCellsResolved()
        {
            if (cellsResolved) return;

            // If user never assigned any cells, attempt auto-discovery: use immediate children that contain the percent text child
            if (orthogonalCells == null || orthogonalCells.Length == 0)
            {
                var discovered = new List<GrowthDirectionCell>();
                // Look at direct children only (avoid deep recursion creating duplicates)
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child.Find(PercentName) != null) // likely a preview cell parent
                    {
                        var gdc = new GrowthDirectionCell { parent = child.gameObject };
                        discovered.Add(gdc);
                    }
                }
                if (discovered.Count > 0)
                    orthogonalCells = discovered.ToArray();
            }

            if (orthogonalCells != null)
            {
                foreach (var cell in orthogonalCells)
                {
                    if (cell == null) continue;
                    cell.ResolveChildren(ArrowName, PercentName, SurgeName);
                }
            }

            cellsResolved = true;
        }

        private void UpdateGrowthChances()
        {
            if (orthogonalCells == null || orthogonalCells.Length == 0) return;
            (float baseChance, float surgeBonus) = GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(trackedPlayer);
            foreach (var cell in orthogonalCells)
            {
                cell?.SetChance(baseChance, surgeBonus, zeroChanceColor);
            }
        }

        private void UpdateStats()
        {
            if (allPlayers == null) return;
            int harmonyLevel = trackedPlayer.GetMutationLevel(MutationIds.HomeostaticHarmony);
            float harmony = harmonyLevel * GameBalance.HomeostaticHarmonyEffectPerLevel;
            float rawRandom = trackedPlayer.GetBaseMycelialDegradationRisk(allPlayers);
            float finalRandom = Mathf.Max(0f, rawRandom - harmony);
            int chronoLevel = trackedPlayer.GetMutationLevel(MutationIds.ChronoresilientCytoplasm);
            float addedThreshold = chronoLevel * GameBalance.ChronoresilientCytoplasmEffectPerLevel;
            float ageThreshold = GameBalance.AgeAtWhichDecayChanceIncreases + addedThreshold;

            if (statsAgeProtectionText)
                statsAgeProtectionText.text = $"Homeostatic Reduction: {(harmony * 100f):F3}%";

            if (statsRandomDecayBreakdownText)
            {
                string finalDisplay = finalRandom <= 1e-6f
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(finalZeroGreen)}>{(finalRandom*100f):F2}%</color>"
                    : $"{(finalRandom*100f):F2}%";
                statsRandomDecayBreakdownText.text = $"Random Decay Chance: {(rawRandom*100f):F2}% - {(harmony*100f):F2}% = {finalDisplay}";
            }

            if (statsAgeThresholdText)
                statsAgeThresholdText.text = $"Age Risk Threshold: {ageThreshold:F0}";
        }

        [Serializable]
        private class GrowthDirectionCell
        {
            [Tooltip("Assign only the parent container GameObject. Children are auto-resolved by fixed names.")]
            public GameObject parent;
            // Auto-resolved
            private TextMeshProUGUI percentText;
            private TextMeshProUGUI surgeText;
            private Image arrowImage;
            private bool resolved;

            public void ResolveChildren(string arrowName, string percentName, string surgeName)
            {
                if (resolved) return;
                if (parent == null)
                    throw new Exception("GrowthDirectionCell parent is not assigned.");

                Transform pT = parent.transform;
                Transform percentT = pT.Find(percentName) ?? throw new Exception($"Missing child '{percentName}' under '{parent.name}'");
                Transform surgeT = pT.Find(surgeName) ?? throw new Exception($"Missing child '{surgeName}' under '{parent.name}'");
                Transform arrowT = pT.Find(arrowName) ?? throw new Exception($"Missing child '{arrowName}' under '{parent.name}'");

                percentText = percentT.GetComponent<TextMeshProUGUI>() ?? throw new Exception($"Child '{percentName}' lacks TextMeshProUGUI component.");
                surgeText = surgeT.GetComponent<TextMeshProUGUI>() ?? throw new Exception($"Child '{surgeName}' lacks TextMeshProUGUI component.");
                arrowImage = arrowT.GetComponent<Image>() ?? throw new Exception($"Child '{arrowName}' lacks Image component.");

                // Ensure rich text for surge
                surgeText.richText = true;
                resolved = true;
            }

            public void SetChance(float baseChance, float surgeBonus, Color zeroChanceColor)
            {
                if (!resolved) return; // safety
                if (percentText)
                    percentText.text = $"{(baseChance * 100f):F3}%";

                if (surgeText)
                {
                    if (surgeBonus > SurgeDisplayEpsilon)
                    {
                        if (!surgeText.gameObject.activeSelf) surgeText.gameObject.SetActive(true);
                        surgeText.text = $"<b><color=#32CD32>+{(surgeBonus * 100f):F3}%</color></b>";
                    }
                    else if (surgeText.gameObject.activeSelf)
                        surgeText.gameObject.SetActive(false);
                }

                if (arrowImage)
                {
                    if (baseChance > 0f)
                    {
                        var c = arrowImage.color; c.a = 1f; arrowImage.color = c;
                    }
                    else
                    {
                        arrowImage.color = zeroChanceColor;
                    }
                }
            }
        }
    }
}
