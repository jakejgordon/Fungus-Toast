using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    public class TooltipPositioner : MonoBehaviour
    {
        public RectTransform tooltipRectTransform;
        public float leftPadding = 30f; // space from the left edge
        public float topPadding = 30f;  // space from the top edge

        public void SetPosition(Vector2 ignored)
        {
            if (tooltipRectTransform == null)
                return;

            // Always place tooltip at top left with specified padding
            tooltipRectTransform.anchoredPosition = new Vector2(leftPadding, -topPadding);
        }
    }
}
