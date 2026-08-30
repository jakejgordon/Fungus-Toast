using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace FungusToast.Unity.Grid
{
    /// <summary>
    /// Fills the attached <see cref="Tilemap"/> with a single tile.
    ///
    /// At runtime this paints the starting substrate before <see cref="GridVisualizer"/>
    /// rebuilds the board. In the editor the fill is a <b>non-persistent preview only</b>:
    /// it is opt-in (Fungus Toast ▸ Grid ▸ Toggle Edit-Mode Board Preview) and is wiped
    /// immediately before the scene is written to disk. This keeps ~100k lines of baked
    /// tile data out of SampleScene.unity, which was the single largest source of scene
    /// churn and merge conflicts.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Tilemap))]
    public class GridFiller : MonoBehaviour
    {
        public Tile tileToUse;
        public Vector2Int gridSize = new Vector2Int(100, 100);

        private Vector2Int _lastFilledSize;
        private Tile _lastTileUsed;
        private Tilemap _tilemap;

        private Tilemap CachedTilemap => _tilemap != null ? _tilemap : (_tilemap = GetComponent<Tilemap>());

#if UNITY_EDITOR
        private const string EditorPreviewPref = "FungusToast.GridFiller.EditorPreview";
        private const string EditorPreviewMenu = "Fungus Toast/Grid/Toggle Edit-Mode Board Preview";

        private static bool EditorPreviewEnabled
        {
            get => EditorPrefs.GetBool(EditorPreviewPref, false);
            set => EditorPrefs.SetBool(EditorPreviewPref, value);
        }

        private bool _fillQueued;

        [MenuItem(EditorPreviewMenu, priority = 200)]
        private static void ToggleEditorPreview()
        {
            EditorPreviewEnabled = !EditorPreviewEnabled;
            foreach (GridFiller filler in FindObjectsByType<GridFiller>())
            {
                filler.QueueEditorPreview();
            }
        }

        [MenuItem(EditorPreviewMenu, validate = true)]
        private static bool ToggleEditorPreviewValidate()
        {
            Menu.SetChecked(EditorPreviewMenu, EditorPreviewEnabled);
            return !Application.isPlaying;
        }
#endif

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                TryFillGrid();
                return;
            }

#if UNITY_EDITOR
            EditorSceneManager.sceneSaving += HandleSceneSaving;
            EditorSceneManager.sceneSaved += HandleSceneSaved;
            QueueEditorPreview();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving -= HandleSceneSaving;
            EditorSceneManager.sceneSaved -= HandleSceneSaved;
            if (_fillQueued)
            {
                EditorApplication.delayCall -= FlushQueuedPreview;
                _fillQueued = false;
            }
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying)
                return;

            QueueEditorPreview();
        }

        private void QueueEditorPreview()
        {
            if (_fillQueued)
                return;

            _fillQueued = true;
            EditorApplication.delayCall += FlushQueuedPreview;
        }

        void FlushQueuedPreview()
        {
            EditorApplication.delayCall -= FlushQueuedPreview;
            _fillQueued = false;

            if (this == null || !isActiveAndEnabled || Application.isPlaying)
                return;

            if (EditorPreviewEnabled)
            {
                TryFillGrid();
            }
            else
            {
                ClearPreview();
            }
        }

        // The preview must never reach disk: clear it just before the scene is
        // serialized, then repaint it once the save completes.
        void HandleSceneSaving(Scene scene, string path)
        {
            if (Application.isPlaying || this == null || gameObject.scene != scene)
                return;

            CachedTilemap.ClearAllTiles();
        }

        void HandleSceneSaved(Scene scene)
        {
            if (Application.isPlaying || this == null || gameObject.scene != scene)
                return;

            _lastFilledSize = default;
            _lastTileUsed = null;
            if (EditorPreviewEnabled)
                QueueEditorPreview();
        }

        void ClearPreview()
        {
            Tilemap tilemap = CachedTilemap;
            if (tilemap.GetUsedTilesCount() == 0)
                return;

            tilemap.ClearAllTiles();
            _lastFilledSize = default;
            _lastTileUsed = null;
        }
#endif

        void TryFillGrid()
        {
            if (tileToUse == null || gridSize.x <= 0 || gridSize.y <= 0)
                return;

            if (_lastFilledSize == gridSize && _lastTileUsed == tileToUse)
                return;

            FillGrid();
            _lastFilledSize = gridSize;
            _lastTileUsed = tileToUse;
        }

        void FillGrid()
        {
            Tilemap tilemap = CachedTilemap;
            tilemap.ClearAllTiles();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileToUse);
                }
            }

            // Deliberately no EditorUtility.SetDirty in the editor: the edit-mode
            // fill is a throwaway preview, and marking the tilemap dirty is exactly
            // what baked 100k lines of tile data into SampleScene.unity.
        }
    }
}
