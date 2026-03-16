using FungusToast.Core; // for MutationType enum
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations; // keep if other mutation id helpers needed
using FungusToast.Core.Phases; // GrowthMutationProcessor lives here
using FungusToast.Core.Players;
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
        private const int AdaptationIconMaxColumns = 7;
        private const float AdaptationIconSpacing = 4f;
        private const float AdaptationSectionSpacing = 8f;
        private const string GrowthPreviewRootName = "UI_GrowthPreviewRoot";
        private const string StatsRootName = "UI_StatsRoot";

        private readonly List<GameObject> adaptationIconObjects = new();

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
            EnsureAdaptationSectionExists();
            ApplyAdaptationSectionStyle();
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

            // Center Player Icon
            if (centerPlayerIcon != null)
            {
                try
                {
                    var sprite = GameManager.Instance?.gridVisualizer?.GetTileForPlayer(player.PlayerId)?.sprite;
                    if (sprite != null) { centerPlayerIcon.sprite = sprite; centerPlayerIcon.enabled = true; }
                    else centerPlayerIcon.enabled = false;
                }
                catch { centerPlayerIcon.enabled = false; }
            }
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
            RefreshAdaptations();
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

            // Center Player Icon
            if (centerPlayerIcon != null)
            {
                try
                {
                    var sprite = GameManager.Instance?.gridVisualizer?.GetTileForPlayer(player.PlayerId)?.sprite;
                    if (sprite != null) { centerPlayerIcon.sprite = sprite; centerPlayerIcon.enabled = true; }
                    else centerPlayerIcon.enabled = false;
                }
                catch { centerPlayerIcon.enabled = false; }
            }

            Refresh();
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

            if (adaptationSectionRoot == null || adaptationIconGridRoot == null)
            {
                return;
            }

            ClearAdaptationIcons();

            bool hasAdaptations = trackedPlayer != null
                && trackedPlayer.PlayerAdaptations != null
                && trackedPlayer.PlayerAdaptations.Count > 0;

            adaptationSectionRoot.gameObject.SetActive(hasAdaptations);
            if (!hasAdaptations)
            {
                return;
            }

            for (int i = 0; i < trackedPlayer.PlayerAdaptations.Count; i++)
            {
                CreateAdaptationIcon(trackedPlayer.PlayerAdaptations[i].Adaptation);
            }

            UpdateAdaptationLayoutMetrics(trackedPlayer.PlayerAdaptations.Count);
            LayoutRebuilder.ForceRebuildLayoutImmediate(adaptationSectionRoot);

            if (transform is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        private void ClearAdaptationIcons()
        {
            for (int i = 0; i < adaptationIconObjects.Count; i++)
            {
                if (adaptationIconObjects[i] != null)
                {
                    Destroy(adaptationIconObjects[i]);
                }
            }

            adaptationIconObjects.Clear();
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

            var hostRoot = ResolveAdaptationSectionHostRoot(rootTransform);
            adaptationSectionRoot = CreateSectionRoot(rootTransform);
            if (hostRoot != null)
            {
                adaptationSectionRoot.SetSiblingIndex(hostRoot.GetSiblingIndex() + 1);
            }

            adaptationHeaderText = CreateSectionHeader(adaptationSectionRoot, adaptationHeaderLabel);
            adaptationIconGridRoot = CreateIconGrid(adaptationSectionRoot);

            ApplyAdaptationSectionStyle();
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
            if (adaptationSectionRoot == null)
            {
                return;
            }

            UIStyleTokens.ApplyPanelSurface(adaptationSectionRoot.gameObject, UIStyleTokens.Surface.PanelSecondary);

            var sectionImage = adaptationSectionRoot.GetComponent<Image>();
            if (sectionImage != null)
            {
                sectionImage.color = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Surface.PanelElevated, 0.35f);
            }

            if (adaptationHeaderText != null)
            {
                adaptationHeaderText.text = adaptationHeaderLabel;
                adaptationHeaderText.color = UIStyleTokens.Text.Primary;
                adaptationHeaderText.fontStyle = FontStyles.Bold;
                adaptationHeaderText.enableAutoSizing = false;
                adaptationHeaderText.fontSize = AdaptationHeaderFontSize;
            }

            UpdateAdaptationGridConstraint();
        }

        private void UpdateAdaptationGridConstraint()
        {
            if (adaptationIconGridRoot == null)
            {
                return;
            }

            var grid = adaptationIconGridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                return;
            }

            grid.cellSize = new Vector2(AdaptationIconSize, AdaptationIconSize);
            grid.spacing = new Vector2(AdaptationIconSpacing, AdaptationIconSpacing);
            grid.constraintCount = GetAdaptationColumnCount();
        }

        private int GetAdaptationColumnCount()
        {
            float availableWidth = 0f;
            if (adaptationIconGridRoot != null)
            {
                availableWidth = adaptationIconGridRoot.rect.width;
            }

            if (availableWidth <= 0f && adaptationSectionRoot != null)
            {
                availableWidth = Mathf.Max(0f, adaptationSectionRoot.rect.width - 20f);
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

            var iconObject = new GameObject($"UI_Adaptation_{adaptation.Id}", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            iconObject.transform.SetParent(adaptationIconGridRoot, false);
            adaptationIconObjects.Add(iconObject);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(AdaptationIconSize, AdaptationIconSize);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.preferredWidth = AdaptationIconSize;
            layout.preferredHeight = AdaptationIconSize;
            layout.minWidth = AdaptationIconSize;
            layout.minHeight = AdaptationIconSize;

            var image = iconObject.GetComponent<Image>();
            image.sprite = AdaptationArtRepository.GetIcon(adaptation);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            var provider = iconObject.AddComponent<AdaptationTooltipProvider>();
            provider.Initialize(adaptation);

            var trigger = iconObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private static RectTransform CreateSectionRoot(RectTransform parent)
        {
            var section = new GameObject("UI_AdaptationSection", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
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

        private static TextMeshProUGUI CreateSectionHeader(RectTransform parent, string label)
        {
            var headerObject = new GameObject("UI_AdaptationHeaderText", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(parent, false);

            var layout = headerObject.GetComponent<LayoutElement>();
            layout.preferredHeight = AdaptationHeaderHeight;

            var text = headerObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = AdaptationHeaderFontSize;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Left;

            return text;
        }

        private static RectTransform CreateIconGrid(RectTransform parent)
        {
            var gridObject = new GameObject("UI_AdaptationIconGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
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

        private void UpdateAdaptationLayoutMetrics(int iconCount)
        {
            if (adaptationIconGridRoot == null || adaptationSectionRoot == null)
            {
                return;
            }

            UpdateAdaptationGridConstraint();

            int columns = GetAdaptationColumnCount();
            int rows = Mathf.Max(1, Mathf.CeilToInt(iconCount / (float)columns));
            float gridHeight = (rows * AdaptationIconSize) + ((rows - 1) * AdaptationIconSpacing);

            var gridLayout = adaptationIconGridRoot.GetComponent<LayoutElement>();
            if (gridLayout != null)
            {
                gridLayout.preferredHeight = gridHeight;
                gridLayout.minHeight = gridHeight;
            }

            var sectionLayout = adaptationSectionRoot.GetComponent<LayoutElement>();
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
