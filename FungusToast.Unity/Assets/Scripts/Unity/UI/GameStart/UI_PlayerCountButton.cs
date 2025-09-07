using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.GameStart
{
    public class UI_PlayerCountButton : MonoBehaviour
    {
        public int playerCount; // set this in the Inspector
        public Image highlightImage; // optional overlay image for selection
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            UI_StartGamePanel.Instance.OnPlayerCountSelected(playerCount);
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = isSelected;
                highlightImage.gameObject.SetActive(isSelected); 
            }
        }

    }
}