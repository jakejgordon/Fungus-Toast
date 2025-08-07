using UnityEngine;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Provides predefined color schemes for game log entries with improved accessibility and readability.
    /// </summary>
    public static class GameLogColorSchemes
    {
        // Current improved scheme (already implemented)
        public static readonly GameLogColorScheme Current = new GameLogColorScheme
        {
            NormalText = Color.white,
            LuckyText = new Color(0.6f, 1f, 0.6f),    // Brighter green
            UnluckyText = new Color(1f, 0.6f, 0.6f),  // Softer red
            NormalBackground = new Color(0.1f, 0.1f, 0.1f, 0.2f),
            LuckyBackground = new Color(0.1f, 0.6f, 0.1f, 0.5f),
            UnluckyBackground = new Color(0.6f, 0.1f, 0.1f, 0.5f)
        };
        
        // High contrast scheme for better accessibility
        public static readonly GameLogColorScheme HighContrast = new GameLogColorScheme
        {
            NormalText = Color.white,
            LuckyText = new Color(0.3f, 1f, 0.3f),    // Very bright green
            UnluckyText = new Color(1f, 0.3f, 0.3f),  // Very bright red
            NormalBackground = new Color(0.05f, 0.05f, 0.05f, 0.3f),
            LuckyBackground = new Color(0.0f, 0.4f, 0.0f, 0.4f),
            UnluckyBackground = new Color(0.4f, 0.0f, 0.0f, 0.4f)
        };
        
        // Colorblind-friendly scheme using blue/yellow instead of green/red
        public static readonly GameLogColorScheme ColorblindFriendly = new GameLogColorScheme
        {
            NormalText = Color.white,
            LuckyText = new Color(0.4f, 0.8f, 1f),    // Light blue for positive
            UnluckyText = new Color(1f, 0.8f, 0.2f),  // Orange/yellow for negative
            NormalBackground = new Color(0.1f, 0.1f, 0.1f, 0.2f),
            LuckyBackground = new Color(0.1f, 0.3f, 0.5f, 0.3f),   // Blue tint
            UnluckyBackground = new Color(0.5f, 0.4f, 0.1f, 0.3f)  // Orange tint
        };
        
        // Subtle scheme with less saturated colors
        public static readonly GameLogColorScheme Subtle = new GameLogColorScheme
        {
            NormalText = Color.white,
            LuckyText = new Color(0.7f, 1f, 0.7f),    // Very light green
            UnluckyText = new Color(1f, 0.8f, 0.8f),  // Very light red
            NormalBackground = new Color(0.1f, 0.1f, 0.1f, 0.15f),
            LuckyBackground = new Color(0.15f, 0.25f, 0.15f, 0.2f),
            UnluckyBackground = new Color(0.25f, 0.15f, 0.15f, 0.2f)
        };
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