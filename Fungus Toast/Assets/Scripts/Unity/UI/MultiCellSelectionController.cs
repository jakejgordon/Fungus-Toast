using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

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
        private int lastRemaining = -1;

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
            selectionActive = true;
            selectedTileIds.Clear();

            // Find valid cells (living, owned by player, not already resistant)
            var validCells = GameManager.Instance.Board.GetAllCellsOwnedBy(playerId)
                .Where(c => c.IsAlive && !c.IsResistant)
                .ToList();

            this.maxSelections = Mathf.Min(maxSelections, validCells.Count);

            selectableTileIds = new HashSet<int>(validCells.Select(c => c.TileId));

            // Highlight valid tiles using GridVisualizer (use magenta-pink like Jetting Mycelium)
            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(1f, 0.15f, 0.8f, 1f),   // Magenta-pink for selectable (matches Jetting Mycelium)
                new Color(1f, 1f, 1f, 1f)          // White
            );

            // Show the initial prompt
            UpdateSelectionPrompt();

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

        private void UpdateSelectionPrompt()
        {
            int remaining = maxSelections - selectedTileIds.Count;
            string cellWord = remaining == 1 ? "cell" : "cells";
            GameManager.Instance.ShowSelectionPrompt($"Select {remaining} {cellWord} to give Resistance.");

            // Animate the prompt if the number changed
            if (remaining != lastRemaining)
            {
                AnimatePromptPop();
                lastRemaining = remaining;
            }
        }

        private void AnimatePromptPop()
        {
            var promptText = GameManager.Instance.SelectionPromptText;
            if (promptText == null) return;
            promptText.transform.localScale = Vector3.one;
            StopAllCoroutines();
            StartCoroutine(PromptPopCoroutine(promptText));
        }

        private IEnumerator PromptPopCoroutine(TMP_Text promptText)
        {
            float popScale = 1.25f;
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one * popScale;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                promptText.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            promptText.transform.localScale = endScale;

            // Scale back down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                promptText.transform.localScale = Vector3.Lerp(endScale, startScale, t);
                yield return null;
            }
            promptText.transform.localScale = startScale;
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
                        new Color(1f, 0.15f, 0.8f, 1f),   // Magenta-pink for selectable
                        new Color(1f, 1f, 1f, 1f)
                    );
                    UpdateSelectionPrompt();
                }
                else if (selectedTileIds.Count < maxSelections)
                {
                    // Select the cell
                    selectedTileIds.Add(tileId);
                    gridVisualizer.HighlightTiles(
                        new[] { tileId },
                        new Color(1f, 0.8f, 0.2f, 1f),   // Orange for selected
                        new Color(1f, 1f, 0.8f, 1f)
                    );
                    UpdateSelectionPrompt();

                    // If we've selected the allowed number, finish immediately
                    if (selectedTileIds.Count == maxSelections)
                    {
                        selectionActive = false;
                        gridVisualizer.ClearHighlights();
                        var selectedCells = selectedTileIds
                            .Select(id => GameManager.Instance.Board.GetCell(id))
                            .Where(c => c != null)
                            .ToList();
                        GameManager.Instance.HideSelectionPrompt();
                        onCellsSelected?.Invoke(selectedCells);
                        Reset();
                    }
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
            // Handle Escape key to cancel selection
            if (selectionActive && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }
        }
    }
} 