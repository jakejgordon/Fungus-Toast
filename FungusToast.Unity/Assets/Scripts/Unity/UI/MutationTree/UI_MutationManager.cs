using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.Tilemaps;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.Tooltips;
using System.Linq;
using FungusToast.Core.Metrics;

namespace FungusToast.Unity.UI.MutationTree
{
    public class UI_MutationManager : MonoBehaviour
    {
        [Header("General UI References")]
        [SerializeField] private MutationManager mutationManager;
        [SerializeField] private GameObject mutationTreePanel;
        [SerializeField] private Button spendPointsButton;
        [SerializeField] private TextMeshProUGUI spendPointsButtonText;
        [SerializeField] private Outline buttonOutline;

        [Header("Mold Icon Display")]
        [SerializeField] private Image playerMoldIcon;
        [SerializeField] private GridVisualizer gridVisualizer;

        [Header("Mutation Tree Dynamic UI")]
        [SerializeField] private MutationTreeBuilder mutationTreeBuilder;

        [Header("Dock")]
        [SerializeField] private TextMeshProUGUI dockButtonText;

        [Header("UI Wiring")]
        [SerializeField] private TextMeshProUGUI mutationPointsCounterText;
        [SerializeField] private Button storePointsButton;

        [Header("Tree Sliding Settings")]
        public float slideDuration = 0.5f;
        public Vector2 hiddenPosition = new Vector2(-1920, 0);
        public Vector2 visiblePosition = new Vector2(0, 0);

        [Header("Pulse Settings")]
        public float pulseStrength = 0.05f;
        public float pulseSpeed = 2f;

        [Header("Shimmer Settings")]
        [Tooltip("Stagger delay between each node shimmer when panel opens")]
        public float shimmerStaggerDelay = 0.03f;

        private RectTransform mutationTreeRect;
        private Vector3 originalButtonScale;
        private Vector3 originalCounterScale;
        private bool isTreeOpen = false;
        private bool isSliding = false;

        private Player humanPlayer;
        private bool humanTurnEnded = false;
        private List<MutationNodeUI> mutationButtons = new();
        private Dictionary<int, List<int>> directDependentsByMutationId = new();

        public bool IsTreeOpen => isTreeOpen;
        public RectTransform MutationTreeRect => mutationTreeRect;
        public Transform MutationTreeTransform => mutationTreePanel != null ? mutationTreePanel.transform : transform;

        private void Awake()
        {
            if (mutationTreePanel != null)
                mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
            else
                Debug.LogError("mutationTreePanel is NULL at Awake()!");
        }

        private void Start()
        {
            storePointsButton.onClick.AddListener(OnStoreMutationPointsClicked);
            RefreshSpendPointsButtonUI();
            originalButtonScale = spendPointsButton.transform.localScale;
            if (mutationPointsCounterText != null)
                originalCounterScale = mutationPointsCounterText.transform.localScale;

            spendPointsButton.onClick.AddListener(OnSpendPointsClicked);

            // ── Apply the dark panel theme to all backgrounds ──
            ApplyPanelTheme();

            ApplyActionStyles();

            // ── Store Points button tooltip ──
            WireStorePointsTooltip();
        }

        private void ApplyActionStyles()
        {
            UIStyleTokens.Button.ApplyStyle(spendPointsButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.SetButtonLabelColor(spendPointsButton, UIStyleTokens.Button.TextDefault);

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.color = MutationTreeColors.PrimaryText;
            }
        }

        private void Update()
        {
            if (humanPlayer != null && humanPlayer.MutationPoints > 0)
                AnimatePulse();
            else
                ResetPulse();
        }

        /// <summary>
        /// Resets transient UI/runtime state between games so stale coroutines and flags
        /// cannot lock spend-point interactions in the next level.
        /// </summary>
        public void ResetForNewGameState()
        {
            StopAllCoroutines();

            isTreeOpen = false;
            isSliding = false;
            humanTurnEnded = false;

            if (mutationTreePanel != null)
            {
                mutationTreePanel.SetActive(false);
            }

            if (mutationTreeRect != null)
            {
                mutationTreeRect.anchoredPosition = hiddenPosition;
            }

            if (dockButtonText != null)
            {
                dockButtonText.text = ">";
            }

            if (spendPointsButton != null)
            {
                spendPointsButton.interactable = false;
            }
        }

