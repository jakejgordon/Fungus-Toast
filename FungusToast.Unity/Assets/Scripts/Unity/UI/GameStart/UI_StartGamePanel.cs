using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Unity;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Testing;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System; // added for strict validation exceptions

namespace FungusToast.Unity.UI.GameStart
{
    public class UI_StartGamePanel : MonoBehaviour
    {
        private const int DefaultHotseatPlayerCount = 8;
        private const int DefaultHotseatHumanPlayerCount = 1;
        private const string AdvancedOptionsExpandedPrefsKey = "StartGame.AdvancedOptionsExpanded";
        private const string DevelopmentTestingEnabledPrefsKey = "StartGame.DevelopmentTestingEnabled";
        private const float StartMenuVerticalMargin = 24f;
        private const float ResponsiveScaleSafetyFactor = 0.97f;
        private const float StartMenuPrimaryColumnWidth = 500f;
        private const float StartMenuDevelopmentRailWidth = 340f;
        private const float StartMenuDevelopmentRailRightMargin = 48f;
        private const float StartMenuDevelopmentRailTopOffset = 0f;
        private const string HumanSelectionPrefix = "Select number of Human Players";

        private enum SetupStep
        {
            CountSelection,
            MoldSelection
        }

        public static UI_StartGamePanel Instance { get; private set; }

        [SerializeField] private List<UI_PlayerCountButton> playerButtons;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject modeSelectPanel;

        [Header("Human Players (Hotseat)")]
        [SerializeField] private GameObject humanPlayerSectionRoot; // container for human player selector (hidden until total picked)
        [SerializeField] private List<UI_HotseatHumanCountButton> humanPlayerButtons; // 1..8 reuse same prefab style
        [SerializeField] private TextMeshProUGUI playerSummaryLabel; // "X Players (Y Human / Z AI)"

        [Header("Legacy Testing Template")]
        [SerializeField] private GameObject testingOptionsSectionRoot;

        // Magnifying glass UI reference
        [SerializeField] private GameObject magnifyingGlassUI;
        // Magnifier visuals (child of magnifyingGlassUI)
        [SerializeField] private GameObject magnifierVisualRoot;

        private int? selectedPlayerCount = DefaultHotseatPlayerCount;
        private int selectedHumanPlayerCount = DefaultHotseatHumanPlayerCount;
        public int SelectedHumanPlayerCount => selectedHumanPlayerCount; // expose for future game manager refactor
        private DevelopmentTestingCardController testingCardController;
        private RectTransform setupContentRoot;
        private RectTransform developmentTestingAnchorRoot;
        private RectTransform developmentTestingRailRoot;
        private RectTransform titleSectionRoot;
        private RectTransform playerCountSectionRoot;
        private RectTransform advancedSettingsSectionRoot;
        private RectTransform advancedSettingsContentRoot;
        private RectTransform boardSizeSectionRoot;
        private RectTransform audioSettingsSectionRoot;
        private RectTransform audioSettingsContentRoot;
        private RectTransform testingCardSectionRoot;
        private RectTransform actionButtonStackRoot;
        private RectTransform moldSelectionSectionRoot;
        private TMP_Dropdown boardSizeDropdown;
        private int selectedBoardSize = DevelopmentTestingBoardSizePresets.DefaultSize;
        private TextMeshProUGUI setupTitleLabel;
        private string defaultTitleText;
        private TextMeshProUGUI moldSelectionTitleLabel;
        private TextMeshProUGUI moldSelectionStatusLabel;
        private GridLayoutGroup moldSelectionGrid;
        private readonly List<Button> moldSelectionButtons = new();
        private readonly List<Image> moldSelectionHighlights = new();
        private readonly List<Image> moldSelectionIcons = new();
        private readonly List<RectTransform> moldSelectionIconRects = new();
        private readonly List<Vector2> moldSelectionIconBasePositions = new();
        private readonly List<Vector2> moldSelectionIconCurrentOffsets = new();
        private readonly List<Vector2> moldSelectionIconTargetOffsets = new();
        private readonly List<float> moldSelectionIconNextMoveTimes = new();
        private readonly List<TextMeshProUGUI> moldSelectionLabels = new();
        private readonly List<int?> selectedHumanMoldIndices = new();
        private Button advancedSettingsToggleButton;
        private Button soundEffectsVolumeButton;
        private Button musicVolumeButton;
        private SetupStep currentStep = SetupStep.CountSelection;
        private int currentHumanMoldSelectionIndex;
        private bool isAdvancedOptionsExpanded = true;
        private Coroutine deferredLayoutRefreshCoroutine;

        private void Awake()
        {
            Instance = this;
            ResolveTestingModeReferences();
            // Strict validation: all required refs must be assigned in Inspector
            ValidateSerializedRefs();

            EnsureRuntimeLayoutScaffold();
            ApplyStyle();
            EnsureBoardSizeSection();
            InitializeTestingCard();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);

            startGameButton.interactable = false;
            InitializeHumanPlayerUI();
            EnsureMoldSelectionSection();
            UpdateSetupStepState();

            // Ensure magnifier visuals are disabled at startup
            if (magnifierVisualRoot != null)
                magnifierVisualRoot.SetActive(false);
        }

        private void OnEnable()
        {
            ResetSelectionState();
            EnsureRuntimeLayoutScaffold();
            testingCardController?.RefreshDropdownOptions();
            LoadPersistedMenuState();
            RefreshAudioSettingsControls();
            RefreshTestingSectionLayout();
            UpdateSetupStepState();
            RefreshStartMenuLayout();
        }

        private void OnDisable()
        {
            SavePersistedMenuState();
            ResetMoldSelectionIconIdleAnimation();

            if (deferredLayoutRefreshCoroutine != null)
            {
                StopCoroutine(deferredLayoutRefreshCoroutine);
                deferredLayoutRefreshCoroutine = null;
            }
        }

        private void Update()
        {
            UpdateMoldSelectionIconIdleAnimation();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshResponsiveStartLayout();
        }

        private void ResetSelectionState()
        {
            selectedPlayerCount = DefaultHotseatPlayerCount;
            selectedHumanPlayerCount = DefaultHotseatHumanPlayerCount;
            ResetMoldSelectionState();
            ConfigureHumanPlayerButtons();
            UpdateButtonVisuals();
            UpdateHumanPlayerButtonVisuals();
            UpdatePlayerSummaryLabel();
        }

        private void ValidateSerializedRefs()
        {
            if (startGameButton == null) throw new InvalidOperationException("UI_StartGamePanel: startGameButton is not assigned.");
            if (testingOptionsSectionRoot == null) Debug.LogWarning("UI_StartGamePanel: testingOptionsSectionRoot not assigned (will use legacy direct testing refs).");
            if (backButton == null) Debug.LogWarning("UI_StartGamePanel: backButton not assigned (menu back navigation disabled).");
            if (modeSelectPanel == null) Debug.LogWarning("UI_StartGamePanel: modeSelectPanel not assigned (menu back navigation disabled).");
            // Human player selection (soft validation: allow scene to run if not yet wired to avoid editor breakage)
            if (humanPlayerSectionRoot == null) Debug.LogWarning("UI_StartGamePanel: humanPlayerSectionRoot not assigned (hotseat selector will not show).");
            if (playerSummaryLabel == null) Debug.LogWarning("UI_StartGamePanel: playerSummaryLabel not assigned.");
        }

        private void ResolveTestingModeReferences()
        {
            var sectionRoot = testingOptionsSectionRoot;
            if (sectionRoot == null)
            {
                var found = transform.Find("UI_TestingOptionsSection");
                if (found != null)
                {
                    sectionRoot = found.gameObject;
                    testingOptionsSectionRoot = sectionRoot;
                }
            }

            if (sectionRoot == null)
            {
                return;
            }

            EnsureTestingSectionLayout(sectionRoot);

        }

