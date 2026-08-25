using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Spawns floating "+N Points!" popups next to the Spend Points button whenever the
    /// human player earns bonus mutation points beyond the base per-round income.
    /// Multiple bonuses in quick succession stack vertically rather than overwriting each other.
    /// </summary>
    public class UI_MutationPointBonusPopupPresenter : MonoBehaviour
    {
        private const float PopInDuration = 0.21f;
        private const float HoldDuration = 0.75f;
        private const float FadeOutDuration = 0.525f;
        private const float RiseDistance = 60f;
        private const float FadeRiseDistance = 18f;
        private const float StackSpacing = 34f;
        private const float HorizontalOffset = 14f;
        private const float HorizontalJitter = 6f;

        private UI_MutationManager mutationManager = null!;
        private AudioClip? bonusPopClip;
        private float bonusPopVolume = 1f;

        private RectTransform? canvasRoot;
        private Canvas? hostCanvas;
        private AudioSource? audioSource;
        private int activeSlotCount;

        public void Initialize(UI_MutationManager manager, AudioClip? sfxClip, float sfxVolume)
        {
            mutationManager = manager;
            bonusPopClip = sfxClip;
            bonusPopVolume = sfxVolume;
        }

        public void ResetForGameTransition()
        {
            StopAllCoroutines();
            activeSlotCount = 0;
        }

        public void ShowBonus(int points)
        {
            if (points <= 0 || mutationManager == null)
            {
                return;
            }

            var anchor = mutationManager.SpendPointsButtonRectTransform;
            if (anchor == null)
            {
                return;
            }

            if (!EnsureCanvasRoot(anchor))
            {
                return;
            }

            PlayPopSound();
            StartCoroutine(RunPopup(points, anchor));
        }

        private bool EnsureCanvasRoot(RectTransform anchor)
        {
            if (canvasRoot != null)
            {
                return true;
            }

            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            hostCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            canvasRoot = hostCanvas.transform as RectTransform;
            return canvasRoot != null;
        }

        private Vector2 ComputeSpawnAnchoredPosition(RectTransform anchor)
        {
            Vector3 worldPos = anchor.TransformPoint(new Vector3(anchor.rect.xMax, 0f, 0f));
            Camera? cam = hostCanvas!.renderMode == RenderMode.ScreenSpaceOverlay ? null : hostCanvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPoint, cam, out Vector2 localPoint);
            return localPoint + new Vector2(HorizontalOffset, 0f);
        }

        private IEnumerator RunPopup(int points, RectTransform anchor)
        {
            int slot = activeSlotCount++;

            RectTransform popupRect = CreatePopupInstance(points);
            CanvasGroup canvasGroup = popupRect.GetComponent<CanvasGroup>();

            float jitterX = Random.Range(-HorizontalJitter, HorizontalJitter);
            // The button marks the midpoint of the total rise, so the popup starts below
            // it, passes through it, and finishes the same distance above it.
            float halfTotalRise = (RiseDistance + FadeRiseDistance) / 2f;
            Vector2 basePos = ComputeSpawnAnchoredPosition(anchor) + new Vector2(jitterX, slot * StackSpacing - halfTotalRise);
            Vector2 risenPos = basePos + new Vector2(0f, RiseDistance);
            Vector2 fadeEndPos = risenPos + new Vector2(0f, FadeRiseDistance);

            popupRect.anchoredPosition = basePos;
            popupRect.localScale = Vector3.one * 0.4f;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < PopInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / PopInDuration);
                float scale = EaseOutBack(progress);
                popupRect.localScale = Vector3.one * scale;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress * 2f);
                yield return null;
            }
            popupRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            elapsed = 0f;
            while (elapsed < HoldDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / HoldDuration);
                popupRect.anchoredPosition = Vector2.Lerp(basePos, risenPos, progress);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / FadeOutDuration);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
                popupRect.anchoredPosition = Vector2.Lerp(risenPos, fadeEndPos, progress);
                yield return null;
            }

            if (popupRect != null)
            {
                Destroy(popupRect.gameObject);
            }

            activeSlotCount = Mathf.Max(0, activeSlotCount - 1);
        }

        private RectTransform CreatePopupInstance(int points)
        {
            var rootObject = new GameObject("MutationPointBonusPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            rootObject.transform.SetParent(canvasRoot, false);
            rootObject.transform.SetAsLastSibling();

            var rect = (RectTransform)rootObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 44f);

            var canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            string pointsLabel = points == 1 ? "Point" : "Points";
            var text = rootObject.GetComponent<TextMeshProUGUI>();
            text.text = $"+{points} {pointsLabel}!";
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = UIStyleTokens.State.Success;
            text.fontSize = 27f;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            var outline = rootObject.AddComponent<Outline>();
            outline.effectColor = new Color(UIStyleTokens.Accent.Spore.r, UIStyleTokens.Accent.Spore.g, UIStyleTokens.Accent.Spore.b, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            return rect;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float shifted = t - 1f;
            return 1f + c3 * (shifted * shifted * shifted) + c1 * (shifted * shifted);
        }

        private void PlayPopSound()
        {
            if (bonusPopClip == null)
            {
                return;
            }

            EnsureAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(bonusPopVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            audioSource!.PlayOneShot(bonusPopClip, effectiveVolume);
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }
}
