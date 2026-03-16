using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    public class UI_MutationTreeToastPresenter : MonoBehaviour
    {
        [SerializeField] private UI_MutationManager mutationManager;
        [SerializeField] private float displayDuration = 2.1f;
        [SerializeField] private float fadeDuration = 0.18f;

        private readonly Queue<string> pendingMessages = new();

        private RectTransform toastRoot;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI messageText;
        private Coroutine activeRoutine;

        public void Initialize(UI_MutationManager manager)
        {
            mutationManager = manager;
        }

        public void ShowIfTreeOpen(string message)
        {
            if (mutationManager == null
                || !mutationManager.IsTreeOpen
                || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnsureToastUi();
            pendingMessages.Enqueue(message);

            if (activeRoutine == null)
            {
                activeRoutine = StartCoroutine(PlayQueue());
            }
        }

        private void EnsureToastUi()
        {
            if (toastRoot != null)
            {
                return;
            }

            var parent = mutationManager != null ? mutationManager.MutationTreeTransform : transform;
            var rootObject = new GameObject("MutationTreeToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.transform.SetParent(parent, false);

            toastRoot = rootObject.GetComponent<RectTransform>();
            toastRoot.anchorMin = new Vector2(0.5f, 1f);
            toastRoot.anchorMax = new Vector2(0.5f, 1f);
            toastRoot.pivot = new Vector2(0.5f, 1f);
            toastRoot.anchoredPosition = new Vector2(0f, -76f);
            toastRoot.sizeDelta = new Vector2(520f, 54f);
            toastRoot.SetAsLastSibling();

            canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.58f);
            backgroundColor.a = 0.96f;
            background.color = backgroundColor;
            background.raycastTarget = false;

            var outline = rootObject.AddComponent<Outline>();
            outline.effectColor = new Color(UIStyleTokens.Accent.Spore.r, UIStyleTokens.Accent.Spore.g, UIStyleTokens.Accent.Spore.b, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rootObject.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            messageText = textObject.GetComponent<TextMeshProUGUI>();
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = UIStyleTokens.Text.Primary;
            messageText.fontSize = 23f;
            messageText.fontStyle = FontStyles.Bold;
            messageText.textWrappingMode = TextWrappingModes.NoWrap;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
            messageText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                messageText.font = TMP_Settings.defaultFontAsset;
            }
        }

        private IEnumerator PlayQueue()
        {
            while (pendingMessages.Count > 0)
            {
                if (mutationManager == null || !mutationManager.IsTreeOpen)
                {
                    pendingMessages.Clear();
                    break;
                }

                messageText.text = pendingMessages.Dequeue();
                yield return FadeTo(1f, fadeDuration);
                yield return new WaitForSeconds(displayDuration);
                yield return FadeTo(0f, fadeDuration);
            }

            activeRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha, float durationSeconds)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float startingAlpha = canvasGroup.alpha;
            if (Mathf.Approximately(durationSeconds, 0f))
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startingAlpha, targetAlpha, elapsed / durationSeconds);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
