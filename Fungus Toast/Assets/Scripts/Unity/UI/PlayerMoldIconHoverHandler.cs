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
            //Debug.Log($"👆 Hovering over mold icon for Player {playerId}");
            gridVisualizer.HighlightPlayerTiles(playerId, true); // Include starting tile ping
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log($"👋 Mouse exited mold icon for Player {playerId}");
            gridVisualizer.ClearHighlights();
        }
    }

}
