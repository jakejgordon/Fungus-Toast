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
        private const float HeaderTextScale = 1.18f;

        [Header("Growth Preview (All 8 Directions)")]
        [Tooltip("Parent containers for the 8 growth preview cells (N,NE,E,SE,S,SW,W,NW). Assign the parent objects; children auto-located by name.")]
        [SerializeField] private GrowthDirectionCell[] directionCells = Array.Empty<GrowthDirectionCell>();
        [SerializeField] private Image centerPlayerIcon;
        [SerializeField] private TextMeshProUGUI growthPreviewHeaderText;

        [Header("Formatting / Visuals")]
        [SerializeField] private Color zeroChanceColor = new Color(1f,1f,1f,0.35f);
        [SerializeField] private Color finalZeroGreen = new Color(0.4f, 1f, 0.4f, 1f);
        [SerializeField] private string growthPreviewHeaderLabel = "Growth Chance Per Living Cell";

        private Player trackedPlayer;
        private List<Player> allPlayers;

        private const string ArrowName = "UI_GrowthPreviewCellArrowImage";
        private const string PercentName = "UI_GrowthPreviewCellPercentText";
        private const string SurgeName = "UI_GrowthPreviewCellSurgeText";

        private bool cellsResolved = false;
        private bool deferredRefreshRequested = false;

        private void Awake()
        {
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelPrimary);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 20f);

            zeroChanceColor = new Color(UIStyleTokens.Text.Primary.r, UIStyleTokens.Text.Primary.g, UIStyleTokens.Text.Primary.b, 0.35f);
            finalZeroGreen = UIStyleTokens.State.Success;

            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image == centerPlayerIcon)
                {
                    continue;
                }

                if (image.sprite != null)
                {
                    continue;
                }

                if (IsBrightNeutral(image.color))
                {
                    image.color = UIStyleTokens.Surface.PanelSecondary;
                }
            }

            ApplyGrowthPreviewHeaderText();
        }

        private void ApplyGrowthPreviewHeaderText()
        {
            if (growthPreviewHeaderText == null)
                growthPreviewHeaderText = TryFindGrowthHeaderLabel();

            if (growthPreviewHeaderText == null)
                return;

            growthPreviewHeaderText.text = growthPreviewHeaderLabel;
            growthPreviewHeaderText.color = UIStyleTokens.Text.Primary;
            growthPreviewHeaderText.fontStyle = FontStyles.Bold;
            ApplyTextScale(growthPreviewHeaderText, HeaderTextScale);
        }

        private TextMeshProUGUI TryFindGrowthHeaderLabel()
        {
            var labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label == null) continue;

                string value = label.text;
                if (string.IsNullOrWhiteSpace(value)) continue;

                string normalized = value.ToLowerInvariant();
                if (normalized.Contains("growth") && !normalized.Contains("%"))
                    return label;
            }

            return null;
        }

        private static void ApplyTextScale(TextMeshProUGUI label, float scale)
        {
            if (label == null || scale <= 1f) return;

            if (label.enableAutoSizing)
            {
                label.fontSizeMin *= scale;
                label.fontSizeMax *= scale;
            }
            else
            {
                label.fontSize *= scale;
            }
        }

        private static bool IsBrightNeutral(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float avg = (color.r + color.g + color.b) / 3f;
            bool lowSaturation = (max - min) < 0.10f;
            return color.a > 0.3f && avg > 0.70f && lowSaturation;
        }

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            EnsureCellsResolved();
            Refresh();

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
    }
}
