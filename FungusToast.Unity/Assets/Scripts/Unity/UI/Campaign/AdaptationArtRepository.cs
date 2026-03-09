using FungusToast.Core.Campaign;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.UI.Campaign
{
    public static class AdaptationArtRepository
    {
        private const int IconSize = 40;
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite GetIcon(AdaptationDefinition adaptation)
        {
            if (adaptation == null)
            {
                return GetFallbackIcon();
            }

            string cacheKey = adaptation.IconId;
            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var sprite = BuildIcon(cacheKey);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite GetFallbackIcon()
        {
            const string fallbackKey = "fallback";
            if (Cache.TryGetValue(fallbackKey, out var cached))
            {
                return cached;
            }

            var sprite = BuildIcon(string.Empty);
            Cache[fallbackKey] = sprite;
            return sprite;
        }

        private static Sprite BuildIcon(string adaptationId)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"AdaptationIcon_{adaptationId}"
            };

            var background = ResolveBackground(adaptationId);
            var accent = ResolveAccent(adaptationId);
            var highlight = Color.Lerp(accent, Color.white, 0.32f);

            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    texture.SetPixel(x, y, background);
                }
            }

            DrawBorder(texture, accent);

            switch (adaptationId)
            {
                case "conidial_relay":
                    DrawConidialRelay(texture, accent, highlight);
                    break;
                case "hyphal_economy":
                    DrawHyphalEconomy(texture, accent, highlight);
                    break;
                case "mycotoxic_halo":
                    DrawMycotoxicHalo(texture, accent, highlight);
                    break;
                default:
                    DrawFallback(texture, accent, highlight);
                    break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Color ResolveBackground(string adaptationId)
        {
            return adaptationId switch
            {
                "conidial_relay" => UIStyleTokens.Surface.PanelSecondary,
                "hyphal_economy" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.45f),
                "mycotoxic_halo" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.4f),
                _ => UIStyleTokens.Surface.PanelPrimary
            };
        }

        private static Color ResolveAccent(string adaptationId)
        {
            return adaptationId switch
            {
                "conidial_relay" => UIStyleTokens.State.Info,
                "hyphal_economy" => UIStyleTokens.State.Success,
                "mycotoxic_halo" => UIStyleTokens.State.Warning,
                _ => UIStyleTokens.Text.Primary
            };
        }

        private static void DrawBorder(Texture2D texture, Color color)
        {
            int max = IconSize - 1;
            for (int i = 0; i < IconSize; i++)
            {
                texture.SetPixel(i, 0, color);
                texture.SetPixel(i, max, color);
                texture.SetPixel(0, i, color);
                texture.SetPixel(max, i, color);
            }
        }

        private static void DrawConidialRelay(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 10, 10, 4, highlight);
            FillCircle(texture, 29, 29, 4, highlight);
            FillCircle(texture, 29, 10, 3, accent);
            DrawLine(texture, 13, 12, 26, 26, accent, 2);
            DrawLine(texture, 12, 10, 25, 10, accent, 1);
            DrawLine(texture, 29, 13, 29, 25, accent, 1);
        }

        private static void DrawHyphalEconomy(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 8, 31, 20, 8, accent, 2);
            DrawLine(texture, 20, 8, 30, 14, highlight, 2);
            DrawLine(texture, 16, 18, 31, 27, accent, 2);
            DrawLine(texture, 11, 24, 23, 31, highlight, 2);
            FillCircle(texture, 20, 8, 3, highlight);
            FillCircle(texture, 30, 14, 2, highlight);
        }

        private static void DrawMycotoxicHalo(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 10, 2, accent);
            DrawRing(texture, 20, 20, 6, 2, highlight);
            FillCircle(texture, 20, 20, 2, accent);
            FillCircle(texture, 10, 20, 2, highlight);
            FillCircle(texture, 30, 20, 2, highlight);
            FillCircle(texture, 20, 10, 2, highlight);
            FillCircle(texture, 20, 30, 2, highlight);
        }

        private static void DrawFallback(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 8, 8, 31, 31, accent, 2);
            DrawLine(texture, 31, 8, 8, 31, highlight, 2);
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!IsInsideBounds(x, y))
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

        private static void DrawRing(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            int inner = (radius - thickness) * (radius - thickness);
            int outer = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!IsInsideBounds(x, y))
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

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
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

        private static void PaintBrush(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (IsInsideBounds(x, y))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool IsInsideBounds(int x, int y)
        {
            return x >= 0 && x < IconSize && y >= 0 && y < IconSize;
        }
    }
}