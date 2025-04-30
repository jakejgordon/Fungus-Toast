using UnityEngine;
using UnityEngine.EventSystems;
using FungusToast.Grid;

public class PlayerMoldIconHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int playerId;
    public GridVisualizer gridVisualizer;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"👆 Hovering over mold icon for Player {playerId}");
        gridVisualizer.HighlightPlayerTiles(playerId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"👋 Mouse exited mold icon for Player {playerId}");
        gridVisualizer.ClearHighlights();
    }
}
