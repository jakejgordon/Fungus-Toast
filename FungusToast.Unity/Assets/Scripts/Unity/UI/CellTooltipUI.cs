using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Component for the Cell Tooltip prefab that handles displaying cell information
    /// with proper layout and icons.
    /// 
    /// Expected Prefab Structure (in this exact order):
    /// CellTooltip (this component)
    /// ? StatusGroup (GameObject) - Contains status text and icon
    /// ? GrowthSourceGroup (GameObject) - Contains growth source text
    /// ? DeathReasonGroup (GameObject) - Contains death reason text
    /// ? OwnerGroup (GameObject) - Contains owner text and icon
    /// ? AgeGroup (GameObject) - Contains growth age text
    /// ? ExpirationGroup (GameObject) - Contains expiration text
    /// ? ResistantGroup (GameObject) - Contains resistant status with shield icon and text
    /// ? LastOwnerGroup (GameObject) - Contains last owner text and icon
    /// ? AdditionalInfoGroup (GameObject) - Contains additional status info (reclaim count, animation states)
    /// 
    /// All layout groups must be assigned in the Inspector - no fallback discovery.
    /// Icons (StatusIcon, OwnerIcon, LastOwnerIcon, ToxinIcon, ResistantIcon) must also be assigned.
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
        [SerializeField] private TextMeshProUGUI resistantText;
        [SerializeField] private TextMeshProUGUI growthSourceText;
        [SerializeField] private TextMeshProUGUI additionalInfoText;

        [Header("Icon Components")]
        [SerializeField] private Image statusIcon;
        [SerializeField] private Image ownerIcon;
        [SerializeField] private Image lastOwnerIcon;
        [SerializeField] private Image toxinIcon;
        [SerializeField] private Image resistantIcon;

        [Header("Layout Groups (optional - for showing/hiding sections)")]
        [SerializeField] private GameObject statusGroup;
        [SerializeField] private GameObject ownerGroup;
        [SerializeField] private GameObject deathReasonGroup;
        [SerializeField] private GameObject lastOwnerGroup;
        [SerializeField] private GameObject ageGroup;
        [SerializeField] private GameObject expirationGroup;
        [SerializeField] private GameObject resistantGroup;
        [SerializeField] private GameObject growthSourceGroup;
        [SerializeField] private GameObject additionalInfoGroup;

        [Header("Style (optional)")]
        [SerializeField] private Image tooltipBackgroundImage;
        [SerializeField, Range(0.5f, 1f)] private float tooltipBackgroundAlpha = 0.96f;
        [SerializeField, Range(0f, 1f)] private float rowBackgroundAlpha = 0.35f;

        // Runtime dependency - injected via SetPlayerBinder()
        private UI_PlayerBinder playerBinder;

        /// <summary>
        /// Sets the UI_PlayerBinder dependency. Call this when creating tooltip instances dynamically.
        /// </summary>
        public void SetPlayerBinder(UI_PlayerBinder binder)
        {
            playerBinder = binder;
        }

        /// <summary>
        /// Updates the tooltip with cell information and appropriate icons.
        /// </summary>
        public void UpdateTooltip(FungalCell cell, GameBoard board, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            ApplyTooltipStyle();
            UpdateStatusInfo(cell);
            UpdateDeathReason(cell);
            UpdateOwnershipInfo(cell);
            UpdateAgeAndExpiration(cell, board);
            UpdateGrowthSource(cell);
            UpdateIcons(cell, gridVisualizer);
            UpdateResistantStatus(cell, gridVisualizer);
            UpdateAdditionalInfo(cell);
            
            // Force correct sibling order in case Unity's layout system is misbehaving
            ForceCorrectOrder();
            
            // Since manual positioning has been fixed in Unity, we don't need to reset positions
            // ResetAllGroupPositions(); // DISABLED - Unity prefab positioning is now correct
            
            // AGGRESSIVE FIX: Force Unity to properly recalculate layout multiple times
            StartCoroutine(ForceLayoutRebuildCoroutine());
        }

        /// <summary>
        /// Aggressively forces Unity's layout system to properly recalculate by doing multiple rebuilds.
        /// This works around Unity's layout timing issues with LayoutElement.ignoreLayout.
        /// </summary>
        private System.Collections.IEnumerator ForceLayoutRebuildCoroutine()
        {
            var rectTransform = GetComponent<RectTransform>();
            
            // Force immediate rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            
            // Force canvas update
            Canvas.ForceUpdateCanvases();
            
            // Wait one frame for Unity to process
            yield return null;
            
            // Force another rebuild after Unity has processed
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            
            // Force canvas update again
            Canvas.ForceUpdateCanvases();
            
            // Wait another frame
            yield return null;
            
            // Final rebuild to ensure everything is properly laid out
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        /// <summary>
        /// Forces the tooltip sections to appear in the correct order by manually setting sibling indices.
        /// Only call this if Unity's VerticalLayoutGroup is not respecting the prefab hierarchy order.
        /// </summary>
        private void ForceCorrectOrder()
        {
            // Set explicit sibling indices to force correct order
            if (statusGroup != null) statusGroup.transform.SetSiblingIndex(0);
            if (growthSourceGroup != null) growthSourceGroup.transform.SetSiblingIndex(1);
            if (deathReasonGroup != null) deathReasonGroup.transform.SetSiblingIndex(2);
            if (ownerGroup != null) ownerGroup.transform.SetSiblingIndex(3);
            if (ageGroup != null) ageGroup.transform.SetSiblingIndex(4);
            if (expirationGroup != null) expirationGroup.transform.SetSiblingIndex(5);
            if (resistantGroup != null) resistantGroup.transform.SetSiblingIndex(6);
            if (lastOwnerGroup != null) lastOwnerGroup.transform.SetSiblingIndex(7); // 2nd to last
            if (additionalInfoGroup != null) additionalInfoGroup.transform.SetSiblingIndex(8); // Last
        }

        /// <summary>
        /// Resets all group positions to let VerticalLayoutGroup handle positioning.
        /// This fixes conflicts between manual positioning and automatic layout.
        /// </summary>
        private void ResetAllGroupPositions()
        {
            ResetGroupPosition(statusGroup);
            ResetGroupPosition(deathReasonGroup);
            ResetGroupPosition(ownerGroup);
            ResetGroupPosition(lastOwnerGroup);
            ResetGroupPosition(ageGroup);
            ResetGroupPosition(expirationGroup);
            ResetGroupPosition(resistantGroup);
            ResetGroupPosition(growthSourceGroup);
            ResetGroupPosition(additionalInfoGroup);
        }

        /// <summary>
        /// Resets a single group's position to work properly with VerticalLayoutGroup.
        /// </summary>
        private void ResetGroupPosition(GameObject group)
        {
            if (group == null) return;

            RectTransform rectTransform = group.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Store original for debugging
            Vector2 originalPos = rectTransform.anchoredPosition;

            // For VerticalLayoutGroup children, we want to let the layout system control everything
            // DON'T set anchors - let them stay as they are in the prefab
            // DON'T force position to zero - let VerticalLayoutGroup position them
            
            // Only reset if there are problematic manual offsets
            if (rectTransform.offsetMin != Vector2.zero || rectTransform.offsetMax != Vector2.zero)
            {
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
            
            // Only reset anchored position if it's obviously wrong (large values that would conflict)
            if (Mathf.Abs(rectTransform.anchoredPosition.x) > 50f || Mathf.Abs(rectTransform.anchoredPosition.y) > 50f)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// Updates the status information display
        /// </summary>
        private void UpdateStatusInfo(FungalCell cell)
        {
            bool hasStatus = true; // Status is always shown for any cell
            
            SetGroupVisibility(statusGroup, hasStatus);
            
            if (statusText != null)
            {
                if (cell.IsAlive)
                {
                    statusText.text = FormatHeaderStatus("Alive", UIStyleTokens.State.Success);
                }
                else if (cell.IsDead)
                {
                    statusText.text = FormatHeaderStatus("Dead", UIStyleTokens.Text.Muted);
                }
                else if (cell.IsToxin)
                {
                    statusText.text = FormatHeaderStatus("Toxin", UIStyleTokens.Category.Fungicide);
                }
            }
        }

        /// <summary>
        /// Updates the death reason information display
        /// </summary>
        private void UpdateDeathReason(FungalCell cell)
        {
            bool showDeathReason = cell.IsDead && cell.CauseOfDeath.HasValue;
            
            SetGroupVisibility(deathReasonGroup, showDeathReason);
            
            if (deathReasonText != null)
            {
                if (showDeathReason)
                {
                    deathReasonText.text = FormatLabelValue("Death", GetDeathReasonDisplayName(cell.CauseOfDeath.Value), UIStyleTokens.Text.Secondary, UIStyleTokens.State.Danger);
                }
                else
                {
                    deathReasonText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the ownership information display (current and last owner)
        /// </summary>
        private void UpdateOwnershipInfo(FungalCell cell)
        {
            // Current Owner
            bool hasOwner = cell.OwnerPlayerId.HasValue;
            
            SetGroupVisibility(ownerGroup, hasOwner);
            
            if (ownerText != null)
            {
                if (hasOwner)
                {
                    ownerText.text = FormatLabelValue("Owner", $"Player {cell.OwnerPlayerId.Value + 1}", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary);
                }
                else
                {
                    ownerText.text = "";
                }
            }

            // Last Owner
            bool showLastOwner = cell.LastOwnerPlayerId.HasValue;
            
            SetGroupVisibility(lastOwnerGroup, showLastOwner);
            
            if (lastOwnerText != null)
            {
                if (showLastOwner)
                {
                    lastOwnerText.text = FormatLabelValue("Last Owner", $"Player {cell.LastOwnerPlayerId.Value + 1}", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Secondary);
                }
                else
                {
                    lastOwnerText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the age and expiration information display
        /// </summary>
        private void UpdateAgeAndExpiration(FungalCell cell, GameBoard board)
        {
            // Growth Cycle Age - always show for any cell
            bool hasAge = true;
            
            SetGroupVisibility(ageGroup, hasAge);
            
            if (growthAgeText != null)
            {
                // Make growth age green for young, living cells to draw attention regardless of phase
                bool highlightYoung = cell.IsAlive && cell.GrowthCycleAge < UIEffectConstants.GrowthCycleAgeHighlightTextThreshold;

                if (highlightYoung)
                {
                    growthAgeText.text = FormatLabelValue("Growth Cycle Age", cell.GrowthCycleAge.ToString(), UIStyleTokens.Text.Secondary, UIStyleTokens.State.Success);
                }
                else
                {
                    growthAgeText.text = FormatLabelValue("Growth Cycle Age", cell.GrowthCycleAge.ToString(), UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary);
                }
            }

            // Toxin Expiration - only show for toxins
            bool showExpiration = cell.IsToxin;
            
            SetGroupVisibility(expirationGroup, showExpiration);
            
            if (expirationText != null)
            {
                if (showExpiration)
                {
                    // Use the new age-based expiration system instead of the old cycle-based system
                    int cyclesRemaining = cell.ToxinExpirationAge - cell.GrowthCycleAge;
                    if (cyclesRemaining > 0)
                        expirationText.text = FormatLabelValue("Cycles Until Expiration", cyclesRemaining.ToString(), UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary);
                    else
                        expirationText.text = FormatLabelValue("Expiration", "Expires this cycle", UIStyleTokens.Text.Secondary, UIStyleTokens.State.Danger);
                }
                else
                {
                    expirationText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the icon displays for the cell (status, owner, toxin)
        /// </summary>
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

            // Owner Icon - Use PlayerBinder for player mold icons
            if (ownerIcon != null)
            {
                if (cell.OwnerPlayerId.HasValue)
                {
                    Sprite playerSprite = playerBinder?.GetPlayerIcon(cell.OwnerPlayerId.Value);
                    
                    if (playerSprite != null)
                    {
                        ownerIcon.sprite = playerSprite;
                        ownerIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        // Fallback to GridVisualizer tile if PlayerBinder doesn't have the icon
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
                }
                else
                {
                    ownerIcon.gameObject.SetActive(false);
                }
            }

            // Last Owner Icon - Use PlayerBinder for player mold icons
            if (lastOwnerIcon != null)
            {
                if (cell.LastOwnerPlayerId.HasValue)
                {
                    Sprite playerSprite = playerBinder?.GetPlayerIcon(cell.LastOwnerPlayerId.Value);
                    if (playerSprite != null)
                    {
                        lastOwnerIcon.sprite = playerSprite;
                        lastOwnerIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        // Fallback to GridVisualizer tile if PlayerBinder doesn't have the icon
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

        /// <summary>
        /// Updates the growth source information display
        /// </summary>
        private void UpdateGrowthSource(FungalCell cell)
        {
            bool showGrowthSource = cell.SourceOfGrowth.HasValue;
            
            SetGroupVisibility(growthSourceGroup, showGrowthSource);
            
            if (growthSourceText != null)
            {
                if (showGrowthSource)
                {
                    growthSourceText.text = FormatLabelValue("Source", GetGrowthSourceDisplayName(cell.SourceOfGrowth.Value), UIStyleTokens.Text.Secondary, UIStyleTokens.State.Info);
                }
                else
                {
                    growthSourceText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the resistant status display with shield icon
        /// </summary>
        private void UpdateResistantStatus(FungalCell cell, FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            bool isResistant = cell.IsResistant;
            
            SetGroupVisibility(resistantGroup, isResistant);
            
            if (resistantText != null)
            {
                if (isResistant)
                {
                    resistantText.text = FormatLabelValue("Resistance", "Active", UIStyleTokens.Text.Secondary, UIStyleTokens.Accent.Spore);
                }
                else
                {
                    resistantText.text = "";
                }
            }

            // Set resistant shield icon
            if (resistantIcon != null)
            {
                if (isResistant && gridVisualizer?.goldShieldOverlayTile != null)
                {
                    resistantIcon.sprite = gridVisualizer.goldShieldOverlayTile.sprite;
                    resistantIcon.gameObject.SetActive(true);
                }
                else
                {
                    resistantIcon.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateAdditionalInfo(FungalCell cell)
        {
            if (additionalInfoText != null)
            {
                var additionalInfo = new System.Text.StringBuilder();

                // Reclaim count
                if (cell.ReclaimCount > 0)
                    additionalInfo.AppendLine(FormatLabelValue("Reclaimed", $"{cell.ReclaimCount}x", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));

                // Animation states (for visual feedback)
                if (cell.IsNewlyGrown)
                    additionalInfo.AppendLine($"<color=#{ToHex(EnsureTooltipContrast(UIStyleTokens.State.Warning))}>• Newly Grown</color>");
                if (cell.IsDying)
                    additionalInfo.AppendLine($"<color=#{ToHex(EnsureTooltipContrast(UIStyleTokens.State.Danger))}>• Dying</color>");
                if (cell.IsReceivingToxinDrop)
                    additionalInfo.AppendLine($"<color=#{ToHex(EnsureTooltipContrast(UIStyleTokens.Category.Fungicide))}>• Receiving Toxin</color>");

                string infoText = additionalInfo.ToString().Trim();
                bool hasAdditionalInfo = !string.IsNullOrEmpty(infoText);
                
                SetGroupVisibility(additionalInfoGroup, hasAdditionalInfo);
                
                additionalInfoText.text = infoText;
            }
            else
            {
                SetGroupVisibility(additionalInfoGroup, false);
            }
        }

        private void ApplyTooltipStyle()
        {
            if (tooltipBackgroundImage == null)
            {
                tooltipBackgroundImage = GetComponent<Image>();
            }

            if (tooltipBackgroundImage != null)
            {
                var panelColor = UIStyleTokens.Surface.PanelSecondary;
                panelColor.a = tooltipBackgroundAlpha;
                tooltipBackgroundImage.color = panelColor;
            }

            ApplyTextDefaults(statusText, UIStyleTokens.Text.Primary, true);
            ApplyTextDefaults(deathReasonText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(ownerText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(lastOwnerText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(growthAgeText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(expirationText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(resistantText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(growthSourceText, UIStyleTokens.Text.Secondary, false);
            ApplyTextDefaults(additionalInfoText, UIStyleTokens.Text.Secondary, false);

            ApplyGroupSurface(statusGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(growthSourceGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(deathReasonGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(ownerGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(ageGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(expirationGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(resistantGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(lastOwnerGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
            ApplyGroupSurface(additionalInfoGroup, UIStyleTokens.Surface.PanelElevated, rowBackgroundAlpha);
        }

        private static void ApplyTextDefaults(TextMeshProUGUI textComponent, Color color, bool isHeader)
        {
            if (textComponent == null)
            {
                return;
            }

            textComponent.color = color;
            textComponent.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.richText = true;
        }

        private static void ApplyGroupSurface(GameObject group, Color baseColor, float alpha)
        {
            if (group == null)
            {
                return;
            }

            var image = group.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var c = baseColor;
            c.a = alpha;
            image.color = c;
        }

        private static string FormatHeaderStatus(string status, Color statusColor)
        {
            Color readableStatusColor = EnsureTooltipContrast(statusColor);
            return $"<color=#{ToHex(UIStyleTokens.Text.Primary)}><b>Status</b></color>: <color=#{ToHex(readableStatusColor)}><b>{status}</b></color>";
        }

        private static string FormatLabelValue(string label, string value, Color labelColor, Color valueColor)
        {
            Color readableValueColor = EnsureTooltipContrast(valueColor);
            return $"<color=#{ToHex(labelColor)}>{label}:</color> <color=#{ToHex(readableValueColor)}>{value}</color>";
        }

        private static Color EnsureTooltipContrast(Color color)
        {
            return Color.Lerp(color, UIStyleTokens.Text.Primary, 0.5f);
        }

        /// <summary>
        /// Properly hides/shows a group by using SetActive instead of LayoutElement.ignoreLayout.
        /// This is the only way to prevent Unity's VerticalLayoutGroup from adding spacing between elements.
        /// </summary>
        private void SetGroupVisibility(GameObject group, bool visible)
        {
            if (group == null) return;

            if (visible)
            {
                // Show the group
                group.SetActive(true);
                
                // Get or add LayoutElement component
                LayoutElement layoutElement = group.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = group.AddComponent<LayoutElement>();
                }
                
                // Participate in layout
                layoutElement.ignoreLayout = false;
                layoutElement.preferredHeight = -1f;
                layoutElement.minHeight = -1f;
                layoutElement.flexibleHeight = 1f;
                
                // Make visible
                CanvasGroup canvasGroup = group.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = group.AddComponent<CanvasGroup>();
                }
                
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            else
            {
                // Hide the group completely - SetActive(false) is the only way to prevent VerticalLayoutGroup spacing
                group.SetActive(false);
            }
        }

        private string GetDeathReasonDisplayName(DeathReason reason)
        {
            return reason switch
            {
                DeathReason.Age => "Old Age",
                DeathReason.Randomness => "Random Death",
                DeathReason.PutrefactiveMycotoxin => "Putrefactive Mycotoxin",
                DeathReason.SporicidalBloom => "Sporicidal Bloom",
                DeathReason.MycotoxinPotentiation => "Mycotoxin Potentiation",
                DeathReason.HyphalVectoring => "Hyphal Vectoring",
                DeathReason.JettingMycelium => "Jetting Mycelium",
                DeathReason.Infested => "Infested",
                DeathReason.Poisoned => "Poisoned",
                DeathReason.PutrefactiveCascade => "Putrefactive Cascade",
                DeathReason.PutrefactiveCascadePoison => "Putrefactive Cascade Poison",
                DeathReason.Unknown => "Unknown",
                _ => reason.ToString()
            };
        }

        private string GetGrowthSourceDisplayName(GrowthSource source)
        {
            return source switch
            {
                GrowthSource.InitialSpore => "Initial Spore",
                GrowthSource.HyphalOutgrowth => "Hyphal Outgrowth",
                GrowthSource.TendrilOutgrowth => "Tendril Outgrowth",
                GrowthSource.RegenerativeHyphae => "Regenerative Hyphae",
                GrowthSource.NecrotoxicConversion => "Necrotoxic Conversion",
                GrowthSource.HyphalSurge => "Hyphal Surge",
                GrowthSource.JettingMycelium => "Jetting Mycelium",
                GrowthSource.HyphalVectoring => "Hyphal Vectoring",
                GrowthSource.SurgicalInoculation => "Surgical Inoculation",
                GrowthSource.Necrosporulation => "Necrosporulation",
                GrowthSource.NecrophyticBloom => "Necrophytic Bloom",
                GrowthSource.NecrohyphalInfiltration => "Necrohyphal Infiltration",
                GrowthSource.CreepingMold => "Creeping Mold",
                GrowthSource.CatabolicRebirth => "Catabolic Rebirth",
                GrowthSource.Ballistospore => "Ballistospore",
                GrowthSource.MycotoxinTracer => "Mycotoxin Tracers",
                GrowthSource.SporicidalBloom => "Sporicidal Bloom",
                GrowthSource.MimeticResilience => "Mimetic Resilience",
                GrowthSource.CornerConduit => "Corner Conduit",
                GrowthSource.AggressotropicConduit => "Aggressotropic Conduit",
                GrowthSource.Manual => "Manual",
                GrowthSource.Unknown => "Unknown",
                _ => source.ToString()
            };
        }

        /// <summary>
        /// Logs the actual prefab structure to help diagnose layout issues.
        /// </summary>
        private void LogPrefabStructure()
        {
            UnityEngine.Debug.Log("=== TOOLTIP PREFAB STRUCTURE DIAGNOSTIC ===");
            
            // Log parent layout component
            var parentLayoutGroup = GetComponent<VerticalLayoutGroup>();
            if (parentLayoutGroup != null)
            {
                UnityEngine.Debug.Log($"Parent VerticalLayoutGroup: " +
                    $"spacing={parentLayoutGroup.spacing}, " +
                    $"padding=({parentLayoutGroup.padding.left},{parentLayoutGroup.padding.top},{parentLayoutGroup.padding.right},{parentLayoutGroup.padding.bottom}), " +
                    $"childControlWidth={parentLayoutGroup.childControlWidth}, " +
                    $"childControlHeight={parentLayoutGroup.childControlHeight}");
            }
            else
            {
                UnityEngine.Debug.Log("No VerticalLayoutGroup found on tooltip root!");
            }
            
            // Log all child GameObjects and their components (with null checks)
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var layoutElement = child.GetComponent<LayoutElement>();
                var canvasGroup = child.GetComponent<CanvasGroup>();
                var horizontalLayoutGroup = child.GetComponent<HorizontalLayoutGroup>();
                
                // Safe alpha access with null check
                float alpha = canvasGroup != null ? canvasGroup.alpha : -1f;
                
                UnityEngine.Debug.Log($"Child[{i}]: {child.name} " +
                    $"active={child.gameObject.activeSelf}, " +
                    $"layoutIgnored={layoutElement?.ignoreLayout ?? false}, " +
                    $"preferredHeight={layoutElement?.preferredHeight ?? -999}, " +
                    $"alpha={alpha}, " +
                    $"hasHorizontalLayout={horizontalLayoutGroup != null}");
            }
            
            // Count visible vs hidden groups
            int visibleCount = 0;
            int hiddenCount = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                    visibleCount++;
                else
                    hiddenCount++;
            }
            
            UnityEngine.Debug.Log($"SUMMARY: {visibleCount} visible groups, {hiddenCount} hidden groups. Expected spacing gaps: {Mathf.Max(0, visibleCount - 1)}");
            UnityEngine.Debug.Log("=== END PREFAB STRUCTURE DIAGNOSTIC ===");
        }

        private static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
