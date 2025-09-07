using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>
    /// Encapsulates ring drawing and starting tile ping logic.
    /// Tilemap writes are handled here; caller decides when to start coroutines.
    /// </summary>
    internal class RingHighlightHelper
    {
        private readonly Tilemap _pingOverlayTileMap;
        private readonly Tilemap _hoverOverlayTileMap;
        private readonly Tile _solidHighlightTile;

        private readonly HashSet<Vector3Int> _ringHighlightPositions = new HashSet<Vector3Int>();

        public RingHighlightHelper(Tilemap pingOverlay, Tilemap hoverOverlay, Tile solidTile)
        {
            _pingOverlayTileMap = pingOverlay;
            _hoverOverlayTileMap = hoverOverlay;
            _solidHighlightTile = solidTile;
        }

        public void DrawRingHighlight(Vector3Int centerPos, float radius, float thickness, Color color, Tilemap targetTilemap)
        {
            ClearRingHighlight(targetTilemap);
            if (radius <= 0f) return;
            float outerSq = radius * radius;
            float inner = Mathf.Max(0f, radius - thickness);
            float innerSq = inner * inner;
            int rInt = Mathf.CeilToInt(radius + 1f);
            for (int dx = -rInt; dx <= rInt; dx++)
            {
                for (int dy = -rInt; dy <= rInt; dy++)
                {
                    float d2 = dx * dx + dy * dy;
                    if (d2 > outerSq || d2 < innerSq) continue;
                    var pos = new Vector3Int(centerPos.x + dx, centerPos.y + dy, 0);
                    targetTilemap.SetTile(pos, _solidHighlightTile);
                    targetTilemap.SetTileFlags(pos, TileFlags.None);
                    targetTilemap.SetColor(pos, color);
                    _ringHighlightPositions.Add(pos);
                }
            }
        }

        public void ClearRingHighlight(Tilemap targetTilemap)
        {
            foreach (var pos in _ringHighlightPositions)
                if (targetTilemap.HasTile(pos)) targetTilemap.SetTile(pos, null);
            _ringHighlightPositions.Clear();
        }

        public IEnumerator StartingTilePingAnimation(Vector3Int centerPos, Tilemap targetTilemap, float duration, float expandPortion, float maxRadius, float ringThickness)
        {
            float contractPortion = 1f - expandPortion;
            float minVisibleRadius = 0.5f;

            float startTime = Time.time;
            while (true)
            {
                float elapsed = Time.time - startTime;
                if (elapsed > duration) break;
                float t = Mathf.Clamp01(elapsed / duration);

                float radius;
                float thickness = ringThickness;

                if (t <= expandPortion)
                {
                    float phaseT = t / expandPortion;
                    float eased = 1f - Mathf.Pow(1f - phaseT, 3f);
                    radius = Mathf.Lerp(minVisibleRadius, maxRadius, eased);
                }
                else
                {
                    float phaseT = (t - expandPortion) / contractPortion;
                    float eased = phaseT * phaseT * phaseT;
                    radius = Mathf.Lerp(maxRadius, minVisibleRadius, eased);
                    thickness = Mathf.Lerp(ringThickness, ringThickness * 0.35f, eased);
                }

                float alpha;
                const float fadeInEnd = 0.06f;
                const float fadeOutStart = 0.85f;
                if (t < fadeInEnd)
                    alpha = Mathf.InverseLerp(0f, fadeInEnd, t);
                else if (t > fadeOutStart)
                    alpha = Mathf.InverseLerp(1f, fadeOutStart, t);
                else
                    alpha = 1f;

                Color ringColor = new Color(1f, 0.85f, 0.15f, alpha);
                DrawRingHighlight(centerPos, radius, thickness, ringColor, targetTilemap);
                yield return null;
            }

            DrawRingHighlight(centerPos, minVisibleRadius * 0.65f, ringThickness * 0.5f, new Color(1f, 0.95f, 0.3f, 0.95f), targetTilemap);
            yield return null;
            ClearRingHighlight(targetTilemap);
        }

        public Tilemap ChoosePingTarget() => _pingOverlayTileMap != null ? _pingOverlayTileMap : _hoverOverlayTileMap;
    }
}
