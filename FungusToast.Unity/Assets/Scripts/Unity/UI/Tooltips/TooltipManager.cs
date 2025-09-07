using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Singleton managing a single tooltip instance for the session.
    /// Attach to a GameObject under the primary Canvas and assign the tooltipPrefab.
    /// </summary>
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [Header("Config")] public GameObject tooltipPrefab;
        [Range(0f,1f)] public float showDelay = 0.38f;
        public Vector2 offset = new Vector2(12f, -8f);
        public Vector2 screenPadding = new Vector2(12f, 12f);

        private TooltipView view;
        private Canvas rootCanvas;
        private RectTransform canvasRect;

        private TooltipTrigger currentSource;
        private TooltipRequest currentRequest;
        private float requestTime;
        private bool pendingShow;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                canvasRect = rootCanvas.transform as RectTransform;
        }

        private void EnsureInstance()
        {
            if (view != null) return;
            if (tooltipPrefab == null)
            {
                Debug.LogWarning("TooltipManager missing tooltipPrefab");
                return;
            }
            var go = Instantiate(tooltipPrefab, transform);
            view = go.GetComponent<TooltipView>();
            go.SetActive(false);
        }

        public void ShowAfterDelay(TooltipTrigger source, TooltipRequest request)
        {
            EnsureInstance();
            currentSource = source;
            currentRequest = request;
            requestTime = Time.unscaledTime;
            pendingShow = true;
        }

        public void Cancel(TooltipTrigger source)
        {
            if (currentSource == source)
            {
                pendingShow = false;
                if (view != null)
                    view.HideImmediate();
                currentSource = null;
            }
        }

        /// <summary>
        /// Hides any visible or pending tooltip regardless of the source. Use when switching panels/phases.
        /// </summary>
        public void CancelAll()
        {
            pendingShow = false;
            if (view != null)
                view.HideImmediate();
            currentSource = null;
        }

        private void Update()
        {
            if (pendingShow && Time.unscaledTime - requestTime >= showDelay)
            {
                pendingShow = false;
                InternalShow();
            }
        }

        private void InternalShow()
        {
            if (view == null) return;
            string text = currentRequest.ResolveText();
            view.SetText(text, currentRequest.MaxWidth);
            Position(view.RectTransform, currentRequest);
            view.ShowImmediate();
        }

        private void Position(RectTransform tooltipRect, TooltipRequest request)
        {
            if (canvasRect == null) return;

            Vector2 ToLocal(Vector2 screenPt)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPt,
                    rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
                    out var localPoint);
                return localPoint;
            }

            // Resolve anchor screen position (default top-right) and its corners
            Vector2 targetScreenPos;
            Vector3[] wc = null;
            if (request.Anchor != null)
            {
                wc = new Vector3[4];
                request.Anchor.GetWorldCorners(wc); // 0=BL,1=TL,2=TR,3=BR
                targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[2]);
            }
            else
            {
                targetScreenPos = (Vector2)Input.mousePosition;
            }

            // Compute edge midpoints if we have corners
            Vector3 midTop = wc != null ? (wc[1] + wc[2]) * 0.5f : (Vector3)targetScreenPos;
            Vector3 midBottom = wc != null ? (wc[0] + wc[3]) * 0.5f : (Vector3)targetScreenPos;
            Vector3 midLeft = wc != null ? (wc[0] + wc[1]) * 0.5f : (Vector3)targetScreenPos;
            Vector3 midRight = wc != null ? (wc[2] + wc[3]) * 0.5f : (Vector3)targetScreenPos;

            // Choose pivot/corner based on placement (explicit) or default NE-like
            Vector2 pivot = new Vector2(0f, 1f); // top-left by default
            Vector2 appliedOffset = offset;
            if (request.Placement != TooltipPlacement.Auto && wc != null)
            {
                switch (request.Placement)
                {
                    case TooltipPlacement.N:
                        pivot = new Vector2(0.5f, 0f); // bottom-center touches top edge
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, midTop);
                        appliedOffset = new Vector2(0f, Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.NE:
                        pivot = new Vector2(0f, 0f); // bottom-left touches top-right
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[2]);
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.E:
                        pivot = new Vector2(0f, 0.5f); // left-center touches right edge mid
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, midRight);
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), 0f);
                        break;
                    case TooltipPlacement.SE:
                        pivot = new Vector2(0f, 1f); // top-left touches bottom-right
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[3]);
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.S:
                        pivot = new Vector2(0.5f, 1f); // top-center touches bottom edge
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, midBottom);
                        appliedOffset = new Vector2(0f, -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.SW:
                        pivot = new Vector2(1f, 1f); // top-right touches bottom-left
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[0]);
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.W:
                        pivot = new Vector2(1f, 0.5f); // right-center touches left edge mid
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, midLeft);
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), 0f);
                        break;
                    case TooltipPlacement.NW:
                        pivot = new Vector2(1f, 0f); // bottom-right touches top-left
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[1]);
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                        break;
                }
            }
            else if (wc != null)
            {
                // Default: NE-like (top-right anchor, bottom-left pivot)
                pivot = new Vector2(0f, 0f);
                targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[2]);
                appliedOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            }

            // Place once with chosen pivot
            tooltipRect.pivot = pivot;
            Vector2 screenWithOffset = new Vector2(targetScreenPos.x + appliedOffset.x, targetScreenPos.y + appliedOffset.y);
            Vector2 localPoint = ToLocal(screenWithOffset);
            tooltipRect.anchoredPosition = localPoint;

            // Build and measure
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            var size = tooltipRect.sizeDelta;
            var canvasSize = canvasRect.rect.size;
            var pos = tooltipRect.anchoredPosition;

            // Auto-flip only when placement is Auto
            if (request.Placement == TooltipPlacement.Auto)
            {
                bool overflowRight = pos.x + size.x + screenPadding.x > canvasSize.x * 0.5f;
                bool overflowLeft = pos.x < -canvasSize.x * 0.5f + screenPadding.x;
                bool overflowTop = pos.y > canvasSize.y * 0.5f - screenPadding.y;
                bool overflowBottom = pos.y - size.y - screenPadding.y < -canvasSize.y * 0.5f;

                if (overflowRight)
                {
                    pivot.x = 1f; appliedOffset.x = -Mathf.Abs(offset.x);
                    if (wc != null) targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[1]);
                }
                else if (overflowLeft)
                {
                    pivot.x = 0f; appliedOffset.x = Mathf.Abs(offset.x);
                    if (wc != null) targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, wc[2]);
                }
                if (overflowTop)
                {
                    pivot.y = 0f; appliedOffset.y = Mathf.Abs(offset.y);
                    if (wc != null)
                    {
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, pivot.x < 0.5f ? wc[3] : wc[0]);
                    }
                }
                else if (overflowBottom)
                {
                    pivot.y = 1f; appliedOffset.y = -Mathf.Abs(offset.y);
                    if (wc != null)
                    {
                        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, pivot.x < 0.5f ? wc[1] : wc[2]);
                    }
                }

                screenWithOffset = new Vector2(targetScreenPos.x + appliedOffset.x, targetScreenPos.y + appliedOffset.y);
                localPoint = ToLocal(screenWithOffset);
                tooltipRect.pivot = pivot;
                tooltipRect.anchoredPosition = localPoint;

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
                size = tooltipRect.sizeDelta;
                pos = tooltipRect.anchoredPosition;
            }

            // Final clamping to keep fully inside canvas
            if (pos.x + size.x + screenPadding.x > canvasSize.x * 0.5f)
                pos.x = canvasSize.x * 0.5f - size.x - screenPadding.x;
            if (pos.y - size.y - screenPadding.y < -canvasSize.y * 0.5f)
                pos.y = -canvasSize.y * 0.5f + size.y + screenPadding.y;
            if (pos.x < -canvasSize.x * 0.5f + screenPadding.x)
                pos.x = -canvasSize.x * 0.5f + screenPadding.x;
            if (pos.y > canvasSize.y * 0.5f - screenPadding.y)
                pos.y = canvasSize.y * 0.5f - screenPadding.y;

            // Enforce directional constraints for explicit placements (prevent clamping from crossing anchor)
            if (request.Placement != TooltipPlacement.Auto && wc != null)
            {
                // local position of chosen anchor point (without offset)
                Vector2 anchorLocal = ToLocal(RectTransformUtility.WorldToScreenPoint(null, targetScreenPos));
                switch (request.Placement)
                {
                    case TooltipPlacement.S:
                    case TooltipPlacement.SE:
                    case TooltipPlacement.SW:
                        // keep tooltip top at or below anchor bottom
                        pos.y = Mathf.Min(pos.y, anchorLocal.y);
                        break;
                    case TooltipPlacement.N:
                    case TooltipPlacement.NE:
                    case TooltipPlacement.NW:
                        // keep tooltip bottom at or above anchor top
                        pos.y = Mathf.Max(pos.y, anchorLocal.y);
                        break;
                }
                switch (request.Placement)
                {
                    case TooltipPlacement.W:
                    case TooltipPlacement.NW:
                    case TooltipPlacement.SW:
                        // keep tooltip right at or left of anchor left
                        pos.x = Mathf.Min(pos.x, anchorLocal.x);
                        break;
                    case TooltipPlacement.E:
                    case TooltipPlacement.NE:
                    case TooltipPlacement.SE:
                        // keep tooltip left at or right of anchor right
                        pos.x = Mathf.Max(pos.x, anchorLocal.x);
                        break;
                }
            }

            tooltipRect.anchoredPosition = pos;
        }
    }
}
