using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>
    /// Draws the photographic backdrop behind a medium at true physical scale: the setting (a seamless tile,
    /// such as a countertop), the surface (plate or cutting board) centered under the medium, and soft contact
    /// shadows under both. Scale comes from <see cref="BoardBackdropCatalog"/>: the medium's real width fixes
    /// how many world units a centimeter is, and everything else is sized from that.
    /// </summary>
    internal sealed class GridBoardBackdropRenderer
    {
        // Sorting offsets relative to the toast tilemap; the medium photo itself sits at -2.
        private const int MediumShadowSortingOffset = -3;
        private const int SurfaceSortingOffset = -4;
        private const int SurfaceShadowSortingOffset = -5;
        private const int SettingSortingOffset = -6;

        // The setting extends this many surface-widths around the medium, which covers the widest
        // zoomed-out view on an ultrawide screen.
        private const float SettingCoverageInSurfaceWidths = 6f;

        // Soft contact shadows are stacks of darkened copies, each a little larger and lower. Eight thin layers
        // fade smoothly; fewer, darker layers read as an outline. The medium's shadow peaks near 44% at its
        // edge so it stays visible on the tan cutting board, where a fainter one disappeared.
        private const int ShadowLayerCount = 8;
        private const float SurfaceShadowSpreadPerLayer = 0.0055f;
        private const float SurfaceShadowDropPerLayer = 0.004f;
        private const float SurfaceShadowAlphaPerLayer = 0.055f;
        private const float MediumShadowSpreadPerLayer = 0.0045f;
        private const float MediumShadowDropPerLayer = 0.003f;
        private const float MediumShadowAlphaPerLayer = 0.07f;

        private readonly Func<Transform> _getVisualParent;
        private readonly Func<Tilemap> _getToastTilemap;
        private readonly Dictionary<string, Sprite> _loadedSprites = new();

        private SpriteRenderer _settingRenderer;
        private SpriteRenderer _surfaceRenderer;
        private SpriteRenderer[] _surfaceShadowRenderers;
        private SpriteRenderer[] _mediumShadowRenderers;
        private Rect _surfaceLocalRect;
        private bool _hasSurfaceLocalRect;

        public GridBoardBackdropRenderer(Func<Transform> getVisualParent, Func<Tilemap> getToastTilemap)
        {
            _getVisualParent = getVisualParent;
            _getToastTilemap = getToastTilemap;
        }

        /// <summary>
        /// The surface's visible area in the visual parent's local space, when a backdrop is showing.
        /// </summary>
        public bool TryGetSurfaceLocalRect(out Rect localRect)
        {
            localRect = _surfaceLocalRect;
            return _hasSurfaceLocalRect;
        }

        public void Render(SpriteRenderer mediumRenderer, Sprite mediumSprite, int gameplaySeed)
        {
            if (mediumRenderer == null
                || !BoardBackdropCatalog.TryGetMedium(mediumSprite, out BoardBackdropCatalog.MediumScale medium)
                || !BoardBackdropCatalog.TryGetSurface(medium.SurfaceId, out BoardBackdropCatalog.Surface surface)
                || !BoardBackdropCatalog.TryPickSetting(surface, gameplaySeed, out BoardBackdropCatalog.Setting setting))
            {
                Reset();
                return;
            }

            Sprite surfaceSprite = LoadSprite(surface.SurfaceResource);
            Sprite settingSprite = LoadSprite(setting.TileResource);
            if (surfaceSprite == null || settingSprite == null)
            {
                Reset();
                return;
            }

            Transform mediumTransform = mediumRenderer.transform;
            float mediumScale = mediumTransform.localScale.x;
            Vector2 mediumCenter = (Vector2)mediumTransform.localPosition
                + (mediumScale * GetVisibleCenterInSprite(mediumSprite, medium.VisibleRectNormalized));
            float mediumVisibleWidth = mediumScale * mediumSprite.bounds.size.x * medium.VisibleRectNormalized.width;
            float unitsPerCm = mediumVisibleWidth / Mathf.Max(0.01f, medium.WidthCm);

            float surfaceScale = (surface.WidthCm * unitsPerCm)
                / Mathf.Max(0.0001f, surfaceSprite.bounds.size.x * surface.VisibleRectNormalized.width);
            Vector2 surfacePosition = mediumCenter
                - (surfaceScale * GetVisibleCenterInSprite(surfaceSprite, surface.VisibleRectNormalized));
            Vector2 surfaceVisibleSize = surfaceScale * Vector2.Scale(
                surfaceSprite.bounds.size,
                surface.VisibleRectNormalized.size);

            // Unity-aware null checks: renderers destroyed with their scene must be recreated.
            if (_surfaceRenderer == null)
            {
                _surfaceRenderer = CreateRenderer("BackdropSurface", SurfaceSortingOffset);
            }

            if (_settingRenderer == null)
            {
                _settingRenderer = CreateRenderer("BackdropSetting", SettingSortingOffset);
            }

            if (_surfaceShadowRenderers == null || _surfaceShadowRenderers[0] == null)
            {
                _surfaceShadowRenderers = CreateShadowRenderers("BackdropSurfaceShadow", SurfaceShadowSortingOffset);
            }

            if (_mediumShadowRenderers == null || _mediumShadowRenderers[0] == null)
            {
                _mediumShadowRenderers = CreateShadowRenderers("MediumShadow", MediumShadowSortingOffset);
            }

            PlaceSprite(_surfaceRenderer, surfaceSprite, surfacePosition, surfaceScale, Color.white);
            PlaceShadows(
                _surfaceShadowRenderers,
                surfaceSprite,
                surfacePosition,
                surfaceScale,
                surfaceVisibleSize.x,
                SurfaceShadowSpreadPerLayer,
                SurfaceShadowDropPerLayer,
                SurfaceShadowAlphaPerLayer);
            PlaceShadows(
                _mediumShadowRenderers,
                mediumSprite,
                mediumTransform.localPosition,
                mediumScale,
                mediumVisibleWidth,
                MediumShadowSpreadPerLayer,
                MediumShadowDropPerLayer,
                MediumShadowAlphaPerLayer);
            PlaceSetting(settingSprite, mediumCenter, setting.TileCm * unitsPerCm, surfaceVisibleSize);

            _surfaceLocalRect = new Rect(mediumCenter - (surfaceVisibleSize * 0.5f), surfaceVisibleSize);
            _hasSurfaceLocalRect = true;
        }

        public void Reset()
        {
            _hasSurfaceLocalRect = false;
            Hide(_settingRenderer);
            Hide(_surfaceRenderer);
            HideAll(_surfaceShadowRenderers);
            HideAll(_mediumShadowRenderers);
        }

        private void PlaceSetting(Sprite tileSprite, Vector2 center, float tileWorldWidth, Vector2 surfaceVisibleSize)
        {
            float tileScale = tileWorldWidth / Mathf.Max(0.0001f, tileSprite.bounds.size.x);
            float coverage = Mathf.Max(surfaceVisibleSize.x, surfaceVisibleSize.y) * SettingCoverageInSurfaceWidths;

            PlaceSprite(_settingRenderer, tileSprite, center, tileScale, Color.white);
            _settingRenderer.drawMode = SpriteDrawMode.Tiled;
            _settingRenderer.tileMode = SpriteTileMode.Continuous;
            _settingRenderer.size = new Vector2(coverage / tileScale, coverage / tileScale);
        }

        private static void PlaceShadows(
            SpriteRenderer[] shadowRenderers,
            Sprite sprite,
            Vector2 position,
            float scale,
            float visibleWidth,
            float spreadPerLayer,
            float dropPerLayer,
            float alphaPerLayer)
        {
            for (int i = 0; i < shadowRenderers.Length; i++)
            {
                int layer = i + 1;
                Vector2 offset = new Vector2(0f, -dropPerLayer * layer * visibleWidth);
                float layerScale = scale * (1f + (spreadPerLayer * layer));
                PlaceSprite(shadowRenderers[i], sprite, position + offset, layerScale, new Color(0f, 0f, 0f, alphaPerLayer));
            }
        }

        private static void PlaceSprite(SpriteRenderer renderer, Sprite sprite, Vector2 position, float scale, Color color)
        {
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.enabled = true;
        }

        // Center of the sprite's visible rect, relative to the sprite pivot, in unscaled sprite units.
        private static Vector2 GetVisibleCenterInSprite(Sprite sprite, Rect visibleRectNormalized)
        {
            Bounds bounds = sprite.bounds;
            return (Vector2)bounds.min + Vector2.Scale(bounds.size, visibleRectNormalized.center);
        }

        private Sprite LoadSprite(string resourcePath)
        {
            if (!_loadedSprites.TryGetValue(resourcePath, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[GridBoardBackdropRenderer] Missing backdrop sprite at Resources/{resourcePath}.");
                }

                _loadedSprites[resourcePath] = sprite;
            }

            return sprite;
        }

        private SpriteRenderer[] CreateShadowRenderers(string name, int sortingOffset)
        {
            var renderers = new SpriteRenderer[ShadowLayerCount];
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i] = CreateRenderer($"{name}{i + 1}", sortingOffset);
            }

            return renderers;
        }

        private SpriteRenderer CreateRenderer(string name, int sortingOffset)
        {
            var rendererObject = new GameObject(name);
            rendererObject.transform.SetParent(_getVisualParent(), false);
            var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();

            var toastTilemap = _getToastTilemap();
            var tilemapRenderer = toastTilemap != null ? toastTilemap.GetComponent<TilemapRenderer>() : null;
            if (tilemapRenderer != null)
            {
                spriteRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = tilemapRenderer.sortingOrder + sortingOffset;
            }

            return spriteRenderer;
        }

        private static void Hide(SpriteRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void HideAll(SpriteRenderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Hide(renderers[i]);
            }
        }
    }
}
