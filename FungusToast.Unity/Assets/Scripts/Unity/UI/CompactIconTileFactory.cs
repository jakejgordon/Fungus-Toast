using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Builds the small hoverable trait icon tiles shared by the mold profile sidebar and the
    /// pinned player inspector. Both surfaces show the same adaptation/mycovariant art at the
    /// same size with the same hover feedback, so the tile geometry lives here rather than being
    /// duplicated per surface.
    /// </summary>
    public static class CompactIconTileFactory
    {
        /// <summary>Pointer target size. Matches the minimum touch target so tiles stay tappable.</summary>
        public const float TileSize = UIStyleTokens.Interaction.MinimumTargetSize;

        /// <summary>Rendered sprite size, inset inside the tile so the tile background reads as a frame.</summary>
        public const float VisualSize = 28f;

        public const float Spacing = 4f;

        /// <summary>
        /// Creates one icon tile parented to <paramref name="parent"/>. The returned object is the
        /// raycast target, so callers attach their own tooltip provider and trigger to it.
        /// </summary>
        public static GameObject CreateTile(string objectName, RectTransform parent, Sprite sprite)
        {
            var iconObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            iconObject.transform.SetParent(parent, false);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(TileSize, TileSize);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.preferredWidth = TileSize;
            layout.preferredHeight = TileSize;
            layout.minWidth = TileSize;
            layout.minHeight = TileSize;

            var background = iconObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;
            background.raycastTarget = true;

            var outline = iconObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = false;

            var renderedIconObject = new GameObject("RenderedIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            renderedIconObject.transform.SetParent(iconObject.transform, false);

            var renderedIconRect = renderedIconObject.GetComponent<RectTransform>();
            renderedIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            renderedIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            renderedIconRect.pivot = new Vector2(0.5f, 0.5f);
            renderedIconRect.anchoredPosition = Vector2.zero;
            renderedIconRect.sizeDelta = new Vector2(VisualSize, VisualSize);

            var renderedIcon = renderedIconObject.GetComponent<Image>();
            renderedIcon.sprite = sprite;
            renderedIcon.type = Image.Type.Simple;
            renderedIcon.preserveAspect = true;
            renderedIcon.color = Color.white;
            renderedIcon.raycastTarget = false;

            var hoverFeedback = iconObject.AddComponent<CompactIconHoverFeedback>();
            hoverFeedback.Initialize(background, outline);

            return iconObject;
        }

        /// <summary>
        /// Creates a fixed-column grid sized for <see cref="CreateTile"/> output.
        /// </summary>
        public static RectTransform CreateGrid(RectTransform parent, string objectName, int maxColumns)
        {
            var gridObject = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(parent, false);

            var rect = gridObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(TileSize, TileSize);
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = maxColumns;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            var layout = gridObject.GetComponent<LayoutElement>();
            layout.preferredHeight = TileSize;
            layout.flexibleHeight = 0f;

            return rect;
        }
    }
}
