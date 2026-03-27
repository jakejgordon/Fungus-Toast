using System.Collections;
using System.Collections.Generic;
using System;
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
        private RectTransform modalRoot;
        private CanvasGroup modalCanvasGroup;
        private TextMeshProUGUI modalMessageText;
        private Button modalCloseButton;
        private Action modalDismissedCallback;

        public void Initialize(UI_MutationManager manager)
        {
            mutationManager = manager;
        }

        public void ResetForGameTransition()
        {
            pendingMessages.Clear();

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (messageText != null)
            {
                messageText.text = string.Empty;
            }

            HideModalImmediate(invokeCallback: false);
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

        public void ShowModalIfTreeOpen(string message, Action onDismissed = null)
        {
            if (mutationManager == null
                || !mutationManager.IsTreeOpen
                || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnsureModalUi();
            modalDismissedCallback = onDismissed;
            modalMessageText.text = message;
            modalCanvasGroup.alpha = 1f;
            modalCanvasGroup.blocksRaycasts = true;
            modalCanvasGroup.interactable = true;
            modalRoot.gameObject.SetActive(true);
            modalRoot.SetAsLastSibling();
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

        private void EnsureModalUi()
        {
            if (modalRoot != null)
            {
                return;
            }

            var parent = mutationManager != null ? mutationManager.MutationTreeTransform : transform;
            var rootObject = new GameObject("MutationTreeGuidanceModal", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.transform.SetParent(parent, false);

            modalRoot = rootObject.GetComponent<RectTransform>();
            modalRoot.anchorMin = Vector2.zero;
            modalRoot.anchorMax = Vector2.one;
            modalRoot.offsetMin = Vector2.zero;
            modalRoot.offsetMax = Vector2.zero;
            modalRoot.SetAsLastSibling();

            modalCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.blocksRaycasts = false;
            modalCanvasGroup.interactable = false;

            var overlay = rootObject.GetComponent<Image>();
            overlay.color = new Color(UIStyleTokens.Surface.OverlayDim.r, UIStyleTokens.Surface.OverlayDim.g, UIStyleTokens.Surface.OverlayDim.b, 0.72f);
            overlay.raycastTarget = true;

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.transform.SetParent(rootObject.transform, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(660f, 240f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);

            var panelBackground = panelObject.GetComponent<Image>();
            var panelColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Success, 0.12f);
            panelColor.a = 0.98f;
            panelBackground.color = panelColor;
            panelBackground.raycastTarget = true;

            var panelOutline = panelObject.GetComponent<Outline>();
            panelOutline.effectColor = new Color(UIStyleTokens.Accent.Spore.r, UIStyleTokens.Accent.Spore.g, UIStyleTokens.Accent.Spore.b, 0.85f);
            panelOutline.effectDistance = new Vector2(1f, -1f);

            var closeButtonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            closeButtonObject.transform.SetParent(panelObject.transform, false);

            var closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);

            var closeImage = closeButtonObject.GetComponent<Image>();
            closeImage.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Text.Primary, 0.18f);

            modalCloseButton = closeButtonObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(modalCloseButton, useSelectedAsNormal: true);
            modalCloseButton.onClick.RemoveAllListeners();
            modalCloseButton.onClick.AddListener(() => HideModalImmediate());

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeButtonObject.transform, false);

            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontSize = 24f;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.raycastTarget = false;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelObject.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(20f, -64f);
            titleRect.offsetMax = new Vector2(-72f, -20f);

            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = "How Mutation Spending Works";
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = UIStyleTokens.Text.Primary;
            titleText.fontSize = 29f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;

            var messageObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            messageObject.transform.SetParent(panelObject.transform, false);

            var messageRect = messageObject.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0f, 0f);
            messageRect.anchorMax = new Vector2(1f, 1f);
            messageRect.offsetMin = new Vector2(20f, 20f);
            messageRect.offsetMax = new Vector2(-20f, -74f);

            modalMessageText = messageObject.GetComponent<TextMeshProUGUI>();
            modalMessageText.alignment = TextAlignmentOptions.TopLeft;
            modalMessageText.color = UIStyleTokens.Text.Primary;
            modalMessageText.fontSize = 23f;
            modalMessageText.textWrappingMode = TextWrappingModes.Normal;
            modalMessageText.overflowMode = TextOverflowModes.Overflow;
            modalMessageText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                modalMessageText.font = TMP_Settings.defaultFontAsset;
                titleText.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void HideModalImmediate(bool invokeCallback = true)
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 0f;
                modalCanvasGroup.blocksRaycasts = false;
                modalCanvasGroup.interactable = false;
            }

            if (modalMessageText != null)
            {
                modalMessageText.text = string.Empty;
            }

            if (modalRoot != null)
            {
                modalRoot.gameObject.SetActive(false);
            }

            var dismissedCallback = modalDismissedCallback;
            modalDismissedCallback = null;
            if (invokeCallback)
            {
                dismissedCallback?.Invoke();
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
