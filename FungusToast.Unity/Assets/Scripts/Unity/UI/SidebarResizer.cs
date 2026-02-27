using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Keeps a sidebar's width at a fixed fraction of the canvas reference width.
    /// Recalculates automatically whenever the RectTransform dimensions change
    /// (e.g. window resize, resolution switch, display change).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SidebarResizer : MonoBehaviour
    {
        [Tooltip("Fraction of the total screen width to use for the sidebar (e.g., 0.2 = 20%)")]
        [Range(0.1f, 0.5f)]
        public float sidebarWidthFraction = 0.2f; // 20% of the screen width

        private RectTransform rectTransform = null!;
        private Canvas? rootCanvas;
        private float lastAppliedWidth;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            ResizeSidebar();
        }

        /// <summary>
        /// Called by Unity whenever this RectTransform (or an ancestor) changes dimensions.
        /// Handles window resizes, resolution changes, and display switches automatically.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            // Guard: skip if not yet initialized (can fire before Awake)
            if (rectTransform == null) return;

            ResizeSidebar();
        }

        private void ResizeSidebar()
        {
            // Use CanvasScaler-aware width when possible so the calculation
            // is correct under UI scaling; fall back to raw Screen.width.
            float referenceWidth = GetCanvasWidth();
            float newWidth = referenceWidth * sidebarWidthFraction;

            // Avoid redundant layout rebuilds when the width hasn't meaningfully changed
            if (Mathf.Approximately(newWidth, lastAppliedWidth)) return;

            lastAppliedWidth = newWidth;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

        /// <summary>
        /// Returns the effective canvas width in reference-resolution pixels.
        /// If a CanvasScaler is present with Scale With Screen Size mode,
        /// this returns Screen.width / scaleFactor so the sidebar fraction
        /// aligns with the scaler's reference resolution rather than raw pixels.
        /// </summary>
        private float GetCanvasWidth()
        {
            if (rootCanvas != null && rootCanvas.scaleFactor > 0f)
            {
                return Screen.width / rootCanvas.scaleFactor;
            }

            return Screen.width;
        }
    }
}
