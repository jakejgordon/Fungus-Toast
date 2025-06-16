using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.Grid; // Needed for GridVisualizer

namespace FungusToast.Unity.UI
{
    public class PlayerSummaryRow : MonoBehaviour
    {
        [SerializeField] private Image moldIconImage;
        [SerializeField] private TextMeshProUGUI livingCellsText;
        [SerializeField] private TextMeshProUGUI deadCellsText;

        // Add this field to keep reference if needed
        private PlayerMoldIconHoverHandler hoverHandler;

        /// <summary>
        /// Sets the mold icon sprite.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (moldIconImage != null)
                moldIconImage.sprite = sprite;
        }

        /// <summary>
        /// Sets the living and dead cell counts.
        /// </summary>
        public void SetCounts(string living, string dead)
        {
            if (livingCellsText != null)
                livingCellsText.text = living; // No label, just the number
            if (deadCellsText != null)
                deadCellsText.text = dead;     // No label, just the number
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
        }
    }
}
