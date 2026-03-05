using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.Campaign; // for save service + GameManager extension
using FungusToast.Core.Mycovariants;
using TMPro;
using System.Collections.Generic;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel: Resume / New / Delete / Back.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private GameObject modeSelectPanel; // reference back to UI_ModeSelectPanel

        [Header("Testing Mode")]
        [SerializeField] private GameObject testingOptionsSectionRoot;

        [Header("Testing Mode Templates")]
        [SerializeField] private Toggle testingModeToggleTemplate;
        [SerializeField] private GameObject testingModePanelTemplate;

        private Toggle testingModeToggle;
        private TMP_Dropdown mycovariantDropdown;
        private TMP_InputField fastForwardRoundsInput;
        private Toggle skipToEndgameToggle;
        private GameObject testingModePanel;

        private void Awake()
        {
            InitializeTestingModeUI();
            ApplyStyle();

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (newButton != null) newButton.onClick.AddListener(OnNewClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            RefreshButtonStates();
            RefreshTestingDropdownOptions();
        }

        private void RefreshButtonStates()
        {
            bool has = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();
            if (resumeButton != null) resumeButton.interactable = has;
            if (deleteButton != null) deleteButton.interactable = has;

            UIStyleTokens.Button.SetButtonLabelColor(resumeButton, has ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
            UIStyleTokens.Button.SetButtonLabelColor(deleteButton, has ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyStyle(resumeButton);
            UIStyleTokens.Button.ApplyStyle(newButton);
            UIStyleTokens.Button.ApplyStyle(deleteButton);
            UIStyleTokens.Button.ApplyStyle(backButton);
        }

        private void InitializeTestingModeUI()
        {
            var contentRoot = transform.Find("UI_CampaignContent") as RectTransform;
            if (contentRoot == null)
            {
                Debug.LogWarning("UI_CampaignPanelController: UI_CampaignContent not found; campaign testing options disabled.");
                return;
            }

            if (testingOptionsSectionRoot == null)
            {
                var found = contentRoot.Find("UI_TestingOptionsSection");
                if (found == null)
                {
                    found = contentRoot.Find("UI_CampaignTestingOptionsSection");
                }

                if (found != null)
                {
                    testingOptionsSectionRoot = found.gameObject;
                }
            }

            if (testingOptionsSectionRoot != null)
            {
                EnsureTestingSectionLayout(testingOptionsSectionRoot);
                testingModeToggle = FindToggleByName(testingOptionsSectionRoot.transform, "TestingModeToggle", "Skip");
                mycovariantDropdown = testingOptionsSectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
                fastForwardRoundsInput = testingOptionsSectionRoot.GetComponentInChildren<TMP_InputField>(true);
                skipToEndgameToggle = FindToggleByName(testingOptionsSectionRoot.transform, "Skip", null);

                var panelTransform = FindTransformByName(testingOptionsSectionRoot.transform, "TestingModePanel");
                testingModePanel = panelTransform != null ? panelTransform.gameObject : testingOptionsSectionRoot;
            }
            else
            {
                if (testingModeToggleTemplate == null || testingModePanelTemplate == null)
                {
                    Debug.LogWarning("UI_CampaignPanelController: Testing mode templates are not assigned; campaign testing options disabled.");
                    return;
                }

                var toggleObject = Instantiate(testingModeToggleTemplate.gameObject, contentRoot);
                toggleObject.name = "UI_CampaignTestingModeToggle";
                testingModeToggle = toggleObject.GetComponent<Toggle>();

                testingModePanel = Instantiate(testingModePanelTemplate, contentRoot);
                testingModePanel.name = "UI_CampaignTestingModePanel";

                if (backButton != null)
                {
                    int backIndex = backButton.transform.GetSiblingIndex();
                    toggleObject.transform.SetSiblingIndex(backIndex);
                    testingModePanel.transform.SetSiblingIndex(backIndex + 1);
                }

                mycovariantDropdown = testingModePanel.GetComponentInChildren<TMP_Dropdown>(true);
                fastForwardRoundsInput = testingModePanel.GetComponentInChildren<TMP_InputField>(true);
                var panelToggles = testingModePanel.GetComponentsInChildren<Toggle>(true);
                skipToEndgameToggle = panelToggles.Length > 0 ? panelToggles[0] : null;
            }

            if (testingModeToggle != null)
            {
                testingModeToggle.isOn = false;
                testingModeToggle.onValueChanged.AddListener(OnTestingModeToggled);
            }

            if (fastForwardRoundsInput != null)
            {
                fastForwardRoundsInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            }

            if (skipToEndgameToggle != null)
            {
                skipToEndgameToggle.isOn = false;
                skipToEndgameToggle.interactable = false;
            }

            if (testingModePanel != null)
            {
                testingModePanel.SetActive(false);
            }

            RefreshTestingSectionLayout(false);

            RefreshTestingDropdownOptions();
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

                if (child.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0)
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
                bool includes = string.IsNullOrEmpty(include) || name.IndexOf(include, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool excludes = !string.IsNullOrEmpty(exclude) && name.IndexOf(exclude, System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (includes && !excludes)
                {
                    return toggle;
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

            if (toggleRow.name.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                toggleRow.name.IndexOf("Label", System.StringComparison.OrdinalIgnoreCase) >= 0)
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

        private void RefreshTestingDropdownOptions()
        {
            if (mycovariantDropdown == null)
            {
                return;
            }

            mycovariantDropdown.ClearOptions();
            var options = new List<string> { "Select Mycovariant..." };
            var mycovariants = MycovariantRepository.All;
            foreach (var mycovariant in mycovariants)
                options.Add($"{mycovariant.Name} (ID: {mycovariant.Id})");

            mycovariantDropdown.AddOptions(options);
            mycovariantDropdown.value = 0;
            mycovariantDropdown.RefreshShownValue();
        }

        private void OnTestingModeToggled(bool isEnabled)
        {
            if (testingModePanel != null)
                testingModePanel.SetActive(isEnabled);

            if (mycovariantDropdown != null)
                mycovariantDropdown.interactable = isEnabled;

            if (fastForwardRoundsInput != null)
                fastForwardRoundsInput.interactable = isEnabled;

            if (skipToEndgameToggle != null)
            {
                skipToEndgameToggle.interactable = isEnabled;
                if (!isEnabled)
                    skipToEndgameToggle.isOn = false;
            }

            RefreshTestingSectionLayout(isEnabled);
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

        private void ApplyTestingModeToGameManager()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (testingModeToggle == null || !testingModeToggle.isOn)
            {
                GameManager.Instance.DisableTestingMode();
                return;
            }

            int? mycovariantId = null;
            if (mycovariantDropdown != null && mycovariantDropdown.value > 0)
            {
                int selected = mycovariantDropdown.value - 1;
                if (selected >= 0 && selected < MycovariantRepository.All.Count)
                    mycovariantId = MycovariantRepository.All[selected].Id;
            }

            int fastForwardRounds = 0;
            if (fastForwardRoundsInput != null && int.TryParse(fastForwardRoundsInput.text, out int parsedRounds))
                fastForwardRounds = Mathf.Max(0, parsedRounds);

            bool skipToEnd = skipToEndgameToggle != null && skipToEndgameToggle.isOn;
            GameManager.Instance.EnableTestingMode(mycovariantId, fastForwardRounds, skipToEnd);
        }

        private void OnResumeClicked()
        {
            ApplyTestingModeToGameManager();
            if (GameManager.Instance != null)
                GameManager.Instance.StartCampaignResume();
            gameObject.SetActive(false);
        }

        private void OnNewClicked()
        {
            ApplyTestingModeToGameManager();
            if (GameManager.Instance != null)
                GameManager.Instance.StartCampaignNew();
            gameObject.SetActive(false);
        }

        private void OnDeleteClicked()
        {
            CampaignSaveService.Delete();
            RefreshButtonStates();
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
            if (modeSelectPanel != null)
                modeSelectPanel.SetActive(true);
        }
    }
}
