using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class TileSelectionController : MonoBehaviour
    {
        public static TileSelectionController Instance { get; private set; }

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

            // Highlight valid tiles
            var highlighter = FindAnyObjectByType<FungusToast.Unity.Grid.TileHoverHighlighter>();
            if (highlighter != null)
                highlighter.SetSelectionTiles(selectableTileIds);

            // (Optional) Block other UI as needed
        }

        /// <summary>
        /// Call this when a tile is clicked. Only acts if selection is active and tile is valid.
        /// </summary>
        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId)) return;

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive)
            {
                selectionActive = false;
                ClearHighlight();
                if (onCellSelected != null)
                    onCellSelected(cell);
                Reset();
            }
        }

        /// <summary>
        /// Call this to cancel selection.
        /// </summary>
        public void CancelSelection()
        {
            if (!selectionActive) return;
            selectionActive = false;
            ClearHighlight();
            if (onCancelled != null)
                onCancelled();
            Reset();
        }

        private void ClearHighlight()
        {
            var highlighter = FindAnyObjectByType<FungusToast.Unity.Grid.TileHoverHighlighter>();
            if (highlighter != null)
                highlighter.ClearSelectionTiles();
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
