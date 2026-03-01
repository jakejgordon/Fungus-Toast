using UnityEngine;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Central color palette for the mutation tree panel.
    /// Provides per-category accent colors, tier-based intensity scaling,
    /// and common state colors (maxed, affordable, locked).
    /// </summary>
    public static class MutationTreeColors
    {
        // ── Category accent colors (lighter pastels for dark-background readability) ──
        private static readonly Color GrowthAccent           = HexColor("#8EDC6A");  // soft lime
        private static readonly Color CellularResilienceAccent = HexColor("#6DC8E0");  // soft cyan
        private static readonly Color FungicideAccent        = HexColor("#C084E0");  // soft lavender
        private static readonly Color GeneticDriftAccent     = HexColor("#F0C850");  // warm gold
        private static readonly Color MycelialSurgesAccent   = HexColor("#F076A8");  // soft rose

        // ── Universal state colors ──────────────────────────────────────
        public static readonly Color MaxedGold       = HexColor("#FFD700");
        public static readonly Color AffordableGlow  = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color LockedTint      = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static readonly Color DefaultNodeBG   = new Color(0.165f, 0.165f, 0.18f, 1f); // #2A2A2E — darker, slightly warm

        // ── Panel-wide dark theme ───────────────────────────────────────
        public static readonly Color PanelBG         = HexColor("#1A1A1F");   // deepest background
        public static readonly Color TopBarBG        = HexColor("#222228");   // top bar / header strip
        public static readonly Color ScrollAreaBG    = HexColor("#1E1E24");   // scroll view viewport
        public static readonly Color DockBG          = HexColor("#252530");   // dock bar along the edge
        public static readonly Color ButtonNormal    = HexColor("#2E2E38");   // default button face
        public static readonly Color ButtonHighlight = HexColor("#3A3A48");   // button hover
        public static readonly Color ButtonPressed   = HexColor("#44445A");   // button pressed
        public static readonly Color PrimaryText     = HexColor("#E0E0E6");   // primary text color
        public static readonly Color SecondaryText   = HexColor("#B0B0B8");   // secondary / dimmed text
        public static readonly Color PulseOutline    = HexColor("#F0C850");   // warm gold pulse outline

        // ── Flash / feedback ────────────────────────────────────────────
        public static readonly Color UpgradeFlashWhite = new Color(1f, 1f, 1f, 0.45f);

        /// <summary>
        /// Returns the accent color for a given mutation category.
        /// </summary>
        public static Color GetCategoryAccent(MutationCategory category)
        {
            return category switch
            {
                MutationCategory.Growth             => GrowthAccent,
                MutationCategory.CellularResilience => CellularResilienceAccent,
                MutationCategory.Fungicide          => FungicideAccent,
                MutationCategory.GeneticDrift       => GeneticDriftAccent,
                MutationCategory.MycelialSurges     => MycelialSurgesAccent,
                _ => Color.white
            };
        }

        /// <summary>
        /// Returns the category accent at reduced alpha, suitable for header backgrounds.
        /// </summary>
        public static Color GetCategoryHeaderBG(MutationCategory category, float alpha = 0.95f)
        {
            // Subtle category tint blended into the dark TopBar base
            Color accent = GetCategoryAccent(category);
            Color blended = Color.Lerp(TopBarBG, accent, 0.18f);
            blended.a = alpha;
            return blended;
        }

        /// <summary>
        /// Returns a tier-scaled color: lower tiers are lighter/desaturated, higher
        /// tiers are more vivid. Uses the category accent as base hue.
        /// tierNumber: 1–10  (clamped).
        /// </summary>
        public static Color GetTierColor(MutationCategory category, int tierNumber)
        {
            Color accent = GetCategoryAccent(category);
            // Intensity ramps from 0.35 (Tier 1) to 1.0 (Tier 7+)
            float t = Mathf.Clamp01((tierNumber - 1) / 6f);
            float intensity = Mathf.Lerp(0.35f, 1f, t);

            // Desaturate at low tiers by lerping toward gray
            Color gray = new Color(0.5f, 0.5f, 0.5f, 1f);
            Color result = Color.Lerp(gray, accent, intensity);
            result.a = 1f;
            return result;
        }

        /// <summary>
        /// Returns a category-tinted "affordable" background color at low alpha.
        /// </summary>
        /// <summary>
        /// Returns the node background color with a subtle category tint blended in.
        /// Uses proper lerp instead of additive to avoid oversaturation.
        /// </summary>
        public static Color GetAffordableNodeBG(MutationCategory category, float blendAmount = 0.12f)
        {
            Color accent = GetCategoryAccent(category);
            Color bg = DefaultNodeBG;
            return Color.Lerp(bg, accent, blendAmount);
        }

        /// <summary>
        /// Returns the level-progress fill color for a category (faint tint behind white text).
        /// </summary>
        public static Color GetProgressBarColor(MutationCategory category)
        {
            Color c = GetCategoryAccent(category);
            return new Color(c.r, c.g, c.b, 0.18f);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            Debug.LogWarning($"MutationTreeColors: failed to parse '{hex}', returning white.");
            return Color.white;
        }
    }
}
