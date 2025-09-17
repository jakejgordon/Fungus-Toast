using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Config;
using TMPro;
using FungusToast.Unity;
using System; // added for strict validation exceptions

namespace FungusToast.Unity.UI.GameStart
{
    public class UI_StartGamePanel : MonoBehaviour
    {
        public static UI_StartGamePanel Instance { get; private set; }

        [SerializeField] private List<UI_PlayerCountButton> playerButtons;
        [SerializeField] private Button startGameButton;

        [Header("Human Players (Hotseat)")]
        [SerializeField] private GameObject humanPlayerSectionRoot; // container for human player selector (hidden until total picked)
        [SerializeField] private List<UI_HotseatHumanCountButton> humanPlayerButtons; // 1..8 reuse same prefab style
        [SerializeField] private TextMeshProUGUI playerSummaryLabel; // "X Players (Y Human / Z AI)"

        [Header("Testing Mode")]
        [SerializeField] private Toggle testingModeToggle;
        [SerializeField] private TMP_Dropdown mycovariantDropdown;
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

        private void Awake()
        {
            Instance = this;
            // Strict validation: all required refs must be assigned in Inspector
            ValidateSerializedRefs();

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
            // Human player selection (soft validation: allow scene to run if not yet wired to avoid editor breakage)
            if (humanPlayerSectionRoot == null) Debug.LogWarning("UI_StartGamePanel: humanPlayerSectionRoot not assigned (hotseat selector will not show).");
            if (playerSummaryLabel == null) Debug.LogWarning("UI_StartGamePanel: playerSummaryLabel not assigned.");
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

        private void InitializeTestingModeUI()
        {
            // Initialize mycovariant dropdown
            mycovariantDropdown.ClearOptions();
            var options = new List<string> { "Select Mycovariant..." };
            var mycovariants = MycovariantRepository.All;
            foreach (var mycovariant in mycovariants)
                options.Add($"{mycovariant.Name} (ID: {mycovariant.Id})");
            mycovariantDropdown.AddOptions(options);
            mycovariantDropdown.value = 0;

            // Set up testing mode toggle
            testingModeToggle.onValueChanged.AddListener(OnTestingModeToggled);
            testingModePanel.SetActive(false);

            // Initialize fast-forward input
            fastForwardRoundsInput.contentType = TMP_InputField.ContentType.IntegerNumber;

            // Default state for skip-to-end toggle
            skipToEndgameToggle.isOn = false;
            skipToEndgameToggle.interactable = false; // disabled until testing mode is enabled
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

                    // Enable testing mode with or without a mycovariant selected
                    if (mycovariantDropdown.value > 0)
                    {
                        var selectedMycovariant = MycovariantRepository.All[mycovariantDropdown.value - 1];
                        GameManager.Instance.EnableTestingMode(selectedMycovariant.Id, fastForwardRounds, skipToEnd);
                    }
                    else
                    {
                        GameManager.Instance.EnableTestingMode(null, fastForwardRounds, skipToEnd);
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
    }
}
