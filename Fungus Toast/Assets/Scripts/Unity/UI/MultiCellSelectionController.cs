using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class MultiCellSelectionController : MonoBehaviour
    {
        public static MultiCellSelectionController Instance { get; private set; }

        [SerializeField] private GridVisualizer gridVisualizer;

        private Action<List<FungalCell>> onCellsSelected;
        private Action onCancelled;
        private int selectingPlayerId = -1;
        private HashSet<int> selectableTileIds = new HashSet<int>();
        private HashSet<int> selectedTileIds = new HashSet<int>();
        private int maxSelections = 5;
        private bool selectionActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this.gameObject);
            else
                Instance = this;

            if (gridVisualizer == null)
                throw new System.Exception($"{nameof(MultiCellSelectionController)} requires a reference to GridVisualizer. Assign it in the Inspector.");
        }

        /// <summary>
        /// Prompts the player to select multiple of their living fungal cells.
        /// Highlights valid cells and waits for clicks.
        /// Shows an instructional prompt if provided.
        /// </summary>
        public void PromptSelectMultipleLivingCells(
            int playerId,
            int maxSelections,
            Action<List<FungalCell>> onSelected,
            Action onCancel = null,
            string promptMessage = null)
        {
            this.selectingPlayerId = playerId;
            this.maxSelections = maxSelections;
            selectionActive = true;
            selectedTileIds.Clear();

            // Show the prompt if a message was provided
            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            // Find valid cells (living, owned by player, not already resistant)
            var validCells = GameManager.Instance.Board.GetAllCellsOwnedBy(playerId)
                .Where(c => c.IsAlive && !c.IsResistant)
                .ToList();

            selectableTileIds = new HashSet<int>(validCells.Select(c => c.TileId));

            // Highlight valid tiles using GridVisualizer
            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(0.2f, 1f, 0.2f, 1f),   // Green for selectable
                new Color(0.8f, 1f, 0.8f, 1f)    // Light green
            );

            // Set up callbacks
            onCellsSelected = (cells) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onSelected?.Invoke(cells);
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };
        }

        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId)) return;

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive && !cell.IsResistant)
            {
                if (selectedTileIds.Contains(tileId))
                {
                    // Deselect the cell
                    selectedTileIds.Remove(tileId);
                    gridVisualizer.HighlightTiles(
                        new[] { tileId },
                        new Color(0.2f, 1f, 0.2f, 1f),   // Green for selectable
                        new Color(0.8f, 1f, 0.8f, 1f)    // Light green
                    );
                }
                else if (selectedTileIds.Count < maxSelections)
                {
                    // Select the cell
                    selectedTileIds.Add(tileId);
                    gridVisualizer.HighlightTiles(
                        new[] { tileId },
                        new Color(1f, 0.8f, 0.2f, 1f),   // Orange for selected
                        new Color(1f, 1f, 0.8f, 1f)      // Light orange
                    );
                }

                // Update the prompt to show selection count
                GameManager.Instance.ShowSelectionPrompt(
                    $"Selected {selectedTileIds.Count}/{maxSelections} cells. Click to select/deselect, or press Enter to confirm."
                );
            }
        }

        public void ConfirmSelection()
        {
            if (!selectionActive) return;

            var selectedCells = selectedTileIds
                .Select(tileId => GameManager.Instance.Board.GetCell(tileId))
                .Where(cell => cell != null)
                .ToList();

            selectionActive = false;
            gridVisualizer.ClearHighlights();
            onCellsSelected?.Invoke(selectedCells);
            Reset();
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
            onCellsSelected = null;
            onCancelled = null;
            selectableTileIds.Clear();
            selectedTileIds.Clear();
            maxSelections = 5;
        }

        /// <summary>
        /// Returns true if the given tile is currently selectable.
        /// </summary>
        public bool IsSelectable(int tileId)
        {
            return selectionActive && selectableTileIds.Contains(tileId);
        }

        /// <summary>
        /// Returns true if the given tile is currently selected.
        /// </summary>
        public bool IsSelected(int tileId)
        {
            return selectionActive && selectedTileIds.Contains(tileId);
        }

        private void Update()
        {
            // Handle Enter key to confirm selection
            if (selectionActive && Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmSelection();
            }

            // Handle Escape key to cancel selection
            if (selectionActive && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }
        }
    }
} 