        private static void EnsureTestingSectionLayout(GameObject sectionRoot)
        {
            var rectTransform = sectionRoot.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y);
                rectTransform.anchorMax = new Vector2(1f, rectTransform.anchorMax.y);
                rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
                rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = sectionRoot.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 8f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = sectionRoot.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = sectionRoot.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = sectionRoot.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = -1f;
            layoutElement.minHeight = 40f;
            layoutElement.minWidth = 300f;
            layoutElement.preferredWidth = -1f;
            layoutElement.preferredHeight = 40f;

        }

        private void InitializeTestingCard()
        {
            if (testingOptionsSectionRoot == null)
            {
                return;
            }

            EnsureTestingCardSection();

            testingCardController = new DevelopmentTestingCardController(new DevelopmentTestingCardOptions
            {
                Parent = testingCardSectionRoot,
                ButtonTemplate = backButton != null ? backButton : startGameButton,
                DropdownTemplate = ResolveDropdownTemplate(),
                SupportsCampaignLevelSelection = false,
                SupportsBoardSizeOverride = false,
                SupportsForcedAdaptation = false,
                CardName = "UI_StartGameTestingCard",
                ControlPrefix = "UI_StartGameTesting",
                LogPrefix = "UI_StartGamePanel",
                LayoutInvalidated = RefreshTestingSectionLayout,
                TestingEnabledChanged = OnTestingEnabledChanged,
                UseCardBackground = false,
                UseSecondaryButtonStyle = true,
                CardWidth = StartMenuDevelopmentRailWidth - 24f,
                SettingWidth = StartMenuDevelopmentRailWidth - 48f
            });
            testingCardController.Build();
            HideLegacyTestingControls();
            EnsureRuntimeLayoutScaffold();
            RefreshTestingSectionLayout();
        }

        private void EnsureRuntimeLayoutScaffold()
        {
            var panelRect = GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var rootLayout = GetComponent<VerticalLayoutGroup>();
            if (rootLayout != null)
            {
                rootLayout.enabled = false;
            }

            if (setupContentRoot == null)
            {
                var existing = transform.Find("UI_StartGameContentRoot") as RectTransform;
                if (existing != null)
                {
                    setupContentRoot = existing;
                }
                else
                {
                    var contentRoot = new GameObject(
                        "UI_StartGameContentRoot",
                        typeof(RectTransform),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    setupContentRoot = contentRoot.GetComponent<RectTransform>();
                    setupContentRoot.SetParent(transform, false);
                }
            }

            ConfigureSetupContentRoot(setupContentRoot);
            ResolveSetupSectionReferences();
            EnsureAdvancedSettingsSection();
            EnsureBoardSizeSection();
            EnsureAudioSettingsSection();
            EnsureMoldSelectionSection();

            var orderedSections = new List<RectTransform>();
            TryAddSetupSection(orderedSections, titleSectionRoot);
            TryAddSetupSection(orderedSections, playerCountSectionRoot);
            TryAddSetupSection(orderedSections, humanPlayerSectionRoot != null ? humanPlayerSectionRoot.GetComponent<RectTransform>() : null);
            TryAddSetupSection(orderedSections, boardSizeSectionRoot);
            TryAddSetupSection(orderedSections, audioSettingsSectionRoot);
            TryAddSetupSection(orderedSections, moldSelectionSectionRoot);

            for (int index = 0; index < orderedSections.Count; index++)
            {
                var section = orderedSections[index];
                if (section == null)
                {
                    continue;
                }

                section.SetParent(setupContentRoot, false);
                section.SetSiblingIndex(index);
                ConfigureSetupSection(section);
            }

            EnsureActionButtonStack();
            ReparentActionButton(startGameButton, 0);
            ReparentActionButton(backButton, 1);
            actionButtonStackRoot.SetParent(setupContentRoot, false);
            actionButtonStackRoot.SetSiblingIndex(setupContentRoot.childCount - 1);
        }

        private void ResolveSetupSectionReferences()
        {
            titleSectionRoot ??= FindNamedRectTransform("UI_HowManyPlayersText");
            playerCountSectionRoot ??= FindNamedRectTransform("UI_PlayerCountButtonGroupWrapper");
            advancedSettingsSectionRoot ??= FindNamedRectTransform("UI_StartGameAdvancedSettingsSection");
            boardSizeSectionRoot ??= FindNamedRectTransform("UI_StartGameBoardSizeSection");
            testingCardSectionRoot ??= FindNamedRectTransform("UI_StartGameTestingSection");
            if (setupTitleLabel == null && titleSectionRoot != null)
            {
                setupTitleLabel = titleSectionRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (setupTitleLabel != null && string.IsNullOrWhiteSpace(defaultTitleText))
                {
                    defaultTitleText = setupTitleLabel.text;
                }
            }

            if (playerCountSectionRoot == null)
            {
                playerCountSectionRoot = GetTopLevelSection(playerButtons != null && playerButtons.Count > 0 ? playerButtons[0]?.transform : null) as RectTransform;
            }

            ConfigureCenteredButtonRow(playerCountSectionRoot, "UI_PlayerCountButtonGroup");
            ConfigureCenteredButtonRow(humanPlayerSectionRoot != null ? humanPlayerSectionRoot.GetComponent<RectTransform>() : null, "UI_HumanPlayerCountButtonGroup");
        }

        private void EnsureAdvancedSettingsSection()
        {
            var panelRoot = GetComponent<RectTransform>();
            if (panelRoot == null)
            {
                return;
            }

            EnsureDevelopmentTestingAnchor(panelRoot);

            if (developmentTestingAnchorRoot == null)
            {
                return;
            }

            if (advancedSettingsSectionRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameAdvancedSettingsSection");
                if (existing != null)
                {
                    advancedSettingsSectionRoot = existing;
                }
                else
                {
                    var sectionObject = new GameObject(
                        "UI_StartGameAdvancedSettingsSection",
                        typeof(RectTransform),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    advancedSettingsSectionRoot = sectionObject.GetComponent<RectTransform>();
                    advancedSettingsSectionRoot.SetParent(developmentTestingAnchorRoot, false);
                }
            }

            developmentTestingRailRoot = advancedSettingsSectionRoot;
            advancedSettingsSectionRoot.SetParent(developmentTestingAnchorRoot, false);
            ConfigureAdvancedSettingsSection(advancedSettingsSectionRoot);
            HideAdvancedSettingsToggleButton();
            EnsureAdvancedSettingsContentRoot();
        }

        private void EnsureDevelopmentTestingAnchor(RectTransform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (developmentTestingAnchorRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameDevelopmentTestingAnchor");
                if (existing != null)
                {
                    developmentTestingAnchorRoot = existing;
                }
                else
                {
                    var anchorObject = new GameObject(
                        "UI_StartGameDevelopmentTestingAnchor",
                        typeof(RectTransform),
                        typeof(LayoutElement));
                    developmentTestingAnchorRoot = anchorObject.GetComponent<RectTransform>();
                    developmentTestingAnchorRoot.SetParent(panelRoot, false);
                }
            }

            ConfigureDevelopmentTestingAnchorRoot(developmentTestingAnchorRoot);
        }

        private void EnsureAdvancedSettingsContentRoot()
        {
            if (advancedSettingsSectionRoot == null)
            {
                return;
            }

            if (advancedSettingsContentRoot == null)
            {
                var existing = advancedSettingsSectionRoot.Find("UI_StartGameAdvancedContent") as RectTransform;
                if (existing != null)
                {
                    advancedSettingsContentRoot = existing;
                }
                else
                {
                    var contentObject = new GameObject(
                        "UI_StartGameAdvancedContent",
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    advancedSettingsContentRoot = contentObject.GetComponent<RectTransform>();
                    advancedSettingsContentRoot.SetParent(advancedSettingsSectionRoot, false);
                }
            }

            advancedSettingsContentRoot.SetParent(advancedSettingsSectionRoot, false);
            advancedSettingsContentRoot.SetSiblingIndex(0);
            ConfigureAdvancedSettingsContentRoot(advancedSettingsContentRoot);
        }

        private void HideAdvancedSettingsToggleButton()
        {
            if (advancedSettingsSectionRoot == null)
            {
                return;
            }

            if (advancedSettingsToggleButton == null)
            {
                var existing = advancedSettingsSectionRoot.Find("UI_StartGameAdvancedToggleButton");
                if (existing != null)
                {
                    advancedSettingsToggleButton = existing.GetComponent<Button>();
                }
            }

            if (advancedSettingsToggleButton != null)
            {
                advancedSettingsToggleButton.gameObject.SetActive(false);
            }
        }

        private void ConfigureCenteredButtonRow(RectTransform sectionRoot, string rowName)
        {
            if (sectionRoot == null || string.IsNullOrWhiteSpace(rowName))
            {
                return;
            }

            var row = FindNamedRectTransform(rowName);
            if (row == null || row.parent != sectionRoot)
            {
                return;
            }

            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, row.anchoredPosition.y);
            row.localScale = Vector3.one;
        }

        private void EnsureTestingCardSection()
        {
            if (advancedSettingsContentRoot == null)
            {
                return;
            }

            if (testingCardSectionRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameTestingSection");
                if (existing != null)
                {
                    testingCardSectionRoot = existing;
                }
                else
                {
                    var sectionObject = new GameObject(
                        "UI_StartGameTestingSection",
                        typeof(RectTransform),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    testingCardSectionRoot = sectionObject.GetComponent<RectTransform>();
                    testingCardSectionRoot.SetParent(advancedSettingsContentRoot, false);
                }
            }

            testingCardSectionRoot.SetParent(advancedSettingsContentRoot, false);
            ConfigureTestingCardSection(testingCardSectionRoot);
        }

        private void EnsureBoardSizeSection()
        {
            if (setupContentRoot == null)
            {
                return;
            }

            if (boardSizeSectionRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameBoardSizeSection");
                if (existing != null)
                {
                    boardSizeSectionRoot = existing;
                }
                else
                {
                    var sectionObject = new GameObject(
                        "UI_StartGameBoardSizeSection",
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    boardSizeSectionRoot = sectionObject.GetComponent<RectTransform>();
                    boardSizeSectionRoot.SetParent(setupContentRoot, false);
                }
            }

            boardSizeSectionRoot.SetParent(setupContentRoot, false);
            ConfigureBoardSizeSection(boardSizeSectionRoot);
            EnsureBoardSizeLabel();
            EnsureBoardSizeDropdown();
            RefreshBoardSizeDropdown();
        }

        private void EnsureAudioSettingsSection()
        {
            if (setupContentRoot == null)
            {
                return;
            }

            if (audioSettingsSectionRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameAudioSettingsSection");
                if (existing != null)
                {
                    audioSettingsSectionRoot = existing;
                }
                else
                {
                    var sectionObject = new GameObject(
                        "UI_StartGameAudioSettingsSection",
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    audioSettingsSectionRoot = sectionObject.GetComponent<RectTransform>();
                    audioSettingsSectionRoot.SetParent(setupContentRoot, false);
                }
            }

            audioSettingsSectionRoot.SetParent(setupContentRoot, false);
            ConfigureAudioSettingsSection(audioSettingsSectionRoot);
            EnsureAudioSettingsContentRoot();
            EnsureAudioSettingsControls();
            RefreshAudioSettingsControls();
        }

        private void EnsureAudioSettingsContentRoot()
        {
            if (audioSettingsSectionRoot == null)
            {
                return;
            }

            if (audioSettingsContentRoot == null)
            {
                var existing = audioSettingsSectionRoot.Find("UI_StartGameAudioContent") as RectTransform;
                if (existing != null)
                {
                    audioSettingsContentRoot = existing;
                }
                else
                {
                    var contentObject = new GameObject(
                        "UI_StartGameAudioContent",
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    audioSettingsContentRoot = contentObject.GetComponent<RectTransform>();
                    audioSettingsContentRoot.SetParent(audioSettingsSectionRoot, false);
                }
            }

            audioSettingsContentRoot.SetParent(audioSettingsSectionRoot, false);
            audioSettingsContentRoot.SetSiblingIndex(0);
            ConfigureAudioSettingsContentRoot(audioSettingsContentRoot);
        }

        private void EnsureMoldSelectionSection()
        {
            if (setupContentRoot == null)
            {
                return;
            }

            if (moldSelectionSectionRoot == null)
            {
                var sectionObject = new GameObject(
                    "UI_MoldSelectionSection",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(LayoutElement));
                moldSelectionSectionRoot = sectionObject.GetComponent<RectTransform>();
                moldSelectionSectionRoot.SetParent(setupContentRoot, false);
            }

            ConfigureMoldSelectionSection(moldSelectionSectionRoot);
            EnsureMoldSelectionHeader();
            EnsureMoldSelectionGrid();
        }

        private void EnsureBoardSizeLabel()
        {
            if (boardSizeSectionRoot == null)
            {
                return;
            }

            var existing = boardSizeSectionRoot.Find("UI_StartGameBoardSizeLabel");
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject("UI_StartGameBoardSizeLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));

            if (existing == null)
            {
                labelObject.transform.SetParent(boardSizeSectionRoot, false);
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Board Size";
            label.color = UIStyleTokens.Text.Primary;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 20f;
            label.alignment = TextAlignmentOptions.Center;

            var element = labelObject.GetComponent<LayoutElement>();
            element.minWidth = 470f;
            element.preferredWidth = 470f;
            element.minHeight = 22f;
            element.preferredHeight = 26f;
        }

        private void EnsureBoardSizeDropdown()
        {
            if (boardSizeSectionRoot == null || boardSizeDropdown != null)
            {
                if (boardSizeDropdown != null)
                {
                    ConfigureBoardSizeDropdown();
                }

                return;
            }

            var existing = boardSizeSectionRoot.Find("UI_StartGameBoardSizeDropdown");
            if (existing != null)
            {
                boardSizeDropdown = existing.GetComponent<TMP_Dropdown>();
            }
            else
            {
                TMP_Dropdown template = ResolveDropdownTemplate();
                if (template == null)
                {
                    return;
                }

                var dropdownObject = Instantiate(template.gameObject, boardSizeSectionRoot);
                dropdownObject.name = "UI_StartGameBoardSizeDropdown";
                boardSizeDropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            }

            if (boardSizeDropdown == null)
            {
                return;
            }

            var dropdownElement = boardSizeDropdown.GetComponent<LayoutElement>();
            if (dropdownElement == null)
            {
                dropdownElement = boardSizeDropdown.gameObject.AddComponent<LayoutElement>();
            }

            dropdownElement.minHeight = 40f;
            dropdownElement.preferredHeight = 44f;
            dropdownElement.minWidth = 470f;
            dropdownElement.preferredWidth = 470f;

            ConfigureBoardSizeDropdown();
        }

        private void ConfigureMoldSelectionSection(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var surface = sectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = UIStyleTokens.Surface.PanelPrimary;
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(18, 18, 18, 18);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 12f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = 540f;
            element.preferredWidth = 540f;
            element.minHeight = 220f;
            element.preferredHeight = -1f;
        }

        private void EnsureMoldSelectionHeader()
        {
            if (moldSelectionSectionRoot == null)
            {
                return;
            }

            moldSelectionTitleLabel ??= CreateMoldSelectionText(
                "UI_MoldSelectionTitle",
                28f,
                FontStyles.Bold,
                UIStyleTokens.Text.Primary,
                TextAlignmentOptions.Center,
                40f);
            moldSelectionStatusLabel ??= CreateMoldSelectionText(
                "UI_MoldSelectionStatus",
                20f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                TextAlignmentOptions.Center,
                56f);
        }

        private TextMeshProUGUI CreateMoldSelectionText(
            string objectName,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment,
            float minHeight)
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
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(moldSelectionSectionRoot, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.color = color;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = string.Empty;

            var layoutElement = label.GetComponent<LayoutElement>();
            layoutElement.minWidth = 500f;
            layoutElement.preferredWidth = 500f;
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = -1f;

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
                var existing = moldSelectionSectionRoot.Find("UI_MoldSelectionGrid") as RectTransform;
                if (existing != null)
                {
                    moldSelectionGrid = existing.GetComponent<GridLayoutGroup>();
                }
                else
                {
                    var gridObject = new GameObject(
                        "UI_MoldSelectionGrid",
                        typeof(RectTransform),
                        typeof(GridLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    var gridRect = gridObject.GetComponent<RectTransform>();
                    gridRect.SetParent(moldSelectionSectionRoot, false);
                    moldSelectionGrid = gridObject.GetComponent<GridLayoutGroup>();
                }
            }

            moldSelectionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            moldSelectionGrid.constraintCount = 4;
            moldSelectionGrid.cellSize = new Vector2(116f, 116f);
            moldSelectionGrid.spacing = new Vector2(10f, 10f);
            moldSelectionGrid.childAlignment = TextAnchor.UpperCenter;
            moldSelectionGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            moldSelectionGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;

            var fitter = moldSelectionGrid.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = moldSelectionGrid.GetComponent<LayoutElement>();
            layoutElement.minWidth = 504f;
            layoutElement.preferredWidth = 504f;
            layoutElement.minHeight = 240f;
            layoutElement.preferredHeight = -1f;
        }

        private static void ConfigureTestingCardSection(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 0f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = StartMenuDevelopmentRailWidth;
            element.preferredWidth = StartMenuDevelopmentRailWidth;
            element.minHeight = 56f;
            element.preferredHeight = -1f;
        }

        private static void ConfigureBoardSizeSection(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var surface = sectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = Color.clear;
                surface.raycastTarget = false;
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 4f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = StartMenuPrimaryColumnWidth;
            element.preferredWidth = StartMenuPrimaryColumnWidth;
            element.minHeight = 62f;
            element.preferredHeight = -1f;
        }

        private void EnsureAudioSettingsControls()
        {
            if (audioSettingsContentRoot == null)
            {
                return;
            }

            EnsureAudioSettingsLabel();
            soundEffectsVolumeButton = EnsureAudioSettingsButton(
                soundEffectsVolumeButton,
                "UI_StartGameSoundEffectsVolumeButton",
                OnSoundEffectsVolumeClicked,
                1);
            musicVolumeButton = EnsureAudioSettingsButton(
                musicVolumeButton,
                "UI_StartGameMusicVolumeButton",
                OnMusicVolumeClicked,
                2);

            var legacyToggle = audioSettingsContentRoot.Find("UI_StartGameSoundEffectsToggleButton");
            if (legacyToggle != null)
            {
                legacyToggle.gameObject.SetActive(false);
            }
        }

        private void EnsureAdvancedSettingsControls()
        {
            if (advancedSettingsSectionRoot == null)
            {
                return;
            }

            advancedSettingsToggleButton = EnsureAdvancedSettingsButton(
                advancedSettingsToggleButton,
                "UI_StartGameAdvancedToggleButton",
                OnAdvancedSettingsToggleClicked,
                0);
        }

        private void EnsureAudioSettingsLabel()
        {
            if (audioSettingsContentRoot == null)
            {
                return;
            }

            var existing = audioSettingsContentRoot.Find("UI_StartGameAudioSettingsLabel");
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject("UI_StartGameAudioSettingsLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));

            if (existing == null)
            {
                labelObject.transform.SetParent(audioSettingsContentRoot, false);
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Audio";
            label.color = UIStyleTokens.Text.Primary;
            label.enableAutoSizing = false;
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Center;

            var element = labelObject.GetComponent<LayoutElement>();
            element.minWidth = 470f;
            element.preferredWidth = 470f;
            element.minHeight = 26f;
            element.preferredHeight = 30f;
        }

        private Button EnsureAudioSettingsButton(Button existingButton, string objectName, UnityEngine.Events.UnityAction onClick, int siblingIndex)
        {
            Button button = existingButton;
            if (button == null && audioSettingsContentRoot != null)
            {
                var existing = audioSettingsContentRoot.Find(objectName);
                if (existing != null)
                {
                    button = existing.GetComponent<Button>();
                }
            }

            if (button == null)
            {
                Button template = backButton != null ? backButton : startGameButton;
                var buttonObject = Instantiate(template.gameObject, audioSettingsContentRoot);
                buttonObject.name = objectName;
                button = buttonObject.GetComponent<Button>();
            }

            button.transform.SetParent(audioSettingsContentRoot, false);
            button.transform.SetSiblingIndex(siblingIndex);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
            EnsureActionButtonLayout(button);
            UIStyleTokens.Button.ApplySecondaryMenuAction(
                button,
                UIStyleTokens.Button.NarrowMenuActionWidth,
                UIStyleTokens.Button.NarrowMenuActionHeight,
                UIStyleTokens.Button.MinimumMenuActionHeight);
            return button;
        }

        private Button EnsureAdvancedSettingsButton(Button existingButton, string objectName, UnityEngine.Events.UnityAction onClick, int siblingIndex)
        {
            Button button = existingButton;
            if (button == null && advancedSettingsSectionRoot != null)
            {
                var existing = advancedSettingsSectionRoot.Find(objectName);
                if (existing != null)
                {
                    button = existing.GetComponent<Button>();
                }
            }

            if (button == null)
            {
                Button template = backButton != null ? backButton : startGameButton;
                var buttonObject = Instantiate(template.gameObject, advancedSettingsSectionRoot);
                buttonObject.name = objectName;
                button = buttonObject.GetComponent<Button>();
            }

            button.transform.SetParent(advancedSettingsSectionRoot, false);
            button.transform.SetSiblingIndex(siblingIndex);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
            EnsureDevelopmentTestingButtonLayout(button);
            UIStyleTokens.Button.ApplySecondaryMenuAction(
                button,
                StartMenuDevelopmentRailWidth,
                UIStyleTokens.Button.NarrowMenuActionHeight,
                UIStyleTokens.Button.MinimumMenuActionHeight);
            return button;
        }

        private static void ConfigureAdvancedSettingsSection(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0f, 1f);
            sectionRoot.anchorMax = new Vector2(0f, 1f);
            sectionRoot.pivot = new Vector2(0f, 1f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 8f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = StartMenuDevelopmentRailWidth;
            element.preferredWidth = StartMenuDevelopmentRailWidth;
            element.minHeight = 52f;
            element.preferredHeight = -1f;
        }

        private static void ConfigureDevelopmentTestingAnchorRoot(RectTransform anchorRoot)
        {
            if (anchorRoot == null)
            {
                return;
            }

            anchorRoot.anchorMin = new Vector2(1f, 1f);
            anchorRoot.anchorMax = new Vector2(1f, 1f);
            anchorRoot.pivot = new Vector2(1f, 1f);
            anchorRoot.anchoredPosition = Vector2.zero;
            anchorRoot.sizeDelta = new Vector2(StartMenuDevelopmentRailWidth, 0f);
            anchorRoot.localScale = Vector3.one;

            var element = anchorRoot.GetComponent<LayoutElement>();
            element.minWidth = StartMenuDevelopmentRailWidth;
            element.preferredWidth = StartMenuDevelopmentRailWidth;
            element.minHeight = 0f;
            element.preferredHeight = -1f;
        }

        private void RepositionDevelopmentTestingAnchor()
        {
            if (developmentTestingAnchorRoot == null)
            {
                return;
            }

            float anchorY = setupContentRoot != null
                ? setupContentRoot.anchoredPosition.y + StartMenuDevelopmentRailTopOffset
                : -36f;
            developmentTestingAnchorRoot.anchoredPosition = new Vector2(-StartMenuDevelopmentRailRightMargin, anchorY);
        }

        private static void ConfigureAdvancedSettingsContentRoot(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var surface = sectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                var panelColor = UIStyleTokens.Surface.PanelPrimary;
                panelColor.a = 0.92f;
                surface.color = panelColor;
                surface.raycastTarget = false;
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 8f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = StartMenuDevelopmentRailWidth;
            element.preferredWidth = StartMenuDevelopmentRailWidth;
            element.minHeight = 80f;
            element.preferredHeight = -1f;
        }

        private static void EnsureDevelopmentTestingButtonLayout(Button button)
        {
            UIStyleTokens.Button.ConfigureMenuActionLayout(
                button,
                StartMenuDevelopmentRailWidth,
                UIStyleTokens.Button.NarrowMenuActionHeight,
                UIStyleTokens.Button.MinimumMenuActionHeight);
        }

        private static void ConfigureAudioSettingsSection(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.localScale = Vector3.one;

            var surface = sectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = Color.clear;
                surface.raycastTarget = false;
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 8f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = 500f;
            element.preferredWidth = 500f;
            element.minHeight = 150f;
            element.preferredHeight = -1f;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        private static void ConfigureAudioSettingsContentRoot(RectTransform sectionRoot)
        {
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.anchorMin = new Vector2(0.5f, 1f);
            sectionRoot.anchorMax = new Vector2(0.5f, 1f);
            sectionRoot.pivot = new Vector2(0.5f, 0.5f);
            sectionRoot.anchoredPosition = Vector2.zero;
            sectionRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            sectionRoot.localScale = Vector3.one;

            var surface = sectionRoot.GetComponent<Image>();
            if (surface != null)
            {
                var panelColor = UIStyleTokens.Surface.PanelPrimary;
                panelColor.a = 0.92f;
                surface.color = panelColor;
                surface.raycastTarget = false;
            }

            var layoutGroup = sectionRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 8f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = sectionRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = sectionRoot.GetComponent<LayoutElement>();
            element.minWidth = 500f;
            element.preferredWidth = 500f;
            element.minHeight = 150f;
            element.preferredHeight = -1f;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        private RectTransform FindNamedRectTransform(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var allChildren = GetComponentsInChildren<RectTransform>(true);
            for (int index = 0; index < allChildren.Length; index++)
            {
                var child = allChildren[index];
                if (child != null && string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private void EnsureActionButtonStack()
        {
            if (setupContentRoot == null)
            {
                return;
            }

            if (actionButtonStackRoot == null)
            {
                var existing = FindNamedRectTransform("UI_StartGameActionStack");
                if (existing != null)
                {
                    actionButtonStackRoot = existing;
                }
                else
                {
                    var stackObject = new GameObject(
                        "UI_StartGameActionStack",
                        typeof(RectTransform),
                        typeof(VerticalLayoutGroup),
                        typeof(ContentSizeFitter),
                        typeof(LayoutElement));
                    actionButtonStackRoot = stackObject.GetComponent<RectTransform>();
                    actionButtonStackRoot.SetParent(setupContentRoot, false);
                }
            }

            actionButtonStackRoot.SetParent(setupContentRoot, false);
            actionButtonStackRoot.SetSiblingIndex(setupContentRoot.childCount - 1);
            ConfigureActionButtonStack(actionButtonStackRoot);
        }

        private static void ConfigureActionButtonStack(RectTransform stackRoot)
        {
            if (stackRoot == null)
            {
                return;
            }

            stackRoot.anchorMin = new Vector2(0.5f, 1f);
            stackRoot.anchorMax = new Vector2(0.5f, 1f);
            stackRoot.pivot = new Vector2(0.5f, 0.5f);
            stackRoot.anchoredPosition = Vector2.zero;
            stackRoot.localScale = Vector3.one;

            var layoutGroup = stackRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 10f;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            var fitter = stackRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = stackRoot.GetComponent<LayoutElement>();
            element.minWidth = 470f;
            element.preferredWidth = 470f;
            element.minHeight = 104f;
            element.preferredHeight = -1f;
        }

        private void ReparentActionButton(Button button, int siblingIndex)
        {
            if (button == null || actionButtonStackRoot == null)
            {
                return;
            }

            button.transform.SetParent(actionButtonStackRoot, false);
            button.transform.SetSiblingIndex(siblingIndex);
            ConfigureSetupSection(button.transform as RectTransform);
            EnsureActionButtonLayout(button);
        }

        private static void EnsureActionButtonLayout(Button button)
        {
            UIStyleTokens.Button.ConfigureMenuActionLayout(
                button,
                UIStyleTokens.Button.NarrowMenuActionWidth,
                UIStyleTokens.Button.NarrowMenuActionHeight,
                UIStyleTokens.Button.MinimumMenuActionHeight);
        }

        private static void ConfigureSetupContentRoot(RectTransform contentRoot)
        {
            if (contentRoot == null)
            {
                return;
            }

            contentRoot.anchorMin = new Vector2(0.5f, 1f);
            contentRoot.anchorMax = new Vector2(0.5f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = new Vector2(0f, -36f);
            contentRoot.sizeDelta = new Vector2(760f, 0f);
            contentRoot.localScale = Vector3.one;

            var layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(24, 24, 0, 24);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 14f;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;

            var fitter = contentRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = contentRoot.GetComponent<LayoutElement>();
            element.minWidth = 760f;
            element.preferredWidth = 760f;
            element.minHeight = 200f;
        }

        private static void ConfigureSetupSection(RectTransform section)
        {
            if (section == null)
            {
                return;
            }

            section.anchorMin = new Vector2(0.5f, 1f);
            section.anchorMax = new Vector2(0.5f, 1f);
            section.pivot = new Vector2(0.5f, 0.5f);
            section.anchoredPosition = Vector2.zero;
            section.localScale = Vector3.one;
        }

        private Transform GetTopLevelSection(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            var current = target;
            while (current.parent != null && current.parent != transform && current.parent != setupContentRoot)
            {
                current = current.parent;
            }

            if (current.parent == setupContentRoot && current != setupContentRoot)
            {
                return current;
            }

            return current.parent == transform ? current : null;
        }

        private void TryAddSetupSection(List<RectTransform> sections, RectTransform candidate)
        {
            if (candidate == null || sections == null)
            {
                return;
            }

            if (candidate == setupContentRoot)
            {
                return;
            }

            if (!sections.Contains(candidate))
            {
                sections.Add(candidate);
            }
        }

        private TMP_Dropdown ResolveDropdownTemplate()
        {
            if (testingOptionsSectionRoot != null)
            {
                return testingOptionsSectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
            }

            return FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
        }

        private void HideLegacyTestingControls()
        {
            if (testingOptionsSectionRoot == null)
            {
                return;
            }

            testingOptionsSectionRoot.SetActive(false);
        }

        private void InitializeHumanPlayerUI()
        {
            if (humanPlayerSectionRoot != null)
                humanPlayerSectionRoot.SetActive(false); // hidden until total player count chosen

            // Disable / hide all human player buttons initially
            if (humanPlayerButtons != null)
            {
                foreach (var btn in humanPlayerButtons)
                {
                    if (btn != null)
                        btn.gameObject.SetActive(false);
                }
            }
            UpdatePlayerSummaryLabel();
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);

            if (humanPlayerSectionRoot != null)
            {
                UIStyleTokens.ApplyPanelSurface(humanPlayerSectionRoot, UIStyleTokens.Surface.PanelPrimary);
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyPrimaryMenuAction(
                startGameButton,
                UIStyleTokens.Button.NarrowMenuActionWidth,
                useSelectedAsNormal: true,
                preferredHeight: UIStyleTokens.Button.NarrowMenuActionHeight,
                minHeight: UIStyleTokens.Button.MinimumMenuActionHeight);
            UIStyleTokens.Button.ApplySecondaryMenuAction(
                backButton,
                UIStyleTokens.Button.NarrowMenuActionWidth,
                UIStyleTokens.Button.NarrowMenuActionHeight,
                UIStyleTokens.Button.MinimumMenuActionHeight);

            if (playerSummaryLabel != null)
            {
                playerSummaryLabel.color = UIStyleTokens.Text.Secondary;
            }

            if (boardSizeDropdown != null)
            {
                ApplyDropdownReadability(boardSizeDropdown);
            }
        }

        private void UpdateSetupStepState()
        {
            EnsureMoldSelectionSection();

            bool isMoldSelectionStep = currentStep == SetupStep.MoldSelection;
            bool showDevelopmentTestingUi = !isMoldSelectionStep && ShouldShowDevelopmentTestingUi();
            if (playerCountSectionRoot != null)
            {
                playerCountSectionRoot.gameObject.SetActive(!isMoldSelectionStep);
            }

            if (humanPlayerSectionRoot != null)
            {
                humanPlayerSectionRoot.SetActive(!isMoldSelectionStep && selectedPlayerCount.HasValue);
            }

            if (developmentTestingRailRoot != null)
            {
                developmentTestingRailRoot.gameObject.SetActive(showDevelopmentTestingUi);
            }

            if (advancedSettingsSectionRoot != null)
            {
                advancedSettingsSectionRoot.gameObject.SetActive(showDevelopmentTestingUi);
            }

            if (advancedSettingsContentRoot != null)
            {
                advancedSettingsContentRoot.gameObject.SetActive(showDevelopmentTestingUi);
            }

            if (boardSizeSectionRoot != null)
            {
                boardSizeSectionRoot.gameObject.SetActive(!isMoldSelectionStep);
            }

            if (audioSettingsSectionRoot != null)
            {
                audioSettingsSectionRoot.gameObject.SetActive(!isMoldSelectionStep);
            }

            if (testingCardSectionRoot != null)
            {
                testingCardSectionRoot.gameObject.SetActive(showDevelopmentTestingUi);
            }

            if (moldSelectionSectionRoot != null)
            {
                moldSelectionSectionRoot.gameObject.SetActive(isMoldSelectionStep);
            }

            if (setupTitleLabel != null)
            {
                setupTitleLabel.text = isMoldSelectionStep ? "Pick Your Mold" : defaultTitleText;
            }

            if (isMoldSelectionStep)
            {
                RefreshMoldSelectionUi();
            }
            SetButtonText(startGameButton, isMoldSelectionStep ? GetMoldStepPrimaryButtonText() : "Start Game >");
            SetButtonText(backButton, isMoldSelectionStep ? "Back" : "Back");
            startGameButton.interactable = isMoldSelectionStep ? IsCurrentHumanMoldSelected() : selectedPlayerCount.HasValue;
            RefreshAudioSettingsControls();
            RefreshTestingSectionLayout();
            RefreshStartMenuLayout();
        }

        private void OnAdvancedSettingsToggleClicked()
        {
            isAdvancedOptionsExpanded = !isAdvancedOptionsExpanded;

            ApplyAdvancedVisibility();
            SavePersistedMenuState();
            UpdateSetupStepState();
        }

        private void OnSoundEffectsVolumeClicked()
        {
            SoundEffectsSettings.CycleVolumeForward();
            RefreshAudioSettingsControls();
        }

        private void OnMusicVolumeClicked()
        {
            MusicSettings.CycleVolumeForward();
            RefreshAudioSettingsControls();
        }

        private void RefreshAudioSettingsControls()
        {
            SetButtonText(soundEffectsVolumeButton, $"SFX Volume: {Mathf.RoundToInt(SoundEffectsSettings.Volume * 100f)}%");
            SetButtonText(musicVolumeButton, $"Music Volume: {Mathf.RoundToInt(MusicSettings.Volume * 100f)}%");
        }

        private void RefreshAdvancedSettingsControls()
        {
            SetButtonText(
                advancedSettingsToggleButton,
                isAdvancedOptionsExpanded
                    ? "Hide Development Testing Options"
                    : "Show Development Testing Options");
        }

        private void OnTestingEnabledChanged(bool isEnabled)
        {
            SavePersistedMenuState();
            UpdateSetupStepState();
        }

        private void LoadPersistedMenuState()
        {
            bool testingEnabled = PlayerPrefs.GetInt(DevelopmentTestingEnabledPrefsKey, 0) != 0;
            isAdvancedOptionsExpanded = true;
            testingCardController?.SetTestingEnabled(testingEnabled);
            ApplyAdvancedVisibility();
        }

        private void SavePersistedMenuState()
        {
            bool testingEnabled = testingCardController != null && testingCardController.IsTestingEnabled;
            PlayerPrefs.SetInt(AdvancedOptionsExpandedPrefsKey, 1);
            PlayerPrefs.SetInt(DevelopmentTestingEnabledPrefsKey, testingEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyAdvancedVisibility()
        {
            bool showAdvancedContent = currentStep == SetupStep.CountSelection && ShouldShowDevelopmentTestingUi();

            if (developmentTestingRailRoot != null)
            {
                developmentTestingRailRoot.gameObject.SetActive(currentStep == SetupStep.CountSelection && ShouldShowDevelopmentTestingUi());
            }

            if (advancedSettingsContentRoot != null)
            {
                advancedSettingsContentRoot.gameObject.SetActive(showAdvancedContent);
            }

            if (testingCardSectionRoot != null)
            {
                testingCardSectionRoot.gameObject.SetActive(showAdvancedContent);
            }
        }

        private static bool ShouldShowDevelopmentTestingUi()
        {
            return true;
        }

        private void ResetMoldSelectionState()
        {
            currentStep = SetupStep.CountSelection;
            currentHumanMoldSelectionIndex = 0;
            selectedHumanMoldIndices.Clear();
        }

        private void EnterMoldSelectionStep()
        {
            int totalPlayers = selectedPlayerCount ?? 0;
            int availableMoldCount = GetAvailableMoldCount();
            if (totalPlayers <= 0 || availableMoldCount <= 0)
            {
                StartConfiguredGame();
                return;
            }

            selectedHumanPlayerCount = Mathf.Clamp(selectedHumanPlayerCount, 1, Mathf.Min(totalPlayers, availableMoldCount));
            if (selectedHumanMoldIndices.Count != selectedHumanPlayerCount)
            {
                selectedHumanMoldIndices.Clear();
                for (int i = 0; i < selectedHumanPlayerCount; i++)
                {
                    selectedHumanMoldIndices.Add(null);
                }
            }

            currentStep = SetupStep.MoldSelection;
            currentHumanMoldSelectionIndex = FindNextHumanWithoutSelection();
            AssignDefaultMoldSelectionForCurrentHuman();
            UpdateSetupStepState();
        }

        private void ReturnToCountSelectionStep()
        {
            currentStep = SetupStep.CountSelection;
            currentHumanMoldSelectionIndex = 0;
            UpdateSetupStepState();
        }

        private int FindNextHumanWithoutSelection()
        {
            for (int i = 0; i < selectedHumanMoldIndices.Count; i++)
            {
                if (!selectedHumanMoldIndices[i].HasValue)
                {
                    return i;
                }
            }

            return Mathf.Max(0, selectedHumanMoldIndices.Count - 1);
        }

        private void AssignDefaultMoldSelectionForCurrentHuman()
        {
            if (currentHumanMoldSelectionIndex < 0 || currentHumanMoldSelectionIndex >= selectedHumanMoldIndices.Count)
            {
                return;
            }

            if (selectedHumanMoldIndices[currentHumanMoldSelectionIndex].HasValue)
            {
                return;
            }

            int defaultMoldIndex = FindFirstAvailableMoldIndex();
            if (defaultMoldIndex >= 0)
            {
                selectedHumanMoldIndices[currentHumanMoldSelectionIndex] = defaultMoldIndex;
            }
        }

        private int FindFirstAvailableMoldIndex()
        {
            int availableMoldCount = GetAvailableMoldCount();
            for (int moldIndex = 0; moldIndex < availableMoldCount; moldIndex++)
            {
                if (!IsMoldTakenByOtherHuman(moldIndex))
                {
                    return moldIndex;
                }
            }

            return -1;
        }

        private void RefreshMoldSelectionUi()
        {
            if (moldSelectionTitleLabel != null)
            {
                moldSelectionTitleLabel.text = "Choose a unique mold icon for each human player.";
            }

            if (moldSelectionStatusLabel != null)
            {
                moldSelectionStatusLabel.text = BuildMoldSelectionStatusText();
            }

            RebuildMoldSelectionButtons();
        }

        private string BuildMoldSelectionStatusText()
        {
            int humanNumber = currentHumanMoldSelectionIndex + 1;
            string currentChoice = "None selected yet";
            if (currentHumanMoldSelectionIndex >= 0
                && currentHumanMoldSelectionIndex < selectedHumanMoldIndices.Count
                && selectedHumanMoldIndices[currentHumanMoldSelectionIndex].HasValue)
            {
                currentChoice = GetMoldDisplayName(selectedHumanMoldIndices[currentHumanMoldSelectionIndex].Value);
            }

            return $"Human {humanNumber} of {selectedHumanPlayerCount} is choosing. Current selection: {currentChoice}.";
        }

        private void RebuildMoldSelectionButtons()
        {
            if (moldSelectionGrid == null)
            {
                return;
            }

            int availableMoldCount = GetAvailableMoldCount();
            EnsureMoldSelectionButtonCount(availableMoldCount);

            for (int moldIndex = 0; moldIndex < moldSelectionButtons.Count; moldIndex++)
            {
                bool shouldShow = moldIndex < availableMoldCount;
                var button = moldSelectionButtons[moldIndex];
                button.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var tile = GetMoldTileAtIndex(moldIndex);
                if (moldIndex < moldSelectionIcons.Count)
                {
                    moldSelectionIcons[moldIndex].sprite = tile != null ? tile.sprite : null;
                    moldSelectionIcons[moldIndex].enabled = tile != null && tile.sprite != null;
                }

                if (moldIndex < moldSelectionIconRects.Count)
                {
                    var iconRect = moldSelectionIconRects[moldIndex];
                    if (iconRect != null)
                    {
                        iconRect.gameObject.SetActive(tile != null && tile.sprite != null);
                    }
                }

                bool isSelected = currentHumanMoldSelectionIndex >= 0
                    && currentHumanMoldSelectionIndex < selectedHumanMoldIndices.Count
                    && selectedHumanMoldIndices[currentHumanMoldSelectionIndex] == moldIndex;
                bool isTakenByOtherHuman = IsMoldTakenByOtherHuman(moldIndex);

                button.interactable = !isTakenByOtherHuman || isSelected;

                if (moldIndex < moldSelectionHighlights.Count)
                {
                    moldSelectionHighlights[moldIndex].enabled = isSelected;
                    moldSelectionHighlights[moldIndex].gameObject.SetActive(isSelected);
                }

                if (moldIndex < moldSelectionLabels.Count)
                {
                    moldSelectionLabels[moldIndex].text = isTakenByOtherHuman && !isSelected ? "Taken" : GetMoldDisplayName(moldIndex);
                    moldSelectionLabels[moldIndex].color = button.interactable ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled;
                }
            }
        }

        private static string GetMoldDisplayName(int moldIndex)
        {
            return MoldCatalog.GetDisplayName(moldIndex);
        }

        private string BuildMoldTooltipText(int moldIndex)
        {
            string moldName = MoldCatalog.GetDisplayName(moldIndex);
            string adaptationId = MoldCatalog.GetStartingAdaptationId(moldIndex);
            if (!AdaptationRepository.TryGetById(adaptationId, out var adaptation))
            {
                return $"<b>{moldName}</b>";
            }

            string description = AdaptationRepository.GetTooltipDescription(adaptation, selectedBoardSize);
            return $"<b>{moldName}</b>\n\n<b>Starting Adaptation: {adaptation.Name}</b>\n{description}";
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
                $"UI_MoldButton_{moldIndex + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(moldSelectionGrid.transform, false);

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            UIStyleTokens.Button.ApplyStyle(button);
            int capturedIndex = moldIndex;
            button.onClick.AddListener(() => OnMoldOptionSelected(capturedIndex));

            var layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = 116f;
            layoutElement.preferredWidth = 116f;
            layoutElement.minHeight = 116f;
            layoutElement.preferredHeight = 116f;

            var highlightObject = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            var highlightRect = highlightObject.GetComponent<RectTransform>();
            highlightRect.SetParent(buttonRect, false);
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
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(buttonRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(58f, 58f);
            iconRect.anchoredPosition = new Vector2(0f, 14f);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(102f, 34f);
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
            moldSelectionIconRects.Add(iconRect);
            moldSelectionIconBasePositions.Add(iconRect.anchoredPosition);
            moldSelectionIconCurrentOffsets.Add(Vector2.zero);
            moldSelectionIconTargetOffsets.Add(GetStartMenuMoldIconIdleOffset(moldIndex, 0));
            moldSelectionIconNextMoveTimes.Add(Time.unscaledTime + GetStartMenuMoldIconIdleMoveInterval(moldIndex, 0));
            moldSelectionLabels.Add(label);

            var tooltipProvider = buttonObject.AddComponent<MoldButtonTooltipProvider>();
            tooltipProvider.Initialize(() => BuildMoldTooltipText(moldIndex));
            buttonObject.AddComponent<TooltipTrigger>();
        }

        private void UpdateMoldSelectionIconIdleAnimation()
        {
            if (moldSelectionSectionRoot == null || !moldSelectionSectionRoot.gameObject.activeInHierarchy)
            {
                ResetMoldSelectionIconIdleAnimation();
                return;
            }

            if (currentStep != SetupStep.MoldSelection)
            {
                ResetMoldSelectionIconIdleAnimation();
                return;
            }

            float now = Time.unscaledTime;
            float lerpFactor = 1f - Mathf.Exp(-UIEffectConstants.StartMenuMoldIconIdleLerpSpeed * Time.unscaledDeltaTime);
            for (int i = 0; i < moldSelectionIconRects.Count; i++)
            {
                var iconRect = moldSelectionIconRects[i];
                if (iconRect == null || !iconRect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (i >= moldSelectionIcons.Count || !moldSelectionIcons[i].enabled)
                {
                    iconRect.anchoredPosition = moldSelectionIconBasePositions[i];
                    moldSelectionIconCurrentOffsets[i] = Vector2.zero;
                    continue;
                }

                if (now >= moldSelectionIconNextMoveTimes[i])
                {
                    int step = Mathf.Max(1, Mathf.FloorToInt(now / Mathf.Max(0.01f, UIEffectConstants.StartMenuMoldIconIdleMoveMinSeconds)));
                    moldSelectionIconTargetOffsets[i] = GetStartMenuMoldIconIdleOffset(i, step);
                    moldSelectionIconNextMoveTimes[i] = now + GetStartMenuMoldIconIdleMoveInterval(i, step);
                }

                Vector2 currentOffset = Vector2.Lerp(moldSelectionIconCurrentOffsets[i], moldSelectionIconTargetOffsets[i], lerpFactor);
                moldSelectionIconCurrentOffsets[i] = currentOffset;
                iconRect.anchoredPosition = moldSelectionIconBasePositions[i] + currentOffset;
            }
        }

        private void ResetMoldSelectionIconIdleAnimation()
        {
            for (int i = 0; i < moldSelectionIconRects.Count; i++)
            {
                var iconRect = moldSelectionIconRects[i];
                if (iconRect == null)
                {
                    continue;
                }

                iconRect.anchoredPosition = moldSelectionIconBasePositions[i];
                moldSelectionIconCurrentOffsets[i] = Vector2.zero;
            }
        }

        private static Vector2 GetStartMenuMoldIconIdleOffset(int moldIndex, int step)
        {
            float shift = UIEffectConstants.StartMenuMoldIconIdleShiftPixels;
            float offsetX = Mathf.Lerp(-shift, shift, GetStartMenuMoldIconNoise01(moldIndex, step, 17u));
            float offsetY = Mathf.Lerp(-shift, shift, GetStartMenuMoldIconNoise01(moldIndex, step, 43u));
            return new Vector2(offsetX, offsetY);
        }

        private static float GetStartMenuMoldIconIdleMoveInterval(int moldIndex, int step)
        {
            float minSeconds = Mathf.Max(0.05f, UIEffectConstants.StartMenuMoldIconIdleMoveMinSeconds);
            float maxSeconds = Mathf.Max(minSeconds, UIEffectConstants.StartMenuMoldIconIdleMoveMaxSeconds);
            return Mathf.Lerp(minSeconds, maxSeconds, GetStartMenuMoldIconNoise01(moldIndex, step, 71u));
        }

        private static float GetStartMenuMoldIconNoise01(int moldIndex, int step, uint salt)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)moldIndex) * 16777619u;
                hash = (hash ^ (uint)step) * 16777619u;
                hash = (hash ^ salt) * 16777619u;
                return (hash & 65535u) / 65535f;
            }
        }

        private void OnMoldOptionSelected(int moldIndex)
        {
            if (currentHumanMoldSelectionIndex < 0 || currentHumanMoldSelectionIndex >= selectedHumanMoldIndices.Count)
            {
                return;
            }

            selectedHumanMoldIndices[currentHumanMoldSelectionIndex] = moldIndex;
            UpdateSetupStepState();
        }

        private bool IsCurrentHumanMoldSelected()
        {
            return currentHumanMoldSelectionIndex >= 0
                && currentHumanMoldSelectionIndex < selectedHumanMoldIndices.Count
                && selectedHumanMoldIndices[currentHumanMoldSelectionIndex].HasValue;
        }

        private bool IsMoldTakenByOtherHuman(int moldIndex)
        {
            for (int i = 0; i < selectedHumanMoldIndices.Count; i++)
            {
                if (i == currentHumanMoldSelectionIndex)
                {
                    continue;
                }

                if (selectedHumanMoldIndices[i] == moldIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private Tile GetMoldTileAtIndex(int moldIndex)
        {
            var visualizer = GameManager.Instance != null ? GameManager.Instance.gridVisualizer : null;
            if (visualizer?.playerMoldTiles == null || moldIndex < 0 || moldIndex >= visualizer.playerMoldTiles.Length)
            {
                return null;
            }

            return visualizer.playerMoldTiles[moldIndex];
        }

        private int GetAvailableMoldCount()
        {
            var visualizer = GameManager.Instance != null ? GameManager.Instance.gridVisualizer : null;
            return visualizer != null ? visualizer.PlayerMoldTileCount : 0;
        }

        private string GetMoldStepPrimaryButtonText()
        {
            return currentHumanMoldSelectionIndex >= selectedHumanPlayerCount - 1 ? "Start Game" : "Next Player";
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

        private void AdvanceMoldSelectionOrStartGame()
        {
            if (!IsCurrentHumanMoldSelected())
            {
                UpdateSetupStepState();
                return;
            }

            if (currentHumanMoldSelectionIndex < selectedHumanPlayerCount - 1)
            {
                currentHumanMoldSelectionIndex++;
                AssignDefaultMoldSelectionForCurrentHuman();
                UpdateSetupStepState();
                return;
            }

            StartConfiguredGame();
        }

        private void StartConfiguredGame()
        {
            if (!selectedPlayerCount.HasValue)
            {
                return;
            }

            var manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            var selectedMolds = new List<int>();
            for (int i = 0; i < selectedHumanMoldIndices.Count; i++)
            {
                if (selectedHumanMoldIndices[i].HasValue)
                {
                    selectedMolds.Add(selectedHumanMoldIndices[i].Value);
                }
            }

            manager.SetHotseatConfig(selectedHumanPlayerCount, selectedMolds);
            ApplySoloBoardSize(manager);
            testingCardController?.ApplyToGameManager(manager);

            manager.InitializeGame(selectedPlayerCount.Value);
            manager.cameraCenterer.CenterCameraSmooth();
            gameObject.SetActive(false);

            if (magnifyingGlassUI != null)
            {
                magnifyingGlassUI.SetActive(true);
            }

            if (magnifierVisualRoot != null)
            {
                magnifierVisualRoot.SetActive(true);
            }

            MagnifyingGlassFollowMouse.gameStarted = true;
        }

        private void ConfigureBoardSizeDropdown()
        {
            if (boardSizeDropdown == null)
            {
                return;
            }

            boardSizeDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            boardSizeDropdown.onValueChanged.AddListener(OnBoardSizeSelected);
            ApplyDropdownReadability(boardSizeDropdown);
        }

        private void RefreshBoardSizeDropdown()
        {
            if (boardSizeDropdown == null)
            {
                return;
            }

            selectedBoardSize = DevelopmentTestingBoardSizePresets.ClampToSupportedSize(selectedBoardSize);
            boardSizeDropdown.ClearOptions();
            boardSizeDropdown.AddOptions(DevelopmentTestingBoardSizePresets.BuildLabels());
            boardSizeDropdown.value = DevelopmentTestingBoardSizePresets.GetIndex(selectedBoardSize);
            boardSizeDropdown.RefreshShownValue();
            ApplyDropdownReadability(boardSizeDropdown);
        }

        private void OnBoardSizeSelected(int index)
        {
            selectedBoardSize = DevelopmentTestingBoardSizePresets.GetSizeAt(index);
            RefreshTestingSectionLayout();
        }

        private static void ApplySoloBoardSize(GameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            int boardSize = Instance != null
                ? DevelopmentTestingBoardSizePresets.ClampToSupportedSize(Instance.selectedBoardSize)
                : GameBalance.BoardWidth;

            manager.boardWidth = boardSize;
            manager.boardHeight = boardSize;
        }

        private static void ApplyDropdownReadability(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = UIStyleTokens.Button.TextDefault;
                dropdown.captionText.enableAutoSizing = false;
                dropdown.captionText.fontSize = 18f;
                dropdown.captionText.fontSizeMin = 18f;
                dropdown.captionText.fontSizeMax = 18f;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = UIStyleTokens.Button.TextDefault;
                dropdown.itemText.enableAutoSizing = false;
                dropdown.itemText.fontSize = 18f;
                dropdown.itemText.fontSizeMin = 18f;
                dropdown.itemText.fontSizeMax = 18f;
            }

            if (dropdown.template != null)
            {
                var scrollRect = dropdown.template.GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null)
                {
                    scrollRect.scrollSensitivity = 1.5f;
                }

                var templateLabels = dropdown.template.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int index = 0; index < templateLabels.Length; index++)
                {
                    var templateLabel = templateLabels[index];
                    if (templateLabel == null)
                    {
                        continue;
                    }

                    templateLabel.enableAutoSizing = false;
                    templateLabel.fontSize = 18f;
                    templateLabel.fontSizeMin = 18f;
                    templateLabel.fontSizeMax = 18f;
                    templateLabel.color = UIStyleTokens.Button.TextDefault;
                }
            }
        }

        private void RefreshTestingSectionLayout()
        {
            if (testingCardSectionRoot == null)
            {
                RepositionDevelopmentTestingAnchor();
                return;
            }

            var sectionRect = testingCardSectionRoot;
            if (sectionRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sectionRect);

                if (sectionRect.parent is RectTransform parentRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

                    if (parentRect.parent is RectTransform grandparentRect)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(grandparentRect);
                    }
                }
            }

            RepositionDevelopmentTestingAnchor();
        }

        private void RefreshStartMenuLayout()
        {
            if (setupContentRoot != null)
            {
                setupContentRoot.localScale = Vector3.one;
            }

            Canvas.ForceUpdateCanvases();

            if (moldSelectionSectionRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(moldSelectionSectionRoot);
            }

            if (advancedSettingsContentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(advancedSettingsContentRoot);
            }

            if (developmentTestingRailRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(developmentTestingRailRoot);
            }

            if (advancedSettingsSectionRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(advancedSettingsSectionRoot);
            }

            if (audioSettingsSectionRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(audioSettingsSectionRoot);
            }

            if (actionButtonStackRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(actionButtonStackRoot);
            }

            if (setupContentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(setupContentRoot);
            }

            var panelRect = GetComponent<RectTransform>();
            if (panelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }

            Canvas.ForceUpdateCanvases();

            RepositionDevelopmentTestingAnchor();
            RefreshResponsiveStartLayout();
            ScheduleDeferredLayoutRefresh();
        }

        private void RefreshResponsiveStartLayout()
        {
            if (setupContentRoot == null)
            {
                return;
            }

            RectTransform panelRect = GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            setupContentRoot.localScale = Vector3.one;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(setupContentRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            Canvas.ForceUpdateCanvases();

            float preferredHeight = LayoutUtility.GetPreferredHeight(setupContentRoot);
            float topInset = Mathf.Max(StartMenuVerticalMargin, -setupContentRoot.anchoredPosition.y);
            float bottomInset = StartMenuVerticalMargin;
            float availableHeight = Mathf.Max(0f, panelRect.rect.height - topInset - bottomInset);
            float scale = preferredHeight > 0f && availableHeight > 0f
                ? Mathf.Min(1f, availableHeight / preferredHeight)
                : 1f;

            scale *= ResponsiveScaleSafetyFactor;

            setupContentRoot.localScale = new Vector3(scale, scale, 1f);
            RepositionDevelopmentTestingAnchor();
        }

        private void ScheduleDeferredLayoutRefresh()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (deferredLayoutRefreshCoroutine != null)
            {
                StopCoroutine(deferredLayoutRefreshCoroutine);
            }

            deferredLayoutRefreshCoroutine = StartCoroutine(RefreshStartMenuLayoutDeferred());
        }

        private IEnumerator RefreshStartMenuLayoutDeferred()
        {
            yield return null;

            if (!isActiveAndEnabled)
            {
                deferredLayoutRefreshCoroutine = null;
                yield break;
            }

            RefreshResponsiveStartLayout();
            deferredLayoutRefreshCoroutine = null;
        }

        public void OnPlayerCountSelected(int count)
        {
            selectedPlayerCount = count;
            ResetMoldSelectionState();
            UpdateButtonVisuals();
            // Reset human player count to default (1) or clamp if fewer than previous selection
            selectedHumanPlayerCount = DefaultHotseatHumanPlayerCount;
            UpdatePlayerButtonVisuals();
            ConfigureHumanPlayerButtons();
            UpdateHumanPlayerButtonVisuals();
            UpdatePlayerSummaryLabel();
            UpdateSetupStepState();
        }

        private void ConfigureHumanPlayerButtons()
        {
            if (humanPlayerSectionRoot == null || humanPlayerButtons == null) return;
            humanPlayerSectionRoot.SetActive(true);
            int total = selectedPlayerCount ?? 0;
            foreach (var btn in humanPlayerButtons)
            {
                if (btn == null) continue;
                bool shouldShow = btn.humanPlayerCount <= total && btn.humanPlayerCount >= 1;
                btn.gameObject.SetActive(shouldShow);
            }
        }

        public void OnHumanPlayerCountSelected(int humanCount)
        {
            if (!selectedPlayerCount.HasValue) return; // ignore if total not yet chosen
            if (humanCount < 1) humanCount = 1; // must have at least one human
            if (humanCount > selectedPlayerCount.Value) humanCount = selectedPlayerCount.Value; // clamp
            selectedHumanPlayerCount = humanCount;
            selectedHumanMoldIndices.Clear();
            currentHumanMoldSelectionIndex = 0;
            UpdateHumanPlayerButtonVisuals();
            UpdatePlayerSummaryLabel();
            UpdateSetupStepState();
        }

        private void UpdatePlayerSummaryLabel()
        {
            if (playerSummaryLabel == null)
                return;
            if (!selectedPlayerCount.HasValue)
            {
                playerSummaryLabel.text = string.Empty;
                return;
            }
            int total = selectedPlayerCount.Value;
            int humans = Mathf.Clamp(selectedHumanPlayerCount, 1, total);
            int ai = Mathf.Max(0, total - humans);
            string humanLabel = humans == 1 ? "1 Human" : $"{humans} Humans";
            string aiLabel = ai == 1 ? "1 AI" : $"{ai} AI";
            playerSummaryLabel.text = $"{HumanSelectionPrefix} ({humanLabel} / {aiLabel})";
        }

        private void UpdateButtonVisuals()
        {
            foreach (var btn in playerButtons)
                btn.SetSelected(btn.playerCount == selectedPlayerCount);
        }

        private void UpdatePlayerButtonVisuals()
        {
            foreach (var btn in playerButtons)
                btn.SetSelected(btn.playerCount == selectedPlayerCount);
        }

        private void UpdateHumanPlayerButtonVisuals()
        {
            if (humanPlayerButtons == null) return;
            foreach (var btn in humanPlayerButtons)
            {
                if (btn == null) continue;
                btn.SetSelected(btn.humanPlayerCount == selectedHumanPlayerCount && btn.gameObject.activeSelf);
            }
        }

        public void OnStartGamePressed()
        {
            if (!selectedPlayerCount.HasValue)
            {
                return;
            }

            if (currentStep == SetupStep.CountSelection)
            {
                EnterMoldSelectionStep();
                return;
            }

            AdvanceMoldSelectionOrStartGame();
        }

        public void OnBackPressed()
        {
            if (currentStep == SetupStep.MoldSelection)
            {
                if (currentHumanMoldSelectionIndex > 0)
                {
                    currentHumanMoldSelectionIndex--;
                    UpdateSetupStepState();
                    return;
                }

                ReturnToCountSelectionStep();
                return;
            }

            gameObject.SetActive(false);
            if (modeSelectPanel != null)
                modeSelectPanel.SetActive(true);
        }

    }
}
