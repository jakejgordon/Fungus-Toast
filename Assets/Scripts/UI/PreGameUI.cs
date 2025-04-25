using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreGameUI : MonoBehaviour
{
    public TMP_Dropdown playerCountDropdown;
    public Button startButton;
    public GameManager gameManager;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        int selectedIndex = playerCountDropdown.value;
        int playerCount = int.Parse(playerCountDropdown.options[selectedIndex].text);

        gameManager.InitializeGame(playerCount);
        gameObject.SetActive(false); // hide UI
    }
}
