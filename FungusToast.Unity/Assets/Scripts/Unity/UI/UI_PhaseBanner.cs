using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseBanner : MonoBehaviour
    {
        public TextMeshProUGUI bannerText;
        public CanvasGroup canvasGroup;
        [SerializeField] private Image bannerBackground;

        private void Awake()
        {
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyStyle();
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
            bannerText.text = text;
            StopAllCoroutines();
            StartCoroutine(FadeInOut(duration));
        }

        public void HideImmediate()
        {
            StopAllCoroutines();
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
            //gameObject.SetActive(true);

            // Fade in
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                canvasGroup.alpha = t / 0.5f;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(duration);

            // Fade out
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                canvasGroup.alpha = 1f - (t / 0.5f);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            //gameObject.SetActive(false);
        }
    }
}

