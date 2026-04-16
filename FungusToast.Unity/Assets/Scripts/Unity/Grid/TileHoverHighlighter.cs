using System;
using System.Collections.Generic;
using FungusToast.Unity.Input;
using FungusToast.Unity.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid
{
    public class TileHoverHighlighter : MonoBehaviour
    {
        public Tilemap groundTilemap; // unused for ID mapping now, kept for backward compat
        [SerializeField] private GridVisualizer gridVisualizer;
        public Tile glowyTile;
        public Tile pressedTile;

        public GameObject crosshairPrefab;
        private GameObject crosshairInstance;

        private Vector3Int? lastHoveredCell = null;
        private HashSet<int> selectionTiles = new();
        private bool hoverVisualsSuppressed;

        /// <summary>
        /// Invoked when a selectable tile is newly hovered (positive tileId) or hover is cleared (-1).
        /// Only fired while there is an active tile selection (selectionTiles.Count > 0).
        /// </summary>
        public Action<int> OnSelectableTileHovered;

        void Start()
        {
            if (crosshairPrefab != null)
            {
                crosshairInstance = Instantiate(crosshairPrefab, this.transform);
                crosshairInstance.SetActive(false);
            }
        }

        void Update()
        {
            if (gridVisualizer == null || gridVisualizer.toastTilemap == null)
                return;

            if (GameManager.Instance != null && GameManager.Instance.IsPauseMenuOpen)
            {
                ClearHoverAndNotify();
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
                return;
            }

            Vector2 pointerScreen = UnityInputAdapter.GetPointerScreenPosition();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, 0f));
            Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorldPos);

            if (!IsCellOnBoard(cellPos))
            {
                ClearHoverAndNotify();
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
                return;
            }

            bool isSelectable = selectionTiles.Count == 0 || IsSelectable(cellPos);

            if (isSelectable && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
                if (lastHoveredCell != cellPos)
                {
                    if (!hoverVisualsSuppressed)
                    {
                        gridVisualizer?.ShowHoverEffect(cellPos);
                    }
                    else
                    {
                        gridVisualizer?.ClearHoverEffect();
                    }

                    if (selectionTiles.Count > 0)
                    {
                        int tileId = TileIdFromCell(cellPos);
                        OnSelectableTileHovered?.Invoke(tileId);
                    }
                }

                if (crosshairInstance != null)
                {
                    crosshairInstance.SetActive(true);
                    Vector3 cellWorldPos = gridVisualizer.toastTilemap.GetCellCenterWorld(cellPos);
                    crosshairInstance.transform.position = new Vector3(cellWorldPos.x, cellWorldPos.y, crosshairInstance.transform.position.z);
                }
                lastHoveredCell = cellPos;
            }
            else
            {
                ClearHoverAndNotify();
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
            }

            if (UnityInputAdapter.WasPrimaryPointerPressedThisFrame() && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
                int tileId = TileIdFromCell(cellPos);
                bool gateOk = selectionTiles.Count == 0 || selectionTiles.Contains(tileId);
                if (gateOk)
                {
                    if (MultiTileSelectionController.Instance != null && MultiTileSelectionController.Instance.IsSelectable(tileId))
                    {
                        MultiTileSelectionController.Instance.OnTileClicked(tileId);
                    }
                    else if (MultiCellSelectionController.Instance != null && MultiCellSelectionController.Instance.IsSelectable(tileId))
                    {
                        MultiCellSelectionController.Instance.OnTileClicked(tileId);
                    }
                    else if (TileSelectionController.Instance != null && TileSelectionController.Instance.IsSelectable(tileId))
                    {
                        TileSelectionController.Instance.OnTileClicked(tileId);
                    }
                }
                TriggerPressedAnimation(cellPos);
            }

            if (UnityInputAdapter.WasSecondaryPointerPressedThisFrame())
            {
                if (selectionTiles.Count > 0)
                {
                    if (MultiCellSelectionController.Instance != null)
                    {
                        MultiCellSelectionController.Instance.CancelSelection();
                    }
                    else if (TileSelectionController.Instance != null)
                    {
                        TileSelectionController.Instance.CancelSelection();
                    }
                }
            }
        }

        bool IsSelectable(Vector3Int cell)
        {
            int tileId = TileIdFromCell(cell);
            return selectionTiles.Contains(tileId);
        }

        int TileIdFromCell(Vector3Int cell)
        {
            var board = GameManager.Instance != null ? GameManager.Instance.Board : null;
            if (board == null) return -1;
            int x = cell.x;
            int y = cell.y;
            return y * board.Width + x;
        }

        void TriggerPressedAnimation(Vector3Int cell) { }

        /// <summary>
        /// Clears the hover effect and, if a selectable tile was previously hovered,
        /// invokes <see cref="OnSelectableTileHovered"/> with -1 to signal hover cleared.
        /// </summary>
        private void ClearHoverAndNotify()
        {
            if (lastHoveredCell.HasValue)
                OnSelectableTileHovered?.Invoke(-1);
            gridVisualizer?.ClearHoverEffect();
            lastHoveredCell = null;
        }

        bool IsCellOnBoard(Vector3Int cellPos)
        {
            return gridVisualizer != null && gridVisualizer.IsPlayableBoardCell(cellPos);
        }

        public void SetSelectableTiles(HashSet<int> selectableTileIds)
        {
            selectionTiles = selectableTileIds ?? new HashSet<int>();
        }

        public void ClearSelectableTiles()
        {
            selectionTiles.Clear();
        }

        public void SetHoverVisualSuppression(bool suppressed)
        {
            hoverVisualsSuppressed = suppressed;
            if (suppressed)
            {
                gridVisualizer?.ClearHoverEffect();
            }
        }
    }
}
