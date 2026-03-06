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
        private const float TopStatsScale = 1.18f;
        private const float SummaryHeaderScale = 1.10f;

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

            ApplyColumnWidth(headerRow.Find("UI_BlankPlayerMoldIconHeaderText"), 50f);
            ApplyColumnWidth(headerRow.Find("UI_AliveHeaderText"), 70f);
            ApplyColumnWidth(headerRow.Find("UI_DeadHeaderText"), 70f);
            ApplyColumnWidth(headerRow.Find("UI_ToxinHeaderText"), 70f);
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
