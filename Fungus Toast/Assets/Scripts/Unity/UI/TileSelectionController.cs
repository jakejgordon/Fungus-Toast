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
        private Action<int> onTileSelected; // For generic board tile selection

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
        /// Shows an instructional prompt if provided.
        /// </summary>
        public void PromptSelectLivingCell(
            int playerId,
            Action<FungalCell> onSelected,
            Action onCancel = null,
            string promptMessage = null)
        {
            selectingPlayerId = playerId;
            selectionActive = true;

            // Show the prompt if a message was provided
            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            // Wraps to clear the prompt on cell selection or cancel
            onCellSelected = (cell) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onSelected?.Invoke(cell);
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };

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

        /// <summary>
        /// Prompts the player to select any board tile matching a predicate.
        /// Highlights valid tiles and waits for click.
        /// </summary>
        public void PromptSelectBoardTile(
            Func<BoardTile, bool> isValidTile,
            Action<BoardTile> onSelected,
            Action onCancel = null,
            string promptMessage = null)
        {
            selectionActive = true;

            // Show the prompt if a message was provided
            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            // Wraps to clear the prompt on tile selection or cancel
            Action<int> onTileSelected = (tileId) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                var tile = GameManager.Instance.Board.GetTileById(tileId);
                onSelected?.Invoke(tile);
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };

            // Find valid tiles
            var validTiles = GameManager.Instance.Board.AllTiles()
                .Where(isValidTile)
                .ToList();
            selectableTileIds = new HashSet<int>(validTiles.Select(t => t.TileId));

            // Highlight valid tiles
            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(0.2f, 0.8f, 1f, 1f),   // Cyan pulse
                new Color(0.7f, 1f, 1f, 1f)      // Light cyan
            );

            // Override OnTileClicked for this selection
            onCellSelected = null; // Not used for BoardTile
            this.onTileSelected = onTileSelected;
        }

        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId))
            {
                if (!selectionActive)
                    Debug.LogWarning($"TileSelectionController.OnTileClicked called when selection is not active. TileId: {tileId}");
                return;
            }

            gridVisualizer.ClearHighlights();

            if (onTileSelected != null)
            {
                onTileSelected(tileId);
                selectionActive = false;
                Reset();
                return;
            }

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive)
            {
                onCellSelected?.Invoke(cell);
                selectionActive = false;
                Reset();
            }
        }

        public void CancelSelection()
        {
            if (!selectionActive) return;
            selectionActive = false;
            gridVisualizer.ClearHighlights();
            Reset();
            onCancelled?.Invoke();
        }

        private void Reset()
        {
            selectingPlayerId = -1;
            onCellSelected = null;
            onTileSelected = null;
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
