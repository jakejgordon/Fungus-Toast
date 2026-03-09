using System;
using System.Collections.Generic;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel with a deterministic layout stack:
    /// Testing card at top, action buttons beneath.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
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
        private GameObject testingCard;
        private GameObject actionStack;

        private Button testingToggleButton;
        private GameObject mycovariantRow;
        private TMP_Dropdown mycovariantDropdown;
        private Button fastForwardButton;
        private Button skipToEndButton;
        private Button forcedResultButton;

        private bool testingEnabled;
        private bool skipToEnd;
        private int fastForwardRounds;
        private ForcedGameResultMode forcedResult = ForcedGameResultMode.Natural;

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
            BuildActionStack();
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
            RefreshTestingVisualState();
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
            var existing = mainStackRoot.Find("UI_CampaignTestingCard");
            if (existing != null)
            {
                testingCard = existing.gameObject;
            }
            else
            {
                testingCard = new GameObject("UI_CampaignTestingCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                testingCard.transform.SetParent(mainStackRoot, false);
            }

            var cardElement = testingCard.GetComponent<LayoutElement>();
            cardElement.minWidth = 460f;
            cardElement.preferredWidth = 500f;
            cardElement.minHeight = 56f;

            var cardBackground = testingCard.GetComponent<Image>();
            var panelColor = UIStyleTokens.Surface.PanelPrimary;
            panelColor.a = 0.92f;
            cardBackground.color = panelColor;
            cardBackground.raycastTarget = false;

            var cardLayout = testingCard.GetComponent<VerticalLayoutGroup>();
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.spacing = 6f;
            cardLayout.padding = new RectOffset(12, 12, 8, 8);

            testingToggleButton = EnsureSettingButton(testingCard.transform, "UI_TestingToggleButton", OnTestingToggleClicked);
            mycovariantRow = EnsureMycovariantRow(testingCard.transform);
            fastForwardButton = EnsureSettingButton(testingCard.transform, "UI_FastForwardButton", OnFastForwardClicked);
            skipToEndButton = EnsureSettingButton(testingCard.transform, "UI_SkipToEndButton", OnSkipToEndClicked);
            forcedResultButton = EnsureSettingButton(testingCard.transform, "UI_ForcedResultButton", OnForcedResultClicked);

            if (testingToggleButton != null)
            {
                testingToggleButton.transform.SetSiblingIndex(0);
            }
            if (mycovariantRow != null)
            {
                mycovariantRow.transform.SetSiblingIndex(1);
            }
            if (fastForwardButton != null)
            {
                fastForwardButton.transform.SetSiblingIndex(2);
            }
            if (skipToEndButton != null)
            {
                skipToEndButton.transform.SetSiblingIndex(3);
            }
            if (forcedResultButton != null)
            {
                forcedResultButton.transform.SetSiblingIndex(4);
            }
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

        private Button EnsureSettingButton(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    existingButton.onClick.RemoveAllListeners();
                    existingButton.onClick.AddListener(action);
                    return existingButton;
                }
            }

            var template = backButton != null ? backButton : resumeButton;
            if (template == null)
            {
                return null;
            }

            var clone = Instantiate(template.gameObject, parent);
            clone.name = name;
            var button = clone.GetComponent<Button>();
            if (button == null)
            {
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            UIStyleTokens.Button.ApplyStyle(button);
            UIStyleTokens.Button.SetButtonLabelColor(button, UIStyleTokens.Button.TextDefault);
            EnsureButtonLayout(button, 470f);

            var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 15f;
                label.fontSizeMax = 24f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = UIStyleTokens.Button.TextDefault;
            }

            return button;
        }

        private GameObject EnsureMycovariantRow(Transform parent)
        {
            var existing = parent.Find("UI_CampaignMycovariantRow");
            if (existing != null)
            {
                var existingDropdown = existing.GetComponentInChildren<TMP_Dropdown>(true);
                if (existingDropdown != null)
                {
                    mycovariantDropdown = existingDropdown;
                }

                return existing.gameObject;
            }

            var row = new GameObject("UI_CampaignMycovariantRow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            var rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 4f;
            rowLayout.padding = new RectOffset(2, 2, 0, 0);

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = 80f;
            rowElement.preferredHeight = 86f;

            var labelObj = new GameObject("UI_CampaignMycovariantLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObj.transform.SetParent(row.transform, false);
            var label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Mycovariant";
            label.color = UIStyleTokens.Text.Primary;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 20f;
            label.alignment = TextAlignmentOptions.Left;

            var labelElement = labelObj.GetComponent<LayoutElement>();
            labelElement.minHeight = 22f;
            labelElement.preferredHeight = 26f;

            var template = FindDropdownTemplate();
            if (template != null)
            {
                var dropdownObj = Instantiate(template.gameObject, row.transform);
                dropdownObj.name = "UI_CampaignMycovariantDropdown";
                mycovariantDropdown = dropdownObj.GetComponent<TMP_Dropdown>();

                var dropdownElement = dropdownObj.GetComponent<LayoutElement>();
                if (dropdownElement == null)
                {
                    dropdownElement = dropdownObj.AddComponent<LayoutElement>();
                }

                dropdownElement.minHeight = 40f;
                dropdownElement.preferredHeight = 44f;
                dropdownElement.minWidth = 460f;
                dropdownElement.preferredWidth = 470f;
            }
            else
            {
                Debug.LogWarning("UI_CampaignPanelController: No TMP_Dropdown template found; mycovariant selector unavailable.");
            }

            return row;
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

            return FindObjectOfType<TMP_Dropdown>(includeInactive: true);
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
            UIStyleTokens.Button.SetButtonLabelColor(testingToggleButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(fastForwardButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(skipToEndButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(forcedResultButton, UIStyleTokens.Button.TextDefault);

            ApplyDropdownReadability(mycovariantDropdown);
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

            var labels = dropdown.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var text = labels[i];
                if (text == null)
                {
                    continue;
                }

                if (text.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text.color = UIStyleTokens.Text.Disabled;
                }
                else
                {
                    text.color = UIStyleTokens.Button.TextDefault;
                }
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

        private void RefreshTestingDropdownOptions()
        {
            if (mycovariantDropdown == null)
            {
                return;
            }

            var options = new List<string> { "Select Mycovariant..." };
            for (int i = 0; i < MycovariantRepository.All.Count; i++)
            {
                var myco = MycovariantRepository.All[i];
                options.Add($"{myco.Name} (ID: {myco.Id})");
            }

            mycovariantDropdown.ClearOptions();
            mycovariantDropdown.AddOptions(options);
            mycovariantDropdown.value = 0;
            mycovariantDropdown.RefreshShownValue();
            ApplyDropdownReadability(mycovariantDropdown);
        }

        private void RefreshTestingVisualState()
        {
            if (mycovariantRow != null)
            {
                mycovariantRow.SetActive(testingEnabled);
            }

            if (fastForwardButton != null)
            {
                fastForwardButton.gameObject.SetActive(testingEnabled);
            }

            if (skipToEndButton != null)
            {
                skipToEndButton.gameObject.SetActive(testingEnabled);
            }

            if (forcedResultButton != null)
            {
                forcedResultButton.gameObject.SetActive(testingEnabled && skipToEnd);
            }

            if (mycovariantDropdown != null)
            {
                mycovariantDropdown.interactable = testingEnabled;
            }

            SetButtonLabel(testingToggleButton, $"Development Testing: {(testingEnabled ? "On" : "Off")}");
            SetButtonLabel(fastForwardButton, $"Fast Forward Rounds: {fastForwardRounds}");
            SetButtonLabel(skipToEndButton, $"Skip to End Game: {(skipToEnd ? "On" : "Off")}");
            SetButtonLabel(forcedResultButton, $"Forced Result: {FormatForcedResult(forcedResult)}");

            if (!skipToEnd)
            {
                forcedResult = ForcedGameResultMode.Natural;
                SetButtonLabel(forcedResultButton, "Forced Result: Natural");
            }
        }

        private void OnTestingToggleClicked()
        {
            testingEnabled = !testingEnabled;
            if (!testingEnabled)
            {
                skipToEnd = false;
                fastForwardRounds = 0;
                forcedResult = ForcedGameResultMode.Natural;
            }

            RefreshTestingVisualState();
            ForceLayoutNow();
        }

        private void OnFastForwardClicked()
        {
            fastForwardRounds = fastForwardRounds switch
            {
                0 => 5,
                5 => 10,
                10 => 25,
                25 => 50,
                50 => 100,
                _ => 0
            };

            RefreshTestingVisualState();
            ForceLayoutNow();
        }

        private void OnSkipToEndClicked()
        {
            skipToEnd = !skipToEnd;
            if (!skipToEnd)
            {
                forcedResult = ForcedGameResultMode.Natural;
            }

            RefreshTestingVisualState();
            ForceLayoutNow();
        }

        private void OnForcedResultClicked()
        {
            forcedResult = forcedResult switch
            {
                ForcedGameResultMode.Natural => ForcedGameResultMode.ForcedWin,
                ForcedGameResultMode.ForcedWin => ForcedGameResultMode.ForcedLoss,
                _ => ForcedGameResultMode.Natural
            };

            RefreshTestingVisualState();
            ForceLayoutNow();
        }

        private void ApplyTestingModeToGameManager()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (!testingEnabled)
            {
                GameManager.Instance.DisableTestingMode();
                return;
            }

            int? selectedMycoId = null;
            if (mycovariantDropdown != null && mycovariantDropdown.value > 0)
            {
                int index = mycovariantDropdown.value - 1;
                if (index >= 0 && index < MycovariantRepository.All.Count)
                {
                    selectedMycoId = MycovariantRepository.All[index].Id;
                }
            }

            var forced = skipToEnd ? forcedResult : ForcedGameResultMode.Natural;
            GameManager.Instance.EnableTestingMode(selectedMycoId, fastForwardRounds, skipToEnd, forced);
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
            ApplyTestingModeToGameManager();
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StartCampaignNew();
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

        private static string FormatForcedResult(ForcedGameResultMode mode)
        {
            return mode switch
            {
                ForcedGameResultMode.ForcedWin => "Forced Win",
                ForcedGameResultMode.ForcedLoss => "Forced Loss",
                _ => "Natural"
            };
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }

            var legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = text;
            }
        }
    }
}
