using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Game;

public class UI_MoldProfilePanel : MonoBehaviour
{
    [Header("UI References")]
    public Image moldIconImage;
    public TextMeshProUGUI growthChanceText;
    public TextMeshProUGUI deathChanceText;
    public TextMeshProUGUI mpIncomeText;

    private Player trackedPlayer;

    public void Initialize(Player player)
    {
        trackedPlayer = player;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (trackedPlayer == null) return;

        // Get sprite from PlayerUIBinder via GameManager
        var binder = GameManager.Instance.GameUI.PlayerUIBinder;
        Debug.Log($"[UI_MoldProfilePanel] binder: {binder}, player: {trackedPlayer}");
        moldIconImage.sprite = binder.GetIcon(trackedPlayer);

        // Call method from Player class for derived values
        growthChanceText.text = $"Hyphal Outgrowth Chance: {(trackedPlayer.GetEffectiveGrowthChance() * 100f):F2}%";
        deathChanceText.text = $"Mycelial Degradation: {(trackedPlayer.GetEffectiveSelfDeathChance() * 100f):F2}%";
        mpIncomeText.text = $"MP per Turn: {trackedPlayer.GetMutationPointIncome()}";
    }

    public void Refresh()
    {
        UpdateDisplay();
    }
}
