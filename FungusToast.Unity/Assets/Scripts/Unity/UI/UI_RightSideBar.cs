using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid; // Needed for GridVisualizer
using FungusToast.Core.Config;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders; // ensure provider namespace is imported
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        private const string ScoreboardCoachmarkSeenKey = "Onboarding.ScoreboardWinConditionSeen";
        private const string ScoreboardCoachmarkTitleText = "How to Win";
        private const string ScoreboardCoachmarkBodyText = "This scoreboard is the clearest way to see who is ahead.\n\nWatch the Alive column. When the toast fills up and the game ends, the colony with the most living cells wins.";
        private const float TopStatsScale = 1.18f;
        private const float SummaryHeaderScale = 1.10f;
        private const float SummaryIconColumnWidth = 50f;
        private const float SummaryStatColumnWidth = 90f;
        private const int SummaryHorizontalInset = 15;

        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;
        [SerializeField] private TextMeshProUGUI endgameCountdownText;
        [SerializeField] private TextMeshProUGUI roundAndOccupancyText;
        [Header("Dynamic Chances")]
        [SerializeField] private TextMeshProUGUI randomDecayChanceText; // NEW UI label (assign in prefab)

        // Add a GridVisualizer field and setter
        private GridVisualizer gridVisualizer;
        private GameBoard board;
        private int? perspectivePlayerId;
        private RectTransform scoreboardCoachmarkRoot;
        private CanvasGroup scoreboardCoachmarkCanvasGroup;
        private TextMeshProUGUI scoreboardCoachmarkTitleTextLabel;
        private TextMeshProUGUI scoreboardCoachmarkBodyTextLabel;
        private Button scoreboardCoachmarkCloseButton;
        private bool hasDismissedScoreboardCoachmarkThisGame;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();

        private void Awake()
        {
            ApplyStyle();
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
            }

            if (randomDecayChanceText != null)
            {
                randomDecayChanceText.color = UIStyleTokens.Text.Secondary;
                ApplyTextScale(randomDecayChanceText, TopStatsScale);
                ConfigureSingleLineAutosize(randomDecayChanceText);
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
            label.overflowMode = TextOverflowModes.Ellipsis;
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
            hasDismissedScoreboardCoachmarkThisGame = false;
            HideScoreboardCoachmarkImmediate(false);
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

        public void TryShowScoreboardWinConditionCoachmark(int currentRound)
        {
            if (currentRound < 2 || hasDismissedScoreboardCoachmarkThisGame)
            {
                return;
            }

            var gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            if (!forceFirstGame && PlayerPrefs.GetInt(ScoreboardCoachmarkSeenKey, 0) != 0)
            {
                return;
            }

            if (gameManager != null && gameManager.IsFastForwarding)
            {
                return;
            }

            EnsureScoreboardCoachmarkUi();
            if (scoreboardCoachmarkRoot == null || scoreboardCoachmarkCanvasGroup == null)
            {
                return;
            }

            scoreboardCoachmarkBodyTextLabel.text = ScoreboardCoachmarkBodyText;
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

            if (transform == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_ScoreboardCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(transform, false);

            scoreboardCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            scoreboardCoachmarkRoot.anchorMin = new Vector2(0f, 1f);
            scoreboardCoachmarkRoot.anchorMax = new Vector2(0f, 1f);
            scoreboardCoachmarkRoot.pivot = new Vector2(1f, 1f);
            scoreboardCoachmarkRoot.anchoredPosition = new Vector2(-16f, -160f);
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
            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.8f);
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
            scoreboardCoachmarkTitleTextLabel.text = ScoreboardCoachmarkTitleText;
            scoreboardCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            scoreboardCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            scoreboardCoachmarkTitleTextLabel.fontSize = 24f;
            scoreboardCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            scoreboardCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            scoreboardCoachmarkTitleTextLabel.overflowMode = TextOverflowModes.Ellipsis;
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
                PlayerPrefs.SetInt(ScoreboardCoachmarkSeenKey, 1);
                PlayerPrefs.Save();
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

        public void SetRoundAndOccupancy(int round, float occupancy)
        {
            string mycovariantDraftTiming = BuildMycovariantDraftTimingText(round);
            roundAndOccupancyText.text = $"<b>Round:</b> {round}\n<b>Occupancy:</b> {occupancy:F2}%\n<b>Mycovariant Draft:</b> {mycovariantDraftTiming}";
            UpdateRandomDecayChance(round); // update dynamic label each round update
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

        // NEW: update random decay chance label
        public void UpdateRandomDecayChance(int currentRound)
        {
            if (randomDecayChanceText == null) return; // optional safety
            float baseChance = GameBalance.BaseRandomDecayChance;
            float additional = GameBalance.GetAdditionalRandomDecayChance(currentRound);
            randomDecayChanceText.text = $"<b>Random Decay Chance:</b> {(baseChance * 100f):0.0}% (+{(additional * 100f):0.0}%)";
        }

        public void InitializeRandomDecayChanceTooltip(GameBoard board, Player perspectivePlayer)
        {
            if (randomDecayChanceText == null) return;
            var go = randomDecayChanceText.gameObject;
            var provider = go.GetComponent<RandomDecayChanceTooltipProvider>();
            if (provider == null)
                provider = go.AddComponent<RandomDecayChanceTooltipProvider>();
            provider.Initialize(board, perspectivePlayer);
            var trigger = go.GetComponent<TooltipTrigger>();
            if (trigger == null)
                trigger = go.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
        }
    }
}
