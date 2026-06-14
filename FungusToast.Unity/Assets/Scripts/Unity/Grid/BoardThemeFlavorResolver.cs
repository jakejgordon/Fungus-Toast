using System;
using UnityEngine;

namespace FungusToast.Unity.Grid
{
    public static class BoardThemeFlavorResolver
    {
        public static (string themeLabel, string themeNoun) Resolve(BoardMediumConfig medium, int width, int height)
        {
            string mediumId = medium?.mediumId?.Trim() ?? string.Empty;
            string spriteName = string.Empty;

            if (medium != null)
            {
                var resolvedSettings = medium.GetResolvedBoardBackgroundSettings(width, height);
                var backgroundSprite = resolvedSettings.BackgroundSprite;
                if (backgroundSprite != null)
                {
                    try
                    {
                        spriteName = backgroundSprite.name?.Trim() ?? string.Empty;
                    }
                    catch (MissingReferenceException)
                    {
                        spriteName = string.Empty;
                    }
                }
            }

            string source = string.IsNullOrWhiteSpace(spriteName) ? mediumId : spriteName;

            if (source.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Seed Cracker", "Crumb");
            }

            if (source.IndexOf("cracker", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("White Cracker", "Crumb");
            }

            if (source.IndexOf("cheese", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("White Cheddar", "Rind");
            }

            if (source.IndexOf("pita", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Pita Bread", "Pocket");
            }

            if (source.IndexOf("kaiser", StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("kaizer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Kaiser Bun", "Crust");
            }

            if (source.IndexOf("hotdog", StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("hot dog", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ("Hot Dog Bun", "Crust");
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
