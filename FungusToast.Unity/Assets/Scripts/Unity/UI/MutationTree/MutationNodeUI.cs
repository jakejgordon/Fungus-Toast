using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;
using System.Linq;
using FungusToast.Unity;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITooltipContentProvider
    {
        private const float MutationNameMinimumFontSize = 10f;
        private const float MutationNameHorizontalPadding = 8f;
        private static readonly Vector2 DefaultHighlightEffectDistance = new(1.2f, -1.2f);
        private static readonly Color HighlightedTextColor = new Color32(0x09, 0x0B, 0x07, 0xFF);
        private static readonly Color HighlightedSecondaryTextColor = new Color32(0x1A, 0x1E, 0x14, 0xFF);
        private static readonly Color TooltipSectionLabelColor = UIStyleTokens.Accent.Spore;
        private static readonly Color TooltipBonusLabelColor = UIStyleTokens.State.Warning;
        private const float DarkTextBackgroundLuminanceThreshold = 0.52f;

        // Upgrade-cost badge layout constants (must match prefab values)
        private const float UpgradeCostIconWidth = 24f;
        private const float UpgradeCostPaddingH = 4f;   // 2 left + 2 right in HorizontalLayoutGroup
        private const float UpgradeCostSpacing = 2f;
        private const float UpgradeCostMinTextWidth = 15f;

        [Header("UI References")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI mutationNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private GameObject upgradeCostGroup;
        [SerializeField] private TextMeshProUGUI upgradeCostText;

        [Header("Surge UI")]
        [SerializeField] private GameObject surgeActiveOverlay;    // Should be top left, with icon+text child
        [SerializeField] private Image surgeActiveIcon;            // The hourglass icon
        [SerializeField] private TextMeshProUGUI surgeActiveText;  // The countdown number

        [Header("Highlight")]
        [SerializeField] private Outline highlightOutline;
        [SerializeField] private GameObject prerequisiteHighlightOverlay; // New field for prerequisite highlighting
        [SerializeField] private GameObject dependentHighlightOverlay;

        [Header("Unlock UI")]
        [SerializeField] private GameObject pendingUnlockOverlay; // Hourglass overlay for pending unlock
        [SerializeField] private TextMeshProUGUI pendingUnlockText;

        // ── New UX enhancement fields (created at runtime if not wired in prefab) ──
        [Header("Enhanced UX — Level Progress Fill")]
        private Image levelProgressFill;                  // Faint category-colored fill behind level text

        [Header("Enhanced UX — Tier Stripe (disabled)")]
        [SerializeField] private Image tierStripe;        // Kept for prefab reference; hidden at runtime

        [Header("Enhanced UX — Node Background")]
        [SerializeField] private Image nodeBackground;    // The main background Image of the node

        [Header("Enhanced UX — MAX Badge")]
        [SerializeField] private GameObject maxBadge;     // Small "MAX" label, top-right

        private Mutation mutation;
        private UI_MutationManager uiManager;
        private Player player;
        private bool isPointerHovering;

        // Animation state
        private Coroutine upgradeEffectCoroutine;
        private float targetProgressFill;
        private float currentProgressFill;
        private static readonly float ProgressLerpSpeed = 6f;
        private const float ProgressFillVisibilityBoost = 3.8f;

        public int MutationId => mutation.Id;

        /// <summary>Exposes the underlying Mutation for external queries (e.g. investment summaries).</summary>
        public Mutation GetMutation() => mutation;

        public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
        {
            this.mutation = mutation;
            this.player = player;
            this.uiManager = uiManager;

            ConfigureMutationNameFit();
            mutationNameText.text = mutation.Name;

            // ── Tier stripe — disabled; visual hierarchy handled by progress fill ──
            if (tierStripe != null)
                tierStripe.gameObject.SetActive(false);

            // ── Runtime-create level-text progress BG if not wired ──
            EnsureLevelProgressBG();

            // ── Runtime-create MAX badge if not wired in prefab ──
            EnsureMaxBadge();

            // ── Subtle border outline for visual node separation ──
            EnsureNodeBorder();

            // Initialise progress fill to current level immediately (no lerp on first draw)
            int currentLevel = player.GetMutationLevel(mutation.Id);
            targetProgressFill = mutation.MaxLevel > 0 ? currentLevel / (float)mutation.MaxLevel : 0f;
            currentProgressFill = targetProgressFill;
            if (levelProgressFill != null)
                levelProgressFill.rectTransform.anchorMax = new Vector2(currentProgressFill, 0f);

            UpdateDisplay();

            // Ensure highlights are off by default
            if (highlightOutline != null)
                highlightOutline.enabled = false;
            if (prerequisiteHighlightOverlay != null)
                prerequisiteHighlightOverlay.SetActive(false);
            EnsureDependentHighlightOverlay();
            if (dependentHighlightOverlay != null)
                dependentHighlightOverlay.SetActive(false);

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

            // Wire up the new unified tooltip system via TooltipTrigger.
            // TooltipTrigger auto-resolves ITooltipContentProvider from this component.
            var trigger = GetComponent<TooltipTrigger>();
            if (trigger == null)
                trigger = gameObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(this);
            trigger.SetAutoPlacementOffsetX(60f);
        }

        /// <summary>
        /// ITooltipContentProvider implementation — supplies rich-text tooltip
        /// content to the unified TooltipManager system.
        /// </summary>
        public string GetTooltipText() => BuildTooltip();
  
        private void OnUpgradeClicked()
        {
            int currentRound = GameManager.Instance.Board.CurrentRound;
            if (!player.CanUpgrade(mutation, currentRound))
                return;

            upgradeButton.interactable = false;

            uiManager.TryUpgradeMutation(mutation, success =>
            {
                if (success)
                {
                    UpdateDisplay();
                    PlayUpgradeEffect();
                }
                else
                {
                    upgradeButton.interactable = true;
                }
            });
        }

        public void UpdateDisplay()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            bool isMaxed = currentLevel >= mutation.MaxLevel;

            // Level text — clean display (MAX badge handles maxed state separately)
            levelText.text = $"Level {currentLevel}/{mutation.MaxLevel}";

            // SURGE LOGIC
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int surgeTurns = isSurgeActive ? player.GetSurgeTurnsRemaining(mutation.Id) : 0;

            // PREREQS
            bool isLocked = false;
            foreach (var prereq in mutation.Prerequisites)
            {
                if (player.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel)
                {
                    isLocked = true;
                    break;
                }
            }

            // COST CALC
            int upgradeCost = player.GetMutationPointCost(mutation);

            bool canAfford = player.MutationPoints >= upgradeCost;

            // LOCK/SURGE/PENDING UI
            bool showPendingUnlock = mutation.Prerequisites.Count > 0
                && player.PlayerMutations.TryGetValue(mutation.Id, out var pm)
                && pm.PrereqMetRound.HasValue
                && pm.PrereqMetRound.Value == GameManager.Instance.Board.CurrentRound;
            lockOverlay.SetActive(isLocked && !isSurgeActive && !showPendingUnlock);
            if (pendingUnlockOverlay != null)
                pendingUnlockOverlay.SetActive(showPendingUnlock);
            if (pendingUnlockText != null)
                pendingUnlockText.text = "1";

            if (canvasGroup != null)
                canvasGroup.alpha = (isLocked || isSurgeActive || showPendingUnlock) ? 0.5f : 1f;

            // Surge overlay (shows when surge is active)
            if (surgeActiveOverlay != null)
            {
                surgeActiveOverlay.SetActive(isSurgeActive);
                if (isSurgeActive)
                {
                    if (surgeActiveIcon != null)
                        surgeActiveIcon.enabled = true;
                    if (surgeActiveText != null)
                        surgeActiveText.text = surgeTurns.ToString();
                }
            }

            // Show cost (top right) — hide when maxed
            if (upgradeCostGroup != null && upgradeCostText != null)
            {
                if (isMaxed)
                {
                    upgradeCostGroup.SetActive(false);
                }
                else if (upgradeCost > 1)
                {
                    upgradeCostGroup.SetActive(true);
                    upgradeCostText.text = $"x{upgradeCost}";
                    ConfigureUpgradeCostBadge();
                }
                else
                {
                    upgradeCostGroup.SetActive(false);
                }
            }

            // ── MAX badge ──
            if (maxBadge != null)
                maxBadge.SetActive(isMaxed);

            // ── Level progress BG: hide until player has invested at least 1 level ──
            if (levelProgressFill != null)
                levelProgressFill.gameObject.SetActive(currentLevel > 0);

            // ── Progress fill target (lerped in Update) ──
            targetProgressFill = mutation.MaxLevel > 0 ? currentLevel / (float)mutation.MaxLevel : 0f;

            // ── Progress fill color ──
            if (levelProgressFill != null)
            {
                Color progressColor = MutationTreeColors.GetProgressBarColor(mutation.Category);
                progressColor.a = Mathf.Clamp01(progressColor.a * ProgressFillVisibilityBoost);
                levelProgressFill.color = progressColor;
            }

            // ── Affordability background tinting ──
            ApplyNodeBackgroundTint(isLocked, isMaxed, canAfford, isSurgeActive, showPendingUnlock);
            ApplyTextContrast(useDarkText: ShouldUseDarkTextForCurrentBackground());

            UpdateInteractable();

            if (isPointerHovering)
            {
                ApplyInteractableHoverVisual();
            }
        }

        private void Update()
        {
            // Smoothly animate level-text progress fill via anchor-based width
            if (levelProgressFill != null && !Mathf.Approximately(currentProgressFill, targetProgressFill))
            {
                currentProgressFill = Mathf.MoveTowards(currentProgressFill, targetProgressFill, ProgressLerpSpeed * Time.deltaTime);
                var fillRect = levelProgressFill.rectTransform;
                fillRect.anchorMax = new Vector2(currentProgressFill, 0f);
            }
        }

        // ── Affordability / state background tinting ──────────────────────

        private void ApplyNodeBackgroundTint(bool isLocked, bool isMaxed, bool canAfford, bool isSurgeActive, bool showPendingUnlock)
        {
            if (nodeBackground == null) return;

            if (isMaxed)
            {
                // Gold-tinted background for maxed nodes
                Color gold = MutationTreeColors.MaxedGold;
                nodeBackground.color = new Color(gold.r * 0.3f, gold.g * 0.3f, gold.b * 0.15f, 1f);
            }
            else if (isLocked || isSurgeActive || showPendingUnlock)
            {
                nodeBackground.color = MutationTreeColors.DefaultNodeBG;
            }
            else if (canAfford)
            {
                // Subtle category-tinted glow when affordable (proper lerp, not additive)
                nodeBackground.color = MutationTreeColors.GetAffordableNodeBG(mutation.Category, 0.15f);
            }
            else
            {
                nodeBackground.color = MutationTreeColors.DefaultNodeBG;
            }
        }

        // ── Hover: prerequisite highlighting + projected cost ────────────

        private void ApplyInteractableHoverVisual()
        {
            if (nodeBackground == null || upgradeButton == null) return;
            if (!upgradeButton.interactable) return;

            nodeBackground.color = Color.Lerp(nodeBackground.color, MutationTreeColors.PrimaryText, 0.30f);
            ApplyTextContrast(useDarkText: true);
        }

        private void ApplyTextContrast(bool useDarkText)
        {
            Color primary = useDarkText ? HighlightedTextColor : MutationTreeColors.PrimaryText;
            Color secondary = useDarkText
                ? HighlightedSecondaryTextColor
                : MutationTreeColors.SecondaryText;

            if (mutationNameText != null)
            {
                mutationNameText.color = primary;
            }

            if (levelText != null)
            {
                levelText.color = secondary;
            }

            if (upgradeCostText != null)
            {
                upgradeCostText.color = primary;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerHovering = true;

            // Tooltip display is now handled by TooltipTrigger + ITooltipContentProvider.
            // We only keep prerequisite highlighting here.
            uiManager.HandleMutationNodeHover(mutation, player);

            // Stronger hover affordance for clickable/upgradeable nodes.
            ApplyInteractableHoverVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerHovering = false;

            // Tooltip hiding is handled by TooltipTrigger.OnPointerExit.
            uiManager.HandleMutationNodeHoverExit(mutation);

            // Restore correct base state tint after hover.
            UpdateDisplay();
        }

        // ── Upgrade feedback animation ───────────────────────────────────

        private void PlayUpgradeEffect()
        {
            if (upgradeEffectCoroutine != null)
                StopCoroutine(upgradeEffectCoroutine);
            upgradeEffectCoroutine = StartCoroutine(UpgradeEffectCoroutine());
        }

        private IEnumerator UpgradeEffectCoroutine()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            float maxScale = 1.06f; // subtle bounce — halved from original 1.12
            Vector3 originalScale = Vector3.one;
            Color originalBG = nodeBackground != null ? nodeBackground.color : Color.clear;
            Color flashColor = MutationTreeColors.UpgradeFlashWhite;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease-out-back scale punch: overshoot then settle
                float scaleT;
                if (t < 0.4f)
                {
                    // Rise to peak
                    scaleT = Mathf.Lerp(1f, maxScale, t / 0.4f);
                }
                else
                {
                    // Settle back with slight overshoot
                    float settleT = (t - 0.4f) / 0.6f;
                    scaleT = Mathf.Lerp(maxScale, 1f, settleT * settleT); // ease-in settle
                }
                transform.localScale = originalScale * scaleT;

                // Flash the background
                if (nodeBackground != null)
                {
                    float flashT = 1f - t; // bright at start, fades
                    nodeBackground.color = Color.Lerp(originalBG, flashColor, flashT * 0.6f);
                }

                yield return null;
            }

            transform.localScale = originalScale;
            // Restore proper background tint
            UpdateDisplay();
            upgradeEffectCoroutine = null;
        }

        // ── Shimmer (called by UI_MutationManager on panel open) ─────────

        /// <summary>
        /// Plays a brief alpha flash to draw attention to affordable nodes.
        /// </summary>
        public IEnumerator PlayShimmer()
        {
            if (canvasGroup == null) yield break;
            float originalAlpha = canvasGroup.alpha;
            float flashAlpha = Mathf.Min(originalAlpha + 0.35f, 1f);

            canvasGroup.alpha = flashAlpha;
            yield return new WaitForSeconds(0.12f);
            canvasGroup.alpha = originalAlpha;
        }

        /// <summary>
        /// Returns true if this node is currently affordable and not locked/maxed.
        /// Used by the shimmer system.
        /// </summary>
        public bool IsAffordableAndAvailable()
        {
            if (mutation == null || player == null) return false;
            int currentLevel = player.GetMutationLevel(mutation.Id);
            if (currentLevel >= mutation.MaxLevel) return false;

            foreach (var prereq in mutation.Prerequisites)
            {
                if (player.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel)
                    return false;
            }

            int cost = player.GetMutationPointCost(mutation);
            return player.MutationPoints >= cost;
        }

        private string BuildTooltip()
        {
            StringBuilder sb = new StringBuilder();
            string categoryDisplayName = GetMutationCategoryDisplayName(mutation.Category);

            sb.AppendLine($"<b>{mutation.Name}</b>");
            sb.AppendLine($"<i><color=#{ToHex(UIStyleTokens.Text.Secondary)}>(Tier {mutation.TierNumber} • {categoryDisplayName})</color></i>");
            sb.AppendLine();

            int currentLevel = player.GetMutationLevel(mutation.Id);

            // Show maxed state prominently
            if (currentLevel >= mutation.MaxLevel)
            {
                sb.AppendLine($"<color=#{ToHex(MutationTreeColors.MaxedGold)}><b>* FULLY UPGRADED *</b></color>");
                sb.AppendLine();
            }

            // Show surge state if relevant
            if (mutation.IsSurge && player.IsSurgeActive(mutation.Id))
            {
                int turns = player.GetSurgeTurnsRemaining(mutation.Id);
                sb.AppendLine($"<color=#{ToHex(MutationTreeColors.GetCategoryAccent(mutation.Category))}>Currently Active ({turns} turn{(turns == 1 ? "" : "s")} left)</color>");
                sb.AppendLine();
            }

            if (currentLevel < mutation.MaxLevel)
            {
                int cost = player.GetMutationPointCost(mutation);

                sb.AppendLine($"<b>Cost:</b> {cost} mutation point{(cost == 1 ? "" : "s")}");
                sb.AppendLine();
            }

            if (mutation.Prerequisites.Count > 0)
            {
                sb.AppendLine($"<i><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Requires:</color></i>");
                foreach (var prereq in mutation.Prerequisites)
                {
                    int ownedLevel = player.GetMutationLevel(prereq.MutationId);
                    var prereqMutation = uiManager.GetMutationById(prereq.MutationId);
                    string prereqText = $"- {prereqMutation?.Name ?? "Unknown"} (Level {ownedLevel}/{prereq.RequiredLevel})";
                    if (ownedLevel < prereq.RequiredLevel)
                    {
                        sb.AppendLine($"<color=#{ToHex(UIStyleTokens.State.Warning)}>{prereqText}</color>");
                    }
                    else
                    {
                        sb.AppendLine(prereqText);
                    }
                }
                sb.AppendLine();
            }

            if (mutation.IsSurge)
            {
                sb.AppendLine(BuildSurgeDurationLine());
                sb.AppendLine();
            }

            sb.AppendLine(FormatMutationDescription(mutation.Description));

            AppendDynamicMutationDetails(sb, currentLevel);

            if (!string.IsNullOrEmpty(mutation.FlavorText))
            {
                sb.AppendLine();
                sb.AppendLine($"<i>{mutation.FlavorText}</i>");
            }

            return sb.ToString();
        }

        private string BuildSurgeDurationLine()
        {
            int totalDuration = player.GetSurgeDuration(mutation);
            int durationBonus = player.GetSurgeDurationBonus(mutation);

            if (durationBonus <= 0)
            {
                return $"<b>Round Duration:</b> {totalDuration}";
            }

            string sourceName = player.GetAdaptation(AdaptationIds.HyphalEcho)?.Adaptation?.Name ?? "Hyphal Echo";
            return $"<b>Round Duration:</b> {totalDuration} (including +{durationBonus} bonus from {sourceName})";
        }

        private void AppendDynamicMutationDetails(StringBuilder sb, int currentLevel)
        {
            switch (mutation.Id)
            {
                case MutationIds.HomeostaticHarmony:
                    AppendHomeostaticHarmonyDetails(sb, currentLevel);
                    break;
                case MutationIds.ChronoresilientCytoplasm:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildChronoresilientCytoplasmSummary);
                    break;
                case MutationIds.Necrosporulation:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildNecrosporulationSummary);
                    break;
                case MutationIds.NecrohyphalInfiltration:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildNecrohyphalInfiltrationSummary);
                    break;
                case MutationIds.CatabolicRebirth:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildCatabolicRebirthSummary);
                    break;
                case MutationIds.HypersystemicRegeneration:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildHypersystemicRegenerationSummary);
                    break;
                case MutationIds.MycelialBloom:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildMycelialBloomSummary);
                    break;
                case MutationIds.TendrilNorthwest:
                case MutationIds.TendrilNortheast:
                case MutationIds.TendrilSoutheast:
                case MutationIds.TendrilSouthwest:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildTendrilSummary);
                    break;
                case MutationIds.MycotropicInduction:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildMycotropicInductionSummary);
                    break;
                case MutationIds.RegenerativeHyphae:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildRegenerativeHyphaeSummary);
                    break;
                case MutationIds.CreepingMold:
                    AppendLevelSummaryBlock(sb, currentLevel, BuildCreepingMoldSummary);
                    break;
            }
        }

        private void AppendHomeostaticHarmonyDetails(StringBuilder sb, int currentLevel)
        {
            AppendLevelSummaryBlock(sb, currentLevel, BuildHomeostaticHarmonySummary);
        }

        private void AppendLevelSummaryBlock(StringBuilder sb, int currentLevel, System.Func<int, string> buildSummary)
        {
            sb.AppendLine();
            sb.AppendLine($"{BuildSectionLabel($"Current Level ({currentLevel})", TooltipSectionLabelColor)} {buildSummary(currentLevel)}");

            if (currentLevel < mutation.MaxLevel)
            {
                sb.AppendLine($"{BuildSectionLabel($"Next Level ({currentLevel + 1})", TooltipSectionLabelColor)} {buildSummary(currentLevel + 1)}");
            }
        }

        private string BuildHomeostaticHarmonySummary(int level)
        {
            if (level <= 0)
            {
                return "No reduction yet.";
            }

            float reductionPercent = mutation.GetTotalEffect(level) * 100f;
            return $"Random decay -{reductionPercent:0.00}%, age-based decay -{reductionPercent:0.00}%";
        }

        private string BuildChronoresilientCytoplasmSummary(int level)
        {
            float ageThreshold = GameBalance.AgeAtWhichDecayChanceIncreases + mutation.GetTotalEffect(level);
            return $"Age-based decay starts after {ageThreshold:0} Growth Cycles";
        }

        private string BuildNecrosporulationSummary(int level)
        {
            if (level <= 0)
            {
                return "No spore-on-death chance yet.";
            }

            float chancePercent = mutation.GetTotalEffect(level) * 100f;
            return $"{chancePercent:0.00}% chance to spawn a new cell on a random open tile when one of your cells dies";
        }

        private string BuildNecrohyphalInfiltrationSummary(int level)
        {
            if (level <= 0)
            {
                return "No infiltration or cascade chance yet.";
            }

            float invadeChancePercent = level * GameBalance.NecrohyphalInfiltrationChancePerLevel * 100f;
            float cascadeChancePercent = level * GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel * 100f;
            return $"Invade adjacent dead enemy cell {invadeChancePercent:0.00}%, cascade from each successful reclaim {cascadeChancePercent:0.00}%";
        }

        private string BuildCatabolicRebirthSummary(int level)
        {
            if (level <= 0)
            {
                return "No resurrection chance when toxins expire.";
            }

            float chancePercent = mutation.GetTotalEffect(level) * 100f;
            if (level >= GameBalance.CatabolicRebirthMaxLevel)
            {
                return $"Revive adjacent dead cell on toxin expiration {chancePercent:0.00}%; enemy toxins next to your dead cells age twice as fast";
            }

            return $"Revive adjacent dead cell on toxin expiration {chancePercent:0.00}%";
        }

        private string BuildHypersystemicRegenerationSummary(int level)
        {
            float effectivenessPercent = level * GameBalance.HypersystemicRegenerationEffectivenessBonus * 100f;
            float resistanceChancePercent = level * GameBalance.HypersystemicRegenerationResistanceChance * 100f;
            if (level >= GameBalance.HypersystemicRegenerationMaxLevel)
            {
                return $"Regenerative Hyphae effectiveness +{effectivenessPercent:0.00}%, reclaimed-cell resistance {resistanceChancePercent:0.00}%, diagonal reclaim unlocked";
            }

            return $"Regenerative Hyphae effectiveness +{effectivenessPercent:0.00}%, reclaimed-cell resistance {resistanceChancePercent:0.00}%";
        }

        private string BuildMycelialBloomSummary(int level)
        {
            if (level <= 0)
            {
                return "No extra cardinal growth (up / down / left / right) or random decay.";
            }

            float orthogonalGrowthPercent = mutation.GetTotalEffect(level) * 100f;
            float randomDecayPercent = level * GameBalance.MycelialBloomRandomDecayPenaltyPerLevel * 100f;
            return $"Cardinal growth +{orthogonalGrowthPercent:0.00}%, random decay +{randomDecayPercent:0.00}%";
        }

        private string BuildTendrilSummary(int level)
        {
            if (level <= 0)
            {
                return $"No diagonal growth bonus or cardinal penalty contribution ({GameBalance.TendrilOrthogonalGrowthMinimumChance * 100f:0.00}% floor).";
            }

            float diagonalGrowthPercent = mutation.GetTotalEffect(level) * 100f;
            float orthogonalPenaltyPercent = level * GameBalance.TendrilOrthogonalGrowthPenaltyPerLevel * 100f;
            return $"Diagonal growth +{diagonalGrowthPercent:0.00}%, cardinal penalty contribution -{orthogonalPenaltyPercent:0.00}% ({GameBalance.TendrilOrthogonalGrowthMinimumChance * 100f:0.00}% floor)";
        }

        private string BuildMycotropicInductionSummary(int level)
        {
            float bonusPercent = mutation.GetTotalEffect(level) * 100f;
            float multiplier = 1f + mutation.GetTotalEffect(level);
            return $"Tendril diagonal multiplier x{multiplier:0.00} (+{bonusPercent:0.00}% of each Tendril's own chance)";
        }

        private string BuildRegenerativeHyphaeSummary(int level)
        {
            if (level <= 0)
            {
                return "No reclaim roll yet.";
            }

            float baseChancePercent = mutation.GetTotalEffect(level) * 100f;
            int hypersystemicLevel = player.GetMutationLevel(MutationIds.HypersystemicRegeneration);
            if (hypersystemicLevel <= 0)
            {
                return $"{baseChancePercent:0.00}% reclaim roll per living cell after the Growth Phase";
            }

            float effectiveChancePercent = (mutation.GetTotalEffect(level) * (1f + (hypersystemicLevel * GameBalance.HypersystemicRegenerationEffectivenessBonus))) * 100f;
            return $"{baseChancePercent:0.00}% reclaim roll per living cell after the Growth Phase ({effectiveChancePercent:0.00}% with Hypersystemic Regeneration)";
        }

        private string FormatMutationDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return string.Empty;
            }

            string formatted = description;
            formatted = formatted.Replace("\n<b>Max Level Bonus:</b>", "\n[[MAX_LEVEL_BONUS]]");
            formatted = formatted.Replace("<b>Max Level Bonus:</b>", "\n[[MAX_LEVEL_BONUS]]");
            formatted = formatted.Replace("<b>Technical:</b>", BuildSectionLabel("Technical", TooltipSectionLabelColor));
            formatted = formatted.Replace("[[MAX_LEVEL_BONUS]]", BuildSectionLabel("Max Level Bonus", TooltipBonusLabelColor));
            formatted = formatted.Replace("\nBuffed by:", "\n[[BUFFED_BY]]");
            formatted = formatted.Replace("Buffed by:", "[[BUFFED_BY]]");
            formatted = formatted.Replace("[[BUFFED_BY]]", BuildSectionLabel("Buffed by", TooltipSectionLabelColor));
            return formatted;
        }

        private static string BuildSectionLabel(string label, Color color)
            => $"<color=#{ToHex(color)}><b>{label}:</b></color>";

        private static string GetMutationCategoryDisplayName(MutationCategory category)
        {
            return category switch
            {
                MutationCategory.CellularResilience => "Cellular Resilience",
                MutationCategory.GeneticDrift => "Genetic Drift",
                MutationCategory.MycelialSurges => "Mycelial Surges",
                _ => category.ToString()
            };
        }

        private string BuildCreepingMoldSummary(int level)
        {
            if (level <= 0)
            {
                return "No failed-growth move chance yet.";
            }

            float moveChancePercent = mutation.GetTotalEffect(level) * 100f;
            if (level >= GameBalance.CreepingMoldMaxLevel)
            {
                return $"{moveChancePercent:0.00}% move chance after a failed growth if the target is open enough; toxin jump unlocked";
            }

            return $"{moveChancePercent:0.00}% move chance after a failed growth if the target is open enough";
        }

        public void DisableUpgrade()
        {
            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }

        public void SetPrerequisiteHighlight(bool on)
        {
            // The old full-card overlay was washing out text because it rendered above the label layer.
            // Keep it disabled and drive highlight readability through background tint + outline instead.
            if (prerequisiteHighlightOverlay != null)
                prerequisiteHighlightOverlay.SetActive(false);

            if (highlightOutline != null)
                highlightOutline.enabled = on;

            if (!on)
                return;

            ApplyHighlightCardVisual();
        }

        public void SetDependentHighlight(bool on)
        {
            // Same issue as prerequisite highlight: the overlay sits above TMP text and kills contrast.
            if (dependentHighlightOverlay != null)
                dependentHighlightOverlay.SetActive(false);

            if (highlightOutline != null)
            {
                highlightOutline.enabled = on;
                if (on)
                {
                    highlightOutline.effectColor = MutationTreeColors.DependentBorder;
                    highlightOutline.effectDistance = new Vector2(3f, -3f);
                }
                else
                {
                    highlightOutline.effectDistance = DefaultHighlightEffectDistance;
                }
            }

            if (!on)
                return;

            ApplyHighlightCardVisual();
        }

        public void ClearHighlights()
        {
            SetPrerequisiteHighlight(false);
            SetDependentHighlight(false);
            if (highlightOutline != null)
            {
                highlightOutline.effectDistance = DefaultHighlightEffectDistance;
                highlightOutline.enabled = false;
            }

            UpdateDisplay();
        }

        public void UpdateInteractable()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int upgradeCost = player.GetMutationPointCost(mutation);
            bool canAfford = player.MutationPoints >= upgradeCost;
            bool isLocked = false;
            foreach (var prereq in mutation.Prerequisites)
            {
                if (player.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel)
                {
                    isLocked = true;
                    break;
                }
            }
            // Check for pending unlock state (only for non-root mutations)
            bool showPendingUnlock = mutation.Prerequisites.Count > 0
                && player.PlayerMutations.TryGetValue(mutation.Id, out var pm)
                && pm.PrereqMetRound.HasValue
                && pm.PrereqMetRound.Value == GameManager.Instance.Board.CurrentRound;
            bool isMaxed = currentLevel >= mutation.MaxLevel;
            bool interactable = !isLocked && canAfford && !isMaxed && !showPendingUnlock;
            if (isSurge && isSurgeActive)
                interactable = false;
            upgradeButton.interactable = interactable;
        }

        // ── Runtime prefab augmentation ──────────────────────────────────
        // Creates UI children if they weren't wired in the prefab,
        // so the feature works even before you update the prefab.

        /// <summary>
        /// Creates a faint category-colored fill image behind the level text to
        /// show upgrade progress.  Replaces the old standalone progress bar.
        /// </summary>
        private void EnsureLevelProgressBG()
        {
            if (levelProgressFill != null) return;
            if (levelText == null) return;

            // Parent to the same container that holds levelText so it overlays
            // exactly behind the text.  Insert just before levelText in sibling
            // order so it renders behind it.
            Transform parent = levelText.transform.parent;
            var fillGO = new GameObject("LevelProgressFill");
            fillGO.transform.SetParent(parent, false);
            fillGO.transform.SetSiblingIndex(levelText.transform.GetSiblingIndex());

            levelProgressFill = fillGO.AddComponent<Image>();
            levelProgressFill.color = Color.white; // tinted per-category in UpdateDisplay
            levelProgressFill.raycastTarget = false;

            // Exclude from VerticalLayoutGroup on the node
            var layoutElem = fillGO.AddComponent<LayoutElement>();
            layoutElem.ignoreLayout = true;

            // Keep the fill as a narrow band aligned to the level row so it
            // reads as progress without flooding the entire center of the node.
            // anchorMax.x is driven by fill ratio in Update().
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 0f);
            fillRect.pivot = new Vector2(0f, 0f);
            fillRect.anchoredPosition = new Vector2(0f, 5f);
            fillRect.sizeDelta = new Vector2(0f, 18f);
        }

        private void EnsureNodeBorder()
        {
            // Add a thin, always-visible outline so each node has a distinct box,
            // even on a dark background.  Uses the upgrade button's Image as the target Graphic.
            var target = upgradeButton != null ? upgradeButton.gameObject : gameObject;

            // Don't duplicate if one already exists (beyond the highlight outline)
            foreach (var existing in target.GetComponents<Outline>())
            {
                if (existing != highlightOutline) return; // border already present
            }

            var border = target.AddComponent<Outline>();
            border.effectColor = new Color(MutationTreeColors.SecondaryText.r, MutationTreeColors.SecondaryText.g, MutationTreeColors.SecondaryText.b, 0.45f);
            border.effectDistance = DefaultHighlightEffectDistance;
        }

        private void EnsureDependentHighlightOverlay()
        {
            if (dependentHighlightOverlay != null) return;
            if (prerequisiteHighlightOverlay == null) return;

            dependentHighlightOverlay = Instantiate(prerequisiteHighlightOverlay, prerequisiteHighlightOverlay.transform.parent);
            dependentHighlightOverlay.name = "UI_DependentHighlightOverlay";
            dependentHighlightOverlay.SetActive(false);

            var dependentImage = dependentHighlightOverlay.GetComponent<Image>();
            if (dependentImage != null)
                dependentImage.color = MutationTreeColors.DependentHover;

            var prereqRect = prerequisiteHighlightOverlay.GetComponent<RectTransform>();
            var dependentRect = dependentHighlightOverlay.GetComponent<RectTransform>();
            if (prereqRect != null && dependentRect != null)
            {
                dependentRect.anchorMin = prereqRect.anchorMin;
                dependentRect.anchorMax = prereqRect.anchorMax;
                dependentRect.pivot = prereqRect.pivot;
                dependentRect.anchoredPosition = prereqRect.anchoredPosition;
                dependentRect.sizeDelta = prereqRect.sizeDelta;
            }

            SyncOverlayRectToButton(dependentHighlightOverlay);
        }

        private void SyncOverlayRectToButton(GameObject overlay)
        {
            if (overlay == null || upgradeButton == null) return;

            var overlayRect = overlay.GetComponent<RectTransform>();
            var buttonRect = upgradeButton.GetComponent<RectTransform>();
            if (overlayRect == null || buttonRect == null) return;

            overlayRect.anchorMin = buttonRect.anchorMin;
            overlayRect.anchorMax = buttonRect.anchorMax;
            overlayRect.pivot = buttonRect.pivot;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = buttonRect.sizeDelta;
            overlayRect.localScale = Vector3.one;
        }

        private bool ShouldUseDarkTextForCurrentBackground()
        {
            if (nodeBackground == null)
                return false;

            Color background = nodeBackground.color;
            float luminance = (0.2126f * background.r) + (0.7152f * background.g) + (0.0722f * background.b);
            return luminance >= DarkTextBackgroundLuminanceThreshold;
        }

        private void ApplyHighlightCardVisual()
        {
            if (nodeBackground != null)
            {
                Color highlightedBackground = Color.Lerp(
                    MutationTreeColors.GetAffordableNodeBG(mutation.Category, 0.24f),
                    MutationTreeColors.PrimaryText,
                    0.52f);
                highlightedBackground.a = 1f;
                nodeBackground.color = highlightedBackground;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            ApplyTextContrast(useDarkText: true);
        }

        private void EnsureMaxBadge()
        {
            if (maxBadge != null) return;

            // Parent to the upgrade button so the badge sits inside the visual card
            Transform badgeParent = upgradeButton != null ? upgradeButton.transform : transform;

            var badgeGO = new GameObject("MaxBadge");
            badgeGO.transform.SetParent(badgeParent, false);

            var badgeBG = badgeGO.AddComponent<Image>();
            badgeBG.color = new Color(MutationTreeColors.MaxedGold.r, MutationTreeColors.MaxedGold.g, MutationTreeColors.MaxedGold.b, 0.9f);

            var badgeRect = badgeGO.GetComponent<RectTransform>();
            // Bottom-center of the card, just above the progress bar
            badgeRect.anchorMin = new Vector2(0.5f, 0);
            badgeRect.anchorMax = new Vector2(0.5f, 0);
            badgeRect.pivot = new Vector2(0.5f, 0);
            badgeRect.anchoredPosition = new Vector2(0, 8);
            badgeRect.sizeDelta = new Vector2(36, 16);

            var textGO = new GameObject("MaxText");
            textGO.transform.SetParent(badgeGO.transform, false);
            var maxText = textGO.AddComponent<TextMeshProUGUI>();
            maxText.text = "MAX";
            maxText.fontSize = 10;
            maxText.fontStyle = FontStyles.Bold;
            maxText.color = UIStyleTokens.Text.OnAccent;
            maxText.alignment = TextAlignmentOptions.Center;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            maxBadge = badgeGO;
            maxBadge.SetActive(false);
        }

        /// <summary>
        /// Enables auto-sizing on the upgrade-cost badge so multi-digit costs
        /// (e.g. "x10") fit without overlapping the icon. Uses Unity's layout
        /// system rather than manual pixel calculations.
        /// </summary>
        private void ConfigureUpgradeCostBadge()
        {
            if (upgradeCostText == null || upgradeCostGroup == null) return;

            var groupRect = (RectTransform)upgradeCostGroup.transform;

            // Turn on childControlWidth so the layout group sizes each child
            // to its preferred width (TMP text reports actual text width,
            // Image reports sprite native size).
            var layoutGroup = upgradeCostGroup.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandWidth = false;
            }

            // Pin the icon to a fixed width via LayoutElement so the layout
            // group doesn't shrink or stretch it.
            if (groupRect.childCount > 0)
            {
                var iconTransform = groupRect.GetChild(0);
                var iconLayout = iconTransform.GetComponent<LayoutElement>();
                if (iconLayout == null)
                    iconLayout = iconTransform.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = UpgradeCostIconWidth;
                iconLayout.minWidth = UpgradeCostIconWidth;
            }

            // Auto-size the group container to fit icon + spacing + text + padding.
            var fitter = upgradeCostGroup.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = upgradeCostGroup.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);
        }

        private void ConfigureMutationNameFit()
        {
            if (mutationNameText == null)
                return;

            var textContainer = mutationNameText.transform.parent as RectTransform;
            if (textContainer != null)
            {
                var anchorMin = textContainer.anchorMin;
                var anchorMax = textContainer.anchorMax;
                anchorMin.x = 0f;
                anchorMax.x = 1f;
                textContainer.anchorMin = anchorMin;
                textContainer.anchorMax = anchorMax;
                textContainer.offsetMin = new Vector2(MutationNameHorizontalPadding, textContainer.offsetMin.y);
                textContainer.offsetMax = new Vector2(-MutationNameHorizontalPadding, textContainer.offsetMax.y);

                var fitter = textContainer.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            float targetSize = mutationNameText.enableAutoSizing ? mutationNameText.fontSizeMax : mutationNameText.fontSize;
            mutationNameText.enableAutoSizing = true;
            mutationNameText.textWrappingMode = TextWrappingModes.Normal;
            mutationNameText.overflowMode = TextOverflowModes.Truncate;
            mutationNameText.fontSizeMax = targetSize;
            mutationNameText.fontSizeMin = Mathf.Min(targetSize, MutationNameMinimumFontSize);
        }

        private static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
