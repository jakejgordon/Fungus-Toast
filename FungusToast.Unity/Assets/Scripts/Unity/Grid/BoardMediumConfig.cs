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
        public sealed class BoardBackgroundSpriteMetadata
        {
            public Sprite backgroundSprite;
            public bool hasVisibleAlphaBounds = false;
            public Rect visibleAlphaBoundsNormalized = new(0f, 0f, 1f, 1f);
            public bool hasBoardBounds = false;
            public Rect boardBoundsNormalized = new(0f, 0f, 1f, 1f);
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
                float backgroundScaleMultiplier,
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
                BackgroundScaleMultiplier = backgroundScaleMultiplier;
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
            public float BackgroundScaleMultiplier { get; }
            public bool RenderBoardEdgeFade { get; }
            public Color BoardEdgeFadeColor { get; }
            public float BoardEdgeFadeWidthTiles { get; }
            public float BoardEdgeFadeNoiseStrength { get; }

            public bool ShouldRenderBoardBackground => RenderBoardBackground && BackgroundSprite != null;
            public bool ShouldHidePlayableSurfaceTiles => ShouldRenderBoardBackground && HidePlayableSurfaceTiles;
            public bool ShouldUseBackgroundAlphaPlayableMask => ShouldRenderBoardBackground && DeriveBlockedTilesFromBackgroundAlpha && BackgroundSprite != null;
            public bool ShouldRenderBoardEdgeFade => ShouldRenderBoardBackground && RenderBoardEdgeFade && BoardEdgeFadeColor.a > 0f && BoardEdgeFadeWidthTiles > 0f;
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
            bool resolvedRenderBoardEdgeFade,
            Color resolvedBoardEdgeFadeColor,
            float resolvedBoardEdgeFadeWidthTiles,
            float resolvedBoardEdgeFadeNoiseStrength)
        {
            bool hasVisibleAlphaBoundsMetadata = TryGetBackgroundSpriteMetadataVisibleAlphaBoundsNormalized(resolvedBackgroundSprite, out Rect visibleAlphaBoundsNormalizedMetadata);
            bool hasBoardBoundsMetadata = TryGetBackgroundSpriteMetadataBoardBoundsNormalized(resolvedBackgroundSprite, out Rect boardBoundsNormalizedMetadata);
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
                resolvedBackgroundScaleMultiplier,
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

        public IReadOnlyCollection<int> GetBlockedTileIdsForSize(int boardWidth, int boardHeight)
        {
            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return Array.Empty<int>();
            }

            var settings = GetResolvedBoardBackgroundSettings(boardWidth, boardHeight);
            if (settings.UseExplicitBlockedTileIds)
            {
                return SanitizeBlockedTileIds(settings.ExplicitBlockedTileIds, boardWidth, boardHeight);
            }

            if (!settings.ShouldUseBackgroundAlphaPlayableMask)
            {
                return Array.Empty<int>();
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
                    settings.ShouldUseBackgroundAlphaPlayableMask,
                    settings.ComposeSafeAreaWithBoardBoundsMetadata,
                    settings.HasVisibleAlphaBoundsMetadata,
                    settings.VisibleAlphaBoundsNormalizedMetadata,
                    settings.HasBoardBoundsMetadata,
                    settings.BoardBoundsNormalizedMetadata,
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
                        if (!isPlayable && maximumTileClipFraction <= 0f && minimumTileCoverage <= 0f)
                        {
                            isPlayable = SampleTileCenterAlpha(
                                samplingTexture,
                                spriteRect,
                                safeArea,
                                boardWidth,
                                boardHeight,
                                x,
                                y) >= alphaThreshold;
                        }

                        if (!isPlayable)
                        {
                            blockedTileIds.Add((y * boardWidth) + x);
                        }
                    }
                }

                if (blockedTileIds.Count >= boardWidth * boardHeight)
                {
                    Debug.LogWarning($"Board medium '{mediumId}' produced a fully blocked playable mask for {boardWidth}x{boardHeight}; ignoring the mask.");
                    return Array.Empty<int>();
                }

                return blockedTileIds;
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
            int boardWidth = 0,
            int boardHeight = 0)
        {
            Rect safeArea = SanitizeNormalizedRect(configuredSafeAreaNormalized);

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

            Rect composedSafeArea = ComposeNormalizedRect(visibleAlphaBounds, safeArea);
            return FitNormalizedRectToAspectRatio(composedSafeArea, boardWidth, boardHeight);
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

        private static Rect FitNormalizedRectToAspectRatio(Rect rect, int boardWidth, int boardHeight)
        {
            Rect sanitized = SanitizeNormalizedRect(rect);
            if (boardWidth <= 0 || boardHeight <= 0)
            {
                return sanitized;
            }

            float targetAspect = boardWidth / (float)boardHeight;
            if (targetAspect <= 0f)
            {
                return sanitized;
            }

            float currentAspect = sanitized.width / sanitized.height;
            if (Mathf.Abs(currentAspect - targetAspect) <= 0.0001f)
            {
                return sanitized;
            }

            if (currentAspect > targetAspect)
            {
                float fittedWidth = sanitized.height * targetAspect;
                float x = sanitized.xMin + ((sanitized.width - fittedWidth) * 0.5f);
                return new Rect(x, sanitized.yMin, fittedWidth, sanitized.height);
            }

            float fittedHeight = sanitized.width / targetAspect;
            float y = sanitized.yMin + ((sanitized.height - fittedHeight) * 0.5f);
            return new Rect(sanitized.xMin, y, sanitized.width, fittedHeight);
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
