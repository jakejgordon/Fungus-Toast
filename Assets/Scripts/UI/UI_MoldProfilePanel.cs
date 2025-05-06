using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Game;
using System.Collections.Generic;

public class UI_MoldProfilePanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI growthChanceText;
    public TextMeshProUGUI deathChanceText;
    public TextMeshProUGUI mpIncomeText;

    private Player trackedPlayer;
    private List<Player> allPlayers;

    public void Initialize(Player player, List<Player> players)
    {
        trackedPlayer = player;
        allPlayers = players;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (trackedPlayer == null) return;

        growthChanceText.text = $"Hyphal Outgrowth Chance: {(trackedPlayer.GetEffectiveGrowthChance() * 100f):F2}%";

        if (allPlayers != null)
        {
            float decay = trackedPlayer.GetBaseMycelialDegradationRisk(allPlayers);
            deathChanceText.text = $"Mycelial Degradation: {decay * 100f:F2}%";
        }

        mpIncomeText.text = $"Mutation Points per Turn: {trackedPlayer.GetBaseMutationPointIncome()}";
    }

    public void Refresh()
    {
        UpdateDisplay();
    }
}
