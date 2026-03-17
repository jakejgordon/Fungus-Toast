using System;
using FungusToast.Core.Campaign;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Unity.UI;

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
            var background = ResolveBackground(adaptationId);
            var accent = ResolveAccent(adaptationId);
            return ProceduralIconUtility.CreateSprite(
                $"AdaptationIcon_{adaptationId}",
                background,
                accent,
                (texture, drawAccent, highlight) =>
                {
                    switch (adaptationId)
                    {
                        case "conidial_relay":
                            DrawConidialRelay(texture, drawAccent, highlight);
                            break;
                        case "hyphal_economy":
                            DrawHyphalEconomy(texture, drawAccent, highlight);
                            break;
                        case "mycotoxic_halo":
                            DrawMycotoxicHalo(texture, drawAccent, highlight);
                            break;
                        case "mycotoxic_lash":
                            DrawMycotoxicLash(texture, drawAccent, highlight);
                            break;
                        case "retrograde_bloom":
                            DrawRetrogradeBloom(texture, drawAccent, highlight);
                            break;
                        case "aegis_hyphae":
                            DrawAegisHyphae(texture, drawAccent, highlight);
                            break;
                        case "saprophage_ring":
                            DrawSaprophageRing(texture, drawAccent, highlight);
                            break;
                        case "marginal_clamp":
                            DrawMarginalClamp(texture, drawAccent, highlight);
                            break;
                        case "apical_yield":
                            DrawApicalYield(texture, drawAccent, highlight);
                            break;
                        case "crustal_callus":
                            DrawCrustalCallus(texture, drawAccent, highlight);
                            break;
                        case "distal_spore":
                            DrawDistalSpore(texture, drawAccent, highlight);
                            break;
                        case "ascus_primacy":
                            DrawAscusPrimacy(texture, drawAccent, highlight);
                            break;
                        case "spore_salvo":
                            DrawSporeSalvo(texture, drawAccent, highlight);
                            break;
                        case "vesicle_burst":
                            DrawVesicleBurst(texture, drawAccent, highlight);
                            break;
                        default:
                            DrawFallback(texture, drawAccent, highlight);
                            break;
                    }
                },
                IconSize);
        }

        private static Color ResolveBackground(string adaptationId)
        {
            return adaptationId switch
            {
                "conidial_relay" => UIStyleTokens.Surface.PanelSecondary,
                "hyphal_economy" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.45f),
                "mycotoxic_halo" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.4f),
                "mycotoxic_lash" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.52f),
                "retrograde_bloom" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "aegis_hyphae" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.58f),
                "saprophage_ring" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.6f),
                "marginal_clamp" => Color.Lerp(UIStyleTokens.State.Danger, UIStyleTokens.Surface.PanelPrimary, 0.55f),
                "apical_yield" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.48f),
                "crustal_callus" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "distal_spore" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.52f),
                "ascus_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "spore_salvo" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.48f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Accent.Putrefaction, 0.3f),
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
                "mycotoxic_lash" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Danger, 0.45f),
                "retrograde_bloom" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Warning, 0.2f),
                "aegis_hyphae" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.15f),
                "saprophage_ring" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.State.Warning, 0.25f),
                "marginal_clamp" => Color.Lerp(UIStyleTokens.State.Danger, UIStyleTokens.State.Warning, 0.2f),
                "apical_yield" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Warning, 0.25f),
                "crustal_callus" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.State.Info, 0.2f),
                "distal_spore" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.08f),
                "ascus_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.12f),
                "spore_salvo" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Danger, 0.35f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Putrefaction, 0.35f),
                _ => UIStyleTokens.Text.Primary
            };
        }

        private static void DrawBorder(Texture2D texture, Color color)
        {
            ProceduralIconUtility.DrawBorder(texture, color);
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

        private static void DrawMycotoxicLash(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 20, 10, 4, highlight);
            DrawLine(texture, 20, 14, 20, 26, accent, 2);
            DrawLine(texture, 20, 26, 13, 32, accent, 2);
            DrawLine(texture, 20, 26, 27, 32, accent, 2);
            DrawLine(texture, 10, 20, 15, 20, highlight, 1);
            DrawLine(texture, 25, 20, 30, 20, highlight, 1);
        }

        private static void DrawRetrogradeBloom(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 11, 2, accent);
            FillCircle(texture, 20, 20, 3, highlight);
            FillCircle(texture, 20, 8, 3, highlight);
            FillCircle(texture, 32, 20, 3, highlight);
            FillCircle(texture, 20, 32, 3, highlight);
            FillCircle(texture, 8, 20, 3, highlight);
            DrawLine(texture, 12, 28, 26, 14, accent, 1);
            DrawLine(texture, 26, 14, 22, 14, accent, 1);
            DrawLine(texture, 26, 14, 26, 18, accent, 1);
        }

        private static void DrawAegisHyphae(Texture2D texture, Color accent, Color highlight)
        {
            FillShield(texture, 20, 21, 10, 12, accent);
            DrawLine(texture, 20, 11, 20, 28, highlight, 1);
            DrawLine(texture, 20, 18, 13, 24, highlight, 1);
            DrawLine(texture, 20, 18, 27, 24, highlight, 1);
            FillCircle(texture, 20, 11, 2, highlight);
            FillCircle(texture, 13, 24, 2, highlight);
            FillCircle(texture, 27, 24, 2, highlight);
        }

        private static void DrawSaprophageRing(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 12, 3, accent);
            DrawRing(texture, 20, 20, 7, 2, highlight);
            FillCircle(texture, 20, 20, 3, new Color(0f, 0f, 0f, 0f));
            FillCircle(texture, 20, 8, 2, highlight);
            FillCircle(texture, 32, 20, 2, highlight);
            FillCircle(texture, 20, 32, 2, highlight);
            FillCircle(texture, 8, 20, 2, highlight);
        }

        private static void DrawMarginalClamp(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 6, 7, 6, 33, accent, 2);
            DrawLine(texture, 6, 7, 12, 7, accent, 2);
            DrawLine(texture, 6, 33, 12, 33, accent, 2);
            FillCircle(texture, 15, 13, 4, highlight);
            FillCircle(texture, 15, 27, 4, highlight);
            FillCircle(texture, 25, 13, 4, accent);
            FillCircle(texture, 25, 27, 4, accent);
            DrawLine(texture, 18, 13, 22, 13, highlight, 1);
            DrawLine(texture, 18, 27, 22, 27, highlight, 1);
            DrawLine(texture, 10, 20, 29, 20, accent, 1);
        }

        private static void DrawApicalYield(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 20, 30, 20, 12, accent, 2);
            DrawLine(texture, 20, 12, 14, 18, accent, 2);
            DrawLine(texture, 20, 12, 26, 18, accent, 2);
            DrawLine(texture, 13, 29, 27, 29, highlight, 1);
            FillCircle(texture, 11, 10, 3, highlight);
            FillCircle(texture, 29, 10, 3, highlight);
            FillCircle(texture, 20, 19, 2, highlight);
        }

        private static void DrawCrustalCallus(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 7, 31, 32, 31, accent, 2);
            DrawLine(texture, 7, 31, 7, 11, accent, 2);
            FillCircle(texture, 15, 24, 5, highlight);
            FillCircle(texture, 23, 20, 5, accent);
            FillCircle(texture, 28, 14, 3, highlight);
            DrawLine(texture, 12, 17, 18, 13, accent, 1);
            DrawLine(texture, 20, 14, 26, 10, highlight, 1);
        }

        private static void DrawDistalSpore(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 10, 10, 4, highlight);
            DrawLine(texture, 13, 12, 28, 27, accent, 1);
            DrawLine(texture, 15, 11, 26, 11, accent, 1);
            FillCircle(texture, 29, 29, 4, accent);
            FillShield(texture, 29, 30, 6, 7, highlight);
            DrawLine(texture, 20, 20, 25, 25, highlight, 1);
        }

        private static void DrawAscusPrimacy(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 12, 11, 4, highlight);
            FillCircle(texture, 20, 11, 4, accent);
            FillCircle(texture, 28, 11, 4, highlight);
            DrawLine(texture, 12, 16, 20, 25, accent, 1);
            DrawLine(texture, 20, 16, 20, 29, accent, 2);
            DrawLine(texture, 28, 16, 20, 25, accent, 1);
            FillCircle(texture, 20, 31, 4, highlight);
        }

        private static void DrawSporeSalvo(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 10, 10, 4, highlight);
            DrawLine(texture, 13, 12, 23, 18, accent, 1);
            DrawLine(texture, 13, 10, 26, 10, accent, 1);
            DrawLine(texture, 13, 8, 23, 2, accent, 1);
            FillCircle(texture, 28, 20, 3, accent);
            FillCircle(texture, 28, 10, 3, accent);
            FillCircle(texture, 28, 30, 3, accent);
            PaintBrush(texture, 31, 20, 2, highlight);
            PaintBrush(texture, 31, 10, 2, highlight);
            PaintBrush(texture, 31, 30, 2, highlight);
        }

        private static void DrawVesicleBurst(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 7, 2, accent);
            FillCircle(texture, 20, 20, 3, highlight);
            FillCircle(texture, 20, 8, 3, accent);
            FillCircle(texture, 20, 32, 3, accent);
            FillCircle(texture, 8, 20, 3, accent);
            FillCircle(texture, 32, 20, 3, accent);
            DrawLine(texture, 20, 13, 20, 17, highlight, 1);
            DrawLine(texture, 20, 23, 20, 27, highlight, 1);
            DrawLine(texture, 13, 20, 17, 20, highlight, 1);
            DrawLine(texture, 23, 20, 27, 20, highlight, 1);
        }

        private static void DrawFallback(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 8, 8, 31, 31, accent, 2);
            DrawLine(texture, 31, 8, 8, 31, highlight, 2);
        }

        private static void FillShield(Texture2D texture, int centerX, int centerY, int halfWidth, int halfHeight, Color color)
        {
            ProceduralIconUtility.FillShield(texture, centerX, centerY, halfWidth, halfHeight, color);
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            ProceduralIconUtility.FillCircle(texture, centerX, centerY, radius, color);
        }

        private static void DrawRing(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            ProceduralIconUtility.DrawRing(texture, centerX, centerY, radius, thickness, color);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            ProceduralIconUtility.DrawLine(texture, x0, y0, x1, y1, color, thickness);
        }

        private static void PaintBrush(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            ProceduralIconUtility.PaintBrush(texture, centerX, centerY, radius, color);
        }

        private static bool IsInsideBounds(int x, int y)
        {
            return x >= 0 && x < IconSize && y >= 0 && y < IconSize;
        }
    }
}

namespace FungusToast.Unity.UI
{
    internal static class ProceduralIconUtility
    {
        public const int DefaultIconSize = 40;

        public static Sprite CreateSprite(
            string textureName,
            Color background,
            Color accent,
            Action<Texture2D, Color, Color> drawAction,
            int size = DefaultIconSize)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = textureName
            };

            Fill(texture, background);
            DrawBorder(texture, accent);

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

        public static void DrawBorder(Texture2D texture, Color color)
        {
            int maxX = texture.width - 1;
            int maxY = texture.height - 1;
            for (int i = 0; i < texture.width; i++)
            {
                texture.SetPixel(i, 0, color);
                texture.SetPixel(i, maxY, color);
            }

            for (int i = 0; i < texture.height; i++)
            {
                texture.SetPixel(0, i, color);
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