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

        /// <summary>
        /// Shows a projected cost preview: "Mutation Points: X → Y"
        /// </summary>
        public void ShowProjectedCost(int currentPoints, int cost)
        {
            if (mutationPointsText == null) return;
            int projected = Mathf.Max(0, currentPoints - cost);
            string mutedHex = ColorUtility.ToHtmlStringRGB(UIStyleTokens.Text.Muted);
            mutationPointsText.text = $"Mutation Points: {currentPoints}  <color=#{mutedHex}>→ {projected}</color>";
        }

        /// <summary>
        /// Restores the normal display text.
        /// </summary>
        public void ClearProjectedCost(int currentPoints)
        {
            if (mutationPointsText == null) return;
            mutationPointsText.text = $"Mutation Points: {currentPoints}";
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
