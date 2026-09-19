using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Input;
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

        // Entering a selection mode requests the target reticle; leaving it (finish,
        // cancel, auto-complete, or a directional phase change) releases it. Routing
        // every path through the setter means no exit can forget the cursor.
        private bool SelectionActive
        {
            get => selectionActive;
            set
            {
                if (selectionActive == value)
                {
                    return;
                }

                selectionActive = value;
                if (value)
                {
                    CursorManager.Instance?.Push(CursorKind.Target, this);
                }
                else
                {
                    CursorManager.Instance?.Pop(this);
                }
            }
        }

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

        private void OnDisable()
        {
            CursorManager.Instance?.Pop(this);
        }

        private void Update()
        {
            if (!SelectionActive || directionalSelectionPhase != DirectionalSelectionPhase.SelectDirection)
            {
                return;
            }

            if (awaitingDirectionalMouseRelease)
            {
                if (!UnityInputAdapter.IsPrimaryPointerPressed())
                {
                    awaitingDirectionalMouseRelease = false;
                }

                return;
            }

            UpdateDirectionalAimPreview();

            if (UnityInputAdapter.WasSecondaryPointerPressedThisFrame())
            {
                CancelSelection();
                return;
            }

            if (UnityInputAdapter.WasPrimaryPointerPressedThisFrame()
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
            string promptMessage = null,
            bool cancellable = false)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectLivingCell called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            selectingPlayerId = playerId;
            SelectionActive = true;
            IsCancellable = cancellable;

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
            string aimPromptMessage = null,
            bool cancellable = false)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectLivingCellAndAimDirection called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            IsCancellable = cancellable;
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
            string cancelButtonLabel = "Cancel",
            bool cancellable = false)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectBoardTile called but GameManager.Instance.Board is null.");
                onCancel?.Invoke();
                return;
            }

            bool IsSelectableTarget(BoardTile tile) => tile != null && !tile.IsBlocked && isValidTile(tile);

            SelectionActive = true;
            // A visible Cancel button only makes sense for a selection the player may back out of.
            IsCancellable = cancellable || showCancelButton;

            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage, showCancelButton, cancelButtonLabel, () => CancelSelection());

            Action<int> onTileSelected = (tileId) =>
            {
                GameManager.Instance.HideSelectionPrompt();
                var tile = board.GetTileById(tileId);
                if (IsSelectableTarget(tile))
                {
                    onSelected?.Invoke(tile);
                }
            };
            onCancelled = () =>
            {
                GameManager.Instance.HideSelectionPrompt();
                onCancel?.Invoke();
            };

            var validTiles = board.AllTiles()
                .Where(IsSelectableTarget)
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
            string promptMessage = null,
            bool cancellable = false)
        {
            var board = GameManager.Instance?.Board;
            if (board == null)
            {
                Debug.LogError("PromptSelectMultipleBoardTiles called but GameManager.Instance.Board is null.");
                onComplete?.Invoke();
                return;
            }

            bool IsSelectableTarget(BoardTile tile) => tile != null && !tile.IsBlocked && isValidTile(tile);

            SelectionActive = true;
            IsCancellable = cancellable;
            if (!string.IsNullOrEmpty(promptMessage))
                GameManager.Instance.ShowSelectionPrompt(promptMessage);

            var validTiles = board.AllTiles()
                .Where(IsSelectableTarget)
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
                if (!IsSelectableTarget(tile))
                    return;
                selectedTileIds.Add(tileId);
                selectedCount++;
                onTileSelected?.Invoke(tile);
                if (selectedCount >= maxTiles || selectedTileIds.Count >= selectableTileIds.Count)
                {
                    SelectionActive = false;
                    gridVisualizer.ClearHighlights();
                    GameManager.Instance.HideSelectionPrompt();
                    if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                    Reset();
                    onComplete?.Invoke();
                }
            };
            onCancelled = () =>
            {
                SelectionActive = false;
                gridVisualizer.ClearHighlights();
                GameManager.Instance.HideSelectionPrompt();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
                onComplete?.Invoke();
            };
        }

        public void OnTileClicked(int tileId)
        {
            if (!SelectionActive || !selectableTileIds.Contains(tileId))
            {
                if (!SelectionActive)
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
                SelectionActive = false;
                gridVisualizer.ClearHighlights();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
                return;
            }

            var cell = GameManager.Instance.Board.GetCell(tileId);
            if (cell != null && cell.IsAlive)
            {
                onCellSelected?.Invoke(cell);
                SelectionActive = false;
                gridVisualizer.ClearHighlights();
                if (hoverHighlighter != null) hoverHighlighter.ClearSelectableTiles();
                Reset();
            }
        }

        /// <summary>
        /// Player-initiated cancel (Escape, right-click, Cancel button). Backing out of
        /// the aim phase to re-pick the source is always allowed since nothing has been
        /// committed; abandoning the selection entirely is only allowed when the prompt
        /// was started as cancellable, so a mandatory placement can never be forfeited
        /// by reflex. Returns whether the cancel was handled.
        /// </summary>
        public bool CancelSelection()
        {
            if (!SelectionActive) return false;

            if (directionalSelectionPhase == DirectionalSelectionPhase.SelectDirection)
            {
                BeginDirectionalSourceSelection();
                return true;
            }

            if (!IsCancellable) return false;

            AbortSelection();
            return true;
        }

        /// <summary>
        /// Forced teardown regardless of cancellability, for when the game itself is
        /// going away (return to main menu). Still fires the cancel callback so the
        /// owning flow can clean up.
        /// </summary>
        public void AbortSelection()
        {
            if (!SelectionActive) return;

            SelectionActive = false;
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
            IsCancellable = false;
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
            return SelectionActive && selectableTileIds.Contains(tileId);
        }

        public void ReapplySelectionHighlights()
        {
            if (!SelectionActive || selectableTileIds.Count == 0)
            {
                return;
            }

            gridVisualizer.HighlightTiles(selectableTileIds, highlightColorA, highlightColorB);
        }

        public bool HasActiveSelection => SelectionActive;

        /// <summary>True when the player may back out of the current selection without resolving it.</summary>
        public bool IsCancellable { get; private set; }

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

            SelectionActive = true;
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
            SelectionActive = true;
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
            Vector2 pointerScreen = UnityInputAdapter.GetPointerScreenPosition();
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, 0f));
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

            SelectionActive = false;
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
