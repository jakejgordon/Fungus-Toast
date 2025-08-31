using FungusToast.Core.Players;
using FungusToast.Unity.Grid; // Needed for GridVisualizer
using FungusToast.Unity.UI.MycovariantDraft;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;

namespace FungusToast.Unity.UI
{
    public class PlayerSummaryRow : MonoBehaviour
    {
        [SerializeField] private Image moldIconImage;
        [SerializeField] private TextMeshProUGUI livingCellsText;
        [SerializeField] private TextMeshProUGUI deadCellsText;
        [SerializeField] private TextMeshProUGUI toxinCellsText;
        [SerializeField] private Transform mycovariantContainer;
        [SerializeField] private MycovariantIcon mycovariantIconPrefab;

        // Add this field to keep reference if needed
        private PlayerMoldIconHoverHandler hoverHandler;

        public int PlayerId { get; set; } // <-- Add this property

        /// <summary>
        /// Sets the mold icon sprite.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (moldIconImage != null)
                moldIconImage.sprite = sprite;
        }

        public void SetCounts(string living, string dead, string toxins)
        {
            if (livingCellsText != null)
                livingCellsText.text = living; // No label, just the number
            if (deadCellsText != null)
                deadCellsText.text = dead;     // No label, just the number
            if (toxinCellsText != null)
                toxinCellsText.text = toxins;
        }


        /// <summary>
        /// Call this after instantiating the row to wire up hover highlighting!
        /// </summary>
        public void SetHoverHighlight(int playerId, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            if (moldIconImage == null)
                return;

            // Add or reuse the hover handler
            hoverHandler = moldIconImage.GetComponent<PlayerMoldIconHoverHandler>();
            if (hoverHandler == null)
                hoverHandler = moldIconImage.gameObject.AddComponent<PlayerMoldIconHoverHandler>();

            hoverHandler.playerId = playerId;
            hoverHandler.gridVisualizer = gridVisualizer;

            // --- Wire tooltip provider on the icon ---
            var tooltipTrigger = moldIconImage.GetComponent<TooltipTrigger>();
            if (tooltipTrigger == null)
                tooltipTrigger = moldIconImage.gameObject.AddComponent<TooltipTrigger>();

            var provider = moldIconImage.GetComponent<PlayerSummaryTooltipProvider>();
            if (provider == null)
                provider = moldIconImage.gameObject.AddComponent<PlayerSummaryTooltipProvider>();

            // Resolve the Player instance from the GameManager's board
            var board = GameManager.Instance?.Board;
            var players = board?.Players;
            if (players != null)
            {
                var player = players.Find(p => p.PlayerId == playerId);
                if (player != null)
                {
                    provider.Initialize(player, players);
                    tooltipTrigger.SetDynamicProvider(provider);
                }
            }
        }

        public void UpdateMycovariants(IReadOnlyList<PlayerMycovariant> mycovariants)
        {
            // Clear old icons
            foreach (Transform child in mycovariantContainer)
                Destroy(child.gameObject);

            // Add new icons
            foreach (var myco in mycovariants)
            {
                var icon = Instantiate(mycovariantIconPrefab, mycovariantContainer);
                icon.SetMycovariant(myco);
            }
        }

    }
}
