using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Game;

public class PreGameUI : MonoBehaviour
{
    public TMP_Dropdown playerCountDropdown;
    public Button startButton;
    public GameManager gameManager;
    public GameObject startGameUI;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        int selectedIndex = playerCountDropdown.value;
        int playerCount = int.Parse(playerCountDropdown.options[selectedIndex].text);

        gameManager.InitializeGame(playerCount);
        gameManager.cameraCenterer.CenterCameraSmooth();

        startGameUI.SetActive(false); // Only hide the start-game controls, not the whole UI
    }

}
