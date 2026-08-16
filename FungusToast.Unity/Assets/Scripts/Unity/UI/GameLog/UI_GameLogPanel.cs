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
        // Keep the two header controls inside the narrowest sidebar while still
        // exceeding the desktop minimum hit target.
        private const float ClearButtonMinimumWidth = 64f;
        private const float HeaderActionInset = 8f;
        // The visible log-header band is 25 units tall. Keep the title's
        // raycastable tooltip area inside that band so it cannot steal hover
        // input from the profile icons immediately above the activity log.
        private const float HeaderTitleHitHeight = 25f;
        // Keep the visibility control compact enough to stay inside the header
        // gutter at every supported sidebar width.
        private const float CollapseButtonWidth = 56f;
        private const float HeaderActionSpacing = 4f;
        private const float HeaderActionsWidth = ClearButtonMinimumWidth + CollapseButtonWidth + HeaderActionSpacing;
        // "Latest (30)" must fit at the normal micro-text size.  The human
        // activity feed uses the same control as the global feed.
        private const float LatestButtonWidth = 88f;
        private const float ActivityButtonHeight = 32f;
        private const float CollapsedHeight = 48f;
        private const float ExpandedMinimumHeight = 180f;
        private const float BottomFollowThreshold = 0.025f;

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
        private RectTransform headerActionsRoot;
        private RectTransform scrollViewRoot;
        private Vector2 headerOriginalAnchoredPosition;
        private Vector2 scrollViewOriginalOffsetMax;
        private bool topActionAttentionActive;
        private float topActionAttentionUntilUnscaledTime;
        private Button collapseButton;
        private TextMeshProUGUI collapseButtonLabel;
        private Button latestButton;
        private TextMeshProUGUI latestButtonLabel;
        private bool isCollapsed;
        private int unseenEntryCount;
        private bool topActionRequestedVisible;

        private void Awake()
        {
            EnsureTopActionUi();
            EnsureActivityControlsUi();
            ApplyStyle();

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);

            if (headerText != null && string.IsNullOrEmpty(headerText.text))
                headerText.text = defaultHeaderText;

            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(OnScrollPositionChanged);

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
                headerText.text = isPlayerSpecificPanel ? "Human Log" : "Global Log";
                headerText.fontSize = UIStyleTokens.Typography.MicroMinimum;
                headerText.enableAutoSizing = false;
                headerText.textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(headerText);
                headerText.alignment = TextAlignmentOptions.MidlineLeft;
            }

            if (clearButton != null)
            {
                UIStyleTokens.Button.ApplyStyle(clearButton);
                ConfigureClearButtonReadability();
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

        private void ConfigureClearButtonReadability()
        {
            if (clearButton != null && clearButton.transform is RectTransform buttonRect)
            {
                if (headerActionsRoot == null)
                {
                    buttonRect.sizeDelta = new Vector2(
                        Mathf.Max(buttonRect.sizeDelta.x, ClearButtonMinimumWidth),
                        ActivityButtonHeight);
                    buttonRect.anchorMin = new Vector2(1f, 1f);
                    buttonRect.anchorMax = new Vector2(1f, 1f);
                    buttonRect.pivot = new Vector2(1f, 0.5f);
                    buttonRect.anchoredPosition = new Vector2(-HeaderActionInset, -20f);
                }

                ConfigureHeaderActionLayout(clearButton, ClearButtonMinimumWidth);
            }

            // Both activity feeds share this header contract. Explicitly reserve
            // the same title lane so per-instance prefab layout does not push the
            // Global Log title left or allow its actions to escape the panel.
            if (headerText != null)
            {
                var headerTextRect = headerText.rectTransform;
                headerTextRect.anchorMin = new Vector2(0f, 0.5f);
                headerTextRect.anchorMax = new Vector2(1f, 0.5f);
                headerTextRect.pivot = new Vector2(0.5f, 0.5f);
                headerTextRect.offsetMin = new Vector2(HeaderActionInset, -HeaderTitleHitHeight * 0.5f);
                headerTextRect.offsetMax = new Vector2(
                    -(HeaderActionsWidth + HeaderActionInset),
                    HeaderTitleHitHeight * 0.5f);
            }

            if (clearButton == null)
            {
                return;
            }

            var labels = clearButton.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].fontSize = UIStyleTokens.Typography.CaptionMinimum;
                labels[i].fontSizeMax = UIStyleTokens.Typography.CaptionMinimum;
                labels[i].fontSizeMin = 10f;
                labels[i].enableAutoSizing = true;
                labels[i].textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(labels[i]);
            }
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

            topActionRequestedVisible = isVisible && onClick != null;
            bool shouldShow = topActionRequestedVisible && !isCollapsed;
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
                unseenEntryCount = 0;
                QueueBottomScrollFollowup();
                UpdateLatestButton();
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
            bool playerChanged = activePlayerId != playerId;
            activePlayerId = playerId;
            if (headerText != null)
                headerText.text = $"{playerName} Activity Log";
            if (isPlayerSpecificPanel)
            {
                RebuildForPlayerEntries(
                    logManager?.GetRecentEntries(maxVisibleEntries) ?? Enumerable.Empty<GameLogEntry>(),
                    resetUnseenCount: playerChanged);
            }
        }

        private void OnDestroy()
        {
            if (subscribed && logManager != null)
            {
                logManager.OnNewLogEntry -= AddLogEntry;
                subscribed = false;
            }

            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollPositionChanged);
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

            }

            bool shouldFollowLatest = ShouldFollowLatest();
            CreateVisualEntry(entry);
            QueueLayoutRefresh();

            if (shouldFollowLatest)
            {
                QueueBottomScrollFollowup();
            }
            else
            {
                unseenEntryCount++;
                UpdateLatestButton();
            }
        }

        private void CreateVisualEntry(GameLogEntry entry)
        {
            bool startsRoundGroup = entryUIs.Count == 0 || entryUIs[entryUIs.Count - 1].DisplayedRound != entry.Round;
            var entryUI = entryPool.Get();
            entryUI.transform.SetParent(contentParent, false);
            entryUI.transform.SetAsLastSibling();
            entryUI.SetEntry(entry, startsRoundGroup);
            entryUIs.Add(entryUI);
            entryUI.FadeIn();

            while (entryUIs.Count > maxVisibleEntries)
            {
                var oldEntry = entryUIs[0];
                entryUIs.RemoveAt(0);
                if (oldEntry != null)
                    entryPool.Release(oldEntry);
            }

            if (entryUIs.Count > 0)
                entryUIs[0].SetRoundGroupStart(true);

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
            unseenEntryCount = 0;
            UpdateLatestButton();
        }

        private void RebuildForPlayerEntries(IEnumerable<GameLogEntry> entries, bool resetUnseenCount = true)
        {
            foreach (var e in entryUIs)
                if (e != null) entryPool.Release(e);
            entryUIs.Clear();
            if (entries == null) return;
            foreach (var entry in entries.Where(e => !e.PlayerId.HasValue || e.PlayerId == activePlayerId).TakeLast(maxVisibleEntries))
                CreateVisualEntry(entry);
            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
            if (resetUnseenCount)
            {
                unseenEntryCount = 0;
            }
            UpdateLatestButton();
        }

        private void QueueLayoutRefresh() => pendingLayoutRebuild = true;

        private void QueueBottomScrollFollowup()
        {
            if (!autoScrollToBottom || scrollRect == null || isCollapsed)
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

            if (autoScrollToBottom && scrollRect != null && ShouldFollowLatest())
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
                unseenEntryCount = 0;
                UpdateLatestButton();
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

        private void EnsureActivityControlsUi()
        {
            if (headerRoot == null || scrollViewRoot == null)
            {
                EnsureTopActionUi();
            }

            if (headerRoot == null || scrollViewRoot == null)
            {
                return;
            }

            EnsureHeaderActionControls();

            ConfigureClearButtonReadability();

            if (latestButton == null)
            {
                latestButton = CreateButton(scrollViewRoot, "ReturnToLatestButton", new Vector2(1f, 0f), new Vector2(-(HeaderActionInset + (LatestButtonWidth * 0.5f)), 22f), new Vector2(LatestButtonWidth, ActivityButtonHeight), out latestButtonLabel);
                latestButton.onClick.AddListener(ScrollToBottom);
            }

            UpdateCollapsedVisuals();
            UpdateLatestButton();
        }

        private void EnsureHeaderActionControls()
        {
            if (headerRoot == null || headerActionsRoot != null)
            {
                return;
            }

            var actionsObject = new GameObject("ActivityHeaderActions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionsObject.transform.SetParent(headerRoot, false);
            actionsObject.transform.SetAsLastSibling();

            headerActionsRoot = actionsObject.GetComponent<RectTransform>();
            headerActionsRoot.anchorMin = new Vector2(1f, 0f);
            headerActionsRoot.anchorMax = new Vector2(1f, 1f);
            headerActionsRoot.pivot = new Vector2(1f, 0.5f);
            headerActionsRoot.anchoredPosition = new Vector2(-HeaderActionInset, 0f);
            headerActionsRoot.sizeDelta = new Vector2(HeaderActionsWidth, 0f);

            // This is an anchored overlay within the header, not a header-row
            // child.  Prevent any prefab/layout variation from reflowing it
            // beyond the Global Log edge.
            var headerActionsLayoutElement = actionsObject.AddComponent<LayoutElement>();
            headerActionsLayoutElement.ignoreLayout = true;

            var actionsLayout = actionsObject.GetComponent<HorizontalLayoutGroup>();
            actionsLayout.padding = new RectOffset(0, 0, 0, 0);
            actionsLayout.spacing = HeaderActionSpacing;
            actionsLayout.childAlignment = TextAnchor.MiddleRight;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = false;
            actionsLayout.childForceExpandHeight = false;

            collapseButton = CreateButton(
                headerActionsRoot,
                "ActivityVisibilityButton",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(CollapseButtonWidth, ActivityButtonHeight),
                out collapseButtonLabel);
            ConfigureHeaderActionLayout(collapseButton, CollapseButtonWidth);
            collapseButton.onClick.AddListener(ToggleCollapsed);

            if (clearButton != null)
            {
                clearButton.transform.SetParent(headerActionsRoot, false);
                clearButton.transform.SetAsLastSibling();
                ConfigureHeaderActionLayout(clearButton, ClearButtonMinimumWidth);
            }
        }

        private static void ConfigureHeaderActionLayout(Button button, float width)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
            layout.minHeight = ActivityButtonHeight;
            layout.preferredHeight = ActivityButtonHeight;
            layout.flexibleHeight = 0f;
        }

        private static Button CreateButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, out TextMeshProUGUI label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchor;
            buttonRect.anchorMax = anchor;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = size;

            var button = buttonObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 2f);
            labelRect.offsetMax = new Vector2(-4f, -2f);

            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = UIStyleTokens.Typography.MicroMinimum;
            label.fontSizeMax = UIStyleTokens.Typography.MicroMinimum;
            label.fontSizeMin = 10f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.color = UIStyleTokens.Text.Primary;
            label.raycastTarget = false;
            return button;
        }

        private void ToggleCollapsed() => SetCollapsed(!isCollapsed);

        private void SetCollapsed(bool collapsed)
        {
            if (isCollapsed == collapsed)
            {
                return;
            }

            isCollapsed = collapsed;
            if (!isCollapsed)
            {
                unseenEntryCount = 0;
                QueueBottomScrollFollowup();
            }

            UpdateCollapsedVisuals();
            QueueLayoutRefresh();
        }

        private void UpdateCollapsedVisuals()
        {
            if (collapseButtonLabel != null)
                collapseButtonLabel.text = isCollapsed ? "Show" : "Hide";

            if (scrollViewRoot != null)
                scrollViewRoot.gameObject.SetActive(!isCollapsed);

            if (topActionRowRoot != null && isCollapsed)
                topActionRowRoot.gameObject.SetActive(false);
            else if (topActionRowRoot != null)
                topActionRowRoot.gameObject.SetActive(topActionRequestedVisible);

            ApplyTopActionLayout(!isCollapsed && topActionRequestedVisible);

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = isCollapsed ? CollapsedHeight : ExpandedMinimumHeight;
                layoutElement.preferredHeight = isCollapsed ? CollapsedHeight : -1f;
                layoutElement.flexibleHeight = isCollapsed ? 0f : 1f;
            }

            UpdateLatestButton();
        }

        private bool ShouldFollowLatest()
        {
            return !isCollapsed && (scrollRect == null || scrollRect.verticalNormalizedPosition <= BottomFollowThreshold);
        }

        private void OnScrollPositionChanged(Vector2 _)
        {
            if (ShouldFollowLatest())
            {
                unseenEntryCount = 0;
            }

            UpdateLatestButton();
        }

        private void UpdateLatestButton()
        {
            if (latestButton == null)
            {
                return;
            }

            bool shouldShow = !isCollapsed && !ShouldFollowLatest() && entryUIs.Count > 0;
            latestButton.gameObject.SetActive(shouldShow);
            if (latestButtonLabel != null)
            {
                latestButtonLabel.text = unseenEntryCount > 0 ? $"Latest ({unseenEntryCount})" : "Latest";
            }
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
