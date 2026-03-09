using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Config;
using TMPro;
using FungusToast.Unity;
using FungusToast.Unity.Campaign;
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
        private TextMeshProUGUI testingModeToggleLabel;
        private Text testingModeToggleLegacyLabel;

        private void Awake()
        {
            Instance = this;
            ResolveTestingModeReferences();
            // Strict validation: all required refs must be assigned in Inspector
            ValidateSerializedRefs();

            ApplyStyle();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);

            startGameButton.interactable = false;
            InitializeTestingModeUI();
            InitializeHumanPlayerUI();

            // Ensure magnifier visuals are disabled at startup
            if (magnifierVisualRoot != null)
                magnifierVisualRoot.SetActive(false);
        }

        private void ValidateSerializedRefs()
        {
            if (startGameButton == null) throw new InvalidOperationException("UI_StartGamePanel: startGameButton is not assigned.");
            if (testingModeToggle == null) throw new InvalidOperationException("UI_StartGamePanel: testingModeToggle is not assigned.");
            if (mycovariantDropdown == null) throw new InvalidOperationException("UI_StartGamePanel: mycovariantDropdown is not assigned.");
            if (testingModePanel == null) throw new InvalidOperationException("UI_StartGamePanel: testingModePanel is not assigned.");
            if (fastForwardRoundsInput == null) throw new InvalidOperationException("UI_StartGamePanel: fastForwardRoundsInput is not assigned.");
            if (fastForwardLabel == null) throw new InvalidOperationException("UI_StartGamePanel: fastForwardLabel is not assigned.");
            if (skipToEndgameToggle == null) throw new InvalidOperationException("UI_StartGamePanel: skipToEndgameToggle is not assigned.");
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

            if (testingModeToggle == null)
            {
                testingModeToggle = FindToggleByName(sectionRoot.transform, "TestingModeToggle", "Skip");
                if (testingModeToggle == null)
                {
                    testingModeToggle = sectionRoot.GetComponentInChildren<Toggle>(true);
                }
            }

            if (mycovariantDropdown == null)
            {
                mycovariantDropdown = FindDropdownByName(sectionRoot.transform, "Mycovariant", "Forced");
                if (mycovariantDropdown == null)
                {
                    mycovariantDropdown = sectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
                }
            }

            if (forcedGameResultDropdown == null)
            {
                forcedGameResultDropdown = FindDropdownByName(sectionRoot.transform, "ForcedGameResult", null);
            }

            if (testingModeToggleLabel == null && testingModeToggle != null)
            {
                var labelTransform = FindTransformByName(testingModeToggle.transform, "ToggleLabel");
                if (labelTransform != null)
                {
                    testingModeToggleLabel = labelTransform.GetComponent<TextMeshProUGUI>();
                    testingModeToggleLegacyLabel = labelTransform.GetComponent<Text>();
                }
            }

            if (fastForwardRoundsInput == null)
            {
                fastForwardRoundsInput = sectionRoot.GetComponentInChildren<TMP_InputField>(true);
            }

            if (skipToEndgameToggle == null)
            {
                skipToEndgameToggle = FindToggleByName(sectionRoot.transform, "Skip", null);
            }

            if (testingModePanel == null)
            {
                var panelTransform = FindTransformByName(sectionRoot.transform, "TestingModePanel");
                if (panelTransform != null)
                {
                    testingModePanel = panelTransform.gameObject;
                }
            }

            if (fastForwardLabel == null)
            {
                var labelTransform = FindTransformByName(sectionRoot.transform, "FastForwardRoundsLabel");
                if (labelTransform != null)
                {
                    fastForwardLabel = labelTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private static Transform FindTransformByName(Transform root, string contains)
        {
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < allChildren.Length; index++)
            {
                var child = allChildren[index];
                if (child == null)
                {
                    continue;
                }

                if (child.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }
            }

            return null;
        }

        private static Toggle FindToggleByName(Transform root, string include, string exclude)
        {
            var toggles = root.GetComponentsInChildren<Toggle>(true);
            for (int index = 0; index < toggles.Length; index++)
            {
                var toggle = toggles[index];
                if (toggle == null)
                {
                    continue;
                }

                string name = toggle.gameObject.name;
                bool includes = string.IsNullOrEmpty(include) || name.IndexOf(include, StringComparison.OrdinalIgnoreCase) >= 0;
                bool excludes = !string.IsNullOrEmpty(exclude) && name.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0;
                if (includes && !excludes)
                {
                    return toggle;
                }
            }

            return null;
        }

        private static TMP_Dropdown FindDropdownByName(Transform root, string include, string exclude)
        {
            var dropdowns = root.GetComponentsInChildren<TMP_Dropdown>(true);
            for (int index = 0; index < dropdowns.Length; index++)
            {
                var dropdown = dropdowns[index];
                if (dropdown == null)
                {
                    continue;
                }

                string name = dropdown.gameObject.name;
                bool includes = string.IsNullOrEmpty(include) || name.IndexOf(include, StringComparison.OrdinalIgnoreCase) >= 0;
                bool excludes = !string.IsNullOrEmpty(exclude) && name.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0;
                if (includes && !excludes)
                {
                    return dropdown;
                }
            }

            return null;
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

            EnsureTestingToggleRowLayout(sectionRoot.transform);
        }

        private static void EnsureTestingToggleRowLayout(Transform sectionRoot)
        {
            var toggleRow = sectionRoot.Find("UI_TestingModeToggle") ?? FindTransformByName(sectionRoot, "TestingModeToggle");
            if (toggleRow == null)
            {
                return;
            }

            if (toggleRow.name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0 ||
                toggleRow.name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                toggleRow = toggleRow.parent;
            }

            if (toggleRow == null)
            {
                return;
            }

            var accidentalLayouts = toggleRow.GetComponents<LayoutGroup>();
            for (int index = 0; index < accidentalLayouts.Length; index++)
            {
                if (accidentalLayouts[index] != null)
                {
                    Destroy(accidentalLayouts[index]);
                }
            }

            var accidentalFitter = toggleRow.GetComponent<ContentSizeFitter>();
            if (accidentalFitter != null)
            {
                Destroy(accidentalFitter);
            }

            var toggleRect = toggleRow.GetComponent<RectTransform>();
            if (toggleRect != null)
            {
                toggleRect.anchorMin = new Vector2(0f, toggleRect.anchorMin.y);
                toggleRect.anchorMax = new Vector2(1f, toggleRect.anchorMax.y);
                toggleRect.anchoredPosition = new Vector2(0f, toggleRect.anchoredPosition.y);
                toggleRect.sizeDelta = new Vector2(0f, Mathf.Max(30f, toggleRect.sizeDelta.y));
            }

            var toggleLayoutElement = toggleRow.GetComponent<LayoutElement>();
            if (toggleLayoutElement == null)
            {
                toggleLayoutElement = toggleRow.gameObject.AddComponent<LayoutElement>();
            }

            toggleLayoutElement.minWidth = 300f;
            toggleLayoutElement.preferredWidth = 300f;
            toggleLayoutElement.minHeight = 30f;
            toggleLayoutElement.preferredHeight = 30f;

            var toggleBackground = FindTransformByName(toggleRow, "ToggleBackground");
            if (toggleBackground is RectTransform backgroundRect)
            {
                backgroundRect.anchorMin = new Vector2(0f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0f, 0.5f);
                backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.anchoredPosition = new Vector2(10f, 0f);
                backgroundRect.sizeDelta = new Vector2(20f, 20f);
            }

            var toggleLabel = FindTransformByName(toggleRow, "ToggleLabel");
            if (toggleLabel is RectTransform labelRect)
            {
                if (labelRect.parent != toggleRow)
                {
                    labelRect.SetParent(toggleRow, false);
                }

                labelRect.anchorMin = new Vector2(0f, 0.5f);
                labelRect.anchorMax = new Vector2(0f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(30f, 0f);
                labelRect.sizeDelta = new Vector2(260f, 30f);

                var labelLayoutElement = labelRect.GetComponent<LayoutElement>();
                if (labelLayoutElement == null)
                {
                    labelLayoutElement = labelRect.gameObject.AddComponent<LayoutElement>();
                }

                labelLayoutElement.ignoreLayout = true;
                labelLayoutElement.preferredWidth = -1f;
                labelLayoutElement.preferredHeight = -1f;
            }

            var legacyLabel = toggleLabel != null ? toggleLabel.GetComponent<Text>() : null;
            if (legacyLabel != null)
            {
                legacyLabel.color = UIStyleTokens.Button.TextDefault;
                legacyLabel.alignment = TextAnchor.MiddleLeft;
            }

            var tmpLabel = toggleLabel != null ? toggleLabel.GetComponent<TextMeshProUGUI>() : null;
            if (tmpLabel != null)
            {
                tmpLabel.color = UIStyleTokens.Button.TextDefault;
                tmpLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }
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

            if (testingModePanel != null)
            {
                UIStyleTokens.ApplyPanelSurface(testingModePanel, UIStyleTokens.Surface.PanelPrimary);
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyStyle(startGameButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.ApplyStyle(backButton);

            if (playerSummaryLabel != null)
            {
                playerSummaryLabel.color = UIStyleTokens.Text.Secondary;
            }

            if (fastForwardLabel != null)
            {
                fastForwardLabel.color = UIStyleTokens.Text.Secondary;
            }

            ApplyTestingInputReadability();
        }

        private void ApplyTestingInputReadability()
        {
            ApplyDropdownReadability(mycovariantDropdown);
            ApplyDropdownReadability(forcedGameResultDropdown);
            ApplyInputReadability(fastForwardRoundsInput);
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
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            var allLabels = dropdown.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < allLabels.Length; i++)
            {
                if (allLabels[i] == null)
                {
                    continue;
                }

                if (allLabels[i].name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    allLabels[i].color = UIStyleTokens.Text.Disabled;
                }
                else
                {
                    allLabels[i].color = UIStyleTokens.Button.TextDefault;
                }
            }
        }

        private static void ApplyInputReadability(TMP_InputField input)
        {
            if (input == null)
            {
                return;
            }

            if (input.textComponent != null)
            {
                input.textComponent.color = UIStyleTokens.Button.TextDefault;
            }

            if (input.placeholder is TextMeshProUGUI placeholderLabel)
            {
                placeholderLabel.color = UIStyleTokens.Text.Disabled;
            }
            else if (input.placeholder is Graphic placeholderGraphic)
            {
                placeholderGraphic.color = UIStyleTokens.Text.Disabled;
            }
        }

        private void InitializeTestingModeUI()
        {
            EnsureForcedGameResultDropdown();

            // Initialize mycovariant dropdown
            mycovariantDropdown.ClearOptions();
            var options = new List<string> { "Select Mycovariant..." };
            var mycovariants = MycovariantRepository.All;
            foreach (var mycovariant in mycovariants)
                options.Add($"{mycovariant.Name} (ID: {mycovariant.Id})");
            mycovariantDropdown.AddOptions(options);
            mycovariantDropdown.value = 0;

            InitializeForcedGameResultDropdownOptions();

            // Set up testing mode toggle
            testingModeToggle.onValueChanged.AddListener(OnTestingModeToggled);
            testingModePanel.SetActive(false);
            RefreshTestingSectionLayout(false);
            UpdateTestingToggleLabel();

            // Initialize fast-forward input
            fastForwardRoundsInput.contentType = TMP_InputField.ContentType.IntegerNumber;

            // Default state for skip-to-end toggle
            skipToEndgameToggle.isOn = false;
            skipToEndgameToggle.interactable = false; // disabled until testing mode is enabled
            skipToEndgameToggle.onValueChanged.AddListener(_ => UpdateForcedGameResultVisibility());

            UpdateForcedGameResultVisibility();
        }

        private void OnTestingModeToggled(bool isEnabled)
        {
            testingModePanel.SetActive(isEnabled);
            mycovariantDropdown.interactable = isEnabled;
            fastForwardRoundsInput.interactable = isEnabled;
            fastForwardLabel.gameObject.SetActive(isEnabled);
            
            // Enable/disable strictly based on Testing Mode; no auto-search fallback
            skipToEndgameToggle.interactable = isEnabled;
            if (!isEnabled)
            {
                // Reset when turning testing mode off
                skipToEndgameToggle.isOn = false;
            }

            RefreshTestingSectionLayout(isEnabled);
            UpdateForcedGameResultVisibility();
            UpdateTestingToggleLabel();
        }

        private void UpdateTestingToggleLabel()
        {
            if (testingModeToggleLabel != null)
            {
                testingModeToggleLabel.text = testingModeToggle != null && testingModeToggle.isOn
                    ? "Development Testing: On"
                    : "Development Testing: Off";
            }

            if (testingModeToggleLegacyLabel != null)
            {
                testingModeToggleLegacyLabel.text = testingModeToggle != null && testingModeToggle.isOn
                    ? "Development Testing: On"
                    : "Development Testing: Off";
            }
        }

        private void RefreshTestingSectionLayout(bool isExpanded)
        {
            if (testingOptionsSectionRoot == null)
            {
                return;
            }

            const float collapsedHeight = 40f;
            float expandedHeight = collapsedHeight;

            if (isExpanded && testingModePanel != null)
            {
                float panelHeight = 220f;
                var panelLayoutElement = testingModePanel.GetComponent<LayoutElement>();
                if (panelLayoutElement != null && panelLayoutElement.preferredHeight > 0f)
                {
                    panelHeight = panelLayoutElement.preferredHeight;
                }

                expandedHeight += 8f + panelHeight;
            }

            var sectionLayoutElement = testingOptionsSectionRoot.GetComponent<LayoutElement>();
            if (sectionLayoutElement != null)
            {
                sectionLayoutElement.minHeight = isExpanded ? expandedHeight : collapsedHeight;
                sectionLayoutElement.preferredHeight = isExpanded ? expandedHeight : collapsedHeight;
            }

            var sectionRect = testingOptionsSectionRoot.GetComponent<RectTransform>();
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

                // Handle testing mode
                if (testingModeToggle.isOn)
                {
                    // Get fast forward rounds regardless of mycovariant selection
                    int fastForwardRounds = 0;
                    if (int.TryParse(fastForwardRoundsInput.text, out int parsedRounds))
                        fastForwardRounds = Mathf.Max(0, parsedRounds);

                    bool skipToEnd = skipToEndgameToggle.isOn;
                    ForcedGameResultMode forcedResultMode = GetForcedGameResultSelection();

                    // Enable testing mode with or without a mycovariant selected
                    if (mycovariantDropdown.value > 0)
                    {
                        var selectedMycovariant = MycovariantRepository.All[mycovariantDropdown.value - 1];
                        GameManager.Instance.EnableTestingMode(selectedMycovariant.Id, fastForwardRounds, skipToEnd, forcedResultMode);
                    }
                    else
                    {
                        GameManager.Instance.EnableTestingMode(null, fastForwardRounds, skipToEnd, forcedResultMode);
                    }
                }
                else
                {
                    GameManager.Instance.DisableTestingMode();
                }

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

        private void EnsureForcedGameResultDropdown()
        {
            if (testingModePanel == null || mycovariantDropdown == null)
            {
                return;
            }

            if (forcedGameResultDropdown == null)
            {
                forcedGameResultDropdown = FindDropdownByName(testingModePanel.transform, "ForcedGameResult", null);
            }

            if (forcedGameResultDropdown != null)
            {
                forcedGameResultRow = forcedGameResultDropdown.transform.parent != null
                    ? forcedGameResultDropdown.transform.parent.gameObject
                    : null;
                return;
            }

            forcedGameResultRow = new GameObject("UI_ForcedGameResultRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            forcedGameResultRow.transform.SetParent(testingModePanel.transform, false);

            var rowLayout = forcedGameResultRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 10f;

            var rowElement = forcedGameResultRow.GetComponent<LayoutElement>();
            rowElement.minHeight = 40f;
            rowElement.preferredHeight = 42f;

            var labelObject = new GameObject("UI_ForcedGameResultLabel", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(forcedGameResultRow.transform, false);
            var labelElement = labelObject.GetComponent<LayoutElement>();
            labelElement.preferredWidth = 170f;
            labelElement.minWidth = 150f;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Game Result";
            label.color = UIStyleTokens.Text.Secondary;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var dropdownObject = Instantiate(mycovariantDropdown.gameObject, forcedGameResultRow.transform);
            dropdownObject.name = "UI_ForcedGameResultDropdown";
            forcedGameResultDropdown = dropdownObject.GetComponent<TMP_Dropdown>();

            if (forcedGameResultDropdown == null)
            {
                Destroy(forcedGameResultRow);
                forcedGameResultRow = null;
                return;
            }

            var dropdownElement = dropdownObject.GetComponent<LayoutElement>();
            if (dropdownElement == null)
            {
                dropdownElement = dropdownObject.AddComponent<LayoutElement>();
            }
            dropdownElement.flexibleWidth = 1f;
            dropdownElement.preferredHeight = 38f;
        }

        private void InitializeForcedGameResultDropdownOptions()
        {
            if (forcedGameResultDropdown == null)
            {
                return;
            }

            forcedGameResultDropdown.ClearOptions();
            forcedGameResultDropdown.AddOptions(new List<string>
            {
                "Natural",
                "Forced Win",
                "Forced Loss"
            });
            forcedGameResultDropdown.value = 0;
            forcedGameResultDropdown.RefreshShownValue();
        }

        private ForcedGameResultMode GetForcedGameResultSelection()
        {
            if (forcedGameResultDropdown == null)
            {
                return ForcedGameResultMode.Natural;
            }

            return forcedGameResultDropdown.value switch
            {
                1 => ForcedGameResultMode.ForcedWin,
                2 => ForcedGameResultMode.ForcedLoss,
                _ => ForcedGameResultMode.Natural
            };
        }

        private void UpdateForcedGameResultVisibility()
        {
            bool testingEnabled = testingModeToggle != null && testingModeToggle.isOn;
            bool skipEnabled = skipToEndgameToggle != null && skipToEndgameToggle.isOn;
            bool showForcedResult = testingEnabled && skipEnabled;

            if (forcedGameResultRow != null)
            {
                forcedGameResultRow.SetActive(showForcedResult);
            }

            if (forcedGameResultDropdown != null)
            {
                forcedGameResultDropdown.interactable = showForcedResult;

                if (!showForcedResult)
                {
                    forcedGameResultDropdown.value = 0;
                    forcedGameResultDropdown.RefreshShownValue();
                }
            }
        }
    }
}
