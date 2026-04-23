using UnityEngine;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI;
using System.Collections.Generic;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public static class MycovariantArtRepository
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        private static readonly Vector2Int[] IdentityAnchors =
        {
            new Vector2Int(10, 10),
            new Vector2Int(20, 8),
            new Vector2Int(30, 10),
            new Vector2Int(32, 20),
            new Vector2Int(30, 30),
            new Vector2Int(20, 32),
            new Vector2Int(10, 30),
            new Vector2Int(8, 20)
        };

        public static Sprite GetIcon(Mycovariant mycovariant)
        {
            if (mycovariant == null)
            {
                return GetFallbackIcon();
            }

            string cacheKey = mycovariant.IconId;
            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var sprite = BuildIcon(mycovariant);
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

            var fallback = new Mycovariant
            {
                Id = -1,
                Name = "Unknown Mycovariant",
                IconId = "myco_fallback",
                Category = MycovariantCategory.Growth,
                Type = MycovariantType.Passive
            };

            var sprite = BuildIcon(fallback);
            Cache[fallbackKey] = sprite;
            return sprite;
        }

        private static Sprite BuildIcon(Mycovariant mycovariant)
        {
            var background = ResolveBackground(mycovariant);
            var accent = ResolveAccent(mycovariant);
            return ProceduralIconUtility.CreateSprite(
                $"MycovariantIcon_{mycovariant.IconId}",
                background,
                accent,
                (texture, drawAccent, highlight) =>
                {
                    DrawCategoryMotif(texture, mycovariant, drawAccent, highlight);
                    DrawIdentityMarks(texture, mycovariant.IconId, drawAccent, highlight);
                    DrawTierPips(texture, ResolveTier(mycovariant.Name), drawAccent, highlight);
                });
        }

        private static Color ResolveBackground(Mycovariant mycovariant)
        {
            return mycovariant.Category switch
            {
                MycovariantCategory.Economy => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Growth => Color.Lerp(UIStyleTokens.Category.Growth, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Resistance => Color.Lerp(UIStyleTokens.Category.CellularResilience, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Fungicide => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Reclamation => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Defense => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                _ => UIStyleTokens.Surface.PanelPrimary
            };
        }

        private static Color ResolveAccent(Mycovariant mycovariant)
        {
            return mycovariant.Type switch
            {
                MycovariantType.Directional => UIStyleTokens.State.Info,
                MycovariantType.Economy => UIStyleTokens.State.Success,
                MycovariantType.Triggered => UIStyleTokens.State.Warning,
                MycovariantType.AreaEffect => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Info, 0.25f),
                MycovariantType.Active => UIStyleTokens.State.Focus,
                MycovariantType.Passive => UIStyleTokens.Text.Primary,
                _ => UIStyleTokens.Text.Primary
            };
        }

        private static void DrawCategoryMotif(Texture2D texture, Mycovariant mycovariant, Color accent, Color highlight)
        {
            if (mycovariant?.IconId == "myco_hyphal_draw")
            {
                DrawHyphalDrawMotif(texture, accent, highlight);
                return;
            }

            if (mycovariant?.IconId == "myco_septal_alarm")
            {
                DrawSeptalAlarmMotif(texture, accent, highlight);
                return;
            }

            if (mycovariant?.IconId == "myco_ascus_wager")
            {
                DrawAscusWagerMotif(texture, accent, highlight);
                return;
            }

            if (mycovariant?.IconId == "myco_ascus_bait")
            {
                DrawAscusBaitMotif(texture, accent, highlight);
                return;
            }

            switch (mycovariant.Type)
            {
                case MycovariantType.Directional:
                    DrawDirectionalMotif(texture, mycovariant, accent, highlight);
                    break;
                case MycovariantType.Economy:
                    DrawEconomyMotif(texture, accent, highlight);
                    break;
                case MycovariantType.Triggered:
                    DrawTriggeredMotif(texture, accent, highlight);
                    break;
                case MycovariantType.AreaEffect:
                    DrawAreaEffectMotif(texture, accent, highlight);
                    break;
                case MycovariantType.Active:
                    DrawActiveMotif(texture, accent, highlight);
                    break;
                case MycovariantType.Passive:
                default:
                    DrawPassiveMotif(texture, accent, highlight);
                    break;
            }
        }

        private static void DrawHyphalDrawMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawLine(texture, 8, 30, 31, 11, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 10, 24, 26, 24, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 11, 27, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 17, 22, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 23, 17, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 29, 13, 3, accent);
            ProceduralIconUtility.DrawRing(texture, 29, 13, 5, 1, highlight);
        }

        private static void DrawSeptalAlarmMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 6, 2, accent);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 20, 14, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 26, 20, 30, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 10, 20, 14, 20, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 26, 20, 30, 20, highlight, 2);
            ProceduralIconUtility.FillCircle(texture, 20, 10, 3, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 30, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 10, 20, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 30, 20, 2, accent);
            ProceduralIconUtility.DrawLine(texture, 14, 14, 26, 26, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 14, 26, 18, 22, accent, 1);
        }

        private static void DrawAscusWagerMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillCircle(texture, 20, 22, 7, accent);
            ProceduralIconUtility.DrawRing(texture, 20, 22, 10, 1, highlight);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 20, 17, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 15, 14, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 25, 14, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 14, 27, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 31, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 26, 27, 2, highlight);
            ProceduralIconUtility.DrawLine(texture, 12, 16, 28, 28, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 28, 16, 12, 28, accent, 1);
        }

        private static void DrawAscusBaitMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillCircle(texture, 20, 18, 6, accent);
            ProceduralIconUtility.DrawRing(texture, 20, 18, 9, 1, highlight);
            ProceduralIconUtility.DrawLine(texture, 20, 8, 20, 13, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 24, 20, 31, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 31, 26, 31, highlight, 2);
            ProceduralIconUtility.DrawLine(texture, 26, 31, 24, 27, highlight, 2);
            ProceduralIconUtility.FillCircle(texture, 14, 22, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 26, 22, 2, highlight);
            ProceduralIconUtility.DrawLine(texture, 14, 22, 26, 22, accent, 1);
        }

        private static void DrawPassiveMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawLine(texture, 10, 10, 30, 30, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 30, 10, 10, 30, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 8, 20, 32, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 8, 20, 32, 20, highlight, 1);
        }

        private static void DrawEconomyMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillCircle(texture, 12, 24, 4, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 14, 4, accent);
            ProceduralIconUtility.FillCircle(texture, 28, 24, 4, highlight);
            ProceduralIconUtility.DrawLine(texture, 12, 24, 20, 14, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 14, 28, 24, accent, 1);
        }

        private static void DrawTriggeredMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 10, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 20, 20, 3, highlight);
            ProceduralIconUtility.DrawLine(texture, 20, 8, 20, 14, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 30, 20, 24, 20, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 32, 20, 26, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 8, 20, 14, 20, highlight, 1);
        }

        private static void DrawAreaEffectMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 12, 2, accent);
            ProceduralIconUtility.DrawRing(texture, 20, 20, 7, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 12, 12, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 28, 12, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 12, 28, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 28, 28, 2, highlight);
        }

        private static void DrawActiveMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillShield(texture, 20, 21, 9, 11, accent);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 20, 28, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 13, 18, 27, 18, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 20, 12, 2, highlight);
        }

        private static void DrawDirectionalMotif(Texture2D texture, Mycovariant mycovariant, Color accent, Color highlight)
        {
            string name = mycovariant?.Name ?? string.Empty;
            if (name.IndexOf("North", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ProceduralIconUtility.DrawLine(texture, 20, 30, 20, 10, accent, 2);
                ProceduralIconUtility.DrawLine(texture, 20, 10, 14, 16, highlight, 2);
                ProceduralIconUtility.DrawLine(texture, 20, 10, 26, 16, highlight, 2);
            }
            else if (name.IndexOf("East", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ProceduralIconUtility.DrawLine(texture, 10, 20, 30, 20, accent, 2);
                ProceduralIconUtility.DrawLine(texture, 30, 20, 24, 14, highlight, 2);
                ProceduralIconUtility.DrawLine(texture, 30, 20, 24, 26, highlight, 2);
            }
            else if (name.IndexOf("South", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ProceduralIconUtility.DrawLine(texture, 20, 10, 20, 30, accent, 2);
                ProceduralIconUtility.DrawLine(texture, 20, 30, 14, 24, highlight, 2);
                ProceduralIconUtility.DrawLine(texture, 20, 30, 26, 24, highlight, 2);
            }
            else
            {
                ProceduralIconUtility.DrawLine(texture, 30, 20, 10, 20, accent, 2);
                ProceduralIconUtility.DrawLine(texture, 10, 20, 16, 14, highlight, 2);
                ProceduralIconUtility.DrawLine(texture, 10, 20, 16, 26, highlight, 2);
            }
        }

        private static void DrawIdentityMarks(Texture2D texture, string iconId, Color accent, Color highlight)
        {
            int hash = ProceduralIconUtility.ComputeStableHash(iconId);
            int previousIndex = -1;
            for (int i = 0; i < 3; i++)
            {
                int index = ((hash >> (i * 5)) + (i * 3)) % IdentityAnchors.Length;
                if (index == previousIndex)
                {
                    index = (index + 2) % IdentityAnchors.Length;
                }

                var anchor = IdentityAnchors[index];
                ProceduralIconUtility.FillCircle(texture, anchor.x, anchor.y, i == 0 ? 2 : 1, i % 2 == 0 ? highlight : accent);
                if (previousIndex >= 0)
                {
                    var previousAnchor = IdentityAnchors[previousIndex];
                    ProceduralIconUtility.DrawLine(texture, previousAnchor.x, previousAnchor.y, anchor.x, anchor.y, accent, 1);
                }

                previousIndex = index;
            }
        }

        private static void DrawTierPips(Texture2D texture, int tier, Color accent, Color highlight)
        {
            if (tier <= 0)
            {
                return;
            }

            int clampedTier = Mathf.Clamp(tier, 1, 3);
            int startX = clampedTier == 1 ? 20 : clampedTier == 2 ? 17 : 14;
            for (int i = 0; i < clampedTier; i++)
            {
                ProceduralIconUtility.FillCircle(texture, startX + (i * 6), 33, 2, i == 1 ? accent : highlight);
            }
        }

        private static int ResolveTier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            if (name.EndsWith("III"))
            {
                return 3;
            }

            if (name.EndsWith("II"))
            {
                return 2;
            }

            if (name.EndsWith("I"))
            {
                return 1;
            }

            return 0;
        }
    }
}
