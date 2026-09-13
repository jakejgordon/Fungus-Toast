using System;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// Anti-aliased software rasterizer for ability icons (Adaptations, Mycovariants, Surges).
    ///
    /// Drawing happens in a resolution-independent 0..<see cref="Extent"/> coordinate space
    /// (origin top-left, y down) into a managed <see cref="Color"/> buffer. Every primitive is
    /// painted from a signed-distance function with one pixel of smoothing, so the same drawing
    /// code produces a crisp result at any texture size. Only <see cref="ToTexture"/> and
    /// <see cref="ToSprite"/> touch native Unity APIs; everything else also runs inside the
    /// <c>tools/icon-preview</c> harness, which is how icons are reviewed without opening Unity.
    /// </summary>
    public sealed class IconCanvas
    {
        /// <summary>Logical width and height of the drawing space.</summary>
        public const float Extent = 100f;

        /// <summary>
        /// Default texture size. Icons are displayed at 28-68 UI units (twice that on a 4K
        /// display), so 128 keeps the largest slot sharp while mipmaps handle the small ones.
        /// </summary>
        public const int DefaultSize = 128;

        private const float BezierFlattenSegments = 24f;

        private readonly Color[] pixels;
        private readonly float scale;

        public int Size { get; }

        /// <summary>Pixel buffer in <see cref="Texture2D"/> layout: row 0 is the bottom row.</summary>
        public Color[] Pixels => pixels;

        public IconCanvas(int size = DefaultSize)
        {
            Size = Math.Max(1, size);
            scale = Size / Extent;
            pixels = new Color[Size * Size];
        }

        // ------------------------------------------------------------------ area primitives

        public void Fill(Color color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        /// <summary>Frame hugging the canvas edge; the thickness is fully inside the canvas.</summary>
        public void Frame(Color color, float thickness, float cornerRadius = 0f)
        {
            float half = thickness * 0.5f;
            StrokeRect(half, half, Extent - thickness, Extent - thickness, thickness, color, Math.Max(0f, cornerRadius - half));
        }

        public void FillCircle(float cx, float cy, float radius, Color color)
        {
            Paint(cx - radius, cy - radius, cx + radius, cy + radius, color,
                (x, y) => Length(x - cx, y - cy) - radius);
        }

        public void Ring(float cx, float cy, float radius, float thickness, Color color)
        {
            float half = thickness * 0.5f;
            Paint(cx - radius - half, cy - radius - half, cx + radius + half, cy + radius + half, color,
                (x, y) => Math.Abs(Length(x - cx, y - cy) - radius) - half);
        }

        public void FillRect(float x, float y, float width, float height, Color color, float cornerRadius = 0f)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float cx = x + hw;
            float cy = y + hh;
            float r = Math.Min(cornerRadius, Math.Min(hw, hh));
            Paint(x, y, x + width, y + height, color,
                (px, py) => RoundBox(px - cx, py - cy, hw, hh, r));
        }

        public void StrokeRect(float x, float y, float width, float height, float thickness, Color color, float cornerRadius = 0f)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float cx = x + hw;
            float cy = y + hh;
            float half = thickness * 0.5f;
            float r = Math.Min(cornerRadius, Math.Min(hw, hh));
            Paint(x - half, y - half, x + width + half, y + height + half, color,
                (px, py) => Math.Abs(RoundBox(px - cx, py - cy, hw, hh, r)) - half);
        }

        public void FillPolygon(IReadOnlyList<Vector2> points, Color color)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            Bounds(points, out float minX, out float minY, out float maxX, out float maxY);
            Paint(minX, minY, maxX, maxY, color, (x, y) => PolygonDistance(points, x, y));
        }

        /// <summary>Regular polygon; <paramref name="rotationDegrees"/> 0 puts the first vertex straight up.</summary>
        public void FillRegularPolygon(float cx, float cy, float radius, int sides, Color color, float rotationDegrees = 0f)
        {
            var points = new Vector2[Math.Max(3, sides)];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = (rotationDegrees - 90f + (360f * i / points.Length)) * (float)Math.PI / 180f;
                points[i] = new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
            }

            FillPolygon(points, color);
        }

        // ------------------------------------------------------------------ stroke primitives

        /// <summary>Line segment with round caps.</summary>
        public void Line(float x0, float y0, float x1, float y1, float thickness, Color color)
        {
            float half = thickness * 0.5f;
            Paint(Math.Min(x0, x1) - half, Math.Min(y0, y1) - half, Math.Max(x0, x1) + half, Math.Max(y0, y1) + half, color,
                (x, y) => SegmentDistance(x, y, x0, y0, x1, y1) - half);
        }

        public void Polyline(IReadOnlyList<Vector2> points, float thickness, Color color, bool closed = false)
        {
            if (points == null || points.Count < 2)
            {
                return;
            }

            int segments = closed ? points.Count : points.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                Line(a.x, a.y, b.x, b.y, thickness, color);
            }
        }

        /// <summary>Arc of a circle; angles in degrees, 0 = right, increasing clockwise on screen.</summary>
        public void Arc(float cx, float cy, float radius, float startDegrees, float endDegrees, float thickness, Color color)
        {
            Polyline(FlattenArc(cx, cy, radius, startDegrees, endDegrees), thickness, color);
        }

        /// <summary>Arc whose radius grows linearly from startRadius to endRadius across the sweep.</summary>
        public void Spiral(float cx, float cy, float startRadius, float endRadius, float startDegrees, float endDegrees, float thickness, Color color)
        {
            Polyline(FlattenSpiral(cx, cy, startRadius, endRadius, startDegrees, endDegrees), thickness, color);
        }

        /// <summary>Quadratic Bezier stroke from (x0,y0) to (x2,y2) bending toward the control point.</summary>
        public void Bezier(float x0, float y0, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            Polyline(FlattenBezier(x0, y0, x1, y1, x2, y2), thickness, color);
        }

        public void DashedLine(float x0, float y0, float x1, float y1, float thickness, float dash, float gap, Color color)
        {
            DashPath(new[] { new Vector2(x0, y0), new Vector2(x1, y1) }, thickness, dash, gap, color);
        }

        public void DashedBezier(float x0, float y0, float x1, float y1, float x2, float y2, float thickness, float dash, float gap, Color color)
        {
            DashPath(FlattenBezier(x0, y0, x1, y1, x2, y2), thickness, dash, gap, color);
        }

        public void DashedArc(float cx, float cy, float radius, float startDegrees, float endDegrees, float thickness, float dash, float gap, Color color)
        {
            DashPath(FlattenArc(cx, cy, radius, startDegrees, endDegrees), thickness, dash, gap, color);
        }

        public void DashedRing(float cx, float cy, float radius, float thickness, float dash, float gap, Color color)
        {
            DashedArc(cx, cy, radius, 0f, 360f, thickness, dash, gap, color);
        }

        /// <summary>Walks a flattened path and strokes only the "on" spans of the dash pattern.</summary>
        public void DashPath(IReadOnlyList<Vector2> path, float thickness, float dash, float gap, Color color)
        {
            if (path == null || path.Count < 2 || dash <= 0f)
            {
                return;
            }

            float period = dash + Math.Max(0f, gap);
            float travelled = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i];
                Vector2 b = path[i + 1];
                float length = Length(b.x - a.x, b.y - a.y);
                if (length <= 0f)
                {
                    continue;
                }

                // Dashes are indexed along the whole path so no floating-point phase accumulates.
                float segmentEnd = travelled + length;
                int firstDash = (int)Math.Floor(travelled / period);
                int lastDash = (int)Math.Floor(segmentEnd / period);
                for (int k = firstDash; k <= lastDash; k++)
                {
                    float dashStart = Math.Max(travelled, k * period);
                    float dashEnd = Math.Min(segmentEnd, k * period + dash);
                    if (dashEnd <= dashStart)
                    {
                        continue;
                    }

                    float t0 = (dashStart - travelled) / length;
                    float t1 = (dashEnd - travelled) / length;
                    Line(
                        a.x + (b.x - a.x) * t0, a.y + (b.y - a.y) * t0,
                        a.x + (b.x - a.x) * t1, a.y + (b.y - a.y) * t1,
                        thickness, color);
                }

                travelled = segmentEnd;
            }
        }

        /// <summary>Straight arrow: a stroked shaft plus a filled triangular head whose tip is at (x1,y1).</summary>
        public void Arrow(float x0, float y0, float x1, float y1, float thickness, float headLength, Color color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float length = Length(dx, dy);
            if (length <= 0f)
            {
                return;
            }

            float ux = dx / length;
            float uy = dy / length;
            float shaftEndX = x1 - ux * headLength * 0.7f;
            float shaftEndY = y1 - uy * headLength * 0.7f;
            Line(x0, y0, shaftEndX, shaftEndY, thickness, color);
            ArrowHead(x1, y1, ux, uy, headLength, color);
        }

        /// <summary>Filled triangular arrow head with its tip at (tipX,tipY) pointing along (ux,uy).</summary>
        public void ArrowHead(float tipX, float tipY, float ux, float uy, float headLength, Color color)
        {
            float halfWidth = headLength * 0.6f;
            float baseX = tipX - ux * headLength;
            float baseY = tipY - uy * headLength;
            float px = -uy;
            float py = ux;
            FillPolygon(new[]
            {
                new Vector2(tipX, tipY),
                new Vector2(baseX + px * halfWidth, baseY + py * halfWidth),
                new Vector2(baseX - px * halfWidth, baseY - py * halfWidth)
            }, color);
        }

        // ------------------------------------------------------------------ path helpers

        public static List<Vector2> FlattenBezier(float x0, float y0, float x1, float y1, float x2, float y2)
        {
            var points = new List<Vector2>();
            for (int i = 0; i <= (int)BezierFlattenSegments; i++)
            {
                float t = i / BezierFlattenSegments;
                float u = 1f - t;
                points.Add(new Vector2(
                    u * u * x0 + 2f * u * t * x1 + t * t * x2,
                    u * u * y0 + 2f * u * t * y1 + t * t * y2));
            }

            return points;
        }

        public static List<Vector2> FlattenArc(float cx, float cy, float radius, float startDegrees, float endDegrees)
        {
            float sweep = endDegrees - startDegrees;
            int segments = Math.Max(4, (int)Math.Ceiling(Math.Abs(sweep) / 6f));
            var points = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (startDegrees + sweep * i / segments) * (float)Math.PI / 180f;
                points.Add(new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius));
            }

            return points;
        }

        public static List<Vector2> FlattenSpiral(float cx, float cy, float startRadius, float endRadius, float startDegrees, float endDegrees)
        {
            float sweep = endDegrees - startDegrees;
            int segments = Math.Max(4, (int)Math.Ceiling(Math.Abs(sweep) / 6f));
            var points = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float radius = startRadius + (endRadius - startRadius) * t;
                float angle = (startDegrees + sweep * t) * (float)Math.PI / 180f;
                points.Add(new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius));
            }

            return points;
        }

        // ------------------------------------------------------------------ Unity conversion

        public Texture2D ToTexture(string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: true)
            {
                name = name,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return texture;
        }

        public Sprite ToSprite(string name)
        {
            var texture = ToTexture(name);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ rasterization core

        /// <summary>
        /// Paints <paramref name="color"/> over every pixel whose centre lies within the logical
        /// bounds, using <paramref name="signedDistance"/> (logical units, negative inside) for
        /// one-pixel anti-aliased coverage.
        /// </summary>
        private void Paint(float minX, float minY, float maxX, float maxY, Color color, Func<float, float, float> signedDistance)
        {
            int pxMin = Math.Max(0, (int)Math.Floor(minX * scale) - 1);
            int pxMax = Math.Min(Size - 1, (int)Math.Ceiling(maxX * scale) + 1);
            int rowMin = Math.Max(0, (int)Math.Floor(minY * scale) - 1);
            int rowMax = Math.Min(Size - 1, (int)Math.Ceiling(maxY * scale) + 1);

            for (int row = rowMin; row <= rowMax; row++)
            {
                float y = (row + 0.5f) / scale;
                int bufferRow = Size - 1 - row;
                for (int px = pxMin; px <= pxMax; px++)
                {
                    float x = (px + 0.5f) / scale;
                    float coverage = Clamp01(0.5f - signedDistance(x, y) * scale);
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int index = bufferRow * Size + px;
                    pixels[index] = Blend(pixels[index], color, coverage);
                }
            }
        }

        private static Color Blend(Color dst, Color src, float coverage)
        {
            float a = src.a * coverage;
            if (a <= 0f)
            {
                return dst;
            }

            float outA = a + dst.a * (1f - a);
            if (outA <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            float w = dst.a * (1f - a);
            return new Color(
                (src.r * a + dst.r * w) / outA,
                (src.g * a + dst.g * w) / outA,
                (src.b * a + dst.b * w) / outA,
                outA);
        }

        private static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

        private static float Length(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        private static float RoundBox(float dx, float dy, float halfWidth, float halfHeight, float radius)
        {
            float qx = Math.Abs(dx) - halfWidth + radius;
            float qy = Math.Abs(dy) - halfHeight + radius;
            float outside = Length(Math.Max(qx, 0f), Math.Max(qy, 0f));
            float inside = Math.Min(Math.Max(qx, qy), 0f);
            return outside + inside - radius;
        }

        private static float SegmentDistance(float x, float y, float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float lengthSquared = dx * dx + dy * dy;
            float t = lengthSquared <= 0f ? 0f : Clamp01(((x - x0) * dx + (y - y0) * dy) / lengthSquared);
            return Length(x - (x0 + dx * t), y - (y0 + dy * t));
        }

        private static float PolygonDistance(IReadOnlyList<Vector2> points, float x, float y)
        {
            float distanceSquared = float.MaxValue;
            float sign = 1f;
            int count = points.Count;
            for (int i = 0, j = count - 1; i < count; j = i, i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[j];
                float ex = b.x - a.x;
                float ey = b.y - a.y;
                float wx = x - a.x;
                float wy = y - a.y;
                float lengthSquared = ex * ex + ey * ey;
                float t = lengthSquared <= 0f ? 0f : Clamp01((wx * ex + wy * ey) / lengthSquared);
                float bx = wx - ex * t;
                float by = wy - ey * t;
                distanceSquared = Math.Min(distanceSquared, bx * bx + by * by);

                bool c0 = y >= a.y;
                bool c1 = y < b.y;
                bool c2 = ex * wy > ey * wx;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2))
                {
                    sign = -sign;
                }
            }

            return sign * (float)Math.Sqrt(distanceSquared);
        }

        private static void Bounds(IReadOnlyList<Vector2> points, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                minX = Math.Min(minX, points[i].x);
                minY = Math.Min(minY, points[i].y);
                maxX = Math.Max(maxX, points[i].x);
                maxY = Math.Max(maxY, points[i].y);
            }
        }
    }
}
