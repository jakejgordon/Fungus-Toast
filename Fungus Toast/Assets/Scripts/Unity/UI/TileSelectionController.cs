using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class TileSelectionController : MonoBehaviour
    {
        public static TileSelectionController Instance { get; private set; }

        [SerializeField] private GridVisualizer gridVisualizer;

        private Action<FungalCell> onCellSelected;
        private Action onCancelled;
        private int selectingPlayerId = -1;
        private HashSet<int> selectableTileIds = new HashSet<int>();
        private bool selectionActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this.gameObject);
            else
                Instance = this;

            if (gridVisualizer == null)
                throw new System.Exception($"{nameof(TileSelectionController)} requires a reference to GridVisualizer. Assign it in the Inspector.");
        }

        /// <summary>
        /// Prompts the player to select one of their living fungal cells.
        /// Highlights valid cells and waits for click.
        /// </summary>
        public void PromptSelectLivingCell(
            int playerId,
            Action<FungalCell> onSelected,
            Action onCancel = null)
        {
            selectingPlayerId = playerId;
            onCellSelected = onSelected;
            onCancelled = onCancel;
            selectionActive = true;

            // Find valid cells
            var validCells = GameManager.Instance.Board.GetAllCellsOwnedBy(playerId)
                .Where(c => c.IsAlive)
                .ToList();

            selectableTileIds = new HashSet<int>(validCells.Select(c => c.TileId));

            // Highlight valid tiles using GridVisualizer (always set!)
            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(1f, 0.2f, 0.8f, 1f),   // Pink pulse
                new Color(1f, 0.7f, 1f, 1f)      // Pinkish white
            );
        }


        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId)) return;

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive)
            {
                selectionActive = false;
                gridVisualizer.ClearHighlights();
                onCellSelected?.Invoke(cell);
                Reset();
            }
        }

        public void CancelSelection()
        {
            if (!selectionActive) return;
            selectionActive = false;
            gridVisualizer.ClearHighlights();
            onCancelled?.Invoke();
            Reset();
        }

        private void Reset()
        {
            selectingPlayerId = -1;
            onCellSelected = null;
            onCancelled = null;
            selectableTileIds.Clear();
        }

        /// <summary>
        /// Returns true if the given tile is currently selectable.
        /// </summary>
        public bool IsSelectable(int tileId)
        {
            return selectionActive && selectableTileIds.Contains(tileId);
        }
    }
}
