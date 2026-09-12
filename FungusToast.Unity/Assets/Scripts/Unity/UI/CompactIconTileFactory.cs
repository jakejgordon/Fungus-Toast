using TMPro;
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

        private const string BadgeObjectName = "CornerBadge";
        private const float BadgeWidth = 18f;
        private const float BadgeHeight = 16f;

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
        /// Sets the small bottom-right number on a tile (rounds remaining on a surge, a stack
        /// count), creating it on first use. Returns the label so a caller that keeps its tiles
        /// across refreshes can update the text without rebuilding.
        /// </summary>
        public static TextMeshProUGUI SetCornerBadge(GameObject tile, string text, Color background)
        {
            Transform existing = tile.transform.Find(BadgeObjectName);
            TextMeshProUGUI label;

            if (existing != null)
            {
                label = existing.GetComponentInChildren<TextMeshProUGUI>(true);
                var existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    existingImage.color = background;
                }
            }
            else
            {
                var badgeObject = new GameObject(BadgeObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObject.transform.SetParent(tile.transform, false);

                var badgeRect = badgeObject.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.anchoredPosition = new Vector2(-1f, 1f);
                badgeRect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);

                var badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.color = background;
                badgeImage.raycastTarget = false;

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(badgeObject.transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                label = labelObject.GetComponent<TextMeshProUGUI>();
                label.fontSize = UIStyleTokens.Typography.MicroMinimum;
                label.fontStyle = FontStyles.Bold;
                label.color = UIStyleTokens.Text.Primary;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                {
                    label.font = TMP_Settings.defaultFontAsset;
                }
            }

            if (label != null)
            {
                label.text = text;
            }

            return label;
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
