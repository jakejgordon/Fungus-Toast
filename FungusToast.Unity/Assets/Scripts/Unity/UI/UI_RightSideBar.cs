using TMPro;
using UnityEngine;
using System.Collections.Generic;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid; // Needed for GridVisualizer
using FungusToast.Core.Config;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders; // ensure provider namespace is imported

namespace FungusToast.Unity.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;
        [SerializeField] private TextMeshProUGUI endgameCountdownText;
        [SerializeField] private TextMeshProUGUI roundAndOccupancyText;
        [Header("Dynamic Chances")]
        [SerializeField] private TextMeshProUGUI randomDecayChanceText; // NEW UI label (assign in prefab)

        // Add a GridVisualizer field and setter
        private GridVisualizer gridVisualizer;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();

        // Add a way to provide GridVisualizer (call this in your GameManager or wherever you wire things up)
        public void SetGridVisualizer(GridVisualizer visualizer)
        {
            gridVisualizer = visualizer;
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
                row.SetCounts("1", "0", "0");
                playerSummaryRows[player.PlayerId] = row;

                // --- Wire up the hover handler on the icon ---
                // (GridVisualizer must be set BEFORE calling this method)
                if (gridVisualizer != null)
                    row.SetHoverHighlight(player.PlayerId, gridVisualizer);
                else
                    Debug.LogWarning("GridVisualizer not set on UI_RightSidebar; hover highlights will not work!");
            }
        }

        public void UpdatePlayerSummaries(List<Player> players)
        {
            // Use optimized single-pass board summary calculation
            var boardSummaries = FungusToast.Core.Board.BoardUtilities.GetPlayerBoardSummaries(players, GameManager.Instance.Board);
            
            foreach (Player player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    var summary = boardSummaries[player.PlayerId];
                    row.SetCounts(
                        summary.LivingCells.ToString(), 
                        summary.DeadCells.ToString(), 
                        summary.ToxinCells.ToString()
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
            // Use optimized single-pass board summary calculation (same as UpdatePlayerSummaries)
            var boardSummaries = FungusToast.Core.Board.BoardUtilities.GetPlayerBoardSummaries(players, GameManager.Instance.Board);
            
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
            // Show as: "Round: 7 | Occupancy: 32.7%"
            roundAndOccupancyText.text = $"<b>Round:</b> {round}   <b>Occupancy:</b> {occupancy:F2}%";
            UpdateRandomDecayChance(round); // update dynamic label each round update
        }

        // NEW: update random decay chance label
        public void UpdateRandomDecayChance(int currentRound)
        {
            if (randomDecayChanceText == null) return; // optional safety
            float baseChance = GameBalance.BaseRandomDecayChance;
            // Fallback/local duplicates of core constants to avoid version skew
            const int ScalingStartRoundLocal = 10; // must match GameBalance.RandomDecayScalingStartRound
            const float AdditionalPerRoundLocal = 0.001f; // must match GameBalance.RandomDecayAdditionalChancePerRound
            float additional = currentRound >= ScalingStartRoundLocal
                ? (currentRound - ScalingStartRoundLocal + 1) * AdditionalPerRoundLocal
                : 0f;
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
