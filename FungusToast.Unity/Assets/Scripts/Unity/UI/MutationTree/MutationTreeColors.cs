using UnityEngine;
using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Central color palette for the mutation tree panel.
    /// Provides per-category card fills and rails, header colors,
    /// and common state colors (maxed, affordable, locked).
    /// </summary>
    public static class MutationTreeColors
    {
        // ── Blend amounts ───────────────────────────────────────────────
        // The card reads on three channels: hue = category (always present),
        // fill brightness + border strength = purchasable right now, and
        // border color + badge = special state (maxed / next round / surge).
        // Every fill below keeps Text.Primary at >= 5:1 against the six accents.
        public const float AvailableFillBlend = 0.26f;
        public const float OwnedFillBlend     = 0.16f;
        public const float HoverFillBlend     = 0.12f;
        public const float MaxedFillBlend     = 0.25f;
        public const float LockedRailBlend    = 0.50f;

        // ── Universal state colors ──────────────────────────────────────
        public static readonly Color MaxedGold      = UIStyleTokens.Accent.Spore;
        public static readonly Color AffordableGlow  = UIStyleTokens.WithAlpha(UIStyleTokens.Text.Primary, 0.08f);
        public static readonly Color LockedTint      = UIStyleTokens.Text.Disabled;
        public static readonly Color WarningOutline  = UIStyleTokens.WithAlpha(UIStyleTokens.State.Warning, 0.95f);
        public static readonly Color DefaultNodeBG   = new Color32(0x3A, 0x3E, 0x33, 0xFF); // category-neutral card base
        public static readonly Color LockedNodeBG    = Color.Lerp(UIStyleTokens.Surface.Canvas, UIStyleTokens.Surface.PanelPrimary, 0.58f);
        public static readonly Color MaxedNodeBG     = Color.Lerp(DefaultNodeBG, MaxedGold, MaxedFillBlend);
        public static readonly Color DependentHover  = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, 0.6f);
        public static readonly Color DependentBorder = UIStyleTokens.WithAlpha(Color.Lerp(UIStyleTokens.State.Focus, UIStyleTokens.Text.Primary, 0.35f), 0.95f);
        public static readonly Color PrerequisiteBorder = UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Spore, 0.95f);
        public static readonly Color PurchasablePrerequisitePulse = UIStyleTokens.WithAlpha(
            Color.Lerp(UIStyleTokens.Accent.Lichen, UIStyleTokens.Accent.Hyphae, 0.42f),
            0.95f);

        // ── Panel-wide dark theme ───────────────────────────────────────
        public static readonly Color PanelBG         = UIStyleTokens.Surface.Canvas;
        public static readonly Color TopBarBG        = UIStyleTokens.Surface.PanelPrimary;
        public static readonly Color ScrollAreaBG    = UIStyleTokens.Surface.PanelSecondary;
        public static readonly Color DockBG          = UIStyleTokens.Surface.PanelElevated;
        public static readonly Color ButtonNormal    = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Surface.Canvas, 0.22f);
        public static readonly Color ButtonHighlight = Color.Lerp(UIStyleTokens.Surface.PanelElevated, UIStyleTokens.Accent.Spore, 0.12f);
        public static readonly Color ButtonPressed   = Color.Lerp(UIStyleTokens.Surface.PanelElevated, UIStyleTokens.Surface.Canvas, 0.20f);
        public static readonly Color PrimaryText      = UIStyleTokens.Text.Primary;
        public static readonly Color SecondaryText    = UIStyleTokens.Text.Secondary;
        public static readonly Color UniformSubheaderText = UIStyleTokens.Text.Secondary;
        public static readonly Color PulseOutline     = UIStyleTokens.State.Focus;

        // ── Flash / feedback ────────────────────────────────────────────
        public static readonly Color UpgradeFlashWhite = new Color(1f, 1f, 1f, 0.45f);

        /// <summary>
        /// Returns the accent color for a given mutation category.
        /// </summary>
        public static Color GetCategoryAccent(MutationCategory category)
        {
            return MutationCategoryPresentationCatalog.Get(category).Accent;
        }

        /// <summary>
        /// Returns a category accent bright enough for contrast-critical text on dark panels.
        /// Raw category accents remain reserved for borders, fills, and other non-text decoration.
        /// </summary>
        public static Color GetReadableCategoryAccent(MutationCategory category)
        {
            return Color.Lerp(GetCategoryAccent(category), UIStyleTokens.Text.Primary, 0.30f);
        }

        /// <summary>
        /// Column header fill: the raw accent, paired with <see cref="HeaderText"/> so the
        /// header row doubles as the legend for the category rails on the cards below it.
        /// </summary>
        public static Color GetCategoryHeaderBG(MutationCategory category, float alpha = 1f)
        {
            return GetCategoryHeaderBG(GetCategoryAccent(category), alpha);
        }

        public static Color GetCategoryHeaderBG(Color accent, float alpha = 1f)
        {
            accent.a = alpha;
            return accent;
        }

        /// <summary>Text color for the solid category headers.</summary>
        public static readonly Color HeaderText = UIStyleTokens.Text.OnAccent;

        /// <summary>
        /// Card fill for a mutation the player can buy right now: the brightest
        /// category tint a card ever shows at rest.
        /// </summary>
        public static Color GetAffordableNodeBG(MutationCategory category)
        {
            return Color.Lerp(DefaultNodeBG, GetCategoryAccent(category), AvailableFillBlend);
        }

        /// <summary>
        /// Card fill for a mutation the player has invested in but cannot buy this turn.
        /// Still clearly in its column, visibly quieter than an affordable card.
        /// </summary>
        public static Color GetOwnedNodeBG(MutationCategory category)
        {
            return Color.Lerp(DefaultNodeBG, GetCategoryAccent(category), OwnedFillBlend);
        }

        /// <summary>
        /// Category rail color for a locked card: the hue survives, dimmed toward the panel.
        /// </summary>
        public static Color GetLockedRailColor(MutationCategory category)
        {
            return Color.Lerp(GetCategoryAccent(category), UIStyleTokens.Surface.PanelPrimary, LockedRailBlend);
        }

        /// <summary>
        /// Returns the level-progress fill color for a category (faint tint behind white text).
        /// </summary>
        public static Color GetProgressBarColor(MutationCategory category)
        {
            Color c = GetCategoryAccent(category);
            return UIStyleTokens.WithAlpha(c, 0.18f);
        }

    }
}
