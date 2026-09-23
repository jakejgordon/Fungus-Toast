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
using FungusToast.Unity.UI.PlayerInspector;
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
        private const float SummaryIconColumnWidth = 84f;
        private const float SummaryStatColumnWidth = 66f;
        // Use the spare right-side space for the final statistic rather than
        // leaving a conspicuous gutter after the toxin count.
        private const float SummaryToxinColumnWidth = 96f;
        private const float SummaryColumnSpacing = 6f;
        private const int SummaryHorizontalInset = 12;
        private const string TopControlsRowName = "UI_RightSidebarTopControlsRow";
        private const float TopControlsRowBottomPadding = 6f;
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
        private CoachmarkLayoutUtility.CoachmarkCard scoreboardCoachmark;
        private CoachmarkLayoutUtility.CoachmarkCard endgameCountdownCoachmark;
        private UI_GameLogPanel draftHistoryLogPanel;
        private Action onDraftHistoryRequested;
        private Func<bool> canOpenDraftHistory;
        private CoachmarkLayoutUtility.CoachmarkCard inspectPlayersCoachmark;
        private bool hasDismissedInspectPlayersCoachmarkThisGame;
        private bool hasPinnedPlayerInspectorThisGame;
        private bool hasDismissedScoreboardCoachmarkThisGame;
        private bool hasDismissedEndgameCountdownCoachmarkThisGame;
        private int lastDraftHistoryAttentionRound = -1;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();
        private RectTransform topControlsRowRect;

        private void Awake()
        {
            ApplyStyle();
            UpdateRoundAndOccupancyTooltip();
        }

        private void OnEnable()
        {
            PlayerInspectorPanel.Pinned += OnPlayerInspectorPinned;
        }

        private void OnDisable()
        {
            PlayerInspectorPanel.Pinned -= OnPlayerInspectorPinned;
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelPrimary);
            UIStyleTokens.ApplyPanelSurface(playerSummaryContainer != null ? playerSummaryContainer.gameObject : null, UIStyleTokens.Surface.PanelSecondary);
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

        /// <summary>
        /// The first row of the sidebar, above the phase tracker, where the persistent HUD
        /// controls (pace toggle on the left, pause menu on the right) live. Built on demand
        /// because the pause menu panel that owns those buttons is a runtime component.
        /// </summary>
        public RectTransform EnsureTopControlsRow()
        {
            if (topControlsRowRect != null)
            {
                return topControlsRowRect;
            }

            Transform layoutContainer = transform.Find("UI_RightSidebarLayoutContainer");
            if (layoutContainer == null)
            {
                return null;
            }

            Transform existing = layoutContainer.Find(TopControlsRowName);
            GameObject rowObject = existing != null
                ? existing.gameObject
                : new GameObject(TopControlsRowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.layer = layoutContainer.gameObject.layer;
            topControlsRowRect = rowObject.GetComponent<RectTransform>();
            topControlsRowRect.SetParent(layoutContainer, false);
            topControlsRowRect.SetSiblingIndex(0);

            float rowHeight = UIStyleTokens.Interaction.MinimumTargetSize + TopControlsRowBottomPadding;

            var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(0, 0, 0, Mathf.RoundToInt(TopControlsRowBottomPadding));
            rowLayout.spacing = 0f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var rowElement = rowObject.GetComponent<LayoutElement>();
            rowElement.minHeight = rowHeight;
            rowElement.preferredHeight = rowHeight;
            rowElement.flexibleHeight = 0f;
            rowElement.flexibleWidth = 1f;

            return topControlsRowRect;
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
                if (normalized.StartsWith("player"))
                {
                    label.fontSizeMax = Mathf.Min(label.fontSizeMax, 20f);
                    label.fontSizeMin = 12f;
                }
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
                headerLayout.spacing = SummaryColumnSpacing;
                headerLayout.childControlWidth = true;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandHeight = false;
            }

            ApplyColumnWidth(headerRow.Find("UI_BlankPlayerMoldIconHeaderText"), SummaryIconColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_AliveHeaderText"), SummaryStatColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_DeadHeaderText"), SummaryStatColumnWidth);
            ApplyColumnWidth(headerRow.Find("UI_ToxinHeaderText"), SummaryToxinColumnWidth);

            var identityHeader = headerRow.Find("UI_BlankPlayerMoldIconHeaderText")?.GetComponent<TextMeshProUGUI>();
            if (identityHeader != null)
            {
                identityHeader.text = "Player";
                identityHeader.alignment = TextAlignmentOptions.Midline;
            }
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
            hasDismissedInspectPlayersCoachmarkThisGame = false;
            hasPinnedPlayerInspectorThisGame = false;
            HideScoreboardCoachmarkImmediate(false);
            HideEndgameCountdownCoachmarkImmediate(false);
            HideInspectPlayersCoachmarkImmediate(false);
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
                row.SetPlayerIdentity(player.PlayerName);
                row.SetRank(playerSummaryRows.Count + 1);
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
            
            // Sort by alive descending, then dead descending, then player ID. The
            // final tie-break keeps hotseat and multiplayer rankings deterministic.
            rowPlayerPairs.Sort((a, b) => {
                int cmp = b.alive.CompareTo(a.alive);
                if (cmp != 0) return cmp;
                cmp = b.dead.CompareTo(a.dead);
                if (cmp != 0) return cmp;
                return a.player.PlayerId.CompareTo(b.player.PlayerId);
            });
            
            // Set sibling index (header stays at index 0)
            for (int i = 0; i < rowPlayerPairs.Count; i++)
            {
                var row = rowPlayerPairs[i].row;
                row.transform.SetSiblingIndex(i + 1); // +1 to keep header at index 0
                row.SetRank(i + 1);
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

            endgameCountdownCoachmark ??= BuildSidebarCoachmark(
                "UI_EndgameCountdownCoachmark", new Vector2(380f, 209f), OnEndgameCountdownCoachmarkDismissed);
            if (endgameCountdownCoachmark == null)
            {
                return;
            }

            endgameCountdownCoachmark.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.EndgameCountdownIntro));
            PositionEndgameCountdownCoachmark();
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

            scoreboardCoachmark ??= BuildSidebarCoachmark(
                "UI_ScoreboardCoachmark", new Vector2(360f, 194f), OnScoreboardCoachmarkDismissed);
            if (scoreboardCoachmark == null)
            {
                return;
            }

            scoreboardCoachmark.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.ScoreboardWinCondition));
            PositionScoreboardCoachmark();
        }

        /// <summary>
        /// Teaches the mold-icon inspector. Hover alone shows a text tooltip; the interactive
        /// panel with hoverable adaptation and mycovariant icons only appears on click, which is
        /// not discoverable without a nudge.
        /// </summary>
        public void TryShowInspectPlayersCoachmark(int currentRound)
        {
            var gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            if (!NewPlayerTooltipRules.ShouldShowInspectPlayersIntro(
                    forceFirstGame,
                    currentRound,
                    hasDismissedInspectPlayersCoachmarkThisGame,
                    hasPinnedPlayerInspectorThisGame,
                    isFastForwarding))
            {
                return;
            }

            inspectPlayersCoachmark ??= BuildSidebarCoachmark(
                "UI_InspectPlayersCoachmark", new Vector2(360f, 214f), OnInspectPlayersCoachmarkDismissed);
            if (inspectPlayersCoachmark == null)
            {
                return;
            }

            inspectPlayersCoachmark.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.InspectPlayersIntro));
            PositionInspectPlayersCoachmark();
        }

        /// <summary>
        /// The sidebar's coachmarks hang off its top-left corner, so they are placed by their
        /// top-right pivot and use the slightly larger type the sidebar has always used.
        /// </summary>
        private CoachmarkLayoutUtility.CoachmarkCard BuildSidebarCoachmark(string name, Vector2 size, Action onDismissed)
        {
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                return null;
            }

            return CoachmarkLayoutUtility.BuildCard(
                name,
                canvas.transform,
                size,
                onDismissed,
                titleFontSize: 24f,
                bodyFontSize: 19f,
                pivot: new Vector2(1f, 1f));
        }

        private void PositionInspectPlayersCoachmark()
        {
            if (inspectPlayersCoachmark == null || inspectPlayersCoachmark.Root == null || transform is not RectTransform sidebarRect)
            {
                return;
            }

            RectTransform parentRect = inspectPlayersCoachmark.Root.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (parentRect == null || canvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            sidebarRect.GetWorldCorners(corners);
            Vector3 topLeftWorld = corners[1];

            // Sits below the win-condition slot so the two never overlap if both are pending.
            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                inspectPlayersCoachmark.Root,
                parentRect,
                canvas,
                topLeftWorld,
                new Vector2(-16f, -240f),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        /// <summary>
        /// Pinning the inspector is the behaviour this coachmark teaches (hover alone is easy to
        /// stumble into), so doing it retires the hint for good rather than showing it next round.
        /// </summary>
        private void OnPlayerInspectorPinned()
        {
            hasPinnedPlayerInspectorThisGame = true;

            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.InspectPlayersIntro);
            }

            HideInspectPlayersCoachmarkImmediate(false);
        }

        private void OnInspectPlayersCoachmarkDismissed()
        {
            hasDismissedInspectPlayersCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.InspectPlayersIntro);
            }

            HideInspectPlayersCoachmarkImmediate(false);
        }

        private void HideInspectPlayersCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedInspectPlayersCoachmarkThisGame = false;
            }

            inspectPlayersCoachmark?.HideImmediate();
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

            scoreboardCoachmark?.HideImmediate();
        }

        private void PositionScoreboardCoachmark()
        {
            if (scoreboardCoachmark == null || scoreboardCoachmark.Root == null || transform is not RectTransform sidebarRect)
            {
                return;
            }

            RectTransform parentRect = scoreboardCoachmark.Root.parent as RectTransform;
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
                scoreboardCoachmark.Root,
                parentRect,
                canvas,
                topLeftWorld,
                new Vector2(-16f, -160f),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void PositionEndgameCountdownCoachmark()
        {
            if (endgameCountdownCoachmark == null || endgameCountdownCoachmark.Root == null || transform is not RectTransform sidebarRect)
            {
                return;
            }

            RectTransform parentRect = endgameCountdownCoachmark.Root.parent as RectTransform;
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
                endgameCountdownCoachmark.Root,
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

        /// <summary>
        /// The endgame card teaches the rule once; after a full round with it up, the sidebar
        /// countdown carries the information, so the card closes itself as if dismissed.
        /// </summary>
        public void RetireEndgameCountdownCoachmark()
        {
            if (endgameCountdownCoachmark != null && endgameCountdownCoachmark.IsVisible)
            {
                OnEndgameCountdownCoachmarkDismissed();
            }
        }

        private void HideEndgameCountdownCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedEndgameCountdownCoachmarkThisGame = false;
            }

            endgameCountdownCoachmark?.HideImmediate();
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
