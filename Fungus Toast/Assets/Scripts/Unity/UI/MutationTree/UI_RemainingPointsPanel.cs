using UnityEngine;
using TMPro;

namespace FungusToast.Unity.UI.MutationTree
{
    public class UI_RemainingPointsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI mutationPointsText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Animator pulseAnimator;

        private int lastDisplayedPoints = -1;

        public void UpdateMutationPointsDisplay(int points)
        {
            if (mutationPointsText != null)
            {
                mutationPointsText.text = $"Mutation Points: {points}";
            }

            if (points > 0 && points != lastDisplayedPoints)
            {
                TriggerPulse();
                SetHighlight(true);
            }
            else if (points == 0)
            {
                SetHighlight(false);
            }

            lastDisplayedPoints = points;
        }

        private void TriggerPulse()
        {
            if (pulseAnimator != null)
            {
                pulseAnimator.SetTrigger("Pulse");
            }
        }

        private void SetHighlight(bool highlight)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = highlight ? 1.0f : 0.6f;
            }
        }
    }
}
