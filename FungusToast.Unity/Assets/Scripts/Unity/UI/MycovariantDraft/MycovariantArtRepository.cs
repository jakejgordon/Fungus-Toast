using System.Collections.Generic;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI.Icons;
using UnityEngine;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    /// <summary>
    /// Sprite cache for the Mycovariant icons. The drawings live in <see cref="MycovariantIcons"/>.
    /// </summary>
    public static class MycovariantArtRepository
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

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
#if UNITY_EDITOR
            if (mycovariant.Id >= 0 && !MycovariantIcons.HasDedicatedIcon(mycovariant.Id))
            {
                Debug.LogWarning($"MycovariantArtRepository: no dedicated icon for '{mycovariant.Name}' ({mycovariant.Id}); drawing the fallback. Add a case to MycovariantIcons.");
            }
#endif
            var canvas = new IconCanvas();
            MycovariantIcons.Draw(canvas, mycovariant);
            return canvas.ToSprite($"MycovariantIcon_{mycovariant.IconId}");
        }
    }
}
