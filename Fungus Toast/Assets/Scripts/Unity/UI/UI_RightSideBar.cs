using TMPro;
using UnityEngine;
using System.Collections.Generic;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid; // Needed for GridVisualizer

namespace FungusToast.Unity.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;
        [SerializeField] private TextMeshProUGUI endgameCountdownText;
        [SerializeField] private TextMeshProUGUI roundAndOccupancyText;

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
            foreach (Player player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    int alive = 0;
                    int dead = 0;
                    int toxins = 0;

                    foreach (var cell in GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId))
                    {
                        if (cell.IsAlive)
                        {
                            alive++;
                        }
                        else if (cell.IsDead) // Only count as dead if truly dead
                        {
                            dead++;
                        }
                        else if (cell.IsToxin)
                        {
                            toxins++;
                        }
                    }

                    row.SetCounts(alive.ToString(), dead.ToString(), toxins.ToString());
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
            // Build a list of rows with their player data
            var rowPlayerPairs = new List<(PlayerSummaryRow row, Player player, int alive, int dead)>();
            foreach (var player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    int alive = 0;
                    int dead = 0;
                    foreach (var cell in GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId))
                    {
                        if (cell.IsAlive) alive++;
                        else if (cell.IsDead) dead++;
                    }
                    rowPlayerPairs.Add((row, player, alive, dead));
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
        }

    }
}
