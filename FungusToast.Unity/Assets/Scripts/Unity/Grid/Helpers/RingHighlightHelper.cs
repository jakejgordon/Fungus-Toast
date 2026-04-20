using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Unity.UI;
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
            DrawRingHighlightWithoutClearing(centerPos, radius, thickness, color, targetTilemap);
        }

        public void DrawRingHighlights(IEnumerable<Vector3Int> centerPositions, float radius, float thickness, Color color, Tilemap targetTilemap)
        {
            ClearRingHighlight(targetTilemap);
            if (centerPositions == null)
            {
                return;
            }

            foreach (var centerPos in centerPositions)
            {
                DrawRingHighlightWithoutClearing(centerPos, radius, thickness, color, targetTilemap);
            }
        }

        private void DrawRingHighlightWithoutClearing(Vector3Int centerPos, float radius, float thickness, Color color, Tilemap targetTilemap)
        {
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

        private void DrawSpokesWithoutClearing(Vector3Int centerPos, float radius, float alpha, Tilemap targetTilemap)
        {
            int spokeCount = Mathf.Max(0, UIEffectConstants.StartingTilePingSpokeCount);
            if (spokeCount == 0 || targetTilemap == null || _solidHighlightTile == null)
            {
                return;
            }

            float innerRadius = Mathf.Max(0f, UIEffectConstants.StartingTilePingSpokeInnerRadiusTiles);
            float outerRadius = Mathf.Max(innerRadius, radius + UIEffectConstants.StartingTilePingSpokeOuterExtensionTiles);
            if (outerRadius <= innerRadius)
            {
                return;
            }

            Color spokeColor = UIEffectConstants.StartingTilePingSpokeColor;
            spokeColor.a *= Mathf.Clamp01(alpha);

            float angleStep = (Mathf.PI * 2f) / spokeCount;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt((outerRadius - innerRadius) * 4f));
            for (int spokeIndex = 0; spokeIndex < spokeCount; spokeIndex++)
            {
                float angle = (-Mathf.PI * 0.5f) + (spokeIndex * angleStep);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                for (int sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    float t = sampleCount == 0 ? 0f : sampleIndex / (float)sampleCount;
                    float distance = Mathf.Lerp(innerRadius, outerRadius, t);
                    var pos = new Vector3Int(
                        centerPos.x + Mathf.RoundToInt(direction.x * distance),
                        centerPos.y + Mathf.RoundToInt(direction.y * distance),
                        0);

                    targetTilemap.SetTile(pos, _solidHighlightTile);
                    targetTilemap.SetTileFlags(pos, TileFlags.None);
                    targetTilemap.SetColor(pos, spokeColor);
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

        public IEnumerator StartingTilePingAnimation(Vector3Int centerPos, Tilemap targetTilemap, float duration, float expandPortion, float maxRadius, float ringThickness, bool drawSpokes = false)
        {
            float contractPortion = 1f - expandPortion;
            float minVisibleRadius = UIEffectConstants.StartingTilePingMinVisibleRadiusTiles;

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
                    thickness = Mathf.Lerp(ringThickness, ringThickness * UIEffectConstants.StartingTilePingContractedThicknessScale, eased);
                }

                float alpha;
                float fadeInEnd = UIEffectConstants.StartingTilePingFadeInEndPortion;
                float fadeOutStart = UIEffectConstants.StartingTilePingFadeOutStartPortion;
                if (t < fadeInEnd)
                    alpha = Mathf.InverseLerp(0f, fadeInEnd, t);
                else if (t > fadeOutStart)
                    alpha = Mathf.InverseLerp(1f, fadeOutStart, t);
                else
                    alpha = 1f;

                Color ringColor = UIEffectConstants.StartingTilePingRingColor;
                ringColor.a = alpha;
                DrawRingHighlight(centerPos, radius, thickness, ringColor, targetTilemap);
                if (drawSpokes)
                {
                    DrawSpokesWithoutClearing(centerPos, radius, alpha, targetTilemap);
                }
                yield return null;
            }

            DrawRingHighlight(
                centerPos,
                minVisibleRadius * UIEffectConstants.StartingTilePingSettleRadiusScale,
                ringThickness * UIEffectConstants.StartingTilePingSettleThicknessScale,
                UIEffectConstants.StartingTilePingSettleColor,
                targetTilemap);
            if (drawSpokes)
            {
                DrawSpokesWithoutClearing(centerPos, minVisibleRadius, 1f, targetTilemap);
            }
            yield return null;
            ClearRingHighlight(targetTilemap);
        }

        public IEnumerator StartingTilePingAnimation(IEnumerable<Vector3Int> centerPositions, Tilemap targetTilemap, float duration, float expandPortion, float maxRadius, float ringThickness, bool drawSpokes = false)
        {
            if (centerPositions == null)
            {
                yield break;
            }

            List<Vector3Int> centers = centerPositions.Distinct().ToList();
            if (centers.Count == 0)
            {
                yield break;
            }

            float contractPortion = 1f - expandPortion;
            float minVisibleRadius = UIEffectConstants.StartingTilePingMinVisibleRadiusTiles;

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
                    thickness = Mathf.Lerp(ringThickness, ringThickness * UIEffectConstants.StartingTilePingContractedThicknessScale, eased);
                }

                float alpha;
                float fadeInEnd = UIEffectConstants.StartingTilePingFadeInEndPortion;
                float fadeOutStart = UIEffectConstants.StartingTilePingFadeOutStartPortion;
                if (t < fadeInEnd)
                    alpha = Mathf.InverseLerp(0f, fadeInEnd, t);
                else if (t > fadeOutStart)
                    alpha = Mathf.InverseLerp(1f, fadeOutStart, t);
                else
                    alpha = 1f;

                Color ringColor = UIEffectConstants.StartingTilePingRingColor;
                ringColor.a = alpha;
                DrawRingHighlights(centers, radius, thickness, ringColor, targetTilemap);
                if (drawSpokes)
                {
                    foreach (Vector3Int center in centers)
                    {
                        DrawSpokesWithoutClearing(center, radius, alpha, targetTilemap);
                    }
                }
                yield return null;
            }

            DrawRingHighlights(
                centers,
                minVisibleRadius * UIEffectConstants.StartingTilePingSettleRadiusScale,
                ringThickness * UIEffectConstants.StartingTilePingSettleThicknessScale,
                UIEffectConstants.StartingTilePingSettleColor,
                targetTilemap);
            if (drawSpokes)
            {
                foreach (Vector3Int center in centers)
                {
                    DrawSpokesWithoutClearing(center, minVisibleRadius, 1f, targetTilemap);
                }
            }
            yield return null;
            ClearRingHighlight(targetTilemap);
        }

        public Tilemap ChoosePingTarget() => _pingOverlayTileMap != null ? _pingOverlayTileMap : _hoverOverlayTileMap;
    }
}
