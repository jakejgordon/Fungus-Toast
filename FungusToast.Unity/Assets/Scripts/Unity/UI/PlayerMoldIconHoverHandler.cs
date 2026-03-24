using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI
{
    public enum BoardOverlayLegendType
    {
        ResistanceShield,
        Toxin,
        DeadCell,
        Chemobeacon
    }

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

            RestoreSelectionHighlightsOrClear(gridVisualizer);
        }

        internal static void RestoreSelectionHighlightsOrClear(GridVisualizer gridVisualizer)
        {
            if (MultiCellSelectionController.Instance != null && MultiCellSelectionController.Instance.HasActiveSelection)
            {
                MultiCellSelectionController.Instance.ReapplySelectionHighlights();
                return;
            }

            if (MultiTileSelectionController.Instance != null && MultiTileSelectionController.Instance.HasActiveSelection)
            {
                MultiTileSelectionController.Instance.ReapplySelectionHighlights();
                return;
            }

            if (TileSelectionController.Instance != null && TileSelectionController.Instance.HasActiveSelection)
            {
                TileSelectionController.Instance.ReapplySelectionHighlights();
                return;
            }

            gridVisualizer?.ClearHighlights();
        }
    }

    public class BoardOverlayLegendHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BoardOverlayLegendType overlayType;
        private GridVisualizer gridVisualizer;

        public void Initialize(BoardOverlayLegendType type, GridVisualizer grid)
        {
            overlayType = type;
            gridVisualizer = grid;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (FungusToast.Unity.GameManager.Instance != null && FungusToast.Unity.GameManager.Instance.IsDraftPhaseActive)
            {
                return;
            }

            var board = gridVisualizer?.ActiveBoard;
            if (board == null)
            {
                return;
            }

            IEnumerable<int> tileIds = GetMatchingTileIds(board);
            gridVisualizer.HighlightTiles(tileIds);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (FungusToast.Unity.GameManager.Instance != null && FungusToast.Unity.GameManager.Instance.IsDraftPhaseActive)
            {
                return;
            }

            PlayerMoldIconHoverHandler.RestoreSelectionHighlightsOrClear(gridVisualizer);
        }

        private IEnumerable<int> GetMatchingTileIds(GameBoard board)
        {
            return overlayType switch
            {
                BoardOverlayLegendType.ResistanceShield => board.AllTiles()
                    .Where(tile => tile.FungalCell != null && tile.FungalCell.IsResistant)
                    .Select(tile => tile.TileId),
                BoardOverlayLegendType.Toxin => board.AllTiles()
                    .Where(tile => tile.FungalCell != null && tile.FungalCell.IsToxin)
                    .Select(tile => tile.TileId),
                BoardOverlayLegendType.DeadCell => board.AllTiles()
                    .Where(tile => tile.FungalCell != null && tile.FungalCell.IsDead)
                    .Select(tile => tile.TileId),
                BoardOverlayLegendType.Chemobeacon => board.GetActiveChemobeacons().Select(marker => marker.TileId),
                _ => Enumerable.Empty<int>()
            };
        }
    }
}
