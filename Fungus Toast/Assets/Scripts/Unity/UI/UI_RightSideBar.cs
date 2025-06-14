using TMPro;
using UnityEngine;
using System.Collections.Generic;
using FungusToast.Core.Players;

namespace FungusToast.Unity.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;
        [SerializeField] private TextMeshProUGUI endgameCountdownText;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();

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

                row.SetIcon(GameManager.Instance.GameUI.PlayerUIBinder.GetPlayerIcon(player.PlayerId));
                row.SetCounts("1", "0");
                playerSummaryRows[player.PlayerId] = row;
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
                        // else (e.g., toxin tiles) are ignored for dead count
                    }

                    row.SetCounts(alive.ToString(), dead.ToString());
                }
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
    }
}
