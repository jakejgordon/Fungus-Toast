using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using FungusToast.Unity;
using FungusToast.Unity.UI.Testing;
using System; // added for strict validation exceptions

namespace FungusToast.Unity.UI.GameStart
{
    public class UI_StartGamePanel : MonoBehaviour
    {
        public static UI_StartGamePanel Instance { get; private set; }

        [SerializeField] private List<UI_PlayerCountButton> playerButtons;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject modeSelectPanel;

        [Header("Human Players (Hotseat)")]
        [SerializeField] private GameObject humanPlayerSectionRoot; // container for human player selector (hidden until total picked)
        [SerializeField] private List<UI_HotseatHumanCountButton> humanPlayerButtons; // 1..8 reuse same prefab style
        [SerializeField] private TextMeshProUGUI playerSummaryLabel; // "X Players (Y Human / Z AI)"

        [Header("Testing Mode")]
        [SerializeField] private GameObject testingOptionsSectionRoot;
        [SerializeField] private Toggle testingModeToggle;
        [SerializeField] private TMP_Dropdown mycovariantDropdown;
        [SerializeField] private TMP_Dropdown forcedGameResultDropdown;
        [SerializeField] private GameObject forcedGameResultRow;
        [SerializeField] private GameObject testingModePanel;
        [SerializeField] private TMP_InputField fastForwardRoundsInput;
        [SerializeField] private TextMeshProUGUI fastForwardLabel;
        [SerializeField] private Toggle skipToEndgameToggle; // NEW: Skip to end-of-game toggle

        // Magnifying glass UI reference
        [SerializeField] private GameObject magnifyingGlassUI;
        // Magnifier visuals (child of magnifyingGlassUI)
        [SerializeField] private GameObject magnifierVisualRoot;

        private int? selectedPlayerCount = null;
        private int selectedHumanPlayerCount = 1; // always defaults to 1 when total players picked
        public int SelectedHumanPlayerCount => selectedHumanPlayerCount; // expose for future game manager refactor
        private DevelopmentTestingCardController testingCardController;
        private RectTransform setupContentRoot;
        private RectTransform titleSectionRoot;
        private RectTransform playerCountSectionRoot;
        private RectTransform testingCardSectionRoot;
        private RectTransform actionButtonStackRoot;

        private void Awake()
        {
            Instance = this;
            ResolveTestingModeReferences();
            // Strict validation: all required refs must be assigned in Inspector
            ValidateSerializedRefs();

            EnsureRuntimeLayoutScaffold();
            ApplyStyle();
            InitializeTestingCard();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);

            startGameButton.interactable = false;
            InitializeHumanPlayerUI();

            // Ensure magnifier visuals are disabled at startup
            if (magnifierVisualRoot != null)
                magnifierVisualRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EnsureRuntimeLayoutScaffold();
            testingCardController?.RefreshDropdownOptions();
            testingCardController?.RefreshVisualState();
            RefreshTestingSectionLayout();
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

            if (mycovariantDropdown == null)
            {
                mycovariantDropdown = sectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
            }
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
                SupportsForcedAdaptation = false,
                CardName = "UI_StartGameTestingCard",
                ControlPrefix = "UI_StartGameTesting",
                LogPrefix = "UI_StartGamePanel",
                LayoutInvalidated = RefreshTestingSectionLayout
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

            var orderedSections = new List<RectTransform>();
            TryAddSetupSection(orderedSections, titleSectionRoot);
            TryAddSetupSection(orderedSections, playerCountSectionRoot);
            TryAddSetupSection(orderedSections, humanPlayerSectionRoot != null ? humanPlayerSectionRoot.GetComponent<RectTransform>() : null);
            TryAddSetupSection(orderedSections, testingCardSectionRoot);

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
        }

        private void ResolveSetupSectionReferences()
        {
            titleSectionRoot ??= FindNamedRectTransform("UI_HowManyPlayersText");
            playerCountSectionRoot ??= FindNamedRectTransform("UI_PlayerCountButtonGroupWrapper");
            testingCardSectionRoot ??= FindNamedRectTransform("UI_StartGameTestingSection");

            if (playerCountSectionRoot == null)
            {
                playerCountSectionRoot = GetTopLevelSection(playerButtons != null && playerButtons.Count > 0 ? playerButtons[0]?.transform : null) as RectTransform;
            }

            ConfigureCenteredButtonRow(playerCountSectionRoot, "UI_PlayerCountButtonGroup");
            ConfigureCenteredButtonRow(humanPlayerSectionRoot != null ? humanPlayerSectionRoot.GetComponent<RectTransform>() : null, "UI_HumanPlayerCountButtonGroup");
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
            if (setupContentRoot == null)
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
                    testingCardSectionRoot.SetParent(setupContentRoot, false);
                }
            }

            testingCardSectionRoot.SetParent(setupContentRoot, false);
            ConfigureTestingCardSection(testingCardSectionRoot);
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
            element.minWidth = 500f;
            element.preferredWidth = 500f;
            element.minHeight = 56f;
            element.preferredHeight = -1f;
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
            if (button == null)
            {
                return;
            }

            var layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = 470f;
            layoutElement.preferredWidth = 470f;
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 52f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
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
            if (mycovariantDropdown != null)
            {
                return mycovariantDropdown;
            }

            if (testingOptionsSectionRoot != null)
            {
                return testingOptionsSectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
            }

            return FindFirstObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
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

            UIStyleTokens.Button.ApplyStyle(startGameButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.ApplyStyle(backButton);

            if (playerSummaryLabel != null)
            {
                playerSummaryLabel.color = UIStyleTokens.Text.Secondary;
            }
        }

        private void RefreshTestingSectionLayout()
        {
            if (testingCardSectionRoot == null)
            {
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
        }

        public void OnPlayerCountSelected(int count)
        {
            selectedPlayerCount = count;
            UpdateButtonVisuals();
            startGameButton.interactable = true;
            // Reset human player count to default (1) or clamp if fewer than previous selection
            selectedHumanPlayerCount = 1;
            UpdatePlayerButtonVisuals();
            ConfigureHumanPlayerButtons();
            UpdateHumanPlayerButtonVisuals();
            UpdatePlayerSummaryLabel();
            startGameButton.interactable = true; // per requirements: selecting total players is sufficient
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
            UpdateHumanPlayerButtonVisuals();
            UpdatePlayerSummaryLabel();
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
            playerSummaryLabel.text = $"{total} Players ({humans} Human / {ai} AI)";
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
            if (selectedPlayerCount.HasValue)
            {
                // Persist hotseat config for future multi-human implementation
                GameManager.Instance?.SetHotseatConfig(selectedHumanPlayerCount);

                testingCardController?.ApplyToGameManager(GameManager.Instance);

                // NOTE: For this initial UI-only step we do not yet create multiple human players.
                // The selectedHumanPlayerCount value is retained for future hotseat implementation.
                GameManager.Instance.InitializeGame(selectedPlayerCount.Value);
                GameManager.Instance.cameraCenterer.CenterCameraSmooth();
                gameObject.SetActive(false);

                // Enable the magnifying glass UI after the game starts
                if (magnifyingGlassUI != null)
                    magnifyingGlassUI.SetActive(true);
                if (magnifierVisualRoot != null)
                    magnifierVisualRoot.SetActive(true);
                MagnifyingGlassFollowMouse.gameStarted = true;
            }
        }

        public void OnBackPressed()
        {
            gameObject.SetActive(false);
            if (modeSelectPanel != null)
                modeSelectPanel.SetActive(true);
        }

    }
}
