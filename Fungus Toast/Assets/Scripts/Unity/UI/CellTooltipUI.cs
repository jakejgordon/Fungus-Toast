using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Board;
using FungusToast.Core.Death;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Component for the Cell Tooltip prefab that handles displaying cell information
    /// with proper layout and icons.
    /// </summary>
    public class CellTooltipUI : MonoBehaviour
    {
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI deathReasonText;
        [SerializeField] private TextMeshProUGUI ownerText;
        [SerializeField] private TextMeshProUGUI lastOwnerText;
        [SerializeField] private TextMeshProUGUI growthAgeText;
        [SerializeField] private TextMeshProUGUI expirationText;
        [SerializeField] private TextMeshProUGUI additionalInfoText;

        [Header("Icon Components")]
        [SerializeField] private Image statusIcon;
        [SerializeField] private Image ownerIcon;
        [SerializeField] private Image lastOwnerIcon;
        [SerializeField] private Image toxinIcon;

        [Header("Layout Groups (optional - for showing/hiding sections)")]
        [SerializeField] private GameObject deathReasonGroup;
        [SerializeField] private GameObject lastOwnerGroup;
        [SerializeField] private GameObject expirationGroup;

        /// <summary>
        /// Updates the tooltip with cell information and appropriate icons.
        /// </summary>
        public void UpdateTooltip(FungalCell cell, GameBoard board, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            UpdateStatusInfo(cell);
            UpdateDeathReason(cell);
            UpdateOwnershipInfo(cell);
            UpdateAgeAndExpiration(cell, board);
            UpdateIcons(cell, gridVisualizer);
            UpdateAdditionalInfo(cell);
        }

        private void UpdateStatusInfo(FungalCell cell)
        {
            if (statusText != null)
            {
                if (cell.IsAlive)
                {
                    statusText.text = "<color=#00FF00><b>Status: Alive</b></color>";
                }
                else if (cell.IsDead)
                {
                    statusText.text = "<color=#808080><b>Status: Dead</b></color>";
                }
                else if (cell.IsToxin)
                {
                    statusText.text = "<color=#FF00FF><b>Status: Toxin</b></color>";
                }
            }
        }

        private void UpdateDeathReason(FungalCell cell)
        {
            bool showDeathReason = cell.IsDead && cell.CauseOfDeath.HasValue;
            
            // Show/hide death reason group
            if (deathReasonGroup != null)
                deathReasonGroup.SetActive(showDeathReason);
            
            if (deathReasonText != null)
            {
                if (showDeathReason)
                {
                    deathReasonText.text = $"Death Reason: {GetDeathReasonDisplayName(cell.CauseOfDeath.Value)}";
                }
                else
                {
                    deathReasonText.text = "";
                }
            }
        }

        private void UpdateOwnershipInfo(FungalCell cell)
        {
            // Current Owner
            if (ownerText != null)
            {
                if (cell.OwnerPlayerId.HasValue)
                {
                    ownerText.text = $"Owner: Player {cell.OwnerPlayerId.Value + 1}";
                }
                else
                {
                    ownerText.text = "";
                }
            }

            // Last Owner
            bool showLastOwner = cell.LastOwnerPlayerId.HasValue;
            
            if (lastOwnerGroup != null)
                lastOwnerGroup.SetActive(showLastOwner);
            
            if (lastOwnerText != null)
            {
                if (showLastOwner)
                {
                    lastOwnerText.text = $"Last Owner: Player {cell.LastOwnerPlayerId.Value + 1}";
                }
                else
                {
                    lastOwnerText.text = "";
                }
            }
        }

        private void UpdateAgeAndExpiration(FungalCell cell, GameBoard board)
        {
            // Growth Cycle Age
            if (growthAgeText != null)
            {
                growthAgeText.text = $"Growth Cycle Age: {cell.GrowthCycleAge}";
            }

            // Toxin Expiration
            bool showExpiration = cell.IsToxin;
            
            if (expirationGroup != null)
                expirationGroup.SetActive(showExpiration);
            
            if (expirationText != null)
            {
                if (showExpiration)
                {
                    int cyclesRemaining = cell.ToxinExpirationCycle - board.CurrentGrowthCycle;
                    if (cyclesRemaining > 0)
                        expirationText.text = $"Cycles Until Expiration: {cyclesRemaining}";
                    else
                        expirationText.text = "<color=#FF0000>Expires this cycle</color>";
                }
                else
                {
                    expirationText.text = "";
                }
            }
        }

        private void UpdateIcons(FungalCell cell, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            if (gridVisualizer == null) return;

            // Status Icon (Dead/Toxin)
            if (statusIcon != null)
            {
                if (cell.IsDead && gridVisualizer.deadTile != null)
                {
                    statusIcon.sprite = gridVisualizer.deadTile.sprite;
                    statusIcon.gameObject.SetActive(true);
                }
                else if (cell.IsToxin && gridVisualizer.toxinOverlayTile != null)
                {
                    statusIcon.sprite = gridVisualizer.toxinOverlayTile.sprite;
                    statusIcon.gameObject.SetActive(true);
                }
                else
                {
                    statusIcon.gameObject.SetActive(false);
                }
            }

            // Owner Icon
            if (ownerIcon != null)
            {
                if (cell.OwnerPlayerId.HasValue)
                {
                    var tile = gridVisualizer.GetTileForPlayer(cell.OwnerPlayerId.Value);
                    if (tile != null && tile.sprite != null)
                    {
                        ownerIcon.sprite = tile.sprite;
                        ownerIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        ownerIcon.gameObject.SetActive(false);
                    }
                }
                else
                {
                    ownerIcon.gameObject.SetActive(false);
                }
            }

            // Last Owner Icon
            if (lastOwnerIcon != null)
            {
                if (cell.LastOwnerPlayerId.HasValue)
                {
                    var tile = gridVisualizer.GetTileForPlayer(cell.LastOwnerPlayerId.Value);
                    if (tile != null && tile.sprite != null)
                    {
                        lastOwnerIcon.sprite = tile.sprite;
                        lastOwnerIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        lastOwnerIcon.gameObject.SetActive(false);
                    }
                }
                else
                {
                    lastOwnerIcon.gameObject.SetActive(false);
                }
            }

            // Toxin Icon (separate from status icon for layout purposes)
            if (toxinIcon != null)
            {
                if (cell.IsToxin && gridVisualizer.toxinOverlayTile != null)
                {
                    toxinIcon.sprite = gridVisualizer.toxinOverlayTile.sprite;
                    toxinIcon.gameObject.SetActive(true);
                }
                else
                {
                    toxinIcon.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateAdditionalInfo(FungalCell cell)
        {
            if (additionalInfoText != null)
            {
                var additionalInfo = new System.Text.StringBuilder();

                // Special status indicators
                if (cell.IsResistant)
                    additionalInfo.AppendLine("<color=#FFD700><b>Resistant</b></color>");

                if (cell.ReclaimCount > 0)
                    additionalInfo.AppendLine($"Reclaimed: {cell.ReclaimCount}x");

                // Animation states (for visual feedback)
                if (cell.IsNewlyGrown)
                    additionalInfo.AppendLine("<color=#FFFF00>? Newly Grown</color>");
                if (cell.IsDying)
                    additionalInfo.AppendLine("<color=#FF0000>?? Dying</color>");
                if (cell.IsReceivingToxinDrop)
                    additionalInfo.AppendLine("<color=#FF00FF>?? Receiving Toxin</color>");

                additionalInfoText.text = additionalInfo.ToString().Trim();
            }
        }

        private string GetDeathReasonDisplayName(DeathReason reason)
        {
            return reason switch
            {
                DeathReason.Age => "Old Age",
                DeathReason.Randomness => "Random Death",
                DeathReason.PutrefactiveMycotoxin => "Putrefactive Mycotoxin",
                DeathReason.SporocidalBloom => "Sporicidal Bloom",
                DeathReason.MycotoxinPotentiation => "Mycotoxin Potentiation",
                DeathReason.HyphalVectoring => "Hyphal Vectoring",
                DeathReason.JettingMycelium => "Jetting Mycelium",
                DeathReason.Infested => "Infested",
                DeathReason.Poisoned => "Poisoned",
                DeathReason.Unknown => "Unknown",
                _ => reason.ToString()
            };
        }
    }
}