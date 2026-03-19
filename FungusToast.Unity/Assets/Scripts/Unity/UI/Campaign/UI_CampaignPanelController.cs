using System;
using System.Collections.Generic;
using FungusToast.Core.Campaign;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Testing;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel with a deterministic layout stack:
    /// Testing card at top, action buttons beneath.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
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
        private RectTransform mainStackRoot;
        private GameObject actionStack;
        private DevelopmentTestingCardController testingCardController;
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
            var existing = contentRoot.Find("UI_CampaignMainStack") as RectTransform;
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

            mainStackRoot.anchorMin = new Vector2(0.5f, 1f);
            mainStackRoot.anchorMax = new Vector2(0.5f, 1f);
            mainStackRoot.pivot = new Vector2(0.5f, 1f);
            mainStackRoot.anchoredPosition = new Vector2(0f, -98f);
            mainStackRoot.sizeDelta = new Vector2(500f, 0f);

            var rootLayout = mainStackRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 14f;
            rootLayout.padding = new RectOffset(0, 0, 0, 0);

            var rootFitter = mainStackRoot.GetComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rootElement = mainStackRoot.GetComponent<LayoutElement>();
            rootElement.minWidth = 460f;
            rootElement.preferredWidth = 500f;

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

                if (mainStackRoot != null && t.IsChildOf(mainStackRoot))
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
                Parent = mainStackRoot,
                ButtonTemplate = backButton != null ? backButton : resumeButton,
                DropdownTemplate = FindDropdownTemplate(),
                SupportsForcedAdaptation = true,
                CardName = "UI_CampaignTestingCard",
                ControlPrefix = "UI_CampaignTesting",
                LogPrefix = "UI_CampaignPanelController",
                LayoutInvalidated = ForceLayoutNow
            });
            testingCardController.Build();
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

            return FindFirstObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
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

            if (testingCardController != null)
            {
                var testingRoot = mainStackRoot != null ? mainStackRoot.Find("UI_CampaignTestingCard") : null;
                if (testingRoot != null)
                {
                    testingRoot.gameObject.SetActive(!selectingMold);
                }
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
                    ? $"Selected mold: Mold {selectedCampaignMoldIndex.Value + 1}. This icon will persist for the whole run."
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
                moldSelectionLabels[moldIndex].text = $"Mold {moldIndex + 1}";
            }
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
            labelRect.sizeDelta = new Vector2(92f, 24f);
            labelRect.anchoredPosition = new Vector2(0f, 10f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            moldSelectionButtons.Add(button);
            moldSelectionHighlights.Add(highlightImage);
            moldSelectionIcons.Add(iconImage);
            moldSelectionLabels.Add(label);
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
            bool resumed = GameManager.Instance.CurrentGameMode == GameMode.Campaign
                           && (GameManager.Instance.Board != null || GameManager.Instance.IsCampaignAwaitingAdaptationSelection());

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
            if (mainStackRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainStackRoot);
            }
            if (contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }
        }

    }
}
