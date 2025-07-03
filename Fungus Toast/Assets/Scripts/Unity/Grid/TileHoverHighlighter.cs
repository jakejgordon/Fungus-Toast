using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using FungusToast.Unity.UI;

namespace FungusToast.Unity.Grid
{
    public class TileHoverHighlighter : MonoBehaviour
    {
        public Tilemap groundTilemap;
        [SerializeField] private GridVisualizer gridVisualizer;
        public Tile glowyTile;
        public Tile pressedTile;

        private Vector3Int? lastHoveredCell = null;
        // New: Set of currently "selectable" tile IDs for interaction
        private HashSet<int> selectionTiles = new();

        void Update()
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorldPos);

            if (!IsCellOnBoard(cellPos))
                return;

            bool isSelectable = selectionTiles.Count == 0 || IsSelectable(cellPos);

            if (lastHoveredCell != null && lastHoveredCell != cellPos)
            {
                gridVisualizer.HoverOverlayTileMap.SetTile(lastHoveredCell.Value, null);
            }

            if (gridVisualizer.toastTilemap.HasTile(cellPos) && isSelectable)
            {
                gridVisualizer.HoverOverlayTileMap.SetTile(cellPos, glowyTile);
                lastHoveredCell = cellPos;
            }

            // Handle click
            if (Input.GetMouseButtonDown(0) && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
                // Only proceed if this tile is currently selectable (or selection not active)
                if (selectionTiles.Count == 0 || IsSelectable(cellPos))
                {
                    int tileId = TileIdFromCell(cellPos);
                    
                    // Try MultiCellSelectionController first, then fall back to TileSelectionController
                    if (MultiCellSelectionController.Instance != null && MultiCellSelectionController.Instance.IsSelectable(tileId))
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

            // Handle cancel (right-click or escape)
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectionTiles.Count > 0)
                {
                    // Try MultiCellSelectionController first, then fall back to TileSelectionController
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
            // Matches your Board's GetXYFromTileId logic
            int x = cell.x;
            int y = cell.y;
            return y * groundTilemap.size.x + x;
        }

        void TriggerPressedAnimation(Vector3Int cell)
        {
            gridVisualizer.HoverOverlayTileMap.SetTile(cell, pressedTile);

            CancelInvoke(nameof(RestoreGlowyTile));
            Invoke(nameof(RestoreGlowyTile), 0.25f);
        }

        void RestoreGlowyTile()
        {
            if (lastHoveredCell != null)
            {
                gridVisualizer.HoverOverlayTileMap.SetTile(lastHoveredCell.Value, glowyTile);
            }
        }

        bool IsCellOnBoard(Vector3Int cellPos)
        {
            return gridVisualizer.toastTilemap.cellBounds.Contains(cellPos);
        }
    }
}
