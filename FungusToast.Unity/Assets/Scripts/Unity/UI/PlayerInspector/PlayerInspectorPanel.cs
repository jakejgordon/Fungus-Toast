#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using FungusToast.Core.Players;
using FungusToast.Unity.UI.Campaign;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// The pinned, interactive counterpart to the mold-icon hover tooltip. The shared
    /// <see cref="TooltipView"/> is deliberately text-only and never a raycast target, so it can
    /// never host hoverable adaptation and mycovariant icons. This panel does: it renders the same
    /// <see cref="PlayerInspectorContent"/> sections, but shows owned traits as real icon tiles
    /// that each carry their own tooltip trigger.
    ///
    /// Nested tooltips work because this panel is not the shared tooltip view — an icon inside it
    /// can take that view without evicting anything.
    ///
    /// One instance exists per session, mirroring <see cref="TooltipManager"/>.
    /// </summary>
    public sealed class PlayerInspectorPanel : MonoBehaviour
    {
        private const float PanelWidth = 380f;
        private const float PanelPadding = 14f;
        private const float SectionSpacing = 8f;
        private const float HeaderHeight = 30f;
        private const float CloseButtonSize = 26f;
        private const float TitleFontSize = 20f;
        private const float SectionHeaderFontSize = 17f;
        private const float BodyFontSize = UIStyleTokens.Typography.CaptionMinimum;

        /// <summary>Gap between the anchor and the panel edge, matching the tooltip's own gap.</summary>
        private const float AnchorGap = 12f;
        private const float ScreenPadding = 12f;

        /// <summary>
        /// Content is re-derived on a timer rather than every frame: the markup rebuild forces a
        /// TMP layout pass, and player state only changes between phases.
        /// </summary>
        private const float ContentRefreshIntervalSeconds = 0.5f;

        private static readonly int IconColumns =
            Mathf.Max(1, Mathf.FloorToInt(
                (PanelWidth - (PanelPadding * 2f) + CompactIconTileFactory.Spacing)
                / (CompactIconTileFactory.TileSize + CompactIconTileFactory.Spacing)));

        /// <summary>Raised whenever the player opens the inspector, so onboarding can retire its hint.</summary>
        public static event Action? Opened;

        private static PlayerInspectorPanel? instance;

        private RectTransform rootRect = null!;
        private CanvasGroup canvasGroup = null!;
        private RectTransform canvasRect = null!;
        private Canvas rootCanvas = null!;
        private TextMeshProUGUI titleText = null!;
        private TextMeshProUGUI bodyText = null!;
        private TextMeshProUGUI adaptationHeaderText = null!;
        private TextMeshProUGUI mycovariantHeaderText = null!;
        private RectTransform adaptationGrid = null!;
        private RectTransform mycovariantGrid = null!;

        private readonly List<GameObject> adaptationTiles = new();
        private readonly List<GameObject> mycovariantTiles = new();

        private Player? trackedPlayer;
        private RectTransform? anchor;
        private float nextContentRefreshTime;

        // Null means "never built", which an empty trait list must not match: otherwise the first
        // refresh for a player with no adaptations skips the rebuild that collapses the empty grid.
        private string? adaptationSignature;
        private string? mycovariantSignature;

        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        /// <summary>True when the panel is currently open against this exact anchor.</summary>
        public static bool IsOpenFor(RectTransform? candidateAnchor) =>
            IsOpen && candidateAnchor != null && instance!.anchor == candidateAnchor;

        /// <summary>
        /// Opens the inspector for <paramref name="player"/>, or closes it if it is already open
        /// against the same anchor. Returns true when the panel ended up open.
        /// </summary>
        public static bool Toggle(Player? player, RectTransform? anchorRect, Canvas? canvas)
        {
            if (IsOpenFor(anchorRect))
            {
                Close();
                return false;
            }

            return Show(player, anchorRect, canvas);
        }

        public static bool Show(Player? player, RectTransform? anchorRect, Canvas? canvas)
        {
            if (player == null || anchorRect == null)
            {
                return false;
            }

            Canvas? host = canvas != null ? canvas.rootCanvas : anchorRect.GetComponentInParent<Canvas>()?.rootCanvas;
            if (host == null)
            {
                return false;
            }

            PlayerInspectorPanel? panel = EnsureInstance(host);
            if (panel == null)
            {
                return false;
            }

            panel.Open(player, anchorRect);
            Opened?.Invoke();
            return true;
        }

        public static void Close()
        {
            if (instance != null)
            {
                instance.Hide();
            }
        }

        private static PlayerInspectorPanel? EnsureInstance(Canvas host)
        {
            if (instance != null)
            {
                return instance;
            }

            var panelObject = new GameObject(
                "UI_PlayerInspectorPanel",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(Outline),
                typeof(PlayerInspectorPanel));
            panelObject.transform.SetParent(host.transform, false);

            instance = panelObject.GetComponent<PlayerInspectorPanel>();
            instance.Build(host);
            return instance;
        }

        private void Build(Canvas host)
        {
            rootCanvas = host;
            canvasRect = (RectTransform)host.transform;

            rootRect = (RectTransform)transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = Vector2.zero;

            // The panel sits above the board and must be clickable, unlike the shared tooltip.
            var panelCanvas = gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = short.MaxValue - 1;
            gameObject.AddComponent<GraphicRaycaster>();

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            var background = GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelSecondary;
            background.raycastTarget = true;

            var outline = GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                Mathf.RoundToInt(PanelPadding),
                Mathf.RoundToInt(PanelPadding),
                Mathf.RoundToInt(PanelPadding),
                Mathf.RoundToInt(PanelPadding));
            layout.spacing = SectionSpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var widthElement = gameObject.AddComponent<LayoutElement>();
            widthElement.preferredWidth = PanelWidth;
            widthElement.minWidth = PanelWidth;

            BuildHeader();
            bodyText = CreateLabel("Body", BodyFontSize, FontStyles.Normal, UIStyleTokens.Text.Primary);
            adaptationHeaderText = CreateLabel("AdaptationsHeader", SectionHeaderFontSize, FontStyles.Bold, UIStyleTokens.Text.Muted);
            adaptationGrid = CompactIconTileFactory.CreateGrid(rootRect, "UI_InspectorAdaptationGrid", IconColumns);
            mycovariantHeaderText = CreateLabel("MycovariantsHeader", SectionHeaderFontSize, FontStyles.Bold, UIStyleTokens.Text.Muted);
            mycovariantGrid = CompactIconTileFactory.CreateGrid(rootRect, "UI_InspectorMycovariantGrid", IconColumns);

            gameObject.SetActive(false);
        }

        private void BuildHeader()
        {
            var headerObject = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            headerObject.transform.SetParent(rootRect, false);

            var headerLayout = headerObject.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = HeaderHeight;
            headerLayout.minHeight = HeaderHeight;

            var headerRect = (RectTransform)headerObject.transform;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(headerRect, false);
            var titleRect = (RectTransform)titleObject.transform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = new Vector2(-(CloseButtonSize + 6f), 0f);

            titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.color = UIStyleTokens.Text.Primary;
            titleText.fontStyle = FontStyles.Bold;
            titleText.fontSize = TitleFontSize;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.raycastTarget = false;
            ApplyDefaultFont(titleText);
            TMPOverflowUtility.SetSafeEllipsis(titleText);

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(headerRect, false);
            var closeRect = (RectTransform)closeObject.transform;
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);
            closeRect.anchoredPosition = Vector2.zero;

            closeObject.GetComponent<Image>().color = UIStyleTokens.Surface.PanelElevated;

            var closeButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(closeButton);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeRect, false);
            var closeLabelRect = (RectTransform)closeLabelObject.transform;
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 16f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;
            ApplyDefaultFont(closeLabel);
        }

        private TextMeshProUGUI CreateLabel(string objectName, float fontSize, FontStyles style, Color color)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rootRect, false);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.color = color;
            label.fontStyle = style;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.richText = true;
            label.raycastTarget = false;
            ApplyDefaultFont(label);

            return label;
        }

        private static void ApplyDefaultFont(TextMeshProUGUI label)
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }
        }

        private void Open(Player player, RectTransform anchorRect)
        {
            trackedPlayer = player;
            anchor = anchorRect;
            adaptationSignature = null;
            mycovariantSignature = null;
            nextContentRefreshTime = 0f;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // The hover tooltip and this panel would otherwise stack on the same icon.
            TooltipManager.Instance?.CancelAll();

            RefreshContent();
            Reposition();
        }

        private void Hide()
        {
            trackedPlayer = null;
            anchor = null;
            ClearTiles(adaptationTiles);
            ClearTiles(mycovariantTiles);
            adaptationSignature = null;
            mycovariantSignature = null;

            // Tiles inside this panel own the shared tooltip while hovered; closing the panel
            // destroys them, so release the tooltip rather than leaving it orphaned on screen.
            TooltipManager.Instance?.CancelAll();
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // The scoreboard re-sorts rows by rank every update, so a static position would drift
            // off the icon it describes.
            if (anchor == null || !anchor.gameObject.activeInHierarchy || trackedPlayer == null)
            {
                Hide();
                return;
            }

            if (Time.unscaledTime >= nextContentRefreshTime)
            {
                RefreshContent();
            }

            Reposition();
        }

        private void RefreshContent()
        {
            nextContentRefreshTime = Time.unscaledTime + ContentRefreshIntervalSeconds;

            if (trackedPlayer == null)
            {
                return;
            }

            var manager = GameManager.Instance;

            titleText.text = trackedPlayer.PlayerName;

            var sections = new List<PlayerInspectorSection>(
                PlayerInspectorContent.BuildSummarySections(trackedPlayer, manager, includeTraitLines: false));

            if (PlayerInspectorContent.IsDevelopmentTestingEnabled(manager))
            {
                sections.AddRange(PlayerInspectorContent.BuildDevelopmentSections(trackedPlayer));
            }

            bodyText.text = PlayerInspectorMarkup.Render(sections);

            IReadOnlyList<PlayerAdaptation> adaptations = PlayerInspectorContent.GetOwnedAdaptations(trackedPlayer);
            IReadOnlyList<PlayerMycovariant> mycovariants = PlayerInspectorContent.GetOwnedMycovariants(trackedPlayer);

            adaptationHeaderText.text = FormatSectionHeader("Adaptations", adaptations.Count);
            mycovariantHeaderText.text = FormatSectionHeader("Mycovariants", mycovariants.Count);

            RebuildAdaptationTiles(adaptations);
            RebuildMycovariantTiles(mycovariants);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        private static string FormatSectionHeader(string label, int count) =>
            count > 0 ? $"{label} ({count})" : $"{label}: None";

        private void RebuildAdaptationTiles(IReadOnlyList<PlayerAdaptation> adaptations)
        {
            string signature = BuildSignature(adaptations, pa => pa.Adaptation.Id.ToString());
            if (signature == adaptationSignature)
            {
                return;
            }

            adaptationSignature = signature;
            ClearTiles(adaptationTiles);

            foreach (PlayerAdaptation owned in adaptations)
            {
                var definition = owned.Adaptation;
                GameObject tile = CompactIconTileFactory.CreateTile(
                    $"UI_InspectorAdaptation_{definition.Id}",
                    adaptationGrid,
                    AdaptationArtRepository.GetIcon(definition));
                adaptationTiles.Add(tile);

                var provider = tile.AddComponent<AdaptationTooltipProvider>();
                provider.Initialize(owned);
                AttachTileTooltip(tile, provider);
            }

            ApplyGridHeight(adaptationGrid, adaptations.Count);
        }

        private void RebuildMycovariantTiles(IReadOnlyList<PlayerMycovariant> mycovariants)
        {
            string signature = BuildSignature(mycovariants, pm => pm.Mycovariant.Id.ToString());
            if (signature == mycovariantSignature)
            {
                return;
            }

            mycovariantSignature = signature;
            ClearTiles(mycovariantTiles);

            foreach (PlayerMycovariant owned in mycovariants)
            {
                var definition = owned.Mycovariant;
                GameObject tile = CompactIconTileFactory.CreateTile(
                    $"UI_InspectorMycovariant_{definition.Id}",
                    mycovariantGrid,
                    MycovariantArtRepository.GetIcon(definition));
                mycovariantTiles.Add(tile);

                var provider = tile.AddComponent<MycovariantTooltipProvider>();
                provider.Initialize(owned);
                AttachTileTooltip(tile, provider);
            }

            ApplyGridHeight(mycovariantGrid, mycovariants.Count);
        }

        private static void AttachTileTooltip(GameObject tile, MonoBehaviour provider)
        {
            var trigger = tile.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private static string BuildSignature<T>(IReadOnlyList<T> items, Func<T, string> idSelector)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                builder.Append(idSelector(items[i])).Append(',');
            }

            return builder.ToString();
        }

        private static void ApplyGridHeight(RectTransform grid, int tileCount)
        {
            if (!grid.TryGetComponent(out LayoutElement layout))
            {
                return;
            }

            int rows = tileCount <= 0 ? 0 : Mathf.CeilToInt(tileCount / (float)IconColumns);
            float height = rows <= 0
                ? 0f
                : (rows * CompactIconTileFactory.TileSize) + ((rows - 1) * CompactIconTileFactory.Spacing);

            layout.preferredHeight = height;
            layout.minHeight = height;
            grid.gameObject.SetActive(rows > 0);
        }

        private static void ClearTiles(List<GameObject> tiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null)
                {
                    Destroy(tiles[i]);
                }
            }

            tiles.Clear();
        }

        /// <summary>
        /// Places the panel beside its anchor, preferring the side with room. The scoreboard lives
        /// on the right, so the left of the anchor is tried first.
        /// </summary>
        private void Reposition()
        {
            if (anchor == null || canvasRect == null)
            {
                return;
            }

            Camera? cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            var anchorCorners = new Vector3[4];
            anchor.GetWorldCorners(anchorCorners); // 0=BL, 1=TL, 2=TR, 3=BR
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, anchorCorners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, anchorCorners[2]);

            float anchorLeft = Mathf.Min(bottomLeft.x, topRight.x);
            float anchorRight = Mathf.Max(bottomLeft.x, topRight.x);
            float anchorTop = Mathf.Max(bottomLeft.y, topRight.y);

            var panelCorners = new Vector3[4];
            rootRect.GetWorldCorners(panelCorners);
            Vector2 panelBottomLeft = RectTransformUtility.WorldToScreenPoint(cam, panelCorners[0]);
            Vector2 panelTopRight = RectTransformUtility.WorldToScreenPoint(cam, panelCorners[2]);
            float panelWidth = Mathf.Abs(panelTopRight.x - panelBottomLeft.x);
            float panelHeight = Mathf.Abs(panelTopRight.y - panelBottomLeft.y);

            // Pivot is top-right, so the placement point is the panel's top-right corner.
            var pivot = new Vector2(1f, 1f);
            float x = anchorLeft - AnchorGap;
            if (x - panelWidth < ScreenPadding && anchorRight + AnchorGap + panelWidth <= Screen.width - ScreenPadding)
            {
                pivot = new Vector2(0f, 1f);
                x = anchorRight + AnchorGap;
            }

            float minX = ScreenPadding + (pivot.x * panelWidth);
            float maxX = Screen.width - ScreenPadding - ((1f - pivot.x) * panelWidth);
            x = maxX >= minX ? Mathf.Clamp(x, minX, maxX) : (minX + maxX) * 0.5f;

            float minY = ScreenPadding + panelHeight;
            float maxY = Screen.height - ScreenPadding;
            float y = maxY >= minY ? Mathf.Clamp(anchorTop, minY, maxY) : (minY + maxY) * 0.5f;

            rootRect.pivot = pivot;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, new Vector2(x, y), cam, out Vector2 localPoint))
            {
                rootRect.anchoredPosition = localPoint;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
