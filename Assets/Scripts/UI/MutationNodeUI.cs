using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using UnityEngine.EventSystems;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null)
            uiManager.ShowMutationDescription(description, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
            uiManager.ClearMutationDescription();
    }
}
