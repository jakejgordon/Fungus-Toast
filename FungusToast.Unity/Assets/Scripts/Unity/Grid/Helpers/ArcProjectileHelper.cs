using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>
    /// Generic arc projectile animator that renders a sprite along a parabolic arc between two grid cells.
    /// Uses a transient SpriteRenderer for smooth motion and scaling.
    /// </summary>
    internal class ArcProjectileHelper
    {
        private readonly MonoBehaviour _runner;
        private readonly Tilemap _referenceTilemap; // used to convert cells to world and to align sorting
        private readonly List<GameObject> _activeBatchProjectiles = new();

        public ArcProjectileHelper(MonoBehaviour runner, Tilemap referenceTilemap)
        {
            _runner = runner;
            _referenceTilemap = referenceTilemap;
        }

        public IEnumerator AnimateArc(
            Vector3Int startCell,
            Vector3Int endCell,
            Sprite sprite,
            float duration,
            float baseArcHeightWorld,
            float arcHeightPerTile,
            float scalePerHeightTile,
            float? peakScaleOverride = null)
        {
            if (_referenceTilemap == null || sprite == null)
                yield break;

            // Build transient GO
            var go = new GameObject("ArcProjectile");
            go.transform.SetParent(_referenceTilemap.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Match tilemap rendering order
            var tmr = _referenceTilemap.GetComponent<TilemapRenderer>();
            if (tmr != null)
            {
                sr.sortingLayerID = tmr.sortingLayerID;
                sr.sortingOrder = tmr.sortingOrder + 10; // ensure on top
            }

            // Compute world positions
            Vector3 startWorld = CellCenterWorld(startCell);
            Vector3 endWorld = CellCenterWorld(endCell);

            // Determine arc height proportional to distance
            float distanceTiles = Vector2.Distance(new Vector2(startCell.x, startCell.y), new Vector2(endCell.x, endCell.y));
            float arcHeightWorld = baseArcHeightWorld + distanceTiles * arcHeightPerTile * _referenceTilemap.cellSize.y;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                // Parabolic height profile: peak at u=0.5, 0 at ends
                float hNorm = 4f * u * (1f - u); // range [0..1]
                float height = hNorm * arcHeightWorld;

                Vector3 pos = Vector3.Lerp(startWorld, endWorld, u) + Vector3.up * height;
                go.transform.position = pos;

                // Scale by apex factor: grow towards mid, shrink back
                float peakScale = Mathf.Max(1f, peakScaleOverride ?? UI.UIEffectConstants.SurgicalInoculationArcPeakScale);
                float scaleEase = 1f - Mathf.Pow(1f - hNorm, 2f); // ease based on height
                float scale = Mathf.Lerp(1f, peakScale, scaleEase);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            Object.Destroy(go);
        }

        /// <summary>
        /// Renders a bounded volley in one coroutine. This deliberately avoids one
        /// coroutine per projectile, which is important for late-game toxin batches.
        /// </summary>
        public IEnumerator AnimateArcBatch(
            Vector3Int startCell,
            IReadOnlyList<Vector3Int> endCells,
            Sprite sprite,
            float duration,
            float baseArcHeightWorld,
            float arcHeightPerTile,
            float peakScale)
        {
            if (_referenceTilemap == null || sprite == null || endCells == null || endCells.Count == 0)
            {
                yield break;
            }

            var projectiles = new List<(GameObject gameObject, Vector3 startWorld, Vector3 endWorld, float arcHeightWorld)>(endCells.Count);
            var tilemapRenderer = _referenceTilemap.GetComponent<TilemapRenderer>();
            Vector3 startWorld = CellCenterWorld(startCell);
            try
            {
                foreach (Vector3Int endCell in endCells)
                {
                    var projectile = new GameObject("ToxinLaunchProjectile");
                    _activeBatchProjectiles.Add(projectile);
                    projectile.transform.SetParent(_referenceTilemap.transform, false);
                    var renderer = projectile.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    if (tilemapRenderer != null)
                    {
                        renderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                        renderer.sortingOrder = tilemapRenderer.sortingOrder + 10;
                    }

                    Vector3 endWorld = CellCenterWorld(endCell);
                    float distanceTiles = Vector2.Distance(new Vector2(startCell.x, startCell.y), new Vector2(endCell.x, endCell.y));
                    float arcHeightWorld = baseArcHeightWorld + distanceTiles * arcHeightPerTile * _referenceTilemap.cellSize.y;
                    projectiles.Add((projectile, startWorld, endWorld, arcHeightWorld));
                }

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float heightNormal = 4f * t * (1f - t);
                    float scaleEase = 1f - Mathf.Pow(1f - heightNormal, 2f);
                    float scale = Mathf.Lerp(1f, Mathf.Max(1f, peakScale), scaleEase);
                    foreach (var projectile in projectiles)
                    {
                        if (projectile.gameObject == null)
                        {
                            continue;
                        }

                        projectile.gameObject.transform.position = Vector3.Lerp(projectile.startWorld, projectile.endWorld, t)
                            + Vector3.up * (heightNormal * projectile.arcHeightWorld);
                        projectile.gameObject.transform.localScale = new Vector3(scale, scale, 1f);
                    }

                    yield return null;
                }
            }
            finally
            {
                foreach (var projectile in projectiles)
                {
                    if (projectile.gameObject != null)
                    {
						_activeBatchProjectiles.Remove(projectile.gameObject);
                        Object.Destroy(projectile.gameObject);
                    }
                }
            }
        }

        public void ClearBatchProjectiles()
        {
            foreach (GameObject projectile in _activeBatchProjectiles)
            {
                if (projectile != null)
                {
                    Object.Destroy(projectile);
                }
            }
            _activeBatchProjectiles.Clear();
        }

        private Vector3 CellCenterWorld(Vector3Int cell)
        {
            Vector3 baseWorld = _referenceTilemap.CellToWorld(cell);
            // Offset by half a cell to center
            var cs = _referenceTilemap.cellSize;
            return baseWorld + new Vector3(cs.x * 0.5f, cs.y * 0.5f, 0f);
        }
    }
}
