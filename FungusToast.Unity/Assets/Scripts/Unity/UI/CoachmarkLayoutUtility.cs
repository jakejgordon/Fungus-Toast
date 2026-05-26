using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    internal static class CoachmarkLayoutUtility
    {
        internal static readonly Vector2 DefaultScreenPadding = new Vector2(8f, 8f);

        internal static bool TryPlaceAtWorldPoint(
            RectTransform coachmarkRect,
            RectTransform boundsRect,
            Canvas canvas,
            Vector3 worldPoint,
            Vector2 offset,
            Vector2 padding)
        {
            if (coachmarkRect == null || boundsRect == null || canvas == null)
            {
                return false;
            }

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPoint);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boundsRect, screenPoint, uiCamera, out Vector2 localPoint))
            {
                return false;
            }

            Vector2 desiredAnchoredPosition = LocalPointToAnchoredPosition(coachmarkRect, boundsRect, localPoint) + offset;
            SetAnchoredPositionClamped(coachmarkRect, boundsRect, desiredAnchoredPosition, padding);
            return true;
        }

        internal static void SetAnchoredPositionClamped(
            RectTransform coachmarkRect,
            RectTransform boundsRect,
            Vector2 desiredAnchoredPosition,
            Vector2 padding)
        {
            if (coachmarkRect == null || boundsRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(coachmarkRect);
            coachmarkRect.anchoredPosition = ClampAnchoredPosition(coachmarkRect, boundsRect, desiredAnchoredPosition, padding);
        }

        private static Vector2 LocalPointToAnchoredPosition(RectTransform coachmarkRect, RectTransform boundsRect, Vector2 localPoint)
        {
            return localPoint - GetAnchorReference(coachmarkRect, boundsRect);
        }

        private static Vector2 ClampAnchoredPosition(
            RectTransform coachmarkRect,
            RectTransform boundsRect,
            Vector2 desiredAnchoredPosition,
            Vector2 padding)
        {
            Rect bounds = boundsRect.rect;
            Vector2 safePadding = new Vector2(Mathf.Max(0f, padding.x), Mathf.Max(0f, padding.y));
            Vector2 size = coachmarkRect.rect.size;
            Vector2 pivot = coachmarkRect.pivot;
            Vector2 anchorReference = GetAnchorReference(coachmarkRect, boundsRect);

            float desiredPivotX = anchorReference.x + desiredAnchoredPosition.x;
            float desiredPivotY = anchorReference.y + desiredAnchoredPosition.y;

            float minPivotX = bounds.xMin + safePadding.x + (pivot.x * size.x);
            float maxPivotX = bounds.xMax - safePadding.x - ((1f - pivot.x) * size.x);
            float minPivotY = bounds.yMin + safePadding.y + (pivot.y * size.y);
            float maxPivotY = bounds.yMax - safePadding.y - ((1f - pivot.y) * size.y);

            float clampedPivotX = ClampOrCenter(desiredPivotX, minPivotX, maxPivotX);
            float clampedPivotY = ClampOrCenter(desiredPivotY, minPivotY, maxPivotY);

            return new Vector2(clampedPivotX - anchorReference.x, clampedPivotY - anchorReference.y);
        }

        private static Vector2 GetAnchorReference(RectTransform coachmarkRect, RectTransform boundsRect)
        {
            Rect bounds = boundsRect.rect;
            Vector2 anchor = coachmarkRect.anchorMin;
            if ((coachmarkRect.anchorMax - coachmarkRect.anchorMin).sqrMagnitude > 0.0001f)
            {
                anchor = Vector2.Lerp(coachmarkRect.anchorMin, coachmarkRect.anchorMax, coachmarkRect.pivot);
            }

            return new Vector2(
                bounds.xMin + (bounds.width * anchor.x),
                bounds.yMin + (bounds.height * anchor.y));
        }

        private static float ClampOrCenter(float value, float min, float max)
        {
            return max >= min ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
        }
    }
}