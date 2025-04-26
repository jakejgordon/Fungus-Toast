using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;

namespace FungusToast.UI
{
    public class MutationNodeUI : MonoBehaviour
    {
        [Header("UI References")]
        public Button upgradeButton;
        public TextMeshProUGUI mutationNameText;
        public TextMeshProUGUI levelText;

        private Mutation mutation;
        private MutationUIManager uiManager;

        public void Initialize(Mutation mutation, MutationUIManager uiManager)
        {
            this.mutation = mutation;
            this.uiManager = uiManager;

            mutationNameText.text = mutation.Name;
            UpdateDisplay();

            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void UpdateDisplay()
        {
            levelText.text = $"Level {mutation.CurrentLevel}/{mutation.MaxLevel}";
            upgradeButton.interactable = mutation.CanUpgrade();
        }

        private void OnUpgradeClicked()
        {
            if (uiManager.TryUpgradeMutation(mutation))
            {
                UpdateDisplay();
            }
        }
    }
}
