using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid
{
    /// <summary>
    /// Visual-only board skin definition. Keeps gameplay rectangular while allowing themed surfaces like toast or crackers.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/Board Medium", fileName = "BoardMediumConfig")]
    public class BoardMediumConfig : ScriptableObject
    {
        [Header("Identity")]
        public string mediumId = "toast";

        [Header("Surface")]
        public bool overridePlayableSurface = false;
        public TileBase boardSurfaceTile;
        public TileBase[] boardSurfaceVariantTiles;
        [Range(0f, 1f)] public float surfaceVariantDensity = 0f;

        [Header("Bread Photo Background")]
        public bool renderBoardBackground = false;
        public Sprite backgroundSprite;
        public Color backgroundColor = Color.white;
        public bool hidePlayableSurfaceTiles = true;
        [Range(0f, 0.49f)] public float backgroundInsetLeftNormalized = 0.16f;
        [Range(0f, 0.49f)] public float backgroundInsetRightNormalized = 0.16f;
        [Range(0f, 0.49f)] public float backgroundInsetBottomNormalized = 0.14f;
        [Range(0f, 0.49f)] public float backgroundInsetTopNormalized = 0.2f;
        [Min(0.01f)] public float backgroundScaleMultiplier = 1f;
        public bool renderBoardEdgeFade = true;
        public Color boardEdgeFadeColor = new(0.35f, 0.2f, 0.08f, 0.2f);
        [Range(0.5f, 6f)] public float boardEdgeFadeWidthTiles = 2.5f;
        [Range(0f, 0.2f)] public float boardEdgeFadeNoiseStrength = 0.035f;

        [Header("Crust")]
        public bool renderCrust = true;
        public TileBase crustEdgeTile;
        public TileBase crustCornerTile;
        [Range(0.02f, 0.25f)] public float crustThicknessRatio = 0.1f;
        [Min(0)] public int minCrustThickness = 1;
        [Min(0)] public int maxCrustThickness = 6;
        [Min(0f)] public float minVisualCrustThickness = 1.75f;
        [Min(0f)] public float maxVisualCrustThickness = 8f;

        [Header("Crust Shape")]
        public bool useBreadSliceSilhouette = true;
        [Range(0f, 1f)] public float topCrustRoundness = 1f;
        [Range(0f, 1f)] public float bottomCrustRoundness = 0.35f;

        [Header("Crust Colors")]
        public Color crustInnerColor = new(0.95f, 0.79f, 0.53f, 1f);
        public Color crustMidColor = new(0.79f, 0.47f, 0.16f, 1f);
        public Color crustOuterColor = new(0.42f, 0.2f, 0.05f, 1f);
        [Range(0f, 1f)] public float crustTopDarkening = 0.18f;
        [Range(0f, 0.2f)] public float crustColorVariation = 0.06f;

        [Header("Bread Interior Colors")]
        public Color breadInteriorColor = new(0.93f, 0.82f, 0.62f, 1f);
        public Color breadShadeColor = new(0.88f, 0.74f, 0.53f, 1f);
        [Range(0f, 0.15f)] public float breadColorVariation = 0.025f;

        [Header("Tile Rendering")]
        [Range(1f, 1.08f)] public float playableSurfaceTileScale = 1.01f;
        [Range(1f, 1.12f)] public float crustTileScale = 1.03f;

        [Header("Inner Browning")]
        public bool tintPerimeterTiles = false;
        [Min(1)] public int perimeterTintDepth = 1;
        public Color perimeterTint = new(0.88f, 0.78f, 0.56f, 1f);

        public int GetCrustThickness(int boardWidth, int boardHeight)
        {
            return Mathf.CeilToInt(GetVisualCrustThickness(boardWidth, boardHeight));
        }

        public float GetVisualCrustThickness(int boardWidth, int boardHeight)
        {
            if (!ShouldRenderCrust)
            {
                return 0;
            }

            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return 0;
            }

            int shortSide = Mathf.Min(boardWidth, boardHeight);
            float unclampedThickness = shortSide * crustThicknessRatio;
            float minThickness = Mathf.Max(minCrustThickness, minVisualCrustThickness);
            float maxThickness = Mathf.Max(minThickness, Mathf.Max(maxCrustThickness, maxVisualCrustThickness));
            return Mathf.Clamp(unclampedThickness, minThickness, maxThickness);
        }

        public bool IsPerimeterTintEnabled => tintPerimeterTiles && perimeterTint.a > 0f;
        public bool ShouldOverridePlayableSurface => overridePlayableSurface && boardSurfaceTile != null;
        public bool ShouldRenderBoardBackground => renderBoardBackground && backgroundSprite != null;
        public bool ShouldHidePlayableSurfaceTiles => ShouldRenderBoardBackground && hidePlayableSurfaceTiles;
        public bool ShouldRenderCrust => renderCrust && !ShouldRenderBoardBackground;
        public bool ShouldRenderBoardEdgeFade => ShouldRenderBoardBackground && renderBoardEdgeFade && boardEdgeFadeColor.a > 0f && boardEdgeFadeWidthTiles > 0f;

        public Rect GetBackgroundSafeAreaNormalized()
        {
            float left = Mathf.Clamp01(backgroundInsetLeftNormalized);
            float right = Mathf.Clamp01(backgroundInsetRightNormalized);
            float bottom = Mathf.Clamp01(backgroundInsetBottomNormalized);
            float top = Mathf.Clamp01(backgroundInsetTopNormalized);

            float width = Mathf.Max(0.01f, 1f - left - right);
            float height = Mathf.Max(0.01f, 1f - bottom - top);
            return new Rect(left, bottom, width, height);
        }

        public TileBase GetSurfaceTile(int x, int y)
        {
            if (boardSurfaceVariantTiles == null || boardSurfaceVariantTiles.Length == 0 || surfaceVariantDensity <= 0f)
            {
                return boardSurfaceTile;
            }

            uint hash = HashCoordinates(x, y);
            float normalized = (hash & 1023u) / 1023f;
            if (normalized > surfaceVariantDensity)
            {
                return boardSurfaceTile;
            }

            int variantIndex = (int)((hash >> 10) % (uint)boardSurfaceVariantTiles.Length);
            return boardSurfaceVariantTiles[variantIndex] != null ? boardSurfaceVariantTiles[variantIndex] : boardSurfaceTile;
        }

        private uint HashCoordinates(int x, int y)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)x) * 16777619u;
                hash = (hash ^ (uint)y) * 16777619u;
                if (!string.IsNullOrEmpty(mediumId))
                {
                    for (int i = 0; i < mediumId.Length; i++)
                    {
                        hash = (hash ^ mediumId[i]) * 16777619u;
                    }
                }

                return hash;
            }
        }
    }
}