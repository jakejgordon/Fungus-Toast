using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using UnityEngine.EventSystems;
using FungusToast.UI;

public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Button upgradeButton;
    public TextMeshProUGUI mutationNameText;
    public TextMeshProUGUI levelText;
    public GameObject lockOverlay; // New: visual overlay
    public CanvasGroup canvasGroup; // New: for dimming

    private Mutation mutation;
    private UI_MutationManager uiManager;
    private string description;

    public void Initialize(Mutation mutation, UI_MutationManager uiManager)
    {
        this.mutation = mutation;
        this.uiManager = uiManager;
        this.description = mutation.Description;

        mutationNameText.text = mutation.Name;
        UpdateDisplay();

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
    }

    private void OnUpgradeClicked()
    {
        Debug.Log($"Upgrade Button clicked for {mutation.Name}");

        if (uiManager.TryUpgradeMutation(mutation))
        {
            UpdateDisplay();
        }
    }

    public void UpdateDisplay()
    {
        levelText.text = $"Level {mutation.CurrentLevel}/{mutation.MaxLevel}";

        if (!mutation.CanUpgrade())
        {
            upgradeButton.interactable = false;
        }
    }

    public void SetLockedState(string reason)
    {
        upgradeButton.interactable = false;
        if (lockOverlay != null) lockOverlay.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 0.5f;

        // Optional tooltip
        uiManager.ShowMutationDescription(reason, transform as RectTransform);
    }

    public void SetUnlockedState()
    {
        upgradeButton.interactable = true;
        if (lockOverlay != null) lockOverlay.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
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