using System;
using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// The shared board vocabulary every ability icon is composed from, so a living cell, a
    /// resistant cell, a toxin or a starting spore looks the same on a surge card as on an
    /// adaptation tile. Coordinates are <see cref="IconCanvas"/> logical units (0..100).
    ///
    /// State is always conveyed by shape as well as colour (shield = resistant, X = dead,
    /// trefoil = toxin, outline = enemy), matching the style guide rule that toxin, resistance
    /// and death never read through hue alone.
    /// </summary>
    public static class IconGlyphs
    {
        /// <summary>Colours the glyphs share regardless of which family's accent is in play.</summary>
        public static class Palette
        {
            public static Color Toxin => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Text.Primary, 0.55f);
            public static Color Dead => UIStyleTokens.Text.Muted;
            public static Color Enemy => Color.Lerp(UIStyleTokens.State.Danger, UIStyleTokens.Text.Primary, 0.15f);
            public static Color Nutrient => UIStyleTokens.State.Success;
            public static Color Mark => UIStyleTokens.Text.Primary;
            public static Color Faint => UIStyleTokens.Text.Disabled;
        }

        public const float CellCornerRatio = 0.18f;

        /// <summary>Mark colour (shield, X) that stays legible on a cell of the given fill.</summary>
        public static Color MarkFor(Color fill)
        {
            float luminance = 0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b;
            return luminance > 0.6f ? UIStyleTokens.Surface.Canvas : Palette.Mark;
        }

        /// <summary>A four-point sparkle.</summary>
        public static void Sparkle(IconCanvas canvas, float cx, float cy, float radius, float thickness, Color color)
        {
            canvas.Line(cx - radius, cy, cx + radius, cy, thickness, color);
            canvas.Line(cx, cy - radius, cx, cy + radius, thickness, color);
            float d = radius * 0.45f;
            canvas.Line(cx - d, cy - d, cx + d, cy + d, thickness * 0.8f, color);
            canvas.Line(cx - d, cy + d, cx + d, cy - d, thickness * 0.8f, color);
        }

        /// <summary>A mutation-tier ladder: two rails with count rungs, bottom rung first.</summary>
        public static void Ladder(IconCanvas canvas, float left, float right, float bottom, float rungSpacing, int count, Color rail)
        {
            float top = bottom - rungSpacing * (count - 1);
            canvas.Line(left, top - 4f, left, bottom + 4f, 3f, rail);
            canvas.Line(right, top - 4f, right, bottom + 4f, 3f, rail);
            for (int i = 0; i < count; i++)
            {
                float y = bottom - rungSpacing * i;
                canvas.Line(left, y, right, y, 3f, rail);
            }
        }

        /// <summary>A living cell belonging to the icon's owner.</summary>
        public static void LivingCell(IconCanvas canvas, float cx, float cy, float size, Color fill)
        {
            canvas.FillRect(cx - size * 0.5f, cy - size * 0.5f, size, size, fill, size * CellCornerRatio);
        }

        /// <summary>A living cell that carries Resistance: the cell plus a small shield.</summary>
        public static void ResistantCell(IconCanvas canvas, float cx, float cy, float size, Color fill, Color mark)
        {
            LivingCell(canvas, cx, cy, size, fill);
            Shield(canvas, cx, cy, size * 0.32f, mark);
        }

        /// <summary>A dead cell: hollow, muted, crossed out.</summary>
        public static void DeadCell(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            float half = size * 0.5f;
            float stroke = Math.Max(2f, size * 0.12f);
            canvas.StrokeRect(cx - half, cy - half, size, size, stroke, color, size * CellCornerRatio);
            float inset = size * 0.28f;
            canvas.Line(cx - inset, cy - inset, cx + inset, cy + inset, stroke, color);
            canvas.Line(cx - inset, cy + inset, cx + inset, cy - inset, stroke, color);
        }

        /// <summary>A rival's living cell: outlined rather than filled.</summary>
        public static void EnemyCell(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            float half = size * 0.5f;
            float stroke = Math.Max(2f, size * 0.14f);
            canvas.StrokeRect(cx - half, cy - half, size, size, stroke, color, size * CellCornerRatio);
        }

        /// <summary>A rival's resistant cell: outline plus shield.</summary>
        public static void EnemyResistantCell(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            EnemyCell(canvas, cx, cy, size, color);
            Shield(canvas, cx, cy, size * 0.32f, color);
        }

        /// <summary>An empty tile: faint dotted outline.</summary>
        public static void EmptyTile(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            float half = size * 0.5f;
            float stroke = Math.Max(1.5f, size * 0.08f);
            var corners = new[]
            {
                new Vector2(cx - half, cy - half),
                new Vector2(cx + half, cy - half),
                new Vector2(cx + half, cy + half),
                new Vector2(cx - half, cy + half),
                new Vector2(cx - half, cy - half)
            };
            canvas.DashPath(corners, stroke, size * 0.22f, size * 0.16f, color);
        }

        /// <summary>A cell in the act of dissolving: dashed outline in the owner's colour.</summary>
        public static void DissolvingCell(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            EmptyTile(canvas, cx, cy, size, color);
        }

        /// <summary>A toxin: simplified biohazard trefoil, echoing the board's toxin overlay.</summary>
        public static void Toxin(IconCanvas canvas, float cx, float cy, float radius, Color color)
        {
            float lobeRadius = radius * 0.46f;
            float lobeOffset = radius * 0.54f;
            for (int i = 0; i < 3; i++)
            {
                float angle = (-90f + 120f * i) * (float)Math.PI / 180f;
                canvas.FillCircle(cx + (float)Math.Cos(angle) * lobeOffset, cy + (float)Math.Sin(angle) * lobeOffset, lobeRadius, color);
            }

            canvas.FillCircle(cx, cy, radius * 0.34f, color);
            canvas.FillCircle(cx, cy, radius * 0.14f, Color.Lerp(color, UIStyleTokens.Surface.Canvas, 0.75f));
        }

        /// <summary>The starting spore: filled dot with an orbit ring.</summary>
        public static void Spore(IconCanvas canvas, float cx, float cy, float radius, Color fill, Color ring)
        {
            canvas.FillCircle(cx, cy, radius * 0.58f, fill);
            canvas.Ring(cx, cy, radius, Math.Max(2f, radius * 0.24f), ring);
        }

        /// <summary>A rival's starting spore: ring only.</summary>
        public static void EnemySpore(IconCanvas canvas, float cx, float cy, float radius, Color color)
        {
            canvas.Ring(cx, cy, radius, Math.Max(2f, radius * 0.24f), color);
            canvas.FillCircle(cx, cy, radius * 0.22f, color);
        }

        /// <summary>A small shield, the board's resistance mark.</summary>
        public static void Shield(IconCanvas canvas, float cx, float cy, float halfHeight, Color color)
        {
            float w = halfHeight * 0.85f;
            canvas.FillPolygon(new[]
            {
                new Vector2(cx - w, cy - halfHeight),
                new Vector2(cx + w, cy - halfHeight),
                new Vector2(cx + w, cy + halfHeight * 0.15f),
                new Vector2(cx, cy + halfHeight),
                new Vector2(cx - w, cy + halfHeight * 0.15f)
            }, color);
        }

        /// <summary>The crust: a thick band along one canvas edge, inside the frame.</summary>
        public static void Crust(IconCanvas canvas, CrustEdge edge, float inset, float thickness, Color color)
        {
            float span = IconCanvas.Extent - inset * 2f;
            switch (edge)
            {
                case CrustEdge.Top:
                    canvas.FillRect(inset, inset, span, thickness, color);
                    break;
                case CrustEdge.Bottom:
                    canvas.FillRect(inset, IconCanvas.Extent - inset - thickness, span, thickness, color);
                    break;
                case CrustEdge.Left:
                    canvas.FillRect(inset, inset, thickness, span, color);
                    break;
                case CrustEdge.Right:
                    canvas.FillRect(IconCanvas.Extent - inset - thickness, inset, thickness, span, color);
                    break;
            }
        }

        /// <summary>Nutrient patch: a loose cluster of dots.</summary>
        public static void NutrientPatch(IconCanvas canvas, float cx, float cy, float radius, Color color)
        {
            float dot = radius * 0.3f;
            canvas.FillCircle(cx, cy, dot, color);
            canvas.FillCircle(cx - radius * 0.62f, cy - radius * 0.2f, dot * 0.85f, color);
            canvas.FillCircle(cx + radius * 0.55f, cy - radius * 0.45f, dot * 0.8f, color);
            canvas.FillCircle(cx + radius * 0.35f, cy + radius * 0.6f, dot * 0.9f, color);
            canvas.FillCircle(cx - radius * 0.4f, cy + radius * 0.62f, dot * 0.7f, color);
        }

        /// <summary>The Mycelial Surge mark: a bolt-like chevron.</summary>
        public static void SurgeChevron(IconCanvas canvas, float cx, float cy, float size, Color color)
        {
            float h = size * 0.5f;
            float w = size * 0.32f;
            canvas.FillPolygon(new[]
            {
                new Vector2(cx + w * 0.35f, cy - h),
                new Vector2(cx - w, cy + h * 0.15f),
                new Vector2(cx - w * 0.1f, cy + h * 0.15f),
                new Vector2(cx - w * 0.35f, cy + h),
                new Vector2(cx + w, cy - h * 0.15f),
                new Vector2(cx + w * 0.1f, cy - h * 0.15f)
            }, color);
        }

        /// <summary>A horizontal row of count pips, centred on (cx, cy).</summary>
        public static void Pips(IconCanvas canvas, float cx, float cy, int count, float radius, float spacing, Color color)
        {
            if (count <= 0)
            {
                return;
            }

            float start = cx - spacing * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                canvas.FillCircle(start + spacing * i, cy, radius, color);
            }
        }

        /// <summary>An X stroke over a target: the kill / clear mark.</summary>
        public static void KillMark(IconCanvas canvas, float cx, float cy, float halfSize, float thickness, Color color)
        {
            canvas.Line(cx - halfSize, cy - halfSize, cx + halfSize, cy + halfSize, thickness, color);
            canvas.Line(cx - halfSize, cy + halfSize, cx + halfSize, cy - halfSize, thickness, color);
        }

        /// <summary>
        /// Dashed flight arc from one point to another, bulging perpendicular to the chord by
        /// <paramref name="bulge"/> (positive bulges to the left of travel, i.e. upward on a
        /// left-to-right arc), ending in an arrow head at the destination.
        /// </summary>
        public static void FlightArc(IconCanvas canvas, float x0, float y0, float x1, float y1, float bulge, float thickness, Color color, bool arrowHead = true)
        {
            float mx = (x0 + x1) * 0.5f;
            float my = (y0 + y1) * 0.5f;
            float dx = x1 - x0;
            float dy = y1 - y0;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0f)
            {
                return;
            }

            float nx = dy / length;
            float ny = -dx / length;
            float controlX = mx + nx * bulge * 2f;
            float controlY = my + ny * bulge * 2f;
            canvas.DashedBezier(x0, y0, controlX, controlY, x1, y1, thickness, thickness * 1.6f, thickness * 1.3f, color);
            if (arrowHead)
            {
                float tx = x1 - controlX;
                float ty = y1 - controlY;
                float tangentLength = (float)Math.Sqrt(tx * tx + ty * ty);
                if (tangentLength > 0f)
                {
                    canvas.ArrowHead(x1, y1, tx / tangentLength, ty / tangentLength, thickness * 2.6f, color);
                }
            }
        }

        /// <summary>A dashed ring showing an effect radius.</summary>
        public static void Radius(IconCanvas canvas, float cx, float cy, float radius, float thickness, Color color)
        {
            canvas.DashedRing(cx, cy, radius, thickness, thickness * 1.8f, thickness * 1.4f, color);
        }
    }

    public enum CrustEdge
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