        public void Initialize(Player player)
        {
            if (mutationTreeRect == null && mutationTreePanel != null)
                mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();

            if (gridVisualizer == null)
            {
                Debug.LogError("❌ GridVisualizer is not assigned to MutationUIManager!");
                return;
            }

            if (player == null)
            {
                Debug.LogError("❌ Player passed to Initialize() is null!");
                return;
            }

            // Do NOT reset humanTurnEnded here; only do so at the true start of a new mutation phase

            humanPlayer = player;
            RefreshSpendPointsButtonUI();

            Tile tile = gridVisualizer.GetTileForPlayer(player.PlayerId);
            if (tile != null && tile.sprite != null)
            {
                playerMoldIcon.sprite = tile.sprite;
                playerMoldIcon.enabled = true;
            }
            else
            {
                Debug.LogWarning("Player tile or tile sprite is null.");
                playerMoldIcon.enabled = false;
            }

            PopulateAllMutations();
            
            // Final safety check before starting coroutine
            if (gameObject.activeInHierarchy && enabled)
            {
                try
                {
                    StartCoroutine(SlideOutTree());
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Failed to start SlideOutTree coroutine: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ UI_MutationManager inactive, skipping SlideOutTree coroutine");
            }
        }

        // Call this ONLY at the true start of a new mutation phase
        public void StartNewMutationPhase()
        {
            humanTurnEnded = false;
        }

        public void OnSpendPointsClicked()
        {
            if (isSliding) return;

            // Initialize if not already done
            if (humanPlayer == null && GameManager.Instance != null && GameManager.Instance.Board.Players.Count > 0)
            {
                Initialize(GameManager.Instance.Board.Players[0]);
            }

            if (!isTreeOpen)
                StartCoroutine(SlideInTree());
            else
                StartCoroutine(SlideOutTree());
        }

        public void SetSpendPointsButtonVisible(bool visible)
        {
            if (spendPointsButton != null)
                spendPointsButton.gameObject.SetActive(visible);
        }

        public void PopulateAllMutations()
        {
            if (humanPlayer == null || mutationTreeBuilder == null || mutationManager == null)
            {
                Debug.LogError("❌ Cannot build mutation tree — missing references.");
                return;
            }

            var mutations = mutationManager.GetAllMutations().ToList();
            BuildDirectDependentLookup(mutations);
            var layout = UI_MutationLayoutProvider.GetDefaultLayout();

            mutationButtons.Clear(); // reset in case we're rebuilding
            mutationButtons = mutationTreeBuilder.BuildTree(mutations, layout, humanPlayer, this);
        }

        public bool TryUpgradeMutation(Mutation mutation)
        {
            int currentRound = GameManager.Instance.Board.CurrentRound;
            
            // Get the observer through GameManager's GameUI.GameLogRouter
            var observer = GameManager.Instance.GameUI.GameLogRouter;
            
            if (humanPlayer.TryUpgradeMutation(mutation, observer, currentRound))
            {
                RefreshSpendPointsButtonUI();
                RefreshAllMutationButtons(); // <-- Ensures hourglass overlays update
                TryEndHumanTurn();
                return true;
            }

            Debug.LogWarning($"⚠️ Player {humanPlayer.PlayerId} failed to upgrade {mutation.Name}");
            return false;
        }

        public void RefreshSpendPointsButtonUI()
        {
            if (spendPointsButton == null || buttonOutline == null || humanPlayer == null)
                return;

            int points = humanPlayer.MutationPoints;
            if (points > 0)
            {
                spendPointsButton.interactable = true;
                spendPointsButtonText.text = $"Spend {points} Points!";
                buttonOutline.enabled = true;
            }
            else
            {
                spendPointsButton.interactable = false;
                spendPointsButtonText.text = "No Points Available";
                buttonOutline.enabled = false;
            }

            if (mutationPointsCounterText != null)
                mutationPointsCounterText.text = $"Mutation Points: {points}";
        }

        private void AnimatePulse()
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed);
            float scale = 1f + pulse * pulseStrength;

