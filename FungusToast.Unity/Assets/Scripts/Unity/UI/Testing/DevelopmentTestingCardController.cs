using System;
using System.Collections.Generic;
using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Testing
{
    public sealed class DevelopmentTestingConfiguration
    {
        public bool IsEnabled { get; }
        public int? MycovariantId { get; }
        public int FastForwardRounds { get; }
        public bool SkipToEndGame { get; }
        public ForcedGameResultMode ForcedResult { get; }
        public string ForcedAdaptationId { get; }

        public DevelopmentTestingConfiguration(
            bool isEnabled,
            int? mycovariantId,
            int fastForwardRounds,
            bool skipToEndGame,
            ForcedGameResultMode forcedResult,
            string forcedAdaptationId)
        {
            IsEnabled = isEnabled;
            MycovariantId = mycovariantId;
            FastForwardRounds = fastForwardRounds;
            SkipToEndGame = skipToEndGame;
            ForcedResult = forcedResult;
            ForcedAdaptationId = forcedAdaptationId ?? string.Empty;
        }
    }

    public sealed class DevelopmentTestingCardOptions
    {
        public Transform Parent { get; set; }
        public Button ButtonTemplate { get; set; }
        public TMP_Dropdown DropdownTemplate { get; set; }
        public bool SupportsForcedAdaptation { get; set; }
        public string CardName { get; set; } = "UI_DevelopmentTestingCard";
        public string ControlPrefix { get; set; } = "UI_DevelopmentTesting";
        public string LogPrefix { get; set; } = "DevelopmentTestingCardController";
        public Action LayoutInvalidated { get; set; }
        public float CardWidth { get; set; } = 500f;
        public float SettingWidth { get; set; } = 470f;
    }

    public sealed class DevelopmentTestingCardController
    {
        private const float TestingDropdownFontSize = 18f;
        private const float TestingDropdownScrollSensitivity = 1.5f;

        private readonly DevelopmentTestingCardOptions options;

        private GameObject cardRoot;
        private Button testingToggleButton;
        private GameObject mycovariantRow;
        private TMP_Dropdown mycovariantDropdown;
        private Button fastForwardButton;
        private Button skipToEndButton;
        private Button forcedResultButton;
        private GameObject adaptationRow;
        private TMP_Dropdown adaptationDropdown;

        private bool testingEnabled;
        private bool skipToEnd;
        private int fastForwardRounds;
        private ForcedGameResultMode forcedResult = ForcedGameResultMode.Natural;

        public DevelopmentTestingCardController(DevelopmentTestingCardOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Parent == null)
            {
                throw new ArgumentNullException(nameof(options.Parent));
            }
        }

        public GameObject RootObject => cardRoot;

        public void Build()
        {
            cardRoot = EnsureCardRoot();
            testingToggleButton = EnsureSettingButton($"{options.ControlPrefix}ToggleButton", OnTestingToggleClicked);
            mycovariantRow = EnsureDropdownRow(
                $"{options.ControlPrefix}MycovariantRow",
                $"{options.ControlPrefix}MycovariantLabel",
                $"{options.ControlPrefix}MycovariantDropdown",
                "Forced Mycovariant",
                out mycovariantDropdown);
            fastForwardButton = EnsureSettingButton($"{options.ControlPrefix}FastForwardButton", OnFastForwardClicked);
            skipToEndButton = EnsureSettingButton($"{options.ControlPrefix}SkipToEndButton", OnSkipToEndClicked);
            forcedResultButton = EnsureSettingButton($"{options.ControlPrefix}ForcedResultButton", OnForcedResultClicked);

            if (options.SupportsForcedAdaptation)
            {
                adaptationRow = EnsureDropdownRow(
                    $"{options.ControlPrefix}AdaptationRow",
                    $"{options.ControlPrefix}AdaptationLabel",
                    $"{options.ControlPrefix}AdaptationDropdown",
                    "Forced Adaptation",
                    out adaptationDropdown);
            }

            SetSiblingIndex(testingToggleButton != null ? testingToggleButton.transform : null, 0);
            SetSiblingIndex(mycovariantRow != null ? mycovariantRow.transform : null, 1);
            SetSiblingIndex(fastForwardButton != null ? fastForwardButton.transform : null, 2);
            SetSiblingIndex(skipToEndButton != null ? skipToEndButton.transform : null, 3);
            SetSiblingIndex(forcedResultButton != null ? forcedResultButton.transform : null, 4);
            SetSiblingIndex(adaptationRow != null ? adaptationRow.transform : null, 5);

            RefreshDropdownOptions();
            RefreshVisualState();
        }

        public void RefreshDropdownOptions()
        {
            if (mycovariantDropdown != null)
            {
                var mycovariantOptions = new List<string> { "Select Mycovariant..." };
                for (int index = 0; index < MycovariantRepository.All.Count; index++)
                {
                    var mycovariant = MycovariantRepository.All[index];
                    mycovariantOptions.Add($"{mycovariant.Name} (ID: {mycovariant.Id})");
                }

                mycovariantDropdown.ClearOptions();
                mycovariantDropdown.AddOptions(mycovariantOptions);
                mycovariantDropdown.value = 0;
                mycovariantDropdown.RefreshShownValue();
                ApplyDropdownReadability(mycovariantDropdown);
            }

            if (adaptationDropdown != null)
            {
                var adaptationOptions = new List<string> { "Select Adaptation..." };
                for (int index = 0; index < AdaptationRepository.All.Count; index++)
                {
                    var adaptation = AdaptationRepository.All[index];
                    adaptationOptions.Add($"{adaptation.Name} (ID: {adaptation.Id})");
                }

                adaptationDropdown.ClearOptions();
                adaptationDropdown.AddOptions(adaptationOptions);
                adaptationDropdown.value = 0;
                adaptationDropdown.RefreshShownValue();
                ApplyDropdownReadability(adaptationDropdown);
            }
        }

        public void RefreshVisualState()
        {
            UpdateButtonState(testingToggleButton, true, true);

            if (mycovariantRow != null)
            {
                mycovariantRow.SetActive(testingEnabled);
            }

            UpdateButtonState(fastForwardButton, testingEnabled, testingEnabled);
            UpdateButtonState(skipToEndButton, testingEnabled, testingEnabled);
            UpdateButtonState(forcedResultButton, testingEnabled && skipToEnd, testingEnabled && skipToEnd);

            if (adaptationRow != null)
            {
                adaptationRow.SetActive(testingEnabled && skipToEnd);
            }

            if (mycovariantDropdown != null)
            {
                mycovariantDropdown.interactable = testingEnabled;
            }

            if (adaptationDropdown != null)
            {
                adaptationDropdown.interactable = testingEnabled && skipToEnd;
            }

            if (!skipToEnd)
            {
                forcedResult = ForcedGameResultMode.Natural;
                if (adaptationDropdown != null)
                {
                    adaptationDropdown.value = 0;
                    adaptationDropdown.RefreshShownValue();
                }
            }

            SetButtonLabel(testingToggleButton, $"Development Testing: {(testingEnabled ? "On" : "Off")}");
            SetButtonLabel(fastForwardButton, $"Fast Forward Rounds: {fastForwardRounds}");
            SetButtonLabel(skipToEndButton, $"Skip to End Game: {(skipToEnd ? "On" : "Off")}");
            SetButtonLabel(forcedResultButton, $"Forced Result: {FormatForcedResult(forcedResult)}");

            NotifyLayoutInvalidated();
        }

        private static void UpdateButtonState(Button button, bool isVisible, bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(isVisible);
            button.interactable = isInteractable;
            UIStyleTokens.Button.SetButtonLabelColor(
                button,
                isInteractable ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
        }

        public DevelopmentTestingConfiguration GetConfiguration()
        {
            if (!testingEnabled)
            {
                return new DevelopmentTestingConfiguration(false, null, 0, false, ForcedGameResultMode.Natural, string.Empty);
            }

            int? selectedMycovariantId = null;
            if (mycovariantDropdown != null && mycovariantDropdown.value > 0)
            {
                int mycovariantIndex = mycovariantDropdown.value - 1;
                if (mycovariantIndex >= 0 && mycovariantIndex < MycovariantRepository.All.Count)
                {
                    selectedMycovariantId = MycovariantRepository.All[mycovariantIndex].Id;
                }
            }

            string selectedAdaptationId = string.Empty;
            if (skipToEnd && adaptationDropdown != null && adaptationDropdown.value > 0)
            {
                int adaptationIndex = adaptationDropdown.value - 1;
                if (adaptationIndex >= 0 && adaptationIndex < AdaptationRepository.All.Count)
                {
                    selectedAdaptationId = AdaptationRepository.All[adaptationIndex].Id;
                }
            }

            return new DevelopmentTestingConfiguration(
                true,
                selectedMycovariantId,
                fastForwardRounds,
                skipToEnd,
                skipToEnd ? forcedResult : ForcedGameResultMode.Natural,
                selectedAdaptationId);
        }

        public void ApplyToGameManager(GameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            var configuration = GetConfiguration();
            if (!configuration.IsEnabled)
            {
                manager.DisableTestingMode();
                return;
            }

            manager.EnableTestingMode(
                configuration.MycovariantId,
                configuration.FastForwardRounds,
                configuration.SkipToEndGame,
                configuration.ForcedResult,
                configuration.ForcedAdaptationId);
        }

        private GameObject EnsureCardRoot()
        {
            var existing = options.Parent.Find(options.CardName);
            if (existing != null)
            {
                ConfigureCardRoot(existing.gameObject);
                return existing.gameObject;
            }

            var card = new GameObject(options.CardName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            card.transform.SetParent(options.Parent, false);
            ConfigureCardRoot(card);
            return card;
        }

        private void ConfigureCardRoot(GameObject card)
        {
            var cardElement = card.GetComponent<LayoutElement>();
            cardElement.minWidth = Mathf.Max(0f, options.CardWidth - 40f);
            cardElement.preferredWidth = options.CardWidth;
            cardElement.minHeight = 56f;

            var cardBackground = card.GetComponent<Image>();
            var panelColor = UIStyleTokens.Surface.PanelPrimary;
            panelColor.a = 0.92f;
            cardBackground.color = panelColor;
            cardBackground.raycastTarget = false;

            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.spacing = 6f;
            cardLayout.padding = new RectOffset(12, 12, 8, 8);
        }

        private Button EnsureSettingButton(string name, UnityEngine.Events.UnityAction action)
        {
            var existing = cardRoot.transform.Find(name);
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    existingButton.onClick = new Button.ButtonClickedEvent();
                    existingButton.onClick.AddListener(action);
                    ConfigureSettingButton(existingButton);
                    return existingButton;
                }
            }

            if (options.ButtonTemplate == null)
            {
                Debug.LogWarning($"{options.LogPrefix}: No button template available for {name}.");
                return null;
            }

            var clone = UnityEngine.Object.Instantiate(options.ButtonTemplate.gameObject, cardRoot.transform);
            clone.name = name;
            var button = clone.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"{options.LogPrefix}: Button template clone for {name} is missing a Button component.");
                return null;
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            ConfigureSettingButton(button);
            return button;
        }

        private void ConfigureSettingButton(Button button)
        {
            UIStyleTokens.Button.ApplyStyle(button);
            UIStyleTokens.Button.SetButtonLabelColor(button, UIStyleTokens.Button.TextDefault);
            EnsureButtonLayout(button, options.SettingWidth);

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 15f;
                label.fontSizeMax = 24f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = UIStyleTokens.Button.TextDefault;
            }
        }

        private GameObject EnsureDropdownRow(string rowName, string labelName, string dropdownName, string labelText, out TMP_Dropdown dropdown)
        {
            dropdown = null;
            var existing = cardRoot.transform.Find(rowName);
            if (existing != null)
            {
                dropdown = existing.GetComponentInChildren<TMP_Dropdown>(true);
                ConfigureDropdownRow(existing.gameObject, labelName, labelText);
                ConfigureDropdown(dropdown);
                return existing.gameObject;
            }

            var row = new GameObject(rowName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(cardRoot.transform, false);
            ConfigureDropdownRow(row, labelName, labelText);

            if (options.DropdownTemplate != null)
            {
                var dropdownObject = UnityEngine.Object.Instantiate(options.DropdownTemplate.gameObject, row.transform);
                dropdownObject.name = dropdownName;
                dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
                ConfigureDropdown(dropdown);

                var dropdownElement = dropdownObject.GetComponent<LayoutElement>();
                if (dropdownElement == null)
                {
                    dropdownElement = dropdownObject.AddComponent<LayoutElement>();
                }

                dropdownElement.minHeight = 40f;
                dropdownElement.preferredHeight = 44f;
                dropdownElement.minWidth = options.SettingWidth - 10f;
                dropdownElement.preferredWidth = options.SettingWidth;
            }
            else
            {
                Debug.LogWarning($"{options.LogPrefix}: No TMP_Dropdown template found; {labelText} selector unavailable.");
            }

            return row;
        }

        private static void ConfigureDropdownRow(GameObject row, string labelName, string labelText)
        {
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

            var existingLabel = row.transform.Find(labelName);
            GameObject labelObject;
            if (existingLabel != null)
            {
                labelObject = existingLabel.gameObject;
            }
            else
            {
                labelObject = new GameObject(labelName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                labelObject.transform.SetParent(row.transform, false);
            }

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.color = UIStyleTokens.Text.Primary;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 20f;
            label.alignment = TextAlignmentOptions.Left;

            var labelElement = labelObject.GetComponent<LayoutElement>();
            labelElement.minHeight = 22f;
            labelElement.preferredHeight = 26f;
        }

        private void ConfigureDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            ApplyDropdownReadability(dropdown);
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

            RefreshVisualState();
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

            RefreshVisualState();
        }

        private void OnSkipToEndClicked()
        {
            skipToEnd = !skipToEnd;
            forcedResult = skipToEnd ? ForcedGameResultMode.ForcedWin : ForcedGameResultMode.Natural;
            RefreshVisualState();
        }

        private void OnForcedResultClicked()
        {
            forcedResult = forcedResult switch
            {
                ForcedGameResultMode.Natural => ForcedGameResultMode.ForcedWin,
                ForcedGameResultMode.ForcedWin => ForcedGameResultMode.ForcedLoss,
                _ => ForcedGameResultMode.Natural
            };

            RefreshVisualState();
        }

        private void NotifyLayoutInvalidated()
        {
            options.LayoutInvalidated?.Invoke();
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

        private static void SetSiblingIndex(Transform target, int index)
        {
            if (target != null)
            {
                target.SetSiblingIndex(index);
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
                dropdown.captionText.fontSize = TestingDropdownFontSize;
                dropdown.captionText.fontSizeMin = TestingDropdownFontSize;
                dropdown.captionText.fontSizeMax = TestingDropdownFontSize;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = UIStyleTokens.Button.TextDefault;
                dropdown.itemText.enableAutoSizing = false;
                dropdown.itemText.fontSize = TestingDropdownFontSize;
                dropdown.itemText.fontSizeMin = TestingDropdownFontSize;
                dropdown.itemText.fontSizeMax = TestingDropdownFontSize;
            }

            if (dropdown.template != null)
            {
                var scrollRect = dropdown.template.GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null)
                {
                    scrollRect.scrollSensitivity = TestingDropdownScrollSensitivity;
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
                    templateLabel.fontSize = TestingDropdownFontSize;
                    templateLabel.fontSizeMin = TestingDropdownFontSize;
                    templateLabel.fontSizeMax = TestingDropdownFontSize;
                }
            }

            var labels = dropdown.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                var label = labels[index];
                if (label == null)
                {
                    continue;
                }

                label.color = label.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0
                    ? UIStyleTokens.Text.Disabled
                    : UIStyleTokens.Button.TextDefault;
                label.enableAutoSizing = false;
                label.fontSize = TestingDropdownFontSize;
                label.fontSizeMin = TestingDropdownFontSize;
                label.fontSizeMax = TestingDropdownFontSize;
            }
        }
    }
}