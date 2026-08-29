using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using FungusToast.Unity;
using FungusToast.Core.Config;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private const float MutationNameMinimumFontSize = UIStyleTokens.Typography.MicroMinimum;
        private const float MutationNameHorizontalPadding = 8f;
        private const int NodeTextTopPadding = 42;
        private const int NodeTextBottomPadding = 4;
        private const float NodeTextSpacing = 2f;
        private const float NodeNameTextHeight = 28f;
        private const float NodeStateTextHeight = 26f;
        private const float CompactNodeHeight = 120f;
        private const float MaxBadgeWidth = 44f;
        private const float MaxBadgeHeight = 20f;
        private const float SearchNonMatchAlpha = 0.10f;
        private const float UnrelatedRelationshipAlpha = 0.24f;
        private const float PurchasablePrerequisitePulseDurationSeconds = 1f;
        private static readonly Vector2 StatusIndicatorOffset = new(-38f, -20f);
        private static readonly Vector2 DefaultHighlightEffectDistance = new(1.2f, -1.2f);
        private static readonly Color HighlightedTextColor = new Color32(0x09, 0x0B, 0x07, 0xFF);
        private static readonly Color HighlightedSecondaryTextColor = new Color32(0x1A, 0x1E, 0x14, 0xFF);
        private const float DarkTextBackgroundLuminanceThreshold = 0.52f;

        // Upgrade-cost badge layout constants (must match prefab values)
        private const float UpgradeCostIconWidth = 28f;
        private const float UpgradeCostPaddingH = 4f;   // 2 left + 2 right in HorizontalLayoutGroup
        private const float UpgradeCostSpacing = 2f;
        private const float UpgradeCostMinTextWidth = 20f;

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
        private Outline nodeStateBorder;
        private Outline searchMatchOutline;
        private Outline purchasablePrerequisitePulseOutline;
        private TextMeshProUGUI purchasedGrowthMark;

        private Mutation mutation;
        private UI_MutationManager uiManager;
        private Player player;
        private bool isPointerHovering;
        private float baseCanvasAlpha = 1f;
        private bool isSearchActive;
        private bool isSearchMatch;
        private bool isRelationshipContextActive;
        private bool isRelationshipRelated;
        private bool isPurchasablePrerequisitePulseActive;

        // Animation state
        private Coroutine upgradeEffectCoroutine;
        private Coroutine blockedInvestmentPulseCoroutine;
        private int lastUpgradeAttemptFrame = -1;
        private BlockedInvestmentClickForwarder blockedInvestmentClickForwarder;
        private float targetProgressFill;
        private float currentProgressFill;
        private static readonly float ProgressLerpSpeed = 6f;
        private const float ProgressFillVisibilityBoost = 3.8f;

        public int MutationId => mutation.Id;

        /// <summary>
        /// The actual rendered card surface. The root node is a layout container
        /// and can be narrower or wider than the button image it hosts, so graph
        /// connectors must use this rect rather than <see cref="transform"/>.
        /// </summary>
        public RectTransform DependencyAnchorRect
        {
            get
            {
                if (nodeBackground != null)
                {
                    return nodeBackground.rectTransform;
                }

                if (upgradeButton != null)
                {
                    return upgradeButton.GetComponent<RectTransform>();
                }

                return transform as RectTransform;
            }
        }

        /// <summary>Exposes the underlying Mutation for external queries (e.g. investment summaries).</summary>
        public Mutation GetMutation() => mutation;

        public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
        {
            this.mutation = mutation;
            this.player = player;
            this.uiManager = uiManager;

            ConfigureMutationNameFit();
            ConfigureStateTextFit();
            ConfigureStatusIndicator(lockOverlay);
            ConfigureStatusIndicator(pendingUnlockOverlay);
            ConfigureStatusIndicator(surgeActiveOverlay);
            mutationNameText.text = mutation.Name;

            // ── Tier stripe — disabled; visual hierarchy handled by progress fill ──
            if (tierStripe != null)
                tierStripe.gameObject.SetActive(false);

            // ── Runtime-create level-text progress BG if not wired ──
            EnsureLevelProgressBG();

            // ── Runtime-create MAX badge if not wired in prefab ──
            EnsureMaxBadge();
            EnsurePurchasedGrowthMark();

            // ── Subtle border outline for visual node separation ──
            EnsureNodeBorder();
            EnsureSearchMatchOutline();
            ConfigureNodeButtonPresentation();

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

            // MutationNodeUI lives on the card root, which has no Graphic, so the
            // child upgrade Button (a Selectable, and therefore an IPointerDownHandler)
            // swallows the pointer press before it can bubble to this component's
            // OnPointerDown. Route click feedback through a forwarder that sits on the
            // Button itself so blocked-investment clicks are actually seen.
            EnsureBlockedInvestmentClickForwarder();
        }

        private void OnUpgradeClicked()
        {
            lastUpgradeAttemptFrame = Time.frameCount;

            var board = GameManager.Instance.Board;
            int currentRound = board.CurrentRound;
            if (!player.CanUpgrade(mutation, currentRound, board, uiManager.GetMutationAvailabilityBoardSummaries()))
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
            if (purchasedGrowthMark != null)
            {
                purchasedGrowthMark.gameObject.SetActive(currentLevel > 0);
            }

            // SURGE LOGIC
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int surgeTurns = isSurgeActive ? player.GetSurgeTurnsRemaining(mutation.Id) : 0;

            // PREREQS
            bool isLocked = !MutationPrerequisiteEvaluator.AreAllMet(mutation, player);

            // COST CALC
            int upgradeCost = player.GetMutationPointCost(mutation);

            bool canAfford = player.MutationPoints >= upgradeCost;

            // LOCK/SURGE/PENDING UI
            bool showPendingUnlock = MutationPrerequisiteEvaluator.HasRequirements(mutation)
                && player.PlayerMutations.TryGetValue(mutation.Id, out var pm)
                && pm.PrereqMetRound.HasValue
                && pm.PrereqMetRound.Value == GameManager.Instance.Board.CurrentRound;
            bool isDisabledBecauseNoEffect = ShouldShowNoEffectDisabledState(isLocked, isSurgeActive, showPendingUnlock, isMaxed);
            levelText.text = BuildNodeStateText(
                currentLevel,
                isLocked,
                isMaxed,
                canAfford,
                isSurgeActive,
                surgeTurns,
                showPendingUnlock,
                isDisabledBecauseNoEffect);
            lockOverlay.SetActive(isLocked && !isSurgeActive && !showPendingUnlock && !isDisabledBecauseNoEffect);
            if (pendingUnlockOverlay != null)
                pendingUnlockOverlay.SetActive(showPendingUnlock);
            if (pendingUnlockText != null)
                pendingUnlockText.text = "1";

            // State is communicated by the authored surface, labels, icons, and borders.
            // Keep the whole card opaque so its text and background retain their contrast;
            // Search is the only feature that intentionally fades mutation cards.
            baseCanvasAlpha = 1f;
            if (canvasGroup != null)
                canvasGroup.alpha = baseCanvasAlpha;

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
                else
                {
                    upgradeCostGroup.SetActive(true);
                    upgradeCostText.text = upgradeCost.ToString();
                    ConfigureUpgradeCostBadge();
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
            ApplyNodeBackgroundTint(currentLevel, isLocked, isMaxed, canAfford, isSurgeActive, showPendingUnlock, isDisabledBecauseNoEffect);
            ApplyTextContrast(useDarkText: ShouldUseDarkTextForCurrentBackground());
            ApplyNodeStateBorder(currentLevel, isLocked, isMaxed, canAfford, isSurgeActive, showPendingUnlock, isDisabledBecauseNoEffect);
            ApplyDisabledNoEffectOutline(isDisabledBecauseNoEffect);

            UpdateInteractable();

            if (isPointerHovering)
            {
                ApplyInteractableHoverVisual();
            }

            ApplySearchVisual();
        }

        private void OnDisable()
        {
            // A coroutine killed by the disable would otherwise leave an element
            // frozen mid-pulse; snap everything the pulse touches back to rest.
            if (blockedInvestmentPulseCoroutine != null)
            {
                StopCoroutine(blockedInvestmentPulseCoroutine);
                blockedInvestmentPulseCoroutine = null;
            }

            if (levelText != null)
                levelText.rectTransform.localScale = Vector3.one;
            if (upgradeCostGroup != null)
                upgradeCostGroup.transform.localScale = Vector3.one;
            if (lockOverlay != null)
                lockOverlay.transform.localScale = Vector3.one;
            transform.localScale = Vector3.one;
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

            UpdatePurchasablePrerequisitePulse();
        }

        // ── Affordability / state background tinting ──────────────────────

        private void ApplyNodeBackgroundTint(int currentLevel, bool isLocked, bool isMaxed, bool canAfford, bool isSurgeActive, bool showPendingUnlock, bool isDisabledBecauseNoEffect)
        {
            if (nodeBackground == null) return;

            if (isMaxed)
            {
                // Gold-tinted background for maxed nodes
                Color gold = MutationTreeColors.MaxedGold;
                nodeBackground.color = new Color(gold.r * 0.3f, gold.g * 0.3f, gold.b * 0.15f, 1f);
            }
            else if (isDisabledBecauseNoEffect)
            {
                nodeBackground.color = MutationTreeColors.WarningNodeBG;
            }
            else if (isLocked || isSurgeActive || showPendingUnlock)
            {
                nodeBackground.color = MutationTreeColors.LockedNodeBG;
            }
            else if (canAfford)
            {
                nodeBackground.color = MutationTreeColors.GetAffordableNodeBG(mutation.Category, currentLevel > 0 ? 0.25f : 0.20f);
            }
            else if (currentLevel > 0)
            {
                nodeBackground.color = MutationTreeColors.GetOwnedNodeBG(mutation.Category);
            }
            else
            {
                nodeBackground.color = MutationTreeColors.DefaultNodeBG;
            }
        }

        private string BuildNodeStateText(
            int currentLevel,
            bool isLocked,
            bool isMaxed,
            bool canAfford,
            bool isSurgeActive,
            int surgeTurns,
            bool showPendingUnlock,
            bool isDisabledBecauseNoEffect)
        {
            string level = $"L{currentLevel}/{mutation.MaxLevel}";

            // The node's top lane is reserved for the lock/status and DNA-cost
            // indicators. Keep the compact state and the level on separate lines
            // so a max level is never clipped on narrow cards.
            // The dedicated MAX badge is the sole terminal-state indicator.
            if (isMaxed) return level;
            if (isSurgeActive) return $"ACTIVE {surgeTurns}R\n{level}";
            if (showPendingUnlock) return $"NEXT ROUND\n{level}";
            if (isLocked) return $"LOCKED\n{level}";
            if (isDisabledBecauseNoEffect) return $"NO TARGET\n{level}";
            if (canAfford) return currentLevel > 0 ? $"READY\n{level}" : $"AVAILABLE\n{level}";
            return currentLevel > 0 ? $"OWNED\n{level}" : $"NEED POINTS\n{level}";
        }

        private void ApplyNodeStateBorder(
            int currentLevel,
            bool isLocked,
            bool isMaxed,
            bool canAfford,
            bool isSurgeActive,
            bool showPendingUnlock,
            bool isDisabledBecauseNoEffect)
        {
            if (nodeStateBorder == null)
            {
                return;
            }

            Color borderColor;
            Vector2 borderDistance = DefaultHighlightEffectDistance;

            if (isMaxed)
            {
                borderColor = UIStyleTokens.WithAlpha(MutationTreeColors.MaxedGold, 0.85f);
            }
            else if (showPendingUnlock || isDisabledBecauseNoEffect)
            {
                borderColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Warning, 0.78f);
            }
            else if (isLocked || isSurgeActive)
            {
                borderColor = UIStyleTokens.WithAlpha(UIStyleTokens.Text.Secondary, 0.78f);
            }
            else if (canAfford)
            {
                borderColor = UIStyleTokens.WithAlpha(MutationTreeColors.GetCategoryAccent(mutation.Category), 0.88f);
                borderDistance = new Vector2(2f, -2f);
            }
            else if (currentLevel > 0)
            {
                borderColor = UIStyleTokens.WithAlpha(MutationTreeColors.GetCategoryAccent(mutation.Category), 0.52f);
            }
            else
            {
                borderColor = UIStyleTokens.WithAlpha(MutationTreeColors.SecondaryText, 0.38f);
            }

            nodeStateBorder.effectColor = borderColor;
            nodeStateBorder.effectDistance = borderDistance;
        }

        // ── Hover: prerequisite highlighting + projected cost ────────────

        private void ApplyInteractableHoverVisual()
        {
            if (nodeBackground == null || upgradeButton == null) return;
            if (!upgradeButton.interactable) return;

            nodeBackground.color = Color.Lerp(
                nodeBackground.color,
                MutationTreeColors.GetCategoryAccent(mutation.Category),
                0.24f);
            ApplyTextContrast(useDarkText: false);
        }

        private void ApplyTextContrast(bool useDarkText)
        {
            Color primary = useDarkText ? HighlightedTextColor : MutationTreeColors.PrimaryText;
            Color secondary = useDarkText
                ? HighlightedSecondaryTextColor
                : MutationTreeColors.PrimaryText;

            if (mutationNameText != null)
            {
                mutationNameText.color = primary;
            }

            if (levelText != null)
            {
                levelText.color = secondary;
            }

            if (purchasedGrowthMark != null)
            {
                purchasedGrowthMark.color = secondary;
            }

            if (upgradeCostText != null)
            {
                upgradeCostText.color = primary;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerHovering = true;

            uiManager.HandleMutationNodeHover(mutation, player);

            // Stronger hover affordance for clickable/upgradeable nodes.
            ApplyInteractableHoverVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerHovering = false;

            uiManager.HandleMutationNodeHoverExit(mutation);

            // Restore correct base state tint after hover.
            UpdateDisplay();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                uiManager.HandleMutationNodeSelected(mutation, player);
            }
        }

        /// <summary>
        /// Invoked by <see cref="BlockedInvestmentClickForwarder"/> for every left click on
        /// the node's Button, including clicks the Button ignores because it is not
        /// interactable (locked / unaffordable) — those never reach this component's
        /// OnPointerDown. A click the Button handled as a real upgrade this frame is
        /// skipped via <see cref="lastUpgradeAttemptFrame"/>.
        /// </summary>
        private void HandleForwardedClick()
        {
            if (lastUpgradeAttemptFrame == Time.frameCount)
                return;

            PlayBlockedInvestmentFeedbackIfNeeded();
        }

        /// <summary>
        /// When the player clicks a mutation they cannot invest in, play a single
        /// attention pulse over the specific blocker so the reason is obvious:
        ///  • unlocked but unaffordable → pulse the "NEED POINTS" line and the cost badge
        ///  • locked → pulse this node's lock indicator and the unmet prerequisite node(s) on its path
        /// Maxed / pending-next-round / active-surge / no-target states are left alone;
        /// they already read clearly and are not something the player can act on here.
        /// </summary>
        private void PlayBlockedInvestmentFeedbackIfNeeded()
        {
            if (mutation == null || player == null || uiManager == null)
                return;

            // Anything the player can actually buy right now needs no nudge.
            if (upgradeButton != null && upgradeButton.interactable)
                return;

            int currentLevel = player.GetMutationLevel(mutation.Id);
            if (currentLevel >= mutation.MaxLevel)
                return;

            bool isSurgeActive = mutation.IsSurge && player.IsSurgeActive(mutation.Id);
            if (isSurgeActive)
                return;

            bool isLocked = !MutationPrerequisiteEvaluator.AreAllMet(mutation, player);

            bool showPendingUnlock = MutationPrerequisiteEvaluator.HasRequirements(mutation)
                && player.PlayerMutations.TryGetValue(mutation.Id, out var pm)
                && pm.PrereqMetRound.HasValue
                && pm.PrereqMetRound.Value == GameManager.Instance.Board.CurrentRound;
            if (showPendingUnlock)
                return;

            if (isLocked)
            {
                // Feedback at the point of interaction (the clicked lock) plus the
                // resolution target (what unlocks it).
                PlayLockedAttentionPulse();
                uiManager.PulseUnmetPrerequisitesFor(mutation, player);
                return;
            }

            if (ShouldShowNoEffectDisabledState(isLocked, isSurgeActive, showPendingUnlock, isMaxed: false))
                return;

            int upgradeCost = player.GetMutationPointCost(mutation);
            if (player.MutationPoints >= upgradeCost)
                return;

            PlayInsufficientPointsAttentionPulse();
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
            float duration = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode
                ? 0.08f
                : 0.22f;
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

        // ── Blocked-investment attention pulse ───────────────────────────
        // A single rise-and-fall pulse (never looping) that points at the reason
        // a click did nothing. Follows the project's established attention-pulse
        // idiom: unscaled time, one Mathf.Sin(progress * PI) sweep, non-blocking.

        private readonly struct PulseTarget
        {
            public PulseTarget(RectTransform rectTransform, Graphic graphic, Color flashColor)
            {
                RectTransform = rectTransform;
                BaseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
                Graphic = graphic;
                BaseColor = graphic != null ? graphic.color : Color.white;
                FlashColor = flashColor;
            }

            public RectTransform RectTransform { get; }
            public Vector3 BaseScale { get; }
            public Graphic Graphic { get; }
            public Color BaseColor { get; }
            public Color FlashColor { get; }
        }

        /// <summary>
        /// Pulses the "NEED POINTS" line and the DNA cost badge to make clear the
        /// only thing standing between the player and this upgrade is mutation points.
        /// </summary>
        public void PlayInsufficientPointsAttentionPulse()
        {
            RestoreBlockedInvestmentPulseTargets();

            var targets = new List<PulseTarget>(3);
            Color danger = UIStyleTokens.State.Danger;

            if (levelText != null)
                targets.Add(new PulseTarget(levelText.rectTransform, levelText, danger));

            if (upgradeCostGroup != null && upgradeCostGroup.activeInHierarchy)
            {
                var groupRect = upgradeCostGroup.transform as RectTransform;
                Graphic costGraphic = upgradeCostText != null ? upgradeCostText : null;
                targets.Add(new PulseTarget(groupRect, costGraphic, danger));
            }

            StartBlockedInvestmentPulse(targets);
        }

        /// <summary>
        /// Pulses this node's lock indicator and "LOCKED" line when the player clicks a
        /// mutation whose prerequisites are not met, so the click registers at the point
        /// of interaction as well as on the prerequisites that resolve it.
        /// </summary>
        public void PlayLockedAttentionPulse()
        {
            RestoreBlockedInvestmentPulseTargets();

            var targets = new List<PulseTarget>(2);
            Color warning = UIStyleTokens.State.Warning;

            if (lockOverlay != null && lockOverlay.activeInHierarchy && lockOverlay.transform is RectTransform lockRect)
            {
                // Scale-only for the lock glyph; the "LOCKED" line below carries the colour cue.
                targets.Add(new PulseTarget(lockRect, graphic: null, warning));
            }

            if (levelText != null)
                targets.Add(new PulseTarget(levelText.rectTransform, levelText, warning));

            StartBlockedInvestmentPulse(targets);
        }

        /// <summary>
        /// Pulses this node as an unmet prerequisite blocking a mutation the player
        /// just tried to buy. Emphasises the whole card so the blocked path is legible.
        /// </summary>
        public void PlayUnmetPrerequisiteAttentionPulse()
        {
            RestoreBlockedInvestmentPulseTargets();

            var targets = new List<PulseTarget>(1);
            // Scaling the card root is safe here: an unmet prerequisite is never
            // mid upgrade-success animation.
            RectTransform cardRect = upgradeEffectCoroutine == null ? transform as RectTransform : null;
            targets.Add(new PulseTarget(cardRect, nodeBackground, UIStyleTokens.State.Warning));

            StartBlockedInvestmentPulse(targets);
        }

        private void StartBlockedInvestmentPulse(List<PulseTarget> targets)
        {
            if (targets == null || targets.Count == 0 || !isActiveAndEnabled)
                return;

            blockedInvestmentPulseCoroutine = StartCoroutine(BlockedInvestmentPulseCoroutine(targets));
        }

        private void RestoreBlockedInvestmentPulseTargets()
        {
            if (blockedInvestmentPulseCoroutine == null)
                return;

            StopCoroutine(blockedInvestmentPulseCoroutine);
            blockedInvestmentPulseCoroutine = null;
            UpdateDisplay();
        }

        private IEnumerator BlockedInvestmentPulseCoroutine(List<PulseTarget> targets)
        {
            float duration = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode
                ? UIEffectConstants.MutationNodeBlockedInvestmentPulseFastDurationSeconds
                : UIEffectConstants.MutationNodeBlockedInvestmentPulseDurationSeconds;
            float peakScale = UIEffectConstants.MutationNodeBlockedInvestmentPulsePeakScaleMultiplier;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI); // 0 → 1 → 0

                for (int i = 0; i < targets.Count; i++)
                {
                    PulseTarget target = targets[i];
                    if (target.RectTransform != null)
                        target.RectTransform.localScale = target.BaseScale * Mathf.Lerp(1f, peakScale, strength);
                    if (target.Graphic != null)
                        target.Graphic.color = Color.Lerp(target.BaseColor, target.FlashColor, strength);
                }

                yield return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                PulseTarget target = targets[i];
                if (target.RectTransform != null)
                    target.RectTransform.localScale = target.BaseScale;
                if (target.Graphic != null)
                    target.Graphic.color = target.BaseColor;
            }

            blockedInvestmentPulseCoroutine = null;

            // Re-sync to authoritative state (restores exact tint/contrast).
            UpdateDisplay();
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

            if (!MutationPrerequisiteEvaluator.AreAllMet(mutation, player)) return false;

            int cost = player.GetMutationPointCost(mutation);
            return player.MutationPoints >= cost;
        }

        public string BuildLevelSummary(int level)
        {
            return mutation.Id switch
            {
                MutationIds.HomeostaticHarmony => BuildHomeostaticHarmonySummary(level),
                MutationIds.ChronoresilientCytoplasm => BuildChronoresilientCytoplasmSummary(level),
                MutationIds.Necrosporulation => BuildNecrosporulationSummary(level),
                MutationIds.NecrohyphalInfiltration => BuildNecrohyphalInfiltrationSummary(level),
                MutationIds.CatabolicRebirth => BuildCatabolicRebirthSummary(level),
                MutationIds.HypersystemicRegeneration => BuildHypersystemicRegenerationSummary(level),
                MutationIds.MycelialBloom => BuildMycelialBloomSummary(level),
                MutationIds.TendrilNorthwest or MutationIds.TendrilNortheast or MutationIds.TendrilSoutheast or MutationIds.TendrilSouthwest => BuildTendrilSummary(level),
                MutationIds.MycotropicInduction => BuildMycotropicInductionSummary(level),
                MutationIds.RegenerativeHyphae => BuildRegenerativeHyphaeSummary(level),
                MutationIds.CreepingMold => BuildCreepingMoldSummary(level),
                MutationIds.MycotoxinTracer => BuildMycotoxinTracerSummary(level),
                MutationIds.MycotoxinPotentiation => BuildMycotoxinPotentiationSummary(level),
                MutationIds.PutrefactiveMycotoxin => BuildPutrefactiveMycotoxinSummary(level),
                MutationIds.SporicidalBloom => BuildSporicidalBloomSummary(level),
                MutationIds.NecrotoxicConversion => BuildNecrotoxicConversionSummary(level),
                MutationIds.PutrefactiveRejuvenation => BuildPutrefactiveRejuvenationSummary(level),
                MutationIds.PutrefactiveCascade => BuildPutrefactiveCascadeSummary(level),
                MutationIds.MutatorPhenotype => BuildMutatorPhenotypeSummary(level),
                MutationIds.AdaptiveExpression => BuildAdaptiveExpressionSummary(level),
                MutationIds.MycotoxinCatabolism => BuildMycotoxinCatabolismSummary(level),
                MutationIds.AnabolicInversion => BuildAnabolicInversionSummary(level),
                MutationIds.LatentPolymorphism => BuildLatentPolymorphismSummary(level),
                MutationIds.NecrophyticBloom => BuildNecrophyticBloomSummary(level),
                MutationIds.MycotoxinFission => BuildToxinborneSeedingSummary(level),
                MutationIds.HyperadaptiveDrift => BuildHyperadaptiveDriftSummary(level),
                MutationIds.OntogenicRegression => BuildOntogenicRegressionSummary(level),
                MutationIds.HyphalSurge => BuildAutolyticSurgeSummary(level),
                MutationIds.ChemotacticBeacon => BuildChemotacticBeaconSummary(level),
                MutationIds.MimeticResilience => BuildMimeticResilienceSummary(level),
                MutationIds.CompetitiveAntagonism => BuildCompetitiveAntagonismSummary(level),
                MutationIds.ChitinFortification => BuildChitinFortificationSummary(level),
                MutationIds.AeratedFrontier => BuildAeratedFrontierSummary(level),
                MutationIds.CrustwardTropism => BuildCrustwardTropismSummary(level),
                MutationIds.CompactionPressure => BuildCompactionPressureSummary(level),
                MutationIds.DetritalEnzymes => BuildDetritalEnzymesSummary(level),
                MutationIds.ToxinMargin => BuildToxinMarginSummary(level),
                _ => BuildFallbackLevelSummary(level)
            };
        }

        private string BuildAutolyticSurgeSummary(int level)
        {
            if (level <= 0)
            {
                return "No active cardinal growth or random-decay change yet.";
            }

            float growthChancePercent = level * GameBalance.HyphalSurgeEffectPerLevel * 100f;
            float randomDecayChancePercent = level * GameBalance.HyphalSurgeRandomDecayPenaltyPerLevel * 100f;
            return $"While active: cardinal growth +{growthChancePercent:0.00}%, random decay +{randomDecayChancePercent:0.00}% for {GameBalance.HyphalSurgeDurationRounds} rounds";
        }

        private string BuildChemotacticBeaconSummary(int level)
        {
            if (level <= 0)
            {
                return "No beacon growth line yet.";
            }

            int projectedCells = GameBalance.ChemotacticBeaconBaseTiles
                + (level * GameBalance.ChemotacticBeaconTilesPerLevel);
            return $"While active: projects up to {projectedCells} living cells toward the marker at Growth Phase end";
        }

        private string BuildMimeticResilienceSummary(int level)
        {
            if (level <= 0)
            {
                return "No resistant foothold attempts yet.";
            }

            return $"While active: one resistant foothold attempt around each qualifying stronger rival within {level + 1} tiles (max 20 successes per rival per Growth Phase)";
        }

        private string BuildCompetitiveAntagonismSummary(int level)
        {
            if (level <= 0)
            {
                return "No stronger-colony targeting priority yet.";
            }

            return $"While active: toxin effects prioritize stronger colonies; level changes activation cost, not targeting strength";
        }

        private string BuildChitinFortificationSummary(int level)
        {
            if (level <= 0)
            {
                return "No cells fortified yet.";
            }

            int cellsPerGrowthPhase = level * GameBalance.ChitinFortificationCellsPerLevel;
            return $"While active: permanently fortifies {cellsPerGrowthPhase} living cells per Growth Phase for {GameBalance.ChitinFortificationSurgeDuration} rounds";
        }

        private string BuildAeratedFrontierSummary(int level)
        {
            if (level <= 0)
            {
                return "No established-frontier growth bonus yet.";
            }

            float chancePercent = level * GameBalance.AeratedFrontierEffectPerLevel * 100f;
            return $"+{chancePercent:0.00}% growth chance from cells aged {GameBalance.AeratedFrontierMinimumEligibleGrowthCycleAge}+ cycles with {GameBalance.AeratedFrontierRequiredOpenOrthogonalSpaces}+ open cardinal neighbors";
        }

        private string BuildCrustwardTropismSummary(int level)
        {
            if (level <= 0)
            {
                return "No outward crustward growth bonus yet.";
            }

            float chancePercent = level * GameBalance.CrustwardTropismEffectPerLevel * 100f;
            string summary = $"+{chancePercent:0.00}% growth chance when the target is closer to the playable crust";
            return level >= GameBalance.CrustwardTropismMaxLevel
                ? summary + "; first qualifying crust placement each Growth Cycle succeeds automatically"
                : summary;
        }

        private string BuildCompactionPressureSummary(int level)
        {
            if (level <= 0)
            {
                return "No cramped-territory growth bonus yet.";
            }

            float chancePercent = level * GameBalance.CompactionPressureEffectPerLevel * 100f;
            return $"+{chancePercent:0.00}% growth chance from cells with {GameBalance.CompactionPressureMinimumLegalOrthogonalTargets}-{GameBalance.CompactionPressureMaximumLegalOrthogonalTargets} legal cardinal targets";
        }

        private string BuildDetritalEnzymesSummary(int level)
        {
            if (level <= 0)
            {
                return "No dead-matter growth bonus yet.";
            }

            float chancePercent = level * GameBalance.DetritalEnzymesEffectPerLevel * 100f;
            string summary = $"+{chancePercent:0.00}% growth chance next to non-toxic dead cells";
            return level >= GameBalance.DetritalEnzymesMaxLevel
                ? summary + $"; +{GameBalance.DetritalEnzymesDenseDeadMatterBonus * 100f:0.00}% with {GameBalance.DetritalEnzymesDenseDeadMatterRequiredNeighbors}+ adjacent dead cells"
                : summary;
        }

        private string BuildToxinMarginSummary(int level)
        {
            if (level <= 0)
            {
                return "No enemy-toxin boundary growth bonus yet.";
            }

            float chancePercent = level * GameBalance.ToxinMarginEffectPerLevel * 100f;
            return $"+{chancePercent:0.00}% growth chance into empty targets next to enemy-owned toxins";
        }

        private string BuildFallbackLevelSummary(int level)
        {
            float totalEffect = mutation.GetTotalEffect(level);
            return totalEffect == 0f
                ? "No level-scaled effect is defined."
                : $"Total effect: {totalEffect:0.##}";
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
                return "No reclaim or cascade chance yet.";
            }

            float reclaimChancePercent = level * GameBalance.NecrohyphalInfiltrationChancePerLevel * 100f;
            float cascadeChancePercent = level * GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel * 100f;
            return $"Reclaim adjacent dead enemy cell {reclaimChancePercent:0.00}%, cascade from each successful reclaim {cascadeChancePercent:0.00}%";
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

        private string BuildMycotoxinTracerSummary(int level)
        {
            if (level <= 0)
            {
                return "No border-toxin pressure yet.";
            }

            float failedGrowthWeightPercent = level * GameBalance.MycotoxinTracerFailedGrowthWeightPerLevel * 100f;
            float lowColonyBonusPercent = level * GameBalance.MycotoxinTracerFailureRateWeightPerLevel * 100f;
            return $"Failed-growth toxin pressure +{failedGrowthWeightPercent:0.00}%, low-colony bonus +{lowColonyBonusPercent:0.00}%, toxin duration {GameBalance.MycotoxinTracerTileDuration} cycles";
        }

        private string BuildMycotoxinPotentiationSummary(int level)
        {
            if (level <= 0)
            {
                return "No toxin duration or kill aura bonus yet.";
            }

            int durationBonus = level * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel;
            float killChancePercent = level * GameBalance.MycotoxinPotentiationKillChancePerLevel * 100f;
            return $"New toxins last +{durationBonus} Growth Cycle{(durationBonus == 1 ? string.Empty : "s")}, orthogonal toxin kill chance {killChancePercent:0.00}%";
        }

        private string BuildPutrefactiveMycotoxinSummary(int level)
        {
            if (level <= 0)
            {
                return "No adjacent toxin-kill chance yet.";
            }

            float killChancePercent = level * GameBalance.PutrefactiveMycotoxinEffectPerLevel * 100f;
            if (level >= GameBalance.PutrefactiveMycotoxinMaxLevel)
            {
                return $"Orthogonal adjacent kill chance {killChancePercent:0.00}%; active Chemotactic Beacon spreads the aura within 2 tiles";
            }

            return $"Orthogonal adjacent kill chance {killChancePercent:0.00}%";
        }

        private string BuildSporicidalBloomSummary(int level)
        {
            if (level <= 0)
            {
                return "No toxic spore drops yet.";
            }

            float sporeRatePercent = level * GameBalance.SporicialBloomEffectPerLevel * 100f;
            if (level >= GameBalance.SporicidalBloomMaxLevel)
            {
                return $"Drops spores equal to about {sporeRatePercent:0.00}% of living cells each Decay Phase, toxin duration {GameBalance.SporocidalToxinTileDuration} cycles, empty-tile pool reduced by 25%";
            }

            return $"Drops spores equal to about {sporeRatePercent:0.00}% of living cells each Decay Phase, toxin duration {GameBalance.SporocidalToxinTileDuration} cycles";
        }

        private string BuildNecrotoxicConversionSummary(int level)
        {
            if (level <= 0)
            {
                return "No toxin-to-reclaim conversion chance yet.";
            }

            float reclaimChancePercent = level * GameBalance.NecrotoxicConversionReclaimChancePerLevel * 100f;
            return $"{reclaimChancePercent:0.00}% chance for your toxin kills to convert straight into living cells";
        }

        private string BuildPutrefactiveRejuvenationSummary(int level)
        {
            if (level <= 0)
            {
                return "No rejuvenation pulse or Putrefactive Mycotoxin bonus yet.";
            }

            int ageReduction = level * GameBalance.PutrefactiveRejuvenationAgeReductionPerLevel;
            float mycotoxinBonusPercent = level * GameBalance.PutrefactiveRejuvenationMycotoxinBonusPerLevel * 100f;
            int radius = level >= GameBalance.PutrefactiveRejuvenationMaxLevel
                ? GameBalance.PutrefactiveRejuvenationEffectRadius * GameBalance.PutrefactiveRejuvenationMaxLevelRangeRadiusMultiplier
                : GameBalance.PutrefactiveRejuvenationEffectRadius;
            return $"Putrefactive kills reduce nearby friendly age by {ageReduction} cycles within radius {radius}, Putrefactive Mycotoxin +{mycotoxinBonusPercent:0.00}%";
        }

        private string BuildPutrefactiveCascadeSummary(int level)
        {
            if (level <= 0)
            {
                return "No cascade chance or Putrefactive Mycotoxin bonus yet.";
            }

            float effectivenessBonusPercent = level * GameBalance.PutrefactiveCascadeEffectivenessBonus * 100f;
            float cascadeChancePercent = level * GameBalance.PutrefactiveCascadeCascadeChance * 100f;
            if (level >= GameBalance.PutrefactiveCascadeMaxLevel)
            {
                return $"Putrefactive Mycotoxin +{effectivenessBonusPercent:0.00}%, directional cascade chance {cascadeChancePercent:0.00}%, cascades leave toxins";
            }

            return $"Putrefactive Mycotoxin +{effectivenessBonusPercent:0.00}%, directional cascade chance {cascadeChancePercent:0.00}%";
        }

        private string BuildMutatorPhenotypeSummary(int level)
        {
            if (level <= 0)
            {
                return "No free Tier 1 upgrade chance yet.";
            }

            float upgradeChancePercent = level * GameBalance.MutatorPhenotypeEffectPerLevel * 100f;
            return $"{upgradeChancePercent:0.00}% chance to auto-upgrade one random Tier 1 mutation at Mutation Phase start";
        }

        private string BuildAdaptiveExpressionSummary(int level)
        {
            if (level <= 0)
            {
                return "No bonus mutation-point chance yet.";
            }

            float firstPointChancePercent = level * GameBalance.AdaptiveExpressionEffectPerLevel * 100f;
            float secondPointChancePercent = level * GameBalance.AdaptiveExpressionSecondPointChancePerLevel * 100f;
            return $"{firstPointChancePercent:0.00}% chance to gain 1 bonus mutation point, then {secondPointChancePercent:0.00}% chance for a second";
        }

        private string BuildMycotoxinCatabolismSummary(int level)
        {
            if (level <= 0)
            {
                return "No toxin cleanup or mutation-point harvest yet.";
            }

            float cleanupChancePercent = level * GameBalance.MycotoxinCatabolismCleanupChancePerLevel * 100f;
            float mutationPointChancePercent = level * GameBalance.MycotoxinCatabolismMutationPointChancePerLevel * 100f;
            return $"Orthogonal toxin cleanup chance {cleanupChancePercent:0.00}%, mutation-point chance per consumed toxin {mutationPointChancePercent:0.00}% (max {GameBalance.MycotoxinCatabolismMaxMutationPointsPerRound}/round)";
        }

        private string BuildAnabolicInversionSummary(int level)
        {
            if (level <= 0)
            {
                return "No catch-up trigger bonus yet.";
            }

            float triggerBonusPercent = level * GameBalance.AnabolicInversionGapBonusPerLevel * 100f;
            return $"Adds +{triggerBonusPercent:0.00}% catch-up trigger chance based on living-cell deficit, payout 1-{GameBalance.AnabolicInversionMaxMutationPointsPerRound} mutation points";
        }

        private string BuildLatentPolymorphismSummary(int level)
        {
            if (level <= 0)
            {
                return "No interest on banked mutation points yet.";
            }

            float interestPercent = level * GameBalance.LatentPolymorphismInterestRatePerLevel * 100f;
            return $"Banked mutation points earn {interestPercent:0.00}% interest, up to {GameBalance.LatentPolymorphismMaxInterestPerRound} bonus points per Mutation Phase";
        }

        private string BuildToxinborneSeedingSummary(int level)
        {
            if (level <= 0)
            {
                return "No friendly-toxin growth bonus or toxin-carried cell seeding yet.";
            }

            float growthChancePercent = level * GameBalance.ToxinborneSeedingEffectPerLevel * 100f;
            return $"+{growthChancePercent:0.00}% growth chance next to friendly toxins; one toxin carries the new cell to enemy territory";
        }

        private string BuildNecrophyticBloomSummary(int level)
        {
            if (level <= 0)
            {
                return "No dead-cluster composting yet.";
            }

            int clusterThreshold = GameBalance.NecrophyticBloomBaseClusterThreshold
                - ((level - 1) * GameBalance.NecrophyticBloomClusterThresholdReductionPerLevel);
            if (clusterThreshold < 1)
            {
                clusterThreshold = 1;
            }

            float compostChancePercent = (
                GameBalance.NecrophyticBloomBaseCompostChance
                + ((level - 1) * GameBalance.NecrophyticBloomCompostChanceIncreasePerLevel)) * 100f;

            string summary = $"Dead clusters of {clusterThreshold}+ cells compost at {compostChancePercent:0.00}% per cluster, up to {GameBalance.NecrophyticBloomMaxPatchesPerRound} patches of {GameBalance.NecrophyticBloomMaxPatchSize} tiles";
            if (level >= GameBalance.NecrophyticBloomMaxLevel)
            {
                summary += "; Hypervariation patches unlocked";
            }

            return summary;
        }

        private string BuildHyperadaptiveDriftSummary(int level)
        {
            if (level <= 0)
            {
                return "No higher-tier or chained Mutator Phenotype bonus yet.";
            }

            float higherTierChancePercent = level * GameBalance.HyperadaptiveDriftHigherTierChancePerLevel * 100f;
            float bonusTierOneChancePercent = level * GameBalance.HyperadaptiveDriftBonusTierOneMutationChancePerLevel * 100f;
            string summary = $"Mutator Phenotype gains {higherTierChancePercent:0.00}% Tier 2-4 targeting chance and {bonusTierOneChancePercent:0.00}% chance to chain up to {GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes} Tier 1 attempts";
            if (level >= GameBalance.HyperadaptiveDriftMaxLevel)
            {
                summary += "; one second-mutation Tier 1 attempt added";
            }

            return summary;
        }

        private string BuildOntogenicRegressionSummary(int level)
        {
            if (level <= 0)
            {
                return "No Tier 1 sacrifice roll yet.";
            }

            float regressionChancePercent = level * GameBalance.OntogenicRegressionChancePerLevel * 100f;
            string summary = $"{regressionChancePercent:0.00}% chance to trade {GameBalance.OntogenicRegressionTier1LevelsToConsume} Tier 1 levels for +1 Tier 5/6 level; otherwise gain {GameBalance.OntogenicRegressionFailureConsolationPoints} mutation points";
            if (level >= GameBalance.OntogenicRegressionMaxLevel)
            {
                summary += $"; rolls twice with {GameBalance.OntogenicRegressionMaxLevelTier6Bias * 100f:0.00}% Tier 6 bias when both tiers are valid";
            }

            return summary;
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
            {
                highlightOutline.enabled = on;
                if (on)
                {
                    highlightOutline.effectColor = MutationTreeColors.PrerequisiteBorder;
                    highlightOutline.effectDistance = new Vector2(3f, -3f);
                }
            }

            if (!on)
                return;

            ApplyHighlightCardVisual();
        }

        /// <summary>
        /// Adds a distinct outer pulse to an unmet prerequisite that can be bought
        /// immediately. This is deliberately separate from the static amber
        /// prerequisite outline so relationship context remains readable.
        /// </summary>
        public void SetPurchasablePrerequisitePulse(bool on)
        {
            isPurchasablePrerequisitePulseActive = on;
            if (on)
            {
                EnsurePurchasablePrerequisitePulseOutline();
                UpdatePurchasablePrerequisitePulse();
                return;
            }

            if (purchasablePrerequisitePulseOutline != null)
            {
                purchasablePrerequisitePulseOutline.enabled = false;
            }
        }

        public void SetInspectedHighlight(bool on)
        {
            if (highlightOutline == null)
            {
                return;
            }

            highlightOutline.enabled = on;
            highlightOutline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.Text.Primary, 0.98f);
            highlightOutline.effectDistance = on ? new Vector2(3f, -3f) : DefaultHighlightEffectDistance;
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
            SetPurchasablePrerequisitePulse(false);
            SetPrerequisiteHighlight(false);
            SetDependentHighlight(false);
            if (highlightOutline != null)
            {
                highlightOutline.effectDistance = DefaultHighlightEffectDistance;
                highlightOutline.enabled = false;
            }

            UpdateDisplay();
        }

        public bool MatchesSearch(string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return true;
            }

            MutationDescriptionSections sections = mutation.DescriptionSections;
            string searchableText = string.Join(" ",
                mutation.Name,
                MutationCategoryPresentationCatalog.Get(mutation.Category).DisplayName,
                sections.Summary,
                sections.TechnicalDetails,
                sections.MaxLevelBonus,
                string.Join(" ", sections.BuffingMutations));
            return searchableText.IndexOf(normalizedQuery, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void SetSearchMatchState(bool searchActive, bool matches)
        {
            isSearchActive = searchActive;
            isSearchMatch = matches;
            ApplySearchVisual();
        }

        public void SetRelationshipContextState(bool contextActive, bool isRelated)
        {
            isRelationshipContextActive = contextActive;
            isRelationshipRelated = isRelated;
            ApplyEmphasisAlpha();
        }

        private void ApplySearchVisual()
        {
            if (searchMatchOutline != null)
            {
                searchMatchOutline.enabled = isSearchActive && isSearchMatch;
            }

            ApplyEmphasisAlpha();
        }

        private void ApplyEmphasisAlpha()
        {
            if (canvasGroup == null)
            {
                return;
            }

            if (isSearchActive)
            {
                canvasGroup.alpha = isSearchMatch
                    ? 1f
                    : Mathf.Min(baseCanvasAlpha, SearchNonMatchAlpha);
                return;
            }

            canvasGroup.alpha = isRelationshipContextActive && !isRelationshipRelated
                ? Mathf.Min(baseCanvasAlpha, UnrelatedRelationshipAlpha)
                : baseCanvasAlpha;
        }

        public void UpdateInteractable()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int upgradeCost = player.GetMutationPointCost(mutation);
            bool canAfford = player.MutationPoints >= upgradeCost;
            bool isLocked = !MutationPrerequisiteEvaluator.AreAllMet(mutation, player);
            // Check for pending unlock state (only for non-root mutations)
            bool showPendingUnlock = MutationPrerequisiteEvaluator.HasRequirements(mutation)
                && player.PlayerMutations.TryGetValue(mutation.Id, out var pm)
                && pm.PrereqMetRound.HasValue
                && pm.PrereqMetRound.Value == GameManager.Instance.Board.CurrentRound;
            bool isMaxed = currentLevel >= mutation.MaxLevel;
            bool isDisabledBecauseNoEffect = ShouldShowNoEffectDisabledState(isLocked, isSurgeActive, showPendingUnlock, isMaxed);
            bool interactable = !isLocked && canAfford && !isMaxed && !showPendingUnlock;
            if (isSurge && isSurgeActive)
                interactable = false;
            if (isDisabledBecauseNoEffect)
                interactable = false;
            upgradeButton.interactable = interactable;
        }

        private bool ShouldShowNoEffectDisabledState(bool isLocked, bool isSurgeActive, bool showPendingUnlock, bool isMaxed)
        {
            if (mutation == null || uiManager == null || player == null)
                return false;

            if (mutation.Id != MutationIds.MimeticResilience && mutation.Id != MutationIds.CompetitiveAntagonism)
                return false;

            return !isLocked
                && !isSurgeActive
                && !showPendingUnlock
                && !isMaxed
                && uiManager.IsMutationDisabledBecauseNoEffect(mutation, player);
        }

        private string GetNoEffectDisabledReasonText()
        {
            return mutation.Id switch
            {
                MutationIds.MimeticResilience => "No rival currently meets Mimetic Resilience's living-cell and board-control thresholds.",
                MutationIds.CompetitiveAntagonism => "No rival currently has more living cells than you, so Competitive Antagonism would have no effect.",
                _ => "This mutation is disabled right now because it would have no effect."
            };
        }

        private void ApplyDisabledNoEffectOutline(bool isDisabledBecauseNoEffect)
        {
            if (highlightOutline == null)
                return;

            if (isDisabledBecauseNoEffect)
            {
                highlightOutline.enabled = true;
                highlightOutline.effectColor = MutationTreeColors.WarningOutline;
                highlightOutline.effectDistance = new Vector2(2.4f, -2.4f);
                return;
            }

            highlightOutline.effectDistance = DefaultHighlightEffectDistance;
            highlightOutline.enabled = false;
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
                if (existing == highlightOutline) continue;
                nodeStateBorder = existing;
                return;
            }

            nodeStateBorder = target.AddComponent<Outline>();
            nodeStateBorder.effectColor = UIStyleTokens.WithAlpha(MutationTreeColors.SecondaryText, 0.45f);
            nodeStateBorder.effectDistance = DefaultHighlightEffectDistance;
        }

        private void EnsureSearchMatchOutline()
        {
            if (searchMatchOutline != null)
                return;

            GameObject target = upgradeButton != null ? upgradeButton.gameObject : gameObject;
            searchMatchOutline = target.AddComponent<Outline>();
            searchMatchOutline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, 1f);
            searchMatchOutline.effectDistance = new Vector2(3.5f, -3.5f);
            searchMatchOutline.enabled = false;
        }

        private void EnsurePurchasablePrerequisitePulseOutline()
        {
            if (purchasablePrerequisitePulseOutline != null)
            {
                return;
            }

            GameObject target = upgradeButton != null ? upgradeButton.gameObject : gameObject;
            purchasablePrerequisitePulseOutline = target.AddComponent<Outline>();
            purchasablePrerequisitePulseOutline.enabled = false;
        }

        private void UpdatePurchasablePrerequisitePulse()
        {
            if (!isPurchasablePrerequisitePulseActive)
            {
                return;
            }

            EnsurePurchasablePrerequisitePulseOutline();
            float phase = (Mathf.Sin((Time.unscaledTime / PurchasablePrerequisitePulseDurationSeconds) * Mathf.PI * 2f) + 1f) * 0.5f;
            Color color = MutationTreeColors.PurchasablePrerequisitePulse;
            color.a = Mathf.Lerp(0.38f, 1f, phase);
            purchasablePrerequisitePulseOutline.effectColor = color;
            purchasablePrerequisitePulseOutline.effectDistance = new Vector2(
                Mathf.Lerp(4f, 6f, phase),
                -Mathf.Lerp(4f, 6f, phase));
            purchasablePrerequisitePulseOutline.enabled = true;
        }

        private void ConfigureNodeButtonPresentation()
        {
            if (upgradeButton == null)
                return;

            // MutationNodeUI owns every card-state and hover color. Letting Selectable
            // tint the same Image creates a second, stateful color layer that can wash
            // locked cards toward the prefab's light disabled color.
            upgradeButton.transition = Selectable.Transition.None;
            if (nodeBackground != null)
            {
                nodeBackground.CrossFadeColor(Color.white, 0f, true, true);
            }
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
                    MutationTreeColors.GetOwnedNodeBG(mutation.Category),
                    MutationTreeColors.GetCategoryAccent(mutation.Category),
                    0.28f);
                highlightedBackground.a = 1f;
                nodeBackground.color = highlightedBackground;
            }

            if (canvasGroup != null)
            {
                baseCanvasAlpha = 1f;
                ApplyEmphasisAlpha();
            }

            ApplyTextContrast(useDarkText: false);
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
            badgeBG.raycastTarget = false;

            var badgeRect = badgeGO.GetComponent<RectTransform>();
            // Bottom-center of the card, just above the progress bar
            badgeRect.anchorMin = new Vector2(0.5f, 0);
            badgeRect.anchorMax = new Vector2(0.5f, 0);
            badgeRect.pivot = new Vector2(0.5f, 0);
            badgeRect.anchoredPosition = new Vector2(0, 8);
            badgeRect.sizeDelta = new Vector2(MaxBadgeWidth, MaxBadgeHeight);

            var textGO = new GameObject("MaxText");
            textGO.transform.SetParent(badgeGO.transform, false);
            var maxText = textGO.AddComponent<TextMeshProUGUI>();
            maxText.text = "MAX";
            maxText.fontSize = UIStyleTokens.Typography.MicroMinimum;
            maxText.fontStyle = FontStyles.Bold;
            maxText.color = UIStyleTokens.Text.OnAccent;
            maxText.alignment = TextAlignmentOptions.Center;
            maxText.raycastTarget = false;
            if (levelText != null)
                maxText.font = levelText.font;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            maxBadge = badgeGO;
            maxBadge.SetActive(false);
        }

        private void EnsurePurchasedGrowthMark()
        {
            if (purchasedGrowthMark != null || upgradeButton == null)
            {
                return;
            }

            var markObject = new GameObject("PurchasedHyphaMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            markObject.layer = gameObject.layer;
            markObject.transform.SetParent(upgradeButton.transform, false);
            RectTransform markRect = markObject.GetComponent<RectTransform>();
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.zero;
            markRect.pivot = Vector2.zero;
            markRect.anchoredPosition = new Vector2(7f, 4f);
            markRect.sizeDelta = new Vector2(38f, 15f);

            purchasedGrowthMark = markObject.GetComponent<TextMeshProUGUI>();
            purchasedGrowthMark.font = mutationNameText != null ? mutationNameText.font : null;
            purchasedGrowthMark.text = "·•·";
            purchasedGrowthMark.fontSize = UIStyleTokens.Typography.MicroMinimum;
            purchasedGrowthMark.fontStyle = FontStyles.Bold;
            purchasedGrowthMark.color = MutationTreeColors.GetCategoryAccent(mutation.Category);
            purchasedGrowthMark.alignment = TextAlignmentOptions.BottomLeft;
            purchasedGrowthMark.raycastTarget = false;
            purchasedGrowthMark.gameObject.SetActive(false);
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
            groupRect.sizeDelta = new Vector2(
                groupRect.sizeDelta.x,
                Mathf.Max(groupRect.sizeDelta.y, UIStyleTokens.Interaction.MinimumTargetSize));

            // Turn on childControlWidth so the layout group sizes each child
            // to its preferred width (TMP text reports actual text width,
            // Image reports sprite native size).
            var layoutGroup = upgradeCostGroup.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                int horizontalPadding = Mathf.RoundToInt(UpgradeCostPaddingH * 0.5f);
                layoutGroup.padding.left = horizontalPadding;
                layoutGroup.padding.right = horizontalPadding;
                layoutGroup.spacing = UpgradeCostSpacing;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
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
                iconLayout.preferredHeight = UpgradeCostIconWidth;
                iconLayout.minHeight = UpgradeCostIconWidth;
            }

            upgradeCostText.fontSize = UIStyleTokens.Typography.CaptionMinimum;
            upgradeCostText.fontStyle = FontStyles.Bold;
            upgradeCostText.enableAutoSizing = false;
            upgradeCostText.textWrappingMode = TextWrappingModes.NoWrap;
            upgradeCostText.raycastTarget = false;

            var textLayout = upgradeCostText.GetComponent<LayoutElement>();
            if (textLayout == null)
                textLayout = upgradeCostText.gameObject.AddComponent<LayoutElement>();
            textLayout.minWidth = UpgradeCostMinTextWidth;
            textLayout.preferredWidth = Mathf.Max(UpgradeCostMinTextWidth, upgradeCostText.preferredWidth);
            textLayout.minHeight = UpgradeCostIconWidth;
            textLayout.preferredHeight = UpgradeCostIconWidth;

            // Auto-size the group container to fit icon + spacing + text + padding.
            var fitter = upgradeCostGroup.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = upgradeCostGroup.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);
        }

        public void SetCompactLayout(string displayName)
        {
            if (mutationNameText != null && !string.IsNullOrWhiteSpace(displayName))
            {
                mutationNameText.text = displayName;
            }

            if (upgradeButton != null && upgradeButton.transform is RectTransform buttonRect)
            {
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(0f, CompactNodeHeight);
            }

            if (lockOverlay != null && lockOverlay.transform is RectTransform lockRect)
            {
                ConfigureCompactStatusIndicator(lockRect);
            }

            if (pendingUnlockOverlay != null && pendingUnlockOverlay.transform is RectTransform pendingRect)
            {
                ConfigureCompactStatusIndicator(pendingRect);
            }

            if (surgeActiveOverlay != null && surgeActiveOverlay.transform is RectTransform surgeRect)
            {
                ConfigureCompactStatusIndicator(surgeRect);
            }
        }

        private static void ConfigureCompactStatusIndicator(RectTransform indicatorRect)
        {
            indicatorRect.anchoredPosition = new Vector2(-29f, -16f);
            indicatorRect.sizeDelta = new Vector2(30f, 30f);
        }

        private void ConfigureMutationNameFit()
        {
            if (mutationNameText == null)
                return;

            var textContainer = mutationNameText.transform.parent as RectTransform;
            if (textContainer != null)
            {
                textContainer.anchorMin = Vector2.zero;
                textContainer.anchorMax = Vector2.one;
                textContainer.offsetMin = new Vector2(MutationNameHorizontalPadding, NodeTextBottomPadding);
                textContainer.offsetMax = new Vector2(-MutationNameHorizontalPadding, -NodeTextTopPadding);

                var layoutGroup = textContainer.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.padding.top = 0;
                    layoutGroup.padding.bottom = 0;
                    layoutGroup.spacing = NodeTextSpacing;
                    layoutGroup.childForceExpandHeight = false;
                }

                var fitter = textContainer.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }

            float targetSize = Mathf.Max(mutationNameText.fontSize, UIStyleTokens.Typography.CaptionMinimum);
            mutationNameText.enableAutoSizing = true;
            mutationNameText.textWrappingMode = TextWrappingModes.Normal;
            mutationNameText.overflowMode = TextOverflowModes.Truncate;
            mutationNameText.fontSizeMax = targetSize;
            mutationNameText.fontSizeMin = Mathf.Min(targetSize, MutationNameMinimumFontSize);
            mutationNameText.maxVisibleLines = 2;

            var nameLayout = mutationNameText.GetComponent<LayoutElement>();
            if (nameLayout == null)
                nameLayout = mutationNameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.minHeight = NodeNameTextHeight;
            nameLayout.preferredHeight = NodeNameTextHeight;
            nameLayout.flexibleHeight = 0f;
        }

        private void ConfigureStateTextFit()
        {
            if (levelText == null)
                return;

            float targetSize = Mathf.Max(levelText.fontSize, UIStyleTokens.Typography.CaptionMinimum);
            levelText.enableAutoSizing = true;
            levelText.textWrappingMode = TextWrappingModes.Normal;
            levelText.overflowMode = TextOverflowModes.Overflow;
            levelText.fontSizeMax = targetSize;
            levelText.fontSizeMin = Mathf.Min(targetSize, UIStyleTokens.Typography.MicroMinimum);
            levelText.fontStyle = FontStyles.Bold;

            var stateLayout = levelText.GetComponent<LayoutElement>();
            if (stateLayout == null)
                stateLayout = levelText.gameObject.AddComponent<LayoutElement>();
            stateLayout.minHeight = NodeStateTextHeight;
            stateLayout.preferredHeight = NodeStateTextHeight;
            stateLayout.flexibleHeight = 0f;
        }

        private static void ConfigureStatusIndicator(GameObject indicator)
        {
            if (indicator == null)
                return;

            if (indicator.transform is RectTransform indicatorRect)
            {
                indicatorRect.anchoredPosition = StatusIndicatorOffset;
                indicatorRect.sizeDelta = Vector2.one * UIStyleTokens.Interaction.MinimumTargetSize;
            }

            var graphics = indicator.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        private void EnsureBlockedInvestmentClickForwarder()
        {
            if (upgradeButton == null)
                return;

            if (blockedInvestmentClickForwarder == null)
            {
                GameObject buttonObject = upgradeButton.gameObject;
                blockedInvestmentClickForwarder = buttonObject.GetComponent<BlockedInvestmentClickForwarder>()
                    ?? buttonObject.AddComponent<BlockedInvestmentClickForwarder>();
            }

            blockedInvestmentClickForwarder.Bind(HandleForwardedClick);
        }

        /// <summary>
        /// Sits on the upgrade Button GameObject and relays left clicks up to
        /// <see cref="MutationNodeUI"/>. The Button (a Selectable) consumes the pointer
        /// press before it can reach the card-root component, so clicks on a
        /// non-interactable node would otherwise be invisible to the node script.
        /// </summary>
        private sealed class BlockedInvestmentClickForwarder : MonoBehaviour, IPointerClickHandler
        {
            private System.Action onLeftClick;

            public void Bind(System.Action handler)
            {
                onLeftClick = handler;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    onLeftClick?.Invoke();
                }
            }
        }

    }
}
