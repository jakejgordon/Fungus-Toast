using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.UI.GameStart; // for UI_StartGamePanel

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// First screen shown on launch: lets player choose Hotseat (single game) or Campaign.
    /// </summary>
    public class UI_ModeSelectPanelController : MonoBehaviour
    {
        [Header("Panels")] 
        [SerializeField] private UI_StartGamePanel startGamePanel; // existing start / player config panel
        [SerializeField] private GameObject campaignPanel; // UI_CampaignPanel root

        [Header("Buttons")] 
        [SerializeField] private Button hotseatButton;
        [SerializeField] private Button campaignButton;

        private void Awake()
        {
            ApplyStyle();

            if (hotseatButton != null) hotseatButton.onClick.AddListener(OnHotseatClicked);
            if (campaignButton != null) campaignButton.onClick.AddListener(OnCampaignClicked);
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyStyle(hotseatButton);
            UIStyleTokens.Button.ApplyStyle(campaignButton);
        }

        private void OnEnable()
        {
            // Ensure subordinate panels start hidden so only mode select is visible.
            if (startGamePanel != null) startGamePanel.gameObject.SetActive(false);
            if (campaignPanel != null) campaignPanel.SetActive(false);
        }

        private void OnHotseatClicked()
        {
            // Show the existing start game panel (player count selection etc.)
            if (startGamePanel != null)
                startGamePanel.gameObject.SetActive(true);
            // Hide self
            gameObject.SetActive(false);
        }

        private void OnCampaignClicked()
        {
            if (campaignPanel != null)
                campaignPanel.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
