using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Activity feed in two parts. A compact strip stays in the sidebar (title,
    /// Show/Hide toggle with an unread count, and the optional top action row);
    /// it takes whatever height the sidebar has left and hugs the bottom edge,
    /// so gameplay controls above keep their footprint. The entries live in a
    /// pop-out panel anchored to the sidebar that overlays half of the playable
    /// board area; it is closed by default.
    /// </summary>
    public class UI_GameLogPanel : MonoBehaviour
    {
        private const float TopActionRowHeight = 40f;
        private const float TopActionRowSpacing = 5f;
        private const float TopActionAttentionPulseSpeed = 6f;
        private const float TopActionAttentionScaleStrength = 0.035f;
        private const float HeaderActionInset = 8f;
        // Keep the title's raycastable tooltip area well inside the header band
        // so it cannot steal hover input from the profile icons immediately
        // above the activity log.
        private const float HeaderTitleHitHeight = 25f;
        // "Show (99) ›" must fit at the normal micro-text size while the
        // control stays inside the header gutter at every supported sidebar width.
        private const float ToggleButtonWidth = 96f;
        private const float PopoutActionButtonWidth = 72f;
        private const float HeaderActionSpacing = 4f;
        private const float PopoutHeaderActionsWidth = (PopoutActionButtonWidth * 2f) + HeaderActionSpacing;
        // "Latest (30)" must fit at the normal micro-text size.  The human
        // activity feed uses the same control as the global feed.
        private const float LatestButtonWidth = 88f;
        private const float ActivityButtonHeight = 32f;
        // Shared by the sidebar strip's header band and the pop-out header.
        private const float HeaderHeight = 40f;
        private const float PopoutContentInset = 8f;
        // Matches the sidebar layout padding so the pop-out lines up with the
        // sidebar's content rather than its outer edge.
        private const float PopoutVerticalMargin = 10f;
        private const float PopoutPlayableWidthFraction = 0.5f;
        private const float PopoutMinimumWidth = 320f;
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
        private readonly Vector3[] cornerBuffer = new Vector3[4];
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
        private bool topActionAttentionActive;
        private float topActionAttentionUntilUnscaledTime;
        private Button collapseButton;
        private TextMeshProUGUI collapseButtonLabel;
        private Button latestButton;
        private TextMeshProUGUI latestButtonLabel;
        private RectTransform popoutRoot;
        private TextMeshProUGUI popoutHeaderText;
        private RectTransform popoutActionsRoot;
        private Button popoutCloseButton;
        private TextMeshProUGUI popoutCloseButtonLabel;
        private RectTransform hostCanvasRect;
        private RectTransform sidebarRect;
        private RectTransform oppositeSidebarRect;
        private bool isCollapsed = true;
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
                SetTitle(defaultHeaderText);

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

            if (popoutRoot != null)
            {
                UIStyleTokens.ApplyPanelSurface(popoutRoot.gameObject, UIStyleTokens.Surface.PanelPrimary);
            }

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
                headerText.fontSize = UIStyleTokens.Typography.MicroMinimum;
                headerText.enableAutoSizing = false;
                headerText.textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(headerText);
                headerText.alignment = TextAlignmentOptions.MidlineLeft;
            }

            SetTitle(isPlayerSpecificPanel ? "Human Log" : "Global Log");

            // Clear sits beside Close in the pop-out header, so it takes the same
            // dark panel style rather than the light default button surface.
            if (clearButton != null)
            {
                UIStyleTokens.Button.ApplyPanelSecondaryStyle(clearButton);
                ConfigureActionButtonLabels(clearButton);
            }

            ConfigureSidebarHeaderBand();
            ConfigureSidebarHeaderTitleLayout();

            if (topActionButton != null)
            {
                ApplyTopActionButtonNormalStyle();
            }

            if (topActionButtonLabel != null)
            {
                topActionButtonLabel.color = UIStyleTokens.Text.Primary;
            }

            // Only the sidebar strip gets the generic palette pass. The pop-out is
            // inactive here, and the pass cannot tell an inactive button's label
            // from body text, so it would paint the action labels near-white.
            // Every pop-out label is styled explicitly instead.
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 22f);
        }

        // The prefab's header band is shorter than the header and the toggle
        // button spilled past it; stretch it to the full header height and give
        // it the same surface as the pop-out header.
        private void ConfigureSidebarHeaderBand()
        {
            if (headerRoot == null)
            {
                return;
            }

            var band = headerRoot.Find("UI_GameLogPanelHeaderBackground") as RectTransform;
            if (band == null)
            {
                return;
            }

            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.pivot = new Vector2(0.5f, 0.5f);
            band.offsetMin = Vector2.zero;
            band.offsetMax = Vector2.zero;
            ApplyImageColor(band.GetComponent<Image>(), UIStyleTokens.Surface.PanelSecondary);
        }

        // Both activity feeds share this header contract. Explicitly reserve
        // the same title lane so per-instance prefab layout does not push the
        // Global Log title left or allow the toggle to escape the panel.
        private void ConfigureSidebarHeaderTitleLayout()
        {
            if (headerText == null)
            {
                return;
            }

            var headerTextRect = headerText.rectTransform;
            headerTextRect.anchorMin = new Vector2(0f, 0.5f);
            headerTextRect.anchorMax = new Vector2(1f, 0.5f);
            headerTextRect.pivot = new Vector2(0.5f, 0.5f);
            headerTextRect.offsetMin = new Vector2(HeaderActionInset, -HeaderTitleHitHeight * 0.5f);
            headerTextRect.offsetMax = new Vector2(
                -(ToggleButtonWidth + (HeaderActionInset * 2f)),
                HeaderTitleHitHeight * 0.5f);
        }

        private static void ConfigureActionButtonLabels(Button button)
        {
            if (button == null)
            {
                return;
            }

            var labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
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

            // The top action is a sidebar control that happens to sit above the
            // log header; it stays available whether or not the pop-out is open.
            topActionRequestedVisible = isVisible && onClick != null;
            topActionButton.interactable = topActionRequestedVisible;
            topActionRowRoot.gameObject.SetActive(topActionRequestedVisible);
            ApplySidebarStripLayout();
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

            if (!isCollapsed)
            {
                // Sidebar widths follow the window size, so keep the pop-out glued
                // to its sidebar rather than caching a rect at open time.
                UpdatePopoutRect();
            }

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
                UpdateUnseenIndicators();
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

        public void SetHeaderText(string text) => SetTitle(text);

        public void SetActivePlayer(int playerId, string playerName)
        {
            bool playerChanged = activePlayerId != playerId;
            activePlayerId = playerId;
            SetTitle($"{playerName} Activity Log");
            if (isPlayerSpecificPanel)
            {
                RebuildForPlayerEntries(
                    logManager?.GetRecentEntries(maxVisibleEntries) ?? Enumerable.Empty<GameLogEntry>(),
                    resetUnseenCount: playerChanged);
            }
        }

        private void SetTitle(string text)
        {
            if (headerText != null)
                headerText.text = text;
            if (popoutHeaderText != null)
                popoutHeaderText.text = text;
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

            // The pop-out lives under the canvas, not under this panel.
            if (popoutRoot != null)
                Destroy(popoutRoot.gameObject);
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
                UpdateUnseenIndicators();
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
            UpdateUnseenIndicators();
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
            UpdateUnseenIndicators();
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

            if (popoutRoot != null && popoutRoot.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(popoutRoot);
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
                UpdateUnseenIndicators();
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

            // The prefab centres the header on the strip's top edge, so half of
            // it pokes into whatever sits above. Pin it to the bottom instead:
            // the strip is flexible, so this is what keeps the band clear of the
            // sidebar content when there is slack, and flush when there is not.
            headerRoot.anchorMin = new Vector2(0f, 0f);
            headerRoot.anchorMax = new Vector2(1f, 0f);
            headerRoot.pivot = new Vector2(0.5f, 0f);
            headerRoot.anchoredPosition = Vector2.zero;
            headerRoot.sizeDelta = new Vector2(0f, HeaderHeight);

            var rowObject = new GameObject("UI_GameLogPanelTopActionRow", typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(transform, false);
            rowObject.transform.SetSiblingIndex(0);

            // Sits directly above the header band.
            topActionRowRoot = rowObject.GetComponent<RectTransform>();
            topActionRowRoot.anchorMin = new Vector2(0f, 0f);
            topActionRowRoot.anchorMax = new Vector2(1f, 0f);
            topActionRowRoot.pivot = new Vector2(0.5f, 0f);
            topActionRowRoot.anchoredPosition = new Vector2(0f, HeaderHeight + TopActionRowSpacing);
            topActionRowRoot.sizeDelta = new Vector2(0f, TopActionRowHeight);

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

            EnsureSidebarToggleButton();
            EnsurePopoutUi();
            ConfigureSidebarHeaderTitleLayout();

            if (latestButton == null)
            {
                latestButton = CreateButton(scrollViewRoot, "ReturnToLatestButton", new Vector2(1f, 0f), new Vector2(-(HeaderActionInset + (LatestButtonWidth * 0.5f)), 22f), new Vector2(LatestButtonWidth, ActivityButtonHeight), out latestButtonLabel);
                latestButton.onClick.AddListener(ScrollToBottom);
            }

            UpdateCollapsedVisuals();
        }

        private void EnsureSidebarToggleButton()
        {
            if (headerRoot == null || collapseButton != null)
            {
                return;
            }

            collapseButton = CreateButton(
                headerRoot,
                "ActivityVisibilityButton",
                new Vector2(1f, 0.5f),
                new Vector2(-(HeaderActionInset + (ToggleButtonWidth * 0.5f)), 0f),
                new Vector2(ToggleButtonWidth, ActivityButtonHeight),
                out collapseButtonLabel);
            collapseButton.transform.SetAsLastSibling();
            collapseButton.onClick.AddListener(ToggleCollapsed);
        }

        private void EnsurePopoutUi()
        {
            if (popoutRoot != null || scrollViewRoot == null)
            {
                return;
            }

            ResolvePopoutContext();

            var popoutObject = new GameObject("UI_GameLogPopout", typeof(RectTransform), typeof(Image), typeof(Outline));
            popoutObject.transform.SetParent(hostCanvasRect != null ? hostCanvasRect : transform, false);
            // The pop-out never overlaps a sidebar, and every transient overlay
            // (phase banner, prompts, draft and tree panels) must stay on top of
            // it, so it sits beneath all of its canvas siblings.
            popoutObject.transform.SetAsFirstSibling();

            popoutRoot = popoutObject.GetComponent<RectTransform>();
            popoutRoot.anchorMin = Vector2.zero;
            popoutRoot.anchorMax = Vector2.zero;
            popoutRoot.pivot = Vector2.zero;

            var background = popoutObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelPrimary;
            background.raycastTarget = true;

            var outline = popoutObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.AccentOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var headerObject = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObject.transform.SetParent(popoutRoot, false);
            var popoutHeaderRoot = headerObject.GetComponent<RectTransform>();
            popoutHeaderRoot.anchorMin = new Vector2(0f, 1f);
            popoutHeaderRoot.anchorMax = new Vector2(1f, 1f);
            popoutHeaderRoot.pivot = new Vector2(0.5f, 1f);
            popoutHeaderRoot.anchoredPosition = Vector2.zero;
            popoutHeaderRoot.sizeDelta = new Vector2(0f, HeaderHeight);
            var headerBackground = headerObject.GetComponent<Image>();
            headerBackground.color = UIStyleTokens.Surface.PanelSecondary;
            headerBackground.raycastTarget = false;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(popoutHeaderRoot, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(HeaderActionInset, 0f);
            titleRect.offsetMax = new Vector2(-(PopoutHeaderActionsWidth + (HeaderActionInset * 2f)), 0f);
            popoutHeaderText = titleObject.GetComponent<TextMeshProUGUI>();
            popoutHeaderText.font = headerText != null && headerText.font != null ? headerText.font : TMP_Settings.defaultFontAsset;
            popoutHeaderText.fontSize = UIStyleTokens.Typography.CaptionMinimum;
            popoutHeaderText.fontStyle = FontStyles.Bold;
            popoutHeaderText.enableAutoSizing = false;
            popoutHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
            popoutHeaderText.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(popoutHeaderText);
            popoutHeaderText.color = UIStyleTokens.Text.Primary;
            popoutHeaderText.raycastTarget = false;

            var actionsObject = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionsObject.transform.SetParent(popoutHeaderRoot, false);
            popoutActionsRoot = actionsObject.GetComponent<RectTransform>();
            popoutActionsRoot.anchorMin = new Vector2(1f, 0f);
            popoutActionsRoot.anchorMax = new Vector2(1f, 1f);
            popoutActionsRoot.pivot = new Vector2(1f, 0.5f);
            popoutActionsRoot.anchoredPosition = new Vector2(-HeaderActionInset, 0f);
            popoutActionsRoot.sizeDelta = new Vector2(PopoutHeaderActionsWidth, 0f);

            var actionsLayout = actionsObject.GetComponent<HorizontalLayoutGroup>();
            actionsLayout.padding = new RectOffset(0, 0, 0, 0);
            actionsLayout.spacing = HeaderActionSpacing;
            actionsLayout.childAlignment = TextAnchor.MiddleRight;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = false;
            actionsLayout.childForceExpandHeight = false;

            // Clearing a log you cannot see is odd, so the prefab's Clear button
            // moves out of the sidebar strip and into the pop-out header.
            if (clearButton != null)
            {
                clearButton.transform.SetParent(popoutActionsRoot, false);
                ConfigureHeaderActionLayout(clearButton, PopoutActionButtonWidth);
            }

            popoutCloseButton = CreateButton(
                popoutActionsRoot,
                "CloseButton",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(PopoutActionButtonWidth, ActivityButtonHeight),
                out popoutCloseButtonLabel);
            ConfigureHeaderActionLayout(popoutCloseButton, PopoutActionButtonWidth);
            popoutCloseButton.transform.SetAsLastSibling();
            popoutCloseButton.onClick.AddListener(() => SetCollapsed(true));

            // The scroll view lives in the pop-out for good; the sidebar strip
            // only ever shows the header band.
            scrollViewRoot.SetParent(popoutRoot, false);
            scrollViewRoot.anchorMin = Vector2.zero;
            scrollViewRoot.anchorMax = Vector2.one;
            scrollViewRoot.pivot = new Vector2(0.5f, 0.5f);
            scrollViewRoot.offsetMin = new Vector2(PopoutContentInset, PopoutContentInset);
            scrollViewRoot.offsetMax = new Vector2(-PopoutContentInset, -(HeaderHeight + PopoutContentInset));
            scrollViewRoot.gameObject.SetActive(true);

            popoutRoot.gameObject.SetActive(false);
        }

        private bool ResolvePopoutContext()
        {
            if (hostCanvasRect != null && sidebarRect != null)
            {
                return true;
            }

            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                return false;
            }

            hostCanvasRect = canvas.rootCanvas.transform as RectTransform;
            if (hostCanvasRect == null)
            {
                return false;
            }

            // The sidebar is whichever ancestor sits directly under the canvas.
            Transform sidebar = transform;
            while (sidebar.parent != null && sidebar.parent != hostCanvasRect)
            {
                sidebar = sidebar.parent;
            }

            sidebarRect = sidebar as RectTransform;
            if (sidebarRect == null || sidebar.parent != hostCanvasRect)
            {
                sidebarRect = null;
                return false;
            }

            oppositeSidebarRect = null;
            for (int i = 0; i < hostCanvasRect.childCount; i++)
            {
                var sibling = hostCanvasRect.GetChild(i) as RectTransform;
                if (sibling != null && sibling != sidebarRect && sibling.GetComponent<SidebarResizer>() != null)
                {
                    oppositeSidebarRect = sibling;
                    break;
                }
            }

            return true;
        }

        // Corner extents of a canvas descendant, measured from the canvas's
        // bottom-left corner in canvas units.
        private void GetCanvasSpan(RectTransform target, out Vector2 min, out Vector2 max)
        {
            target.GetWorldCorners(cornerBuffer);
            Vector2 origin = hostCanvasRect.rect.min;
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < cornerBuffer.Length; i++)
            {
                Vector2 local = (Vector2)hostCanvasRect.InverseTransformPoint(cornerBuffer[i]) - origin;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }

        // Which way the pop-out opens. Positions are only trustworthy once the
        // canvas has been driven, so before its first layout (Awake-time label
        // setup) fall back to the sidebar's own anchors.
        private bool IsAnchoredToLeftSidebar()
        {
            if (!ResolvePopoutContext())
            {
                return true;
            }

            float canvasWidth = hostCanvasRect.rect.width;
            if (canvasWidth > 0f)
            {
                GetCanvasSpan(sidebarRect, out Vector2 sidebarMin, out Vector2 sidebarMax);
                return (sidebarMin.x + sidebarMax.x) * 0.5f <= canvasWidth * 0.5f;
            }

            return sidebarRect.anchorMin.x + sidebarRect.anchorMax.x <= 1f;
        }

        private void UpdatePopoutRect()
        {
            if (popoutRoot == null || !ResolvePopoutContext())
            {
                return;
            }

            Vector2 canvasSize = hostCanvasRect.rect.size;
            GetCanvasSpan(sidebarRect, out Vector2 sidebarMin, out Vector2 sidebarMax);
            float sidebarWidth = sidebarMax.x - sidebarMin.x;

            float oppositeWidth = sidebarWidth;
            if (oppositeSidebarRect != null)
            {
                GetCanvasSpan(oppositeSidebarRect, out Vector2 oppositeMin, out Vector2 oppositeMax);
                oppositeWidth = oppositeMax.x - oppositeMin.x;
            }

            bool anchorsToLeftSidebar = IsAnchoredToLeftSidebar();

            float playableWidth = Mathf.Max(0f, canvasSize.x - sidebarWidth - oppositeWidth);
            float width = Mathf.Max(PopoutMinimumWidth, playableWidth * PopoutPlayableWidthFraction);
            float x = anchorsToLeftSidebar ? sidebarMax.x : sidebarMin.x - width;
            float bottom = Mathf.Max(0f, sidebarMin.y) + PopoutVerticalMargin;
            float top = Mathf.Min(canvasSize.y, sidebarMax.y) - PopoutVerticalMargin;

            var position = new Vector2(x, bottom);
            var size = new Vector2(width, Mathf.Max(0f, top - bottom));
            if (popoutRoot.anchoredPosition != position)
                popoutRoot.anchoredPosition = position;
            if (popoutRoot.sizeDelta != size)
                popoutRoot.sizeDelta = size;
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
            UpdateCollapsedVisuals();

            if (!isCollapsed)
            {
                unseenEntryCount = 0;
                UpdatePopoutRect();
                RefreshEntryHeights();
                QueueBottomScrollFollowup();
            }

            QueueLayoutRefresh();
            UpdateUnseenIndicators();
        }

        // Entries added while the pop-out was hidden measured their wrapped
        // height against a stale width, so re-measure them once it is on screen.
        private void RefreshEntryHeights()
        {
            if (popoutRoot == null || !popoutRoot.gameObject.activeInHierarchy)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(popoutRoot);
            for (int i = 0; i < entryUIs.Count; i++)
            {
                if (entryUIs[i] != null)
                    entryUIs[i].RecalculateHeight();
            }
        }

        private void UpdateCollapsedVisuals()
        {
            if (popoutRoot != null)
                popoutRoot.gameObject.SetActive(!isCollapsed);

            if (topActionRowRoot != null)
                topActionRowRoot.gameObject.SetActive(topActionRequestedVisible);

            ApplySidebarStripLayout();
            UpdateUnseenIndicators();
        }

        private void ApplySidebarStripLayout()
        {
            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            // The strip is the sidebar's flexible child: it absorbs the slack so
            // the header sits at the bottom edge, and its surface matches the
            // sidebar so the empty stretch above the band is invisible.
            float height = HeaderHeight + (topActionRequestedVisible ? TopActionRowHeight + TopActionRowSpacing : 0f);
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 1f;
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

            UpdateUnseenIndicators();
        }

        // The unread count surfaces on whichever control can reveal it: the
        // sidebar toggle while the pop-out is closed, the Latest button while
        // it is open but scrolled away from the bottom. Chevrons point the way
        // the pop-out opens (away from its sidebar) and back again to close.
        private void UpdateUnseenIndicators()
        {
            if (collapseButtonLabel != null || popoutCloseButtonLabel != null)
            {
                bool opensRightward = IsAnchoredToLeftSidebar();
                string show = unseenEntryCount > 0 ? $"Show ({unseenEntryCount})" : "Show";
                if (collapseButtonLabel != null)
                {
                    collapseButtonLabel.text = isCollapsed
                        ? (opensRightward ? $"{show} ›" : $"‹ {show}")
                        : (opensRightward ? "‹ Hide" : "Hide ›");
                }

                if (popoutCloseButtonLabel != null)
                {
                    popoutCloseButtonLabel.text = opensRightward ? "‹ Close" : "Close ›";
                }
            }

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
    }
}
