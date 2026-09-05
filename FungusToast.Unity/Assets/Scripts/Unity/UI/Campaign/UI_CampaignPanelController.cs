using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.AI;
using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Testing;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityMoldinessProgressionState = FungusToast.Unity.Campaign.MoldinessProgressionState;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel with a deterministic two-column layout:
    /// primary menu content on the left and development testing controls on the right.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
        private const string MoldinessSummaryTooltipText = "Earn moldiness for completing campaign levels. Higher levels award more moldiness. Whenever a moldiness threshold is reached, you can pick between permanent rewards to enhance future campaign runs.";
        private const float PrimaryColumnWidth = 500f;
        private const float DevelopmentRailWidth = 400f;
        private const float LayoutShellWidth = 500f;
        private const float DevelopmentRailOffsetX = 440f;
        private const float DevelopmentRailTopOffsetY = -18f;
        private const float MoldinessSummaryPanelMinWidth = 440f;
        private const float MoldinessSummaryPanelPreferredWidth = 460f;
        private const float MoldinessSummaryTextWidth = 400f;
        private const float MoldinessProgressBarWidth = 400f;
        private const float MoldinessUnlockedRewardsGridWidth = 400f;
        private const float ActionButtonIconSize = 22f;
        private const float ActionButtonContentSpacing = 10f;
        private const float ActionButtonHorizontalPadding = 12f;
        private const float CampaignSetupWidth = 620f;
        private const float CampaignSetupInnerWidth = 584f;
        private const string CampaignStartDifficultyTitleText = "1. Choose Starting Difficulty";
        private const string MoldSelectionTitleText = "2. Choose Your Mold";

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

        // Built at runtime by BuildContentRoot()/BuildActionButtons(); see
        // UNITY_CODE_FIRST_MIGRATION.md. The Home screen controller is resolved
        // live through MainMenuRegistry rather than held as a serialized
        // cross-reference, since panel Awake order across the scene is not
        // guaranteed.
        private static UI_ModeSelectPanelController ModeSelectController => MainMenuRegistry.ModeSelectPanel;
        private static Sprite BackButtonIcon => UiSpriteLibrary.BackArrow;

        private Button resumeButton;
        private Button newButton;
        private Button backButton;

        private RectTransform contentRoot;
        private RectTransform layoutShellRoot;
        private RectTransform mainStackRoot;
        private RectTransform developmentTestingRailRoot;
        private GameObject actionStack;
        private DevelopmentTestingCardController testingCardController;
        private RectTransform moldinessSummarySectionRoot;
        private TextMeshProUGUI moldinessSummaryTitleLabel;
        private TextMeshProUGUI moldinessSummaryStatusLabel;
        private TextMeshProUGUI moldinessSummaryLifetimeLabel;
        private TextMeshProUGUI moldinessSummaryNextRewardLabel;
        private TextMeshProUGUI moldinessSummaryPendingLabel;
        private Slider moldinessSummaryProgressBar;
        private Image moldinessSummaryProgressFill;
        private MoldinessUnlockedRewardsStripController moldinessUnlockedRewardsStrip;
        private TextMeshProUGUI moldinessUnlockedRewardsLabel;
        private RectTransform moldinessUnlockedRewardsGridRoot;
        private GridLayoutGroup moldinessUnlockedRewardsGrid;
        private readonly List<GameObject> moldinessUnlockedRewardIcons = new();
        private readonly List<TextMeshProUGUI> moldinessUnlockedRewardCountBadges = new();
        private RectTransform moldSelectionSectionRoot;
        private TextMeshProUGUI moldSelectionTitleLabel;
        private TextMeshProUGUI moldSelectionStatusLabel;
        private TextMeshProUGUI campaignStartDifficultyTitleLabel;
        private TextMeshProUGUI campaignStartDifficultyStatusLabel;
        private GridLayoutGroup campaignStartDifficultyGrid;
        private readonly List<Button> campaignStartDifficultyButtons = new();
        private readonly List<Image> campaignStartDifficultyHighlights = new();
        private readonly List<Outline> campaignStartDifficultyOutlines = new();
        private readonly List<TextMeshProUGUI> campaignStartDifficultyLabels = new();
        private GridLayoutGroup moldSelectionGrid;
        private readonly List<Button> moldSelectionButtons = new();
        private readonly List<Image> moldSelectionHighlights = new();
        private readonly List<Outline> moldSelectionOutlines = new();
        private readonly List<Image> moldSelectionIcons = new();
        private readonly List<TextMeshProUGUI> moldSelectionLabels = new();
        private CampaignPanelStep currentStep = CampaignPanelStep.MainActions;
        private int? selectedCampaignMoldIndex;
        private int selectedCampaignStartDifficultyIndex;

        private void Awake()
        {
            MainMenuRegistry.CampaignPanel = this;

            BuildContentRoot();
            if (contentRoot == null)
            {
                Debug.LogError("UI_CampaignPanelController: contentRoot failed to build.");
                return;
            }

            // Built before BuildLayoutScaffold/BuildTestingCard: the testing card
            // clones backButton as its own button template (see BuildTestingCard),
            // so the action buttons must exist first.
            BuildActionButtons();
            BuildLayoutScaffold();
            BuildTestingCard();
            BuildMoldinessSummarySection();
            BuildMoldSelectionSection();
            BuildActionStack();
            ApplyStyle();
            UpdateStepState();

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (newButton != null) newButton.onClick.AddListener(OnNewClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            ApplyMenuTooltips();
            ValidateBuiltUi();
        }

        private void OnDestroy()
        {
            if (MainMenuRegistry.CampaignPanel == this)
            {
                MainMenuRegistry.CampaignPanel = null;
            }
        }

        /// <summary>
        /// Builds the Campaign screen's root content container in code, replacing
        /// the scene-authored <c>UI_CampaignContent</c>. BuildLayoutScaffold and the
        /// section builders populate it exactly as they did for the scene-authored
        /// version.
        /// </summary>
        private void BuildContentRoot()
        {
            MainMenuRegistry.DestroyLegacyChildIfPresent(transform, "UI_CampaignContent");

            GameObject contentObject = new GameObject(
                "UI_CampaignContent",
                typeof(RectTransform),
                typeof(ContentSizeFitter),
                typeof(VerticalLayoutGroup));
            contentObject.transform.SetParent(transform, false);
            contentObject.layer = gameObject.layer;

            contentRoot = contentObject.GetComponent<RectTransform>();
            contentRoot.sizeDelta = new Vector2(400f, 520f);

            ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(40, 40, 40, 40);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.spacing = 18f;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = false;
        }

        /// <summary>
        /// Builds the Resume/New/Back buttons as bare button+label objects.
        /// ConfigureButtonContent/ConfigureMenuActionLayout (driven from
        /// ApplyStyle/UpdateStepState) finish icon, color, and sizing exactly as
        /// they did for the scene-authored buttons; BuildActionStack reparents
        /// these into the action stack it builds.
        /// </summary>
        private void BuildActionButtons()
        {
            resumeButton = CreateActionButton("UI_CampaignResumeButton", "Resume Campaign");
            newButton = CreateActionButton("UI_CampaignNewButton", "Start New Campaign");
            backButton = CreateActionButton("UI_CampaignBackButton", "Back");
        }

        private Button CreateActionButton(string objectName, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.layer = gameObject.layer;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            return button;
        }

        private void ValidateBuiltUi()
        {
            if (contentRoot == null) Debug.LogError("UI_CampaignPanelController: contentRoot failed to build.");
            if (resumeButton == null) Debug.LogError("UI_CampaignPanelController: resumeButton failed to build.");
            if (newButton == null) Debug.LogError("UI_CampaignPanelController: newButton failed to build.");
            if (backButton == null) Debug.LogError("UI_CampaignPanelController: backButton failed to build.");
            if (layoutShellRoot == null) Debug.LogError("UI_CampaignPanelController: layoutShellRoot failed to build.");
            if (mainStackRoot == null) Debug.LogError("UI_CampaignPanelController: mainStackRoot failed to build.");
            if (actionStack == null) Debug.LogError("UI_CampaignPanelController: actionStack failed to build.");
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
            if (DevelopmentTestingAccess.IsAvailable && developmentTestingRailRoot == null)
            {
                var rail = new GameObject("UI_CampaignDevelopmentTestingRail", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                rail.transform.SetParent(layoutShellRoot, false);
                developmentTestingRailRoot = rail.GetComponent<RectTransform>();
            }

            if (developmentTestingRailRoot != null)
            {
                developmentTestingRailRoot.SetParent(layoutShellRoot, false);
                developmentTestingRailRoot.SetSiblingIndex(1);
                ConfigureDevelopmentTestingRailRoot(developmentTestingRailRoot);
                developmentTestingRailRoot.gameObject.SetActive(DevelopmentTestingAccess.IsAvailable);
            }

            HideLegacyTestingBlocks();
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
            if (!DevelopmentTestingAccess.IsAvailable || developmentTestingRailRoot == null)
            {
                if (developmentTestingRailRoot != null)
                {
                    developmentTestingRailRoot.gameObject.SetActive(false);
                }

                return;
            }

            testingCardController = new DevelopmentTestingCardController(new DevelopmentTestingCardOptions
            {
                Parent = developmentTestingRailRoot,
                ButtonTemplate = backButton != null ? backButton : resumeButton,
                DropdownTemplate = FindDropdownTemplate(),
                SupportsCampaignLevelSelection = true,
                SupportsForcedAdaptation = true,
                SupportsForceMoldinessRewards = true,
                SupportsForcedMoldinessRewardSelection = true,
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
            return DevelopmentTestingAccess.IsAvailable;
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
            EnsureMoldinessSummaryProgressBar();
            EnsureMoldinessUnlockedRewardsStrip();
            ReorderMoldinessSummaryContent();
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

            var tooltipTrigger = moldinessSummarySectionRoot.GetComponent<TooltipTrigger>()
                ?? moldinessSummarySectionRoot.gameObject.AddComponent<TooltipTrigger>();
            tooltipTrigger.SetStaticText(MoldinessSummaryTooltipText);
            tooltipTrigger.SetPinOnClick(false);

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
            element.minWidth = MoldinessSummaryPanelMinWidth;
            element.preferredWidth = MoldinessSummaryPanelPreferredWidth;
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
            moldinessSummaryLifetimeLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryLifetime",
                20f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                26f);
            moldinessSummaryNextRewardLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryNextReward",
                17f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                42f);
            moldinessSummaryPendingLabel ??= CreateMoldinessSummaryText(
                "UI_CampaignMoldinessSummaryPending",
                18f,
                FontStyles.Italic,
                UIStyleTokens.State.Warning,
                30f);
        }

        private void EnsureMoldinessUnlockedRewardsStrip()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            moldinessUnlockedRewardsStrip ??= new MoldinessUnlockedRewardsStripController(
                moldinessSummarySectionRoot,
                "UI_CampaignMoldiness",
                MoldinessUnlockedRewardsGridWidth,
                GetMoldinessRewardIcon);
            moldinessUnlockedRewardsStrip.EnsureBuilt();
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
            label.raycastTarget = false;

            var element = label.GetComponent<LayoutElement>();
            element.minWidth = MoldinessSummaryTextWidth;
            element.preferredWidth = MoldinessSummaryTextWidth;
            element.minHeight = minHeight;
            element.preferredHeight = -1f;

            return label;
        }

        private void EnsureMoldinessSummaryProgressBar()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            RectTransform progressRoot = moldinessSummarySectionRoot.Find("UI_CampaignMoldinessProgressBar") as RectTransform;
            if (progressRoot == null)
            {
                GameObject progressObject = new GameObject(
                    "UI_CampaignMoldinessProgressBar",
                    typeof(RectTransform),
                    typeof(Slider),
                    typeof(LayoutElement));
                progressObject.transform.SetParent(moldinessSummarySectionRoot, false);
                progressRoot = progressObject.GetComponent<RectTransform>();

                GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
                RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
                backgroundRect.SetParent(progressRoot, false);
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                Image background = backgroundObject.GetComponent<Image>();
                background.color = UIStyleTokens.Surface.PanelSecondary;
                background.raycastTarget = false;

                GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.SetParent(progressRoot, false);
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(3f, 3f);
                fillRect.offsetMax = new Vector2(-3f, -3f);
                moldinessSummaryProgressFill = fillObject.GetComponent<Image>();
                moldinessSummaryProgressFill.color = UIStyleTokens.Accent.Lichen;
                moldinessSummaryProgressFill.raycastTarget = false;

                moldinessSummaryProgressBar = progressObject.GetComponent<Slider>();
                moldinessSummaryProgressBar.fillRect = fillRect;
                moldinessSummaryProgressBar.direction = Slider.Direction.LeftToRight;
                moldinessSummaryProgressBar.minValue = 0f;
                moldinessSummaryProgressBar.maxValue = 1f;
                moldinessSummaryProgressBar.wholeNumbers = false;
                moldinessSummaryProgressBar.interactable = false;
                moldinessSummaryProgressBar.transition = Selectable.Transition.None;
            }
            else
            {
                moldinessSummaryProgressBar = progressRoot.GetComponent<Slider>();
                moldinessSummaryProgressFill = progressRoot.Find("Fill")?.GetComponent<Image>();
            }

            LayoutElement progressLayout = progressRoot.GetComponent<LayoutElement>();
            progressLayout.minWidth = MoldinessProgressBarWidth;
            progressLayout.preferredWidth = MoldinessProgressBarWidth;
            progressLayout.minHeight = 22f;
            progressLayout.preferredHeight = 22f;
            progressLayout.flexibleWidth = 0f;
            progressLayout.flexibleHeight = 0f;

        }

        private void EnsureMoldinessUnlockedRewardsGrid()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            if (moldinessUnlockedRewardsGrid == null)
            {
                moldinessUnlockedRewardsGridRoot = moldinessSummarySectionRoot.Find("UI_CampaignMoldinessUnlockedRewardsGrid") as RectTransform;
                if (moldinessUnlockedRewardsGridRoot == null)
                {
                    var gridObject = new GameObject(
                        "UI_CampaignMoldinessUnlockedRewardsGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    gridObject.transform.SetParent(moldinessSummarySectionRoot, false);
                    moldinessUnlockedRewardsGridRoot = gridObject.GetComponent<RectTransform>();
                }

                moldinessUnlockedRewardsGrid = moldinessUnlockedRewardsGridRoot.GetComponent<GridLayoutGroup>();
            }

            moldinessUnlockedRewardsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            moldinessUnlockedRewardsGrid.constraintCount = 7;
            moldinessUnlockedRewardsGrid.cellSize = new Vector2(48f, 48f);
            moldinessUnlockedRewardsGrid.spacing = new Vector2(8f, 8f);
            moldinessUnlockedRewardsGrid.childAlignment = TextAnchor.UpperCenter;

            var fitter = moldinessUnlockedRewardsGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldinessUnlockedRewardsGrid.GetComponent<LayoutElement>();
            element.minWidth = MoldinessUnlockedRewardsGridWidth;
            element.preferredWidth = MoldinessUnlockedRewardsGridWidth;
            element.minHeight = 0f;
            element.preferredHeight = -1f;
        }

        private void ReorderMoldinessSummaryContent()
        {
            if (moldinessSummarySectionRoot == null)
            {
                return;
            }

            int siblingIndex = 0;
            moldinessSummaryTitleLabel?.transform.SetSiblingIndex(siblingIndex++);
            moldinessSummaryStatusLabel?.transform.SetSiblingIndex(siblingIndex++);
            moldinessSummaryProgressBar?.transform.SetSiblingIndex(siblingIndex++);
            moldinessSummaryLifetimeLabel?.transform.SetSiblingIndex(siblingIndex++);
            moldinessSummaryNextRewardLabel?.transform.SetSiblingIndex(siblingIndex++);
            moldinessSummaryPendingLabel?.transform.SetSiblingIndex(siblingIndex++);
            moldinessUnlockedRewardsStrip?.RootTransform?.SetSiblingIndex(siblingIndex);
        }

        private void RefreshMoldinessUnlockedRewardsGrid(List<MoldinessUnlockDefinition> unlockedRewards)
        {
            if (moldinessUnlockedRewardsGridRoot == null || moldinessUnlockedRewardsGrid == null)
            {
                return;
            }

            unlockedRewards ??= new List<MoldinessUnlockDefinition>();
            moldinessUnlockedRewardsGridRoot.gameObject.SetActive(unlockedRewards.Count > 0);
            var rewardCounts = unlockedRewards
                .GroupBy(reward => reward.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var uniqueRewards = unlockedRewards
                .GroupBy(reward => reward.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            while (moldinessUnlockedRewardIcons.Count < uniqueRewards.Count)
            {
                var iconRoot = new GameObject(
                    $"UI_CampaignMoldinessUnlockedReward_{moldinessUnlockedRewardIcons.Count + 1}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement),
                    typeof(TooltipTrigger));
                iconRoot.transform.SetParent(moldinessUnlockedRewardsGrid.transform, false);

                var background = iconRoot.GetComponent<Image>();
                background.color = UIStyleTokens.Surface.PanelSecondary;
                background.raycastTarget = true;

                var layout = iconRoot.GetComponent<LayoutElement>();
                layout.minWidth = 48f;
                layout.preferredWidth = 48f;
                layout.minHeight = 48f;
                layout.preferredHeight = 48f;

                var outline = iconRoot.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
                outline.effectDistance = new Vector2(1f, -1f);

                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(iconRoot.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(34f, 34f);
                iconRect.anchoredPosition = Vector2.zero;

                var badgeObject = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
                badgeObject.transform.SetParent(iconRoot.transform, false);
                var badgeRect = badgeObject.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.anchoredPosition = new Vector2(-2f, 2f);
                badgeRect.sizeDelta = new Vector2(20f, 20f);
                var badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.color = UIStyleTokens.Surface.PanelPrimary;
                badgeImage.raycastTarget = false;

                var badgeOutline = badgeObject.AddComponent<Outline>();
                badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
                badgeOutline.effectDistance = new Vector2(1f, -1f);

                var badgeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeLabelObject.transform.SetParent(badgeObject.transform, false);
                var badgeLabelRect = badgeLabelObject.GetComponent<RectTransform>();
                badgeLabelRect.anchorMin = Vector2.zero;
                badgeLabelRect.anchorMax = Vector2.one;
                badgeLabelRect.offsetMin = Vector2.zero;
                badgeLabelRect.offsetMax = Vector2.zero;
                var badgeLabel = badgeLabelObject.GetComponent<TextMeshProUGUI>();
                badgeLabel.alignment = TextAlignmentOptions.Center;
                badgeLabel.fontSize = 13f;
                badgeLabel.fontStyle = FontStyles.Bold;
                badgeLabel.color = UIStyleTokens.Text.Primary;
                badgeLabel.enableAutoSizing = true;
                badgeLabel.fontSizeMin = 10f;
                badgeLabel.fontSizeMax = 13f;
                badgeLabel.raycastTarget = false;

                moldinessUnlockedRewardIcons.Add(iconRoot);
                moldinessUnlockedRewardCountBadges.Add(badgeLabel);
            }

            for (int i = 0; i < moldinessUnlockedRewardIcons.Count; i++)
            {
                var iconRoot = moldinessUnlockedRewardIcons[i];
                bool shouldShow = i < uniqueRewards.Count;
                iconRoot.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var reward = uniqueRewards[i];
                int ownedCount = rewardCounts.TryGetValue(reward.Id, out int count) ? count : 1;
                var background = iconRoot.GetComponent<Image>();
                background.color = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.16f);

                var outline = iconRoot.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.45f);
                }

                var iconImage = iconRoot.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = GetMoldinessRewardIcon(reward);
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                    iconImage.raycastTarget = false;
                }

                if (i < moldinessUnlockedRewardCountBadges.Count)
                {
                    var badgeLabel = moldinessUnlockedRewardCountBadges[i];
                    var badgeRoot = badgeLabel != null ? badgeLabel.transform.parent?.gameObject : null;
                    bool showBadge = reward.IsRepeatable && ownedCount > 1;
                    if (badgeRoot != null)
                    {
                        badgeRoot.SetActive(showBadge);
                        var badgeImage = badgeRoot.GetComponent<Image>();
                        if (badgeImage != null)
                        {
                            badgeImage.color = new Color(reward.AccentColor.r, reward.AccentColor.g, reward.AccentColor.b, 0.92f);
                        }
                    }

                    if (badgeLabel != null)
                    {
                        badgeLabel.text = CompactCountLabel(ownedCount);
                        badgeLabel.color = UIStyleTokens.Text.Primary;
                    }
                }

                var tooltipTrigger = iconRoot.GetComponent<TooltipTrigger>();
                if (reward.Type == MoldinessUnlockType.UnlockAdaptation && AdaptationRepository.TryGetById(reward.AdaptationId, out var adaptation))
                {
                    var provider = iconRoot.GetComponent<AdaptationTooltipProvider>() ?? iconRoot.AddComponent<AdaptationTooltipProvider>();
                    provider.Initialize(adaptation);
                    tooltipTrigger.SetDynamicProvider(provider);
                }
                else
                {
                    var provider = iconRoot.GetComponent<MoldinessRewardTooltipProvider>() ?? iconRoot.AddComponent<MoldinessRewardTooltipProvider>();
                    int carryoverCapacity = reward.Type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover
                        ? Mathf.Max(0, GameManager.Instance?.CampaignController?.State?.moldiness?.failedRunAdaptationCarryoverCount ?? 0)
                        : 0;
                    provider.Initialize(reward, ownedCount, carryoverCapacity);
                    tooltipTrigger.SetDynamicProvider(provider);
                }

                tooltipTrigger.SetAutoPlacementOffsetX(18f);
                tooltipTrigger.SetPinOnClick(false);
            }
        }

        private static string CompactCountLabel(int count)
        {
            if (count < 100)
            {
                return count.ToString();
            }

            if (count < 1000)
            {
                return $"{count / 100f:0.#}h";
            }

            return $"{count / 1000f:0.#}k";
        }

        private static Sprite GetMoldinessRewardIcon(MoldinessUnlockDefinition offer)
        {
            if (offer == null)
            {
                return AdaptationArtRepository.GetIcon(null);
            }

            if (offer.Type == MoldinessUnlockType.UnlockAdaptation && AdaptationRepository.TryGetById(offer.AdaptationId, out var adaptation))
            {
                return AdaptationArtRepository.GetIcon(adaptation);
            }

            if (offer.Type == MoldinessUnlockType.UnlockMycovariant)
            {
                return CreateMycovariantUnlockIcon(offer);
            }

            return CreateCampaignUpgradeIcon(offer);
        }

        private static Sprite CreateCampaignUpgradeIcon(MoldinessUnlockDefinition offer)
        {
            return ProceduralIconUtility.CreateSprite(
                $"MoldinessReward_{offer.Id}",
                Color.Lerp(offer.AccentColor, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                offer.AccentColor,
                (texture, accent, highlight) =>
                {
                    if (offer.Type == MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover)
                    {
                        DrawCarryoverUpgradeMotif(texture, accent, highlight, Mathf.Max(1, GetRomanTierValue(offer.DisplayName)));
                        return;
                    }

                    switch (offer.Id)
                    {
                        case MoldinessUnlockCatalog.StrainProfilingRewardId:
                            DrawStrainProfilingMotif(texture, accent, highlight);
                            break;
                        case MoldinessUnlockCatalog.SporeSiftingRewardId:
                            DrawSporeSiftingMotif(texture, accent, highlight);
                            break;
                        default:
                            DrawFallbackUpgradeMotif(texture, accent, highlight, offer.Id);
                            break;
                    }
                },
                40);
        }

        private static Sprite CreateMycovariantUnlockIcon(MoldinessUnlockDefinition offer)
        {
            var mycovariant = MycovariantRepository.All.Find(candidate => candidate.Id == offer.MycovariantId);
            string identityKey = mycovariant?.IconId ?? offer.Id;

            return ProceduralIconUtility.CreateSprite(
                $"MoldinessReward_{offer.Id}",
                Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                UIStyleTokens.State.Info,
                (texture, accent, highlight) => DrawMycovariantUnlockMotif(texture, accent, highlight, identityKey),
                40);
        }

        private static void DrawCarryoverUpgradeMotif(Texture2D texture, Color accent, Color highlight, int tier)
        {
            ProceduralIconUtility.DrawLine(texture, 10, 10, 30, 10, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 10, 20, 20, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 12, 22, 20, 30, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 28, 22, 20, 30, accent, 2);
            ProceduralIconUtility.FillCircle(texture, 12, 18, 3, accent);
            ProceduralIconUtility.FillCircle(texture, 20, 22, 3, accent);
            ProceduralIconUtility.FillCircle(texture, 28, 18, 3, accent);
            DrawPips(texture, Mathf.Clamp(tier, 1, 5), highlight);
        }

        private static void DrawStrainProfilingMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 16, 22, 7, 2, accent);
            ProceduralIconUtility.DrawLine(texture, 21, 17, 29, 9, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 10, 9, 10, 29, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 10, 9, 30, 9, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 11, 13, 16, 16, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 16, 16, 21, 14, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 21, 14, 28, 21, accent, 1);
            ProceduralIconUtility.FillCircle(texture, 16, 22, 2, highlight);
        }

        private static void DrawSporeSiftingMotif(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawLine(texture, 10, 30, 30, 30, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 10, 30, 17, 18, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 30, 30, 23, 18, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 17, 18, 23, 18, accent, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 18, 20, 10, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 17, 24, 23, 24, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 14, 27, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 25, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 26, 27, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 12, 2, accent);
        }

        private static void DrawFallbackUpgradeMotif(Texture2D texture, Color accent, Color highlight, string identity)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 9, 2, accent);
            DrawHashGlyph(texture, accent, highlight, identity);
        }

        private static void DrawMycovariantUnlockMotif(Texture2D texture, Color accent, Color highlight, string identity)
        {
            int hash = ProceduralIconUtility.ComputeStableHash(identity);
            int pattern = hash % 4;
            switch (pattern)
            {
                case 0:
                    ProceduralIconUtility.DrawRing(texture, 20, 20, 8, 2, accent);
                    ProceduralIconUtility.DrawLine(texture, 20, 12, 20, 28, highlight, 1);
                    ProceduralIconUtility.DrawLine(texture, 12, 20, 28, 20, highlight, 1);
                    break;
                case 1:
                    ProceduralIconUtility.DrawLine(texture, 10, 30, 30, 10, accent, 2);
                    ProceduralIconUtility.DrawLine(texture, 10, 22, 22, 10, highlight, 1);
                    ProceduralIconUtility.DrawLine(texture, 18, 30, 30, 18, highlight, 1);
                    break;
                case 2:
                    ProceduralIconUtility.DrawRing(texture, 15, 15, 5, 2, accent);
                    ProceduralIconUtility.DrawRing(texture, 25, 25, 5, 2, accent);
                    ProceduralIconUtility.DrawLine(texture, 18, 18, 22, 22, highlight, 1);
                    break;
                default:
                    ProceduralIconUtility.FillShield(texture, 20, 21, 8, 9, accent);
                    ProceduralIconUtility.DrawLine(texture, 13, 15, 27, 27, highlight, 1);
                    ProceduralIconUtility.DrawLine(texture, 27, 15, 13, 27, highlight, 1);
                    break;
            }

            DrawHashGlyph(texture, accent, highlight, identity);
        }

        private static void DrawHashGlyph(Texture2D texture, Color accent, Color highlight, string identity)
        {
            int hash = ProceduralIconUtility.ComputeStableHash(identity);
            for (int i = 0; i < 3; i++)
            {
                int x = 10 + (((hash >> (i * 4)) & 0x7) * 3);
                int y = 10 + (((hash >> (i * 7 + 3)) & 0x7) * 3);
                int radius = 1 + ((hash >> (i * 5 + 2)) & 0x1);
                ProceduralIconUtility.FillCircle(texture, Mathf.Clamp(x, 8, 32), Mathf.Clamp(y, 8, 32), radius, i % 2 == 0 ? highlight : accent);
            }
        }

        private static void DrawPips(Texture2D texture, int count, Color color)
        {
            int startX = 20 - ((count - 1) * 3);
            for (int i = 0; i < count; i++)
            {
                ProceduralIconUtility.FillCircle(texture, startX + (i * 6), 34, 1, color);
            }
        }

        private static int GetRomanTierValue(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return 1;
            }

            int lastSpace = displayName.LastIndexOf(' ');
            if (lastSpace < 0 || lastSpace >= displayName.Length - 1)
            {
                return 1;
            }

            string numeral = displayName.Substring(lastSpace + 1).Trim().ToUpperInvariant();
            return numeral switch
            {
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                "V" => 5,
                _ => 1
            };
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

            FungusToast.Unity.Campaign.MoldinessProgressSnapshot snapshot = campaignController.MoldinessProgress;
            int level = snapshot.CurrentTierIndex + 1;
            int threshold = Math.Max(1, snapshot.CurrentThreshold);
            int progress = Mathf.Clamp(snapshot.CurrentProgress, 0, threshold);
            if (moldinessSummaryTitleLabel != null)
            {
                moldinessSummaryTitleLabel.text = $"Moldiness Level {level}";
            }

            if (moldinessSummaryStatusLabel != null)
            {
                moldinessSummaryStatusLabel.text = $"{progress} / {threshold} to Level {level + 1}";
            }

            if (moldinessSummaryProgressBar != null)
            {
                moldinessSummaryProgressBar.value = threshold > 0 ? progress / (float)threshold : 0f;
            }

            if (moldinessSummaryLifetimeLabel != null)
            {
                moldinessSummaryLifetimeLabel.text = $"Lifetime earned: {snapshot.LifetimeEarned}";
                moldinessSummaryLifetimeLabel.fontSize = 16f;
                moldinessSummaryLifetimeLabel.color = UIStyleTokens.Text.Muted;
            }

            if (moldinessSummaryNextRewardLabel != null)
            {
                moldinessSummaryNextRewardLabel.text = BuildNextMoldinessRewardPreview(campaignController.State?.moldiness);
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

            moldinessUnlockedRewardsStrip?.Refresh(campaignController.State?.moldiness);

            bool pendingSporePreservation = campaignController.IsAwaitingDefeatCarryoverSelection;
            bool pendingMoldinessReward = campaignController.HasPendingMoldinessUnlockChoice;
            int nextLevelDisplay = GetNextCampaignLevelDisplay(campaignController);
            string resumableLevelLabel = BuildResumableLevelLabel(campaignController, nextLevelDisplay);
            if (resumeButton != null)
            {
                SetButtonText(
                    resumeButton,
                    pendingSporePreservation
                        ? $"Resume Campaign (Pending Spore Preservation, Level {nextLevelDisplay})"
                        : pendingMoldinessReward
                            ? $"Resume Campaign (Pending Reward, Level {nextLevelDisplay})"
                            : $"Resume Campaign ({resumableLevelLabel})");
            }
        }

        private static string BuildNextMoldinessRewardPreview(UnityMoldinessProgressionState progressionState)
        {
            int currentUnlockLevel = Mathf.Max(0, progressionState?.unlockLevel ?? 0);
            var nextRewards = MoldinessUnlockCatalog.All
                .Where(definition => definition.RequiredUnlockLevel > currentUnlockLevel)
                .OrderBy(definition => definition.RequiredUnlockLevel)
                .ThenBy(definition => MoldinessUnlockCatalog.GetSortIndex(definition.Id))
                .ToList();

            if (nextRewards.Count == 0)
            {
                return "All catalog reward tiers unlocked.";
            }

            int nextLevel = nextRewards[0].RequiredUnlockLevel;
            var rewardsAtLevel = nextRewards
                .Where(definition => definition.RequiredUnlockLevel == nextLevel)
                .ToList();
            string additionalRewards = rewardsAtLevel.Count > 1
                ? $" + {rewardsAtLevel.Count - 1} more"
                : string.Empty;
            return $"Next reward tier: Level {nextLevel} adds {rewardsAtLevel[0].DisplayName}{additionalRewards}.";
        }

        private static string BuildResumableLevelLabel(CampaignController campaignController, int nextLevelDisplay)
        {
            if (campaignController?.State?.hasInLevelGameplayCheckpoint == true
                && campaignController.State.inLevelRuntimeSnapshot != null)
            {
                return $"Level {nextLevelDisplay}, Round {campaignController.State.inLevelRuntimeSnapshot.CurrentRound}";
            }

            return $"Level {nextLevelDisplay}";
        }

        private static int GetNextCampaignLevelDisplay(CampaignController campaignController)
        {
            if (campaignController?.State == null)
            {
                return 1;
            }

            if (campaignController.IsAwaitingDefeatCarryoverSelection)
            {
                return 1;
            }

            int nextLevelDisplay = campaignController.State.levelIndex + 1;
            if (campaignController.IsAwaitingAdaptationSelection)
            {
                nextLevelDisplay++;
            }

            return Mathf.Max(1, nextLevelDisplay);
        }

        private static int GetMoldinessRewardCategorySortOrder(MoldinessUnlockDefinition definition)
        {
            if (definition == null)
            {
                return int.MaxValue;
            }

            return definition.Type switch
            {
                MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover => 0,
                MoldinessUnlockType.UnlockCampaignIntel => 0,
                MoldinessUnlockType.UnlockCampaignDraftRedraw => 0,
                MoldinessUnlockType.UnlockAdaptation => 1,
                MoldinessUnlockType.UnlockMycovariant => 2,
                _ => 3
            };
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
            EnsureCampaignStartDifficultyHeader();
            EnsureCampaignStartDifficultyGrid();
            EnsureMoldSelectionGrid();
            ReorderMoldSelectionContent();
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
            element.minWidth = CampaignSetupWidth;
            element.preferredWidth = CampaignSetupWidth;
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

        private void EnsureCampaignStartDifficultyHeader()
        {
            campaignStartDifficultyTitleLabel ??= CreateMoldSelectionText(
                "UI_CampaignStartDifficultyTitle",
                24f,
                FontStyles.Bold,
                UIStyleTokens.Text.Primary,
                34f);
            campaignStartDifficultyStatusLabel ??= CreateMoldSelectionText(
                "UI_CampaignStartDifficultyStatus",
                18f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                46f);
        }

        private void ReorderMoldSelectionContent()
        {
            if (campaignStartDifficultyTitleLabel != null)
            {
                campaignStartDifficultyTitleLabel.transform.SetSiblingIndex(0);
            }

            if (campaignStartDifficultyStatusLabel != null)
            {
                campaignStartDifficultyStatusLabel.transform.SetSiblingIndex(1);
            }

            if (campaignStartDifficultyGrid != null)
            {
                campaignStartDifficultyGrid.transform.SetSiblingIndex(2);
            }

            if (moldSelectionTitleLabel != null)
            {
                moldSelectionTitleLabel.transform.SetSiblingIndex(3);
            }

            if (moldSelectionStatusLabel != null)
            {
                moldSelectionStatusLabel.transform.SetSiblingIndex(4);
            }

            if (moldSelectionGrid != null)
            {
                moldSelectionGrid.transform.SetSiblingIndex(5);
            }
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
            element.minWidth = CampaignSetupInnerWidth;
            element.preferredWidth = CampaignSetupInnerWidth;
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
            moldSelectionGrid.cellSize = new Vector2(132f, 122f);
            moldSelectionGrid.spacing = new Vector2(10f, 10f);
            moldSelectionGrid.childAlignment = TextAnchor.UpperCenter;

            var fitter = moldSelectionGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = moldSelectionGrid.GetComponent<LayoutElement>();
            element.minWidth = 558f;
            element.preferredWidth = 558f;
            element.minHeight = 254f;
            element.preferredHeight = -1f;
        }

        private void EnsureCampaignStartDifficultyGrid()
        {
            if (moldSelectionSectionRoot == null)
            {
                return;
            }

            if (campaignStartDifficultyGrid == null)
            {
                var existing = moldSelectionSectionRoot.Find("UI_CampaignStartDifficultyGrid") as RectTransform;
                if (existing != null)
                {
                    campaignStartDifficultyGrid = existing.GetComponent<GridLayoutGroup>();
                }
                else
                {
                    var gridObject = new GameObject(
                        "UI_CampaignStartDifficultyGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    gridObject.transform.SetParent(moldSelectionSectionRoot, false);
                    campaignStartDifficultyGrid = gridObject.GetComponent<GridLayoutGroup>();
                }
            }

            campaignStartDifficultyGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            campaignStartDifficultyGrid.constraintCount = 3;
            campaignStartDifficultyGrid.cellSize = new Vector2(188f, 96f);
            campaignStartDifficultyGrid.spacing = new Vector2(8f, 10f);
            campaignStartDifficultyGrid.childAlignment = TextAnchor.UpperCenter;

            var fitter = campaignStartDifficultyGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = campaignStartDifficultyGrid.GetComponent<LayoutElement>();
            element.minWidth = CampaignSetupInnerWidth;
            element.preferredWidth = CampaignSetupInnerWidth;
            element.minHeight = 202f;
            element.preferredHeight = -1f;
        }

        private void EnsureCampaignStartDifficultyButtonCount(int requiredCount)
        {
            while (campaignStartDifficultyButtons.Count < requiredCount)
            {
                CreateCampaignStartDifficultyButton(campaignStartDifficultyButtons.Count);
            }
        }

        private void CreateCampaignStartDifficultyButton(int optionIndex)
        {
            var buttonObject = new GameObject(
                $"UI_CampaignStartDifficultyButton_{optionIndex + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(Outline));
            buttonObject.transform.SetParent(campaignStartDifficultyGrid.transform, false);

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            UIStyleTokens.Button.ApplyStyle(button);
            int capturedIndex = optionIndex;
            button.onClick.AddListener(() => OnCampaignStartDifficultySelected(capturedIndex));

            var tooltipTrigger = buttonObject.AddComponent<TooltipTrigger>();
            tooltipTrigger.SetPinOnClick(false);
            tooltipTrigger.SetAutoPlacementOffsetX(18f);

            var element = buttonObject.GetComponent<LayoutElement>();
            element.minWidth = 188f;
            element.preferredWidth = 188f;
            element.minHeight = 96f;
            element.preferredHeight = 96f;

            var selectionOutline = buttonObject.GetComponent<Outline>();
            selectionOutline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Lichen, UIStyleTokens.Alpha.FocusOutline);
            selectionOutline.effectDistance = new Vector2(2f, -2f);
            selectionOutline.useGraphicAlpha = false;
            selectionOutline.enabled = false;

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

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(170f, 82f);
            labelRect.anchoredPosition = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 16f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 17f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            campaignStartDifficultyButtons.Add(button);
            campaignStartDifficultyHighlights.Add(highlightImage);
            campaignStartDifficultyOutlines.Add(selectionOutline);
            campaignStartDifficultyLabels.Add(label);
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
            ReparentActionButton(backButton, 2);
        }

        private void ReparentActionButton(Button button, int index)
        {
            if (button == null || actionStack == null)
            {
                return;
            }

            button.transform.SetParent(actionStack.transform, false);
            button.transform.SetSiblingIndex(index);
            EnsureButtonLayout(
                button,
                button == backButton ? UIStyleTokens.Button.DesktopCompactMenuActionWidth : UIStyleTokens.Button.DesktopPrimaryMenuActionWidth);
        }

        private static void EnsureButtonLayout(Button button, float width)
        {
            UIStyleTokens.Button.ConfigureMenuActionLayout(button, width);
        }

        private TMP_Dropdown FindDropdownTemplate()
        {
            return FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.WithAlpha(UIStyleTokens.Surface.Canvas, 0f));
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);
            ApplyActionButtonSemantics();
        }

        private void ApplyActionButtonSemantics()
        {
            bool selectingMold = currentStep == CampaignPanelStep.MoldSelection;
            bool hasResumableCampaignSave = GameManager.Instance != null && GameManager.Instance.HasResumableCampaignSave();
            bool useAffirmativeResume = !selectingMold && hasResumableCampaignSave;
            bool useAffirmativeNew = selectingMold || !hasResumableCampaignSave;

            if (resumeButton != null)
            {
                if (useAffirmativeResume)
                {
                    UIStyleTokens.Button.ApplyAffirmativeMenuAction(resumeButton);
                }
                else
                {
                    UIStyleTokens.Button.ApplyNeutralMenuAction(resumeButton);
                }
            }

            if (newButton != null)
            {
                if (useAffirmativeNew)
                {
                    UIStyleTokens.Button.ApplyAffirmativeMenuAction(newButton);
                }
                else
                {
                    UIStyleTokens.Button.ApplyNeutralMenuAction(newButton);
                }
            }

            UIStyleTokens.Button.ApplySecondaryMenuAction(backButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
        }

        private void UpdateStepState()
        {
            bool selectingMold = currentStep == CampaignPanelStep.MoldSelection;
            bool hasCampaignSave = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();
            bool hasResumableCampaignSave = GameManager.Instance != null && GameManager.Instance.HasResumableCampaignSave();
            ConfigureCampaignContentWidth(selectingMold);
            ApplyActionButtonSemantics();
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
                resumeButton.gameObject.SetActive(!selectingMold && hasResumableCampaignSave);
            }

            if (moldinessSummarySectionRoot != null)
            {
                moldinessSummarySectionRoot.gameObject.SetActive(!selectingMold && hasCampaignSave);
            }

            if (newButton != null)
            {
                SetButtonText(newButton, selectingMold ? "Start Campaign" : "Start New Campaign");
                newButton.interactable = !selectingMold || selectedCampaignMoldIndex.HasValue;
            }

            if (backButton != null)
            {
                ConfigureButtonContent(backButton, "Back", BackButtonIcon);
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

        private void ConfigureCampaignContentWidth(bool useSetupWidth)
        {
            if (mainStackRoot == null)
            {
                return;
            }

            float width = useSetupWidth ? CampaignSetupWidth : PrimaryColumnWidth;
            mainStackRoot.sizeDelta = new Vector2(width, mainStackRoot.sizeDelta.y);
            LayoutElement element = mainStackRoot.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.minWidth = useSetupWidth ? CampaignSetupWidth : PrimaryColumnWidth - 40f;
                element.preferredWidth = width;
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
                moldSelectionTitleLabel.text = MoldSelectionTitleText;
            }

            if (moldSelectionStatusLabel != null)
            {
                moldSelectionStatusLabel.text = selectedCampaignMoldIndex.HasValue
                    ? $"Selected: {GetMoldDisplayName(selectedCampaignMoldIndex.Value)} • Used for this entire run"
                    : "Select one mold for this campaign run.";
            }

            RefreshCampaignStartDifficultyUi();
            RebuildMoldSelectionButtons();
        }

        private void RefreshCampaignStartDifficultyUi()
        {
            var campaignController = GameManager.Instance?.CampaignController;
            var options = campaignController?.GetCampaignStartDifficultyOptions() ?? Array.Empty<CampaignController.CampaignStartDifficultyOption>();

            if (campaignStartDifficultyTitleLabel != null)
            {
                campaignStartDifficultyTitleLabel.text = CampaignStartDifficultyTitleText;
            }

            if (options.Count == 0)
            {
                if (campaignStartDifficultyStatusLabel != null)
                {
                    campaignStartDifficultyStatusLabel.text = "Campaign progression data is unavailable.";
                }

                for (int i = 0; i < campaignStartDifficultyButtons.Count; i++)
                {
                    campaignStartDifficultyButtons[i].gameObject.SetActive(false);
                }

                return;
            }

            int highestUnlockedIndex = campaignController?.GetHighestUnlockedCampaignStartDifficultyIndex() ?? 0;
            highestUnlockedIndex = Mathf.Clamp(highestUnlockedIndex, 0, options.Count - 1);
            selectedCampaignStartDifficultyIndex = Mathf.Clamp(selectedCampaignStartDifficultyIndex, 0, highestUnlockedIndex);

            var selectedOption = options[selectedCampaignStartDifficultyIndex];
            string unlockStatus = highestUnlockedIndex + 1 < options.Count
                ? $"Next victorious full clear unlocks {options[highestUnlockedIndex + 1].Label}."
                : "All starting difficulties unlocked.";

            if (campaignStartDifficultyStatusLabel != null)
            {
                campaignStartDifficultyStatusLabel.text =
                    $"Selected start: {selectedOption.Label} (Level {selectedOption.StartLevelDisplay}). {unlockStatus}";
            }

            EnsureCampaignStartDifficultyButtonCount(options.Count);
            for (int optionIndex = 0; optionIndex < campaignStartDifficultyButtons.Count; optionIndex++)
            {
                bool shouldShow = optionIndex < options.Count;
                var button = campaignStartDifficultyButtons[optionIndex];
                button.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                bool isUnlocked = optionIndex <= highestUnlockedIndex;
                bool isSelected = isUnlocked && optionIndex == selectedCampaignStartDifficultyIndex;
                var option = options[optionIndex];

                UIStyleTokens.Startup.ApplyChoice(
                    button,
                    isSelected,
                    isUnlocked,
                    campaignStartDifficultyHighlights[optionIndex]);
                if (optionIndex < campaignStartDifficultyOutlines.Count)
                {
                    campaignStartDifficultyOutlines[optionIndex].enabled = isSelected;
                }

                campaignStartDifficultyLabels[optionIndex].text = BuildCampaignStartDifficultyCardText(
                    options,
                    optionIndex,
                    isUnlocked,
                    isSelected);

                var tooltipTrigger = button.GetComponent<TooltipTrigger>();
                if (tooltipTrigger != null)
                {
                    tooltipTrigger.SetStaticText(BuildCampaignStartDifficultyTooltipText(option, isUnlocked, isSelected));
                }

            }
        }

        private static string BuildCampaignStartDifficultyCardText(
            IReadOnlyList<CampaignController.CampaignStartDifficultyOption> options,
            int optionIndex,
            bool isUnlocked,
            bool isSelected)
        {
            var option = options[optionIndex];
            if (!isUnlocked)
            {
                string prerequisite = optionIndex > 0 ? options[optionIndex - 1].Label : "the prior difficulty";
                return $"LOCKED • {option.Label}\n<size=68%>Clear {prerequisite} to unlock</size>";
            }

            bool usesRandomDrafting = option.Difficulty == CampaignDifficulty.Training
                || option.Difficulty == CampaignDifficulty.Easy;
            string drafting = usesRandomDrafting ? "Random AI drafts" : "Smarter AI drafts";
            string start = option.Difficulty == CampaignDifficulty.Training
                ? "Full campaign"
                : $"Starts at Level {option.StartLevelDisplay}";
            string selectedMarker = isSelected ? "SELECTED • " : string.Empty;
            return $"{selectedMarker}{option.Label}\n<size=68%>{start}\n{drafting}</size>";
        }

        private static string BuildCampaignStartDifficultyTooltipText(
            CampaignController.CampaignStartDifficultyOption option,
            bool isUnlocked,
            bool isSelected)
        {
            bool isTraining = option.Difficulty == CampaignDifficulty.Training;
            bool usesRandomMycovariantDrafting = option.Difficulty == CampaignDifficulty.Training
                || option.Difficulty == CampaignDifficulty.Easy;
            string availability = isUnlocked
                ? (isSelected ? "Currently selected for this run." : "Unlocked and available now.")
                : "Locked until you clear the full campaign on your current highest unlocked start.";

            string startSummary = isTraining
                ? $"Starts a new campaign at <b>Level {option.StartLevelDisplay}</b>, using the normal authored board, opponents, and rewards."
                : $"Starts a new campaign at <b>Level {option.StartLevelDisplay}</b>, using the normal authored board, opponents, and rewards for that later point in the campaign.";
            string mycovariantDraftingSummary = usesRandomMycovariantDrafting
                ? "AI players draft Mycovariants randomly (instead of intelligently)."
                : "AI players draft Mycovariants more intelligently.";
            string skippedRewardsSummary = isTraining
                ? string.Empty
                : "You also do <b>not</b> retroactively earn the skipped levels' adaptation drafts or other victory rewards, so the deeper start is still tougher than a full run from Training.\n\n";

            return $"<b>{option.Label}</b>\n\n"
                + $"{startSummary}\n\n"
                + $"{mycovariantDraftingSummary}\n\n"
                + skippedRewardsSummary
                + availability;
        }

        private void ApplyMenuTooltips()
        {
            EnsureTooltip(resumeButton, GetResumeTooltipText);
            EnsureTooltip(newButton, GetNewButtonTooltipText);
            EnsureTooltip(backButton, GetBackButtonTooltipText);
        }

        private static void EnsureTooltip(Button button, string text)
        {
            EnsureTooltip(button, () => text);
        }

        private static void EnsureTooltip(Button button, Func<string> resolver)
        {
            if (button == null || resolver == null)
            {
                return;
            }

            var provider = button.GetComponent<MoldButtonTooltipProvider>();
            if (provider == null)
            {
                provider = button.gameObject.AddComponent<MoldButtonTooltipProvider>();
            }

            provider.Initialize(resolver);

            var trigger = button.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<TooltipTrigger>();
            }

            trigger.SetDynamicProvider(provider);
        }

        private static string GetResumeTooltipText()
        {
            return "Resume the current campaign save.";
        }

        private string GetNewButtonTooltipText()
        {
            if (currentStep == CampaignPanelStep.MainActions)
            {
                return HasPendingSporePreservation()
                    ? "Resume the saved campaign so you can resolve the pending preserve-spores choice first."
                    : "Start a new campaign. You will pick a mold icon and starting difficulty next.";
            }

            return selectedCampaignMoldIndex.HasValue
                ? "Start a new campaign with the selected mold icon and starting difficulty."
                : "Choose a mold icon before starting the campaign.";
        }

        private string GetBackButtonTooltipText()
        {
            return currentStep == CampaignPanelStep.MoldSelection
                ? "Return to the main campaign action menu."
                : "Back to the mode select menu.";
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
                UIStyleTokens.Startup.ApplyChoice(
                    button,
                    isSelected,
                    true,
                    moldSelectionHighlights[moldIndex]);
                if (moldIndex < moldSelectionOutlines.Count)
                {
                    moldSelectionOutlines[moldIndex].enabled = isSelected;
                }

                moldSelectionLabels[moldIndex].text = isSelected
                    ? $"Selected • {GetMoldDisplayName(moldIndex)}"
                    : GetMoldDisplayName(moldIndex);
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
                typeof(LayoutElement),
                typeof(Outline));
            buttonObject.transform.SetParent(moldSelectionGrid.transform, false);

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            UIStyleTokens.Button.ApplyStyle(button);
            int capturedIndex = moldIndex;
            button.onClick.AddListener(() => OnCampaignMoldSelected(capturedIndex));

            var element = buttonObject.GetComponent<LayoutElement>();
            element.minWidth = 132f;
            element.preferredWidth = 132f;
            element.minHeight = 122f;
            element.preferredHeight = 122f;

            var selectionOutline = buttonObject.GetComponent<Outline>();
            selectionOutline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Lichen, UIStyleTokens.Alpha.FocusOutline);
            selectionOutline.effectDistance = new Vector2(2f, -2f);
            selectionOutline.useGraphicAlpha = false;
            selectionOutline.enabled = false;

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
            iconRect.sizeDelta = new Vector2(68f, 68f);
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
            labelRect.sizeDelta = new Vector2(118f, 38f);
            labelRect.anchoredPosition = new Vector2(0f, 6f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 14f;
            label.alignment = TextAlignmentOptions.Center;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            moldSelectionButtons.Add(button);
            moldSelectionHighlights.Add(highlightImage);
            moldSelectionOutlines.Add(selectionOutline);
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

        private void OnCampaignStartDifficultySelected(int optionIndex)
        {
            selectedCampaignStartDifficultyIndex = Mathf.Max(0, optionIndex);
            UpdateStepState();
        }

        private int GetSelectedCampaignStartLevelIndex()
        {
            var campaignController = GameManager.Instance?.CampaignController;
            var options = campaignController?.GetCampaignStartDifficultyOptions() ?? Array.Empty<CampaignController.CampaignStartDifficultyOption>();
            if (options.Count == 0)
            {
                return 0;
            }

            int highestUnlockedIndex = campaignController?.GetHighestUnlockedCampaignStartDifficultyIndex() ?? 0;
            int resolvedIndex = Mathf.Clamp(selectedCampaignStartDifficultyIndex, 0, Mathf.Clamp(highestUnlockedIndex, 0, options.Count - 1));
            return options[resolvedIndex].StartLevelIndex;
        }

        private CampaignDifficulty GetSelectedCampaignStartDifficulty()
        {
            var campaignController = GameManager.Instance?.CampaignController;
            var options = campaignController?.GetCampaignStartDifficultyOptions() ?? Array.Empty<CampaignController.CampaignStartDifficultyOption>();
            if (options.Count == 0)
            {
                return CampaignDifficulty.Training;
            }

            int highestUnlockedIndex = campaignController?.GetHighestUnlockedCampaignStartDifficultyIndex() ?? 0;
            int resolvedIndex = Mathf.Clamp(selectedCampaignStartDifficultyIndex, 0, Mathf.Clamp(highestUnlockedIndex, 0, options.Count - 1));
            return options[resolvedIndex].Difficulty;
        }

        private int GetAvailableMoldCount()
        {
            return GameManager.Instance?.gridVisualizer != null ? GameManager.Instance.gridVisualizer.PlayerMoldTileCount : 0;
        }

        private Tile GetMoldTileAtIndex(int moldIndex)
        {
            var visualizer = GameManager.Instance?.gridVisualizer;
            return visualizer?.GetMoldIconTileForMoldIndex(moldIndex);
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

        private void ConfigureButtonContent(Button button, string text, Sprite icon)
        {
            if (button == null)
            {
                return;
            }

            if (TryConfigureCompoundButtonContent(button, text, icon))
            {
                return;
            }

            SetButtonText(button, text);
            SetDirectButtonLabelsActive(button.transform, true);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(label);
                label.margin = icon == null
                    ? Vector4.zero
                    : new Vector4(ActionButtonIconSize + ActionButtonContentSpacing + ActionButtonHorizontalPadding, 0f, 0f, 0f);
            }

            var legacyContent = button.transform.Find("ButtonContent");
            if (legacyContent != null)
            {
                legacyContent.gameObject.SetActive(false);
            }

            Image iconImage = EnsureButtonIcon(button.transform, icon);
            if (iconImage != null)
            {
                iconImage.color = label != null ? label.color : UIStyleTokens.Button.TextDefault;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
            }
        }

        private bool TryConfigureCompoundButtonContent(Button button, string text, Sprite icon)
        {
            RectTransform contentRoot = button.transform.Find("ButtonContent") as RectTransform;
            if (contentRoot == null)
            {
                return false;
            }

            contentRoot.gameObject.SetActive(true);
            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = Vector2.zero;

            SetDirectButtonLabelsActive(button.transform, false);
            EnsureCompoundButtonContentLayout(contentRoot);

            TextMeshProUGUI label = contentRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = text;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                TMPOverflowUtility.SetSafeEllipsis(label);
                label.margin = Vector4.zero;

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;
                label.transform.SetSiblingIndex(icon != null ? 1 : 0);
            }

            Image iconImage = EnsureCompoundButtonIcon(contentRoot, icon);
            if (iconImage != null)
            {
                iconImage.color = label != null ? label.color : UIStyleTokens.Button.TextDefault;
                iconImage.transform.SetSiblingIndex(0);
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
            }

            return true;
        }

        private static void EnsureCompoundButtonContentLayout(RectTransform contentRoot)
        {
            HorizontalLayoutGroup contentLayout = contentRoot.GetComponent<HorizontalLayoutGroup>();
            if (contentLayout == null)
            {
                contentLayout = contentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            contentLayout.spacing = ActionButtonContentSpacing;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childScaleWidth = false;
            contentLayout.childScaleHeight = false;

            ContentSizeFitter contentFitter = contentRoot.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
            {
                contentFitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private Image EnsureCompoundButtonIcon(RectTransform contentRoot, Sprite icon)
        {
            Transform iconTransform = contentRoot.Find("ButtonIcon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon == null)
            {
                if (iconImage != null)
                {
                    iconImage.gameObject.SetActive(false);
                }

                return null;
            }

            if (iconImage == null)
            {
                GameObject iconObject = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(contentRoot, false);
                iconObject.layer = gameObject.layer;
                iconImage = iconObject.GetComponent<Image>();
            }

            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.gameObject.SetActive(true);

            LayoutElement iconLayout = iconImage.GetComponent<LayoutElement>();
            if (iconLayout == null)
            {
                iconLayout = iconImage.gameObject.AddComponent<LayoutElement>();
            }

            iconLayout.minWidth = ActionButtonIconSize;
            iconLayout.preferredWidth = ActionButtonIconSize;
            iconLayout.minHeight = ActionButtonIconSize;
            iconLayout.preferredHeight = ActionButtonIconSize;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(ActionButtonIconSize, ActionButtonIconSize);
            return iconImage;
        }

        private Image EnsureButtonIcon(Transform buttonTransform, Sprite icon)
        {
            if (buttonTransform == null)
            {
                return null;
            }

            var iconTransform = buttonTransform.Find("ButtonIcon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon == null)
            {
                if (iconTransform != null)
                {
                    iconTransform.gameObject.SetActive(false);
                }

                return null;
            }

            if (iconImage == null)
            {
                GameObject iconObject = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(buttonTransform, false);
                iconObject.layer = gameObject.layer;

                iconImage = iconObject.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                var iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.minWidth = ActionButtonIconSize;
                iconLayout.preferredWidth = ActionButtonIconSize;
                iconLayout.minHeight = ActionButtonIconSize;
                iconLayout.preferredHeight = ActionButtonIconSize;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;

                var createdIconRect = iconObject.GetComponent<RectTransform>();
                createdIconRect.sizeDelta = new Vector2(ActionButtonIconSize, ActionButtonIconSize);
            }

            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.gameObject.SetActive(true);

            var existingLayout = iconImage.GetComponent<LayoutElement>();
            if (existingLayout != null)
            {
                existingLayout.minWidth = ActionButtonIconSize;
                existingLayout.preferredWidth = ActionButtonIconSize;
                existingLayout.minHeight = ActionButtonIconSize;
                existingLayout.preferredHeight = ActionButtonIconSize;
                existingLayout.flexibleWidth = 0f;
                existingLayout.flexibleHeight = 0f;
            }

            var iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(ActionButtonHorizontalPadding, 0f);
            iconRect.sizeDelta = new Vector2(ActionButtonIconSize, ActionButtonIconSize);
            return iconImage;
        }

        private static void SetDirectButtonLabelsActive(Transform buttonTransform, bool isActive)
        {
            if (buttonTransform == null)
            {
                return;
            }

            for (int index = 0; index < buttonTransform.childCount; index++)
            {
                var child = buttonTransform.GetChild(index);
                if (child == null || string.Equals(child.name, "ButtonContent", StringComparison.Ordinal) || string.Equals(child.name, "ButtonIcon", StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.GetComponent<TextMeshProUGUI>() != null || child.GetComponent<Text>() != null)
                {
                    child.gameObject.SetActive(isActive);
                }
            }
        }

        private void RefreshButtonStates()
        {
            bool hasSave = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();
            bool hasResumableCampaignSave = GameManager.Instance != null && GameManager.Instance.HasResumableCampaignSave();
            ApplyActionButtonSemantics();

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(hasResumableCampaignSave);
                resumeButton.interactable = hasResumableCampaignSave;
                UIStyleTokens.Button.SetButtonLabelColor(resumeButton, hasResumableCampaignSave ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
            }

            if (moldinessSummarySectionRoot != null)
            {
                moldinessSummarySectionRoot.gameObject.SetActive(hasSave && currentStep == CampaignPanelStep.MainActions);
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
                HideModeSelectBackground();
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
                        HideModeSelectBackground();
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
            GameManager.Instance.StartCampaignNew(
                selectedCampaignMoldIndex.Value,
                GetSelectedCampaignStartDifficulty(),
                GetSelectedCampaignStartLevelIndex());
            if (GameManager.Instance.Board != null && GameManager.Instance.CurrentGameMode == GameMode.Campaign)
            {
                HideModeSelectBackground();
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
                || (GameManager.Instance.CampaignController?.HasPendingMoldinessUnlockChoice ?? false)
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

        private void OnBackClicked()
        {
            if (currentStep == CampaignPanelStep.MoldSelection)
            {
                ReturnToActionStep();
                return;
            }

            gameObject.SetActive(false);
            ModeSelectController?.ShowMainMenuAfterSubpanel();
        }

        private void HideModeSelectBackground()
        {
            ModeSelectController?.HideForGameplay();
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
