using System;

namespace FungusToast.Unity.Grid
{
    public static class BoardThemeFlavorResolver
    {
        public static (string themeLabel, string themeNoun) Resolve(BoardMediumConfig medium, int width, int height)
        {
            string mediumId = medium?.mediumId?.Trim() ?? string.Empty;
            string spriteName = medium?.GetResolvedBoardBackgroundSettings(width, height).BackgroundSprite?.name?.Trim() ?? string.Empty;
            string source = string.IsNullOrWhiteSpace(spriteName) ? mediumId : spriteName;

            if (source.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Seed Cracker", "Crumb");
            }

            if (source.IndexOf("cracker", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Cracker", "Crumb");
            }

            if (source.IndexOf("cheese", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Cheese Board", "Rind");
            }

            if (source.IndexOf("pita", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Pita Bread", "Pocket");
            }

            if (source.IndexOf("white_bread", StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("bread", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("White Bread", "Crust");
            }

            if (string.Equals(mediumId, "toast", StringComparison.OrdinalIgnoreCase))
            {
                return ("Toast", "Crust");
            }

            return ("Board", "Spore");
        }
    }
}
