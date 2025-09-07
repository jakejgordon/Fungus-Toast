using UnityEngine;
using UnityEngine.EventSystems;
using FungusToast.Unity.Grid;

namespace FungusToast.Unity.UI
{
    public class PlayerMoldIconHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public int playerId;
        public GridVisualizer gridVisualizer;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Disable highlight while draft is active to avoid clobbering draft selection highlights
            if (FungusToast.Unity.GameManager.Instance != null && FungusToast.Unity.GameManager.Instance.IsDraftPhaseActive)
                return;

            gridVisualizer.HighlightPlayerTiles(playerId, true); // Include starting tile ping
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Do not clear highlights during draft - draft UI controls highlights then
            if (FungusToast.Unity.GameManager.Instance != null && FungusToast.Unity.GameManager.Instance.IsDraftPhaseActive)
                return;

            gridVisualizer.ClearHighlights();
        }
    }

}
