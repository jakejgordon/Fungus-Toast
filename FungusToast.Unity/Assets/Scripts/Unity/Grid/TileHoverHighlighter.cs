using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using FungusToast.Unity.UI;

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

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = gridVisualizer.toastTilemap.WorldToCell(mouseWorldPos);

            if (!IsCellOnBoard(cellPos))
            {
                gridVisualizer?.ClearHoverEffect();
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
                lastHoveredCell = null;
                return;
            }

            bool isSelectable = selectionTiles.Count == 0 || IsSelectable(cellPos);

            if (isSelectable && gridVisualizer.toastTilemap.HasTile(cellPos))
            {
                if (lastHoveredCell != cellPos)
                {
                    gridVisualizer?.ShowHoverEffect(cellPos);
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
                gridVisualizer?.ClearHoverEffect();
                if (crosshairInstance != null)
                    crosshairInstance.SetActive(false);
                lastHoveredCell = null;
            }

            if (Input.GetMouseButtonDown(0) && gridVisualizer.toastTilemap.HasTile(cellPos))
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
            var board = GameManager.Instance != null ? GameManager.Instance.Board : null;
            if (board == null) return -1;
            int x = cell.x;
            int y = cell.y;
            return y * board.Width + x;
        }

        void TriggerPressedAnimation(Vector3Int cell) { }

        bool IsCellOnBoard(Vector3Int cellPos)
        {
            return gridVisualizer.toastTilemap.cellBounds.Contains(cellPos);
        }

        public void SetSelectableTiles(HashSet<int> selectableTileIds)
        {
            selectionTiles = selectableTileIds ?? new HashSet<int>();
        }

        public void ClearSelectableTiles()
        {
            selectionTiles.Clear();
        }
    }
}
