using FungusToast.Core.Board;
using FungusToast.Unity.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI
{
    public class MultiTileSelectionController : MonoBehaviour
    {
        public static MultiTileSelectionController Instance { get; private set; }

        [SerializeField] private GridVisualizer gridVisualizer;

        private Action<List<BoardTile>> onTilesSelected;
        private Action onCancelled;
        private Action<BoardTile> onTilePickedImmediate;
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
                throw new System.Exception($"{nameof(MultiTileSelectionController)} requires a reference to GridVisualizer. Assign it in the Inspector.");
        }

        /// <summary>
        /// Prompts the player to select multiple board tiles matching a predicate.
        /// Highlights valid tiles and waits for clicks.
        /// Shows an instructional prompt if provided.
        /// </summary>
        public void PromptSelectMultipleTiles(
            Func<BoardTile, bool> isValidTile,
            int maxSelections,
            Action<List<BoardTile>> onSelected,
            Action onCancel = null,
            string promptMessage = null,
            Action<BoardTile> onTilePicked = null)
        {
            selectionActive = true;
            selectedTileIds.Clear();

            var validTiles = GameManager.Instance.Board.AllTiles()
                .Where(isValidTile)
                .ToList();

            this.maxSelections = Mathf.Min(maxSelections, validTiles.Count);
            selectableTileIds = new HashSet<int>(validTiles.Select(t => t.TileId));

            // Highlight valid tiles with pinkish color
            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(1f, 0.2f, 0.8f, 1f),   // Pink pulse
                new Color(1f, 0.7f, 1f, 1f)      // Pinkish white
            );

            // Show the initial prompt
            UpdateSelectionPrompt();

            // Set up callbacks
            onTilesSelected = (tiles) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onSelected?.Invoke(tiles);
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };
            onTilePickedImmediate = onTilePicked;
        }

        private void UpdateSelectionPrompt()
        {
            int remaining = maxSelections - selectedTileIds.Count;
            string tileWord = remaining == 1 ? "tile" : "tiles";
            GameManager.Instance.ShowSelectionPrompt($"Select {remaining} {tileWord}.");
        }

        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId)) return;

            var tile = GameManager.Instance.Board.GetTileById(tileId);
            if (tile != null)
            {
                if (selectedTileIds.Contains(tileId))
                {
                    // Deselect the tile (remove overlay only)
                    selectedTileIds.Remove(tileId);
                }
                else if (selectedTileIds.Count < maxSelections)
                {
                    // Select the tile (add overlay only)
                    selectedTileIds.Add(tileId);
                }

                // Build per-tile highlight dictionary with pinkish for unselected
                var tileHighlights = new Dictionary<int, (Color, Color)>();
                foreach (var id in selectableTileIds)
                {
                    if (selectedTileIds.Contains(id))
                        tileHighlights[id] = (Color.black, Color.black);
                    else
                        tileHighlights[id] = (new Color(1f, 0.2f, 0.8f, 1f), new Color(1f, 0.7f, 1f, 1f));
                }
                gridVisualizer.HighlightTiles(tileHighlights);
                UpdateSelectionPrompt();

                // If we've selected the allowed number, finish immediately
                if (selectedTileIds.Count == maxSelections)
                {
                    selectionActive = false;
                    gridVisualizer.ClearHighlights();
                    var selectedTiles = selectedTileIds
                        .Select(id => GameManager.Instance.Board.GetTileById(id))
                        .Where(t => t != null)
                        .ToList();
                    GameManager.Instance.HideSelectionPrompt();
                    onTilesSelected?.Invoke(selectedTiles);
                    Reset();
                }
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
            onTilesSelected = null;
            onCancelled = null;
            onTilePickedImmediate = null;
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
            // Handle Escape key to cancel selection
            if (selectionActive && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }
        }
    }
}
