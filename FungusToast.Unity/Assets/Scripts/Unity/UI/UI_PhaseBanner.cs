using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseBanner : MonoBehaviour
    {
        private const float StandardFadeDuration = 0.5f;
        private const float CampaignIntroFadeInDuration = 0.16f;
        private const float CampaignIntroSettleDuration = 0.08f;
        private const float CampaignIntroFadeOutDuration = 0.2f;
        private const float CampaignIntroHoldDuration = 1.65f;
        private const float CampaignIntroStartYOffset = -18f;
        private const float CampaignIntroEndYOffset = 14f;
        private const float CampaignIntroStartScale = 0.94f;
        private const float CampaignIntroOvershootScale = 1.04f;
        private const float GameStartSlamDurationSeconds = 0.12f;
        private const float GameStartSurfaceNameDelaySeconds = 0.32f;
        private const float GameStartSurfaceNameStampDurationSeconds = 0.16f;
        private const float GameStartHoldDurationSeconds = 2.65f;
        private const float GameStartExitDurationSeconds = 0.18f;
        private const float GameStartTitleFontSize = 72f;
        private const float GameStartTitleCardWidth = 1400f;
        private const float GameStartTitleCardHeight = 240f;
        private const float GameStartSlamStartYOffset = 480f;
        private const float GameStartSlamOvershootScale = 2.6f;
        private const float GameStartSurfaceNameStartXOffset = 200f;
        private const float GameStartSurfaceNameStartScale = 0.1f;
        private const float GameStartSurfaceNameOvershootScale = 1.8f;

        public TextMeshProUGUI bannerText;
        public CanvasGroup canvasGroup;
        [SerializeField] private Image bannerBackground;

        private RectTransform bannerRectTransform;
        private RectTransform bannerPanelRectTransform;
        private Vector2 baseAnchoredPosition;
        private Vector3 baseScale = Vector3.one;
        private Vector2 baseBannerSize;
        private Vector2 baseBannerTextSize;
        private Vector2 baseBannerPanelSize;
        private float baseBannerTextFontSize;
        private bool isCampaignIntroPlaying;
        private string pendingBannerText;
        private float pendingBannerDuration;
        private bool hasPendingBanner;

        private void Awake()
        {
            CacheTransformDefaults();
            ApplyStyle();
        }

        private void OnEnable()
        {
            CacheTransformDefaults();
            ApplyStyle();
        }

        private void Start()
        {
            HideImmediate();
        }

        private void CacheTransformDefaults()
        {
            if (bannerRectTransform != null)
            {
                return;
            }

            bannerRectTransform = transform as RectTransform;
            if (bannerRectTransform != null)
            {
                baseAnchoredPosition = bannerRectTransform.anchoredPosition;
                baseScale = bannerRectTransform.localScale;
                baseBannerSize = bannerRectTransform.sizeDelta;
            }

            if (bannerText != null)
            {
                baseBannerTextSize = bannerText.rectTransform.sizeDelta;
                baseBannerTextFontSize = bannerText.fontSize;
                bannerPanelRectTransform = bannerText.rectTransform.parent as RectTransform;
                if (bannerPanelRectTransform != null)
                {
                    baseBannerPanelSize = bannerPanelRectTransform.sizeDelta;
                }
            }
        }

        private void ApplyStyle()
        {
            if (bannerText != null)
            {
                bannerText.color = UIStyleTokens.Text.Primary;
            }

            if (bannerBackground == null)
            {
                bannerBackground = GetComponentInChildren<Image>(true);
            }

            if (bannerBackground != null)
            {
                var bg = UIStyleTokens.Surface.PanelSecondary;
                bg.a = 0.85f;
                bannerBackground.color = bg;
            }
        }

        public void Show(string text, float duration = 2f)
        {
            if (isCampaignIntroPlaying)
            {
                pendingBannerText = text ?? string.Empty;
                pendingBannerDuration = duration;
                hasPendingBanner = true;
                return;
            }

            bannerText.text = text;
            StopAllCoroutines();
            ResetBannerTransform();
            StartCoroutine(FadeInOut(duration));
        }

        public void ShowStyledIntro(string overline, string title, float holdDuration = CampaignIntroHoldDuration)
        {
            string resolvedTitle = title?.Trim() ?? string.Empty;
            string resolvedOverline = overline?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(resolvedTitle))
            {
                Show(resolvedOverline, holdDuration);
                return;
            }

            if (string.IsNullOrWhiteSpace(resolvedOverline))
            {
                Show(resolvedTitle, holdDuration);
                return;
            }

            if (bannerText == null || canvasGroup == null)
            {
                Show($"{resolvedOverline}\n{resolvedTitle}", holdDuration);
                return;
            }

            bannerText.text = $"<size=60%>{resolvedOverline}</size>\n{resolvedTitle}";
            StopAllCoroutines();
            ClearPendingBanner();
            isCampaignIntroPlaying = true;
            StartCoroutine(PlayCampaignLevelIntro(holdDuration));
        }

        public void ShowCampaignLevelIntro(int levelDisplay, string levelTitle, float holdDuration = CampaignIntroHoldDuration)
        {
            ShowStyledIntro($"Level {Mathf.Max(1, levelDisplay)}", levelTitle, holdDuration);
        }

        public void ShowGameStartSurfaceIntro(string surfaceName)
        {
            if (bannerText == null || canvasGroup == null)
            {
                return;
            }

            string resolvedSurfaceName = surfaceName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedSurfaceName))
            {
                return;
            }

            StopAllCoroutines();
            ClearPendingBanner();
            isCampaignIntroPlaying = true;
            StartCoroutine(PlayGameStartSurfaceIntro(resolvedSurfaceName));
        }

        public void ShowPersistent(string text)
        {
            StopAllCoroutines();
            ResetBannerTransform();
            isCampaignIntroPlaying = false;
            ClearPendingBanner();

            if (bannerText != null)
            {
                bannerText.text = text;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void HideImmediate()
        {
            StopAllCoroutines();
            ResetBannerTransform();
            isCampaignIntroPlaying = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            if (bannerText != null)
            {
                bannerText.text = string.Empty;
            }
        }

        private IEnumerator FadeInOut(float duration)
        {
            canvasGroup.alpha = 0f;

            // Fade in
            for (float t = 0; t < StandardFadeDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = t / StandardFadeDuration;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(duration);

            // Fade out
            for (float t = 0; t < StandardFadeDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = 1f - (t / StandardFadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            ResetBannerTransform();
        }

        private IEnumerator PlayCampaignLevelIntro(float holdDuration)
        {
            CacheTransformDefaults();
            SetBannerVisualState(0f, CampaignIntroStartYOffset, CampaignIntroStartScale);

            for (float elapsed = 0f; elapsed < CampaignIntroFadeInDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / CampaignIntroFadeInDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                SetBannerVisualState(
                    eased,
                    Mathf.Lerp(CampaignIntroStartYOffset, 0f, eased),
                    Mathf.Lerp(CampaignIntroStartScale, CampaignIntroOvershootScale, eased));
                yield return null;
            }

            SetBannerVisualState(1f, 0f, CampaignIntroOvershootScale);

            for (float elapsed = 0f; elapsed < CampaignIntroSettleDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / CampaignIntroSettleDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                SetBannerVisualState(1f, 0f, Mathf.Lerp(CampaignIntroOvershootScale, 1f, eased));
                yield return null;
            }

            SetBannerVisualState(1f, 0f, 1f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdDuration));

            for (float elapsed = 0f; elapsed < CampaignIntroFadeOutDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / CampaignIntroFadeOutDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                SetBannerVisualState(
                    1f - eased,
                    Mathf.Lerp(0f, CampaignIntroEndYOffset, eased),
                    Mathf.Lerp(1f, 1.02f, eased));
                yield return null;
            }

            bool shouldFlushPendingBanner = hasPendingBanner;
            HideImmediate();
            if (shouldFlushPendingBanner)
            {
                FlushPendingBanner();
            }
        }

        private IEnumerator PlayGameStartSurfaceIntro(string surfaceName)
        {
            CacheTransformDefaults();
            PrepareGameStartTitleCard();
            bannerText.text = $"<b>Fungus</b> • {surfaceName}";
            int surfaceNameStartCharacterIndex = "Fungus • ".Length;
            SetSurfaceNameStampVisuals(surfaceNameStartCharacterIndex, 0f, GameStartSurfaceNameStartXOffset, GameStartSurfaceNameStartScale, 0f);
            SetBannerVisualState(1f, GameStartSlamStartYOffset, GameStartSlamOvershootScale);
            for (float elapsed = 0f; elapsed < GameStartSlamDurationSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / GameStartSlamDurationSeconds);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                SetBannerVisualState(
                    1f,
                    Mathf.Lerp(GameStartSlamStartYOffset, 0f, eased),
                    Mathf.Lerp(GameStartSlamOvershootScale, 1f, eased));
                yield return null;
            }

            SetBannerVisualState(1f, 0f, 1f);
            yield return new WaitForSecondsRealtime(GameStartSurfaceNameDelaySeconds);

            for (float elapsed = 0f; elapsed < GameStartSurfaceNameStampDurationSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / GameStartSurfaceNameStampDurationSeconds);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                float scaleProgress = progress / 0.65f;
                float scale = progress < 0.65f
                    ? Mathf.Lerp(GameStartSurfaceNameStartScale, GameStartSurfaceNameOvershootScale, 1f - Mathf.Pow(1f - scaleProgress, 3f))
                    : Mathf.Lerp(GameStartSurfaceNameOvershootScale, 1f, Mathf.SmoothStep(0f, 1f, (progress - 0.65f) / 0.35f));

                SetSurfaceNameStampVisuals(
                    surfaceNameStartCharacterIndex,
                    eased,
                    Mathf.Lerp(GameStartSurfaceNameStartXOffset, 0f, eased),
                    scale,
                    eased);
                yield return null;
            }

            SetSurfaceNameStampVisuals(surfaceNameStartCharacterIndex, 1f, 0f, 1f, 1f);
            yield return new WaitForSecondsRealtime(GameStartHoldDurationSeconds);

            for (float elapsed = 0f; elapsed < GameStartExitDurationSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / GameStartExitDurationSeconds);
                SetBannerVisualState(1f - progress, 0f, 1f);
                yield return null;
            }

            HideImmediate();
        }

        private void SetSurfaceNameStampVisuals(int firstSurfaceNameCharacterIndex, float alpha, float xOffset, float scale, float colorProgress)
        {
            bannerText.ForceMeshUpdate();
            TMP_TextInfo textInfo = bannerText.textInfo;
            Color stampColor = Color.Lerp(UIStyleTokens.Accent.Spore, UIStyleTokens.Text.Primary, colorProgress);
            Color32 stampColor32 = stampColor;
            byte stampAlpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * byte.MaxValue);

            for (int characterIndex = firstSurfaceNameCharacterIndex; characterIndex < textInfo.characterCount; characterIndex++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
                if (!characterInfo.isVisible)
                {
                    continue;
                }

                TMP_MeshInfo meshInfo = textInfo.meshInfo[characterInfo.materialReferenceIndex];
                int vertexIndex = characterInfo.vertexIndex;
                Vector3 characterCenter = (meshInfo.vertices[vertexIndex] + meshInfo.vertices[vertexIndex + 2]) * 0.5f;

                for (int vertexOffset = 0; vertexOffset < 4; vertexOffset++)
                {
                    int currentVertexIndex = vertexIndex + vertexOffset;
                    meshInfo.vertices[currentVertexIndex] = characterCenter + ((meshInfo.vertices[currentVertexIndex] - characterCenter) * scale) + Vector3.right * xOffset;
                    meshInfo.colors32[currentVertexIndex] = new Color32(stampColor32.r, stampColor32.g, stampColor32.b, stampAlpha);
                }
            }

            bannerText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void PrepareGameStartTitleCard()
        {
            if (bannerText == null)
            {
                return;
            }

            bannerText.fontSize = GameStartTitleFontSize;
            bannerText.rectTransform.sizeDelta = new Vector2(GameStartTitleCardWidth, GameStartTitleCardHeight);

            if (bannerRectTransform != null)
            {
                bannerRectTransform.sizeDelta = new Vector2(GameStartTitleCardWidth, GameStartTitleCardHeight);
            }

            if (bannerPanelRectTransform != null)
            {
                bannerPanelRectTransform.sizeDelta = new Vector2(GameStartTitleCardWidth, GameStartTitleCardHeight);
            }
        }

        private void SetBannerVisualState(float alpha, float yOffset, float scale)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            if (bannerRectTransform != null)
            {
                bannerRectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, yOffset);
                bannerRectTransform.localScale = baseScale * scale;
            }
        }

        private void ResetBannerTransform()
        {
            CacheTransformDefaults();
            if (bannerRectTransform != null)
            {
                bannerRectTransform.anchoredPosition = baseAnchoredPosition;
                bannerRectTransform.localScale = baseScale;
                bannerRectTransform.sizeDelta = baseBannerSize;
            }

            if (bannerText != null)
            {
                bannerText.fontSize = baseBannerTextFontSize;
                bannerText.rectTransform.sizeDelta = baseBannerTextSize;
            }

            if (bannerPanelRectTransform != null)
            {
                bannerPanelRectTransform.sizeDelta = baseBannerPanelSize;
            }
        }

        private void FlushPendingBanner()
        {
            if (!hasPendingBanner)
            {
                return;
            }

            string text = pendingBannerText;
            float duration = pendingBannerDuration;
            ClearPendingBanner();
            Show(text, duration);
        }

        private void ClearPendingBanner()
        {
            pendingBannerText = string.Empty;
            pendingBannerDuration = 0f;
            hasPendingBanner = false;
        }
    }
}
