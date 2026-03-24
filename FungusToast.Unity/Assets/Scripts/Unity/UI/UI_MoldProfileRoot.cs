using FungusToast.Core; // for MutationType enum
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations; // keep if other mutation id helpers needed
using FungusToast.Core.Phases; // GrowthMutationProcessor lives here
using FungusToast.Core.Players;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Unity.UI.Campaign;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_MoldProfileRoot : MonoBehaviour
    {
        private const float HeaderTextScale = 1.18f;

        [Header("Growth Preview (All 8 Directions)")]
        [Tooltip("Parent containers for the 8 growth preview cells (N,NE,E,SE,S,SW,W,NW). Assign the parent objects; children auto-located by name.")]
        [SerializeField] private GrowthDirectionCell[] directionCells = Array.Empty<GrowthDirectionCell>();
        [SerializeField] private Image centerPlayerIcon;
        [SerializeField] private TextMeshProUGUI growthPreviewHeaderText;

        [Header("Campaign Adaptations")]
        [SerializeField] private RectTransform adaptationSectionRoot;
        [SerializeField] private TextMeshProUGUI adaptationHeaderText;
        [SerializeField] private RectTransform adaptationIconGridRoot;
        [SerializeField] private RectTransform adaptationSectionHostRoot;
        [SerializeField] private string adaptationHeaderLabel = "Adaptations";
        [SerializeField, TextArea] private string adaptationHeaderTooltip = "Adaptations are permanent campaign traits your mold develops by winning campaign levels.";

        [Header("Campaign Mycovariants")]
        [SerializeField] private RectTransform mycovariantSectionRoot;
        [SerializeField] private TextMeshProUGUI mycovariantHeaderText;
        [SerializeField] private RectTransform mycovariantIconGridRoot;
        [SerializeField] private string mycovariantHeaderLabel = "Mycovariants";
        [SerializeField, TextArea] private string mycovariantHeaderTooltip = "Mycovariants are game-only traits your mold carries for the current match.";

        [Header("Board Overlay Legend")]
        [SerializeField] private RectTransform boardOverlayLegendSectionRoot;
        [SerializeField] private TextMeshProUGUI boardOverlayLegendHeaderText;
        [SerializeField] private RectTransform boardOverlayLegendIconGridRoot;
        [SerializeField] private string boardOverlayLegendHeaderLabel = "Common Symbols";
        [SerializeField, TextArea] private string boardOverlayLegendHeaderTooltip = "Hover a symbol to highlight every matching tile on the toast and learn what that overlay means.";

        [Header("Formatting / Visuals")]
        [SerializeField] private Color zeroChanceColor = new Color(1f,1f,1f,0.35f);
        [SerializeField] private Color finalZeroGreen = new Color(0.4f, 1f, 0.4f, 1f);
        [SerializeField] private string growthPreviewHeaderLabel = "Growth Chance Per Living Cell";

        private Player trackedPlayer;
        private List<Player> allPlayers;

        private const string ArrowName = "UI_GrowthPreviewCellArrowImage";
        private const string PercentName = "UI_GrowthPreviewCellPercentText";
        private const string SurgeName = "UI_GrowthPreviewCellSurgeText";
        private const float AdaptationHeaderFontSize = 18f;
        private const float AdaptationHeaderHeight = 24f;
        private const int AdaptationIconSize = 24;
        private const int AdaptationIconMaxColumns = 12;
        private const float AdaptationIconSpacing = 4f;
        private const float AdaptationSectionSpacing = 8f;
        private const string GrowthPreviewRootName = "UI_GrowthPreviewRoot";
        private const string StatsRootName = "UI_StatsRoot";

        private readonly List<GameObject> adaptationIconObjects = new();
        private readonly List<GameObject> mycovariantIconObjects = new();
        private readonly List<GameObject> boardOverlayLegendObjects = new();

        private bool cellsResolved = false;
        private bool deferredRefreshRequested = false;

        private void Awake()
        {
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelPrimary);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 20f);

            zeroChanceColor = new Color(UIStyleTokens.Text.Primary.r, UIStyleTokens.Text.Primary.g, UIStyleTokens.Text.Primary.b, 0.35f);
            finalZeroGreen = UIStyleTokens.State.Success;

            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image == centerPlayerIcon)
                {
                    continue;
                }

                if (image.sprite != null)
                {
                    continue;
                }

                if (IsBrightNeutral(image.color))
                {
                    image.color = UIStyleTokens.Surface.PanelSecondary;
                }
            }

            ApplyGrowthPreviewHeaderText();
            EnsureBoardOverlayLegendSectionExists();
            EnsureAdaptationSectionExists();
            EnsureMycovariantSectionExists();
            UpdateSectionSiblingOrder();
            ApplyBoardOverlayLegendSectionStyle();
            ApplyAdaptationSectionStyle();
            ApplyMycovariantSectionStyle();
            ApplyLayoutBehavior();
        }

        private void ApplyLayoutBehavior()
        {
            if (TryGetComponent<LayoutElement>(out var rootLayout))
            {
                rootLayout.preferredHeight = -1f;
                rootLayout.flexibleHeight = 0f;
            }

            if (TryGetComponent<VerticalLayoutGroup>(out var rootGroup))
            {
                rootGroup.childControlHeight = true;
                rootGroup.childForceExpandHeight = false;
            }

            ApplyChildLayoutBehavior(GrowthPreviewRootName, ignoreIfEmpty: false);
            ApplyChildLayoutBehavior(StatsRootName, ignoreIfEmpty: true);

            if (transform is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        private void ApplyChildLayoutBehavior(string childName, bool ignoreIfEmpty)
        {
            var child = FindDirectChildRect(childName);
            if (child == null)
            {
                return;
            }

            if (child.TryGetComponent<LayoutElement>(out var layout))
            {
                bool shouldIgnoreLayout = ignoreIfEmpty && child.childCount == 0;
                layout.ignoreLayout = shouldIgnoreLayout;
                layout.flexibleHeight = 0f;

                if (shouldIgnoreLayout)
                {
                    layout.minHeight = 0f;
                    layout.preferredHeight = -1f;
                }
            }

            if (child.TryGetComponent<VerticalLayoutGroup>(out var verticalLayout))
            {
                verticalLayout.childForceExpandHeight = false;
            }

            if (child.TryGetComponent<HorizontalLayoutGroup>(out var horizontalLayout))
            {
                horizontalLayout.childForceExpandHeight = false;
            }
        }

        private RectTransform FindDirectChildRect(string childName)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is RectTransform child && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private void ApplyGrowthPreviewHeaderText()
        {
            if (growthPreviewHeaderText == null)
                growthPreviewHeaderText = TryFindGrowthHeaderLabel();

            if (growthPreviewHeaderText == null)
                return;

            growthPreviewHeaderText.text = growthPreviewHeaderLabel;
            growthPreviewHeaderText.color = UIStyleTokens.Text.Primary;
            growthPreviewHeaderText.fontStyle = FontStyles.Bold;
            ApplyTextScale(growthPreviewHeaderText, HeaderTextScale);
        }

        private TextMeshProUGUI TryFindGrowthHeaderLabel()
        {
            var labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label == null) continue;

                string value = label.text;
                if (string.IsNullOrWhiteSpace(value)) continue;

                string normalized = value.ToLowerInvariant();
                if (normalized.Contains("growth") && !normalized.Contains("%"))
                    return label;
            }

            return null;
        }

        private static void ApplyTextScale(TextMeshProUGUI label, float scale)
        {
            if (label == null || scale <= 1f) return;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin *= scale;
                label.fontSizeMax *= scale;
            }
            else
            {
                label.fontSize *= scale;
            }
        }

        private static bool IsBrightNeutral(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float avg = (color.r + color.g + color.b) / 3f;
            bool lowSaturation = (max - min) < 0.10f;
            return color.a > 0.3f && avg > 0.70f && lowSaturation;
        }

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            EnsureCellsResolved();
            Refresh();

            ConfigureCenterPlayerIcon(player);
        }

        public void Refresh()
        {
            if (trackedPlayer == null) return;

            // Skip updates during fast-forward; mark deferred
            if (GameManager.Instance != null && GameManager.Instance.IsFastForwarding)
            {
                deferredRefreshRequested = true;
                return;
            }

            EnsureCellsResolved();
            UpdateGrowthChances();
            RefreshBoardOverlayLegend();
            RefreshAdaptations();
            RefreshMycovariants();
            deferredRefreshRequested = false;
        }

        // Called by GameManager after fast-forward completes
        public void ApplyDeferredRefreshIfNeeded()
        {
            if (deferredRefreshRequested)
            {
                Refresh();
            }
        }

        public void SwitchPlayer(Player player, List<Player> players)
        {
            if (player == null) { Debug.LogError("UI_MoldProfileRoot.SwitchPlayer called with null player"); return; }
            trackedPlayer = player;
            allPlayers = players;

            ConfigureCenterPlayerIcon(player);

            Refresh();
        }

        private void ConfigureCenterPlayerIcon(Player player)
        {
            if (centerPlayerIcon == null)
            {
                return;
            }

            try
            {
                var grid = GameManager.Instance?.gridVisualizer;
                var sprite = grid?.GetTileForPlayer(player.PlayerId)?.sprite;
                centerPlayerIcon.sprite = sprite;
                centerPlayerIcon.enabled = sprite != null;

                var hoverHandler = centerPlayerIcon.GetComponent<PlayerMoldIconHoverHandler>();
                if (hoverHandler == null)
                {
                    hoverHandler = centerPlayerIcon.gameObject.AddComponent<PlayerMoldIconHoverHandler>();
                }

                hoverHandler.playerId = player.PlayerId;
                hoverHandler.gridVisualizer = grid;
                hoverHandler.enabled = sprite != null && grid != null;
                centerPlayerIcon.raycastTarget = hoverHandler.enabled;
            }
            catch
            {
                centerPlayerIcon.enabled = false;

                var hoverHandler = centerPlayerIcon.GetComponent<PlayerMoldIconHoverHandler>();
                if (hoverHandler != null)
                {
                    hoverHandler.enabled = false;
                }
            }
        }

        private void EnsureCellsResolved()
        {
            if (cellsResolved) return;
            if (directionCells == null || directionCells.Length == 0)
                throw new Exception("Growth direction cells not assigned on UI_MoldProfileRoot.");

            foreach (var cell in directionCells)
            {
                if (cell == null)
                    throw new Exception("A GrowthDirectionCell entry is null in UI_MoldProfileRoot.");
                if (cell.parent == null)
                    throw new Exception($"GrowthDirectionCell '{cell.direction}' parent GameObject not assigned.");
                cell.ResolveChildren(ArrowName, PercentName, SurgeName);
            }
            cellsResolved = true;
        }

        private void UpdateGrowthChances()
        {
            if (directionCells == null || directionCells.Length == 0) return;
            foreach (var cell in directionCells)
            {
                if (cell == null) continue;
                GetDirectionalGrowthChance(trackedPlayer, cell.direction, out float baseChance, out float surgeBonus);
                cell.SetChance(baseChance, surgeBonus, zeroChanceColor);
            }
        }

        private void RefreshAdaptations()
        {
            EnsureAdaptationSectionExists();

            var adaptations = new List<AdaptationDefinition>();
            if (trackedPlayer?.PlayerAdaptations != null)
            {
                for (int i = 0; i < trackedPlayer.PlayerAdaptations.Count; i++)
                {
                    adaptations.Add(trackedPlayer.PlayerAdaptations[i].Adaptation);
                }
            }

            RefreshIconSection(adaptationSectionRoot, adaptationIconGridRoot, adaptationIconObjects, adaptations, CreateAdaptationIcon);
        }

        private void RefreshBoardOverlayLegend()
        {
            EnsureBoardOverlayLegendSectionExists();

            var overlayTypes = new[]
            {
                BoardOverlayLegendType.ResistanceShield,
                BoardOverlayLegendType.Toxin,
                BoardOverlayLegendType.DeadCell,
                BoardOverlayLegendType.Chemobeacon
            };

            RefreshIconSection(boardOverlayLegendSectionRoot, boardOverlayLegendIconGridRoot, boardOverlayLegendObjects, overlayTypes, CreateBoardOverlayLegendIcon);
        }

        private void RefreshMycovariants()
        {
            EnsureMycovariantSectionExists();
            RefreshIconSection(mycovariantSectionRoot, mycovariantIconGridRoot, mycovariantIconObjects, trackedPlayer?.PlayerMycovariants, CreateMycovariantIcon);
        }

        private void RefreshIconSection<T>(RectTransform sectionRoot, RectTransform iconGridRoot, List<GameObject> iconObjects, IList<T> entries, Action<T> createIcon)
        {
            if (sectionRoot == null || iconGridRoot == null)
            {
                return;
            }

            ClearIconObjects(iconObjects);

            bool hasEntries = entries != null && entries.Count > 0;
            sectionRoot.gameObject.SetActive(hasEntries);
            if (!hasEntries)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                createIcon(entries[i]);
            }

            UpdateSectionLayoutMetrics(sectionRoot, iconGridRoot, entries.Count);
            LayoutRebuilder.ForceRebuildLayoutImmediate(sectionRoot);

            if (transform is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        private static void ClearIconObjects(List<GameObject> iconObjects)
        {
            for (int i = 0; i < iconObjects.Count; i++)
            {
                if (iconObjects[i] != null)
                {
                    Destroy(iconObjects[i]);
                }
            }

            iconObjects.Clear();
        }

        private void EnsureAdaptationSectionExists()
        {
            if (adaptationSectionRoot != null && adaptationIconGridRoot != null && adaptationHeaderText != null)
            {
                return;
            }

            var rootTransform = transform as RectTransform;
            if (rootTransform == null)
            {
                return;
            }

            adaptationSectionRoot = CreateSectionRoot(rootTransform, "UI_AdaptationSection");

            adaptationHeaderText = CreateSectionHeader(adaptationSectionRoot, "UI_AdaptationHeaderText", adaptationHeaderLabel, adaptationHeaderTooltip);
            adaptationIconGridRoot = CreateIconGrid(adaptationSectionRoot, "UI_AdaptationIconGrid");

            ApplyAdaptationSectionStyle();
        }

        private void EnsureBoardOverlayLegendSectionExists()
        {
            if (boardOverlayLegendSectionRoot != null && boardOverlayLegendIconGridRoot != null && boardOverlayLegendHeaderText != null)
            {
                return;
            }

            var rootTransform = transform as RectTransform;
            if (rootTransform == null)
            {
                return;
            }

            boardOverlayLegendSectionRoot = CreateSectionRoot(rootTransform, "UI_BoardOverlayLegendSection");
            boardOverlayLegendHeaderText = CreateSectionHeader(boardOverlayLegendSectionRoot, "UI_BoardOverlayLegendHeaderText", boardOverlayLegendHeaderLabel, boardOverlayLegendHeaderTooltip);
            boardOverlayLegendIconGridRoot = CreateIconGrid(boardOverlayLegendSectionRoot, "UI_BoardOverlayLegendIconGrid");

            ApplyBoardOverlayLegendSectionStyle();
        }

        private void EnsureMycovariantSectionExists()
        {
            EnsureAdaptationSectionExists();

            if (mycovariantSectionRoot != null && mycovariantIconGridRoot != null && mycovariantHeaderText != null)
            {
                return;
            }

            var rootTransform = transform as RectTransform;
            if (rootTransform == null)
            {
                return;
            }

            mycovariantSectionRoot = CreateSectionRoot(rootTransform, "UI_MycovariantSection");

            mycovariantHeaderText = CreateSectionHeader(mycovariantSectionRoot, "UI_MycovariantHeaderText", mycovariantHeaderLabel, mycovariantHeaderTooltip);
            mycovariantIconGridRoot = CreateIconGrid(mycovariantSectionRoot, "UI_MycovariantIconGrid");

            ApplyMycovariantSectionStyle();
        }

        private void UpdateSectionSiblingOrder()
        {
            var rootTransform = transform as RectTransform;
            if (rootTransform == null)
            {
                return;
            }

            var hostRoot = ResolveAdaptationSectionHostRoot(rootTransform);
            int nextIndex = hostRoot != null ? hostRoot.GetSiblingIndex() + 1 : rootTransform.childCount;

            if (boardOverlayLegendSectionRoot != null)
            {
                boardOverlayLegendSectionRoot.SetSiblingIndex(nextIndex++);
            }

            if (adaptationSectionRoot != null)
            {
                adaptationSectionRoot.SetSiblingIndex(nextIndex++);
            }

            if (mycovariantSectionRoot != null)
            {
                mycovariantSectionRoot.SetSiblingIndex(nextIndex);
            }
        }

        private RectTransform ResolveAdaptationSectionHostRoot(RectTransform fallbackRoot)
        {
            if (adaptationSectionHostRoot != null)
            {
                return adaptationSectionHostRoot;
            }

            for (int i = 0; i < fallbackRoot.childCount; i++)
            {
                if (fallbackRoot.GetChild(i) is RectTransform child && child.name == "UI_GrowthPreviewRoot")
                {
                    adaptationSectionHostRoot = child;
                    break;
                }
            }

            return adaptationSectionHostRoot;
        }

        private void ApplyAdaptationSectionStyle()
        {
            ApplySectionStyle(adaptationSectionRoot, adaptationHeaderText, adaptationIconGridRoot, adaptationHeaderLabel);
        }

        private void ApplyBoardOverlayLegendSectionStyle()
        {
            ApplySectionStyle(boardOverlayLegendSectionRoot, boardOverlayLegendHeaderText, boardOverlayLegendIconGridRoot, boardOverlayLegendHeaderLabel);
        }

        private void ApplyMycovariantSectionStyle()
        {
            ApplySectionStyle(mycovariantSectionRoot, mycovariantHeaderText, mycovariantIconGridRoot, mycovariantHeaderLabel);
        }

        private void ApplySectionStyle(RectTransform sectionRoot, TextMeshProUGUI headerText, RectTransform iconGridRoot, string headerLabel)
        {
            if (sectionRoot == null)
            {
                return;
            }

            UIStyleTokens.ApplyPanelSurface(sectionRoot.gameObject, UIStyleTokens.Surface.PanelSecondary);

            var sectionImage = sectionRoot.GetComponent<Image>();
            if (sectionImage != null)
            {
                sectionImage.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Surface.PanelElevated, 0.35f);
            }

            if (headerText != null)
            {
                headerText.text = headerLabel;
                headerText.color = UIStyleTokens.Text.Primary;
                headerText.fontStyle = FontStyles.Bold;
                headerText.enableAutoSizing = false;
                headerText.fontSize = AdaptationHeaderFontSize;
            }

            UpdateSectionGridConstraint(sectionRoot, iconGridRoot);
        }

        private void UpdateSectionGridConstraint(RectTransform sectionRoot, RectTransform iconGridRoot)
        {
            if (iconGridRoot == null)
            {
                return;
            }

            var grid = iconGridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                return;
            }

            grid.cellSize = new Vector2(AdaptationIconSize, AdaptationIconSize);
            grid.spacing = new Vector2(AdaptationIconSpacing, AdaptationIconSpacing);
            grid.constraintCount = GetSectionColumnCount(sectionRoot, iconGridRoot);
        }

        private int GetSectionColumnCount(RectTransform sectionRoot, RectTransform iconGridRoot)
        {
            float availableWidth = 0f;
            if (iconGridRoot != null)
            {
                availableWidth = iconGridRoot.rect.width;
            }

            if (availableWidth <= 0f && sectionRoot != null)
            {
                availableWidth = Mathf.Max(0f, sectionRoot.rect.width - 20f);
            }

            if (availableWidth <= 0f)
            {
                return AdaptationIconMaxColumns;
            }

            int columns = Mathf.FloorToInt((availableWidth + AdaptationIconSpacing) / (AdaptationIconSize + AdaptationIconSpacing));
            return Mathf.Clamp(columns, 1, AdaptationIconMaxColumns);
        }

        private void CreateAdaptationIcon(AdaptationDefinition adaptation)
        {
            if (adaptationIconGridRoot == null)
            {
                return;
            }

            var iconObject = CreateIconObject($"UI_Adaptation_{adaptation.Id}", adaptationIconGridRoot, adaptationIconObjects, AdaptationArtRepository.GetIcon(adaptation));

            var provider = iconObject.AddComponent<AdaptationTooltipProvider>();
            provider.Initialize(adaptation);

            var trigger = iconObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private void CreateBoardOverlayLegendIcon(BoardOverlayLegendType overlayType)
        {
            if (boardOverlayLegendIconGridRoot == null)
            {
                return;
            }

            var grid = GameManager.Instance?.gridVisualizer;
            var iconObject = CreateIconObject($"UI_BoardOverlayLegend_{overlayType}", boardOverlayLegendIconGridRoot, boardOverlayLegendObjects, GetBoardOverlayLegendSprite(overlayType, grid));

            var provider = iconObject.AddComponent<BoardOverlayLegendTooltipProvider>();
            provider.Initialize(overlayType);

            var hoverHandler = iconObject.AddComponent<BoardOverlayLegendHoverHandler>();
            hoverHandler.Initialize(overlayType, grid);
            hoverHandler.enabled = grid != null;

            var trigger = iconObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private void CreateMycovariantIcon(PlayerMycovariant playerMycovariant)
        {
            if (mycovariantIconGridRoot == null || playerMycovariant?.Mycovariant == null)
            {
                return;
            }

            var definition = playerMycovariant.Mycovariant;
            var iconObject = CreateIconObject($"UI_Mycovariant_{definition.Id}", mycovariantIconGridRoot, mycovariantIconObjects, MycovariantArtRepository.GetIcon(definition));

            var provider = iconObject.AddComponent<MycovariantTooltipProvider>();
            provider.Initialize(definition);

            var trigger = iconObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private static GameObject CreateIconObject(string objectName, RectTransform gridRoot, List<GameObject> iconObjects, Sprite sprite)
        {
            var iconObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            iconObject.transform.SetParent(gridRoot, false);
            iconObjects.Add(iconObject);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(AdaptationIconSize, AdaptationIconSize);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.preferredWidth = AdaptationIconSize;
            layout.preferredHeight = AdaptationIconSize;
            layout.minWidth = AdaptationIconSize;
            layout.minHeight = AdaptationIconSize;

            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            return iconObject;
        }

        private static Sprite GetBoardOverlayLegendSprite(BoardOverlayLegendType overlayType, FungusToast.Unity.Grid.GridVisualizer grid)
        {
            return overlayType switch
            {
                BoardOverlayLegendType.ResistanceShield => grid?.goldShieldOverlayTile?.sprite,
                BoardOverlayLegendType.Toxin => grid?.toxinOverlayTile?.sprite,
                BoardOverlayLegendType.DeadCell => grid?.deadTile?.sprite,
                BoardOverlayLegendType.Chemobeacon => grid?.GetChemobeaconLegendSprite(),
                _ => null
            };
        }

        private static RectTransform CreateSectionRoot(RectTransform parent, string objectName)
        {
            var section = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            section.transform.SetParent(parent, false);

            var rect = section.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var layoutGroup = section.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 8, 10);
            layoutGroup.spacing = AdaptationSectionSpacing;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var fitter = section.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = section.GetComponent<LayoutElement>();
            layout.preferredHeight = -1f;
            layout.minHeight = 0f;
            layout.flexibleHeight = 0f;

            return rect;
        }

        private static TextMeshProUGUI CreateSectionHeader(RectTransform parent, string objectName, string label, string tooltipText)
        {
            var headerObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(parent, false);

            var layout = headerObject.GetComponent<LayoutElement>();
            layout.preferredHeight = AdaptationHeaderHeight;

            var text = headerObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = AdaptationHeaderFontSize;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Left;

            if (!string.IsNullOrWhiteSpace(tooltipText))
            {
                var trigger = headerObject.AddComponent<TooltipTrigger>();
                trigger.SetStaticText(tooltipText);
                trigger.SetAutoPlacementOffsetX(18f);
            }

            return text;
        }

        private static RectTransform CreateIconGrid(RectTransform parent, string objectName)
        {
            var gridObject = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(parent, false);

            var rect = gridObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(AdaptationIconSize, AdaptationIconSize);
            grid.spacing = new Vector2(AdaptationIconSpacing, AdaptationIconSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = AdaptationIconMaxColumns;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            var layout = gridObject.GetComponent<LayoutElement>();
            layout.preferredHeight = AdaptationIconSize;
            layout.flexibleHeight = 0f;

            return rect;
        }

        private void UpdateSectionLayoutMetrics(RectTransform sectionRoot, RectTransform iconGridRoot, int iconCount)
        {
            if (iconGridRoot == null || sectionRoot == null)
            {
                return;
            }

            UpdateSectionGridConstraint(sectionRoot, iconGridRoot);

            int columns = GetSectionColumnCount(sectionRoot, iconGridRoot);
            int rows = Mathf.Max(1, Mathf.CeilToInt(iconCount / (float)columns));
            float gridHeight = (rows * AdaptationIconSize) + ((rows - 1) * AdaptationIconSpacing);

            var gridLayout = iconGridRoot.GetComponent<LayoutElement>();
            if (gridLayout != null)
            {
                gridLayout.preferredHeight = gridHeight;
                gridLayout.minHeight = gridHeight;
            }

            var sectionLayout = sectionRoot.GetComponent<LayoutElement>();
            if (sectionLayout != null)
            {
                sectionLayout.preferredHeight = 8f + AdaptationHeaderHeight + AdaptationSectionSpacing + gridHeight + 10f;
                sectionLayout.minHeight = sectionLayout.preferredHeight;
            }
        }

        private static bool IsCardinal(GrowthPreviewDirection dir)
            => dir == GrowthPreviewDirection.North || dir == GrowthPreviewDirection.East || dir == GrowthPreviewDirection.South || dir == GrowthPreviewDirection.West;

        private static float GetDiagonalBaseEffect(Player player, GrowthPreviewDirection dir)
        {
            return dir switch
            {
                GrowthPreviewDirection.Northwest => player.GetMutationEffect(MutationType.GrowthDiagonal_NW),
                GrowthPreviewDirection.Northeast => player.GetMutationEffect(MutationType.GrowthDiagonal_NE),
                GrowthPreviewDirection.Southeast => player.GetMutationEffect(MutationType.GrowthDiagonal_SE),
                GrowthPreviewDirection.Southwest => player.GetMutationEffect(MutationType.GrowthDiagonal_SW),
                _ => 0f
            };
        }

        private void GetDirectionalGrowthChance(Player player, GrowthPreviewDirection dir, out float baseChance, out float surgeBonus)
        {
            if (IsCardinal(dir))
            {
                (float orthBase, float surge) = GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(player);
                baseChance = orthBase;
                surgeBonus = surge;
            }
            else
            {
                float raw = GetDiagonalBaseEffect(player, dir);
                float multiplier = GrowthMutationProcessor.GetTendrilDiagonalGrowthMultiplier(player);
                baseChance = raw * multiplier;
                surgeBonus = 0f;
            }
        }
    }
}
