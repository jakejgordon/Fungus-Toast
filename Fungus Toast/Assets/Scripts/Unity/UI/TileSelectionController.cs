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
        [SerializeField] private TileHoverHighlighter hoverHighlighter; // optional

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

        public void PromptSelectLivingCell(
            int playerId,
            Action<FungalCell> onSelected,
            Action onCancel = null,
            string promptMessage = null)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectLivingCell called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            selectingPlayerId = playerId;
            selectionActive = true;

            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

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

            var validCells = board.GetAllCellsOwnedBy(playerId)
                .Where(c => c.IsAlive)
                .ToList();

            selectableTileIds = new HashSet<int>(validCells.Select(c => c.TileId));

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(1f, 0.2f, 0.8f, 1f),
                new Color(1f, 0.7f, 1f, 1f)
            );
        }

        public void PromptSelectBoardTile(
            Func<BoardTile, bool> isValidTile,
            Action<BoardTile> onSelected,
            Action onCancel = null,
            string promptMessage = null)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectBoardTile called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            selectionActive = true;

            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            Action<int> onTileSelected = (tileId) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                var tile = board.GetTileById(tileId);
                onSelected?.Invoke(tile);
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };

            var validTiles = board.AllTiles()
                .Where(isValidTile)
                .ToList();
            selectableTileIds = new HashSet<int>(validTiles.Select(t => t.TileId));

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(0.2f, 0.8f, 1f, 1f),
                new Color(0.7f, 1f, 1f, 1f)
            );

            onCellSelected = null;
            this.onTileSelected = onTileSelected;
        }

        public void PromptSelectMultipleBoardTiles(
            Func<BoardTile, bool> isValidTile,
            Action<BoardTile> onTileSelected,
            Action onComplete,
            int maxTiles,
            string promptMessage = null)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectMultipleBoardTiles called but GameManager.Instance.Board is null.");
                onComplete?.Invoke();
                return;
            }

            selectionActive = true;
            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            var validTiles = board.AllTiles()
                .Where(isValidTile)
                .ToList();
            selectableTileIds = new HashSet<int>(validTiles.Select(t => t.TileId));

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            gridVisualizer.HighlightTiles(
                selectableTileIds,
                new Color(0.2f, 0.8f, 1f, 1f),
                new Color(0.7f, 1f, 1f, 1f)
            );

            var selectedTileIds = new HashSet<int>();
            int selectedCount = 0;

            onCellSelected = null;
            this.onTileSelected = (tileId) =>
            {
                if (!selectableTileIds.Contains(tileId) || selectedTileIds.Contains(tileId))
                    return;
                var tile = board.GetTileById(tileId);
                selectedTileIds.Add(tileId);
                selectedCount++;
                onTileSelected?.Invoke(tile);
                if (selectedCount >= maxTiles || selectedTileIds.Count >= selectableTileIds.Count)
                {
                    selectionActive = false;
                    gridVisualizer.ClearHighlights();
                    GameManager.Instance.HideSelectionPrompt();
                    if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                    Reset();
                    onComplete?.Invoke();
                }
            };
            onCancelled = () =>
            {
                selectionActive = false;
                gridVisualizer.ClearHighlights();
                GameManager.Instance.HideSelectionPrompt();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
                onComplete?.Invoke();
            };
        }

        public void OnTileClicked(int tileId)
        {
            if (!selectionActive || !selectableTileIds.Contains(tileId))
            {
                if (!selectionActive)
                    Debug.LogWarning($"TileSelectionController.OnTileClicked called when selection is not active. TileId: {tileId}");
                return;
            }

            if (onTileSelected != null)
            {
                onTileSelected(tileId);
                selectionActive = false;
                gridVisualizer.ClearHighlights();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
                return;
            }

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive)
            {
                onCellSelected?.Invoke(cell);
                selectionActive = false;
                gridVisualizer.ClearHighlights();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
            }
        }

        public void CancelSelection()
        {
            if (!selectionActive) return;
            selectionActive = false;
            gridVisualizer.ClearHighlights();
            if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
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

        public bool IsSelectable(int tileId)
        {
            return selectionActive && selectableTileIds.Contains(tileId);
        }
    }
}
