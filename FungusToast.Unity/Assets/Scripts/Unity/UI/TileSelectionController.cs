using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI
{
    public class TileSelectionController : MonoBehaviour
    {
        private enum DirectionalSelectionPhase
        {
            None,
            SelectSource,
            SelectDirection,
        }

        private const float DirectionAimDeadZoneWorldUnits = 0.15f;
        private static readonly Color DirectionalAnchorColor = new Color(1f, 0.35f, 0.85f, 1f);
        private MagnifyingGlassFollowMouse magnifyingGlass;

        public static TileSelectionController Instance { get; private set; }

        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private TileHoverHighlighter hoverHighlighter; // optional

        private Action<FungalCell> onCellSelected;
        private Action onCancelled;
        private int selectingPlayerId = -1;
        private HashSet<int> selectableTileIds = new HashSet<int>();
        private bool selectionActive = false;
        private Action<int> onTileSelected; // For generic board tile selection
        private Color highlightColorA = new Color(0.2f, 0.8f, 1f, 1f);
        private Color highlightColorB = new Color(0.7f, 1f, 1f, 1f);
        private Action<int> _hoverPreviewCallback;
        private DirectionalSelectionPhase directionalSelectionPhase = DirectionalSelectionPhase.None;
        private Action<int, CardinalDirection> onDirectionalSelectionConfirmed;
        private Action onDirectionalSelectionCancelled;
        private Action<int, CardinalDirection?> onDirectionalSelectionPreviewChanged;
        private string directionalSourcePromptMessage;
        private string directionalAimPromptMessage;
        private int directionalSelectingPlayerId = -1;
        private int directionalAnchorTileId = -1;
        private CardinalDirection? currentDirectionalAim;
        private bool awaitingDirectionalMouseRelease;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this.gameObject);
            else
                Instance = this;

            if (gridVisualizer == null)
                throw new System.Exception($"{nameof(TileSelectionController)} requires a reference to GridVisualizer. Assign it in the Inspector.");

            magnifyingGlass = FindAnyObjectByType<MagnifyingGlassFollowMouse>();
        }

        private void Update()
        {
            if (!selectionActive || directionalSelectionPhase != DirectionalSelectionPhase.SelectDirection)
            {
                return;
            }

            if (awaitingDirectionalMouseRelease)
            {
                if (!Input.GetMouseButton(0))
                {
                    awaitingDirectionalMouseRelease = false;
                }

                return;
            }

            UpdateDirectionalAimPreview();

            if (Input.GetMouseButtonDown(1))
            {
                CancelSelection();
                return;
            }

            if (Input.GetMouseButtonDown(0)
                && currentDirectionalAim.HasValue
                && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                ConfirmDirectionalSelection();
            }
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
            highlightColorA = new Color(1f, 0.2f, 0.8f, 1f);
            highlightColorB = new Color(1f, 0.7f, 1f, 1f);

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            ReapplySelectionHighlights();
        }

        public void PromptSelectLivingCellAndAimDirection(
            int playerId,
            Action<int, CardinalDirection> onConfirmed,
            Action onCancel = null,
            Action<int, CardinalDirection?> onPreviewChanged = null,
            string sourcePromptMessage = null,
            string aimPromptMessage = null)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectLivingCellAndAimDirection called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            onDirectionalSelectionConfirmed = onConfirmed;
            onDirectionalSelectionCancelled = onCancel;
            onDirectionalSelectionPreviewChanged = onPreviewChanged;
            directionalSelectingPlayerId = playerId;
            directionalSourcePromptMessage = sourcePromptMessage;
            directionalAimPromptMessage = aimPromptMessage;

            BeginDirectionalSourceSelection();
        }

        public void PromptSelectBoardTile(
            Func<BoardTile, bool> isValidTile,
            Action<BoardTile> onSelected,
            Action onCancel = null,
            string promptMessage = null,
            bool showCancelButton = false,
            string cancelButtonLabel = "Cancel")
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
                GameManager.Instance.ShowSelectionPrompt(promptMessage, showCancelButton, cancelButtonLabel, CancelSelection);

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
            highlightColorA = new Color(0.2f, 0.8f, 1f, 1f);
            highlightColorB = new Color(0.7f, 1f, 1f, 1f);

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            ReapplySelectionHighlights();

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
            highlightColorA = new Color(0.2f, 0.8f, 1f, 1f);
            highlightColorB = new Color(0.7f, 1f, 1f, 1f);

            if (hoverHighlighter != null)
                hoverHighlighter.SetSelectableTiles(selectableTileIds);

            ReapplySelectionHighlights();

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

            if (directionalSelectionPhase == DirectionalSelectionPhase.SelectSource)
            {
                BeginDirectionalAim(tileId);
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

            if (directionalSelectionPhase == DirectionalSelectionPhase.SelectDirection)
            {
                BeginDirectionalSourceSelection();
                return;
            }

            selectionActive = false;
            gridVisualizer.ClearHighlights();
            if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
            var cancelled = onCancelled;
            Reset();
            cancelled?.Invoke();
        }

        private void Reset()
        {
            if (directionalSelectionPhase != DirectionalSelectionPhase.None)
            {
                gridVisualizer.ClearJettingMyceliumPreview();
                gridVisualizer.ClearSelectedTiles();
                GameManager.Instance?.HideSelectionPrompt();
            }

            SetSelectionModeVisualSuppression(false);
            SetHoverVisualSuppression(false);

            selectingPlayerId = -1;
            onCellSelected = null;
            onTileSelected = null;
            onCancelled = null;
            selectableTileIds.Clear();
            _hoverPreviewCallback?.Invoke(-1);
            _hoverPreviewCallback = null;
            if (hoverHighlighter != null)
                hoverHighlighter.OnSelectableTileHovered = null;

            directionalSelectionPhase = DirectionalSelectionPhase.None;
            onDirectionalSelectionConfirmed = null;
            onDirectionalSelectionCancelled = null;
            onDirectionalSelectionPreviewChanged = null;
            directionalSourcePromptMessage = null;
            directionalAimPromptMessage = null;
            directionalSelectingPlayerId = -1;
            directionalAnchorTileId = -1;
            currentDirectionalAim = null;
            awaitingDirectionalMouseRelease = false;
        }

        public bool IsSelectable(int tileId)
        {
            return selectionActive && selectableTileIds.Contains(tileId);
        }

        public void ReapplySelectionHighlights()
        {
            if (!selectionActive || selectableTileIds.Count == 0)
            {
                return;
            }

            gridVisualizer.HighlightTiles(selectableTileIds, highlightColorA, highlightColorB);
        }

        public bool HasActiveSelection => selectionActive;

        /// <summary>
        /// Registers a callback that is invoked whenever a selectable tile is newly hovered
        /// (called with the tileId) or the hover is cleared (called with -1).
        /// The callback is automatically cleared when the selection ends.
        /// </summary>
        public void SetHoverPreviewCallback(Action<int> onHoverTileId)
        {
            _hoverPreviewCallback = onHoverTileId;
            if (hoverHighlighter != null)
                hoverHighlighter.OnSelectableTileHovered = onHoverTileId;
        }

        private void BeginDirectionalSourceSelection()
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                var cancelled = onDirectionalSelectionCancelled;
                Reset();
                cancelled?.Invoke();
                return;
            }

            selectionActive = true;
            directionalSelectionPhase = DirectionalSelectionPhase.SelectSource;
            selectingPlayerId = directionalSelectingPlayerId;
            directionalAnchorTileId = -1;
            currentDirectionalAim = null;
            awaitingDirectionalMouseRelease = false;
            onCellSelected = null;
            onTileSelected = null;
            onCancelled = onDirectionalSelectionCancelled;

            selectableTileIds = new HashSet<int>(board.GetAllCellsOwnedBy(directionalSelectingPlayerId)
                .Where(cell => cell.IsAlive)
                .Select(cell => cell.TileId));

            highlightColorA = new Color(1f, 0.2f, 0.8f, 1f);
            highlightColorB = new Color(1f, 0.7f, 1f, 1f);

            gridVisualizer.ClearJettingMyceliumPreview();
            gridVisualizer.ClearAllHighlights();

            SetSelectionModeVisualSuppression(true);
            SetHoverVisualSuppression(false);

            if (hoverHighlighter != null)
            {
                hoverHighlighter.SetSelectableTiles(selectableTileIds);
                hoverHighlighter.OnSelectableTileHovered = null;
            }

            if (!string.IsNullOrEmpty(directionalSourcePromptMessage))
            {
                GameManager.Instance.ShowSelectionPrompt(directionalSourcePromptMessage);
            }

            ReapplySelectionHighlights();
        }

        private void BeginDirectionalAim(int anchorTileId)
        {
            selectionActive = true;
            directionalSelectionPhase = DirectionalSelectionPhase.SelectDirection;
            directionalAnchorTileId = anchorTileId;
            currentDirectionalAim = null;
            awaitingDirectionalMouseRelease = true;
            selectableTileIds.Clear();
            onCellSelected = null;
            onTileSelected = null;
            onCancelled = onDirectionalSelectionCancelled;

            gridVisualizer.ClearAllHighlights();
            gridVisualizer.ShowSelectedTiles(new[] { anchorTileId }, DirectionalAnchorColor);

            SetSelectionModeVisualSuppression(true);
            SetHoverVisualSuppression(true);

            if (hoverHighlighter != null)
            {
                hoverHighlighter.ClearSelectableTiles();
                hoverHighlighter.OnSelectableTileHovered = null;
            }

            if (!string.IsNullOrEmpty(directionalAimPromptMessage))
            {
                GameManager.Instance.ShowSelectionPrompt(directionalAimPromptMessage);
            }

            UpdateDirectionalAimPreview();
        }

        private void UpdateDirectionalAimPreview()
        {
            var nextDirection = ResolveDirectionalAimDirection();
            if (nextDirection == currentDirectionalAim)
            {
                return;
            }

            currentDirectionalAim = nextDirection;
            gridVisualizer.ClearAllHighlights();
            gridVisualizer.ShowSelectedTiles(new[] { directionalAnchorTileId }, DirectionalAnchorColor);
            onDirectionalSelectionPreviewChanged?.Invoke(directionalAnchorTileId, currentDirectionalAim);
        }

        private CardinalDirection? ResolveDirectionalAimDirection()
        {
            var board = GameManager.Instance?.Board;
            if (board == null || gridVisualizer == null || gridVisualizer.toastTilemap == null || Camera.main == null || directionalAnchorTileId < 0)
            {
                return null;
            }

            Vector3Int anchorCell = new Vector3Int(
                directionalAnchorTileId % board.Width,
                directionalAnchorTileId / board.Width,
                0);

            Vector3 anchorWorld = gridVisualizer.toastTilemap.GetCellCenterWorld(anchorCell);
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 delta = new Vector2(mouseWorld.x - anchorWorld.x, mouseWorld.y - anchorWorld.y);

            if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < DirectionAimDeadZoneWorldUnits)
            {
                return currentDirectionalAim;
            }

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x >= 0f ? CardinalDirection.East : CardinalDirection.West;
            }

            return delta.y >= 0f ? CardinalDirection.North : CardinalDirection.South;
        }

        private void ConfirmDirectionalSelection()
        {
            if (!currentDirectionalAim.HasValue)
            {
                return;
            }

            var confirmed = onDirectionalSelectionConfirmed;
            int anchorTileId = directionalAnchorTileId;
            CardinalDirection direction = currentDirectionalAim.Value;

            selectionActive = false;
            onDirectionalSelectionPreviewChanged?.Invoke(anchorTileId, null);
            Reset();
            confirmed?.Invoke(anchorTileId, direction);
        }

        private void SetSelectionModeVisualSuppression(bool suppressed)
        {
            magnifyingGlass?.SetSelectionModeVisualSuppression(suppressed);
        }

        private void SetHoverVisualSuppression(bool suppressed)
        {
            hoverHighlighter?.SetHoverVisualSuppression(suppressed);
        }
    }
}
