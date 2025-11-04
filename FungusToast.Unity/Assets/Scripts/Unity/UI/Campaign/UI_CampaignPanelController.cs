using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.Campaign; // for save service + GameManager extension

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel: Resume / New / Delete / Back.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private GameObject modeSelectPanel; // reference back to UI_ModeSelectPanel

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (newButton != null) newButton.onClick.AddListener(OnNewClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            bool has = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();
            if (resumeButton != null) resumeButton.interactable = has;
            if (deleteButton != null) deleteButton.interactable = has;
        }

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartCampaignResume();
            gameObject.SetActive(false);
        }

        private void OnNewClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartCampaignNew();
            gameObject.SetActive(false);
        }

        private void OnDeleteClicked()
        {
            CampaignSaveService.Delete();
            RefreshButtonStates();
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
            if (modeSelectPanel != null)
                modeSelectPanel.SetActive(true);
        }
    }
}
