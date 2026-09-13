using System;
using UnityEngine;

// Legacy 40px integer rasterizer, still used by the campaign panel's moldiness reward
// icons and the end-game panel. Ability icons (adaptations, mycovariants, surges) now use
// FungusToast.Unity.UI.Icons.IconCanvas instead.
namespace FungusToast.Unity.UI
{
    internal static class ProceduralIconUtility
    {
        public const int DefaultIconSize = 40;

        /// <param name="filterMode">
        /// Point keeps the pixel look but, under a ScaleWithScreenSize canvas, nearest sampling
        /// can skip whole texel columns when the icon is drawn smaller than its texture — the
        /// far-edge border is the first casualty. Pass Bilinear for icons that must keep a
        /// complete frame at every canvas scale.
        /// </param>
        /// <param name="borderThickness">Frame width in texels; 1 matches the original icons.</param>
        public static Sprite CreateSprite(
            string textureName,
            Color background,
            Color accent,
            Action<Texture2D, Color, Color> drawAction,
            int size = DefaultIconSize,
            FilterMode filterMode = FilterMode.Point,
            int borderThickness = 1)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = textureName
            };

            Fill(texture, background);
            for (int inset = 0; inset < borderThickness; inset++)
            {
                DrawBorder(texture, accent, inset);
            }

            var highlight = Color.Lerp(accent, Color.white, 0.32f);
            drawAction?.Invoke(texture, accent, highlight);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static int ComputeStableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                string source = value ?? string.Empty;
                for (int i = 0; i < source.Length; i++)
                {
                    hash ^= source[i];
                    hash *= 16777619;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }

        public static void Fill(Texture2D texture, Color color)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        public static void DrawBorder(Texture2D texture, Color color, int inset = 0)
        {
            int minX = inset;
            int minY = inset;
            int maxX = texture.width - 1 - inset;
            int maxY = texture.height - 1 - inset;
            for (int i = minX; i <= maxX; i++)
            {
                texture.SetPixel(i, minY, color);
                texture.SetPixel(i, maxY, color);
            }

            for (int i = minY; i <= maxY; i++)
            {
                texture.SetPixel(minX, i, color);
                texture.SetPixel(maxX, i, color);
            }
        }

        public static void FillShield(Texture2D texture, int centerX, int centerY, int halfWidth, int halfHeight, Color color)
        {
            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
            {
                for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                {
                    if (!IsInsideBounds(texture, x, y))
                    {
                        continue;
                    }

                    float normalizedX = Mathf.Abs(x - centerX) / (float)halfWidth;
                    bool withinTopHalf = y <= centerY && normalizedX <= 0.95f - ((centerY - y) / (float)(halfHeight * 3));
                    bool withinBottomHalf = y > centerY && normalizedX <= 1f - ((y - centerY) / (float)halfHeight);
                    if (withinTopHalf || withinBottomHalf)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        public static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!IsInsideBounds(texture, x, y))
                    {
                        continue;
                    }

                    int dx = x - centerX;
                    int dy = y - centerY;
                    if ((dx * dx) + (dy * dy) <= radiusSquared)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        public static void DrawRing(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            int inner = (radius - thickness) * (radius - thickness);
            int outer = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!IsInsideBounds(texture, x, y))
                    {
                        continue;
                    }

                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distance = (dx * dx) + (dy * dy);
                    if (distance <= outer && distance >= inner)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        public static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                PaintBrush(texture, x0, y0, thickness, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = error * 2;
                if (e2 > -dy)
                {
                    error -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        public static void PaintBrush(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (IsInsideBounds(texture, x, y))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        public static bool IsInsideBounds(Texture2D texture, int x, int y)
        {
            return x >= 0 && x < texture.width && y >= 0 && y < texture.height;
        }
    }
}