            if (spendPointsButton != null)
                spendPointsButton.transform.localScale = originalButtonScale * scale;

            if (mutationPointsCounterText != null)
                mutationPointsCounterText.transform.localScale = originalCounterScale * scale;

            if (buttonOutline != null)
            {
                Color baseColor = MutationTreeColors.PulseOutline;
                float normalizedPulse = (pulse + 1f) / 2f;
                float alpha = Mathf.Lerp(0.5f, 1f, normalizedPulse);
                buttonOutline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
        }

        private void ResetPulse()
        {
            if (spendPointsButton != null)
                spendPointsButton.transform.localScale = originalButtonScale;

            if (mutationPointsCounterText != null)
                mutationPointsCounterText.transform.localScale = originalCounterScale;
        }

        public void TogglePanelDock()
        {
            if (isTreeOpen)
                StartCoroutine(SlideOutTree());
            else
                StartCoroutine(SlideInTree());
        }



        private IEnumerator SlideInTree()
        {
            isSliding = true;

            mutationTreePanel.SetActive(true);
            isTreeOpen = true;

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, visiblePosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = visiblePosition;

            if (dockButtonText != null)
                dockButtonText.text = "<";

            isSliding = false;

            // ── Play shimmer on affordable nodes after panel opens ──
            if (humanPlayer != null && humanPlayer.MutationPoints > 0)
                StartCoroutine(PlayAffordableShimmer());
        }

        private IEnumerator SlideOutTree()
        {
            isSliding = true;

            isTreeOpen = false;

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, hiddenPosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = hiddenPosition;
            mutationTreePanel.SetActive(false);

            if (dockButtonText != null)
                dockButtonText.text = ">";

            isSliding = false;
        }

        private void TryEndHumanTurn()
        {
            if (!humanTurnEnded && humanPlayer != null && humanPlayer.MutationPoints <= 0)
            {
                EndHumanMutationPhase();
            }
        }

        private IEnumerator ClosePanelThenTriggerAI()
        {
            if (isTreeOpen)
                yield return StartCoroutine(SlideOutTree());
            // Multi-human: now signal GameManager to advance to next human
            if (GameManager.Instance != null && GameManager.Instance.ConfiguredHumanPlayerCount > 1)
            {
                GameManager.Instance.OnHumanMutationTurnFinished(humanPlayer);
                yield break;
            }
            // Single human: go straight to AI spending
            GameManager.Instance.SpendAllMutationPointsForAIPlayers();
        }

        public void RefreshAllMutationButtons()
        {
            foreach (var button in mutationButtons)
            {
                button.UpdateDisplay();
            }

            // Also refresh category investment summaries
            if (mutationTreeBuilder != null && humanPlayer != null)
                mutationTreeBuilder.UpdateCategoryInvestmentSummaries(mutationButtons, humanPlayer);
        }

        public Mutation GetMutationById(int id)
        {
            return mutationManager?.GetMutationById(id);
        }

        private void OnStoreMutationPointsClicked()
        {
            if (humanPlayer != null)
            {
                humanPlayer.WantsToBankPointsThisTurn = true;
                EndHumanMutationPhase();
            }
        }

        private void EndHumanMutationPhase()
        {
            humanTurnEnded = true;
            SetSpendPointsButtonInteractable(false);
            // Defer notifying GameManager until UI is closed so prompt shows cleanly
            StartCoroutine(ClosePanelThenTriggerAI());
        }

        public void DisableAllMutationButtons()
        {
            foreach (var btn in mutationButtons)
                btn.DisableUpgrade();
        }

        public void SetSpendPointsButtonInteractable(bool interactable)
        {
            if (spendPointsButton != null)
                spendPointsButton.interactable = interactable;
        }

