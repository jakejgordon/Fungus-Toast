using UnityEngine;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Provides predefined color schemes for game log entries with improved accessibility and readability.
    /// </summary>
    public static class GameLogColorSchemes
    {
        // Current default scheme aligned with global UI style tokens.
        public static readonly GameLogColorScheme Current = new GameLogColorScheme
        {
            NormalText = UIStyleTokens.Text.Secondary,
            LuckyText = UIStyleTokens.State.Success,
            UnluckyText = UIStyleTokens.State.Danger,
            NormalBackground = WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.35f),
            LuckyBackground = WithAlpha(UIStyleTokens.State.Success, 0.26f),
            UnluckyBackground = WithAlpha(UIStyleTokens.State.Danger, 0.26f)
        };
        
        // High contrast scheme for better accessibility
        public static readonly GameLogColorScheme HighContrast = new GameLogColorScheme
        {
            NormalText = UIStyleTokens.Text.Primary,
            LuckyText = new Color(0.3f, 1f, 0.3f),    // Very bright green
            UnluckyText = new Color(1f, 0.3f, 0.3f),  // Very bright red
            NormalBackground = WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.45f),
            LuckyBackground = new Color(0.0f, 0.4f, 0.0f, 0.4f),
            UnluckyBackground = new Color(0.4f, 0.0f, 0.0f, 0.4f)
        };
        
        // Colorblind-friendly scheme using blue/yellow instead of green/red
        public static readonly GameLogColorScheme ColorblindFriendly = new GameLogColorScheme
        {
            NormalText = UIStyleTokens.Text.Primary,
            LuckyText = new Color(0.4f, 0.8f, 1f),    // Light blue for positive
            UnluckyText = new Color(1f, 0.8f, 0.2f),  // Orange/yellow for negative
            NormalBackground = WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.35f),
            LuckyBackground = new Color(0.1f, 0.3f, 0.5f, 0.3f),   // Blue tint
            UnluckyBackground = new Color(0.5f, 0.4f, 0.1f, 0.3f)  // Orange tint
        };
        
        // Subtle scheme with less saturated colors
        public static readonly GameLogColorScheme Subtle = new GameLogColorScheme
        {
            NormalText = UIStyleTokens.Text.Muted,
            LuckyText = WithAlpha(UIStyleTokens.State.Success, 0.9f),
            UnluckyText = WithAlpha(UIStyleTokens.State.Danger, 0.9f),
            NormalBackground = WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.22f),
            LuckyBackground = WithAlpha(UIStyleTokens.State.Success, 0.2f),
            UnluckyBackground = WithAlpha(UIStyleTokens.State.Danger, 0.2f)
        };

        public static Color GetTextColor(GameLogCategory category, GameLogColorScheme? schemeOverride = null)
        {
            var scheme = schemeOverride ?? Current;
            return category switch
            {
                GameLogCategory.Normal => scheme.NormalText,
                GameLogCategory.Lucky => scheme.LuckyText,
                GameLogCategory.Unlucky => scheme.UnluckyText,
                _ => scheme.NormalText
            };
        }

        public static Color GetBackgroundColor(GameLogCategory category, GameLogColorScheme? schemeOverride = null)
        {
            var scheme = schemeOverride ?? Current;
            return category switch
            {
                GameLogCategory.Normal => scheme.NormalBackground,
                GameLogCategory.Lucky => scheme.LuckyBackground,
                GameLogCategory.Unlucky => scheme.UnluckyBackground,
                _ => scheme.NormalBackground
            };
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
    
    [System.Serializable]
    public struct GameLogColorScheme
    {
        public Color NormalText;
        public Color LuckyText;
        public Color UnluckyText;
        public Color NormalBackground;
        public Color LuckyBackground;
        public Color UnluckyBackground;
    }
}