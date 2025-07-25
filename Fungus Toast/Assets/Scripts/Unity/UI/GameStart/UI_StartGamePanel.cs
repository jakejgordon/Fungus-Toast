using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Config;
using TMPro;
using FungusToast.Unity;

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

        // Magnifying glass UI reference
        [SerializeField] private GameObject magnifyingGlassUI;
        // Magnifier visuals (child of magnifyingGlassUI)
        [SerializeField] private GameObject magnifierVisualRoot;

        private int? selectedPlayerCount = null;

        private void Awake()
        {
            Instance = this;
            startGameButton.interactable = false;
            InitializeTestingModeUI();

            // Ensure magnifier visuals are disabled at startup
            if (magnifierVisualRoot != null)
                magnifierVisualRoot.SetActive(false);
        }

        private void InitializeTestingModeUI()
        {
            // Initialize mycovariant dropdown
            mycovariantDropdown.ClearOptions();
            var options = new List<string> { "Select Mycovariant..." };
            var mycovariants = MycovariantRepository.All;
            
            foreach (var mycovariant in mycovariants)
            {
                options.Add($"{mycovariant.Name} (ID: {mycovariant.Id})");
            }
            
            mycovariantDropdown.AddOptions(options);
            mycovariantDropdown.value = 0;
            
            // Set up testing mode toggle
            testingModeToggle.onValueChanged.AddListener(OnTestingModeToggled);
            testingModePanel.SetActive(false);
            
            // Initialize fast-forward input
            fastForwardRoundsInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        private void OnTestingModeToggled(bool isEnabled)
        {
            testingModePanel.SetActive(isEnabled);
            mycovariantDropdown.interactable = isEnabled;
            fastForwardRoundsInput.interactable = isEnabled;
            fastForwardLabel.gameObject.SetActive(isEnabled);
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
            {
                btn.SetSelected(btn.playerCount == selectedPlayerCount);
            }
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
                    {
                        fastForwardRounds = Mathf.Max(0, parsedRounds);
                    }
                    
                    // Enable testing mode with or without a mycovariant selected
                    if (mycovariantDropdown.value > 0)
                    {
                        // Mycovariant selected - enable testing mode with specific mycovariant
                        var selectedMycovariant = MycovariantRepository.All[mycovariantDropdown.value - 1];
                        GameManager.Instance.EnableTestingMode(selectedMycovariant.Id, fastForwardRounds);
                    }
                    else
                    {
                        // No mycovariant selected - enable testing mode without specific mycovariant (will skip draft)
                        GameManager.Instance.EnableTestingMode(null, fastForwardRounds);
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
                // Enable the magnifier visuals
                if (magnifierVisualRoot != null)
                    magnifierVisualRoot.SetActive(true);
                // Set the flag so the magnifier can appear
                MagnifyingGlassFollowMouse.gameStarted = true;
            }
        }
    }

}
