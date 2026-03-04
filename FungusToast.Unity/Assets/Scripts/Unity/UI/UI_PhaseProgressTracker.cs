using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseProgressTracker : MonoBehaviour
    {
        private const float PhaseLabelScale = 1.10f;
        private const float CycleLabelScale = 1.12f;

        [SerializeField] private TextMeshProUGUI mutationPhaseLabel;
        [SerializeField] private TextMeshProUGUI growthPhaseLabel;
        [SerializeField] private List<TextMeshProUGUI> growthCycleLabels; // Should be exactly 5
        [SerializeField] private TextMeshProUGUI decayPhaseLabel;

        private Color normalColor;
        private Color highlightColor;

        private void Awake()
        {
            normalColor = UIStyleTokens.Text.Muted;
            highlightColor = UIStyleTokens.Text.Primary;
            ApplyReadabilityScale();
            ResetAllStyles();
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
            HighlightLabel(mutationPhaseLabel);
        }

        public void HighlightMutationPhase()
        {
            ResetAllStyles();
            HighlightLabel(mutationPhaseLabel);
        }

        public void AdvanceToNextGrowthCycle(int cycle)
        {
            ResetAllStyles();

            if (cycle >= 1 && cycle <= growthCycleLabels.Count)
            {
                HighlightLabel(growthCycleLabels[cycle - 1]);
                HighlightLabel(growthPhaseLabel);  // ← NEW
            }
        }


        public void HighlightDecayPhase()
        {
            ResetAllStyles();
            HighlightLabel(decayPhaseLabel);
        }

        public void HighlightDraftPhase()
        {
            ResetAllStyles();
            HighlightLabel(mutationPhaseLabel); // Highlight the mutationPhaseLabel even though it now says "DRAFT"
        }


        private void ResetAllStyles()
        {
            SetDim(mutationPhaseLabel);
            SetDim(growthPhaseLabel); 
            SetDim(decayPhaseLabel);

            foreach (var cycleLabel in growthCycleLabels)
                SetDim(cycleLabel);
        }


        private void HighlightLabel(TextMeshProUGUI label)
        {
            if (label == null) return;
            label.color = highlightColor;
            label.fontStyle = FontStyles.Bold;
            label.ForceMeshUpdate(); // ensure style redraws
        }

        private void SetDim(TextMeshProUGUI label)
        {
            if (label == null) return;
            label.color = normalColor;
            label.fontStyle = FontStyles.Normal;
            label.ForceMeshUpdate(); // force Unity to apply style
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
