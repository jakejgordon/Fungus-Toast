using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Represents a single directional growth preview UI cell.
    /// Now a standalone serializable class for reuse and cleaner parent component.
    /// </summary>
    [Serializable]
    public class GrowthDirectionCell
    {
        private const float PercentTextScale = 1.18f;
        private const float SurgeTextScale = 1.12f;

        [Tooltip("Which direction this cell represents.")] public GrowthPreviewDirection direction;
        [Tooltip("Assign only the parent container GameObject. Children are auto-resolved by fixed names.")]
        public GameObject parent;

        // Auto-resolved
        private TextMeshProUGUI percentText;
        private TextMeshProUGUI surgeText;
        private Image arrowImage;
        private bool resolved;
        private Color originalArrowColor;

        private const float SurgeDisplayEpsilon = 1e-6f;

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
            originalArrowColor = arrowImage.color; // capture starting color so we leave it unchanged

            // Ensure rich text for surge
            surgeText.richText = true;

            ApplyTextScale(percentText, PercentTextScale);
            percentText.fontStyle = FontStyles.Bold;
            percentText.color = UIStyleTokens.Text.Primary;

            ApplyTextScale(surgeText, SurgeTextScale);
            resolved = true;
        }

        public void SetChance(float baseChance, float surgeBonus, Color zeroChanceColor)
        {
            if (!resolved) return; // safety
            if (percentText)
            {
                percentText.text = $"{(baseChance * 100f):F2}%";
                percentText.color = baseChance <= SurgeDisplayEpsilon ? zeroChanceColor : UIStyleTokens.Text.Primary;
            }

            if (surgeText)
            {
                if (surgeBonus > SurgeDisplayEpsilon)
                {
                    if (!surgeText.gameObject.activeSelf) surgeText.gameObject.SetActive(true);
                    string successHex = ColorUtility.ToHtmlStringRGB(UIStyleTokens.State.Success);
                    surgeText.text = $"<b><color=#{successHex}>+{(surgeBonus * 100f):F3}%</color></b>";
                }
                else if (surgeText.gameObject.activeSelf)
                    surgeText.gameObject.SetActive(false);
            }

            // Keep arrow color constant for readability (no dimming / whitening)
            if (arrowImage && arrowImage.color != originalArrowColor)
                arrowImage.color = originalArrowColor;
        }

        private static void ApplyTextScale(TextMeshProUGUI label, float scale)
        {
            if (label == null || scale <= 1f) return;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin *= scale;
                label.fontSizeMax *= scale;
            }
            else
            {
                label.fontSize *= scale;
            }
        }
    }
}
