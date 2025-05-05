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

        private Dictionary<int, GameObject> playerSummaryRows = new();

        public void InitializePlayerSummaries(List<Player> players)
        {
            foreach (Transform child in playerSummaryContainer)
                Destroy(child.gameObject);

            playerSummaryRows.Clear();

            foreach (Player player in players)
            {
                GameObject row = Instantiate(playerSummaryPrefab, playerSummaryContainer);
                row.transform.localScale = Vector3.one;
                playerSummaryRows[player.PlayerId] = row;

                var icon = row.transform.Find("UI_MoldIconImage").GetComponent<Image>();
                var living = row.transform.Find("UI_LivingCellsText").GetComponent<TextMeshProUGUI>();
                var dead = row.transform.Find("UI_DeadCellsText").GetComponent<TextMeshProUGUI>();

                icon.sprite = GameManager.Instance.GameUI.PlayerUIBinder.GetPlayerIcon(player.PlayerId);
                living.text = "Alive: ?";
                dead.text = "Dead: ?";
            }
        }

        public void UpdatePlayerSummaries(List<Player> players)
        {
            foreach (Player player in players)
            {
                if (playerSummaryRows.TryGetValue(player.PlayerId, out var row))
                {
                    int alive = player.ControlledTileIds.Count;
                    int dead = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId).Count - alive;

                    row.transform.Find("UI_LivingCellsText").GetComponent<TextMeshProUGUI>().text = $"Alive: {alive}";
                    row.transform.Find("UI_DeadCellsText").GetComponent<TextMeshProUGUI>().text = $"Dead: {dead}";
                }
            }
        }
    }
}