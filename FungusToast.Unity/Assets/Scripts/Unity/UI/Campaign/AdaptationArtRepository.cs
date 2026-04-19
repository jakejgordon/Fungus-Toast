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
            var background = Color.Lerp(ResolveBackground(adaptationId), UIStyleTokens.Text.Secondary, 0.24f);
            var accent = Color.Lerp(ResolveAccent(adaptationId), UIStyleTokens.Text.Primary, 0.28f);

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
                        case "hyphal_bridge":
                            DrawHyphalBridge(texture, drawAccent, highlight);
                            break;
                        case "rhizomorphic_hunger":
                            DrawRhizomorphicHunger(texture, drawAccent, highlight);
                            break;
                        case "vesicle_burst":
                            DrawVesicleBurst(texture, drawAccent, highlight);
                            break;
                        case "mycelial_crescendo":
                            DrawMycelialCrescendo(texture, drawAccent, highlight);
                            break;
                        case "ossified_advance":
                            DrawOssifiedAdvance(texture, drawAccent, highlight);
                            break;
                        case "conidia_ascent":
                            DrawConidiaAscent(texture, drawAccent, highlight);
                            break;
                        case "oblique_filament":
                            DrawObliqueFilament(texture, drawAccent, highlight);
                            break;
                        case "thanatrophic_rebound":
                            DrawThanatrophicRebound(texture, drawAccent, highlight);
                            break;
                        case "toxin_primacy":
                            DrawToxinPrimacy(texture, drawAccent, highlight);
                            break;
                        case "centripetal_germination":
                            DrawCentripetalGermination(texture, drawAccent, highlight);
                            break;
                        case "signal_economy":
                            DrawSignalEconomy(texture, drawAccent, highlight);
                            break;
                        case "liminal_sporemeal":
                            DrawLiminalSporemeal(texture, drawAccent, highlight);
                            break;
                        case "putrefactive_resilience":
                            DrawPutrefactiveResilience(texture, drawAccent, highlight);
                            break;
                        case "compound_reserve":
                            DrawCompoundReserve(texture, drawAccent, highlight);
                            break;
                        case "hyphal_priming":
                            DrawHyphalPriming(texture, drawAccent, highlight);
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
                "hyphal_bridge" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "rhizomorphic_hunger" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.46f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Accent.Putrefaction, 0.3f),
                "mycelial_crescendo" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.40f),
                "ossified_advance" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.45f),
                "conidia_ascent" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.35f),
                "oblique_filament" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.55f),
                "thanatrophic_rebound" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "toxin_primacy" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.38f),
                "centripetal_germination" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.4f),
                "signal_economy" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.44f),
                "liminal_sporemeal" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelPrimary, 0.46f),
                "putrefactive_resilience" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "compound_reserve" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.34f),
                "hyphal_priming" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.28f),
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
                "hyphal_bridge" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Info, 0.25f),
                "rhizomorphic_hunger" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Moss, 0.32f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Putrefaction, 0.35f),
                "mycelial_crescendo" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Warning, 0.25f),
                "ossified_advance" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.18f),
                "conidia_ascent" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Info, 0.45f),
                "oblique_filament" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Text.Primary, 0.15f),
                "thanatrophic_rebound" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Accent.Putrefaction, 0.32f),
                "toxin_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.14f),
                "centripetal_germination" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.State.Warning, 0.28f),
                "signal_economy" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Info, 0.22f),
                "liminal_sporemeal" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Success, 0.18f),
                "putrefactive_resilience" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.State.Info, 0.24f),
                "compound_reserve" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Warning, 0.18f),
                "hyphal_priming" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.12f),
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

        private static void DrawHyphalPriming(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 10, 29, 20, 11, accent, 2);
            DrawLine(texture, 20, 11, 30, 29, accent, 2);
            DrawLine(texture, 20, 11, 20, 31, highlight, 2);
            DrawLine(texture, 13, 23, 27, 23, highlight, 1);
            FillCircle(texture, 20, 9, 3, highlight);
            FillCircle(texture, 12, 29, 2, accent);
            FillCircle(texture, 28, 29, 2, accent);
            FillCircle(texture, 20, 23, 2, highlight);
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

        private static void DrawHyphalBridge(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 8, 11, 4, highlight);
            FillCircle(texture, 32, 29, 4, highlight);
            DrawLine(texture, 11, 13, 29, 27, accent, 2);
            FillCircle(texture, 13, 15, 2, accent);
            FillCircle(texture, 18, 19, 2, highlight);
            FillCircle(texture, 22, 23, 2, accent);
            FillCircle(texture, 27, 27, 2, highlight);
        }

        private static void DrawRhizomorphicHunger(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 8, 1, accent);
            FillCircle(texture, 20, 20, 4, highlight);

            DrawLine(texture, 20, 6, 20, 14, accent, 2);
            DrawLine(texture, 20, 34, 20, 26, accent, 2);
            DrawLine(texture, 6, 20, 14, 20, accent, 2);
            DrawLine(texture, 34, 20, 26, 20, accent, 2);

            DrawLine(texture, 14, 20, 10, 14, highlight, 1);
            DrawLine(texture, 14, 20, 10, 26, highlight, 1);
            DrawLine(texture, 26, 20, 30, 14, highlight, 1);
            DrawLine(texture, 26, 20, 30, 26, highlight, 1);
            DrawLine(texture, 20, 14, 14, 10, highlight, 1);
            DrawLine(texture, 20, 14, 26, 10, highlight, 1);
            DrawLine(texture, 20, 26, 14, 30, highlight, 1);
            DrawLine(texture, 20, 26, 26, 30, highlight, 1);

            FillCircle(texture, 20, 6, 2, highlight);
            FillCircle(texture, 20, 34, 2, highlight);
            FillCircle(texture, 6, 20, 2, highlight);
            FillCircle(texture, 34, 20, 2, highlight);
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

        private static void DrawMycelialCrescendo(Texture2D texture, Color accent, Color highlight)
        {
            // Outer burst ring representing the surge wave
            DrawRing(texture, 20, 20, 13, 2, accent);
            // Inner ring — tighter energy band
            DrawRing(texture, 20, 20, 7, 1, highlight);
            // Glowing core
            FillCircle(texture, 20, 20, 3, highlight);
            // Two surge nodes — one for each trigger round (6 and 16)
            FillCircle(texture, 20, 7, 3, accent);
            FillCircle(texture, 20, 33, 3, accent);
            // Energy lines connecting core to nodes
            DrawLine(texture, 20, 17, 20, 10, highlight, 1);
            DrawLine(texture, 20, 23, 20, 30, highlight, 1);
            // Forked lateral bursts suggesting radiating surge energy
            DrawLine(texture, 27, 14, 32, 10, accent, 1);
            DrawLine(texture, 27, 14, 32, 18, accent, 1);
            DrawLine(texture, 13, 14, 8, 10, accent, 1);
            DrawLine(texture, 13, 14, 8, 18, accent, 1);
        }

        private static void DrawOssifiedAdvance(Texture2D texture, Color accent, Color highlight)
        {
            // Armored cell body — thick outer ring representing resistance
            DrawRing(texture, 20, 20, 12, 3, accent);
            // Inner resistant core
            FillCircle(texture, 20, 20, 5, highlight);
            // Four orthogonal growth spurs radiating outward from the fortified cell
            DrawLine(texture, 20, 25, 20, 33, accent, 2); // North
            DrawLine(texture, 20, 15, 20, 7,  accent, 2); // South
            DrawLine(texture, 25, 20, 33, 20, accent, 2); // East
            DrawLine(texture, 15, 20, 7,  20, accent, 2); // West
            // Arrow tips on each spur
            FillCircle(texture, 20, 33, 2, highlight);
            FillCircle(texture, 20, 7,  2, highlight);
            FillCircle(texture, 33, 20, 2, highlight);
            FillCircle(texture, 7,  20, 2, highlight);
        }

        private static void DrawConidiaAscent(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 11, 28, 2, highlight);
            FillCircle(texture, 17, 28, 2, highlight);
            FillCircle(texture, 11, 22, 2, highlight);
            FillCircle(texture, 17, 22, 2, highlight);
            FillCircle(texture, 5, 34, 2, accent);
            FillCircle(texture, 11, 34, 2, accent);
            FillCircle(texture, 17, 34, 2, accent);
            FillCircle(texture, 5, 28, 2, accent);
            FillCircle(texture, 5, 22, 2, accent);
            DrawLine(texture, 19, 20, 27, 12, accent, 2);
            DrawLine(texture, 27, 12, 33, 8, highlight, 2);
            FillCircle(texture, 29, 24, 2, accent);
            FillCircle(texture, 35, 18, 2, highlight);
            FillCircle(texture, 29, 18, 2, highlight);
            FillCircle(texture, 35, 12, 2, highlight);
            DrawLine(texture, 20, 18, 24, 18, accent, 1);
            DrawLine(texture, 23, 15, 27, 15, highlight, 1);
        }

        private static void DrawObliqueFilament(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 8, 30, 30, 8, accent, 2);
            DrawLine(texture, 10, 34, 34, 10, highlight, 1);
            DrawLine(texture, 6, 22, 18, 10, highlight, 1);
            DrawLine(texture, 22, 30, 34, 18, highlight, 1);
            FillCircle(texture, 12, 26, 3, highlight);
            FillCircle(texture, 28, 14, 3, accent);
        }

        private static void DrawThanatrophicRebound(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 20, 26, 6, accent);
            DrawRing(texture, 20, 26, 8, 1, highlight);
            DrawLine(texture, 20, 18, 28, 10, highlight, 2);
            FillShield(texture, 29, 11, 5, 6, highlight);
            DrawLine(texture, 14, 13, 20, 18, accent, 1);
        }

        private static void DrawToxinPrimacy(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 12, 12, 4, accent);
            FillCircle(texture, 28, 12, 4, accent);
            FillCircle(texture, 20, 28, 5, highlight);
            DrawLine(texture, 15, 15, 20, 23, highlight, 1);
            DrawLine(texture, 25, 15, 20, 23, highlight, 1);
            DrawRing(texture, 20, 20, 12, 1, accent);
        }

        private static void DrawCentripetalGermination(Texture2D texture, Color accent, Color highlight)
        {
            FillCircle(texture, 9, 31, 4, highlight);
            DrawLine(texture, 12, 28, 20, 20, accent, 2);
            DrawLine(texture, 20, 20, 29, 20, highlight, 1);
            DrawLine(texture, 20, 20, 20, 11, highlight, 1);
            FillCircle(texture, 20, 20, 3, accent);
            DrawRing(texture, 20, 20, 9, 1, highlight);
        }

        private static void DrawSignalEconomy(Texture2D texture, Color accent, Color highlight)
        {
            DrawRing(texture, 20, 20, 10, 2, accent);
            FillCircle(texture, 20, 20, 3, highlight);
            DrawLine(texture, 20, 10, 20, 15, highlight, 1);
            DrawLine(texture, 30, 20, 25, 20, highlight, 1);
            DrawLine(texture, 20, 30, 20, 25, highlight, 1);
            DrawLine(texture, 10, 20, 15, 20, highlight, 1);
            DrawLine(texture, 26, 30, 34, 30, accent, 2);
        }

        private static void DrawLiminalSporemeal(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 6, 33, 34, 33, accent, 2);
            FillCircle(texture, 11, 27, 3, highlight);
            FillCircle(texture, 18, 24, 4, accent);
            FillCircle(texture, 25, 27, 3, highlight);
            FillCircle(texture, 31, 23, 2, accent);
            DrawLine(texture, 18, 24, 24, 17, highlight, 1);
            FillCircle(texture, 27, 14, 3, highlight);
        }

        private static void DrawPutrefactiveResilience(Texture2D texture, Color accent, Color highlight)
        {
            FillShield(texture, 16, 21, 8, 10, highlight);
            FillCircle(texture, 29, 13, 4, accent);
            DrawLine(texture, 26, 16, 21, 19, accent, 2);
            DrawLine(texture, 18, 16, 12, 10, highlight, 1);
            DrawLine(texture, 18, 22, 12, 28, highlight, 1);
        }

        private static void DrawCompoundReserve(Texture2D texture, Color accent, Color highlight)
        {
            DrawLine(texture, 10, 28, 30, 28, accent, 2);
            DrawLine(texture, 10, 22, 30, 22, accent, 2);
            DrawLine(texture, 10, 16, 30, 16, accent, 2);
            FillCircle(texture, 14, 28, 2, highlight);
            FillCircle(texture, 26, 22, 2, highlight);
            FillCircle(texture, 20, 10, 4, highlight);
            DrawLine(texture, 20, 6, 20, 14, accent, 1);
            DrawLine(texture, 16, 10, 24, 10, accent, 1);
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