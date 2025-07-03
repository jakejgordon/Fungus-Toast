using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.GameStart
{
    public class UI_StartGamePanel : MonoBehaviour
    {
        public static UI_StartGamePanel Instance { get; private set; }

        [SerializeField] private List<UI_PlayerCountButton> playerButtons;
        [SerializeField] private Button startGameButton;

        private int? selectedPlayerCount = null;

        private void Awake()
        {
            Instance = this;
            startGameButton.interactable = false;
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
                GameManager.Instance.InitializeGame(selectedPlayerCount.Value);
                GameManager.Instance.cameraCenterer.CenterCameraSmooth();
                gameObject.SetActive(false);
            }
        }
    }

}
