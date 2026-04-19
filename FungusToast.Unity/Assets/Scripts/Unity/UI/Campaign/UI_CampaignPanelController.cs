using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Campaign;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Testing;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel with a deterministic two-column layout:
    /// primary menu content on the left and development testing controls on the right.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
        private const float PrimaryColumnWidth = 500f;
        private const float DevelopmentRailWidth = 340f;
        private const float LayoutShellWidth = 500f;
        private const float DevelopmentRailOffsetX = 440f;
        private const float DevelopmentRailTopOffsetY = -18f;

        private static readonly string[] MoldDisplayNames =
        {
            "Mycelavis",
            "Sporalunea",
            "Cineramyxa",
            "Velutora",
            "Glaucoryza",
            "Viridomyxa",
            "Noctephyra",
            "Aureomycella"
        };

        private enum CampaignPanelStep
        {
            MainActions,
            MoldSelection
        }

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private GameObject modeSelectPanel;

        [Header("Legacy Testing References")]
        [SerializeField] private GameObject testingOptionsSectionRoot;
        [SerializeField] private Toggle testingModeToggleTemplate;
        [SerializeField] private GameObject testingModePanelTemplate;

        private RectTransform contentRoot;
        private RectTransform layoutShellRoot;
        private RectTransform mainStackRoot;
        private RectTransform developmentTestingRailRoot;
        private GameObject actionStack;
        private DevelopmentTestingCardController testingCardController;
        private RectTransform moldinessSummarySectionRoot;
        private TextMeshProUGUI moldinessSummaryTitleLabel;
        private TextMeshProUGUI moldinessSummaryStatusLabel;
        private TextMeshProUGUI moldinessSummaryPendingLabel;
        private TextMeshProUGUI permanentUpgradesLabel;
        private GridLayoutGroup moldinessSummaryToastGrid;
        private readonly List<Image> moldinessSummaryToastTiles = new();
        private System.Random moldinessSummaryToastRandom;
        private RectTransform moldSelectionSectionRoot;
        private TextMeshProUGUI moldSelectionTitleLabel;
        private TextMeshProUGUI moldSelectionStatusLabel;
        private GridLayoutGroup moldSelectionGrid;
        private readonly List<Button> moldSelectionButtons = new();
        private readonly List<Image> moldSelectionHighlights = new();
        private readonly List<Image> moldSelectionIcons = new();
        private readonly List<TextMeshProUGUI> moldSelectionLabels = new();
        private CampaignPanelStep currentStep = CampaignPanelStep.MainActions;
        private int? selectedCampaignMoldIndex;

        private void Awake()
        {
            contentRoot = transform.Find("UI_CampaignContent") as RectTransform;
            if (contentRoot == null)
            {
                Debug.LogWarning("UI_CampaignPanelController: UI_CampaignContent not found.");
                return;
            }

            BuildLayoutScaffold();
            BuildTestingCard();
            BuildMoldinessSummarySection();
            BuildMoldSelectionSection();
            BuildActionStack();
            ApplyStyle();
            UpdateStepState();

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (newButton != null) newButton.onClick.AddListener(OnNewClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            currentStep = CampaignPanelStep.MainActions;
            RefreshButtonStates();
            testingCardController?.RefreshDropdownOptions();
            testingCardController?.RefreshVisualState();
            UpdateStepState();
            ForceLayoutNow();
        }

        private void BuildLayoutScaffold()
        {
            if (contentRoot == null)
            {
                return;
            }

            layoutShellRoot = FindNamedRectTransform("UI_CampaignLayoutShell");
            if (layoutShellRoot == null)
            {
                var shell = new GameObject("UI_CampaignLayoutShell", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                shell.transform.SetParent(contentRoot, false);
                layoutShellRoot = shell.GetComponent<RectTransform>();
            }

            layoutShellRoot.SetParent(contentRoot, false);
            ConfigureLayoutShellRoot(layoutShellRoot);

            var existing = FindNamedRectTransform("UI_CampaignMainStack");
            if (existing != null)
            {
                mainStackRoot = existing;
            }
            else
            {
                var root = new GameObject("UI_CampaignMainStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                root.transform.SetParent(contentRoot, false);
                mainStackRoot = root.GetComponent<RectTransform>();
            }

            mainStackRoot.SetParent(contentRoot, false);
            ConfigureMainStackRoot(mainStackRoot);

            developmentTestingRailRoot = FindNamedRectTransform("UI_CampaignDevelopmentTestingRail");
            if (developmentTestingRailRoot == null)
            {
                var rail = new GameObject("UI_CampaignDevelopmentTestingRail", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                rail.transform.SetParent(layoutShellRoot, false);
                developmentTestingRailRoot = rail.GetComponent<RectTransform>();
            }

            developmentTestingRailRoot.SetParent(layoutShellRoot, false);
            developmentTestingRailRoot.SetSiblingIndex(1);
            ConfigureDevelopmentTestingRailRoot(developmentTestingRailRoot);

            if (testingOptionsSectionRoot != null)
            {
                testingOptionsSectionRoot.SetActive(false);
            }

            HideLegacyTestingBlocks();

            if (testingModeToggleTemplate != null)
            {
                testingModeToggleTemplate.gameObject.SetActive(false);
            }

            if (testingModePanelTemplate != null)
            {
                testingModePanelTemplate.SetActive(false);
            }
        }

        private void HideLegacyTestingBlocks()
        {
            if (contentRoot == null)
            {
                return;
            }

            var all = contentRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                {
                    continue;
                }

                if (layoutShellRoot != null && t.IsChildOf(layoutShellRoot))
                {
                    continue;
                }

                var name = t.name;
                bool legacyTestingName =
                    name.IndexOf("TestingOptionsSection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("CampaignTestingOptionsSection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("TestingModePanel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("TestingModeToggle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("FastForwardRounds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("SkipToEnd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ForcedGameResult", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!legacyTestingName)
                {
                    continue;
                }

                // Keep modern runtime controls enabled.
                if (name.IndexOf("UI_Campaign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                t.gameObject.SetActive(false);
            }
        }

        private void BuildTestingCard()
        {
            testingCardController = new DevelopmentTestingCardController(new DevelopmentTestingCardOptions
            {
                Parent = developmentTestingRailRoot,
                ButtonTemplate = backButton != null ? backButton : resumeButton,
                DropdownTemplate = FindDropdownTemplate(),
                SupportsForcedAdaptation = true,
                CardName = "UI_CampaignTestingCard",
                ControlPrefix = "UI_CampaignTesting",
                LogPrefix = "UI_CampaignPanelController",
                LayoutInvalidated = ForceLayoutNow,
                CardWidth = DevelopmentRailWidth,
                SettingWidth = DevelopmentRailWidth - 24f
            });
            testingCardController.Build();
        }

        private RectTransform FindNamedRectTransform(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var children = GetComponentsInChildren<RectTransform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                var child = children[index];
                if (child != null && string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void ConfigureLayoutShellRoot(RectTransform shellRoot)
        {
            if (shellRoot == null)
            {
                return;
            }

            shellRoot.anchorMin = new Vector2(0.5f, 1f);
            shellRoot.anchorMax = new Vector2(0.5f, 1f);
            shellRoot.pivot = new Vector2(0.5f, 1f);
            shellRoot.anchoredPosition = new Vector2(0f, -98f);
            shellRoot.sizeDelta = new Vector2(LayoutShellWidth, 0f);
            shellRoot.localScale = Vector3.one;

            var layout = shellRoot.GetComponent<HorizontalLayoutGroup>();
            layout.enabled = false;

            var fitter = shellRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var element = shellRoot.GetComponent<LayoutElement>();
            element.ignoreLayout = true;
            element.minWidth = PrimaryColumnWidth;
            element.preferredWidth = PrimaryColumnWidth;
            element.minHeight = 0f;
            element.preferredHeight = -1f;
        }

        private static void ConfigureMainStackRoot(RectTransform stackRoot)
        {
            if (stackRoot == null)
            {
                return;
            }

            stackRoot.anchorMin = new Vector2(0.5f, 1f);
            stackRoot.anchorMax = new Vector2(0.5f, 1f);
            stackRoot.pivot = new Vector2(0.5f, 1f);
            stackRoot.anchoredPosition = new Vector2(0f, -98f);
            stackRoot.sizeDelta = new Vector2(PrimaryColumnWidth, 0f);
            stackRoot.localScale = Vector3.one;

            var rootLayout = stackRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 14f;
            rootLayout.padding = new RectOffset(0, 0, 0, 0);

            var rootFitter = stackRoot.GetComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rootElement = stackRoot.GetComponent<LayoutElement>();
            rootElement.minWidth = PrimaryColumnWidth - 40f;
            rootElement.preferredWidth = PrimaryColumnWidth;
            rootElement.minHeight = 200f;
            rootElement.preferredHeight = -1f;
        }

        private static void ConfigureDevelopmentTestingRailRoot(RectTransform railRoot)
        {
            if (railRoot == null)
            {
                return;
            }

            railRoot.anchorMin = new Vector2(0.5f, 1f);
            railRoot.anchorMax = new Vector2(0.5f, 1f);
            railRoot.pivot = new Vector2(0.5f, 1f);
            railRoot.anchoredPosition = new Vector2(DevelopmentRailOffsetX, DevelopmentRailTopOffsetY);
            railRoot.sizeDelta = new Vector2(DevelopmentRailWidth, 0f);
            railRoot.localScale = Vector3.one;

            var layout = railRoot.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = railRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = railRoot.GetComponent<LayoutElement>();
            element.minWidth = DevelopmentRailWidth;
            element.preferredWidth = DevelopmentRailWidth;
            element.minHeight = 44f;
            element.preferredHeight = -1f;
        }

        private static bool ShouldShowDevelopmentTestingUi()
        {
            return true;
        }

        private void BuildMoldinessSummarySection()
        {
            if (mainStackRoot == null)
            {
                return;
            }

            var existing = mainStackRoot.Find("UI_CampaignMoldinessSummarySection") as RectTransform;
            if (existing != null)
            {
                moldinessSummarySectionRoot = existing;
            }
            else
            {
                var section = new GameObject(
                    "UI_CampaignMoldinessSummarySection",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(LayoutElement));
                section.transform.SetParent(mainStackRoot, false);
                moldinessSummarySectionRoot = section.GetComponent<RectTransform>();
            }

            ConfigureMoldinessSummarySection();
            EnsureMoldinessSummaryHeader();
            EnsureMoldinessSummaryToastGrid();
        }

        private void ConfigureMoldinessSummarySection()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            moldinessSummarySectionRoot.anchorMin = new Vector2(0.5f, 1f);
            moldinessSummarySectionRoot.anchorMax = new Vector2(0.5f, 1f);
            moldinessSummarySectionRoot.pivot = new Vector2(0.5f, 0.5f);
            moldinessSummarySectionRoot.anchoredPosition = Vector2.zero;
            moldinessSummarySectionRoot.localScale = Vector3.one;

            var surface = moldinessSummarySectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = UIStyleTokens.Surface.PanelPrimary;
            }

            var layout = moldinessSummarySectionRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = moldinessSummarySectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldinessSummarySectionRoot.GetComponent<LayoutElement>();
            element.minWidth = 460f;
            element.preferredWidth = 500f;
            element.minHeight = 190f;
            element.preferredHeight = -1f;
        }

        private void EnsureMoldinessSummaryHeader()
        {
            moldinessSummaryTitleLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryTitle",
                28f,
                FontStyles.Bold,
                UIStyleTokens.Text.Primary,
                38f);
            moldinessSummaryStatusLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryStatus",
                20f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                48f);
            moldinessSummaryPendingLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryPending",
                18f,
                FontStyles.Italic,
                UIStyleTokens.State.Warning,
                30f);
            permanentUpgradesLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignPermanentUpgrades",
                18f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                30f);
        }

        private TextMeshProUGUI CreateMoldinessSummaryText(string objectName, float fontSize, FontStyles fontStyle, Color color, float minHeight)
        {
            var existing = moldinessSummarySectionRoot.Find(objectName) as RectTransform;
            TextMeshProUGUI label;
            if (existing != null)
            {
                label = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(moldinessSummarySectionRoot, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = string.Empty;

            var element = label.GetComponent<LayoutElement>();
            element.minWidth = 460f;
            element.preferredWidth = 460f;
            element.minHeight = minHeight;
            element.preferredHeight = -1f;

            return label;
        }

        private void EnsureMoldinessSummaryToastGrid()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            if (moldinessSummaryToastGrid == null)
            {
                var existing = moldinessSummarySectionRoot.Find("UI_CampaignMoldinessSummaryToastGrid") as RectTransform;
                if (existing != null)
                {
                    moldinessSummaryToastGrid = existing.GetComponent<GridLayoutGroup>();
                }
                else
                {
                    var gridObject = new GameObject(
                        "UI_CampaignMoldinessSummaryToastGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    gridObject.transform.SetParent(moldinessSummarySectionRoot, false);
                    moldinessSummaryToastGrid = gridObject.GetComponent<GridLayoutGroup>();
                }
            }

            var fitter = moldinessSummaryToastGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldinessSummaryToastGrid.GetComponent<LayoutElement>();
            element.minWidth = 200f;
            element.preferredWidth = 240f;
            element.minHeight = 92f;
            element.preferredHeight = -1f;
        }

        private void EnsureMoldinessSummaryToastTileCount(int requiredCount)
        {
            while (moldinessSummaryToastTiles.Count < requiredCount)
            {
                var tileObject = new GameObject(
                    $"UI_CampaignMoldinessToastTile_{moldinessSummaryToastTiles.Count + 1}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                tileObject.transform.SetParent(moldinessSummaryToastGrid.transform, false);

                var tileImage = tileObject.GetComponent<Image>();
                tileImage.raycastTarget = false;
                tileImage.color = UIStyleTokens.Surface.PanelSecondary;

                var tileElement = tileObject.GetComponent<LayoutElement>();
                tileElement.minWidth = 42f;
                tileElement.preferredWidth = 42f;
                tileElement.minHeight = 42f;
                tileElement.preferredHeight = 42f;

                moldinessSummaryToastTiles.Add(tileImage);
            }
        }

        private void RefreshMoldinessSummaryUi()
        {
            var gameManager = GameManager.Instance;
            bool hasSave = gameManager != null && gameManager.HasCampaignSave();
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            moldinessSummarySectionRoot.gameObject.SetActive(hasSave && currentStep == CampaignPanelStep.MainActions);
            if (!hasSave)
            {
                return;
            }

            var campaignController = gameManager?.CampaignController;
            if (campaignController?.State == null && FungusToast.Unity.Campaign.CampaignSaveService.Exists())
            {
                campaignController?.Resume();
            }

            if (campaignController?.State == null)
            {
                return;
            }

            MoldinessProgressSnapshot snapshot = campaignController.MoldinessProgress;
            int level = snapshot.CurrentTierIndex + 1;
            int threshold = Math.Max(1, snapshot.CurrentThreshold);
            int progress = Mathf.Clamp(snapshot.CurrentProgress, 0, threshold);
            ConfigureMoldinessSummaryToastGrid(threshold);
            EnsureMoldinessSummaryToastTileCount(threshold);
            int filledTileCount = Mathf.Clamp(progress, 0, moldinessSummaryToastTiles.Count);

            if (moldinessSummaryTitleLabel != null)
            {
                moldinessSummaryTitleLabel.text = $"Moldiness Level {level}";
            }

            if (moldinessSummaryStatusLabel != null)
            {
                moldinessSummaryStatusLabel.text = $"{progress} / {threshold} to next threshold  •  Lifetime earned: {snapshot.LifetimeEarned}";
            }

            if (moldinessSummaryPendingLabel != null)
            {
                int pendingCount = snapshot.PendingUnlockCount;
                bool hasPendingSporePreservationMessage = campaignController.IsAwaitingDefeatCarryoverSelection;
                var pendingMessages = new List<string>();
                if (hasPendingSporePreservationMessage)
                {
                    pendingMessages.Add("Spore preservation is pending from your last failed campaign. Resolve it before starting a new run.");
                }

                if (pendingCount > 0)
                {
                    pendingMessages.Add($"{pendingCount} pending moldiness reward{(pendingCount == 1 ? string.Empty : "s")}");
                }

                moldinessSummaryPendingLabel.gameObject.SetActive(pendingMessages.Count > 0);
                moldinessSummaryPendingLabel.text = string.Join("\n", pendingMessages);
            }

            if (permanentUpgradesLabel != null)
            {
                var unlockedPermanentUpgrades = campaignController.State?.moldiness?.unlockedRewardIds != null
                    ? campaignController.State.moldiness.unlockedRewardIds
                        .Select(id => MoldinessUnlockCatalog.TryGetById(id, out var definition) ? definition : null)
                        .Where(definition => definition != null && definition.Type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover)
                        .ToList()
                    : new List<MoldinessUnlockDefinition>();

                permanentUpgradesLabel.gameObject.SetActive(unlockedPermanentUpgrades.Count > 0);
                permanentUpgradesLabel.text = unlockedPermanentUpgrades.Count > 0
                    ? $"Permanent Campaign Upgrades: {string.Join(", ", unlockedPermanentUpgrades.Select(definition => definition.DisplayName))}"
                    : string.Empty;
            }

            ApplyMoldinessSummaryToastPattern(progress, threshold, filledTileCount);

            bool pendingReward = snapshot.PendingUnlockCount > 0;
            bool pendingSporePreservation = campaignController.IsAwaitingDefeatCarryoverSelection;
            if (resumeButton != null)
            {
                SetButtonText(
                    resumeButton,
                    pendingSporePreservation
                        ? "Resume Campaign (Pending Spore Preservation)"
                        : (pendingReward ? "Resume Campaign (Pending Reward)" : "Resume Campaign"));
            }
        }

        private void ConfigureMoldinessSummaryToastGrid(int threshold)
        {
            if (moldinessSummaryToastGrid == null)
            {
                return;
            }

            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(threshold)), 3, 8);
            int rows = Mathf.Max(1, Mathf.CeilToInt(threshold / (float)columns));
            float maxGridWidth = 220f;
            float maxGridHeight = 112f;
            float spacing = threshold <= 12 ? 6f : 4f;
            float cellWidth = Mathf.Clamp((maxGridWidth - ((columns - 1) * spacing)) / columns, 14f, 34f);
            float cellHeight = Mathf.Clamp((maxGridHeight - ((rows - 1) * spacing)) / rows, 14f, 34f);

            moldinessSummaryToastGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            moldinessSummaryToastGrid.constraintCount = columns;
            moldinessSummaryToastGrid.cellSize = new Vector2(cellWidth, cellHeight);
            moldinessSummaryToastGrid.spacing = new Vector2(spacing, spacing);
            moldinessSummaryToastGrid.childAlignment = TextAnchor.UpperCenter;

            var element = moldinessSummaryToastGrid.GetComponent<LayoutElement>();
            element.minWidth = maxGridWidth;
            element.preferredWidth = maxGridWidth;
            element.minHeight = Mathf.Max(56f, (rows * cellHeight) + ((rows - 1) * spacing));
            element.preferredHeight = -1f;
        }

        private void ApplyMoldinessSummaryToastPattern(int progress, int threshold, int filledTileCount)
        {
            if (moldinessSummaryToastTiles.Count == 0)
            {
                return;
            }

            moldinessSummaryToastRandom ??= new System.Random(1337);
            var orderedIndices = Enumerable.Range(0, threshold)
                .OrderBy(index => GetToastTileSortKey(index, threshold))
                .ThenBy(index => index)
                .ToList();
            var filledIndices = new HashSet<int>(orderedIndices.Take(filledTileCount));

            for (int i = 0; i < moldinessSummaryToastTiles.Count; i++)
            {
                var tile = moldinessSummaryToastTiles[i];
                if (tile == null)
                {
                    continue;
                }

                bool shouldShow = i < threshold;
                tile.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                tile.color = filledIndices.Contains(i) ? UIStyleTokens.Accent.Lichen : UIStyleTokens.Surface.PanelSecondary;
            }
        }

        private static float GetToastTileSortKey(int index, int threshold)
        {
            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(threshold)), 3, 8);
            int row = index / columns;
            int column = index % columns;
            float centerColumn = (columns - 1) * 0.5f;
            float columnDistance = Mathf.Abs(column - centerColumn);
            return (row * 10f) + columnDistance + ((index * 37) % 11) * 0.01f;
        }

        private void BuildMoldSelectionSection()
        {
            if (mainStackRoot == null)
            {
                return;
            }

            var existing = mainStackRoot.Find("UI_CampaignMoldSelectionSection") as RectTransform;
            if (existing != null)
            {
                moldSelectionSectionRoot = existing;
            }
            else
            {
                var section = new GameObject(
                    "UI_CampaignMoldSelectionSection",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(LayoutElement));
                section.transform.SetParent(mainStackRoot, false);
                moldSelectionSectionRoot = section.GetComponent<RectTransform>();
            }

            ConfigureMoldSelectionSection();
            EnsureMoldSelectionHeader();
            EnsureMoldSelectionGrid();
        }

        private void ConfigureMoldSelectionSection()
        {
            if (moldSelectionSectionRoot == null)
            {
                return;
            }

            moldSelectionSectionRoot.anchorMin = new Vector2(0.5f, 1f);
            moldSelectionSectionRoot.anchorMax = new Vector2(0.5f, 1f);
            moldSelectionSectionRoot.pivot = new Vector2(0.5f, 0.5f);
            moldSelectionSectionRoot.anchoredPosition = Vector2.zero;
            moldSelectionSectionRoot.localScale = Vector3.one;

            var surface = moldSelectionSectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = UIStyleTokens.Surface.PanelPrimary;
            }

            var layout = moldSelectionSectionRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = moldSelectionSectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldSelectionSectionRoot.GetComponent<LayoutElement>();
            element.minWidth = 460f;
            element.preferredWidth = 500f;
            element.minHeight = 220f;
            element.preferredHeight = -1f;
        }

        private void EnsureMoldSelectionHeader()
        {
            moldSelectionTitleLabel ??= CreateMoldSelectionText(
                "UI_CampaignMoldSelectionTitle",
                28f,
                FontStyles.Bold,
                UIStyleTokens.Text.Primary,
                40f);
            moldSelectionStatusLabel ??= CreateMoldSelectionText(
                "UI_CampaignMoldSelectionStatus",
                20f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                56f);
        }

        private TextMeshProUGUI CreateMoldSelectionText(string objectName, float fontSize, FontStyles fontStyle, Color color, float minHeight)
        {
            var existing = moldSelectionSectionRoot.Find(objectName) as RectTransform;
            TextMeshProUGUI label;
            if (existing != null)
            {
                label = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(moldSelectionSectionRoot, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = string.Empty;

            var element = label.GetComponent<LayoutElement>();
            element.minWidth = 460f;
            element.preferredWidth = 460f;
            element.minHeight = minHeight;
            element.preferredHeight = -1f;

            return label;
        }

        private void EnsureMoldSelectionGrid()
        {
            if (moldSelectionSectionRoot == null)
            {
                return;
            }

            if (moldSelectionGrid == null)
            {
                var existing = moldSelectionSectionRoot.Find("UI_CampaignMoldSelectionGrid") as RectTransform;
                if (existing != null)
                {
                    moldSelectionGrid = existing.GetComponent<GridLayoutGroup>();
                }
                else
                {
                    var gridObject = new GameObject(
                        "UI_CampaignMoldSelectionGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    gridObject.transform.SetParent(moldSelectionSectionRoot, false);
                    moldSelectionGrid = gridObject.GetComponent<GridLayoutGroup>();
                }
            }

            moldSelectionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            moldSelectionGrid.constraintCount = 4;
            moldSelectionGrid.cellSize = new Vector2(110f, 110f);
            moldSelectionGrid.spacing = new Vector2(10f, 10f);
            moldSelectionGrid.childAlignment = TextAnchor.UpperCenter;

            var fitter = moldSelectionGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldSelectionGrid.GetComponent<LayoutElement>();
            element.minWidth = 470f;
            element.preferredWidth = 470f;
            element.minHeight = 180f;
            element.preferredHeight = -1f;
        }

        private void BuildActionStack()
        {
            var existing = mainStackRoot.Find("UI_CampaignActionStack");
            if (existing != null)
            {
                actionStack = existing.gameObject;
            }
            else
            {
                actionStack = new GameObject("UI_CampaignActionStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                actionStack.transform.SetParent(mainStackRoot, false);
            }

            var actionLayout = actionStack.GetComponent<VerticalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.UpperCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            actionLayout.spacing = 14f;
            actionLayout.padding = new RectOffset(0, 0, 0, 0);

            var actionFitter = actionStack.GetComponent<ContentSizeFitter>();
            actionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            actionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var actionElement = actionStack.GetComponent<LayoutElement>();
            actionElement.minWidth = 460f;
            actionElement.preferredWidth = 500f;

            ReparentActionButton(resumeButton, 0);
            ReparentActionButton(newButton, 1);
            ReparentActionButton(deleteButton, 2);
            ReparentActionButton(backButton, 3);

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(false);
            }
        }

        private void ReparentActionButton(Button button, int index)
        {
            if (button == null || actionStack == null)
            {
                return;
            }

            button.transform.SetParent(actionStack.transform, false);
            button.transform.SetSiblingIndex(index);
            EnsureButtonLayout(button, button == backButton ? 330f : 500f);
        }

        private static void EnsureButtonLayout(Button button, float width)
        {
            if (button == null)
            {
                return;
            }

            var element = button.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = button.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = 52f;
            element.preferredHeight = 56f;
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        private TMP_Dropdown FindDropdownTemplate()
        {
            if (testingOptionsSectionRoot != null)
            {
                var fromLegacy = testingOptionsSectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
                if (fromLegacy != null)
                {
                    return fromLegacy;
                }
            }

            return FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyStyle(resumeButton);
            UIStyleTokens.Button.ApplyStyle(newButton);
            UIStyleTokens.Button.ApplyStyle(deleteButton);
            UIStyleTokens.Button.ApplyStyle(backButton);

            UIStyleTokens.Button.SetButtonLabelColor(resumeButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(newButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(deleteButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(backButton, UIStyleTokens.Button.TextDefault);
        }

        private void UpdateStepState()
        {
            bool selectingMold = currentStep == CampaignPanelStep.MoldSelection;
            if (moldSelectionSectionRoot != null)
            {
                moldSelectionSectionRoot.gameObject.SetActive(selectingMold);
            }

            if (actionStack != null)
            {
                actionStack.SetActive(true);
            }

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(!selectingMold && GameManager.Instance != null && GameManager.Instance.HasCampaignSave());
            }

            if (moldinessSummarySectionRoot != null)
            {
                moldinessSummarySectionRoot.gameObject.SetActive(!selectingMold && GameManager.Instance != null && GameManager.Instance.HasCampaignSave());
            }

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(false);
            }

            if (newButton != null)
            {
                SetButtonText(newButton, selectingMold ? "Start Campaign" : "New Campaign");
                newButton.interactable = !selectingMold || selectedCampaignMoldIndex.HasValue;
            }

            if (backButton != null)
            {
                SetButtonText(backButton, "Back");
            }

            if (developmentTestingRailRoot != null)
            {
                developmentTestingRailRoot.gameObject.SetActive(!selectingMold && ShouldShowDevelopmentTestingUi());
            }

            if (!selectingMold)
            {
                RefreshMoldinessSummaryUi();
            }

            if (selectingMold)
            {
                RefreshMoldSelectionUi();
            }
        }

        private void EnterMoldSelectionStep()
        {
            currentStep = CampaignPanelStep.MoldSelection;
            if (!selectedCampaignMoldIndex.HasValue)
            {
                selectedCampaignMoldIndex = 0;
            }

            UpdateStepState();
            ForceLayoutNow();
        }

        private void ReturnToActionStep()
        {
            currentStep = CampaignPanelStep.MainActions;
            UpdateStepState();
            ForceLayoutNow();
        }

        private void RefreshMoldSelectionUi()
        {
            if (moldSelectionTitleLabel != null)
            {
                moldSelectionTitleLabel.text = "Choose your mold icon for this campaign run.";
            }

            if (moldSelectionStatusLabel != null)
            {
                moldSelectionStatusLabel.text = selectedCampaignMoldIndex.HasValue
                    ? $"Selected mold: {GetMoldDisplayName(selectedCampaignMoldIndex.Value)}. This icon will persist for the whole run."
                    : "Select a mold icon before starting the campaign.";
            }

            RebuildMoldSelectionButtons();
        }

        private void RebuildMoldSelectionButtons()
        {
            if (moldSelectionGrid == null)
            {
                return;
            }

            int moldCount = GetAvailableMoldCount();
            EnsureMoldSelectionButtonCount(moldCount);

            for (int moldIndex = 0; moldIndex < moldSelectionButtons.Count; moldIndex++)
            {
                bool shouldShow = moldIndex < moldCount;
                var button = moldSelectionButtons[moldIndex];
                button.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var tile = GetMoldTileAtIndex(moldIndex);
                moldSelectionIcons[moldIndex].sprite = tile != null ? tile.sprite : null;
                moldSelectionIcons[moldIndex].enabled = tile != null && tile.sprite != null;

                bool isSelected = selectedCampaignMoldIndex == moldIndex;
                moldSelectionHighlights[moldIndex].enabled = isSelected;
                moldSelectionHighlights[moldIndex].gameObject.SetActive(isSelected);
                moldSelectionLabels[moldIndex].text = GetMoldDisplayName(moldIndex);
            }
        }

        private static string GetMoldDisplayName(int moldIndex)
        {
            return MoldCatalog.GetDisplayName(moldIndex);
        }

        private void EnsureMoldSelectionButtonCount(int requiredCount)
        {
            while (moldSelectionButtons.Count < requiredCount)
            {
                CreateMoldSelectionButton(moldSelectionButtons.Count);
            }
        }

        private void CreateMoldSelectionButton(int moldIndex)
        {
            var buttonObject = new GameObject(
                $"UI_CampaignMoldButton_{moldIndex + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(moldSelectionGrid.transform, false);

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            UIStyleTokens.Button.ApplyStyle(button);
            int capturedIndex = moldIndex;
            button.onClick.AddListener(() => OnCampaignMoldSelected(capturedIndex));

            var element = buttonObject.GetComponent<LayoutElement>();
            element.minWidth = 110f;
            element.preferredWidth = 110f;
            element.minHeight = 110f;
            element.preferredHeight = 110f;

            var highlightObject = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            highlightObject.transform.SetParent(buttonObject.transform, false);
            var highlightRect = highlightObject.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = new Vector2(4f, 4f);
            highlightRect.offsetMax = new Vector2(-4f, -4f);
            var highlightImage = highlightObject.GetComponent<Image>();
            var selectedTint = UIStyleTokens.Button.BackgroundSelected;
            selectedTint.a = 0.4f;
            highlightImage.color = selectedTint;
            highlightImage.raycastTarget = false;
            highlightImage.enabled = false;
            highlightObject.SetActive(false);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(buttonObject.transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(56f, 56f);
            iconRect.anchoredPosition = new Vector2(0f, 14f);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(98f, 34f);
            labelRect.anchoredPosition = new Vector2(0f, 6f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            moldSelectionButtons.Add(button);
            moldSelectionHighlights.Add(highlightImage);
            moldSelectionIcons.Add(iconImage);
            moldSelectionLabels.Add(label);

            var tooltipProvider = buttonObject.AddComponent<MoldButtonTooltipProvider>();
            tooltipProvider.Initialize(BuildMoldTooltipText(moldIndex));
            buttonObject.AddComponent<TooltipTrigger>();
        }

        private static string BuildMoldTooltipText(int moldIndex)
        {
            string moldName = MoldCatalog.GetDisplayName(moldIndex);
            string adaptId = MoldCatalog.GetStartingAdaptationId(moldIndex);
            if (!AdaptationRepository.TryGetById(adaptId, out var def))
                return $"<b>{moldName}</b>";
            return $"<b>{moldName}</b>\n\n<b>Starting Adaptation: {def.Name}</b>\n{def.Description}";
        }

        private void OnCampaignMoldSelected(int moldIndex)
        {
            selectedCampaignMoldIndex = moldIndex;
            UpdateStepState();
        }

        private int GetAvailableMoldCount()
        {
            return GameManager.Instance?.gridVisualizer != null ? GameManager.Instance.gridVisualizer.PlayerMoldTileCount : 0;
        }

        private Tile GetMoldTileAtIndex(int moldIndex)
        {
            var visualizer = GameManager.Instance?.gridVisualizer;
            if (visualizer?.playerMoldTiles == null || moldIndex < 0 || moldIndex >= visualizer.playerMoldTiles.Length)
            {
                return null;
            }

            return visualizer.playerMoldTiles[moldIndex];
        }

        private void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var tmpLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpLabel != null)
            {
                tmpLabel.text = text;
                return;
            }

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.text = text;
            }
        }

        private void RefreshButtonStates()
        {
            bool hasSave = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(hasSave);
                resumeButton.interactable = hasSave;
                UIStyleTokens.Button.SetButtonLabelColor(resumeButton, hasSave ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
            }

            if (moldinessSummarySectionRoot != null)
            {
                moldinessSummarySectionRoot.gameObject.SetActive(hasSave && currentStep == CampaignPanelStep.MainActions);
            }

            if (deleteButton != null)
            {
                // Start New Campaign already replaces existing progress.
                deleteButton.gameObject.SetActive(false);
                deleteButton.interactable = false;
            }
        }

        private void ApplyTestingModeToGameManager()
        {
            testingCardController?.ApplyToGameManager(GameManager.Instance);
        }

        private void OnResumeClicked()
        {
            ApplyTestingModeToGameManager();
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StartCampaignResume();
            bool resumed = DidCampaignNavigationActivate();

            if (resumed)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("UI_CampaignPanelController: Resume failed; keeping panel open.");
            }
        }

        private void OnNewClicked()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (currentStep == CampaignPanelStep.MainActions)
            {
                if (HasPendingSporePreservation())
                {
                    ApplyTestingModeToGameManager();
                    GameManager.Instance.StartCampaignResume();
                    if (DidCampaignNavigationActivate())
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError("UI_CampaignPanelController: Pending spore preservation prompt failed to open; keeping panel open.");
                    }

                    return;
                }

                EnterMoldSelectionStep();
                return;
            }

            if (!selectedCampaignMoldIndex.HasValue)
            {
                UpdateStepState();
                return;
            }

            ApplyTestingModeToGameManager();
            GameManager.Instance.StartCampaignNew(selectedCampaignMoldIndex.Value);
            if (GameManager.Instance.Board != null && GameManager.Instance.CurrentGameMode == GameMode.Campaign)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("UI_CampaignPanelController: New campaign failed; keeping panel open.");
            }
        }

        private bool DidCampaignNavigationActivate()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentGameMode != GameMode.Campaign)
            {
                return false;
            }

            return GameManager.Instance.Board != null
                || GameManager.Instance.IsCampaignAwaitingAdaptationSelection()
                || (GameManager.Instance.CampaignController?.IsAwaitingDefeatCarryoverSelection ?? false);
        }

        private bool HasPendingSporePreservation()
        {
            if (GameManager.Instance == null || !GameManager.Instance.HasCampaignSave())
            {
                return false;
            }

            var campaignController = GameManager.Instance.CampaignController;
            if (campaignController?.State == null && CampaignSaveService.Exists())
            {
                campaignController?.Resume();
            }

            return campaignController?.IsAwaitingDefeatCarryoverSelection ?? false;
        }

        private void OnDeleteClicked()
        {
            CampaignSaveService.Delete();
            RefreshButtonStates();
            ForceLayoutNow();
        }

        private void OnBackClicked()
        {
            if (currentStep == CampaignPanelStep.MoldSelection)
            {
                ReturnToActionStep();
                return;
            }

            gameObject.SetActive(false);
            if (modeSelectPanel != null)
            {
                modeSelectPanel.SetActive(true);
            }
        }

        private void ForceLayoutNow()
        {
            Canvas.ForceUpdateCanvases();
            if (layoutShellRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutShellRoot);
            }
            if (mainStackRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainStackRoot);
            }
            if (developmentTestingRailRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(developmentTestingRailRoot);
            }
            if (contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }
        }

    }
}
