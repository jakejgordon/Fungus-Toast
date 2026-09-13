using System.Collections.Generic;
using FungusToast.Core.Campaign;
using FungusToast.Unity.UI.Icons;
using UnityEngine;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Sprite cache for the Adaptation icons. The drawings live in <see cref="AdaptationIcons"/>.
    /// </summary>
    public static class AdaptationArtRepository
    {
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
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(adaptationId) && !AdaptationIcons.HasDedicatedIcon(adaptationId))
            {
                Debug.LogWarning($"AdaptationArtRepository: no dedicated icon for '{adaptationId}'; drawing the fallback. Add a drawer to AdaptationIcons.");
            }
#endif
            var canvas = new IconCanvas();
            AdaptationIcons.Draw(canvas, adaptationId);
            return canvas.ToSprite($"AdaptationIcon_{adaptationId}");
        }
    }
}
