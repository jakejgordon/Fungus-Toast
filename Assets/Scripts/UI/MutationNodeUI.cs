using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using FungusToast.UI;

/// <summary>
/// Handles display and interaction for a single mutation node in the tree.
/// </summary>
public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Button upgradeButton;
    public TextMeshProUGUI mutationNameText;
    public TextMeshProUGUI levelText;
    public GameObject lockOverlay;
    public CanvasGroup canvasGroup;

    private Mutation mutation;
    private Player player;
    private UI_MutationManager uiManager;
    private string description;

    /// <summary>
    /// Initializes this mutation UI element with the relevant player, mutation, and manager.
    /// </summary>
    public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
    {
        this.mutation = mutation;
        this.player = player;
        this.uiManager = uiManager;
        this.description = mutation?.Description ?? "No description available";

        mutationNameText.text = mutation?.Name ?? "Unknown Mutation";

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClicked);

        UpdateDisplay();
    }

    private void OnUpgradeClicked()
    {
        if (mutation == null || player == null || uiManager == null)
        {
            Debug.LogWarning("⚠️ MutationNodeUI.OnUpgradeClicked: Incomplete setup.");
            return;
        }

        Debug.Log($"🧬 Upgrade Button clicked for {mutation.Name}");

        if (uiManager.TryUpgradeMutation(mutation))
        {
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Refreshes the level and interactivity display for this mutation.
    /// </summary>
    public void UpdateDisplay()
    {
        if (mutation == null || player == null)
        {
            Debug.LogWarning("⚠️ MutationNodeUI.UpdateDisplay: Missing mutation or player.");
            return;
        }

        int currentLevel = player.GetMutationLevel(mutation.Id);
        levelText.text = $"Level {currentLevel}/{mutation.MaxLevel}";
        upgradeButton.interactable = player.CanUpgrade(mutation);
    }

    /// <summary>
    /// Visually marks this node as locked and dims it.
    /// </summary>
    public void SetLockedState(string reason)
    {
        upgradeButton.interactable = false;

        if (lockOverlay != null)
            lockOverlay.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0.5f;

        uiManager?.ShowMutationDescription(reason, transform as RectTransform);
    }

    /// <summary>
    /// Restores full interactivity and opacity.
    /// </summary>
    public void SetUnlockedState()
    {
        upgradeButton.interactable = true;

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        uiManager?.ShowMutationDescription(description, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        uiManager?.ClearMutationDescription();
    }
}
