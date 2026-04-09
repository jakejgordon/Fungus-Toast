using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseProgressTracker : MonoBehaviour
    {
        private const float PhaseLabelScale = 1.10f;
        private const float CycleLabelScale = 1.12f;
        private const float InactiveAlpha = 0.52f;
        private const float CompletedAlpha = 0.90f;
        private const float SecondaryScaleMultiplier = 1.04f;
        private const float PrimaryScaleMultiplier = 1.12f;
        private const float PrimaryPulseAmplitude = 0.045f;
        private const float PrimaryPulseSpeed = 8f;
        private const float PrimaryOutlineWidth = 0.28f;
        private const float SecondaryOutlineWidth = 0.12f;
        private const float PrimaryCharacterSpacing = 2.5f;
        private const float SecondaryCharacterSpacing = 0.8f;

        [SerializeField] private TextMeshProUGUI mutationPhaseLabel;
        [SerializeField] private TextMeshProUGUI growthPhaseLabel;
        [SerializeField] private List<TextMeshProUGUI> growthCycleLabels; // Should be exactly 5
        [SerializeField] private TextMeshProUGUI decayPhaseLabel;

        private Color normalColor;
        private readonly Dictionary<TextMeshProUGUI, Vector3> baseScales = new();
        private TextMeshProUGUI activePulseLabel;

        private void Awake()
        {
            normalColor = WithAlpha(UIStyleTokens.Text.Muted, InactiveAlpha);
            ApplyReadabilityScale();
            CacheBaseScales();
            ResetAllStyles();
        }

        private void Update()
        {
            UpdatePulse();
        }

        private void ApplyReadabilityScale()
        {
            ApplyTextScale(mutationPhaseLabel, PhaseLabelScale);
            ApplyTextScale(growthPhaseLabel, PhaseLabelScale);
            ApplyTextScale(decayPhaseLabel, PhaseLabelScale);

            ConfigureSingleLineFit(mutationPhaseLabel);
            ConfigureSingleLineFit(growthPhaseLabel);
            ConfigureSingleLineFit(decayPhaseLabel);

            foreach (var cycleLabel in growthCycleLabels)
                ApplyTextScale(cycleLabel, CycleLabelScale);
        }

        private void CacheBaseScales()
        {
            baseScales.Clear();

            CacheBaseScale(mutationPhaseLabel);
            CacheBaseScale(growthPhaseLabel);
            CacheBaseScale(decayPhaseLabel);

            foreach (var cycleLabel in growthCycleLabels)
            {
                CacheBaseScale(cycleLabel);
            }
        }

        private void CacheBaseScale(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            baseScales[label] = label.rectTransform.localScale;
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

        private static void ConfigureSingleLineFit(TextMeshProUGUI label)
        {
            if (label == null) return;

            float targetSize = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMax = targetSize;
            label.fontSizeMin = Mathf.Max(10f, targetSize * 0.70f);
        }

        public void ResetTracker()
        {
            ResetAllStyles();
            HighlightPrimary(mutationPhaseLabel, UIStyleTokens.Accent.Spore);
        }

        public void HighlightMutationPhase()
        {
            ResetAllStyles();
            HighlightPrimary(mutationPhaseLabel, UIStyleTokens.Accent.Spore);
        }

        public void AdvanceToNextGrowthCycle(int cycle)
        {
            ResetAllStyles();
            HighlightSecondary(growthPhaseLabel, UIStyleTokens.State.Success);

            if (cycle >= 1 && cycle <= growthCycleLabels.Count)
            {
                for (int i = 0; i < cycle - 1; i++)
                {
                    SetCompleted(growthCycleLabels[i], UIStyleTokens.State.Success);
                }

                HighlightPrimary(growthCycleLabels[cycle - 1], UIStyleTokens.State.Success);
            }
        }


        public void HighlightDecayPhase()
        {
            ResetAllStyles();
            HighlightPrimary(decayPhaseLabel, UIStyleTokens.State.Warning);
        }

        public void HighlightDraftPhase()
        {
            ResetAllStyles();
            HighlightPrimary(mutationPhaseLabel, UIStyleTokens.Accent.Hyphae);
        }


        private void ResetAllStyles()
        {
            activePulseLabel = null;
            SetDim(mutationPhaseLabel);
            SetDim(growthPhaseLabel);
            SetDim(decayPhaseLabel);

            foreach (var cycleLabel in growthCycleLabels)
                SetDim(cycleLabel);
        }


        private void HighlightPrimary(TextMeshProUGUI label, Color accentColor)
        {
            if (label == null) return;

            ApplyStyledState(
                label,
                UIStyleTokens.Text.Primary,
                accentColor,
                PrimaryOutlineWidth,
                FontStyles.Bold,
                PrimaryScaleMultiplier,
                PrimaryCharacterSpacing,
                enablePulse: true);
        }

        private void HighlightSecondary(TextMeshProUGUI label, Color accentColor)
        {
            if (label == null) return;

            ApplyStyledState(
                label,
                Blend(accentColor, UIStyleTokens.Text.Primary, 0.30f),
                accentColor,
                SecondaryOutlineWidth,
                FontStyles.Bold,
                SecondaryScaleMultiplier,
                SecondaryCharacterSpacing,
                enablePulse: false);
        }

        private void SetCompleted(TextMeshProUGUI label, Color accentColor)
        {
            if (label == null) return;

            ApplyStyledState(
                label,
                WithAlpha(Blend(accentColor, UIStyleTokens.Text.Primary, 0.45f), CompletedAlpha),
                Color.clear,
                0f,
                FontStyles.Bold,
                1f,
                0f,
                enablePulse: false);
        }

        private void SetDim(TextMeshProUGUI label)
        {
            if (label == null) return;

            label.color = normalColor;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = 0f;
            label.outlineWidth = 0f;
            label.outlineColor = Color.clear;
            label.extraPadding = false;
            label.rectTransform.localScale = GetBaseScale(label);
            label.ForceMeshUpdate();
        }

        private void ApplyStyledState(
            TextMeshProUGUI label,
            Color faceColor,
            Color outlineColor,
            float outlineWidth,
            FontStyles fontStyle,
            float scaleMultiplier,
            float characterSpacing,
            bool enablePulse)
        {
            label.color = faceColor;
            label.fontStyle = fontStyle;
            label.characterSpacing = characterSpacing;
            label.outlineWidth = outlineWidth;
            label.outlineColor = outlineWidth > 0f ? WithAlpha(Darken(outlineColor, 0.55f), 0.95f) : Color.clear;
            label.extraPadding = outlineWidth > 0f;
            label.rectTransform.localScale = GetBaseScale(label) * scaleMultiplier;

            if (enablePulse)
            {
                activePulseLabel = label;
            }

            label.ForceMeshUpdate();
        }

        private void UpdatePulse()
        {
            if (activePulseLabel == null)
            {
                return;
            }

            Vector3 baseScale = GetBaseScale(activePulseLabel);
            float pulse = 1f + PrimaryPulseAmplitude * Mathf.Sin(Time.unscaledTime * PrimaryPulseSpeed);
            activePulseLabel.rectTransform.localScale = baseScale * (PrimaryScaleMultiplier * pulse);
        }

        private Vector3 GetBaseScale(TextMeshProUGUI label)
        {
            if (label != null && baseScales.TryGetValue(label, out Vector3 scale))
            {
                return scale;
            }

            return Vector3.one;
        }

        private static Color Blend(Color from, Color to, float amount)
        {
            return Color.Lerp(from, to, Mathf.Clamp01(amount));
        }

        private static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, UIStyleTokens.Surface.Canvas, Mathf.Clamp01(amount));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        public void SetMutationPhaseLabel(string text)
        {
            if (mutationPhaseLabel != null)
            {
                mutationPhaseLabel.text = text;
                mutationPhaseLabel.ForceMeshUpdate(); // ensure text updates immediately
            }
        }

    }
}