        // Highlights unmet prerequisite nodes for a hovered mutation
        public void HighlightUnmetPrerequisites(Mutation mutation, Player player)
        {
            // First, clear any previous highlights
            ClearAllHighlights();
            foreach (var prereq in mutation.Prerequisites)
            {
                int ownedLevel = player.GetMutationLevel(prereq.MutationId);
                if (ownedLevel < prereq.RequiredLevel)
                {
                    var node = mutationButtons.FirstOrDefault(n => n.MutationId == prereq.MutationId);
                    if (node != null)
                        node.SetPrerequisiteHighlight(true);
                }
            }
        }

        // Highlights direct dependent nodes for a hovered, currently unlocked mutation.
        public void HighlightDirectDependents(Mutation mutation)
        {
            ClearAllHighlights();
            if (mutation == null) return;

            if (!directDependentsByMutationId.TryGetValue(mutation.Id, out var dependentIds))
                return;

            foreach (var dependentId in dependentIds)
            {
                var node = mutationButtons.FirstOrDefault(n => n.MutationId == dependentId);
                if (node != null)
                    node.SetDependentHighlight(true);
            }
        }

        // Clears all node highlights
        public void ClearAllHighlights()
        {
            foreach (var node in mutationButtons)
                node.ClearHighlights();
        }

        private void BuildDirectDependentLookup(List<Mutation> mutations)
        {
            directDependentsByMutationId = new Dictionary<int, List<int>>();
            if (mutations == null) return;

            foreach (var mutation in mutations)
            {
                foreach (var prereq in mutation.Prerequisites)
                {
                    if (!directDependentsByMutationId.TryGetValue(prereq.MutationId, out var dependents))
                    {
                        dependents = new List<int>();
                        directDependentsByMutationId[prereq.MutationId] = dependents;
                    }

                    if (!dependents.Contains(mutation.Id))
                        dependents.Add(mutation.Id);
                }
            }
        }

        public void UpdateAllMutationNodeInteractables()
        {
            foreach (var node in mutationButtons)
            {
                node.UpdateInteractable();
            }
        }

