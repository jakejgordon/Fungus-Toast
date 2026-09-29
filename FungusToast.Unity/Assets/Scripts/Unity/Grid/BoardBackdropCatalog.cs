using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Grid
{
    /// <summary>
    /// Real-world sizes for mediums and the photographic surfaces (plate, cutting board) they sit on,
    /// so the backdrop renders at true physical scale. See NEW_BACKGROUND_HELPER.md, "Mediums And Backdrops".
    ///
    /// Visible rects are the normalized opaque bounds of each sprite (origin bottom-left), measured from the
    /// source PNGs, so scale and centering ignore transparent margins. Re-measure them when an image changes.
    /// </summary>
    public static class BoardBackdropCatalog
    {
        public const string PlateSurfaceId = "plate";
        public const string SmallPlateSurfaceId = "small_plate";
        public const string CuttingBoardSurfaceId = "cutting_board";
        public const string CuttingBoardMaterialSettingId = "cutting_board_material";
        public const string CountertopSettingId = "countertop";

        private const string ResourceFolder = "Backdrops/";

        // Salts the gameplay seed so the setting pick does not correlate with other seeded choices.
        private const int SettingPickSalt = 0x5E771;

        public readonly struct MediumScale
        {
            public MediumScale(float widthCm, Rect visibleRectNormalized, string surfaceId)
            {
                WidthCm = widthCm;
                VisibleRectNormalized = visibleRectNormalized;
                SurfaceId = surfaceId;
            }

            /// <summary>Real width of the medium's visible (opaque) area.</summary>
            public float WidthCm { get; }
            public Rect VisibleRectNormalized { get; }
            public string SurfaceId { get; }
        }

        public readonly struct Surface
        {
            public Surface(
                string surfaceResource,
                float widthCm,
                Rect visibleRectNormalized,
                IReadOnlyList<string> allowedSettingIds)
            {
                SurfaceResource = surfaceResource;
                WidthCm = widthCm;
                VisibleRectNormalized = visibleRectNormalized;
                AllowedSettingIds = allowedSettingIds;
            }

            public string SurfaceResource { get; }
            /// <summary>Real width of the surface's visible (opaque) area.</summary>
            public float WidthCm { get; }
            public Rect VisibleRectNormalized { get; }
            /// <summary>Settings that contrast with this surface; one is picked per game.</summary>
            public IReadOnlyList<string> AllowedSettingIds { get; }
        }

        /// <summary>What the surface rests on: a seamless tile repeated to fill the view.</summary>
        public readonly struct Setting
        {
            public Setting(string tileResource, float tileCm)
            {
                TileResource = tileResource;
                TileCm = tileCm;
            }

            public string TileResource { get; }
            /// <summary>Real width covered by one tile.</summary>
            public float TileCm { get; }
        }

        // Typical food sizes, not measurements; tune by eye.
        private static readonly Dictionary<string, MediumScale> MediumsBySpriteName = new()
        {
            ["white_bread_1024x1024"] = new MediumScale(11.0f, new Rect(0.0459f, 0.0352f, 0.9102f, 0.9258f), PlateSurfaceId),
            ["kaiser_bun_1086x1448"] = new MediumScale(10.5f, new Rect(0.0313f, 0.1561f, 0.9227f, 0.7134f), PlateSurfaceId),
            ["pita_900x900"] = new MediumScale(16.5f, new Rect(0.0222f, 0.0322f, 0.9567f, 0.9411f), PlateSurfaceId),
            ["hotdog_bun_900x900"] = new MediumScale(16.0f, new Rect(0.0082f, 0.2788f, 0.9904f, 0.4245f), PlateSurfaceId),
            ["hotdog_bun_900x600"] = new MediumScale(16.0f, new Rect(0.1067f, 0.2150f, 0.8022f, 0.5283f), PlateSurfaceId),
            ["cheese_800x800"] = new MediumScale(8.0f, new Rect(0.0850f, 0.0350f, 0.8525f, 0.9313f), CuttingBoardSurfaceId),
            ["yellow_cheese_600x600"] = new MediumScale(7.0f, new Rect(0.1183f, 0.0600f, 0.7567f, 0.8767f), CuttingBoardSurfaceId),
            ["cracker_final_600x600"] = new MediumScale(5.0f, new Rect(0.0100f, 0.0200f, 0.9750f, 0.9683f), SmallPlateSurfaceId),
            ["seed_cracker_550x550"] = new MediumScale(6.0f, new Rect(0.0036f, 0.0273f, 0.9945f, 0.9382f), SmallPlateSurfaceId),
        };

        // A surface may only sit on settings it contrasts with: the tan cutting board vanishes on the tan
        // board material, so it only gets the countertop. White plates stand out on either.
        private static readonly string[] AnySetting = { CuttingBoardMaterialSettingId, CountertopSettingId };
        private static readonly string[] CountertopOnly = { CountertopSettingId };

        // Surfaces are measured, except the small plate.
        private static readonly Dictionary<string, Surface> SurfacesById = new()
        {
            [PlateSurfaceId] = new Surface(
                ResourceFolder + "plate_surface_1476x1476",
                21.6f,
                new Rect(0.0115f, 0.0122f, 0.9763f, 0.9763f),
                AnySetting),
            // A 6.5 in bread plate drawn from the dinner plate photo, like a matching plate from the same set.
            // Small mediums on a large board are mostly plain board at default zoom; a small plate reads sooner.
            [SmallPlateSurfaceId] = new Surface(
                ResourceFolder + "plate_surface_1476x1476",
                16.5f,
                new Rect(0.0115f, 0.0122f, 0.9763f, 0.9763f),
                AnySetting),
            [CuttingBoardSurfaceId] = new Surface(
                ResourceFolder + "cutting_board_surface_2048x1504",
                43.8f,
                new Rect(0.0137f, 0.0166f, 0.9741f, 0.9688f),
                CountertopOnly),
        };

        // The cutting-board material's grain matches the measured board photo at roughly 48 cm per tile.
        private static readonly Dictionary<string, Setting> SettingsById = new()
        {
            [CuttingBoardMaterialSettingId] = new Setting(ResourceFolder + "cutting_board_material_1024x1024", 48f),
            [CountertopSettingId] = new Setting(ResourceFolder + "countertop_material_512x512", 15f),
        };

        public static bool TryGetMedium(Sprite mediumSprite, out MediumScale medium)
        {
            medium = default;
            return mediumSprite != null && MediumsBySpriteName.TryGetValue(StripSpriteSuffix(mediumSprite.name), out medium);
        }

        public static bool TryGetSurface(string surfaceId, out Surface surface)
        {
            surface = default;
            return !string.IsNullOrEmpty(surfaceId) && SurfacesById.TryGetValue(surfaceId, out surface);
        }

        /// <summary>
        /// Picks one of the surface's allowed settings from the gameplay seed, so a game keeps the same look when
        /// it is resumed or its checkpoint is reloaded, while different games vary.
        /// </summary>
        public static bool TryPickSetting(Surface surface, int gameplaySeed, out Setting setting)
        {
            setting = default;
            IReadOnlyList<string> allowed = surface.AllowedSettingIds;
            if (allowed == null || allowed.Count == 0)
            {
                return false;
            }

            int index = new System.Random(gameplaySeed ^ SettingPickSalt).Next(allowed.Count);
            return SettingsById.TryGetValue(allowed[index], out setting);
        }

        // Multiple-mode sprites are named "<texture>_0"; single-mode sprites use the texture name.
        private static string StripSpriteSuffix(string spriteName)
        {
            if (spriteName.EndsWith("_0") && !MediumsBySpriteName.ContainsKey(spriteName))
            {
                return spriteName.Substring(0, spriteName.Length - 2);
            }

            return spriteName;
        }
    }
}
