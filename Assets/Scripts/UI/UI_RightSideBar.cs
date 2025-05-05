using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FungusToast.Core.Players;
using FungusToast.Game;

namespace FungusToast.UI
{
    public class UI_RightSidebar : MonoBehaviour
    {
        [Header("Player Summary Panel")]
        [SerializeField] private Transform playerSummaryContainer;
        [SerializeField] private GameObject playerSummaryPrefab;

        private Dictionary<int, PlayerSummaryRow> playerSummaryRows = new();

        public void InitializePlayerSummaries(List<Player> players)
        {
            foreach (Transform child in playerSummaryContainer)
                Destroy(child.gameObject);

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
                row.SetCounts("?", "?");
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
                        if (cell.IsAlive) alive++;
                        else dead++;
                    }

                    row.SetCounts(alive.ToString(), dead.ToString());
                }
            }
        }


    }
}
