using FungusToast.Core; // for MutationType enum
using FungusToast.Core.Config;
using FungusToast.Core.Mutations; // keep if other mutation id helpers needed
using FungusToast.Core.Phases; // GrowthMutationProcessor lives here
using FungusToast.Core.Players;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class UI_MoldProfileRoot : MonoBehaviour
    {
        [Header("Growth Preview (All 8 Directions)")]
        [Tooltip("Parent containers for the 8 growth preview cells (N,NE,E,SE,S,SW,W,NW). Assign the parent objects; children auto-located by name.")]
        [SerializeField] private GrowthDirectionCell[] directionCells = Array.Empty<GrowthDirectionCell>();
        [SerializeField] private Image centerPlayerIcon;

        [Header("Stat Text References")]
        [SerializeField] private TextMeshProUGUI statsAgeThresholdText; // UI_StatsRootStartDecayAge

        [Header("Formatting / Visuals")]
        [SerializeField] private Color zeroChanceColor = new Color(1f,1f,1f,0.35f);
        [SerializeField] private Color finalZeroGreen = new Color(0.4f, 1f, 0.4f, 1f);

        [Header("Help Icons")]
        [SerializeField] private AgeDelayThresholdTooltipProvider ageDelayThresholdTooltipProvider;

        private Player trackedPlayer;
        private List<Player> allPlayers;

        private const string ArrowName = "UI_GrowthPreviewCellArrowImage";
        private const string PercentName = "UI_GrowthPreviewCellPercentText";
        private const string SurgeName = "UI_GrowthPreviewCellSurgeText";

        private bool cellsResolved = false;
        private bool deferredRefreshRequested = false;

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            EnsureCellsResolved();
            Refresh();
            ageDelayThresholdTooltipProvider.Initialize(trackedPlayer);

            // Center Player Icon
            if (centerPlayerIcon != null)
            {
                try
                {
                    var sprite = GameManager.Instance?.gridVisualizer?.GetTileForPlayer(player.PlayerId)?.sprite;
                    if (sprite != null) { centerPlayerIcon.sprite = sprite; centerPlayerIcon.enabled = true; }
                    else centerPlayerIcon.enabled = false;
                }
                catch { centerPlayerIcon.enabled = false; }
            }
        }

        public void Refresh()
        {
            if (trackedPlayer == null) return;

            // Skip updates during fast-forward; mark deferred
            if (GameManager.Instance != null && GameManager.Instance.IsFastForwarding)
            {
                deferredRefreshRequested = true;
                return;
            }

            EnsureCellsResolved();
            UpdateGrowthChances();
            UpdateStats();
            deferredRefreshRequested = false;
        }

        // Called by GameManager after fast-forward completes
        public void ApplyDeferredRefreshIfNeeded()
        {
            if (deferredRefreshRequested)
            {
                Refresh();
            }
        }

        public void SwitchPlayer(Player player, List<Player> players)
        {
            if (player == null) { Debug.LogError("UI_MoldProfileRoot.SwitchPlayer called with null player"); return; }
            trackedPlayer = player;
            allPlayers = players;
            // Re-init tooltip provider if present
            if (ageDelayThresholdTooltipProvider != null)
            {
                ageDelayThresholdTooltipProvider.Initialize(trackedPlayer);
            }

            // Center Player Icon
            if (centerPlayerIcon != null)
            {
                try
                {
                    var sprite = GameManager.Instance?.gridVisualizer?.GetTileForPlayer(player.PlayerId)?.sprite;
                    if (sprite != null) { centerPlayerIcon.sprite = sprite; centerPlayerIcon.enabled = true; }
                    else centerPlayerIcon.enabled = false;
                }
                catch { centerPlayerIcon.enabled = false; }
            }

            Refresh();
        }

        private void EnsureCellsResolved()
        {
            if (cellsResolved) return;
            if (directionCells == null || directionCells.Length == 0)
                throw new Exception("Growth direction cells not assigned on UI_MoldProfileRoot.");

            foreach (var cell in directionCells)
            {
                if (cell == null)
                    throw new Exception("A GrowthDirectionCell entry is null in UI_MoldProfileRoot.");
                if (cell.parent == null)
                    throw new Exception($"GrowthDirectionCell '{cell.direction}' parent GameObject not assigned.");
                cell.ResolveChildren(ArrowName, PercentName, SurgeName);
            }
            cellsResolved = true;
        }

        private void UpdateGrowthChances()
        {
            if (directionCells == null || directionCells.Length == 0) return;
            foreach (var cell in directionCells)
            {
                if (cell == null) continue;
                GetDirectionalGrowthChance(trackedPlayer, cell.direction, out float baseChance, out float surgeBonus);
                cell.SetChance(baseChance, surgeBonus, zeroChanceColor);
            }
        }

        private static bool IsCardinal(GrowthPreviewDirection dir)
            => dir == GrowthPreviewDirection.North || dir == GrowthPreviewDirection.East || dir == GrowthPreviewDirection.South || dir == GrowthPreviewDirection.West;

        private static float GetDiagonalBaseEffect(Player player, GrowthPreviewDirection dir)
        {
            return dir switch
            {
                GrowthPreviewDirection.Northwest => player.GetMutationEffect(MutationType.GrowthDiagonal_NW),
                GrowthPreviewDirection.Northeast => player.GetMutationEffect(MutationType.GrowthDiagonal_NE),
                GrowthPreviewDirection.Southeast => player.GetMutationEffect(MutationType.GrowthDiagonal_SE),
                GrowthPreviewDirection.Southwest => player.GetMutationEffect(MutationType.GrowthDiagonal_SW),
                _ => 0f
            };
        }

        private void GetDirectionalGrowthChance(Player player, GrowthPreviewDirection dir, out float baseChance, out float surgeBonus)
        {
            if (IsCardinal(dir))
            {
                (float orthBase, float surge) = GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(player);
                baseChance = orthBase;
                surgeBonus = surge;
            }
            else
            {
                float raw = GetDiagonalBaseEffect(player, dir);
                float multiplier = GrowthMutationProcessor.GetTendrilDiagonalGrowthMultiplier(player);
                baseChance = raw * multiplier;
                surgeBonus = 0f;
            }
        }

        private void UpdateStats()
        {
            if (allPlayers == null || trackedPlayer == null) return;
            int harmonyLevel = trackedPlayer.GetMutationLevel(MutationIds.HomeostaticHarmony);
            float harmony = harmonyLevel * GameBalance.HomeostaticHarmonyEffectPerLevel;
            float rawRandom = GameBalance.BaseRandomDecayChance;
            float finalRandom = Mathf.Max(0f, rawRandom - harmony); // currently unused but retained for future UI
            int chronoLevel = trackedPlayer.GetMutationLevel(MutationIds.ChronoresilientCytoplasm);
            float addedThreshold = chronoLevel * GameBalance.ChronoresilientCytoplasmEffectPerLevel;
            float ageThreshold = GameBalance.AgeAtWhichDecayChanceIncreases + addedThreshold;

            if (statsAgeThresholdText)
                statsAgeThresholdText.text = $"Age Risk Threshold: {GameBalance.ChronoresilientCytoplasmEffectPerLevel:F0} - {addedThreshold:F0}  = {ageThreshold:F0}";
        }
    }
}
