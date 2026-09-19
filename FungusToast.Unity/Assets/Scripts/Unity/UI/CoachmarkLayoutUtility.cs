using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.UI.Onboarding;

namespace FungusToast.Unity.UI
{
    internal static class CoachmarkLayoutUtility
    {
        internal static readonly Vector2 DefaultScreenPadding = new Vector2(8f, 8f);

        // Dismiss control shared by every onboarding coachmark. Sized up 30% from the
        // original 34px square so the X is an easy click target.
        internal const float CloseButtonSize = 44f;
        internal const float CloseButtonFontSize = 26f;
        internal const float CloseButtonInset = 8f;
        // Title text stops short of the close button; body text starts below it.
        internal const float TitleRightInset = CloseButtonInset + CloseButtonSize + 10f;
        internal const float BodyTopInset = CloseButtonInset + CloseButtonSize + 2f;

        internal static void PlayAttention(RectTransform coachmarkRect)
        {
            if (coachmarkRect == null)
            {
                return;
            }

            var effect = coachmarkRect.GetComponent<CoachmarkAttentionEffect>();
            if (effect == null)
            {
                effect = coachmarkRect.gameObject.AddComponent<CoachmarkAttentionEffect>();
            }

            effect.Play();
        }

        internal static void PrepareAttentionEntrance(RectTransform coachmarkRect)
        {
            if (coachmarkRect == null)
            {
                return;
            }

            var effect = coachmarkRect.GetComponent<CoachmarkAttentionEffect>();
            if (effect == null)
            {
                effect = coachmarkRect.gameObject.AddComponent<CoachmarkAttentionEffect>();
            }

            effect.PrepareEntrance();
        }

        /// <summary>
        /// The pieces of a runtime-built coachmark card that a host needs to drive it.
        /// </summary>
        internal sealed class CoachmarkCard
        {
            public RectTransform Root;
            public CanvasGroup CanvasGroup;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Body;
            public Button CloseButton;

            public bool IsVisible => Root != null && CanvasGroup != null && Root.gameObject.activeSelf && CanvasGroup.alpha > 0f;

            public void Show(NewPlayerTooltipDefinition definition)
            {
                if (definition == null || Root == null || CanvasGroup == null)
                {
                    return;
                }

                Title.text = definition.Title;
                Body.text = definition.Body;
                PrepareAttentionEntrance(Root);
                Root.gameObject.SetActive(true);
                Root.SetAsLastSibling();
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
                PlayAttention(Root);
            }

            public void HideImmediate()
            {
                if (CanvasGroup != null)
                {
                    CanvasGroup.alpha = 0f;
                    CanvasGroup.blocksRaycasts = false;
                    CanvasGroup.interactable = false;
                }

                if (Root != null)
                {
                    Root.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Builds the standard onboarding coachmark card (tinted panel, bold title, wrapped body,
        /// X close button) under <paramref name="parent"/>, hidden. The card pivot is top-left so
        /// <see cref="TryPlaceAtWorldPoint"/> can drop it below an anchor control.
        /// </summary>
        internal static CoachmarkCard BuildCard(
            string name,
            Transform parent,
            Vector2 size,
            Action onDismissed,
            float titleFontSize = 22f,
            float bodyFontSize = 17f)
        {
            var card = new CoachmarkCard();

            var rootObject = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(parent, false);

            card.Root = rootObject.GetComponent<RectTransform>();
            card.Root.anchorMin = new Vector2(0.5f, 0.5f);
            card.Root.anchorMax = new Vector2(0.5f, 0.5f);
            card.Root.pivot = new Vector2(0f, 1f);
            card.Root.anchoredPosition = Vector2.zero;
            card.Root.sizeDelta = size;

            card.CanvasGroup = rootObject.GetComponent<CanvasGroup>();
            card.CanvasGroup.alpha = 0f;
            card.CanvasGroup.blocksRaycasts = false;
            card.CanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Info, 0.16f);
            backgroundColor.a = 0.98f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-TitleRightInset, -12f);

            card.Title = titleObject.GetComponent<TextMeshProUGUI>();
            card.Title.text = string.Empty;
            card.Title.color = UIStyleTokens.Text.Primary;
            card.Title.fontStyle = FontStyles.Bold;
            card.Title.fontSize = titleFontSize;
            card.Title.alignment = TextAlignmentOptions.Left;
            card.Title.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(card.Title);
            card.Title.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -BodyTopInset);

            card.Body = bodyObject.GetComponent<TextMeshProUGUI>();
            card.Body.color = UIStyleTokens.Text.Primary;
            card.Body.fontSize = bodyFontSize;
            card.Body.alignment = TextAlignmentOptions.TopLeft;
            card.Body.textWrappingMode = TextWrappingModes.Normal;
            card.Body.overflowMode = TextOverflowModes.Overflow;
            card.Body.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);
            closeRect.anchoredPosition = new Vector2(-CloseButtonInset, -CloseButtonInset);
            closeObject.GetComponent<Image>().color = UIStyleTokens.Surface.PanelElevated;

