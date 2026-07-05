using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid; // Needed for GridVisualizer
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Unity.UI.GameLog;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders; // ensure provider namespace is imported
using FungusToast.Unity.UI.Onboarding;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        private const string DraftHistoryButtonLabel = "View Draft Log";
        private const float RoundAndOccupancyBottomTrim = 10f;
        private const float PlayerSummaryRowSpacing = 5f;
        private const float TopStatsScale = 1.18f;
        private const float SummaryHeaderScale = 1.10f;
        private const float SummaryIconColumnWidth = 50f;
        private const float SummaryStatColumnWidth = 90f;
        private const int SummaryHorizontalInset = 15;
        private const float DraftHistoryAttentionDurationSeconds = 4f;

        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;
        [SerializeField] private TextMeshProUGUI endgameCountdownText;
        [SerializeField] private TextMeshProUGUI roundAndOccupancyText;

        // Add a GridVisualizer field and setter
        private GridVisualizer gridVisualizer;
        private GameBoard board;
        private int? perspectivePlayerId;
        private RectTransform scoreboardCoachmarkRoot;
        private CanvasGroup scoreboardCoachmarkCanvasGroup;
        private TextMeshProUGUI scoreboardCoachmarkTitleTextLabel;
        private TextMeshProUGUI scoreboardCoachmarkBodyTextLabel;
        private Button scoreboardCoachmarkCloseButton;
        private RectTransform endgameCountdownCoachmarkRoot;
        private CanvasGroup endgameCountdownCoachmarkCanvasGroup;
        private TextMeshProUGUI endgameCountdownCoachmarkTitleTextLabel;
        private TextMeshProUGUI endgameCountdownCoachmarkBodyTextLabel;
        private Button endgameCountdownCoachmarkCloseButton;
        private UI_GameLogPanel draftHistoryLogPanel;
        private Action onDraftHistoryRequested;
        private Func<bool> canOpenDraftHistory;
        private bool hasDismissedScoreboardCoachmarkThisGame;
        private bool hasDismissedEndgameCountdownCoachmarkThisGame;
        private int lastDraftHistoryAttentionRound = -1;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();

        private void Awake()
        {
            ApplyStyle();
            UpdateRoundAndOccupancyTooltip();
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelSecondary);
            UIStyleTokens.ApplyPanelSurface(playerSummaryContainer != null ? playerSummaryContainer.gameObject : null, UIStyleTokens.Surface.PanelElevated);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 30f);
            ApplySidebarLayoutBehavior();
            ApplyPlayerSummaryContainerPadding();

            if (roundAndOccupancyText != null)
            {
                roundAndOccupancyText.color = UIStyleTokens.Text.Primary;
                ApplyTextScale(roundAndOccupancyText, TopStatsScale);
                roundAndOccupancyText.fontStyle = FontStyles.Bold;
                ConfigureSingleLineAutosize(roundAndOccupancyText);
                ApplyRoundAndOccupancyLayout();
            }

            if (endgameCountdownText != null)
            {
                endgameCountdownText.color = UIStyleTokens.State.Warning;
                ApplyTextScale(endgameCountdownText, TopStatsScale);
                endgameCountdownText.fontStyle = FontStyles.Bold;
            }

            ApplyPlayerSummaryHeaderReadability();
        }

        private void ApplySidebarLayoutBehavior()
        {
            Transform layoutContainer = transform.Find("UI_RightSidebarLayoutContainer");
            if (layoutContainer is not RectTransform layoutRect)
            {
                return;
            }

            layoutRect.anchorMin = new Vector2(0f, 0f);
            layoutRect.anchorMax = new Vector2(1f, 1f);
            layoutRect.pivot = new Vector2(0.5f, 1f);
            layoutRect.offsetMin = Vector2.zero;
            layoutRect.offsetMax = Vector2.zero;

            var sizeFitter = layoutRect.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            var layoutGroup = layoutRect.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandHeight = false;
            }
        }

        private void ApplyPlayerSummaryContainerPadding()
        {
            if (playerSummaryContainer == null)
            {
                return;
            }

            var layoutGroup = playerSummaryContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                return;
            }

            layoutGroup.padding.left = SummaryHorizontalInset;
            layoutGroup.padding.right = SummaryHorizontalInset;
            layoutGroup.spacing = PlayerSummaryRowSpacing;
        }

        private void ApplyRoundAndOccupancyLayout()
        {
            if (roundAndOccupancyText == null)
            {
                return;
            }

            var layoutElement = roundAndOccupancyText.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = roundAndOccupancyText.gameObject.AddComponent<LayoutElement>();
            }

            roundAndOccupancyText.ForceMeshUpdate();

            float heightReduction = roundAndOccupancyText.textInfo.lineCount > 1
                ? RoundAndOccupancyBottomTrim
                : 0f;
            float preferredHeight = Mathf.Max(0f, roundAndOccupancyText.preferredHeight - heightReduction);

            layoutElement.minHeight = preferredHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = -1f;
        }

        private void ApplyPlayerSummaryHeaderReadability()
        {
            if (playerSummaryContainer == null) return;

            Transform headerRow = playerSummaryContainer.Find("UI_PlayerSummariesPanelHeaderRow");
            if (headerRow == null) return;

            AlignPlayerSummaryHeaderColumns(headerRow);

            var headerLabels = headerRow.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in headerLabels)
            {
                if (label == null) continue;
                label.color = UIStyleTokens.Text.Primary;
                label.fontStyle = FontStyles.Bold;
                ApplyTextScale(label, SummaryHeaderScale);
                ConfigureSingleLineAutosize(label);

                string normalized = label.text?.Trim().ToLowerInvariant() ?? string.Empty;
                if (normalized.StartsWith("alive") || normalized.StartsWith("dead") || normalized.StartsWith("toxin"))
                {
                    label.alignment = TextAlignmentOptions.Midline;
                }

            }
        }

        private static void AlignPlayerSummaryHeaderColumns(Transform headerRow)
        {
            if (headerRow == null)
            {
                return;
            }

            var headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout != null)
            {
                headerLayout.spacing = 10f;
                headerLayout.childControlWidth = true;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandHeight = false;
            }

            ApplyColumnWidth(headerRow.Find("UI_BlankPlayerMoldIconHeaderText"), SummaryIconColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_AliveHeaderText"), SummaryStatColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_DeadHeaderText"), SummaryStatColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_ToxinHeaderText"), SummaryStatColumnWidth);
        }

        private static void ApplyColumnWidth(Transform cell, float width)
        {
            if (cell == null)
            {
                return;
            }

            var layout = cell.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cell.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.flexibleWidth = -1f;
        }

        private static void ApplyTextScale(TextMeshProUGUI label, float scale)
        {
            if (label == null || scale <= 1f) return;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin *= scale;
                label.fontSizeMax *= scale;
            }
            else
            {
                label.fontSize *= scale;
            }
        }

        private static void ConfigureSingleLineAutosize(TextMeshProUGUI label)
        {
            if (label == null) return;

            float targetSize = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.enableAutoSizing = true;
            label.fontSizeMax = targetSize;
            label.fontSizeMin = Mathf.Max(10f, targetSize * 0.70f);
        }

        // Add a way to provide GridVisualizer (call this in your GameManager or wherever you wire things up)
        public void SetGridVisualizer(GridVisualizer visualizer)
        {
            gridVisualizer = visualizer;
        }

        public void SetBoard(GameBoard gameBoard)
        {
            board = gameBoard;
            lastDraftHistoryAttentionRound = -1;
            hasDismissedScoreboardCoachmarkThisGame = false;
            hasDismissedEndgameCountdownCoachmarkThisGame = false;
            HideScoreboardCoachmarkImmediate(false);
            HideEndgameCountdownCoachmarkImmediate(false);
            UpdateRoundAndOccupancyTooltip();
            RefreshDraftHistoryAvailability();
        }

        public void SetDraftHistoryHandler(Action onViewDraftHistoryRequested, Func<bool> canViewDraftHistory)
        {
            onDraftHistoryRequested = onViewDraftHistoryRequested;
            canOpenDraftHistory = canViewDraftHistory;
            EnsureDraftHistoryButtonUi();
            RefreshDraftHistoryAvailability();
        }

        public void RefreshDraftHistoryAvailability()
        {
            EnsureDraftHistoryButtonUi();
            if (draftHistoryLogPanel == null)
            {
                return;
            }

            bool isAvailable = onDraftHistoryRequested != null && canOpenDraftHistory?.Invoke() == true;
            draftHistoryLogPanel.ConfigureTopActionButton(
                DraftHistoryButtonLabel,
                isAvailable ? onDraftHistoryRequested : null,
                isAvailable);

            var gameManager = GameManager.Instance;
            int completedDraftRound = gameManager != null ? gameManager.LastCompletedMycovariantDraftRound : -1;
            int currentRound = board?.CurrentRound ?? -1;
            bool justCompletedThisRound = isAvailable && completedDraftRound >= 0 && completedDraftRound == currentRound;
            if (justCompletedThisRound && lastDraftHistoryAttentionRound != completedDraftRound)
            {
                draftHistoryLogPanel.TriggerTopActionAttention(DraftHistoryAttentionDurationSeconds);
                lastDraftHistoryAttentionRound = completedDraftRound;
            }
        }

        public void InitializePlayerSummaries(List<Player> players)
        {
            foreach (Transform child in playerSummaryContainer)
            {
                if (child.name == "UI_PlayerSummariesPanelHeaderRow")
                    continue;
                Destroy(child.gameObject);
            }

            playerSummaryRows.Clear();

            foreach (Player player in players)
            {
                GameObject rowGO = Instantiate(playerSummaryPrefab, playerSummaryContainer);
                rowGO.transform.localScale = Vector3.one;

                var row = rowGO.GetComponent<PlayerSummaryRow>();
                if (row == null)
                {
                    Debug.LogError("❌ PlayerSummaryRow component missing on prefab!");
                    continue;
                }

                row.PlayerId = player.PlayerId; // <-- Set PlayerId
                row.SetIcon(GameManager.Instance.GameUI.PlayerUIBinder.GetPlayerIcon(player.PlayerId));
                row.SetCounts(1, 0, 0);
                playerSummaryRows[player.PlayerId] = row;

                // --- Wire up the hover handler on the icon ---
                // (GridVisualizer must be set BEFORE calling this method)
                if (gridVisualizer != null)
                    row.SetHoverHighlight(player.PlayerId, gridVisualizer);
                else
                    Debug.LogWarning("GridVisualizer not set on UI_RightSidebar; hover highlights will not work!");

                row.SetPerspectivePlayer(perspectivePlayerId.HasValue && player.PlayerId == perspectivePlayerId.Value);
            }
        }

        public void SetPerspectivePlayer(Player perspectivePlayer)
        {
            perspectivePlayerId = perspectivePlayer?.PlayerId;
            ApplyPerspectiveHighlightState();
        }

        private void ApplyPerspectiveHighlightState()
        {
            foreach (var pair in playerSummaryRows)
            {
                bool isPerspective = perspectivePlayerId.HasValue && pair.Key == perspectivePlayerId.Value;
                pair.Value?.SetPerspectivePlayer(isPerspective);
            }
        }

        public void UpdatePlayerSummaries(List<Player> players)
        {
            if (board == null)
            {
                Debug.LogWarning("UI_RightSidebar board reference not set; cannot update player summaries.");
                return;
            }

            // Use optimized single-pass board summary calculation
            var boardSummaries = FungusToast.Core.Board.BoardUtilities.GetPlayerBoardSummaries(players, board);
            
            foreach (Player player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    var summary = boardSummaries[player.PlayerId];
                    row.SetCounts(
                        summary.LivingCells, 
                        summary.DeadCells, 
                        summary.ToxinCells
                    );
                }
            }
        }

        public void SortPlayerSummaryRows(List<Player> players)
        {
            SortAndAnimatePlayerSummaryRows(players);
        }

        // Add this method to sort and animate the rows
        private void SortAndAnimatePlayerSummaryRows(List<Player> players)
        {
            if (board == null)
            {
                Debug.LogWarning("UI_RightSidebar board reference not set; cannot sort player summaries.");
                return;
            }

            // Use optimized single-pass board summary calculation (same as UpdatePlayerSummaries)
            var boardSummaries = FungusToast.Core.Board.BoardUtilities.GetPlayerBoardSummaries(players, board);
            
            // Build a list of rows with their player data
            var rowPlayerPairs = new List<(PlayerSummaryRow row, Player player, int alive, int dead)>();
            foreach (var player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    var summary = boardSummaries[player.PlayerId];
                    rowPlayerPairs.Add((row, player, summary.LivingCells, summary.DeadCells));
                }
            }
            
            // Sort by alive descending, then dead descending
            rowPlayerPairs.Sort((a, b) => {
                int cmp = b.alive.CompareTo(a.alive);
                if (cmp != 0) return cmp;
                return b.dead.CompareTo(a.dead);
            });
            
            // Set sibling index (header stays at index 0)
            for (int i = 0; i < rowPlayerPairs.Count; i++)
            {
                var row = rowPlayerPairs[i].row;
                row.transform.SetSiblingIndex(i + 1); // +1 to keep header at index 0
            }
        }


        public void SetEndgameCountdownText(string message)
        {
            if (endgameCountdownText != null)
            {
                endgameCountdownText.text = message;
                endgameCountdownText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        public void TryShowEndgameCountdownCoachmark()
        {
            var gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            if (!NewPlayerTooltipRules.ShouldShowEndgameCountdownIntro(forceFirstGame, hasDismissedEndgameCountdownCoachmarkThisGame, isFastForwarding))
            {
                return;
            }

            EnsureEndgameCountdownCoachmarkUi();
            if (endgameCountdownCoachmarkRoot == null || endgameCountdownCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.EndgameCountdownIntro);
            endgameCountdownCoachmarkTitleTextLabel.text = definition.Title;
            endgameCountdownCoachmarkBodyTextLabel.text = definition.Body;
            PositionEndgameCountdownCoachmark();
            endgameCountdownCoachmarkRoot.gameObject.SetActive(true);
            endgameCountdownCoachmarkRoot.SetAsLastSibling();
            endgameCountdownCoachmarkCanvasGroup.alpha = 1f;
            endgameCountdownCoachmarkCanvasGroup.blocksRaycasts = true;
            endgameCountdownCoachmarkCanvasGroup.interactable = true;
        }

        public void TryShowScoreboardWinConditionCoachmark(int currentRound)
        {
            var gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            if (!NewPlayerTooltipRules.ShouldShowScoreboardWinCondition(
                    forceFirstGame,
                    currentRound,
                    hasDismissedScoreboardCoachmarkThisGame,
                    isFastForwarding))
            {
                return;
            }

            EnsureScoreboardCoachmarkUi();
            if (scoreboardCoachmarkRoot == null || scoreboardCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.ScoreboardWinCondition);
            scoreboardCoachmarkTitleTextLabel.text = definition.Title;
            scoreboardCoachmarkBodyTextLabel.text = definition.Body;
            PositionScoreboardCoachmark();
            scoreboardCoachmarkRoot.gameObject.SetActive(true);
            scoreboardCoachmarkRoot.SetAsLastSibling();
            scoreboardCoachmarkCanvasGroup.alpha = 1f;
            scoreboardCoachmarkCanvasGroup.blocksRaycasts = true;
            scoreboardCoachmarkCanvasGroup.interactable = true;
        }

        private void EnsureScoreboardCoachmarkUi()
        {
            if (scoreboardCoachmarkRoot != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_ScoreboardCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(canvas.transform, false);

            scoreboardCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            scoreboardCoachmarkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            scoreboardCoachmarkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            scoreboardCoachmarkRoot.pivot = new Vector2(1f, 1f);
            scoreboardCoachmarkRoot.anchoredPosition = Vector2.zero;
            scoreboardCoachmarkRoot.sizeDelta = new Vector2(360f, 190f);

            scoreboardCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            scoreboardCoachmarkCanvasGroup.alpha = 0f;
            scoreboardCoachmarkCanvasGroup.blocksRaycasts = false;
            scoreboardCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Info, 0.16f);
            backgroundColor.a = 0.98f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-52f, -12f);

            scoreboardCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            scoreboardCoachmarkTitleTextLabel.text = string.Empty;
            scoreboardCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            scoreboardCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            scoreboardCoachmarkTitleTextLabel.fontSize = 24f;
            scoreboardCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            scoreboardCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(scoreboardCoachmarkTitleTextLabel);
            scoreboardCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);

            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -50f);

            scoreboardCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            scoreboardCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            scoreboardCoachmarkBodyTextLabel.fontSize = 19f;
            scoreboardCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            scoreboardCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            scoreboardCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            scoreboardCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);

            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            scoreboardCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(scoreboardCoachmarkCloseButton);
            scoreboardCoachmarkCloseButton.onClick.RemoveAllListeners();
            scoreboardCoachmarkCloseButton.onClick.AddListener(OnScoreboardCoachmarkDismissed);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);

            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                scoreboardCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                scoreboardCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void OnScoreboardCoachmarkDismissed()
        {
            hasDismissedScoreboardCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.ScoreboardWinCondition);
            }

            HideScoreboardCoachmarkImmediate(false);
        }

        private void HideScoreboardCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedScoreboardCoachmarkThisGame = false;
            }

            if (scoreboardCoachmarkCanvasGroup != null)
            {
                scoreboardCoachmarkCanvasGroup.alpha = 0f;
                scoreboardCoachmarkCanvasGroup.blocksRaycasts = false;
                scoreboardCoachmarkCanvasGroup.interactable = false;
            }

            if (scoreboardCoachmarkRoot != null)
            {
                scoreboardCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureEndgameCountdownCoachmarkUi()
        {
            if (endgameCountdownCoachmarkRoot != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_EndgameCountdownCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(canvas.transform, false);

            endgameCountdownCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            endgameCountdownCoachmarkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            endgameCountdownCoachmarkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            endgameCountdownCoachmarkRoot.pivot = new Vector2(1f, 1f);
            endgameCountdownCoachmarkRoot.anchoredPosition = Vector2.zero;
            endgameCountdownCoachmarkRoot.sizeDelta = new Vector2(380f, 205f);

            endgameCountdownCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            endgameCountdownCoachmarkCanvasGroup.alpha = 0f;
            endgameCountdownCoachmarkCanvasGroup.blocksRaycasts = false;
            endgameCountdownCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Warning, 0.18f);
            backgroundColor.a = 0.98f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-52f, -12f);

            endgameCountdownCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            endgameCountdownCoachmarkTitleTextLabel.text = string.Empty;
            endgameCountdownCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            endgameCountdownCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            endgameCountdownCoachmarkTitleTextLabel.fontSize = 24f;
            endgameCountdownCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            endgameCountdownCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(endgameCountdownCoachmarkTitleTextLabel);
            endgameCountdownCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -50f);

            endgameCountdownCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            endgameCountdownCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            endgameCountdownCoachmarkBodyTextLabel.fontSize = 18f;
            endgameCountdownCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            endgameCountdownCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            endgameCountdownCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            endgameCountdownCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            endgameCountdownCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(endgameCountdownCoachmarkCloseButton);
            endgameCountdownCoachmarkCloseButton.onClick.RemoveAllListeners();
            endgameCountdownCoachmarkCloseButton.onClick.AddListener(OnEndgameCountdownCoachmarkDismissed);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);
            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                endgameCountdownCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                endgameCountdownCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void PositionScoreboardCoachmark()
        {
            if (scoreboardCoachmarkRoot == null || transform is not RectTransform sidebarRect)
            {
                return;
            }

            RectTransform parentRect = scoreboardCoachmarkRoot.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (parentRect == null || canvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            sidebarRect.GetWorldCorners(corners);
            Vector3 topLeftWorld = corners[1];

            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                scoreboardCoachmarkRoot,
                parentRect,
                canvas,
                topLeftWorld,
                new Vector2(-16f, -160f),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void PositionEndgameCountdownCoachmark()
        {
            if (endgameCountdownCoachmarkRoot == null || transform is not RectTransform sidebarRect)
            {
                return;
            }

            RectTransform parentRect = endgameCountdownCoachmarkRoot.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (parentRect == null || canvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            sidebarRect.GetWorldCorners(corners);
            Vector3 topLeftWorld = corners[1];

            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                endgameCountdownCoachmarkRoot,
                parentRect,
                canvas,
                topLeftWorld,
                new Vector2(-16f, -52f),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void OnEndgameCountdownCoachmarkDismissed()
        {
            hasDismissedEndgameCountdownCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.EndgameCountdownIntro);
            }

            HideEndgameCountdownCoachmarkImmediate(false);
        }

        private void HideEndgameCountdownCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedEndgameCountdownCoachmarkThisGame = false;
            }

            if (endgameCountdownCoachmarkCanvasGroup != null)
            {
                endgameCountdownCoachmarkCanvasGroup.alpha = 0f;
                endgameCountdownCoachmarkCanvasGroup.blocksRaycasts = false;
                endgameCountdownCoachmarkCanvasGroup.interactable = false;
            }

            if (endgameCountdownCoachmarkRoot != null)
            {
                endgameCountdownCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        public void SetRoundAndOccupancy(int round, float occupancy)
        {
            string mycovariantDraftTiming = BuildMycovariantDraftTimingText(round);
            roundAndOccupancyText.text = $"<b>Round:</b> {round}\n<b>Occupancy:</b> {occupancy:F2}%\n<b>Mycovariant Draft:</b> {mycovariantDraftTiming}";
            ApplyRoundAndOccupancyLayout();
            RefreshDraftHistoryAvailability();
        }

        private void EnsureDraftHistoryButtonUi()
        {
            if (draftHistoryLogPanel != null || roundAndOccupancyText == null)
            {
                return;
            }

            Transform parent = roundAndOccupancyText.transform.parent != null
                ? roundAndOccupancyText.transform.parent
                : transform;

            draftHistoryLogPanel = parent
                .GetComponentsInChildren<UI_GameLogPanel>(true)
                .FirstOrDefault(panel => panel.transform.parent == parent);
        }

        private void UpdateRoundAndOccupancyTooltip()
        {
            if (roundAndOccupancyText == null)
            {
                return;
            }

            var trigger = roundAndOccupancyText.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                return;
            }

            int boardArea = board?.PlayableTileCount ?? (GameBalance.BoardWidth * GameBalance.BoardHeight);
            float threshold = GameBalance.GetGameEndTileOccupancyThreshold(boardArea);
            trigger.SetStaticText(
                $"The Round number increases after each Decay Phase. Occupancy % represents the percentage of the board that is occupied. Once the board reaches {threshold:P0} occupancy, a {GameBalance.TurnsAfterEndGameTileOccupancyThresholdMet}-round end-of-game countdown starts.");
        }

        private static string BuildMycovariantDraftTimingText(int currentRound)
        {
            var gameManager = GameManager.Instance;
            bool isDraftPhaseActive = gameManager != null && gameManager.IsDraftPhaseActive;
            bool draftCompletedThisRound = gameManager != null && gameManager.LastCompletedMycovariantDraftRound == currentRound;

            if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(currentRound) && (isDraftPhaseActive || !draftCompletedThisRound))
            {
                return "Now";
            }

            int? nextDraftRound = MycovariantGameBalance.MycovariantSelectionTriggerRounds
                .Where(triggerRound => triggerRound > currentRound)
                .OrderBy(triggerRound => triggerRound)
                .Select(triggerRound => (int?)triggerRound)
                .FirstOrDefault();

            if (!nextDraftRound.HasValue)
            {
                return "No upcoming draft";
            }

            int roundsRemaining = nextDraftRound.Value - currentRound;
            return $"Round {nextDraftRound.Value} (in {roundsRemaining})";
        }
    }
}
