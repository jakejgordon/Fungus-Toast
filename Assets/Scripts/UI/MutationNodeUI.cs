using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;
using UnityEngine.EventSystems; // 🆕 Needed for hover detection

public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Button upgradeButton;
    public TextMeshProUGUI mutationNameText;
    public TextMeshProUGUI levelText;

    private Mutation mutation;
    private MutationUIManager uiManager;
    private string description;

    public void Initialize(Mutation mutation, MutationUIManager uiManager)
    {
        this.mutation = mutation;
        this.uiManager = uiManager;
        this.description = mutation.Description;

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

        if (!mutation.CanUpgrade())
        {
            upgradeButton.interactable = false;
        }
    }

    public string GetDescription()
    {
        return description;
    }

    // Called when mouse pointer enters the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        uiManager.ShowMutationDescription(description);
    }

    // Called when mouse pointer exits the button
    public void OnPointerExit(PointerEventData eventData)
    {
        uiManager.ClearMutationDescription();
    }
}
