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

        private void Awake()
        {
            Instance = this;
            // Strict validation: all required refs must be assigned in Inspector
            ValidateSerializedRefs();

            startGameButton.interactable = false;
            InitializeTestingModeUI();

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
        }

        private void UpdateButtonVisuals()
        {
            foreach (var btn in playerButtons)
                btn.SetSelected(btn.playerCount == selectedPlayerCount);
        }

        public void OnStartGamePressed()
        {
            if (selectedPlayerCount.HasValue)
            {
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
