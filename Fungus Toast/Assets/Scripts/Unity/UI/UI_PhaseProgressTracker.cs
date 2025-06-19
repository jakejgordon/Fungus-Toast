using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseProgressTracker : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mutationPhaseLabel;
        [SerializeField] private TextMeshProUGUI growthPhaseLabel;
        [SerializeField] private List<TextMeshProUGUI> growthCycleLabels; // Should be exactly 5
        [SerializeField] private TextMeshProUGUI decayPhaseLabel;

        private Color normalColor = new Color(1f, 1f, 1f, 0.6f);
        private Color highlightColor = new Color(1f, 1f, 1f, 1f);

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
