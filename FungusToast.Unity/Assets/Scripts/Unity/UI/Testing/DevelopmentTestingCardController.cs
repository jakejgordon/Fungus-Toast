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
    public static class DevelopmentTestingBoardSizePresets
    {
        public const int DefaultSize = 160;
        public const int MinimumSize = 10;
        public const int MaximumSize = 200;
        public const int Increment = 5;

        private static readonly List<int> Sizes = BuildSizes();

        public static IReadOnlyList<int> All => Sizes;

        public static int ClampToSupportedSize(int size)
        {
            if (size <= MinimumSize)
            {
                return MinimumSize;
            }

            if (size >= MaximumSize)
            {
                return MaximumSize;
            }

            int offset = size - MinimumSize;
            int steps = (int)Math.Round(offset / (double)Increment, MidpointRounding.AwayFromZero);
            return MinimumSize + (steps * Increment);
        }

        public static List<string> BuildLabels()
        {
            var labels = new List<string>(Sizes.Count);
            for (int index = 0; index < Sizes.Count; index++)
            {
                int size = Sizes[index];
                labels.Add($"{size}x{size}");
            }

            return labels;
        }

        public static int GetIndex(int size)
        {
            int clampedSize = ClampToSupportedSize(size);
            int index = Sizes.IndexOf(clampedSize);
            return index >= 0 ? index : Sizes.IndexOf(DefaultSize);
        }

        public static int GetSizeAt(int index)
        {
            if (index < 0 || index >= Sizes.Count)
            {
                return DefaultSize;
            }

            return Sizes[index];
        }

        private static List<int> BuildSizes()
        {
            var sizes = new List<int>();
            for (int size = MinimumSize; size <= MaximumSize; size += Increment)
            {
                sizes.Add(size);
            }

            return sizes;
        }
    }

    public static class DevelopmentTestingFastForwardPresets
    {
        private static readonly int[] Presets = { 0, 5, 10, 15, 20, 25, 30, 35 };

        public static int GetNext(int current)
        {
            int index = Array.IndexOf(Presets, current);
            if (index < 0 || index >= Presets.Length - 1)
            {
                return Presets[0];
            }

            return Presets[index + 1];
        }
    }

    public sealed class DevelopmentTestingConfiguration
    {
        public bool IsEnabled { get; }
        public int? BoardSizeOverride { get; }
        public int? MycovariantId { get; }
        public int FastForwardRounds { get; }
        public bool SkipToEndGame { get; }
        public bool ForceFirstGame { get; }
        public ForcedGameResultMode ForcedResult { get; }
        public string ForcedAdaptationId { get; }

        public DevelopmentTestingConfiguration(
            bool isEnabled,
            int? boardSizeOverride,
            int? mycovariantId,
            int fastForwardRounds,
            bool skipToEndGame,
            bool forceFirstGame,
            ForcedGameResultMode forcedResult,
            string forcedAdaptationId)
        {
            IsEnabled = isEnabled;
            BoardSizeOverride = boardSizeOverride;
            MycovariantId = mycovariantId;
            FastForwardRounds = fastForwardRounds;
            SkipToEndGame = skipToEndGame;
            ForceFirstGame = forceFirstGame;
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
        public bool SupportsBoardSizeOverride { get; set; }
        public string CardName { get; set; } = "UI_DevelopmentTestingCard";
        public string ControlPrefix { get; set; } = "UI_DevelopmentTesting";
        public string LogPrefix { get; set; } = "DevelopmentTestingCardController";
        public Action LayoutInvalidated { get; set; }
        public float CardWidth { get; set; } = 500f;
        public float SettingWidth { get; set; } = 470f;
    }

    public sealed class DevelopmentTestingCardController
    {
        private const float CardVerticalSpacing = 4f;
        private const int CardHorizontalPadding = 12;
        private const int CardVerticalPadding = 6;
        private const float SettingButtonMinHeight = 40f;
        private const float SettingButtonPreferredHeight = 44f;
        private const float DropdownRowMinHeight = 64f;
        private const float DropdownRowPreferredHeight = 70f;
        private const float DropdownControlMinHeight = 34f;
        private const float DropdownControlPreferredHeight = 38f;
        private const float DropdownLabelMinHeight = 18f;
        private const float DropdownLabelPreferredHeight = 22f;
        private const float TestingDropdownFontSize = 18f;
        private const float TestingDropdownScrollSensitivity = 1.5f;

        private readonly DevelopmentTestingCardOptions options;

        private GameObject cardRoot;
        private Button testingToggleButton;
        private GameObject boardSizeRow;
        private TMP_Dropdown boardSizeDropdown;
        private GameObject mycovariantRow;
        private TMP_Dropdown mycovariantDropdown;
        private Button fastForwardButton;
        private Button firstGameButton;
        private Button skipToEndButton;
        private Button forcedResultButton;
        private GameObject adaptationRow;
        private TMP_Dropdown adaptationDropdown;

        private bool testingEnabled;
        private bool skipToEnd;
        private bool forceFirstGame;
        private int fastForwardRounds;
        private int selectedBoardSize = DevelopmentTestingBoardSizePresets.DefaultSize;
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

            if (options.SupportsBoardSizeOverride)
            {
                boardSizeRow = EnsureDropdownRow(
                    $"{options.ControlPrefix}BoardSizeRow",
                    $"{options.ControlPrefix}BoardSizeLabel",
                    $"{options.ControlPrefix}BoardSizeDropdown",
                    "Board Size",
                    out boardSizeDropdown);
                ConfigureBoardSizeDropdown();
            }

            mycovariantRow = EnsureDropdownRow(
                $"{options.ControlPrefix}MycovariantRow",
                $"{options.ControlPrefix}MycovariantLabel",
                $"{options.ControlPrefix}MycovariantDropdown",
                "Forced Mycovariant",
                out mycovariantDropdown);
            fastForwardButton = EnsureSettingButton($"{options.ControlPrefix}FastForwardButton", OnFastForwardClicked);
            firstGameButton = EnsureSettingButton($"{options.ControlPrefix}FirstGameButton", OnFirstGameClicked);
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
            SetSiblingIndex(boardSizeRow != null ? boardSizeRow.transform : null, 1);
            SetSiblingIndex(mycovariantRow != null ? mycovariantRow.transform : null, 2);
            SetSiblingIndex(fastForwardButton != null ? fastForwardButton.transform : null, 3);
            SetSiblingIndex(firstGameButton != null ? firstGameButton.transform : null, 4);
            SetSiblingIndex(skipToEndButton != null ? skipToEndButton.transform : null, 5);
            SetSiblingIndex(forcedResultButton != null ? forcedResultButton.transform : null, 6);
            SetSiblingIndex(adaptationRow != null ? adaptationRow.transform : null, 7);

            RefreshDropdownOptions();
            RefreshVisualState();
        }

        public void RefreshDropdownOptions()
        {
            if (boardSizeDropdown != null)
            {
                int selectedIndex = DevelopmentTestingBoardSizePresets.GetIndex(selectedBoardSize);
                boardSizeDropdown.ClearOptions();
                boardSizeDropdown.AddOptions(DevelopmentTestingBoardSizePresets.BuildLabels());
                boardSizeDropdown.value = selectedIndex;
                boardSizeDropdown.RefreshShownValue();
                ApplyDropdownReadability(boardSizeDropdown);
            }

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

            if (boardSizeRow != null)
            {
                boardSizeRow.SetActive(testingEnabled);
            }

            if (mycovariantRow != null)
            {
                mycovariantRow.SetActive(testingEnabled);
            }

            UpdateButtonState(fastForwardButton, testingEnabled, testingEnabled);
            UpdateButtonState(firstGameButton, testingEnabled, testingEnabled);
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

            if (boardSizeDropdown != null)
            {
                boardSizeDropdown.interactable = testingEnabled;
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
            SetButtonLabel(firstGameButton, $"First Game?: {(forceFirstGame ? "Yes" : "No")}");
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
                return new DevelopmentTestingConfiguration(false, null, null, 0, false, false, ForcedGameResultMode.Natural, string.Empty);
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
                options.SupportsBoardSizeOverride ? selectedBoardSize : null,
                selectedMycovariantId,
                fastForwardRounds,
                skipToEnd,
                forceFirstGame,
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
                configuration.ForceFirstGame,
                configuration.ForcedResult,
                configuration.ForcedAdaptationId);
        }

        private void ConfigureBoardSizeDropdown()
        {
            if (boardSizeDropdown == null)
            {
                return;
            }

            boardSizeDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            boardSizeDropdown.onValueChanged.AddListener(OnBoardSizeChanged);
            selectedBoardSize = DevelopmentTestingBoardSizePresets.ClampToSupportedSize(selectedBoardSize);
            boardSizeDropdown.value = DevelopmentTestingBoardSizePresets.GetIndex(selectedBoardSize);
            boardSizeDropdown.RefreshShownValue();
            ApplyDropdownReadability(boardSizeDropdown);
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
            cardElement.minHeight = 44f;

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
            cardLayout.spacing = CardVerticalSpacing;
            cardLayout.padding = new RectOffset(CardHorizontalPadding, CardHorizontalPadding, CardVerticalPadding, CardVerticalPadding);
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

                dropdownElement.minHeight = DropdownControlMinHeight;
                dropdownElement.preferredHeight = DropdownControlPreferredHeight;
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
            rowLayout.spacing = CardVerticalSpacing;
            rowLayout.padding = new RectOffset(2, 2, 0, 0);

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = DropdownRowMinHeight;
            rowElement.preferredHeight = DropdownRowPreferredHeight;

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
            label.fontSizeMin = 14f;
            label.fontSizeMax = 18f;
            label.alignment = TextAlignmentOptions.Left;

            var labelElement = labelObject.GetComponent<LayoutElement>();
            labelElement.minHeight = DropdownLabelMinHeight;
            labelElement.preferredHeight = DropdownLabelPreferredHeight;
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
                forceFirstGame = false;
                fastForwardRounds = 0;
                forcedResult = ForcedGameResultMode.Natural;
            }

            RefreshVisualState();
        }

        private void OnFastForwardClicked()
        {
            fastForwardRounds = DevelopmentTestingFastForwardPresets.GetNext(fastForwardRounds);

            RefreshVisualState();
        }

        private void OnFirstGameClicked()
        {
            forceFirstGame = !forceFirstGame;
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

        private void OnBoardSizeChanged(int index)
        {
            selectedBoardSize = DevelopmentTestingBoardSizePresets.GetSizeAt(index);
            NotifyLayoutInvalidated();
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

            element.minHeight = SettingButtonMinHeight;
            element.preferredHeight = SettingButtonPreferredHeight;
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