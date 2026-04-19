using System;
using System.Collections.Generic;
using System.Linq;
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
        public int CampaignLevelIndex { get; }
        public string ForcedAdaptationId { get; }
        public IReadOnlyList<string> ForcedStartingAdaptationIds { get; }

        public DevelopmentTestingConfiguration(
            bool isEnabled,
            int? boardSizeOverride,
            int? mycovariantId,
            int fastForwardRounds,
            bool skipToEndGame,
            bool forceFirstGame,
            ForcedGameResultMode forcedResult,
            int campaignLevelIndex,
            string forcedAdaptationId,
            IReadOnlyList<string> forcedStartingAdaptationIds)
        {
            IsEnabled = isEnabled;
            BoardSizeOverride = boardSizeOverride;
            MycovariantId = mycovariantId;
            FastForwardRounds = fastForwardRounds;
            SkipToEndGame = skipToEndGame;
            ForceFirstGame = forceFirstGame;
            ForcedResult = forcedResult;
            CampaignLevelIndex = Math.Max(0, campaignLevelIndex);
            ForcedAdaptationId = forcedAdaptationId ?? string.Empty;
            ForcedStartingAdaptationIds = forcedStartingAdaptationIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? new List<string>();
        }
    }

    public sealed class DevelopmentTestingCardOptions
    {
        public Transform Parent { get; set; }
        public Button ButtonTemplate { get; set; }
        public TMP_Dropdown DropdownTemplate { get; set; }
        public bool SupportsForcedAdaptation { get; set; }
        public bool SupportsBoardSizeOverride { get; set; }
        public bool SupportsFirstGameToggle { get; set; } = true;
        public string CardName { get; set; } = "UI_DevelopmentTestingCard";
        public string ControlPrefix { get; set; } = "UI_DevelopmentTesting";
        public string LogPrefix { get; set; } = "DevelopmentTestingCardController";
        public Action LayoutInvalidated { get; set; }
        public Action<bool> TestingEnabledChanged { get; set; }
        public bool UseCardBackground { get; set; } = true;
        public bool UseSecondaryButtonStyle { get; set; }
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
        private const float ForcedAdaptationListViewportHeight = 220f;

        private readonly DevelopmentTestingCardOptions options;

        private GameObject cardRoot;
        private Button testingToggleButton;
        private GameObject boardSizeRow;
        private TMP_Dropdown boardSizeDropdown;
        private GameObject campaignLevelRow;
        private TMP_Dropdown campaignLevelDropdown;
        private GameObject mycovariantRow;
        private TMP_Dropdown mycovariantDropdown;
        private Button fastForwardButton;
        private Button firstGameButton;
        private Button skipToEndButton;
        private Button forcedResultButton;
        private GameObject adaptationRow;
        private TMP_Dropdown adaptationDropdown;
        private Button forcedStartingAdaptationsToggleButton;
        private GameObject forcedStartingAdaptationsRow;
        private ScrollRect forcedStartingAdaptationsScrollRect;
        private readonly List<Toggle> forcedStartingAdaptationToggles = new();
        private List<Mycovariant> sortedMycovariants = new List<Mycovariant>();
        private List<AdaptationDefinition> sortedAdaptations = new List<AdaptationDefinition>();

        private bool testingEnabled;
        private bool skipToEnd;
        private bool forceFirstGame;
        private bool forceStartingAdaptationsEnabled;
        private readonly HashSet<string> selectedForcedStartingAdaptationIds = new(StringComparer.Ordinal);
        private int fastForwardRounds;
        private int selectedBoardSize = DevelopmentTestingBoardSizePresets.DefaultSize;
        private int selectedCampaignLevelIndex;
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

        public bool IsTestingEnabled => testingEnabled;

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

            campaignLevelRow = EnsureDropdownRow(
                $"{options.ControlPrefix}CampaignLevelRow",
                $"{options.ControlPrefix}CampaignLevelLabel",
                $"{options.ControlPrefix}CampaignLevelDropdown",
                "Campaign Level",
                out campaignLevelDropdown);
            ConfigureCampaignLevelDropdown();

            mycovariantRow = EnsureDropdownRow(
                $"{options.ControlPrefix}MycovariantRow",
                $"{options.ControlPrefix}MycovariantLabel",
                $"{options.ControlPrefix}MycovariantDropdown",
                "Forced Mycovariant",
                out mycovariantDropdown);
            fastForwardButton = EnsureSettingButton($"{options.ControlPrefix}FastForwardButton", OnFastForwardClicked);

            if (options.SupportsFirstGameToggle)
            {
                firstGameButton = EnsureSettingButton($"{options.ControlPrefix}FirstGameButton", OnFirstGameClicked);
            }

            skipToEndButton = EnsureSettingButton($"{options.ControlPrefix}SkipToEndButton", OnSkipToEndClicked);
            forcedResultButton = EnsureSettingButton($"{options.ControlPrefix}ForcedResultButton", OnForcedResultClicked);

            if (options.SupportsForcedAdaptation)
            {
                adaptationRow = EnsureDropdownRow(
                    $"{options.ControlPrefix}AdaptationRow",
                    $"{options.ControlPrefix}AdaptationLabel",
                    $"{options.ControlPrefix}AdaptationDropdown",
                    "Forced Adaptation in Draft",
                    out adaptationDropdown);
            }

            forcedStartingAdaptationsToggleButton = EnsureSettingButton($"{options.ControlPrefix}ForcedStartingAdaptationsToggleButton", OnForcedStartingAdaptationsToggleClicked);
            forcedStartingAdaptationsRow = EnsureForcedStartingAdaptationsRow();

            SetSiblingIndex(testingToggleButton != null ? testingToggleButton.transform : null, 0);
            SetSiblingIndex(campaignLevelRow != null ? campaignLevelRow.transform : null, 1);
            SetSiblingIndex(fastForwardButton != null ? fastForwardButton.transform : null, 2);
            SetSiblingIndex(skipToEndButton != null ? skipToEndButton.transform : null, 3);
            SetSiblingIndex(forcedResultButton != null ? forcedResultButton.transform : null, 4);
            SetSiblingIndex(adaptationRow != null ? adaptationRow.transform : null, 5);
            SetSiblingIndex(firstGameButton != null ? firstGameButton.transform : null, 6);
            SetSiblingIndex(forcedStartingAdaptationsToggleButton != null ? forcedStartingAdaptationsToggleButton.transform : null, 7);
            SetSiblingIndex(forcedStartingAdaptationsRow != null ? forcedStartingAdaptationsRow.transform : null, 8);
            SetSiblingIndex(mycovariantRow != null ? mycovariantRow.transform : null, 9);

            RefreshDropdownOptions();
            RefreshVisualState();
        }

        public void LoadConfiguration(DevelopmentTestingConfiguration configuration)
        {
            if (configuration == null)
            {
                SetTestingEnabled(false);
                return;
            }

            if (options.SupportsBoardSizeOverride && configuration.BoardSizeOverride.HasValue)
            {
                selectedBoardSize = DevelopmentTestingBoardSizePresets.ClampToSupportedSize(configuration.BoardSizeOverride.Value);
            }

            testingEnabled = configuration.IsEnabled;
            fastForwardRounds = Math.Max(0, configuration.FastForwardRounds);
            selectedCampaignLevelIndex = Math.Max(0, configuration.CampaignLevelIndex);
            skipToEnd = testingEnabled && configuration.SkipToEndGame;
            forceFirstGame = options.SupportsFirstGameToggle && testingEnabled && configuration.ForceFirstGame;
            selectedForcedStartingAdaptationIds.Clear();
            foreach (var adaptationId in configuration.ForcedStartingAdaptationIds)
            {
                selectedForcedStartingAdaptationIds.Add(adaptationId);
            }
            forceStartingAdaptationsEnabled = testingEnabled && configuration.ForcedStartingAdaptationIds.Count > 0;
            forcedResult = skipToEnd ? configuration.ForcedResult : ForcedGameResultMode.Natural;

            RefreshDropdownOptions();

            if (boardSizeDropdown != null)
            {
                int boardSizeIndex = DevelopmentTestingBoardSizePresets.GetIndex(selectedBoardSize);
                boardSizeDropdown.SetValueWithoutNotify(boardSizeIndex);
                boardSizeDropdown.RefreshShownValue();
            }

            if (campaignLevelDropdown != null)
            {
                campaignLevelDropdown.SetValueWithoutNotify(Mathf.Max(0, Mathf.Min(selectedCampaignLevelIndex, campaignLevelDropdown.options.Count - 1)));
                campaignLevelDropdown.RefreshShownValue();
            }

            if (mycovariantDropdown != null)
            {
                int selectedMycovariantIndex = 0;
                if (configuration.MycovariantId.HasValue)
                {
                    int found = sortedMycovariants.FindIndex(mycovariant => mycovariant.Id == configuration.MycovariantId.Value);
                    if (found >= 0)
                    {
                        selectedMycovariantIndex = found + 1;
                    }
                }

                mycovariantDropdown.SetValueWithoutNotify(selectedMycovariantIndex);
                mycovariantDropdown.RefreshShownValue();
            }

            if (adaptationDropdown != null)
            {
                int selectedAdaptationIndex = 0;
                if (!string.IsNullOrWhiteSpace(configuration.ForcedAdaptationId))
                {
                    int found = sortedAdaptations.FindIndex(adaptation => string.Equals(adaptation.Id, configuration.ForcedAdaptationId, StringComparison.Ordinal));
                    if (found >= 0)
                    {
                        selectedAdaptationIndex = found + 1;
                    }
                }

                adaptationDropdown.SetValueWithoutNotify(selectedAdaptationIndex);
                adaptationDropdown.RefreshShownValue();
            }

            RefreshVisualState();
        }

        public void SetTestingEnabled(bool value)
        {
            if (testingEnabled == value)
            {
                RefreshVisualState();
                return;
            }

            testingEnabled = value;
            if (!testingEnabled)
            {
                ResetTestingState();
            }

            RefreshVisualState();
            NotifyTestingEnabledChanged();
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

            if (campaignLevelDropdown != null)
            {
                var campaignLevelOptions = BuildCampaignLevelOptions();
                campaignLevelDropdown.ClearOptions();
                campaignLevelDropdown.AddOptions(campaignLevelOptions);
                campaignLevelDropdown.value = Mathf.Max(0, Mathf.Min(selectedCampaignLevelIndex, campaignLevelOptions.Count - 1));
                campaignLevelDropdown.RefreshShownValue();
                ApplyDropdownReadability(campaignLevelDropdown);
            }

            if (mycovariantDropdown != null)
            {
                sortedMycovariants = MycovariantRepository.All
                    .OrderBy(mycovariant => mycovariant.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(mycovariant => mycovariant.Id)
                    .ToList();

                var mycovariantOptions = new List<string> { "Select Mycovariant..." };
                for (int index = 0; index < sortedMycovariants.Count; index++)
                {
                    var mycovariant = sortedMycovariants[index];
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
                sortedAdaptations = AdaptationRepository.All
                    .OrderBy(adaptation => adaptation.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(adaptation => adaptation.Id, StringComparer.Ordinal)
                    .ToList();

                var adaptationOptions = new List<string> { "Select Adaptation..." };
                for (int index = 0; index < sortedAdaptations.Count; index++)
                {
                    var adaptation = sortedAdaptations[index];
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

            if (campaignLevelRow != null)
            {
                campaignLevelRow.SetActive(testingEnabled);
            }

            if (campaignLevelDropdown != null)
            {
                campaignLevelDropdown.interactable = testingEnabled;
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

            UpdateButtonState(forcedStartingAdaptationsToggleButton, testingEnabled, testingEnabled);
            if (forcedStartingAdaptationsRow != null)
            {
                forcedStartingAdaptationsRow.SetActive(testingEnabled && forceStartingAdaptationsEnabled);
            }

            SetButtonLabel(testingToggleButton, $"Development Testing: {(testingEnabled ? "On" : "Off")}");
            SetButtonLabel(fastForwardButton, $"Fast Forward Rounds: {fastForwardRounds}");
            SetButtonLabel(firstGameButton, $"First Game?: {(forceFirstGame ? "Yes" : "No")}");
            SetButtonLabel(skipToEndButton, $"Skip to End Game: {(skipToEnd ? "On" : "Off")}");
            SetButtonLabel(forcedResultButton, $"Forced Result: {FormatForcedResult(forcedResult)}");
            SetButtonLabel(forcedStartingAdaptationsToggleButton, $"Forced Adaptations?: {(forceStartingAdaptationsEnabled ? "On" : "Off")}");

            NotifyLayoutInvalidated();
        }

        private void UpdateButtonState(Button button, bool isVisible, bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(isVisible);
            button.interactable = isInteractable;
            UIStyleTokens.Button.SetButtonLabelColor(
                button,
                options.UseSecondaryButtonStyle
                    ? (isInteractable ? UIStyleTokens.Text.Primary : UIStyleTokens.Text.Disabled)
                    : (isInteractable ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled));
        }

        public DevelopmentTestingConfiguration GetConfiguration()
        {
            if (!testingEnabled)
            {
                return new DevelopmentTestingConfiguration(false, null, null, 0, false, false, ForcedGameResultMode.Natural, 0, string.Empty, Array.Empty<string>());
            }

            int? selectedMycovariantId = null;
            if (mycovariantDropdown != null && mycovariantDropdown.value > 0)
            {
                int mycovariantIndex = mycovariantDropdown.value - 1;
                if (mycovariantIndex >= 0 && mycovariantIndex < sortedMycovariants.Count)
                {
                    selectedMycovariantId = sortedMycovariants[mycovariantIndex].Id;
                }
            }

            string selectedAdaptationId = string.Empty;
            if (skipToEnd && adaptationDropdown != null && adaptationDropdown.value > 0)
            {
                int adaptationIndex = adaptationDropdown.value - 1;
                if (adaptationIndex >= 0 && adaptationIndex < sortedAdaptations.Count)
                {
                    selectedAdaptationId = sortedAdaptations[adaptationIndex].Id;
                }
            }

            var forcedStartingAdaptationIds = forceStartingAdaptationsEnabled
                ? GetSelectedForcedStartingAdaptationIds()
                : Array.Empty<string>();

            return new DevelopmentTestingConfiguration(
                true,
                options.SupportsBoardSizeOverride ? selectedBoardSize : null,
                selectedMycovariantId,
                fastForwardRounds,
                skipToEnd,
                options.SupportsFirstGameToggle && forceFirstGame,
                skipToEnd ? forcedResult : ForcedGameResultMode.Natural,
                selectedCampaignLevelIndex,
                selectedAdaptationId,
                forcedStartingAdaptationIds);
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
                configuration.CampaignLevelIndex,
                configuration.ForcedAdaptationId,
                configuration.ForcedStartingAdaptationIds);
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

        private void ConfigureCampaignLevelDropdown()
        {
            if (campaignLevelDropdown == null)
            {
                return;
            }

            campaignLevelDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            campaignLevelDropdown.onValueChanged.AddListener(OnCampaignLevelChanged);
            campaignLevelDropdown.RefreshShownValue();
            ApplyDropdownReadability(campaignLevelDropdown);
        }

        private List<string> BuildCampaignLevelOptions()
        {
            var options = new List<string>();
            int maxLevels = GameManager.Instance?.CampaignProgression?.MaxLevels ?? 0;
            for (int levelIndex = 0; levelIndex < maxLevels; levelIndex++)
            {
                options.Add($"Campaign{levelIndex}");
            }

            if (options.Count == 0)
            {
                options.Add("Campaign0");
            }

            return options;
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
            panelColor.a = options.UseCardBackground ? 0.92f : 0f;
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
            if (options.UseSecondaryButtonStyle)
            {
                UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);
                UIStyleTokens.Button.SetButtonLabelColor(button, UIStyleTokens.Text.Primary);
            }
            else
            {
                UIStyleTokens.Button.ApplyStyle(button);
                UIStyleTokens.Button.SetButtonLabelColor(button, UIStyleTokens.Button.TextDefault);
            }

            EnsureButtonLayout(button, options.SettingWidth);

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 15f;
                label.fontSizeMax = 24f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = options.UseSecondaryButtonStyle ? UIStyleTokens.Text.Primary : UIStyleTokens.Button.TextDefault;
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

        private GameObject EnsureForcedStartingAdaptationsRow()
        {
            var existing = cardRoot.transform.Find($"{options.ControlPrefix}ForcedStartingAdaptationsRow");
            if (existing != null)
            {
                forcedStartingAdaptationToggles.Clear();
                BuildForcedStartingAdaptationChecklist(existing.gameObject);
                return existing.gameObject;
            }

            var row = new GameObject($"{options.ControlPrefix}ForcedStartingAdaptationsRow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(cardRoot.transform, false);

            var layout = row.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(6, 6, 6, 6);

            var image = row.GetComponent<Image>();
            var bg = UIStyleTokens.Surface.PanelSecondary;
            bg.a = 0.55f;
            image.color = bg;
            image.raycastTarget = false;

            var element = row.GetComponent<LayoutElement>();
            element.minHeight = ForcedAdaptationListViewportHeight + 32f;
            element.preferredHeight = ForcedAdaptationListViewportHeight + 32f;
            element.minWidth = options.SettingWidth;
            element.preferredWidth = options.SettingWidth;

            BuildForcedStartingAdaptationChecklist(row);
            return row;
        }

        private void BuildForcedStartingAdaptationChecklist(GameObject row)
        {
            forcedStartingAdaptationToggles.Clear();
            foreach (Transform child in row.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            var labelObject = new GameObject($"{options.ControlPrefix}ForcedStartingAdaptationsLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(row.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Starting Adaptations";
            label.color = UIStyleTokens.Text.Primary;
            label.enableAutoSizing = false;
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.Left;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var labelElement = labelObject.GetComponent<LayoutElement>();
            labelElement.minHeight = DropdownLabelMinHeight;
            labelElement.preferredHeight = DropdownLabelPreferredHeight;

            var viewportObject = new GameObject($"{options.ControlPrefix}ForcedStartingAdaptationsViewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(LayoutElement));
            viewportObject.transform.SetParent(row.transform, false);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            var viewportMask = viewportObject.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportElement = viewportObject.GetComponent<LayoutElement>();
            viewportElement.minHeight = ForcedAdaptationListViewportHeight;
            viewportElement.preferredHeight = ForcedAdaptationListViewportHeight;
            viewportElement.minWidth = options.SettingWidth - 12f;
            viewportElement.preferredWidth = options.SettingWidth - 12f;

            var contentObject = new GameObject($"{options.ControlPrefix}ForcedStartingAdaptationsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);

            var contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            forcedStartingAdaptationsScrollRect = row.GetComponent<ScrollRect>();
            if (forcedStartingAdaptationsScrollRect == null)
            {
                forcedStartingAdaptationsScrollRect = row.AddComponent<ScrollRect>();
            }

            forcedStartingAdaptationsScrollRect.horizontal = false;
            forcedStartingAdaptationsScrollRect.vertical = true;
            forcedStartingAdaptationsScrollRect.scrollSensitivity = 24f;
            forcedStartingAdaptationsScrollRect.viewport = viewportObject.GetComponent<RectTransform>();
            forcedStartingAdaptationsScrollRect.content = contentRect;

            foreach (var adaptation in AdaptationRepository.All
                         .Where(adaptation => !adaptation.IsStartingAdaptation)
                         .OrderBy(adaptation => adaptation.Name, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(adaptation => adaptation.Id, StringComparer.Ordinal))
            {
                var item = new GameObject($"{options.ControlPrefix}ForcedStartingAdaptation_{adaptation.Id}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                item.transform.SetParent(contentObject.transform, false);

                var itemLayout = item.GetComponent<HorizontalLayoutGroup>();
                itemLayout.childAlignment = TextAnchor.MiddleLeft;
                itemLayout.childControlWidth = false;
                itemLayout.childControlHeight = false;
                itemLayout.childForceExpandWidth = false;
                itemLayout.childForceExpandHeight = false;
                itemLayout.spacing = 8f;
                itemLayout.padding = new RectOffset(0, 0, 2, 2);

                var itemElement = item.GetComponent<LayoutElement>();
                itemElement.minHeight = 24f;
                itemElement.preferredHeight = 26f;
                itemElement.minWidth = options.SettingWidth - 32f;
                itemElement.preferredWidth = options.SettingWidth - 32f;

                var toggleRoot = new GameObject("ToggleRoot", typeof(RectTransform), typeof(LayoutElement));
                toggleRoot.transform.SetParent(item.transform, false);
                var toggleRootElement = toggleRoot.GetComponent<LayoutElement>();
                toggleRootElement.minWidth = 18f;
                toggleRootElement.preferredWidth = 18f;
                toggleRootElement.minHeight = 18f;
                toggleRootElement.preferredHeight = 18f;

                var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
                toggleObject.transform.SetParent(toggleRoot.transform, false);
                var toggleRect = toggleObject.GetComponent<RectTransform>();
                toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
                toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
                toggleRect.pivot = new Vector2(0.5f, 0.5f);
                toggleRect.sizeDelta = new Vector2(18f, 18f);
                toggleRect.anchoredPosition = Vector2.zero;

                var toggleBackground = toggleObject.GetComponent<Image>();
                toggleBackground.color = new Color(1f, 1f, 1f, 0.12f);
                toggleBackground.type = Image.Type.Sliced;

                var checkmarkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkmarkObject.transform.SetParent(toggleObject.transform, false);
                var checkmark = checkmarkObject.GetComponent<Image>();
                checkmark.color = UIStyleTokens.Text.Primary;
                var checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
                checkmarkRect.anchorMin = Vector2.zero;
                checkmarkRect.anchorMax = Vector2.one;
                checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
                checkmarkRect.offsetMin = new Vector2(4f, 4f);
                checkmarkRect.offsetMax = new Vector2(-4f, -4f);

                var toggle = toggleObject.GetComponent<Toggle>();
                toggle.targetGraphic = toggleBackground;
                toggle.graphic = checkmark;
                toggle.isOn = selectedForcedStartingAdaptationIds.Contains(adaptation.Id);
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        selectedForcedStartingAdaptationIds.Add(adaptation.Id);
                    }
                    else
                    {
                        selectedForcedStartingAdaptationIds.Remove(adaptation.Id);
                    }
                });
                forcedStartingAdaptationToggles.Add(toggle);

                var toggleLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                toggleLabelObject.transform.SetParent(item.transform, false);
                var toggleLabel = toggleLabelObject.GetComponent<TextMeshProUGUI>();
                toggleLabel.text = adaptation.Name;
                toggleLabel.color = UIStyleTokens.Text.Primary;
                toggleLabel.enableAutoSizing = false;
                toggleLabel.fontSize = 15f;
                toggleLabel.alignment = TextAlignmentOptions.Left;
                toggleLabel.overflowMode = TextOverflowModes.Ellipsis;

                var textElement = toggleLabelObject.GetComponent<LayoutElement>();
                textElement.minWidth = options.SettingWidth - 56f;
                textElement.preferredWidth = options.SettingWidth - 56f;
                textElement.minHeight = 22f;
                textElement.preferredHeight = 24f;
            }
        }

        private IReadOnlyList<string> GetSelectedForcedStartingAdaptationIds()
        {
            if (forcedStartingAdaptationsRow == null)
            {
                return Array.Empty<string>();
            }

            return selectedForcedStartingAdaptationIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
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
            SetTestingEnabled(!testingEnabled);
        }

        private void ResetTestingState()
        {
            skipToEnd = false;
            forceFirstGame = false;
            forceStartingAdaptationsEnabled = false;
            selectedForcedStartingAdaptationIds.Clear();
            fastForwardRounds = 0;
            selectedCampaignLevelIndex = 0;
            forcedResult = ForcedGameResultMode.Natural;
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

        private void OnForcedStartingAdaptationsToggleClicked()
        {
            forceStartingAdaptationsEnabled = !forceStartingAdaptationsEnabled;
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

        private void OnCampaignLevelChanged(int index)
        {
            selectedCampaignLevelIndex = Math.Max(0, index);
            NotifyLayoutInvalidated();
        }

        private void NotifyLayoutInvalidated()
        {
            options.LayoutInvalidated?.Invoke();
        }

        private void NotifyTestingEnabledChanged()
        {
            options.TestingEnabledChanged?.Invoke(testingEnabled);
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