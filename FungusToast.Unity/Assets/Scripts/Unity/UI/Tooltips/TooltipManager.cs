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
            Transform parent = canvasRect != null ? canvasRect : transform;
            var go = Instantiate(tooltipPrefab, parent, false);
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
            view.PrepareForLayout();
            string text = currentRequest.ResolveText();
            view.SetText(text, currentRequest.MaxWidth);
            Position(view.RectTransform, currentRequest);
            view.ShowImmediate();
        }

        private void Position(RectTransform tooltipRect, TooltipRequest request)
        {
            if (canvasRect == null) return;

            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            // ── Force layout rebuild to get correct tooltip size ──
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            // ── Tooltip screen-space size from world corners (accurate regardless of canvas setup) ──
            Vector3[] ttWc = new Vector3[4];
            tooltipRect.GetWorldCorners(ttWc);
            Vector2 ttS0 = RectTransformUtility.WorldToScreenPoint(cam, ttWc[0]); // BL
            Vector2 ttS2 = RectTransformUtility.WorldToScreenPoint(cam, ttWc[2]); // TR
            float ttW = Mathf.Abs(ttS2.x - ttS0.x);
            float ttH = Mathf.Abs(ttS2.y - ttS0.y);

            // ── Screen safe bounds (in screen pixels) ──
            float sL = screenPadding.x;
            float sR = Screen.width - screenPadding.x;
            float sB = screenPadding.y;
            float sT = Screen.height - screenPadding.y;

            // ── Helpers (all work in screen pixel space) ──

            // Clamp a screen-space tooltip pivot position so the tooltip stays fully on-screen
            Vector2 ClampScreen(Vector2 pos, Vector2 piv)
            {
                float minX = sL + piv.x * ttW;
                float maxX = sR - (1f - piv.x) * ttW;
                float minY = sB + piv.y * ttH;
                float maxY = sT - (1f - piv.y) * ttH;
                return new Vector2(
                    maxX >= minX ? Mathf.Clamp(pos.x, minX, maxX) : (minX + maxX) * 0.5f,
                    maxY >= minY ? Mathf.Clamp(pos.y, minY, maxY) : (minY + maxY) * 0.5f);
            }

            // Overlap area between tooltip (at screen pos with pivot) and a screen-space rect
            float Overlap(Vector2 pos, Vector2 piv, float rL, float rR, float rB, float rT)
            {
                float tL = pos.x - piv.x * ttW;
                float tR = tL + ttW;
                float tB = pos.y - piv.y * ttH;
                float tT = tB + ttH;
                return Mathf.Max(0f, Mathf.Min(tR, rR) - Mathf.Max(tL, rL))
                     * Mathf.Max(0f, Mathf.Min(tT, rT) - Mathf.Max(tB, rB));
            }

            // Place the tooltip at a screen-pixel position with a given pivot
            void PlaceAtScreen(Vector2 screenPos, Vector2 piv)
            {
                tooltipRect.pivot = piv;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        canvasRect, screenPos, cam, out Vector3 worldPos))
                {
                    tooltipRect.position = worldPos;
                }
            }

            // Screen position from a world-space point
            Vector2 WTS(Vector3 worldPt) => RectTransformUtility.WorldToScreenPoint(cam, worldPt);

            // ── Anchor world corners ──
            Vector3[] wc = null;
            if (request.Anchor != null)
            {
                wc = new Vector3[4];
                request.Anchor.GetWorldCorners(wc); // 0=BL, 1=TL, 2=TR, 3=BR
            }

            // ══════════════════════════════════════════════
            //  AUTO PLACEMENT — all math in screen pixels
            //  Tries 6 positions, picks first with zero overlap.
            //  Priority: right of node → left → below → above
            // ══════════════════════════════════════════════
            if (request.Placement == TooltipPlacement.Auto)
            {
                if (wc != null)
                {
                    // Node screen bounds
                    Vector2 ns0 = WTS(wc[0]), ns1 = WTS(wc[1]), ns2 = WTS(wc[2]), ns3 = WTS(wc[3]);
                    float nL = Mathf.Min(ns0.x, ns1.x, ns2.x, ns3.x);
                    float nR = Mathf.Max(ns0.x, ns1.x, ns2.x, ns3.x);
                    float nB = Mathf.Min(ns0.y, ns1.y, ns2.y, ns3.y);
                    float nT = Mathf.Max(ns0.y, ns1.y, ns2.y, ns3.y);

                    const float gap = 12f;

                    // Each candidate: (pivot of tooltip, screen position of that pivot)
                    var candidates = new (Vector2 piv, Vector2 pos)[]
                    {
                        // Right of node, top-aligned:  tooltip left edge at nR+gap, top at nT
                        (new Vector2(0f, 1f), new Vector2(nR + gap, nT)),
                        // Right of node, bottom-aligned
                        (new Vector2(0f, 0f), new Vector2(nR + gap, nB)),
                        // Left of node, top-aligned:  tooltip right edge at nL-gap, top at nT
                        (new Vector2(1f, 1f), new Vector2(nL - gap, nT)),
                        // Left of node, bottom-aligned
                        (new Vector2(1f, 0f), new Vector2(nL - gap, nB)),
                        // Below node, left-aligned
                        (new Vector2(0f, 1f), new Vector2(nL, nB - gap)),
                        // Above node, left-aligned
                        (new Vector2(0f, 0f), new Vector2(nL, nT + gap)),
                    };

                    int bestIdx = 0;
                    float bestOv = float.MaxValue;

                    for (int i = 0; i < candidates.Length; i++)
                    {
                        var (cpiv, craw) = candidates[i];
                        Vector2 clamped = ClampScreen(craw, cpiv);
                        float ov = Overlap(clamped, cpiv, nL, nR, nB, nT);
                        if (ov < bestOv)
                        {
                            bestOv = ov;
                            bestIdx = i;
                            if (ov <= 0f) break;
                        }
                    }

                    var (fPiv, fRaw) = candidates[bestIdx];
                    Vector2 finalPos = ClampScreen(fRaw, fPiv);
                    finalPos.x += request.AutoPlacementOffsetX;
                    finalPos = ClampScreen(finalPos, fPiv);
                    PlaceAtScreen(finalPos, fPiv);
                    return;
                }

                // No anchor — follow mouse
                Vector2 mp = (Vector2)Input.mousePosition + new Vector2(16f, -16f);
                Vector2 mPiv = new Vector2(0f, 1f);
                PlaceAtScreen(ClampScreen(mp, mPiv), mPiv);
                return;
            }

            // ══════════════════════════════════════════════
            //  EXPLICIT PLACEMENT  (N / NE / E / SE / S / SW / W / NW)
            // ══════════════════════════════════════════════
            Vector2 targetScreen = wc != null ? WTS(wc[2]) : (Vector2)Input.mousePosition;
            Vector3 MidOf(int a, int b) => (wc[a] + wc[b]) * 0.5f;

            Vector2 pivot = new Vector2(0f, 1f);
            Vector2 appliedOffset = offset;

            if (wc != null)
            {
                switch (request.Placement)
                {
                    case TooltipPlacement.N:
                        pivot = new Vector2(0.5f, 0f);
                        targetScreen = WTS(MidOf(1, 2));
                        appliedOffset = new Vector2(0f, Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.NE:
                        pivot = new Vector2(0f, 0f);
                        targetScreen = WTS(wc[2]);
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.E:
                        pivot = new Vector2(0f, 0.5f);
                        targetScreen = WTS(MidOf(2, 3));
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), 0f);
                        break;
                    case TooltipPlacement.SE:
                        pivot = new Vector2(0f, 1f);
                        targetScreen = WTS(wc[3]);
                        appliedOffset = new Vector2(Mathf.Abs(offset.x), -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.S:
                        pivot = new Vector2(0.5f, 1f);
                        targetScreen = WTS(MidOf(0, 3));
                        appliedOffset = new Vector2(0f, -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.SW:
                        pivot = new Vector2(1f, 1f);
                        targetScreen = WTS(wc[0]);
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), -Mathf.Abs(offset.y));
                        break;
                    case TooltipPlacement.W:
                        pivot = new Vector2(1f, 0.5f);
                        targetScreen = WTS(MidOf(0, 1));
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), 0f);
                        break;
                    case TooltipPlacement.NW:
                        pivot = new Vector2(1f, 0f);
                        targetScreen = WTS(wc[1]);
                        appliedOffset = new Vector2(-Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                        break;
                }
            }

            PlaceAtScreen(ClampScreen(targetScreen + appliedOffset, pivot), pivot);
        }
    }
}
