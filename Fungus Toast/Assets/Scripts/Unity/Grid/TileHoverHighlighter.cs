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

        // Crosshair prefab to use for hover highlight
        public GameObject crosshairPrefab;
        private GameObject crosshairInstance;

        private Vector3Int? lastHoveredCell = null;
        // New: Set of currently "selectable" tile IDs for interaction
        private HashSet<int> selectionTiles = new();

        void Start()
        {
            if (crosshairPrefab != null)
            {
                crosshairInstance = Instantiate(crosshairPrefab, this.transform); // Child of Grid
                crosshairInstance.SetActive(false);
            }
        }

        void Update()
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorldPos);

            if (!IsCellOnBoard(cellPos))
            {
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
                return;
            }

            bool isSelectable = selectionTiles.Count == 0 || IsSelectable(cellPos);

            if (isSelectable && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
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
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
            }

            // Handle click
            if (Input.GetMouseButtonDown(0) && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
                if (selectionTiles.Count == 0 || IsSelectable(cellPos))
                {
                    int tileId = TileIdFromCell(cellPos);
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
            // Optionally, you can add a pressed effect to the crosshair here
            // For now, just keep the crosshair visible
        }

        void RestoreGlowyTile()
        {
            // No longer needed
        }

        bool IsCellOnBoard(Vector3Int cellPos)
        {
            return gridVisualizer.toastTilemap.cellBounds.Contains(cellPos);
        }
    }
}
