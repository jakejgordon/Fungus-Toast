using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public enum BoardOverlayLegendType
    {
        ResistanceShield,
        Toxin,
        DeadCell,
        Chemobeacon,
        AdaptogenPatch,
        SporemealPatch,
        HypervariationPatch
    }

    public class PlayerMoldIconHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public int playerId;
        public GridVisualizer gridVisualizer;

        public void Initialize(int hoveredPlayerId, GridVisualizer visualizer)
        {
            playerId = hoveredPlayerId;
            gridVisualizer = visualizer;
            enabled = playerId >= 0 && gridVisualizer != null;
        }

        public static PlayerMoldIconHoverHandler Attach(GameObject target, int hoveredPlayerId, GridVisualizer visualizer)
        {
            if (target == null)
            {
                return null;
            }

            var handler = target.GetComponent<PlayerMoldIconHoverHandler>();
            if (handler == null)
            {
                handler = target.AddComponent<PlayerMoldIconHoverHandler>();
            }

            handler.Initialize(hoveredPlayerId, visualizer);

            if (target.TryGetComponent<Graphic>(out var graphic))
            {
                graphic.raycastTarget = handler.enabled;
            }

            return handler;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Disable highlight while draft is active to avoid clobbering draft selection highlights
            if (!enabled || gridVisualizer == null)
                return;

            if (FungusToast.Unity.GameManager.Instance != null && FungusToast.Unity.GameManager.Instance.IsDraftPhaseActive)
                return;

            gridVisualizer.HighlightPlayerTiles(playerId);
            gridVisualizer.StartStartingTileHoverPing(playerId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Do not clear highlights during draft - draft UI controls highlights then
            if (!enabled || gridVisualizer == null)
                return;

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

            switch (overlayType)
            {
                case BoardOverlayLegendType.Chemobeacon:
                    gridVisualizer.TriggerChemobeaconPing();
                    break;
                case BoardOverlayLegendType.AdaptogenPatch:
                    gridVisualizer.TriggerNutrientPatchPing(NutrientPatchType.Adaptogen);
                    break;
                case BoardOverlayLegendType.SporemealPatch:
                    gridVisualizer.TriggerNutrientPatchPing(NutrientPatchType.Sporemeal);
                    break;
                case BoardOverlayLegendType.HypervariationPatch:
                    gridVisualizer.TriggerNutrientPatchPing(NutrientPatchType.Hypervariation);
                    break;
            }
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
                BoardOverlayLegendType.AdaptogenPatch => board.AllNutrientPatchTiles()
                    .Where(tile => tile.NutrientPatch?.PatchType == NutrientPatchType.Adaptogen)
                    .Select(tile => tile.TileId),
                BoardOverlayLegendType.SporemealPatch => board.AllNutrientPatchTiles()
                    .Where(tile => tile.NutrientPatch?.PatchType == NutrientPatchType.Sporemeal)
                    .Select(tile => tile.TileId),
                BoardOverlayLegendType.HypervariationPatch => board.AllNutrientPatchTiles()
                    .Where(tile => tile.NutrientPatch?.PatchType == NutrientPatchType.Hypervariation)
                    .Select(tile => tile.TileId),
                _ => Enumerable.Empty<int>()
            };
        }
    }
}
