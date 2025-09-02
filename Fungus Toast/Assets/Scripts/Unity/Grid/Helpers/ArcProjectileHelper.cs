using System.Collections;
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
            float scalePerHeightTile)
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
                float peakScale = Mathf.Max(1f, UI.UIEffectConstants.SurgicalInoculationArcPeakScale);
                float scaleEase = 1f - Mathf.Pow(1f - hNorm, 2f); // ease based on height
                float scale = Mathf.Lerp(1f, peakScale, scaleEase);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            Object.Destroy(go);
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
