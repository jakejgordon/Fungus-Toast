using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid
{
    [ExecuteAlways] // Runs in Edit Mode *and* Play Mode
    [RequireComponent(typeof(Tilemap))]
    public class GridFiller : MonoBehaviour
    {
        public Tile tileToUse;
        public Vector2Int gridSize = new Vector2Int(100, 100);

        private Vector2Int _lastFilledSize;
        private Tile _lastTileUsed;

        void OnEnable()
        {
            // Only run in the Editor or at runtime when the script is enabled
            TryFillGrid();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Runs when you change grid size or tile in the Inspector
            TryFillGrid();
        }
#endif

        void TryFillGrid()
        {
            // Don't run without a tile or in Prefab preview
            if (tileToUse == null || gridSize.x <= 0 || gridSize.y <= 0)
                return;

            // Avoid unnecessary re-fills
            if (_lastFilledSize == gridSize && _lastTileUsed == tileToUse)
                return;

            FillGrid();
            _lastFilledSize = gridSize;
            _lastTileUsed = tileToUse;
        }

        void FillGrid()
        {
            Tilemap tilemap = GetComponent<Tilemap>();
            tilemap.ClearAllTiles();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileToUse);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(tilemap); // Mark tilemap as modified so Unity saves it
#endif

        }
    }

}