        public void ReinitializeForPlayer(Player player, bool keepPanelClosed = true)
        {
            if (player == null)
            {
                Debug.LogError("❌ ReinitializeForPlayer received null player");
                return;
            }
            Debug.Log($"[UI_MutationManager] ReinitializeForPlayer playerId={player.PlayerId} name={player.PlayerName} mp={player.MutationPoints}");
            humanPlayer = player;
            // Ensure spend button exists
            if (spendPointsButton == null)
                Debug.LogError("[UI_MutationManager] spendPointsButton missing");

            RefreshSpendPointsButtonUI();

            // Update icon
            if (gridVisualizer != null)
            {
                Tile tile = gridVisualizer.GetTileForPlayer(player.PlayerId);
                if (tile != null && tile.sprite != null)
                {
                    playerMoldIcon.sprite = tile.sprite;
                    playerMoldIcon.enabled = true;
                }
                else
                {
                    playerMoldIcon.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("[UI_MutationManager] GridVisualizer null during ReinitializeForPlayer");
            }

            PopulateAllMutations();
            // Force enable controls regardless of previous turn state
            SetSpendPointsButtonInteractable(true);
            if (buttonOutline != null) buttonOutline.enabled = player.MutationPoints > 0;
            if (spendPointsButtonText != null)
            {
                spendPointsButtonText.text = player.MutationPoints > 0 ? $"Spend {player.MutationPoints} Points!" : "No Points Available";
            }
            if (mutationPointsCounterText != null)
                mutationPointsCounterText.text = $"Mutation Points: {player.MutationPoints}";

            Debug.Log($"[UI_MutationManager] After force-enable interactable={spendPointsButton?.interactable} text='{spendPointsButtonText?.text}'");

            if (!keepPanelClosed && !isTreeOpen && gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(SlideOutTree()); // maintain original closed presentation
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Projected cost display on hover
        // ═══════════════════════════════════════════════════════════════

        private string basePointsText;

        /// <summary>
        /// Shows a projected "→ N" next to the mutation points counter when hovering a node.
        /// </summary>
        public void ShowProjectedCost(int cost)
        {
            if (mutationPointsCounterText == null || humanPlayer == null) return;
            int current = humanPlayer.MutationPoints;
            int projected = Mathf.Max(0, current - cost);
            basePointsText ??= mutationPointsCounterText.text;
            mutationPointsCounterText.text = $"Mutation Points: {current}  <color=#AAAAAA>→ {projected}</color>";
        }

        /// <summary>Restores normal points counter text.</summary>
        public void ClearProjectedCost()
        {
            if (mutationPointsCounterText == null || humanPlayer == null) return;
            mutationPointsCounterText.text = $"Mutation Points: {humanPlayer.MutationPoints}";
            basePointsText = null;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Affordable node shimmer on panel open
        // ═══════════════════════════════════════════════════════════════

        private IEnumerator PlayAffordableShimmer()
        {
            foreach (var node in mutationButtons)
            {
                if (node.IsAffordableAndAvailable())
                {
                    StartCoroutine(node.PlayShimmer());
                    yield return new WaitForSeconds(shimmerStaggerDelay);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Store Points button tooltip
        // ═══════════════════════════════════════════════════════════════

        private void WireStorePointsTooltip()
        {
            if (storePointsButton == null) return;

            // ── Make the button clearly visible against the dark panel ──
            StyleStorePointsButton();

            var trigger = storePointsButton.GetComponent<TooltipTrigger>();
            if (trigger == null)
                trigger = storePointsButton.gameObject.AddComponent<TooltipTrigger>();

            trigger.SetStaticText("Store your unspent mutation points.\nThey will carry over to the next turn,\nallowing you to save up for expensive upgrades.");
        }

        /// <summary>
        /// Styles the Store Points button with explicit colors and a storage icon
        /// so it stands out against the dark panel header.
        /// </summary>
        private void StyleStorePointsButton()
        {
            UIStyleTokens.Button.ApplyStyle(storePointsButton);
            UIStyleTokens.Button.SetButtonLabelColor(storePointsButton, UIStyleTokens.Button.TextDefault);

            // Button background — high contrast against top bar so it reads as interactive
            var btnImage = storePointsButton.GetComponent<Image>();
            if (btnImage != null)
            {
                Color ctaBase = Color.Lerp(MutationTreeColors.ButtonHighlight, MutationTreeColors.PulseOutline, 0.18f);
                btnImage.color = ctaBase;
                btnImage.raycastTarget = true;

                var border = storePointsButton.GetComponent<Outline>();
                if (border == null)
                    border = storePointsButton.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(
                    MutationTreeColors.PulseOutline.r,
                    MutationTreeColors.PulseOutline.g,
                    MutationTreeColors.PulseOutline.b,
                    0.65f);
                border.effectDistance = new Vector2(1f, -1f);
            }

            // Ensure minimum clickable footprint (44px+ recommended touch target)
            var layout = storePointsButton.GetComponent<LayoutElement>();
            if (layout == null)
                layout = storePointsButton.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(layout.minHeight, 36f);
            layout.minWidth = Mathf.Max(layout.minWidth, 220f);

            // Button color block for hover / press states
            var colors = storePointsButton.colors;
            Color normal = Color.Lerp(MutationTreeColors.ButtonHighlight, MutationTreeColors.PulseOutline, 0.18f);
            colors.normalColor      = normal;
            colors.highlightedColor = Color.Lerp(normal, MutationTreeColors.PrimaryText, 0.30f);
            colors.pressedColor     = MutationTreeColors.ButtonPressed;
            colors.selectedColor    = colors.highlightedColor;
            colors.disabledColor    = new Color(
                MutationTreeColors.SecondaryText.r,
                MutationTreeColors.SecondaryText.g,
                MutationTreeColors.SecondaryText.b,
                0.45f);
            colors.colorMultiplier  = 1.05f;
            colors.fadeDuration     = 0.08f;
            storePointsButton.transition = Selectable.Transition.ColorTint;
            storePointsButton.colors = colors;

            // Text — bold, bright, and action-oriented
            var btnText = storePointsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.color = MutationTreeColors.PrimaryText;
                btnText.fontStyle = FontStyles.Bold;
                btnText.characterSpacing = 1.2f;
                btnText.margin = new Vector4(10f, 3f, 10f, 3f);

                // Prepend a bank/save icon using TMP rich text (downward arrow into tray)
                if (!btnText.text.StartsWith("<"))
                    btnText.text = "<b>[</b><size=80%>\u2193</size><b>]</b> " + btnText.text;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Panel-wide dark theme
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Walks the mutation tree panel hierarchy and recolors backgrounds,
        /// buttons, and text to match the MutationTreeColors dark theme.
        /// Called once in Start().
        /// </summary>
        private void ApplyPanelTheme()
        {
            if (mutationTreePanel == null) return;

            // Root panel background
            SetImageColor(mutationTreePanel, MutationTreeColors.PanelBG);

            // Walk everything and apply context-aware colors
            ApplyThemeRecursive(mutationTreePanel.transform);
        }

        private void ApplyThemeRecursive(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                string nameLower = child.name.ToLowerInvariant();

                // ── Skip dynamically-created mutation nodes; they theme themselves ──
                if (child.GetComponent<MutationNodeUI>() != null)
                    continue;

                // ── Skip category headers and their children; they have explicit styling ──
                if (nameLower.StartsWith("header"))
                    continue;

                // ── Scroll views / viewports ──
                if (nameLower.Contains("scroll") || nameLower.Contains("viewport"))
                {
                    SetImageColor(child.gameObject, MutationTreeColors.ScrollAreaBG);
                }
                // ── Column containers should be transparent so scroll BG shows ──
                else if (nameLower.Contains("column") || nameLower.Contains("content"))
                {
                    SetImageColor(child.gameObject, Color.clear);
                }
                // ── Dock / side bar ──
                else if (nameLower.Contains("dock"))
                {
                    SetImageColor(child.gameObject, MutationTreeColors.DockBG);
                }
                // ── Top bar / header bar ──
                else if (nameLower.Contains("topbar") || nameLower.Contains("headerbar") || nameLower.Contains("top_bar") || nameLower.Contains("header_bar"))
                {
                    SetImageColor(child.gameObject, MutationTreeColors.TopBarBG);
                }
                // ── Generic panels that look too bright ──
                else
                {
                    var img = child.GetComponent<Image>();
                    if (img != null && IsDefaultOrBrightColor(img.color))
                        img.color = MutationTreeColors.TopBarBG;
                }

                // ── Theme any buttons on this child ──
                var btn = child.GetComponent<Button>();
                if (btn != null)
                    ThemeButton(btn);

                // ── Soften text colors ──
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && IsDefaultOrBrightColor(tmp.color))
                    tmp.color = MutationTreeColors.PrimaryText;

                // Recurse (but skip mutation nodes handled above)
                ApplyThemeRecursive(child);
            }
        }

        private static void ThemeButton(Button btn)
        {
            var colors = btn.colors;
            colors.normalColor      = MutationTreeColors.ButtonNormal;
            colors.highlightedColor = MutationTreeColors.ButtonHighlight;
            colors.pressedColor     = MutationTreeColors.ButtonPressed;
            colors.selectedColor    = MutationTreeColors.ButtonHighlight;
            colors.disabledColor    = new Color(
                MutationTreeColors.ButtonNormal.r * 0.6f,
                MutationTreeColors.ButtonNormal.g * 0.6f,
                MutationTreeColors.ButtonNormal.b * 0.6f, 1f);
            btn.colors = colors;

            // Also theme the button's Image if it's bright
            var img = btn.GetComponent<Image>();
            if (img != null && IsDefaultOrBrightColor(img.color))
                img.color = MutationTreeColors.ButtonNormal;
        }

        private static void SetImageColor(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img != null)
                img.color = color;
        }

        /// <summary>
        /// Returns true when a color looks like Unity's default white/light-gray
        /// or any bright panel background that should be darkened.
        /// </summary>
        private static bool IsDefaultOrBrightColor(Color c)
        {
            // Consider anything with average RGB > 0.55 and meaningful alpha as "bright"
            float avg = (c.r + c.g + c.b) / 3f;
            return avg > 0.55f && c.a > 0.3f;
        }
    }
}
