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

        public TextMeshProUGUI bannerText;
        public CanvasGroup canvasGroup;
        [SerializeField] private Image bannerBackground;

        private RectTransform bannerRectTransform;
        private Vector2 baseAnchoredPosition;
        private Vector3 baseScale = Vector3.one;
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

