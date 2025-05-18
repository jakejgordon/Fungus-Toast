using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    public class TooltipPositioner : MonoBehaviour
    {
        public RectTransform tooltipRectTransform;
        public float horizontalOffset = 50f;
        public float verticalMargin = 10f;

        public void SetPosition(Vector2 screenPosition)
        {
            if (tooltipRectTransform == null)
                return;

            // First, add horizontal offset
            float tooltipWidth = tooltipRectTransform.rect.width;
            screenPosition.x += tooltipWidth * 0.15f + horizontalOffset;


            // Measure tooltip height
            float tooltipHeight = tooltipRectTransform.rect.height;
            float screenHeight = Screen.height;

            // If tooltip would go off-screen at bottom, flip it up above the mouse
            if (screenPosition.y - tooltipHeight < verticalMargin)
            {
                screenPosition.y += tooltipHeight; // render upward instead
            }

            // Apply final position directly in screen space
            tooltipRectTransform.position = screenPosition;
        }
    }
}
