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
        private Tilemap _tilemap;

#if UNITY_EDITOR
        private bool _fillQueued;
#endif

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                TryFillGrid();
                return;
            }

#if UNITY_EDITOR
            QueueEditorFill();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (_fillQueued)
            {
                UnityEditor.EditorApplication.delayCall -= FlushQueuedFill;
                _fillQueued = false;
            }
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            QueueEditorFill();
        }

        void QueueEditorFill()
        {
            if (_fillQueued)
                return;

            _fillQueued = true;
            UnityEditor.EditorApplication.delayCall += FlushQueuedFill;
        }

        void FlushQueuedFill()
        {
            UnityEditor.EditorApplication.delayCall -= FlushQueuedFill;
            _fillQueued = false;

            if (this == null || !isActiveAndEnabled || Application.isPlaying)
                return;

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
            Tilemap tilemap = _tilemap != null ? _tilemap : (_tilemap = GetComponent<Tilemap>());
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

