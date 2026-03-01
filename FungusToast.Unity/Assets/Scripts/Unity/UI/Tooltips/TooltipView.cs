using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Runtime instance handling sizing and text assignment. Created from prefab and reused.
    /// </summary>
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RectTransform background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private LayoutElement layoutElement;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.15f;

        private Coroutine fadeCoroutine;

        public RectTransform RectTransform => transform as RectTransform;

        public void PrepareForLayout()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetText(string value, int? maxWidth)
        {
            if (text == null) return;
            text.richText = true;
            text.text = value;
            if (layoutElement != null)
            {
                if (maxWidth.HasValue)
                {
                    layoutElement.enabled = true;
                    layoutElement.preferredWidth = maxWidth.Value;
                }
                else
                {
                    layoutElement.enabled = false;
                }
            }
            Canvas.ForceUpdateCanvases();
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                if (fadeDuration > 0f)
                {
                    if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(Fade(canvasGroup.alpha, 1f));
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        public void HideImmediate()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            if (canvasGroup != null)
                canvasGroup.alpha = to;
            fadeCoroutine = null;
        }
    }
}
