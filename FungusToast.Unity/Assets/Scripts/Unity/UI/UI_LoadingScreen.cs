using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Full-screen overlay shown while the game initializes (board creation,
    /// grid rendering, starting-spore animation). Uses a CanvasGroup for
    /// smooth fade-out once the game is ready.
    ///
    /// Setup in Unity Editor:
    ///   1. Create a Panel under the main Canvas (full-stretch anchors).
    ///   2. Add a CanvasGroup component (alpha = 1, blocks raycasts = true).
    ///   3. Add this script.
    ///   4. Optionally assign a TextMeshProUGUI child for the status label.
    ///   5. Wire the reference into GameUIManager.loadingScreen in the Inspector.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UI_LoadingScreen : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Optional UI")]
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Image backgroundImage;

        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = UIStyleTokens.Surface.OverlayDim;
            }

            if (statusLabel != null)
            {
                statusLabel.color = UIStyleTokens.Text.Primary;
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 28f);
        }

        /// <summary>Show the overlay instantly at full opacity.</summary>
        public void Show(string message = "Loading…")
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (statusLabel != null)
            {
                statusLabel.text = message;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
            // Ensure loading screen renders on top of all sibling panels
            transform.SetAsLastSibling();        }

        /// <summary>Fade out and deactivate. Safe to call when already hidden.</summary>
        public void FadeOut()
        {
            if (!gameObject.activeSelf) return;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        /// <summary>Update the status label without affecting visibility.</summary>
        public void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private IEnumerator FadeOutCoroutine()
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            fadeCoroutine = null;
        }
    }
}
