using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    public class TooltipPositioner : MonoBehaviour
    {
        public RectTransform tooltipRectTransform;
        public float horizontalOffset = 75f;
        public float verticalMargin = 10f;

        public void SetPosition(Vector2 screenPosition)
        {
            if (tooltipRectTransform == null)
                return;

            // Step 1: Offset to the right
            screenPosition.x += horizontalOffset;

            // Step 2: Clamp Y to screen bounds so tooltip doesn't go off top/bottom
            float tooltipHeight = tooltipRectTransform.rect.height;
            float screenHeight = Screen.height;

            float minY = verticalMargin;
            float maxY = screenHeight - tooltipHeight - verticalMargin;
            screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

            // Step 3: Set world-space screen position directly
            tooltipRectTransform.position = screenPosition;

            Debug.Log($"✅ Tooltip WORLD position set to: {screenPosition}");
        }
    }
}
