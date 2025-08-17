using UnityEngine;

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
            Vector2 targetScreenPos;
            if (request.Anchor != null)
            {
                var worldCorners = new Vector3[4];
                request.Anchor.GetWorldCorners(worldCorners);
                // top right by default
                targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]);
            }
            else
            {
                targetScreenPos = (Vector2)Input.mousePosition;
            }

            // apply configured offset safely
            targetScreenPos = new Vector2(targetScreenPos.x + offset.x, targetScreenPos.y + offset.y);

            // Convert to local canvas space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera, out var localPoint);
            tooltipRect.pivot = new Vector2(0f,1f);
            tooltipRect.anchoredPosition = localPoint;

            Canvas.ForceUpdateCanvases();
            var size = tooltipRect.sizeDelta;
            // Clamp inside canvas
            var canvasSize = canvasRect.rect.size;
            var pos = tooltipRect.anchoredPosition;

            if (pos.x + size.x + screenPadding.x > canvasSize.x * 0.5f)
                pos.x = canvasSize.x * 0.5f - size.x - screenPadding.x;
            if (pos.y - size.y - screenPadding.y < -canvasSize.y * 0.5f)
                pos.y = -canvasSize.y * 0.5f + size.y + screenPadding.y;
            if (pos.x < -canvasSize.x * 0.5f + screenPadding.x)
                pos.x = -canvasSize.x * 0.5f + screenPadding.x;
            if (pos.y > canvasSize.y * 0.5f - screenPadding.y)
                pos.y = canvasSize.y * 0.5f - screenPadding.y;

            tooltipRect.anchoredPosition = pos;
        }
    }
}
