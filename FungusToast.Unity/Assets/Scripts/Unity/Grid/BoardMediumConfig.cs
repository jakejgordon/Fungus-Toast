using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
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
        private const float VisibleAlphaBoundsThreshold = 1f / 255f;
        private static readonly Dictionary<Sprite, Rect> VisibleAlphaBoundsCache = new();

        [Serializable]
        public sealed class PlayableHorizontalSpanStop
        {
            [Range(0f, 1f)] public float normalizedY = 0f;
            [Range(0f, 1f)] public float minXNormalized = 0f;
            [Range(0f, 1f)] public float maxXNormalized = 1f;
        }

        [Serializable]
        public sealed class BakedBlockedTileMask
        {
            [Min(1)] public int boardWidth = 1;
            [Min(1)] public int boardHeight = 1;
            public string bakeVersion = string.Empty;
            public string spriteContentHash = string.Empty;
            public List<int> blockedTileIds = new();

            public bool Matches(int width, int height)
            {
                return width > 0
                    && height > 0
                    && boardWidth == width
                    && boardHeight == height;
            }
        }

        [Serializable]
        public sealed class BoardBackgroundSpriteMetadata
        {
            public Sprite backgroundSprite;
            public bool hasVisibleAlphaBounds = false;
            public Rect visibleAlphaBoundsNormalized = new(0f, 0f, 1f, 1f);
            public bool hasBoardBounds = false;
            public Rect boardBoundsNormalized = new(0f, 0f, 1f, 1f);
            public bool hasPlayableEllipse = false;
            public Vector2 playableEllipseCenterNormalized = new(0.5f, 0.5f);
            public Vector2 playableEllipseRadiiNormalized = new(0.5f, 0.5f);
            public bool hasPlayableHorizontalSpanProfile = false;
            [Range(0f, 1f)] public float playableHorizontalSpanProfileMinYNormalized = 0f;
            [Range(0f, 1f)] public float playableHorizontalSpanProfileMaxYNormalized = 1f;
            public List<PlayableHorizontalSpanStop> playableHorizontalSpanProfile = new();
            public List<BakedBlockedTileMask> bakedBlockedTileMasks = new();
        }

        [Serializable]
        public sealed class BoardBackgroundSizeOverride
        {
            [Min(1)] public int minBoardWidth = 1;
            [Min(1)] public int minBoardHeight = 1;
            [Min(1)] public int maxBoardWidth = 20;
            [Min(1)] public int maxBoardHeight = 20;

            [Header("Background")]
            public bool renderBoardBackground = true;
            public Sprite backgroundSprite;
            public Color backgroundColor = Color.white;
            public bool hidePlayableSurfaceTiles = true;
            public bool deriveBlockedTilesFromBackgroundAlpha = false;
            [Range(0f, 1f)] public float backgroundAlphaPlayableThreshold = 0.1f;
            [Range(0f, 1f)] public float backgroundMinTileCoverage = 0f;
            [Range(0f, 0.49f)] public float backgroundMaxTileClipFraction = 0.1f;
            [Range(1, 7)] public int backgroundTileClipSampleResolution = 5;
            public bool useExplicitBlockedTileIds = false;
            public List<int> explicitBlockedTileIds = new();
            [Range(0f, 0.49f)] public float backgroundInsetLeftNormalized = 0.16f;
            [Range(0f, 0.49f)] public float backgroundInsetRightNormalized = 0.16f;
            [Range(0f, 0.49f)] public float backgroundInsetBottomNormalized = 0.14f;
            [Range(0f, 0.49f)] public float backgroundInsetTopNormalized = 0.2f;
            public bool composeSafeAreaWithBoardBoundsMetadata = false;
            [Min(0.01f)] public float backgroundScaleMultiplier = 1.05f;
            public bool renderPlayableAreaOverlay = true;
            public Color playableAreaOverlayColor = new(1f, 0.97f, 0.88f, 0.055f);
            public bool renderBoardEdgeFade = true;
            public Color boardEdgeFadeColor = new(0.35f, 0.2f, 0.08f, 0.2f);
            [Range(0.5f, 6f)] public float boardEdgeFadeWidthTiles = 2.5f;
            [Range(0f, 0.2f)] public float boardEdgeFadeNoiseStrength = 0.035f;

            public bool Matches(int boardWidth, int boardHeight)
            {
                return boardWidth > 0
                    && boardHeight > 0
                    && boardWidth >= minBoardWidth
                    && boardHeight >= minBoardHeight
                    && boardWidth <= maxBoardWidth
                    && boardHeight <= maxBoardHeight;
            }

            public Rect GetBackgroundSafeAreaNormalized()
            {
                return BuildBackgroundSafeAreaNormalized(
                    backgroundInsetLeftNormalized,
                    backgroundInsetRightNormalized,
                    backgroundInsetBottomNormalized,
                    backgroundInsetTopNormalized);
            }

        }

        public readonly struct ResolvedBoardBackgroundSettings
        {
            public ResolvedBoardBackgroundSettings(
                bool renderBoardBackground,
                Sprite backgroundSprite,
                Color backgroundColor,
                bool hidePlayableSurfaceTiles,
                bool deriveBlockedTilesFromBackgroundAlpha,
                float backgroundAlphaPlayableThreshold,
                float backgroundMinTileCoverage,
                float backgroundMaxTileClipFraction,
                int backgroundTileClipSampleResolution,
                bool useExplicitBlockedTileIds,
                IReadOnlyList<int> explicitBlockedTileIds,
                Rect safeAreaNormalized,
                bool composeSafeAreaWithBoardBoundsMetadata,
                bool hasVisibleAlphaBoundsMetadata,
                Rect visibleAlphaBoundsNormalizedMetadata,
                bool hasBoardBoundsMetadata,
                Rect boardBoundsNormalizedMetadata,
                bool hasPlayableEllipseMetadata,
                Vector2 playableEllipseCenterNormalizedMetadata,
                Vector2 playableEllipseRadiiNormalizedMetadata,
                bool hasPlayableHorizontalSpanProfileMetadata,
                float playableHorizontalSpanProfileMinYNormalizedMetadata,
                float playableHorizontalSpanProfileMaxYNormalizedMetadata,
                IReadOnlyList<PlayableHorizontalSpanStop> playableHorizontalSpanProfileMetadata,
                IReadOnlyList<BakedBlockedTileMask> bakedBlockedTileMasksMetadata,
                float backgroundScaleMultiplier,
                bool renderPlayableAreaOverlay,
                Color playableAreaOverlayColor,
                bool renderBoardEdgeFade,
                Color boardEdgeFadeColor,
                float boardEdgeFadeWidthTiles,
                float boardEdgeFadeNoiseStrength)
            {
                RenderBoardBackground = renderBoardBackground;
                BackgroundSprite = backgroundSprite;
                BackgroundColor = backgroundColor;
                HidePlayableSurfaceTiles = hidePlayableSurfaceTiles;
                DeriveBlockedTilesFromBackgroundAlpha = deriveBlockedTilesFromBackgroundAlpha;
                BackgroundAlphaPlayableThreshold = backgroundAlphaPlayableThreshold;
                BackgroundMinTileCoverage = backgroundMinTileCoverage;
                BackgroundMaxTileClipFraction = backgroundMaxTileClipFraction;
                BackgroundTileClipSampleResolution = Mathf.Clamp(backgroundTileClipSampleResolution, 1, 7);
                UseExplicitBlockedTileIds = useExplicitBlockedTileIds;
                ExplicitBlockedTileIds = explicitBlockedTileIds ?? Array.Empty<int>();
                SafeAreaNormalized = SanitizeNormalizedRect(safeAreaNormalized);
                ComposeSafeAreaWithBoardBoundsMetadata = composeSafeAreaWithBoardBoundsMetadata;
                HasVisibleAlphaBoundsMetadata = hasVisibleAlphaBoundsMetadata;
                VisibleAlphaBoundsNormalizedMetadata = SanitizeNormalizedRect(visibleAlphaBoundsNormalizedMetadata);
                HasBoardBoundsMetadata = hasBoardBoundsMetadata;
                BoardBoundsNormalizedMetadata = SanitizeNormalizedRect(boardBoundsNormalizedMetadata);
                HasPlayableEllipseMetadata = hasPlayableEllipseMetadata;
                (PlayableEllipseCenterNormalizedMetadata, PlayableEllipseRadiiNormalizedMetadata) = SanitizeNormalizedEllipse(
                    playableEllipseCenterNormalizedMetadata,
                    playableEllipseRadiiNormalizedMetadata);
                HasPlayableHorizontalSpanProfileMetadata = hasPlayableHorizontalSpanProfileMetadata;
                PlayableHorizontalSpanProfileMinYNormalizedMetadata = Mathf.Clamp01(Mathf.Min(playableHorizontalSpanProfileMinYNormalizedMetadata, playableHorizontalSpanProfileMaxYNormalizedMetadata));
                PlayableHorizontalSpanProfileMaxYNormalizedMetadata = Mathf.Clamp01(Mathf.Max(playableHorizontalSpanProfileMinYNormalizedMetadata, playableHorizontalSpanProfileMaxYNormalizedMetadata));
                PlayableHorizontalSpanProfileMetadata = playableHorizontalSpanProfileMetadata ?? Array.Empty<PlayableHorizontalSpanStop>();
                BakedBlockedTileMasksMetadata = bakedBlockedTileMasksMetadata ?? Array.Empty<BakedBlockedTileMask>();
                BackgroundScaleMultiplier = backgroundScaleMultiplier;
                RenderPlayableAreaOverlay = renderPlayableAreaOverlay;
                PlayableAreaOverlayColor = playableAreaOverlayColor;
                RenderBoardEdgeFade = renderBoardEdgeFade;
                BoardEdgeFadeColor = boardEdgeFadeColor;
                BoardEdgeFadeWidthTiles = boardEdgeFadeWidthTiles;
                BoardEdgeFadeNoiseStrength = boardEdgeFadeNoiseStrength;
            }

            public bool RenderBoardBackground { get; }
            public Sprite BackgroundSprite { get; }
            public Color BackgroundColor { get; }
            public bool HidePlayableSurfaceTiles { get; }
            public bool DeriveBlockedTilesFromBackgroundAlpha { get; }
            public float BackgroundAlphaPlayableThreshold { get; }
            public float BackgroundMinTileCoverage { get; }
            public float BackgroundMaxTileClipFraction { get; }
            public int BackgroundTileClipSampleResolution { get; }
            public bool UseExplicitBlockedTileIds { get; }
            public IReadOnlyList<int> ExplicitBlockedTileIds { get; }
            public Rect SafeAreaNormalized { get; }
            public bool ComposeSafeAreaWithBoardBoundsMetadata { get; }
            public bool HasVisibleAlphaBoundsMetadata { get; }
            public Rect VisibleAlphaBoundsNormalizedMetadata { get; }
            public bool HasBoardBoundsMetadata { get; }
            public Rect BoardBoundsNormalizedMetadata { get; }
            public bool HasPlayableEllipseMetadata { get; }
            public Vector2 PlayableEllipseCenterNormalizedMetadata { get; }
            public Vector2 PlayableEllipseRadiiNormalizedMetadata { get; }
            public bool HasPlayableHorizontalSpanProfileMetadata { get; }
            public float PlayableHorizontalSpanProfileMinYNormalizedMetadata { get; }
            public float PlayableHorizontalSpanProfileMaxYNormalizedMetadata { get; }
            public IReadOnlyList<PlayableHorizontalSpanStop> PlayableHorizontalSpanProfileMetadata { get; }
            public IReadOnlyList<BakedBlockedTileMask> BakedBlockedTileMasksMetadata { get; }
            public float BackgroundScaleMultiplier { get; }
            public bool RenderPlayableAreaOverlay { get; }
            public Color PlayableAreaOverlayColor { get; }
            public bool RenderBoardEdgeFade { get; }
            public Color BoardEdgeFadeColor { get; }
            public float BoardEdgeFadeWidthTiles { get; }
            public float BoardEdgeFadeNoiseStrength { get; }

            public bool ShouldRenderBoardBackground => RenderBoardBackground && BackgroundSprite != null;
            public bool ShouldHidePlayableSurfaceTiles => ShouldRenderBoardBackground && HidePlayableSurfaceTiles;
            public bool ShouldUseBackgroundPlayableMask => ShouldRenderBoardBackground && BackgroundSprite != null && (DeriveBlockedTilesFromBackgroundAlpha || HasPlayableEllipseMetadata || HasPlayableHorizontalSpanProfileMetadata || BakedBlockedTileMasksMetadata.Count > 0);
            public bool ShouldRenderPlayableAreaOverlay => ShouldUseBackgroundPlayableMask && RenderPlayableAreaOverlay && PlayableAreaOverlayColor.a > 0f;
            public bool ShouldRenderBoardEdgeFade => ShouldRenderBoardBackground && RenderBoardEdgeFade && BoardEdgeFadeColor.a > 0f && BoardEdgeFadeWidthTiles > 0f;
        }

        private readonly struct SanitizedPlayableHorizontalSpanStop
        {
            public SanitizedPlayableHorizontalSpanStop(float normalizedY, float minXNormalized, float maxXNormalized)
            {
                NormalizedY = normalizedY;
                MinXNormalized = minXNormalized;
                MaxXNormalized = maxXNormalized;
            }

            public float NormalizedY { get; }
            public float MinXNormalized { get; }
            public float MaxXNormalized { get; }
        }

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
        public bool deriveBlockedTilesFromBackgroundAlpha = false;
        [Range(0f, 1f)] public float backgroundAlphaPlayableThreshold = 0.1f;
        [Range(0f, 1f)] public float backgroundMinTileCoverage = 0f;
        [Range(0f, 0.49f)] public float backgroundMaxTileClipFraction = 0.1f;
        [Range(1, 7)] public int backgroundTileClipSampleResolution = 5;
        public bool useExplicitBlockedTileIds = false;
        public List<int> explicitBlockedTileIds = new();
        [Range(0f, 0.49f)] public float backgroundInsetLeftNormalized = 0.16f;
        [Range(0f, 0.49f)] public float backgroundInsetRightNormalized = 0.16f;
        [Range(0f, 0.49f)] public float backgroundInsetBottomNormalized = 0.14f;
        [Range(0f, 0.49f)] public float backgroundInsetTopNormalized = 0.2f;
        public bool composeSafeAreaWithBoardBoundsMetadata = false;
        [Min(0.01f)] public float backgroundScaleMultiplier = 1.05f;
        public bool renderPlayableAreaOverlay = true;
        public Color playableAreaOverlayColor = new(1f, 0.97f, 0.88f, 0.055f);
        public bool renderBoardEdgeFade = true;
        public Color boardEdgeFadeColor = new(0.35f, 0.2f, 0.08f, 0.2f);
        [Range(0.5f, 6f)] public float boardEdgeFadeWidthTiles = 2.5f;
        [Range(0f, 0.2f)] public float boardEdgeFadeNoiseStrength = 0.035f;

        [Header("Board Background Overrides")]
        public List<BoardBackgroundSizeOverride> boardBackgroundOverrides = new();

        [Header("Board Background Sprite Metadata")]
        public List<BoardBackgroundSpriteMetadata> boardBackgroundSpriteMetadata = new();

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
        public bool ShouldRenderBoardBackground => GetResolvedBoardBackgroundSettings(int.MaxValue, int.MaxValue).ShouldRenderBoardBackground;
        public bool ShouldHidePlayableSurfaceTiles => GetResolvedBoardBackgroundSettings(int.MaxValue, int.MaxValue).ShouldHidePlayableSurfaceTiles;
        public bool ShouldRenderCrust => renderCrust && !ShouldRenderBoardBackground;
        public bool ShouldRenderBoardEdgeFade => GetResolvedBoardBackgroundSettings(int.MaxValue, int.MaxValue).ShouldRenderBoardEdgeFade;

        public ResolvedBoardBackgroundSettings GetResolvedBoardBackgroundSettings(int boardWidth, int boardHeight)
        {
            if (boardBackgroundOverrides != null)
            {
                for (int i = 0; i < boardBackgroundOverrides.Count; i++)
                {
                    BoardBackgroundSizeOverride backgroundOverride = boardBackgroundOverrides[i];
                    if (backgroundOverride != null && backgroundOverride.Matches(boardWidth, boardHeight))
                    {
                        return BuildResolvedBoardBackgroundSettings(
                            backgroundOverride.renderBoardBackground,
                            backgroundOverride.backgroundSprite,
                            backgroundOverride.backgroundColor,
                            backgroundOverride.hidePlayableSurfaceTiles,
                            backgroundOverride.deriveBlockedTilesFromBackgroundAlpha,
                            backgroundOverride.backgroundAlphaPlayableThreshold,
                            backgroundOverride.backgroundMinTileCoverage,
                            backgroundOverride.backgroundMaxTileClipFraction,
                            backgroundOverride.backgroundTileClipSampleResolution,
                            backgroundOverride.useExplicitBlockedTileIds,
                            backgroundOverride.explicitBlockedTileIds,
                            backgroundOverride.GetBackgroundSafeAreaNormalized(),
                            backgroundOverride.composeSafeAreaWithBoardBoundsMetadata,
                            backgroundOverride.backgroundScaleMultiplier,
                            backgroundOverride.renderPlayableAreaOverlay,
                            backgroundOverride.playableAreaOverlayColor,
                            backgroundOverride.renderBoardEdgeFade,
                            backgroundOverride.boardEdgeFadeColor,
                            backgroundOverride.boardEdgeFadeWidthTiles,
                            backgroundOverride.boardEdgeFadeNoiseStrength);
                    }
                }
            }

            return BuildResolvedBoardBackgroundSettings(
                renderBoardBackground,
                backgroundSprite,
                backgroundColor,
                hidePlayableSurfaceTiles,
                deriveBlockedTilesFromBackgroundAlpha,
                backgroundAlphaPlayableThreshold,
                backgroundMinTileCoverage,
                backgroundMaxTileClipFraction,
                backgroundTileClipSampleResolution,
                useExplicitBlockedTileIds,
                explicitBlockedTileIds,
                GetBackgroundSafeAreaNormalized(),
                composeSafeAreaWithBoardBoundsMetadata,
                backgroundScaleMultiplier,
                renderPlayableAreaOverlay,
                playableAreaOverlayColor,
                renderBoardEdgeFade,
                boardEdgeFadeColor,
                boardEdgeFadeWidthTiles,
                boardEdgeFadeNoiseStrength);
        }

        private ResolvedBoardBackgroundSettings BuildResolvedBoardBackgroundSettings(
            bool resolvedRenderBoardBackground,
            Sprite resolvedBackgroundSprite,
            Color resolvedBackgroundColor,
            bool resolvedHidePlayableSurfaceTiles,
            bool resolvedDeriveBlockedTilesFromBackgroundAlpha,
            float resolvedBackgroundAlphaPlayableThreshold,
            float resolvedBackgroundMinTileCoverage,
            float resolvedBackgroundMaxTileClipFraction,
            int resolvedBackgroundTileClipSampleResolution,
            bool resolvedUseExplicitBlockedTileIds,
            IReadOnlyList<int> resolvedExplicitBlockedTileIds,
            Rect resolvedSafeAreaNormalized,
            bool resolvedComposeSafeAreaWithBoardBoundsMetadata,
            float resolvedBackgroundScaleMultiplier,
            bool resolvedRenderPlayableAreaOverlay,
            Color resolvedPlayableAreaOverlayColor,
            bool resolvedRenderBoardEdgeFade,
            Color resolvedBoardEdgeFadeColor,
            float resolvedBoardEdgeFadeWidthTiles,
            float resolvedBoardEdgeFadeNoiseStrength)
        {
            bool hasVisibleAlphaBoundsMetadata = TryGetBackgroundSpriteMetadataVisibleAlphaBoundsNormalized(resolvedBackgroundSprite, out Rect visibleAlphaBoundsNormalizedMetadata);
            bool hasBoardBoundsMetadata = TryGetBackgroundSpriteMetadataBoardBoundsNormalized(resolvedBackgroundSprite, out Rect boardBoundsNormalizedMetadata);
            bool hasPlayableEllipseMetadata = TryGetBackgroundSpriteMetadataPlayableEllipseNormalized(resolvedBackgroundSprite, out Vector2 playableEllipseCenterNormalizedMetadata, out Vector2 playableEllipseRadiiNormalizedMetadata);
            bool hasPlayableHorizontalSpanProfileMetadata = TryGetBackgroundSpriteMetadataPlayableHorizontalSpanProfile(
                resolvedBackgroundSprite,
                out float playableHorizontalSpanProfileMinYNormalizedMetadata,
                out float playableHorizontalSpanProfileMaxYNormalizedMetadata,
                out IReadOnlyList<PlayableHorizontalSpanStop> playableHorizontalSpanProfileMetadata);
            TryGetBackgroundSpriteMetadataBakedBlockedTileMasks(
                resolvedBackgroundSprite,
                out IReadOnlyList<BakedBlockedTileMask> bakedBlockedTileMasksMetadata);
            return new ResolvedBoardBackgroundSettings(
                resolvedRenderBoardBackground,
                resolvedBackgroundSprite,
                resolvedBackgroundColor,
                resolvedHidePlayableSurfaceTiles,
                resolvedDeriveBlockedTilesFromBackgroundAlpha,
                resolvedBackgroundAlphaPlayableThreshold,
                resolvedBackgroundMinTileCoverage,
                resolvedBackgroundMaxTileClipFraction,
                resolvedBackgroundTileClipSampleResolution,
                resolvedUseExplicitBlockedTileIds,
                resolvedExplicitBlockedTileIds,
                resolvedSafeAreaNormalized,
                resolvedComposeSafeAreaWithBoardBoundsMetadata,
                hasVisibleAlphaBoundsMetadata,
                visibleAlphaBoundsNormalizedMetadata,
                hasBoardBoundsMetadata,
                boardBoundsNormalizedMetadata,
                hasPlayableEllipseMetadata,
                playableEllipseCenterNormalizedMetadata,
                playableEllipseRadiiNormalizedMetadata,
                hasPlayableHorizontalSpanProfileMetadata,
                playableHorizontalSpanProfileMinYNormalizedMetadata,
                playableHorizontalSpanProfileMaxYNormalizedMetadata,
                playableHorizontalSpanProfileMetadata,
                bakedBlockedTileMasksMetadata,
                resolvedBackgroundScaleMultiplier,
                resolvedRenderPlayableAreaOverlay,
                resolvedPlayableAreaOverlayColor,
                resolvedRenderBoardEdgeFade,
                resolvedBoardEdgeFadeColor,
                resolvedBoardEdgeFadeWidthTiles,
                resolvedBoardEdgeFadeNoiseStrength);
        }

        public bool ShouldRenderBoardBackgroundForSize(int boardWidth, int boardHeight)
        {
            return GetResolvedBoardBackgroundSettings(boardWidth, boardHeight).ShouldRenderBoardBackground;
        }

        public bool ShouldHidePlayableSurfaceTilesForSize(int boardWidth, int boardHeight)
        {
            return GetResolvedBoardBackgroundSettings(boardWidth, boardHeight).ShouldHidePlayableSurfaceTiles;
        }

        public bool ShouldRenderBoardEdgeFadeForSize(int boardWidth, int boardHeight)
        {
            return GetResolvedBoardBackgroundSettings(boardWidth, boardHeight).ShouldRenderBoardEdgeFade;
        }

        public bool ShouldRenderPlayableAreaOverlayForSize(int boardWidth, int boardHeight)
        {
            return GetResolvedBoardBackgroundSettings(boardWidth, boardHeight).ShouldRenderPlayableAreaOverlay;
        }

        public IReadOnlyCollection<int> GetBlockedTileIdsForSize(int boardWidth, int boardHeight)
        {
            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return Array.Empty<int>();
            }

            var settings = GetResolvedBoardBackgroundSettings(boardWidth, boardHeight);
            IReadOnlyCollection<int> explicitBlockedTileIds = settings.UseExplicitBlockedTileIds
                ? SanitizeBlockedTileIds(settings.ExplicitBlockedTileIds, boardWidth, boardHeight)
                : Array.Empty<int>();

            if (!settings.ShouldUseBackgroundPlayableMask)
            {
                return explicitBlockedTileIds;
            }

            if (TryGetBakedBlockedTileIds(settings, boardWidth, boardHeight, out IReadOnlyCollection<int> bakedBlockedTileIds))
            {
                return MergeBlockedTileIds(bakedBlockedTileIds, explicitBlockedTileIds, boardWidth, boardHeight);
            }

            if (settings.HasPlayableHorizontalSpanProfileMetadata)
            {
                return MergeBlockedTileIds(
                    BuildBlockedTileIdsFromHorizontalSpanProfile(settings, boardWidth, boardHeight),
                    explicitBlockedTileIds,
                    boardWidth,
                    boardHeight);
            }

            if (settings.HasPlayableEllipseMetadata)
            {
                return MergeBlockedTileIds(
                    BuildBlockedTileIdsFromEllipse(settings, boardWidth, boardHeight),
                    explicitBlockedTileIds,
                    boardWidth,
                    boardHeight);
            }

            Texture2D samplingTexture = null;
            bool ownsSamplingTexture = false;
            try
            {
                samplingTexture = GetReadableTexture(settings.BackgroundSprite, out ownsSamplingTexture);
                if (samplingTexture == null)
                {
                    return Array.Empty<int>();
                }

                Rect spriteRect = settings.BackgroundSprite.textureRect;
                Rect safeArea = GetEffectiveBackgroundSafeAreaNormalized(
                    settings.BackgroundSprite,
                    settings.SafeAreaNormalized,
                    settings.ShouldUseBackgroundPlayableMask,
                    settings.ComposeSafeAreaWithBoardBoundsMetadata,
                    settings.HasVisibleAlphaBoundsMetadata,
                    settings.VisibleAlphaBoundsNormalizedMetadata,
                    settings.HasBoardBoundsMetadata,
                    settings.BoardBoundsNormalizedMetadata,
                    settings.HasPlayableEllipseMetadata,
                    settings.PlayableEllipseCenterNormalizedMetadata,
                    settings.PlayableEllipseRadiiNormalizedMetadata,
                    boardWidth,
                    boardHeight);
                float alphaThreshold = Mathf.Clamp01(settings.BackgroundAlphaPlayableThreshold);
                float minimumTileCoverage = Mathf.Clamp01(settings.BackgroundMinTileCoverage);
                float maximumTileClipFraction = Mathf.Clamp(settings.BackgroundMaxTileClipFraction, 0f, 0.49f);
                int tileClipSampleResolution = Mathf.Clamp(settings.BackgroundTileClipSampleResolution, 1, 7);
                float playableSurfaceTileScaleForMask = Mathf.Max(1f, playableSurfaceTileScale);
                float[] clipBudgetSampleOffsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
                    playableSurfaceTileScaleForMask,
                    maximumTileClipFraction,
                    tileClipSampleResolution);
                var blockedTileIds = new List<int>();

                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        bool satisfiesClipBudget = clipBudgetSampleOffsets.Length == 0
                            || EvaluateTileClipBudget(
                                samplingTexture,
                                spriteRect,
                                safeArea,
                                boardWidth,
                                boardHeight,
                                x,
                                y,
                                alphaThreshold,
                                clipBudgetSampleOffsets);

                        bool satisfiesCoverage = minimumTileCoverage <= 0f
                            || EvaluateTileCoverage(
                                samplingTexture,
                                spriteRect,
                                safeArea,
                                boardWidth,
                                boardHeight,
                                x,
                                y,
                                alphaThreshold,
                                minimumTileCoverage);

                        bool isPlayable = satisfiesClipBudget && satisfiesCoverage;
                        if (!isPlayable)
                        {
                            blockedTileIds.Add((y * boardWidth) + x);
                        }
                    }
                }

                if (blockedTileIds.Count >= boardWidth * boardHeight)
                {
                    Debug.LogWarning($"Board medium '{mediumId}' produced a fully blocked playable mask for {boardWidth}x{boardHeight}; ignoring the mask.");
                    return explicitBlockedTileIds;
                }

                return MergeBlockedTileIds(blockedTileIds, explicitBlockedTileIds, boardWidth, boardHeight);
            }
            finally
            {
                if (ownsSamplingTexture && samplingTexture != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(samplingTexture);
                    }
                    else
                    {
                        DestroyImmediate(samplingTexture);
                    }
                }
            }
        }

        public Rect GetBackgroundSafeAreaNormalized()
        {
            return BuildBackgroundSafeAreaNormalized(
                backgroundInsetLeftNormalized,
                backgroundInsetRightNormalized,
                backgroundInsetBottomNormalized,
                backgroundInsetTopNormalized);
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

        private static Rect BuildBackgroundSafeAreaNormalized(float leftInset, float rightInset, float bottomInset, float topInset)
        {
            float left = Mathf.Clamp01(leftInset);
            float right = Mathf.Clamp01(rightInset);
            float bottom = Mathf.Clamp01(bottomInset);
            float top = Mathf.Clamp01(topInset);

            float width = Mathf.Max(0.01f, 1f - left - right);
            float height = Mathf.Max(0.01f, 1f - bottom - top);
            return new Rect(left, bottom, width, height);
        }

        private static Rect SanitizeNormalizedRect(Rect rect)
        {
            float xMin = Mathf.Clamp01(rect.xMin);
            float yMin = Mathf.Clamp01(rect.yMin);
            float xMax = Mathf.Clamp(rect.xMax, xMin + 0.001f, 1f);
            float yMax = Mathf.Clamp(rect.yMax, yMin + 0.001f, 1f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private bool TryGetBackgroundSpriteMetadataVisibleAlphaBoundsNormalized(Sprite sprite, out Rect visibleAlphaBoundsNormalized)
        {
            visibleAlphaBoundsNormalized = new Rect(0f, 0f, 1f, 1f);
            BoardBackgroundSpriteMetadata metadata = GetBackgroundSpriteMetadata(sprite);
            if (metadata == null || !metadata.hasVisibleAlphaBounds)
            {
                return false;
            }

            visibleAlphaBoundsNormalized = SanitizeNormalizedRect(metadata.visibleAlphaBoundsNormalized);
            return true;
        }

        private bool TryGetBackgroundSpriteMetadataBoardBoundsNormalized(Sprite sprite, out Rect boardBoundsNormalized)
        {
            boardBoundsNormalized = new Rect(0f, 0f, 1f, 1f);
            BoardBackgroundSpriteMetadata metadata = GetBackgroundSpriteMetadata(sprite);
            if (metadata == null || !metadata.hasBoardBounds)
            {
                return false;
            }

            boardBoundsNormalized = SanitizeNormalizedRect(metadata.boardBoundsNormalized);
            return true;
        }

        private bool TryGetBackgroundSpriteMetadataPlayableEllipseNormalized(Sprite sprite, out Vector2 playableEllipseCenterNormalized, out Vector2 playableEllipseRadiiNormalized)
        {
            playableEllipseCenterNormalized = new Vector2(0.5f, 0.5f);
            playableEllipseRadiiNormalized = new Vector2(0.5f, 0.5f);
            BoardBackgroundSpriteMetadata metadata = GetBackgroundSpriteMetadata(sprite);
            if (metadata == null || !metadata.hasPlayableEllipse)
            {
                return false;
            }

            (playableEllipseCenterNormalized, playableEllipseRadiiNormalized) = SanitizeNormalizedEllipse(
                metadata.playableEllipseCenterNormalized,
                metadata.playableEllipseRadiiNormalized);
            return true;
        }

        private bool TryGetBackgroundSpriteMetadataPlayableHorizontalSpanProfile(
            Sprite sprite,
            out float playableHorizontalSpanProfileMinYNormalized,
            out float playableHorizontalSpanProfileMaxYNormalized,
            out IReadOnlyList<PlayableHorizontalSpanStop> playableHorizontalSpanProfile)
        {
            playableHorizontalSpanProfileMinYNormalized = 0f;
            playableHorizontalSpanProfileMaxYNormalized = 1f;
            playableHorizontalSpanProfile = Array.Empty<PlayableHorizontalSpanStop>();
            BoardBackgroundSpriteMetadata metadata = GetBackgroundSpriteMetadata(sprite);
            if (metadata == null || !metadata.hasPlayableHorizontalSpanProfile || metadata.playableHorizontalSpanProfile == null || metadata.playableHorizontalSpanProfile.Count == 0)
            {
                return false;
            }

            playableHorizontalSpanProfileMinYNormalized = Mathf.Clamp01(Mathf.Min(metadata.playableHorizontalSpanProfileMinYNormalized, metadata.playableHorizontalSpanProfileMaxYNormalized));
            playableHorizontalSpanProfileMaxYNormalized = Mathf.Clamp01(Mathf.Max(metadata.playableHorizontalSpanProfileMinYNormalized, metadata.playableHorizontalSpanProfileMaxYNormalized));
            playableHorizontalSpanProfile = metadata.playableHorizontalSpanProfile;
            return true;
        }

        private bool TryGetBackgroundSpriteMetadataBakedBlockedTileMasks(
            Sprite sprite,
            out IReadOnlyList<BakedBlockedTileMask> bakedBlockedTileMasks)
        {
            bakedBlockedTileMasks = Array.Empty<BakedBlockedTileMask>();
            BoardBackgroundSpriteMetadata metadata = GetBackgroundSpriteMetadata(sprite);
            if (metadata == null || metadata.bakedBlockedTileMasks == null || metadata.bakedBlockedTileMasks.Count == 0)
            {
                return false;
            }

            bakedBlockedTileMasks = metadata.bakedBlockedTileMasks;
            return true;
        }

        private BoardBackgroundSpriteMetadata GetBackgroundSpriteMetadata(Sprite sprite)
        {
            if (sprite == null || boardBackgroundSpriteMetadata == null)
            {
                return null;
            }

            for (int i = 0; i < boardBackgroundSpriteMetadata.Count; i++)
            {
                BoardBackgroundSpriteMetadata metadata = boardBackgroundSpriteMetadata[i];
                if (metadata != null && metadata.backgroundSprite == sprite)
                {
                    return metadata;
                }
            }

            return null;
        }

        private static IReadOnlyCollection<int> SanitizeBlockedTileIds(IReadOnlyList<int> blockedTileIds, int boardWidth, int boardHeight)
        {
            if (blockedTileIds == null || blockedTileIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            int totalTiles = boardWidth * boardHeight;
            var sanitized = new HashSet<int>();
            for (int i = 0; i < blockedTileIds.Count; i++)
            {
                int tileId = blockedTileIds[i];
                if (tileId >= 0 && tileId < totalTiles)
                {
                    sanitized.Add(tileId);
                }
            }

            if (sanitized.Count >= totalTiles)
            {
                return Array.Empty<int>();
            }

            return sanitized.Count == 0 ? Array.Empty<int>() : new List<int>(sanitized);
        }

        private static IReadOnlyCollection<int> MergeBlockedTileIds(
            IReadOnlyCollection<int> primaryBlockedTileIds,
            IReadOnlyCollection<int> additionalBlockedTileIds,
            int boardWidth,
            int boardHeight)
        {
            if ((primaryBlockedTileIds == null || primaryBlockedTileIds.Count == 0)
                && (additionalBlockedTileIds == null || additionalBlockedTileIds.Count == 0))
            {
                return Array.Empty<int>();
            }

            var merged = new HashSet<int>();
            if (primaryBlockedTileIds != null)
            {
                foreach (int tileId in primaryBlockedTileIds)
                {
                    merged.Add(tileId);
                }
            }

            if (additionalBlockedTileIds != null)
            {
                foreach (int tileId in additionalBlockedTileIds)
                {
                    merged.Add(tileId);
                }
            }

            return SanitizeBlockedTileIds(new List<int>(merged), boardWidth, boardHeight);
        }

        private IReadOnlyCollection<int> GetSanitizedBakedBlockedTileIds(BakedBlockedTileMask bakedMask, int boardWidth, int boardHeight)
        {
            if (bakedMask == null)
            {
                return Array.Empty<int>();
            }

            IReadOnlyCollection<int> sanitizedBlockedTileIds = SanitizeBlockedTileIds(bakedMask.blockedTileIds, boardWidth, boardHeight);
            if (sanitizedBlockedTileIds.Count >= boardWidth * boardHeight)
            {
                Debug.LogWarning($"Board medium '{mediumId}' has a fully blocked baked mask for {boardWidth}x{boardHeight}; ignoring the baked mask.");
                return Array.Empty<int>();
            }

            return sanitizedBlockedTileIds;
        }

        private bool TryGetBakedBlockedTileIds(
            ResolvedBoardBackgroundSettings settings,
            int boardWidth,
            int boardHeight,
            out IReadOnlyCollection<int> bakedBlockedTileIds)
        {
            bakedBlockedTileIds = Array.Empty<int>();
            if (settings.BakedBlockedTileMasksMetadata == null || settings.BakedBlockedTileMasksMetadata.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < settings.BakedBlockedTileMasksMetadata.Count; i++)
            {
                BakedBlockedTileMask bakedMask = settings.BakedBlockedTileMasksMetadata[i];
                if (bakedMask == null || !bakedMask.Matches(boardWidth, boardHeight))
                {
                    continue;
                }

                bakedBlockedTileIds = GetSanitizedBakedBlockedTileIds(bakedMask, boardWidth, boardHeight);
                return bakedBlockedTileIds.Count > 0;
            }

            return false;
        }

        private IReadOnlyCollection<int> BuildBlockedTileIdsFromEllipse(ResolvedBoardBackgroundSettings settings, int boardWidth, int boardHeight)
        {
            if (!TryGetEffectiveBackgroundEllipseNormalized(
                    settings.SafeAreaNormalized,
                    settings.HasPlayableEllipseMetadata,
                    settings.PlayableEllipseCenterNormalizedMetadata,
                    settings.PlayableEllipseRadiiNormalizedMetadata,
                    out Vector2 effectiveEllipseCenterNormalized,
                    out Vector2 effectiveEllipseRadiiNormalized))
            {
                return Array.Empty<int>();
            }

            float minimumTileCoverage = Mathf.Clamp01(settings.BackgroundMinTileCoverage);
            float maximumTileClipFraction = Mathf.Clamp(settings.BackgroundMaxTileClipFraction, 0f, 0.49f);
            int tileClipSampleResolution = Mathf.Clamp(settings.BackgroundTileClipSampleResolution, 1, 7);
            float playableSurfaceTileScaleForMask = Mathf.Max(1f, playableSurfaceTileScale);
            float[] clipBudgetSampleOffsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
                playableSurfaceTileScaleForMask,
                maximumTileClipFraction,
                tileClipSampleResolution);
            var blockedTileIds = new List<int>();

            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    bool satisfiesClipBudget = clipBudgetSampleOffsets.Length == 0
                        || EvaluateTileEllipseClipBudget(
                            effectiveEllipseCenterNormalized,
                            effectiveEllipseRadiiNormalized,
                            boardWidth,
                            boardHeight,
                            x,
                            y,
                            clipBudgetSampleOffsets);

                    bool satisfiesCoverage = minimumTileCoverage <= 0f
                        || EvaluateTileEllipseCoverage(
                            effectiveEllipseCenterNormalized,
                            effectiveEllipseRadiiNormalized,
                            boardWidth,
                            boardHeight,
                            x,
                            y,
                            minimumTileCoverage);

                    if (!(satisfiesClipBudget && satisfiesCoverage))
                    {
                        blockedTileIds.Add((y * boardWidth) + x);
                    }
                }
            }

            if (blockedTileIds.Count >= boardWidth * boardHeight)
            {
                Debug.LogWarning($"Board medium '{mediumId}' produced a fully blocked ellipse mask for {boardWidth}x{boardHeight}; ignoring the mask.");
                return Array.Empty<int>();
            }

            return blockedTileIds;
        }

        private IReadOnlyCollection<int> BuildBlockedTileIdsFromHorizontalSpanProfile(ResolvedBoardBackgroundSettings settings, int boardWidth, int boardHeight)
        {
            if (!TryGetEffectivePlayableHorizontalSpanProfileGeometry(
                    settings,
                    boardWidth,
                    boardHeight,
                    out Rect effectiveProfileAreaNormalized,
                    out float effectiveMinYNormalized,
                    out float effectiveMaxYNormalized)
                || !TryBuildSanitizedPlayableHorizontalSpanProfile(
                    settings.PlayableHorizontalSpanProfileMetadata,
                    effectiveProfileAreaNormalized,
                    out List<SanitizedPlayableHorizontalSpanStop> sanitizedProfile))
            {
                return Array.Empty<int>();
            }

            float minimumTileCoverage = Mathf.Clamp01(settings.BackgroundMinTileCoverage);
            float maximumTileClipFraction = Mathf.Clamp(settings.BackgroundMaxTileClipFraction, 0f, 0.49f);
            int tileClipSampleResolution = Mathf.Clamp(settings.BackgroundTileClipSampleResolution, 1, 7);
            float playableSurfaceTileScaleForMask = Mathf.Max(1f, playableSurfaceTileScale);
            float[] clipBudgetSampleOffsets = BoardMaskClipSampling.BuildClipBudgetSampleOffsets(
                playableSurfaceTileScaleForMask,
                maximumTileClipFraction,
                tileClipSampleResolution);
            var blockedTileIds = new List<int>();

            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    bool satisfiesClipBudget = clipBudgetSampleOffsets.Length == 0
                        || EvaluateTileHorizontalSpanProfileClipBudget(
                            sanitizedProfile,
                            effectiveMinYNormalized,
                            effectiveMaxYNormalized,
                            boardWidth,
                            boardHeight,
                            x,
                            y,
                            clipBudgetSampleOffsets);

                    bool satisfiesCoverage = minimumTileCoverage <= 0f
                        || EvaluateTileHorizontalSpanProfileCoverage(
                            sanitizedProfile,
                            effectiveMinYNormalized,
                            effectiveMaxYNormalized,
                            boardWidth,
                            boardHeight,
                            x,
                            y,
                            minimumTileCoverage);

                    if (!(satisfiesClipBudget && satisfiesCoverage))
                    {
                        blockedTileIds.Add((y * boardWidth) + x);
                    }
                }
            }

            if (blockedTileIds.Count >= boardWidth * boardHeight)
            {
                Debug.LogWarning($"Board medium '{mediumId}' produced a fully blocked horizontal-span mask for {boardWidth}x{boardHeight}; ignoring the mask.");
                return Array.Empty<int>();
            }

            return blockedTileIds;
        }

        private static bool EvaluateTileHorizontalSpanProfileCoverage(
            IReadOnlyList<SanitizedPlayableHorizontalSpanStop> sanitizedProfile,
            float minYNormalized,
            float maxYNormalized,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float minimumTileCoverage)
        {
            const int sampleResolution = 5;
            int coveredSamples = 0;
            int totalSamples = sampleResolution * sampleResolution;

            for (int sampleY = 0; sampleY < sampleResolution; sampleY++)
            {
                for (int sampleX = 0; sampleX < sampleResolution; sampleX++)
                {
                    Vector2 point = SampleHorizontalSpanProfilePointNormalized(
                        boardWidth,
                        boardHeight,
                        tileX,
                        tileY,
                        (sampleX + 0.5f) / sampleResolution,
                        (sampleY + 0.5f) / sampleResolution);
                    if (IsPointInsidePlayableHorizontalSpanProfile(point, sanitizedProfile, minYNormalized, maxYNormalized))
                    {
                        coveredSamples++;
                    }
                }
            }

            return ((float)coveredSamples / totalSamples) >= minimumTileCoverage;
        }

        private static bool EvaluateTileHorizontalSpanProfileClipBudget(
            IReadOnlyList<SanitizedPlayableHorizontalSpanStop> sanitizedProfile,
            float minYNormalized,
            float maxYNormalized,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            IReadOnlyList<float> sampleOffsets)
        {
            if (sampleOffsets == null || sampleOffsets.Count == 0)
            {
                return true;
            }

            for (int sampleY = 0; sampleY < sampleOffsets.Count; sampleY++)
            {
                float sampleOffsetY = sampleOffsets[sampleY];
                for (int sampleX = 0; sampleX < sampleOffsets.Count; sampleX++)
                {
                    float sampleOffsetX = sampleOffsets[sampleX];
                    Vector2 point = SampleHorizontalSpanProfilePointNormalized(
                        boardWidth,
                        boardHeight,
                        tileX,
                        tileY,
                        0.5f + sampleOffsetX,
                        0.5f + sampleOffsetY);
                    if (!IsPointInsidePlayableHorizontalSpanProfile(point, sanitizedProfile, minYNormalized, maxYNormalized))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool EvaluateTileCoverage(
            Texture2D samplingTexture,
            Rect spriteRect,
            Rect safeArea,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float alphaThreshold,
            float minimumTileCoverage)
        {
            const int sampleResolution = 5;
            int coveredSamples = 0;
            int totalSamples = sampleResolution * sampleResolution;

            for (int sampleY = 0; sampleY < sampleResolution; sampleY++)
            {
                for (int sampleX = 0; sampleX < sampleResolution; sampleX++)
                {
                    float normalizedX = safeArea.xMin + ((tileX + ((sampleX + 0.5f) / sampleResolution)) / boardWidth) * safeArea.width;
                    float normalizedY = safeArea.yMin + ((tileY + ((sampleY + 0.5f) / sampleResolution)) / boardHeight) * safeArea.height;
                    float sampleU = (spriteRect.x + (normalizedX * spriteRect.width)) / samplingTexture.width;
                    float sampleV = (spriteRect.y + (normalizedY * spriteRect.height)) / samplingTexture.height;
                    float alpha = samplingTexture.GetPixelBilinear(sampleU, sampleV).a;
                    if (alpha >= alphaThreshold)
                    {
                        coveredSamples++;
                    }
                }
            }

            return ((float)coveredSamples / totalSamples) >= minimumTileCoverage;
        }

        private static bool EvaluateTileClipBudget(
            Texture2D samplingTexture,
            Rect spriteRect,
            Rect safeArea,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float alphaThreshold,
            IReadOnlyList<float> sampleOffsets)
        {
            if (sampleOffsets == null || sampleOffsets.Count == 0)
            {
                return true;
            }

            for (int sampleY = 0; sampleY < sampleOffsets.Count; sampleY++)
            {
                float sampleOffsetY = sampleOffsets[sampleY];
                for (int sampleX = 0; sampleX < sampleOffsets.Count; sampleX++)
                {
                    float sampleOffsetX = sampleOffsets[sampleX];
                    float normalizedX = safeArea.xMin + ((tileX + 0.5f + sampleOffsetX) / boardWidth) * safeArea.width;
                    float normalizedY = safeArea.yMin + ((tileY + 0.5f + sampleOffsetY) / boardHeight) * safeArea.height;
                    float sampleU = (spriteRect.x + (normalizedX * spriteRect.width)) / samplingTexture.width;
                    float sampleV = (spriteRect.y + (normalizedY * spriteRect.height)) / samplingTexture.height;
                    float alpha = samplingTexture.GetPixelBilinear(sampleU, sampleV).a;
                    if (alpha < alphaThreshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool EvaluateTileEllipseCoverage(
            Vector2 ellipseCenterNormalized,
            Vector2 ellipseRadiiNormalized,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float minimumTileCoverage)
        {
            const int sampleResolution = 5;
            int coveredSamples = 0;
            int totalSamples = sampleResolution * sampleResolution;

            for (int sampleY = 0; sampleY < sampleResolution; sampleY++)
            {
                for (int sampleX = 0; sampleX < sampleResolution; sampleX++)
                {
                    Vector2 point = SampleEllipsePointNormalized(
                        ellipseCenterNormalized,
                        ellipseRadiiNormalized,
                        boardWidth,
                        boardHeight,
                        tileX,
                        tileY,
                        (sampleX + 0.5f) / sampleResolution,
                        (sampleY + 0.5f) / sampleResolution);
                    if (IsPointInsideNormalizedEllipse(point, ellipseCenterNormalized, ellipseRadiiNormalized))
                    {
                        coveredSamples++;
                    }
                }
            }

            return ((float)coveredSamples / totalSamples) >= minimumTileCoverage;
        }

        private static bool EvaluateTileEllipseClipBudget(
            Vector2 ellipseCenterNormalized,
            Vector2 ellipseRadiiNormalized,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            IReadOnlyList<float> sampleOffsets)
        {
            if (sampleOffsets == null || sampleOffsets.Count == 0)
            {
                return true;
            }

            for (int sampleY = 0; sampleY < sampleOffsets.Count; sampleY++)
            {
                float sampleOffsetY = sampleOffsets[sampleY];
                for (int sampleX = 0; sampleX < sampleOffsets.Count; sampleX++)
                {
                    float sampleOffsetX = sampleOffsets[sampleX];
                    Vector2 point = SampleEllipsePointNormalized(
                        ellipseCenterNormalized,
                        ellipseRadiiNormalized,
                        boardWidth,
                        boardHeight,
                        tileX,
                        tileY,
                        0.5f + sampleOffsetX,
                        0.5f + sampleOffsetY);
                    if (!IsPointInsideNormalizedEllipse(point, ellipseCenterNormalized, ellipseRadiiNormalized))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Vector2 SampleHorizontalSpanProfilePointNormalized(
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float sampleXWithinTile,
            float sampleYWithinTile)
        {
            return new Vector2(
                Mathf.Clamp01((tileX + sampleXWithinTile) / boardWidth),
                Mathf.Clamp01((tileY + sampleYWithinTile) / boardHeight));
        }

        private static bool IsPointInsidePlayableHorizontalSpanProfile(
            Vector2 pointNormalized,
            IReadOnlyList<SanitizedPlayableHorizontalSpanStop> sanitizedProfile,
            float minYNormalized,
            float maxYNormalized)
        {
            if (pointNormalized.y < minYNormalized || pointNormalized.y > maxYNormalized)
            {
                return false;
            }

            EvaluatePlayableHorizontalSpanAtYNormalized(sanitizedProfile, pointNormalized.y, out float minXNormalized, out float maxXNormalized);
            return pointNormalized.x >= minXNormalized && pointNormalized.x <= maxXNormalized;
        }

        private static void EvaluatePlayableHorizontalSpanAtYNormalized(IReadOnlyList<SanitizedPlayableHorizontalSpanStop> sanitizedProfile, float normalizedY, out float minXNormalized, out float maxXNormalized)
        {
            if (sanitizedProfile == null || sanitizedProfile.Count == 0)
            {
                minXNormalized = 0f;
                maxXNormalized = 1f;
                return;
            }

            float clampedY = Mathf.Clamp01(normalizedY);
            if (sanitizedProfile.Count == 1 || clampedY <= sanitizedProfile[0].NormalizedY)
            {
                minXNormalized = sanitizedProfile[0].MinXNormalized;
                maxXNormalized = sanitizedProfile[0].MaxXNormalized;
                return;
            }

            SanitizedPlayableHorizontalSpanStop lastStop = sanitizedProfile[sanitizedProfile.Count - 1];
            if (clampedY >= lastStop.NormalizedY)
            {
                minXNormalized = lastStop.MinXNormalized;
                maxXNormalized = lastStop.MaxXNormalized;
                return;
            }

            for (int i = 0; i < sanitizedProfile.Count - 1; i++)
            {
                SanitizedPlayableHorizontalSpanStop lowerStop = sanitizedProfile[i];
                SanitizedPlayableHorizontalSpanStop upperStop = sanitizedProfile[i + 1];
                if (clampedY < lowerStop.NormalizedY || clampedY > upperStop.NormalizedY)
                {
                    continue;
                }

                float range = Mathf.Max(0.0001f, upperStop.NormalizedY - lowerStop.NormalizedY);
                float t = Mathf.Clamp01((clampedY - lowerStop.NormalizedY) / range);
                minXNormalized = Mathf.Lerp(lowerStop.MinXNormalized, upperStop.MinXNormalized, t);
                maxXNormalized = Mathf.Lerp(lowerStop.MaxXNormalized, upperStop.MaxXNormalized, t);
                return;
            }

            minXNormalized = lastStop.MinXNormalized;
            maxXNormalized = lastStop.MaxXNormalized;
        }

        private static bool TryBuildSanitizedPlayableHorizontalSpanProfile(
            IReadOnlyList<PlayableHorizontalSpanStop> playableHorizontalSpanProfileMetadata,
            Rect profileAreaNormalized,
            out List<SanitizedPlayableHorizontalSpanStop> sanitizedProfile)
        {
            sanitizedProfile = null;
            if (playableHorizontalSpanProfileMetadata == null || playableHorizontalSpanProfileMetadata.Count == 0)
            {
                return false;
            }

            Rect profileArea = SanitizeNormalizedRect(profileAreaNormalized);
            var workingProfile = new List<SanitizedPlayableHorizontalSpanStop>(playableHorizontalSpanProfileMetadata.Count);
            for (int i = 0; i < playableHorizontalSpanProfileMetadata.Count; i++)
            {
                PlayableHorizontalSpanStop stop = playableHorizontalSpanProfileMetadata[i];
                if (stop == null)
                {
                    continue;
                }

                float localNormalizedY = Mathf.Clamp01(stop.normalizedY);
                float localMinXNormalized = Mathf.Clamp01(stop.minXNormalized);
                float localMaxXNormalized = Mathf.Clamp(stop.maxXNormalized, localMinXNormalized, 1f);
                float normalizedY = profileArea.yMin + (localNormalizedY * profileArea.height);
                float minXNormalized = profileArea.xMin + (localMinXNormalized * profileArea.width);
                float maxXNormalized = profileArea.xMin + (localMaxXNormalized * profileArea.width);
                workingProfile.Add(new SanitizedPlayableHorizontalSpanStop(normalizedY, minXNormalized, maxXNormalized));
            }

            if (workingProfile.Count == 0)
            {
                return false;
            }

            workingProfile.Sort((left, right) => left.NormalizedY.CompareTo(right.NormalizedY));
            sanitizedProfile = workingProfile;
            return true;
        }

        private static bool TryGetEffectivePlayableHorizontalSpanProfileGeometry(
            ResolvedBoardBackgroundSettings settings,
            int boardWidth,
            int boardHeight,
            out Rect effectiveProfileAreaNormalized,
            out float effectiveMinYNormalized,
            out float effectiveMaxYNormalized)
        {
            effectiveProfileAreaNormalized = Rect.MinMaxRect(0f, 0f, 1f, 1f);
            effectiveMinYNormalized = settings.PlayableHorizontalSpanProfileMinYNormalizedMetadata;
            effectiveMaxYNormalized = settings.PlayableHorizontalSpanProfileMaxYNormalizedMetadata;

            if (!settings.ComposeSafeAreaWithBoardBoundsMetadata)
            {
                return true;
            }

            effectiveProfileAreaNormalized = GetEffectiveBackgroundSafeAreaNormalized(
                settings.BackgroundSprite,
                settings.SafeAreaNormalized,
                settings.ShouldUseBackgroundPlayableMask,
                settings.ComposeSafeAreaWithBoardBoundsMetadata,
                settings.HasVisibleAlphaBoundsMetadata,
                settings.VisibleAlphaBoundsNormalizedMetadata,
                settings.HasBoardBoundsMetadata,
                settings.BoardBoundsNormalizedMetadata,
                false,
                default,
                default,
                boardWidth,
                boardHeight);
            effectiveProfileAreaNormalized = SanitizeNormalizedRect(effectiveProfileAreaNormalized);
            effectiveMinYNormalized = effectiveProfileAreaNormalized.yMin + (effectiveMinYNormalized * effectiveProfileAreaNormalized.height);
            effectiveMaxYNormalized = effectiveProfileAreaNormalized.yMin + (effectiveMaxYNormalized * effectiveProfileAreaNormalized.height);
            return true;
        }

        private static float SampleTileCenterAlpha(
            Texture2D samplingTexture,
            Rect spriteRect,
            Rect safeArea,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY)
        {
            float normalizedX = safeArea.xMin + ((tileX + 0.5f) / boardWidth) * safeArea.width;
            float normalizedY = safeArea.yMin + ((tileY + 0.5f) / boardHeight) * safeArea.height;
            float sampleU = (spriteRect.x + (normalizedX * spriteRect.width)) / samplingTexture.width;
            float sampleV = (spriteRect.y + (normalizedY * spriteRect.height)) / samplingTexture.height;
            return samplingTexture.GetPixelBilinear(sampleU, sampleV).a;
        }

        public static Rect GetEffectiveBackgroundSafeAreaNormalized(
            Sprite sprite,
            Rect configuredSafeAreaNormalized,
            bool fitToVisibleAlphaBounds,
            bool composeSafeAreaWithBoardBoundsMetadata = false,
            bool hasVisibleAlphaBoundsMetadata = false,
            Rect visibleAlphaBoundsNormalizedMetadata = default,
            bool hasBoardBoundsMetadata = false,
            Rect boardBoundsNormalizedMetadata = default,
            bool hasPlayableEllipseMetadata = false,
            Vector2 playableEllipseCenterNormalizedMetadata = default,
            Vector2 playableEllipseRadiiNormalizedMetadata = default,
            int boardWidth = 0,
            int boardHeight = 0)
        {
            Rect safeArea = SanitizeNormalizedRect(configuredSafeAreaNormalized);

            if (TryGetEffectiveBackgroundEllipseNormalized(
                    safeArea,
                    hasPlayableEllipseMetadata,
                    playableEllipseCenterNormalizedMetadata,
                    playableEllipseRadiiNormalizedMetadata,
                    out Vector2 effectiveEllipseCenterNormalized,
                    out Vector2 effectiveEllipseRadiiNormalized))
            {
                return BuildNormalizedEllipseBounds(effectiveEllipseCenterNormalized, effectiveEllipseRadiiNormalized);
            }

            if (hasBoardBoundsMetadata)
            {
                Rect boardBounds = SanitizeNormalizedRect(boardBoundsNormalizedMetadata);
                return composeSafeAreaWithBoardBoundsMetadata
                    ? ComposeNormalizedRect(boardBounds, safeArea)
                    : boardBounds;
            }

            Rect visibleAlphaBounds;
            if (hasVisibleAlphaBoundsMetadata)
            {
                visibleAlphaBounds = SanitizeNormalizedRect(visibleAlphaBoundsNormalizedMetadata);
            }
            else if (!fitToVisibleAlphaBounds || sprite == null || !TryGetVisibleAlphaBoundsNormalized(sprite, out visibleAlphaBounds))
            {
                return safeArea;
            }

            Rect effectiveSafeArea = ComposeNormalizedRect(visibleAlphaBounds, safeArea);
            return InscribeBoardAspectRatio(effectiveSafeArea, boardWidth, boardHeight);
        }

        private static bool TryGetEffectiveBackgroundEllipseNormalized(
            Rect configuredSafeAreaNormalized,
            bool hasPlayableEllipseMetadata,
            Vector2 playableEllipseCenterNormalizedMetadata,
            Vector2 playableEllipseRadiiNormalizedMetadata,
            out Vector2 effectiveEllipseCenterNormalized,
            out Vector2 effectiveEllipseRadiiNormalized)
        {
            effectiveEllipseCenterNormalized = default;
            effectiveEllipseRadiiNormalized = default;
            if (!hasPlayableEllipseMetadata)
            {
                return false;
            }

            (Vector2 ellipseCenterNormalized, Vector2 ellipseRadiiNormalized) = SanitizeNormalizedEllipse(
                playableEllipseCenterNormalizedMetadata,
                playableEllipseRadiiNormalizedMetadata);
            Rect effectiveEllipseBounds = ComposeNormalizedRect(
                BuildNormalizedEllipseBounds(ellipseCenterNormalized, ellipseRadiiNormalized),
                SanitizeNormalizedRect(configuredSafeAreaNormalized));
            effectiveEllipseCenterNormalized = new Vector2(effectiveEllipseBounds.center.x, effectiveEllipseBounds.center.y);
            effectiveEllipseRadiiNormalized = new Vector2(effectiveEllipseBounds.width * 0.5f, effectiveEllipseBounds.height * 0.5f);
            return effectiveEllipseRadiiNormalized.x > 0f && effectiveEllipseRadiiNormalized.y > 0f;
        }

        private static Rect ComposeNormalizedRect(Rect outerRectNormalized, Rect innerRectNormalized)
        {
            Rect outer = SanitizeNormalizedRect(outerRectNormalized);
            Rect inner = SanitizeNormalizedRect(innerRectNormalized);
            return new Rect(
                outer.xMin + (inner.xMin * outer.width),
                outer.yMin + (inner.yMin * outer.height),
                outer.width * inner.width,
                outer.height * inner.height);
        }

        private static (Vector2 centerNormalized, Vector2 radiiNormalized) SanitizeNormalizedEllipse(Vector2 centerNormalized, Vector2 radiiNormalized)
        {
            centerNormalized.x = Mathf.Clamp01(centerNormalized.x);
            centerNormalized.y = Mathf.Clamp01(centerNormalized.y);

            float maxRadiusX = Mathf.Max(0.001f, Mathf.Min(centerNormalized.x, 1f - centerNormalized.x));
            float maxRadiusY = Mathf.Max(0.001f, Mathf.Min(centerNormalized.y, 1f - centerNormalized.y));
            radiiNormalized.x = Mathf.Clamp(radiiNormalized.x, 0.001f, maxRadiusX);
            radiiNormalized.y = Mathf.Clamp(radiiNormalized.y, 0.001f, maxRadiusY);
            return (centerNormalized, radiiNormalized);
        }

        private static Rect BuildNormalizedEllipseBounds(Vector2 centerNormalized, Vector2 radiiNormalized)
        {
            return Rect.MinMaxRect(
                centerNormalized.x - radiiNormalized.x,
                centerNormalized.y - radiiNormalized.y,
                centerNormalized.x + radiiNormalized.x,
                centerNormalized.y + radiiNormalized.y);
        }

        private static Vector2 SampleEllipsePointNormalized(
            Vector2 ellipseCenterNormalized,
            Vector2 ellipseRadiiNormalized,
            int boardWidth,
            int boardHeight,
            int tileX,
            int tileY,
            float sampleXWithinTile,
            float sampleYWithinTile)
        {
            float normalizedX = ellipseCenterNormalized.x + ((((tileX + sampleXWithinTile) / boardWidth) * 2f) - 1f) * ellipseRadiiNormalized.x;
            float normalizedY = ellipseCenterNormalized.y + ((((tileY + sampleYWithinTile) / boardHeight) * 2f) - 1f) * ellipseRadiiNormalized.y;
            return new Vector2(normalizedX, normalizedY);
        }

        private static bool IsPointInsideNormalizedEllipse(Vector2 pointNormalized, Vector2 ellipseCenterNormalized, Vector2 ellipseRadiiNormalized)
        {
            float deltaX = (pointNormalized.x - ellipseCenterNormalized.x) / Mathf.Max(0.001f, ellipseRadiiNormalized.x);
            float deltaY = (pointNormalized.y - ellipseCenterNormalized.y) / Mathf.Max(0.001f, ellipseRadiiNormalized.y);
            return ((deltaX * deltaX) + (deltaY * deltaY)) <= 1f;
        }

        private static Rect InscribeBoardAspectRatio(Rect candidateRectNormalized, int boardWidth, int boardHeight)
        {
            Rect candidate = SanitizeNormalizedRect(candidateRectNormalized);
            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return candidate;
            }

            float targetAspectRatio = boardWidth / (float)boardHeight;
            if (targetAspectRatio <= 0f)
            {
                return candidate;
            }

            float candidateAspectRatio = candidate.width / Mathf.Max(0.001f, candidate.height);
            if (Mathf.Abs(candidateAspectRatio - targetAspectRatio) <= 0.0001f)
            {
                return candidate;
            }

            if (candidateAspectRatio > targetAspectRatio)
            {
                float inscribedWidth = candidate.height * targetAspectRatio;
                float insetX = (candidate.width - inscribedWidth) * 0.5f;
                return new Rect(candidate.xMin + insetX, candidate.yMin, inscribedWidth, candidate.height);
            }

            float inscribedHeight = candidate.width / targetAspectRatio;
            float insetY = (candidate.height - inscribedHeight) * 0.5f;
            return new Rect(candidate.xMin, candidate.yMin + insetY, candidate.width, inscribedHeight);
        }

        private static bool TryGetVisibleAlphaBoundsNormalized(Sprite sprite, out Rect bounds)
        {
            bounds = new Rect(0f, 0f, 1f, 1f);
            if (sprite == null)
            {
                return false;
            }

            if (VisibleAlphaBoundsCache.TryGetValue(sprite, out Rect cachedBounds))
            {
                bounds = cachedBounds;
                return true;
            }

            Texture2D samplingTexture = null;
            bool ownsSamplingTexture = false;
            try
            {
                samplingTexture = GetReadableTexture(sprite, out ownsSamplingTexture);
                if (samplingTexture == null)
                {
                    return false;
                }

                Rect spriteRect = sprite.textureRect;
                int minX = Mathf.FloorToInt(spriteRect.xMin);
                int maxX = Mathf.CeilToInt(spriteRect.xMax) - 1;
                int minY = Mathf.FloorToInt(spriteRect.yMin);
                int maxY = Mathf.CeilToInt(spriteRect.yMax) - 1;

                int alphaMinX = maxX;
                int alphaMaxX = minX;
                int alphaMinY = maxY;
                int alphaMaxY = minY;
                bool foundVisiblePixel = false;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (samplingTexture.GetPixel(x, y).a < VisibleAlphaBoundsThreshold)
                        {
                            continue;
                        }

                        foundVisiblePixel = true;
                        alphaMinX = Mathf.Min(alphaMinX, x);
                        alphaMaxX = Mathf.Max(alphaMaxX, x);
                        alphaMinY = Mathf.Min(alphaMinY, y);
                        alphaMaxY = Mathf.Max(alphaMaxY, y);
                    }
                }

                if (!foundVisiblePixel)
                {
                    return false;
                }

                float width = Mathf.Max(1f, spriteRect.width);
                float height = Mathf.Max(1f, spriteRect.height);
                bounds = new Rect(
                    Mathf.Clamp01((alphaMinX - spriteRect.xMin) / width),
                    Mathf.Clamp01((alphaMinY - spriteRect.yMin) / height),
                    Mathf.Clamp((alphaMaxX + 1f - alphaMinX) / width, 0.001f, 1f),
                    Mathf.Clamp((alphaMaxY + 1f - alphaMinY) / height, 0.001f, 1f));

                VisibleAlphaBoundsCache[sprite] = bounds;
                return true;
            }
            finally
            {
                if (ownsSamplingTexture && samplingTexture != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(samplingTexture);
                    }
                    else
                    {
                        DestroyImmediate(samplingTexture);
                    }
                }
            }
        }

        private static Texture2D GetReadableTexture(Sprite sprite, out bool ownsTexture)
        {
            ownsTexture = false;
            if (sprite == null || sprite.texture == null)
            {
                return null;
            }

            Texture2D sourceTexture = sprite.texture;
            if (sourceTexture.isReadable)
            {
                return sourceTexture;
            }

            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                Graphics.Blit(sourceTexture, temporaryRenderTexture);
                RenderTexture.active = temporaryRenderTexture;
                var readableTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, mipChain: false);
                readableTexture.ReadPixels(new Rect(0f, 0f, temporaryRenderTexture.width, temporaryRenderTexture.height), 0, 0);
                readableTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                ownsTexture = true;
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(temporaryRenderTexture);
            }
        }
    }
}
