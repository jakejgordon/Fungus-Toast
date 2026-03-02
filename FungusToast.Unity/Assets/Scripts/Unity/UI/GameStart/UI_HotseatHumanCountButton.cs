using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.GameStart
{
    // Separate file definition for hotseat human count selection button
    public class UI_HotseatHumanCountButton : MonoBehaviour
    {
        public int humanPlayerCount; // set via Inspector
        public Image highlightImage; // optional overlay image for selection
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                UIStyleTokens.Button.ApplyStyle(_button);
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (UI_StartGamePanel.Instance != null)
            {
                UI_StartGamePanel.Instance.OnHumanPlayerCountSelected(humanPlayerCount);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightImage != null)
            {
                var selectedTint = UIStyleTokens.Button.BackgroundSelected;
                selectedTint.a = 0.55f;
                highlightImage.color = selectedTint;
                highlightImage.enabled = isSelected;
                highlightImage.gameObject.SetActive(isSelected);
            }
        }
    }
}
