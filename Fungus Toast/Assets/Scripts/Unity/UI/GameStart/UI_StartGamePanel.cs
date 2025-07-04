using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FungusToast.Core.Mycovariants;
using TMPro;

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

        private int? selectedPlayerCount = null;

        private void Awake()
        {
            Instance = this;
            startGameButton.interactable = false;
            InitializeTestingModeUI();
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
        }

        private void OnTestingModeToggled(bool isEnabled)
        {
            testingModePanel.SetActive(isEnabled);
            mycovariantDropdown.interactable = isEnabled;
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
                if (testingModeToggle.isOn && mycovariantDropdown.value > 0)
                {
                    var selectedMycovariant = MycovariantRepository.All[mycovariantDropdown.value - 1];
                    GameManager.Instance.EnableTestingMode(selectedMycovariant.Id);
                }
                else
                {
                    GameManager.Instance.DisableTestingMode();
                }

                GameManager.Instance.InitializeGame(selectedPlayerCount.Value);
                GameManager.Instance.cameraCenterer.CenterCameraSmooth();
                gameObject.SetActive(false);
            }
        }
    }

}
