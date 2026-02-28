using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;
using FungusToast.Unity;
using FungusToast.Unity.UI.Tooltips;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITooltipContentProvider
    {
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

        // Animation state
        private Coroutine upgradeEffectCoroutine;
        private float targetProgressFill;
        private float currentProgressFill;
        private static readonly float ProgressLerpSpeed = 6f;

        public int MutationId => mutation.Id;

        /// <summary>Exposes the underlying Mutation for external queries (e.g. investment summaries).</summary>
        public Mutation GetMutation() => mutation;

        public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
        {
            this.mutation = mutation;
            this.player = player;
            this.uiManager = uiManager;

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
                levelProgressFill.rectTransform.anchorMax = new Vector2(currentProgressFill, 1);

            UpdateDisplay();

            // Ensure highlights are off by default
            if (highlightOutline != null)
                highlightOutline.enabled = false;
            if (prerequisiteHighlightOverlay != null)
                prerequisiteHighlightOverlay.SetActive(false);

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

            // Wire up the new unified tooltip system via TooltipTrigger.
            // TooltipTrigger auto-resolves ITooltipContentProvider from this component.
            var trigger = GetComponent<TooltipTrigger>();
            if (trigger == null)
                trigger = gameObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(this);
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

            bool success = uiManager.TryUpgradeMutation(mutation);

            if (success)
            {
                UpdateDisplay();
                PlayUpgradeEffect();
            }
            else
            {
                upgradeButton.interactable = true;
            }
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
            int upgradeCost = isSurge
                ? mutation.GetSurgeActivationCost(currentLevel)
                : mutation.PointsPerUpgrade;

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
                levelProgressFill.color = MutationTreeColors.GetProgressBarColor(mutation.Category);

            // ── Affordability background tinting ──
            ApplyNodeBackgroundTint(isLocked, isMaxed, canAfford, isSurgeActive, showPendingUnlock);

            UpdateInteractable();
        }

        private void Update()
        {
            // Smoothly animate level-text progress fill via anchor-based width
            if (levelProgressFill != null && !Mathf.Approximately(currentProgressFill, targetProgressFill))
            {
                currentProgressFill = Mathf.MoveTowards(currentProgressFill, targetProgressFill, ProgressLerpSpeed * Time.deltaTime);
                var fillRect = levelProgressFill.rectTransform;
                fillRect.anchorMax = new Vector2(currentProgressFill, 1);
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Tooltip display is now handled by TooltipTrigger + ITooltipContentProvider.
            // We only keep prerequisite highlighting here.
            uiManager.HighlightUnmetPrerequisites(mutation, player);

            // Show projected cost in the points panel
            if (mutation != null && player != null)
            {
                int currentLevel = player.GetMutationLevel(mutation.Id);
                bool isMaxed = currentLevel >= mutation.MaxLevel;
                if (!isMaxed)
                {
                    int cost = mutation.IsSurge
                        ? mutation.GetSurgeActivationCost(currentLevel)
                        : mutation.PointsPerUpgrade;
                    uiManager.ShowProjectedCost(cost);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Tooltip hiding is handled by TooltipTrigger.OnPointerExit.
            uiManager.ClearAllHighlights();
            uiManager.ClearProjectedCost();
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

            int cost = mutation.IsSurge
                ? mutation.GetSurgeActivationCost(currentLevel)
                : mutation.PointsPerUpgrade;
            return player.MutationPoints >= cost;
        }

        private string BuildTooltip()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<b>{mutation.Name}</b>");
            sb.AppendLine($"<i>(Tier {mutation.TierNumber} • {mutation.Category})</i>");
            sb.AppendLine();

            int currentLevel = player.GetMutationLevel(mutation.Id);

            // Show maxed state prominently
            if (currentLevel >= mutation.MaxLevel)
            {
                sb.AppendLine("<color=#FFD700><b>✦ FULLY UPGRADED ✦</b></color>");
                sb.AppendLine();
            }

            // Show surge state if relevant
            if (mutation.IsSurge && player.IsSurgeActive(mutation.Id))
            {
                int turns = player.GetSurgeTurnsRemaining(mutation.Id);
                sb.AppendLine($"<color=#90f>Currently Active ({turns} turn{(turns == 1 ? "" : "s")} left)</color>");
                sb.AppendLine();
            }

            if (currentLevel < mutation.MaxLevel)
            {
                int cost = mutation.IsSurge
                    ? mutation.GetSurgeActivationCost(currentLevel)
                    : mutation.PointsPerUpgrade;

                sb.AppendLine($"<b>Cost:</b> {cost} mutation point{(cost == 1 ? "" : "s")}");
                sb.AppendLine();
            }

            if (mutation.Prerequisites.Count > 0)
            {
                sb.AppendLine("<i>Requires:</i>");
                foreach (var prereq in mutation.Prerequisites)
                {
                    int ownedLevel = player.GetMutationLevel(prereq.MutationId);
                    var prereqMutation = uiManager.GetMutationById(prereq.MutationId);
                    string prereqText = $"- {prereqMutation?.Name ?? "Unknown"} (Level {ownedLevel}/{prereq.RequiredLevel})";
                    if (ownedLevel < prereq.RequiredLevel)
                    {
                        sb.AppendLine($"<color=#CFFF04>{prereqText}</color>"); // Yellow-green for unmet
                    }
                    else
                    {
                        sb.AppendLine(prereqText);
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine(mutation.Description);

            if (!string.IsNullOrEmpty(mutation.FlavorText))
            {
                sb.AppendLine();
                sb.AppendLine($"<i>{mutation.FlavorText}</i>");
            }

            return sb.ToString();
        }

        public void DisableUpgrade()
        {
            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }

        public void SetHighlight(bool on)
        {
            // Use the new prerequisite highlight overlay for full square highlighting
            if (prerequisiteHighlightOverlay != null)
            {
                prerequisiteHighlightOverlay.SetActive(on);
                
                if (on)
                {
                    var rectTransform = prerequisiteHighlightOverlay.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // Fix the size issue by copying the size from the button
                        var buttonRect = upgradeButton.GetComponent<RectTransform>();
                        if (buttonRect != null)
                        {
                            // If the size is zero, copy from the button
                            if (rectTransform.sizeDelta == Vector2.zero)
                            {
                                rectTransform.sizeDelta = buttonRect.sizeDelta;
                            }
                            
                            // Ensure it matches the button's anchored position
                            rectTransform.anchoredPosition = buttonRect.anchoredPosition;
                        }
                    }
                    
                    // Force a layout rebuild and canvas update
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
            else
            {
                // Fallback to outline if no prerequisite highlight overlay is configured
                if (highlightOutline != null)
                    highlightOutline.enabled = on;
            }
        }

        public void UpdateInteractable()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int upgradeCost = isSurge
                ? mutation.GetSurgeActivationCost(currentLevel)
                : mutation.PointsPerUpgrade;
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

            // Stretch to match the level-text area exactly.
            // Anchors span the full parent; anchorMax.x will be driven by fill ratio.
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);  // starts at zero width
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
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
            border.effectColor = new Color(0.4f, 0.4f, 0.45f, 0.45f); // faint cool-gray
            border.effectDistance = new Vector2(1.2f, -1.2f);
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
            maxText.color = new Color(0.15f, 0.1f, 0f, 1f);
            maxText.alignment = TextAlignmentOptions.Center;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            maxBadge = badgeGO;
            maxBadge.SetActive(false);
        }
    }
}
