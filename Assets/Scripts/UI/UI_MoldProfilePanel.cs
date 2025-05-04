using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Game;

public class UI_MoldProfilePanel : MonoBehaviour
{
    [Header("UI References")]
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

        growthChanceText.text = $"Hyphal Outgrowth Chance: {(trackedPlayer.GetEffectiveGrowthChance() * 100f):F2}%";
        deathChanceText.text = $"Decay Chance: {(trackedPlayer.GetEffectiveSelfDeathChance() * 100f):F2}%";
        mpIncomeText.text = $"Mutation Points per Turn: {trackedPlayer.GetBaseMutationPointIncome()}";
    }

    public void Refresh()
    {
        UpdateDisplay();
    }
}