            card.CloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(card.CloseButton);
            card.CloseButton.onClick.RemoveAllListeners();
            if (onDismissed != null)
            {
                card.CloseButton.onClick.AddListener(() => onDismissed());
            }

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);
            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = CloseButtonFontSize;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                card.Title.font = TMP_Settings.defaultFontAsset;
                card.Body.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
            return card;
        }

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
                Vector2 anchorMin = coachmarkRect.anchorMin;
                Vector2 anchorMax = coachmarkRect.anchorMax;
                Vector2 pivot = coachmarkRect.pivot;
                anchor = new Vector2(
                    Mathf.Lerp(anchorMin.x, anchorMax.x, pivot.x),
                    Mathf.Lerp(anchorMin.y, anchorMax.y, pivot.y));
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

    /// <summary>
    /// Gives every onboarding coachmark the same finite entrance emphasis without
    /// drawing attention toward its dismiss control or blocking the surrounding UI.
    /// </summary>
    internal sealed class CoachmarkAttentionEffect : MonoBehaviour
    {
        private RectTransform coachmarkRect;
        private CanvasGroup canvasGroup;
        private Outline outline;
        private RectTransform backdropRect;
        private Image backdropImage;
        private Coroutine animationCoroutine;
        private Vector3 restingScale;
        private Color restingOutlineColor;
        private Vector2 restingOutlineDistance;
        private float backdropStartAlpha;
        private bool hasCapturedRestingVisuals;
        private bool isEntrancePrepared;

