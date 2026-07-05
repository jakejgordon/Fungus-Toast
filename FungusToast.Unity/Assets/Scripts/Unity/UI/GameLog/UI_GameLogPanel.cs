using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.GameLog
{
    public class UI_GameLogPanel : MonoBehaviour
    {
        private const float TopActionRowHeight = 40f;
        private const float TopActionRowVerticalOffset = 8f;
        private const float TopActionReservedHeight = 45f;
        private const float TopActionAttentionPulseSpeed = 6f;
        private const float TopActionAttentionScaleStrength = 0.035f;

        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentParent;
        [SerializeField] private UI_GameLogEntry entryPrefab;
        [SerializeField] private Button clearButton;
        [SerializeField] private TextMeshProUGUI headerText;

        [Header("Settings")]
        [SerializeField] private int maxVisibleEntries = 30;
        [SerializeField] private bool autoScrollToBottom = true;
        [SerializeField] private string defaultHeaderText = "Game Log";
        [SerializeField] private bool isPlayerSpecificPanel = false; // set true for per-player log (can be forced at runtime)

        private readonly List<UI_GameLogEntry> entryUIs = new();
        private ObjectPool<UI_GameLogEntry> entryPool;
        private IGameLogManager logManager;
        private int activePlayerId = -1; // for player-specific filtering
        private bool subscribed = false; // prevent double subscription
        private bool pendingLayoutRebuild = false; // coalesce multiple adds per frame
        private int pendingBottomScrollFrames = 0;
        private RectTransform topActionRowRoot;
        private Button topActionButton;
        private TextMeshProUGUI topActionButtonLabel;
        private RectTransform headerRoot;
        private RectTransform scrollViewRoot;
        private Vector2 headerOriginalAnchoredPosition;
        private Vector2 scrollViewOriginalOffsetMax;
        private bool topActionAttentionActive;
        private float topActionAttentionUntilUnscaledTime;

        private void Awake()
        {
            EnsureTopActionUi();
            ApplyStyle();

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);

            if (headerText != null && string.IsNullOrEmpty(headerText.text))
                headerText.text = defaultHeaderText;

            // Initialize the object pool for log entries.
            // defaultCapacity matches maxVisibleEntries; max is a safety cap.
            entryPool = new ObjectPool<UI_GameLogEntry>(
                createFunc: () =>
                {
                    var entry = Instantiate(entryPrefab, contentParent);
                    return entry;
                },
                actionOnGet: entry =>
                {
                    entry.transform.SetParent(contentParent, false);
                    entry.gameObject.SetActive(true);
                },
                actionOnRelease: entry =>
                {
                    entry.ResetForReuse();
                    entry.gameObject.SetActive(false);
                },
                actionOnDestroy: entry =>
                {
                    if (entry != null) Destroy(entry.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: maxVisibleEntries,
                maxSize: maxVisibleEntries * 2
            );
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelPrimary);

            if (contentParent != null)
            {
                UIStyleTokens.ApplyPanelSurface(contentParent.gameObject, UIStyleTokens.Surface.PanelSecondary);
            }

            if (scrollRect != null)
            {
                ApplyImageColor(scrollRect.GetComponent<Image>(), UIStyleTokens.Surface.PanelPrimary);
                if (scrollRect.viewport != null)
                {
                    ApplyImageColor(scrollRect.viewport.GetComponent<Image>(), UIStyleTokens.Surface.PanelSecondary);
                }

                if (scrollRect.content != null)
                {
                    ApplyImageColor(scrollRect.content.GetComponent<Image>(), UIStyleTokens.Surface.PanelSecondary);
                }
            }

            if (headerText != null)
            {
                headerText.color = UIStyleTokens.Text.Primary;
            }

            if (clearButton != null)
            {
                UIStyleTokens.Button.ApplyStyle(clearButton);
            }

            if (topActionButton != null)
            {
                ApplyTopActionButtonNormalStyle();
            }

            if (topActionButtonLabel != null)
            {
                topActionButtonLabel.color = UIStyleTokens.Text.Primary;
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 22f);
        }

        public void ConfigureTopActionButton(string label, Action onClick, bool isVisible)
        {
            EnsureTopActionUi();
            if (topActionRowRoot == null || topActionButton == null || topActionButtonLabel == null)
            {
                return;
            }

            topActionButton.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                topActionButton.onClick.AddListener(() => onClick());
            }

            topActionButtonLabel.text = label ?? string.Empty;

            bool shouldShow = isVisible && onClick != null;
            topActionButton.interactable = shouldShow;
            topActionRowRoot.gameObject.SetActive(shouldShow);
            ApplyTopActionLayout(shouldShow);
            ForceLayoutRefreshImmediate();
        }

        public void TriggerTopActionAttention(float durationSeconds)
        {
            EnsureTopActionUi();
            if (topActionButton == null || topActionRowRoot == null || durationSeconds <= 0f)
            {
                return;
            }

            topActionAttentionActive = true;
            topActionAttentionUntilUnscaledTime = Mathf.Max(topActionAttentionUntilUnscaledTime, Time.unscaledTime + durationSeconds);
            ApplyTopActionAttentionVisual(0f);
        }

        private static void ApplyImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private void LateUpdate()
        {
            UpdateTopActionAttentionState();

            if (pendingLayoutRebuild)
            {
                ForceLayoutRefreshImmediate();
                pendingLayoutRebuild = false;
            }

            if (pendingBottomScrollFrames > 0)
            {
                ForceLayoutRefreshImmediate();
                pendingBottomScrollFrames--;
            }
        }

        public void Initialize(IGameLogManager gameLogManager)
        {
            // Unsubscribe old manager if switching
            if (subscribed && logManager != null && !ReferenceEquals(logManager, gameLogManager))
            {
                logManager.OnNewLogEntry -= AddLogEntry;
                subscribed = false;
            }

            logManager = gameLogManager;
            if (logManager != null)
            {
                if (!subscribed)
                {
                    logManager.OnNewLogEntry += AddLogEntry;
                    subscribed = true;
                }

                // Clear existing visual list to avoid duplicates when re-initializing
                foreach (var e in entryUIs)
                    if (e != null) entryPool.Release(e);
                entryUIs.Clear();

                if (isPlayerSpecificPanel && activePlayerId >= 0)
                {
                    RebuildForPlayerEntries(logManager.GetRecentEntries(maxVisibleEntries));
                }
                else
                {
                    // Populate with current history once
                    foreach (var entry in logManager.GetRecentEntries(maxVisibleEntries))
                    {
                        AddLogEntry(entry);
                    }
                }

                QueueLayoutRefresh();
                QueueBottomScrollFollowup();
            }
        }

        /// <summary>
        /// Force this panel into player-specific filtering mode at runtime (safety for misconfigured inspector).
        /// </summary>
        public void EnablePlayerSpecificFiltering()
        {
            if (!isPlayerSpecificPanel)
            {
                isPlayerSpecificPanel = true;
                // Rebuild with current player filter if already set
                if (activePlayerId >= 0 && logManager != null)
                {
                    RebuildForPlayerEntries(logManager.GetRecentEntries(maxVisibleEntries));
                }
            }
        }

        public void SetHeaderText(string text)
        {
            if (headerText != null)
                headerText.text = text;
        }

        public void SetActivePlayer(int playerId, string playerName)
        {
            activePlayerId = playerId;
            if (headerText != null)
                headerText.text = $"{playerName} Activity Log";
            if (isPlayerSpecificPanel)
            {
                RebuildForPlayerEntries(logManager?.GetRecentEntries(maxVisibleEntries) ?? Enumerable.Empty<GameLogEntry>());
            }
        }

        private void OnDestroy()
        {
            if (subscribed && logManager != null)
            {
                logManager.OnNewLogEntry -= AddLogEntry;
                subscribed = false;
            }
        }

        public void AddLogEntry(GameLogEntry entry)
        {
            if (entryPrefab == null || contentParent == null)
            {
                Debug.LogError("UI_GameLogPanel: Missing prefab or content parent references!");
                return;
            }

            if (isPlayerSpecificPanel)
            {
                if (activePlayerId < 0) return; // not yet bound
                if (entry.PlayerId.HasValue && entry.PlayerId.Value != activePlayerId) return;

                if (logManager != null)
                {
                    RebuildForPlayerEntries(logManager.GetRecentEntries(maxVisibleEntries));
                    return;
                }
            }

            CreateVisualEntry(entry);
        }

        private void CreateVisualEntry(GameLogEntry entry)
        {
            var entryUI = entryPool.Get();
            entryUI.transform.SetParent(contentParent, false);
            entryUI.transform.SetAsLastSibling();
            entryUI.SetEntry(entry);
            entryUIs.Add(entryUI);
            entryUI.FadeIn();

            while (entryUIs.Count > maxVisibleEntries)
            {
                var oldEntry = entryUIs[0];
                entryUIs.RemoveAt(0);
                if (oldEntry != null)
                    entryPool.Release(oldEntry);
            }

            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void ClearLog()
        {
            foreach (var entryUI in entryUIs)
            {
                if (entryUI != null)
                    entryPool.Release(entryUI);
            }
            entryUIs.Clear();

            if (!isPlayerSpecificPanel && logManager != null)
                logManager.ClearLog();

            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void RebuildForPlayerEntries(IEnumerable<GameLogEntry> entries)
        {
            foreach (var e in entryUIs)
                if (e != null) entryPool.Release(e);
            entryUIs.Clear();
            if (entries == null) return;
            foreach (var entry in entries.Where(e => !e.PlayerId.HasValue || e.PlayerId == activePlayerId).TakeLast(maxVisibleEntries))
                CreateVisualEntry(entry);
            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void QueueLayoutRefresh() => pendingLayoutRebuild = true;

        private void QueueBottomScrollFollowup()
        {
            if (!autoScrollToBottom || scrollRect == null)
            {
                return;
            }

            pendingBottomScrollFrames = Mathf.Max(pendingBottomScrollFrames, 3);
        }

        private void ForceLayoutRefreshImmediate()
        {
            if (transform is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }

            if (contentParent == null) return;

            Canvas.ForceUpdateCanvases();

            var contentRT = contentParent as RectTransform;
            if (contentRT != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);

            if (scrollRect != null && scrollRect.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);

            if (scrollRect != null && scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            if (autoScrollToBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 0f;
                scrollRect.velocity = Vector2.zero;
            }
        }

        public void SetAutoScroll(bool enabled) => autoScrollToBottom = enabled;
        public void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 0f;
                scrollRect.velocity = Vector2.zero;
            }
        }
        public void ScrollToTop()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void EnsureTopActionUi()
        {
            if (topActionRowRoot != null)
            {
                return;
            }

            headerRoot = transform.Find("UI_GameLogPanelHeader") as RectTransform;
            scrollViewRoot = transform.Find("UI_GameLogPanelScrollView") as RectTransform;
            if (headerRoot == null || scrollViewRoot == null)
            {
                return;
            }

            headerOriginalAnchoredPosition = headerRoot.anchoredPosition;
            scrollViewOriginalOffsetMax = scrollViewRoot.offsetMax;

            var rowObject = new GameObject("UI_GameLogPanelTopActionRow", typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(transform, false);
            rowObject.transform.SetSiblingIndex(0);

            topActionRowRoot = rowObject.GetComponent<RectTransform>();
            topActionRowRoot.anchorMin = new Vector2(0f, 1f);
            topActionRowRoot.anchorMax = new Vector2(1f, 1f);
            topActionRowRoot.pivot = new Vector2(0.5f, 1f);
            topActionRowRoot.offsetMin = new Vector2(0f, -(TopActionRowHeight - TopActionRowVerticalOffset));
            topActionRowRoot.offsetMax = new Vector2(0f, TopActionRowVerticalOffset);

            var rowBackground = rowObject.GetComponent<Image>();
            rowBackground.color = UIStyleTokens.Surface.PanelPrimary;
            rowBackground.raycastTarget = false;

            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(topActionRowRoot, false);

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = new Vector2(4f, 2f);
            buttonRect.offsetMax = new Vector2(-4f, -2f);

            topActionButton = buttonObject.GetComponent<Button>();
            ApplyTopActionButtonNormalStyle();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            topActionButtonLabel = labelObject.GetComponent<TextMeshProUGUI>();
            topActionButtonLabel.fontStyle = FontStyles.Bold;
            topActionButtonLabel.fontSize = 18f;
            topActionButtonLabel.color = UIStyleTokens.Text.Primary;
            topActionButtonLabel.alignment = TextAlignmentOptions.Center;
            topActionButtonLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                topActionButtonLabel.font = TMP_Settings.defaultFontAsset;
            }

            topActionRowRoot.gameObject.SetActive(false);
            ApplyTopActionLayout(false);
        }

        private void ApplyTopActionButtonNormalStyle()
        {
            if (topActionButton == null)
            {
                return;
            }

            UIStyleTokens.Button.ApplyPanelSecondaryStyle(topActionButton);
            UIStyleTokens.Button.SetButtonLabelColor(topActionButton, UIStyleTokens.Text.Primary);
            topActionButton.transform.localScale = Vector3.one;
        }

        private void UpdateTopActionAttentionState()
        {
            if (topActionButton == null || !topActionAttentionActive)
            {
                return;
            }

            if (!topActionRowRoot.gameObject.activeInHierarchy || !topActionButton.interactable)
            {
                topActionAttentionActive = false;
                ApplyTopActionButtonNormalStyle();
                return;
            }

            float remaining = topActionAttentionUntilUnscaledTime - Time.unscaledTime;
            if (remaining <= 0f)
            {
                topActionAttentionActive = false;
                ApplyTopActionButtonNormalStyle();
                return;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * TopActionAttentionPulseSpeed) + 1f) * 0.5f;
            ApplyTopActionAttentionVisual(pulse);
        }

        private void ApplyTopActionAttentionVisual(float pulse)
        {
            if (topActionButton == null)
            {
                return;
            }

            var colors = UIStyleTokens.Button.BuildColorBlock();
            colors.normalColor = Color.Lerp(UIStyleTokens.Button.BackgroundSelected, UIStyleTokens.Accent.Spore, 0.28f + (pulse * 0.18f));
            colors.highlightedColor = Color.Lerp(UIStyleTokens.Button.BackgroundHover, UIStyleTokens.Accent.Spore, 0.42f);
            colors.pressedColor = Color.Lerp(UIStyleTokens.Button.BackgroundPressed, UIStyleTokens.Accent.Moss, 0.28f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = UIStyleTokens.WithAlpha(UIStyleTokens.Surface.PanelPrimary, UIStyleTokens.Alpha.PanelDisabled);
            topActionButton.colors = colors;
            UIStyleTokens.Button.SetButtonLabelColor(topActionButton, UIStyleTokens.Text.OnAccent);

            float scale = 1f + (pulse * TopActionAttentionScaleStrength);
            topActionButton.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyTopActionLayout(bool showTopAction)
        {
            if (headerRoot == null || scrollViewRoot == null)
            {
                return;
            }

            float verticalOffset = showTopAction ? TopActionReservedHeight : 0f;
            headerRoot.anchoredPosition = headerOriginalAnchoredPosition + new Vector2(0f, -verticalOffset);
            scrollViewRoot.offsetMax = scrollViewOriginalOffsetMax + new Vector2(0f, -verticalOffset);
        }
    }
}
