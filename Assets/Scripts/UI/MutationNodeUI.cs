using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core; // Assuming Mutation is in this namespace

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

    private void OnUpgradeClicked()
    {
        if (uiManager.TryUpgradeMutation(mutation))
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        levelText.text = $"Level {mutation.CurrentLevel}/{mutation.MaxLevel}";

        // Disable button if fully upgraded
        if (!mutation.CanUpgrade())
        {
            upgradeButton.interactable = false;
        }
    }
}