        internal void Play()
        {
            ResolveComponents();
            if (coachmarkRect == null || canvasGroup == null || outline == null)
            {
                return;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            if (!isEntrancePrepared)
            {
                PrepareEntrance();
            }

            EnsureBackdrop();
            animationCoroutine = StartCoroutine(PlayAttentionAnimation());
            isEntrancePrepared = false;
        }

        internal void PrepareEntrance()
        {
            ResolveComponents();
            if (coachmarkRect == null || canvasGroup == null || outline == null)
            {
                return;
            }

            RestoreCoachmarkVisuals();
            canvasGroup.alpha = 0f;
            coachmarkRect.localScale = restingScale * UIEffectConstants.CoachmarkEntranceStartScale;
            isEntrancePrepared = true;
        }

        private void ResolveComponents()
        {
            coachmarkRect ??= GetComponent<RectTransform>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            outline ??= GetComponent<Outline>();

            if (!hasCapturedRestingVisuals && coachmarkRect != null && outline != null)
            {
                restingScale = coachmarkRect.localScale;
                restingOutlineColor = outline.effectColor;
                restingOutlineDistance = outline.effectDistance * UIEffectConstants.CoachmarkBorderWidthMultiplier;
                hasCapturedRestingVisuals = true;
            }
        }

        private void EnsureBackdrop()
        {
            if (coachmarkRect == null || coachmarkRect.parent == null)
            {
                return;
            }

            if (backdropRect == null)
            {
                Transform existingBackdrop = coachmarkRect.parent.Find("CoachmarkAttentionBackdrop");
                GameObject backdropObject = existingBackdrop != null
                    ? existingBackdrop.gameObject
                    : new GameObject("CoachmarkAttentionBackdrop", typeof(RectTransform), typeof(Image));

                if (existingBackdrop == null)
                {
                    backdropObject.layer = gameObject.layer;
                    backdropObject.transform.SetParent(coachmarkRect.parent, false);
                }

                backdropRect = backdropObject.GetComponent<RectTransform>();
                backdropRect.anchorMin = Vector2.zero;
                backdropRect.anchorMax = Vector2.one;
                backdropRect.offsetMin = Vector2.zero;
                backdropRect.offsetMax = Vector2.zero;

                backdropImage = backdropObject.GetComponent<Image>();
                backdropImage.raycastTarget = false;
            }

            int firstCoachmarkSiblingIndex = coachmarkRect.GetSiblingIndex();
            for (int childIndex = 0; childIndex < coachmarkRect.parent.childCount; childIndex++)
            {
                Transform sibling = coachmarkRect.parent.GetChild(childIndex);
                var siblingEffect = sibling.GetComponent<CoachmarkAttentionEffect>();
                if (siblingEffect != null && siblingEffect.isActiveAndEnabled)
                {
                    firstCoachmarkSiblingIndex = Mathf.Min(firstCoachmarkSiblingIndex, sibling.GetSiblingIndex());
                }
            }

            backdropStartAlpha = backdropRect.gameObject.activeSelf && backdropImage != null
                ? backdropImage.color.a
                : 0f;
            backdropRect.SetSiblingIndex(firstCoachmarkSiblingIndex);
            backdropRect.gameObject.SetActive(true);
            SetBackdropAlpha(backdropStartAlpha);
        }

        private IEnumerator PlayAttentionAnimation()
        {
            float entranceDuration = Mathf.Max(0.01f, UIEffectConstants.CoachmarkEntranceDurationSeconds);
            float entranceElapsed = 0f;
            Vector3 entranceScale = restingScale * UIEffectConstants.CoachmarkEntranceStartScale;

            canvasGroup.alpha = 0f;
            coachmarkRect.localScale = entranceScale;

            yield return new WaitForSecondsRealtime(UIEffectConstants.CoachmarkEntranceDelaySeconds);

            while (entranceElapsed < entranceDuration)
            {
                float progress = Mathf.Clamp01(entranceElapsed / entranceDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                canvasGroup.alpha = eased;
                coachmarkRect.localScale = Vector3.LerpUnclamped(entranceScale, restingScale, eased);
                SetBackdropAlpha(Mathf.Lerp(backdropStartAlpha, UIEffectConstants.CoachmarkBackdropAlpha, eased));
                yield return null;
                entranceElapsed += Time.unscaledDeltaTime;
            }

            canvasGroup.alpha = 1f;
            coachmarkRect.localScale = restingScale;
            SetBackdropAlpha(UIEffectConstants.CoachmarkBackdropAlpha);

            float pulseDuration = Mathf.Max(0.01f, UIEffectConstants.CoachmarkBorderPulseDurationSeconds);
            for (int pulseIndex = 0; pulseIndex < UIEffectConstants.CoachmarkBorderPulseCount; pulseIndex++)
            {
                float pulseElapsed = 0f;
                while (pulseElapsed < pulseDuration)
                {
                    pulseElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(pulseElapsed / pulseDuration);
                    float strength = Mathf.Sin(progress * Mathf.PI);
                    ApplyOutlinePulse(strength);
                    yield return null;
                }
            }

            RestoreCoachmarkVisuals();
            SetBackdropAlpha(UIEffectConstants.CoachmarkBackdropAlpha);
            animationCoroutine = null;
        }

        private void ApplyOutlinePulse(float strength)
        {
            Color pulseColor = restingOutlineColor;
            pulseColor.a = Mathf.Lerp(restingOutlineColor.a, 1f, strength);
            outline.effectColor = pulseColor;

            float xSign = Mathf.Approximately(restingOutlineDistance.x, 0f) ? 1f : Mathf.Sign(restingOutlineDistance.x);
            float ySign = Mathf.Approximately(restingOutlineDistance.y, 0f) ? -1f : Mathf.Sign(restingOutlineDistance.y);
            float restingMagnitude = Mathf.Max(Mathf.Abs(restingOutlineDistance.x), Mathf.Abs(restingOutlineDistance.y));
            float distance = Mathf.Lerp(restingMagnitude, UIEffectConstants.CoachmarkBorderPulsePeakDistance, strength);
            outline.effectDistance = new Vector2(xSign * distance, ySign * distance);
        }

        private void SetBackdropAlpha(float alpha)
        {
            if (backdropImage == null)
            {
                return;
            }

            Color backdropColor = UIStyleTokens.Surface.OverlayDim;
            backdropColor.a = Mathf.Clamp01(alpha);
            backdropImage.color = backdropColor;
        }

        private void RestoreCoachmarkVisuals()
        {
            if (coachmarkRect != null)
            {
                coachmarkRect.localScale = restingScale;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (outline != null)
            {
                outline.effectColor = restingOutlineColor;
                outline.effectDistance = restingOutlineDistance;
            }
        }

        private void OnDisable()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            RestoreCoachmarkVisuals();
            isEntrancePrepared = false;
            if (backdropRect != null && !HasOtherVisibleCoachmark())
            {
                backdropRect.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (backdropRect != null && !HasOtherVisibleCoachmark())
            {
                backdropRect.gameObject.SetActive(false);
            }
        }

        private bool HasOtherVisibleCoachmark()
        {
            if (coachmarkRect == null || coachmarkRect.parent == null)
            {
                return false;
            }

            for (int childIndex = 0; childIndex < coachmarkRect.parent.childCount; childIndex++)
            {
                var siblingEffect = coachmarkRect.parent.GetChild(childIndex).GetComponent<CoachmarkAttentionEffect>();
                if (siblingEffect != null && siblingEffect != this && siblingEffect.isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
