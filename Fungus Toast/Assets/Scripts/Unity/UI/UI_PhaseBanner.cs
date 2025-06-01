using UnityEngine;
using TMPro;
using System.Collections;

namespace FungusToast.Unity.UI
{
    public class UI_PhaseBanner : MonoBehaviour
    {
        public TextMeshProUGUI bannerText;
        public CanvasGroup canvasGroup;

        public void Show(string text, float duration = 2f)
        {
            bannerText.text = text;
            StopAllCoroutines();
            StartCoroutine(FadeInOut(duration));
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

