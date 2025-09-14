using FungusToast.Core; // for MutationType enum
using FungusToast.Core.Config;
using FungusToast.Core.Mutations; // keep if other mutation id helpers needed
using FungusToast.Core.Phases; // GrowthMutationProcessor lives here
using FungusToast.Core.Players;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.VersionControl;
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
        [SerializeField] private TextMeshProUGUI statsAgeThresholdText;            // UI_StatsRootStartDecayAge

        [Header("Formatting / Visuals")] 
        [SerializeField] private Color zeroChanceColor = new Color(1f,1f,1f,0.35f);
        [SerializeField] private Color finalZeroGreen = new Color(0.4f, 1f, 0.4f, 1f);

        [Header("Help Icons")]
        [SerializeField] private AgeDelayThresholdTooltipProvider ageDelayThresholdTooltipProvider;

        private Player trackedPlayer;
        private List<Player> allPlayers;

        // Child name constants
        private const string ArrowName = "UI_GrowthPreviewCellArrowImage";
        private const string PercentName = "UI_GrowthPreviewCellPercentText";
        private const string SurgeName = "UI_GrowthPreviewCellSurgeText";

        private bool cellsResolved = false;

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            EnsureCellsResolved();
            Refresh();

            ageDelayThresholdTooltipProvider.Initialize(trackedPlayer);
        }

        public void Refresh()
        {
            if (trackedPlayer == null) return;
            EnsureCellsResolved();
            UpdateGrowthChances();
            UpdateStats();
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
            if (directionCells == null || directionCells.Length == 0) return; // defensive (will already have thrown earlier)
            foreach (var cell in directionCells)
            {
                if (cell == null) continue; // safety
                GetDirectionalGrowthChance(trackedPlayer, cell.direction, out float baseChance, out float surgeBonus);
                cell.SetChance(baseChance, surgeBonus, zeroChanceColor);
            }
        }

        private static bool IsCardinal(GrowthPreviewDirection dir) => dir == GrowthPreviewDirection.North || dir == GrowthPreviewDirection.East || dir == GrowthPreviewDirection.South || dir == GrowthPreviewDirection.West;

        // Retrieves the base (pre-multiplier) diagonal growth chance provided by individual Tendril mutations.
        private static float GetDiagonalBaseEffect(Player player, GrowthPreviewDirection dir)
        {
            switch (dir)
            {
                case GrowthPreviewDirection.Northwest: return player.GetMutationEffect(MutationType.GrowthDiagonal_NW);
                case GrowthPreviewDirection.Northeast: return player.GetMutationEffect(MutationType.GrowthDiagonal_NE);
                case GrowthPreviewDirection.Southeast: return player.GetMutationEffect(MutationType.GrowthDiagonal_SE);
                case GrowthPreviewDirection.Southwest: return player.GetMutationEffect(MutationType.GrowthDiagonal_SW);
                default: return 0f; // non-diagonal
            }
        }

        /// <summary>
        /// Computes base chance and surge bonus (if any) for a given direction.
        /// Surge applies only to orthogonal directions at present.
        /// </summary>
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
                float multiplier = GrowthMutationProcessor.GetTendrilDiagonalGrowthMultiplier(player); // includes +1 baseline
                baseChance = raw * multiplier;
                surgeBonus = 0f;
            }
        }

        private void UpdateStats()
        {
            if (allPlayers == null) return;
            int harmonyLevel = trackedPlayer.GetMutationLevel(MutationIds.HomeostaticHarmony);
            float harmony = harmonyLevel * GameBalance.HomeostaticHarmonyEffectPerLevel;
            float rawRandom = GameBalance.BaseRandomDecayChance;
            float finalRandom = Mathf.Max(0f, rawRandom - harmony);
            int chronoLevel = trackedPlayer.GetMutationLevel(MutationIds.ChronoresilientCytoplasm);
            float addedThreshold = chronoLevel * GameBalance.ChronoresilientCytoplasmEffectPerLevel;
            float ageThreshold = GameBalance.AgeAtWhichDecayChanceIncreases + addedThreshold;

            if (statsAgeThresholdText)
                statsAgeThresholdText.text = $"Age Risk Threshold: {GameBalance.ChronoresilientCytoplasmEffectPerLevel:F0} - {addedThreshold:F0}  = {ageThreshold:F0}";
        }
    }
}
