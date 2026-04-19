using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using UnityEngine.Tilemaps;
using FungusToast.Unity.Grid;
using FungusToast.Unity;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.Tooltips;
using System.Linq;
using FungusToast.Core.Metrics;

namespace FungusToast.Unity.UI.MutationTree
{
    public class UI_MutationManager : MonoBehaviour
    {
        private const string MutationTreeGuidanceSeenKey = "Onboarding.AlphaMutationTreeGuidanceSeen";
        private const string SpendPointsTooltipText = "Open your upgrades and spend your mutation points now.";
        private const string StorePointsTooltipText = "Store your unspent mutation points.\nThey carry over to the next turn,\nso you can save for stronger upgrades.";
        private const string FirstTreeGuidanceToastText = "Hover upgrades to inspect them, then click an affordable one to buy it.\n\nIf you want stronger upgrades later, use Store Mutation Points at the top of this panel.";
        private const float SpendButtonMinWidth = 220f;
        private const float SpendButtonMinHeight = 40f;
        private const float StoreButtonMinWidth = 220f;
        private const float StoreButtonMinHeight = 36f;
        private const float MutationPanelMaxWidth = 1125f;
        private const float MutationPanelTopInsetPadding = 6f;

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
        [SerializeField] private Button dockButton;
        [SerializeField] private TextMeshProUGUI dockButtonText;

        [Header("UI Wiring")]
        [SerializeField] private TextMeshProUGUI mutationPointsCounterText;
        [SerializeField] private Button storePointsButton;
        [SerializeField] private AudioClip mutationUpgradeSuccessClip = null;
        [SerializeField, Range(0f, 1f)] private float mutationUpgradeSuccessVolume = 1f;
        [SerializeField] private AudioClip mutationStorePointsClip = null;
        [SerializeField, Range(0f, 1f)] private float mutationStorePointsVolume = 1f;

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
        private RectTransform parentRectTransform;
        private RectTransform mutationScrollViewRect;
        private RectTransform mutationViewportRect;
        private RectTransform mutationScrollViewContentRect;
        private Canvas rootCanvas;
        private Vector3 originalButtonScale;
        private Vector3 originalCounterScale;
        private bool isTreeOpen = false;
        private bool isSliding = false;
        private bool hasDismissedTreeGuidanceThisGame;
        private TooltipTrigger spendPointsTooltipTrigger;
        private AudioSource soundEffectAudioSource;

        private Player humanPlayer;
        private bool humanTurnEnded = false;
        private List<MutationNodeUI> mutationButtons = new();
        private Dictionary<int, List<int>> directDependentsByMutationId = new();
        private PendingTargetedSurgeSelection pendingTargetedSurgeSelection;
        private Vector2 lastKnownParentSize = new(-1f, -1f);
        private int lastKnownScreenWidth = -1;
        private int lastKnownScreenHeight = -1;

        private sealed class PendingTargetedSurgeSelection
        {
            public PendingTargetedSurgeSelection(Mutation mutation, int reservedCost, int currentRound)
            {
                Mutation = mutation;
                ReservedCost = reservedCost;
                CurrentRound = currentRound;
            }

            public Mutation Mutation { get; }
            public int ReservedCost { get; }
            public int CurrentRound { get; }
        }

        public bool IsTreeOpen => isTreeOpen;
        public RectTransform MutationTreeRect => mutationTreeRect;
        public Transform MutationTreeTransform => mutationTreePanel != null ? mutationTreePanel.transform : transform;

        private void Awake()
        {
            if (mutationTreePanel != null)
            {
                CacheMutationPanelLayoutReferences();
            }
            else
                Debug.LogError("mutationTreePanel is NULL at Awake()!");

            RefreshResponsiveMutationPanelLayout();
            EnsureSoundEffectAudioSource();
        }

        private void OnEnable()
        {
            SetDockButtonVisible(true);
            RefreshResponsiveMutationPanelLayout();
            StartCoroutine(RefreshResponsiveMutationPanelLayoutNextFrame());
        }

        private void OnDisable()
        {
            SetDockButtonVisible(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (mutationTreeRect == null)
            {
                return;
            }

            RefreshResponsiveMutationPanelLayout();
        }

        private void Start()
        {
            storePointsButton.onClick.AddListener(OnStoreMutationPointsClicked);
            RefreshSpendPointsButtonUI();
            CaptureOriginalControlScales();
            if (mutationPointsCounterText != null)
                originalCounterScale = mutationPointsCounterText.transform.localScale;

            spendPointsButton.onClick.AddListener(OnSpendPointsClicked);

            // ── Apply the dark panel theme to all backgrounds ──
            ApplyPanelTheme();

            ApplyActionStyles();
            RestoreActionRowLayout();
            WireSpendPointsTooltip();
            RefreshResponsiveMutationPanelLayout();

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
            RefreshResponsiveMutationPanelLayoutIfNeeded();

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
            CaptureOriginalControlScales();
            RefreshResponsiveMutationPanelLayout();

            isTreeOpen = false;
            isSliding = false;
            humanTurnEnded = false;
            humanPlayer = null;
            mutationButtons.Clear();
            directDependentsByMutationId.Clear();
            pendingTargetedSurgeSelection = null;
            hasDismissedTreeGuidanceThisGame = false;

            if (mutationTreePanel != null)
            {
                mutationTreePanel.SetActive(false);
            }

            if (mutationTreeRect != null)
            {
                mutationTreeRect.anchoredPosition = GetHiddenPosition();
            }

            if (dockButtonText != null)
            {
                dockButtonText.text = ">";
            }

            SetDockButtonVisible(false);

            if (spendPointsButton != null)
            {
                spendPointsButton.gameObject.SetActive(true);
                spendPointsButton.interactable = false;
            }

            if (storePointsButton != null)
            {
                storePointsButton.gameObject.SetActive(true);
                storePointsButton.interactable = false;
            }

            if (spendPointsButtonText != null)
            {
                spendPointsButtonText.text = "No Points Available";
            }

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.text = "Mutation Points: 0";
            }

            ResetPulse();
            RestoreActionRowLayout();
        }

        public void Initialize(Player player)
        {
            if (mutationTreeRect == null && mutationTreePanel != null)
                CacheMutationPanelLayoutReferences();

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

            ConfigurePlayerHoverTargets(player.PlayerId, playerMoldIcon.enabled);

            PopulateAllMutations();
            RefreshResponsiveMutationPanelLayout();
            
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
            if (isSliding || pendingTargetedSurgeSelection != null) return;

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
            {
                spendPointsButton.gameObject.SetActive(visible);
                if (visible)
                {
                    RestoreActionRowLayout();
                }
            }
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
            RefreshResponsiveMutationPanelLayout();
        }

        public void TryUpgradeMutation(Mutation mutation, Action<bool> onResolved)
        {
            if (mutation.Id == MutationIds.ChemotacticBeacon)
            {
                StartCoroutine(ResolveChemotacticBeaconUpgrade(mutation, onResolved));
                return;
            }

            int currentRound = GameManager.Instance.Board.CurrentRound;

            // Get the observer through GameManager's GameUI.GameLogRouter
            var observer = GameManager.Instance.GameUI.GameLogRouter;

            if (humanPlayer.TryUpgradeMutation(mutation, observer, currentRound))
            {
                RefreshSpendPointsButtonUI();
                RefreshAllMutationButtons(); // <-- Ensures hourglass overlays update
                PlayMutationUpgradeSuccessSound();
                TryEndHumanTurn();
                onResolved?.Invoke(true);
                return;
            }

            Debug.LogWarning($"⚠️ Player {humanPlayer.PlayerId} failed to upgrade {mutation.Name}");
            onResolved?.Invoke(false);
        }

        private IEnumerator ResolveChemotacticBeaconUpgrade(Mutation mutation, Action<bool> onResolved)
        {
            var gameManager = GameManager.Instance;
            var board = gameManager?.Board;
            if (gameManager == null || board == null || humanPlayer == null)
            {
                onResolved?.Invoke(false);
                yield break;
            }

            if (pendingTargetedSurgeSelection != null)
            {
                onResolved?.Invoke(false);
                yield break;
            }

            int currentRound = board.CurrentRound;
            if (!humanPlayer.CanUpgrade(mutation, currentRound))
            {
                onResolved?.Invoke(false);
                yield break;
            }

            var validTiles = board.AllTiles()
                .Where(tile => board.IsTileOpenForChemobeacon(tile.TileId))
                .ToList();
            if (validTiles.Count == 0)
            {
                Debug.LogWarning("⚠️ No valid Chemobeacon tiles are available.");
                onResolved?.Invoke(false);
                yield break;
            }

            int reservedCost = humanPlayer.GetMutationPointCost(mutation);
            if (humanPlayer.MutationPoints < reservedCost)
            {
                onResolved?.Invoke(false);
                yield break;
            }

            pendingTargetedSurgeSelection = new PendingTargetedSurgeSelection(mutation, reservedCost, currentRound);
            humanPlayer.MutationPoints -= reservedCost;
            RefreshSpendPointsButtonUI();
            RefreshAllMutationButtons();
            SetMutationChoiceLocked(true);
            ForceCloseTreePanel();

            bool resolved = false;
            bool success = false;
            var observer = gameManager.GameUI.GameLogRouter;

            TileSelectionController.Instance.PromptSelectBoardTile(
                tile => board.IsTileOpenForChemobeacon(tile.TileId),
                tile =>
                {
                    success = humanPlayer.TryActivateReservedTargetedSurge(
                        mutation,
                        board,
                        tile.TileId,
                        observer,
                        currentRound,
                        reservedCost);
                    if (!success)
                    {
                        humanPlayer.MutationPoints += reservedCost;
                    }

                    pendingTargetedSurgeSelection = null;
                    if (success)
                    {
                        var placedTile = board.GetTileById(tile.TileId);
                        if (placedTile != null)
                        {
                            observer.RecordChemobeaconPlacement(humanPlayer.PlayerId, placedTile.X, placedTile.Y);
                        }

                        RefreshSpendPointsButtonUI();
                        RefreshAllMutationButtons();
                        PlayMutationUpgradeSuccessSound();
                        TryEndHumanTurn();
                    }
                    else
                    {
                        RefreshSpendPointsButtonUI();
                        RefreshAllMutationButtons();
                    }

                    SetMutationChoiceLocked(false);

                    resolved = true;
                },
                () =>
                {
                    humanPlayer.MutationPoints += reservedCost;
                    pendingTargetedSurgeSelection = null;
                    RefreshSpendPointsButtonUI();
                    RefreshAllMutationButtons();
                    SetMutationChoiceLocked(false);
                    resolved = true;
                },
                "Select one empty, non-nutrient tile to place your Chemobeacon.",
                showCancelButton: true,
                cancelButtonLabel: "Cancel Beacon"
            );

            while (!resolved)
            {
                yield return null;
            }

            onResolved?.Invoke(success);
        }

        private void EnsureSoundEffectAudioSource()
        {
            if (soundEffectAudioSource != null)
            {
                return;
            }

            soundEffectAudioSource = GetComponent<AudioSource>();
            if (soundEffectAudioSource == null)
            {
                soundEffectAudioSource = gameObject.AddComponent<AudioSource>();
            }

            soundEffectAudioSource.playOnAwake = false;
            soundEffectAudioSource.loop = false;
            soundEffectAudioSource.spatialBlend = 0f;
        }

        private void PlayMutationUpgradeSuccessSound()
        {
            if (mutationUpgradeSuccessClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(mutationUpgradeSuccessVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(mutationUpgradeSuccessClip, effectiveVolume);
        }

        private void PlayMutationStorePointsSound()
        {
            if (mutationStorePointsClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(mutationStorePointsVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(mutationStorePointsClip, effectiveVolume);
        }

        public void RefreshSpendPointsButtonUI()
        {
            if (spendPointsButton == null || buttonOutline == null || humanPlayer == null)
                return;

            if (pendingTargetedSurgeSelection != null)
            {
                spendPointsButton.interactable = false;
                spendPointsButtonText.text = "Select Chemobeacon Tile";
                buttonOutline.enabled = false;

                if (mutationPointsCounterText != null)
                    mutationPointsCounterText.text = $"Mutation Points: {humanPlayer.MutationPoints}";

                return;
            }

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
            CaptureOriginalControlScales();

            if (spendPointsButton != null)
                spendPointsButton.transform.localScale = originalButtonScale;

            if (mutationPointsCounterText != null)
                mutationPointsCounterText.transform.localScale = originalCounterScale;
        }

        public void TogglePanelDock()
        {
            if (!isActiveAndEnabled || mutationTreePanel == null)
            {
                return;
            }

            if (pendingTargetedSurgeSelection != null)
                return;

            if (isSliding)
                return;

            if (isTreeOpen)
                StartCoroutine(SlideOutTree());
            else
                StartCoroutine(SlideInTree());
        }

        public void ForceCloseTreePanel()
        {
            if (mutationTreePanel == null)
            {
                return;
            }

            StopAllCoroutines();

            isTreeOpen = false;
            isSliding = false;
            ApplyResponsiveMutationPanelLayout();

            if (mutationTreeRect != null)
            {
                mutationTreeRect.anchoredPosition = GetHiddenPosition();
            }

            mutationTreePanel.SetActive(false);

            if (dockButtonText != null)
            {
                dockButtonText.text = ">";
            }

            SetDockButtonVisible(false);
        }



        private IEnumerator SlideInTree()
        {
            isSliding = true;

            mutationTreePanel.SetActive(true);
            SetDockButtonVisible(true);
            isTreeOpen = true;
            RefreshResponsiveMutationPanelLayout();
            yield return null;
            RefreshResponsiveMutationPanelLayout();

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            Vector2 targetVisiblePosition = GetVisiblePosition();
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, targetVisiblePosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = targetVisiblePosition;

            if (dockButtonText != null)
                dockButtonText.text = "<";

            isSliding = false;

            // ── Play shimmer on affordable nodes after panel opens ──
            if (humanPlayer != null && humanPlayer.MutationPoints > 0)
                StartCoroutine(PlayAffordableShimmer());

            ShowFirstTreeGuidanceToast();
        }

        private IEnumerator SlideOutTree()
        {
            isSliding = true;

            isTreeOpen = false;
            RefreshResponsiveMutationPanelLayout();

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            Vector2 targetHiddenPosition = GetHiddenPosition();
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, targetHiddenPosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = targetHiddenPosition;
            mutationTreePanel.SetActive(false);

            if (dockButtonText != null)
                dockButtonText.text = ">";

            SetDockButtonVisible(false);
            isSliding = false;
        }

        private void SetDockButtonVisible(bool visible)
        {
            if (dockButton != null)
            {
                dockButton.gameObject.SetActive(visible);
            }
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
            if (pendingTargetedSurgeSelection != null)
            {
                return;
            }

            if (humanPlayer != null)
            {
                int pointsBanked = humanPlayer.MutationPoints;
                humanPlayer.WantsToBankPointsThisTurn = true;
                AdaptationEffectProcessor.OnMutationPointsBanked(humanPlayer, pointsBanked);
                int bonusPointsAwarded = Math.Max(0, humanPlayer.MutationPoints - pointsBanked);
                if (bonusPointsAwarded > 0)
                {
                    RefreshSpendPointsButtonUI();
                    GameManager.Instance?.GameUI?.GameLogRouter?.RecordCompoundReserveBonus(humanPlayer.PlayerId, bonusPointsAwarded);
                }

                PlayMutationStorePointsSound();
                EndHumanMutationPhase();
            }
        }

        private void SetMutationChoiceLocked(bool locked)
        {
            if (storePointsButton != null)
            {
                storePointsButton.interactable = !locked;
            }

            if (locked)
            {
                SetSpendPointsButtonInteractable(false);
            }
            else if (humanPlayer != null)
            {
                RefreshSpendPointsButtonUI();
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

        private void ApplyResponsiveMutationPanelLayout()
        {
            if (mutationTreeRect == null)
            {
                return;
            }

            CacheMutationPanelLayoutReferences();

            float targetWidth = GetTargetPanelWidth();
            if (targetWidth > 0f)
            {
                ConfigureMutationPanelRect(targetWidth);
            }

            ConfigureMutationScrollViewRect(GetMutationPanelTopInset());

            if (mutationViewportRect != null)
            {
                mutationViewportRect.anchorMin = Vector2.zero;
                mutationViewportRect.anchorMax = Vector2.one;
                mutationViewportRect.offsetMin = Vector2.zero;
                mutationViewportRect.offsetMax = Vector2.zero;
                mutationViewportRect.pivot = new Vector2(0f, 1f);
            }

            if (mutationScrollViewContentRect != null)
            {
                mutationScrollViewContentRect.anchorMin = new Vector2(0f, 1f);
                mutationScrollViewContentRect.anchorMax = new Vector2(0f, 1f);
                mutationScrollViewContentRect.pivot = new Vector2(0f, 1f);
                mutationScrollViewContentRect.anchoredPosition = Vector2.zero;
            }

            if (!isSliding)
            {
                mutationTreeRect.anchoredPosition = isTreeOpen ? GetVisiblePosition() : GetHiddenPosition();
            }
        }

        private float GetTargetPanelWidth()
        {
            Vector2 canvasSize = GetEffectiveCanvasSize();
            if (canvasSize.x <= 0f)
            {
                return MutationPanelMaxWidth;
            }

            return Mathf.Min(MutationPanelMaxWidth, canvasSize.x);
        }

        private float GetMutationPanelTopInset()
        {
            if (mutationTreeRect == null)
            {
                return 45f;
            }

            float topInset = 0f;
            for (int i = 0; i < mutationTreeRect.childCount; i++)
            {
                RectTransform child = mutationTreeRect.GetChild(i) as RectTransform;
                if (child == null || child == mutationScrollViewRect)
                {
                    continue;
                }

                if (child.anchorMin.y < 0.9f || child.anchorMax.y < 0.9f)
                {
                    continue;
                }

                float childTopInset = Mathf.Max(0f, -child.anchoredPosition.y + (child.rect.height * child.pivot.y));
                topInset = Mathf.Max(topInset, childTopInset);
            }

            return topInset > 0f ? topInset + MutationPanelTopInsetPadding : 45f;
        }

        private Vector2 GetVisiblePosition()
        {
            return visiblePosition;
        }

        private Vector2 GetHiddenPosition()
        {
            if (mutationTreeRect == null)
            {
                return hiddenPosition;
            }

            float hiddenX = -Mathf.Max(mutationTreeRect.rect.width, 1f);
            return new Vector2(hiddenX, visiblePosition.y);
        }

        private void CacheMutationPanelLayoutReferences()
        {
            if (mutationTreePanel == null)
            {
                return;
            }

            mutationTreeRect ??= mutationTreePanel.GetComponent<RectTransform>();
            parentRectTransform ??= mutationTreeRect?.parent as RectTransform;
            rootCanvas ??= mutationTreeRect?.GetComponentInParent<Canvas>()?.rootCanvas;

            if (mutationTreeRect == null)
            {
                return;
            }

            mutationScrollViewRect ??= mutationTreeRect.Find("UI_MutationScrollView") as RectTransform;
            mutationViewportRect ??= mutationScrollViewRect?.Find("UI_MutationViewport") as RectTransform;
            mutationScrollViewContentRect ??= mutationViewportRect?.Find("UI_MutationScrollViewContent") as RectTransform;
        }

        private void ConfigureMutationPanelRect(float targetWidth)
        {
            Vector2 canvasSize = GetEffectiveCanvasSize();

            mutationTreeRect.anchorMin = new Vector2(0f, 0f);
            mutationTreeRect.anchorMax = new Vector2(0f, 1f);
            mutationTreeRect.pivot = new Vector2(0f, 0.5f);
            mutationTreeRect.sizeDelta = new Vector2(targetWidth, 0f);

            if (canvasSize.y > 0f)
            {
                mutationTreeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, canvasSize.y);
            }
        }

        private void ConfigureMutationScrollViewRect(float topInset)
        {
            if (mutationScrollViewRect == null)
            {
                return;
            }

            mutationScrollViewRect.anchorMin = Vector2.zero;
            mutationScrollViewRect.anchorMax = Vector2.one;
            mutationScrollViewRect.pivot = new Vector2(0f, 1f);
            mutationScrollViewRect.anchoredPosition = new Vector2(0f, -topInset);
            mutationScrollViewRect.sizeDelta = new Vector2(0f, -topInset);
        }

        private void RefreshResponsiveMutationPanelLayout()
        {
            CacheMutationPanelLayoutReferences();
            ApplyResponsiveMutationPanelLayout();
            ForceMutationPanelLayoutRebuild();
            CaptureResponsiveMutationPanelLayoutState();
        }

        private void RefreshResponsiveMutationPanelLayoutIfNeeded()
        {
            if (mutationTreeRect == null)
            {
                return;
            }

            CacheMutationPanelLayoutReferences();

            Vector2 parentSize = GetEffectiveCanvasSize();
            if (parentSize == lastKnownParentSize
                && Screen.width == lastKnownScreenWidth
                && Screen.height == lastKnownScreenHeight)
            {
                return;
            }

            RefreshResponsiveMutationPanelLayout();
        }

        private IEnumerator RefreshResponsiveMutationPanelLayoutNextFrame()
        {
            yield return null;
            RefreshResponsiveMutationPanelLayout();
            yield return null;
            RefreshResponsiveMutationPanelLayout();
        }

        private void CaptureResponsiveMutationPanelLayoutState()
        {
            lastKnownParentSize = GetEffectiveCanvasSize();
            lastKnownScreenWidth = Screen.width;
            lastKnownScreenHeight = Screen.height;
        }

        private Vector2 GetEffectiveCanvasSize()
        {
            if (rootCanvas != null && rootCanvas.scaleFactor > 0f)
            {
                return new Vector2(Screen.width / rootCanvas.scaleFactor, Screen.height / rootCanvas.scaleFactor);
            }

            if (parentRectTransform != null)
            {
                return parentRectTransform.rect.size;
            }

            return Vector2.zero;
        }

        private void ForceMutationPanelLayoutRebuild()
        {
            if (mutationTreeRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mutationTreeRect);

            if (mutationScrollViewRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mutationScrollViewRect);
            }

            if (mutationViewportRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mutationViewportRect);
            }

            if (mutationScrollViewContentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mutationScrollViewContentRect);

                float preferredWidth = LayoutUtility.GetPreferredWidth(mutationScrollViewContentRect);
                float preferredHeight = LayoutUtility.GetPreferredHeight(mutationScrollViewContentRect);

                if (preferredWidth > 0f || preferredHeight > 0f)
                {
                    mutationScrollViewContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
                    mutationScrollViewContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(mutationScrollViewContentRect);
                }
            }
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
            CaptureOriginalControlScales();
            humanPlayer = player;
            SetSpendPointsButtonVisible(true);
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

            ConfigurePlayerHoverTargets(player.PlayerId, playerMoldIcon.enabled);

            PopulateAllMutations();
            // Force enable controls regardless of previous turn state
            SetSpendPointsButtonInteractable(true);
            if (storePointsButton != null)
            {
                storePointsButton.gameObject.SetActive(true);
                storePointsButton.interactable = true;
            }
            if (buttonOutline != null) buttonOutline.enabled = player.MutationPoints > 0;
            if (spendPointsButtonText != null)
            {
                spendPointsButtonText.text = player.MutationPoints > 0 ? $"Spend {player.MutationPoints} Points!" : "No Points Available";
            }
            if (mutationPointsCounterText != null)
                mutationPointsCounterText.text = $"Mutation Points: {player.MutationPoints}";

            RestoreActionRowLayout();

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

            trigger.SetStaticText(StorePointsTooltipText);
        }

        private void WireSpendPointsTooltip()
        {
            if (spendPointsButton == null)
            {
                return;
            }

            spendPointsTooltipTrigger = spendPointsButton.GetComponent<TooltipTrigger>();
            if (spendPointsTooltipTrigger == null)
            {
                spendPointsTooltipTrigger = spendPointsButton.gameObject.AddComponent<TooltipTrigger>();
            }

            spendPointsTooltipTrigger.SetStaticText(SpendPointsTooltipText);
            spendPointsTooltipTrigger.SetAutoPlacementOffsetX(60f);
        }

        private void ConfigurePlayerHoverTargets(int playerId, bool hasVisibleIcon)
        {
            var iconHoverHandler = PlayerMoldIconHoverHandler.Attach(playerMoldIcon != null ? playerMoldIcon.gameObject : null, playerId, gridVisualizer);
            if (iconHoverHandler != null)
            {
                iconHoverHandler.enabled = hasVisibleIcon && gridVisualizer != null;
            }

            if (playerMoldIcon != null)
            {
                playerMoldIcon.raycastTarget = hasVisibleIcon && gridVisualizer != null;
            }
        }

        private void ShowFirstTreeGuidanceToast()
        {
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (hasDismissedTreeGuidanceThisGame)
            {
                return;
            }

            if (!forceFirstGame && PlayerPrefs.GetInt(MutationTreeGuidanceSeenKey, 0) != 0)
            {
                return;
            }

            var toastPresenter = GameManager.Instance?.GameUI?.MutationTreeToastPresenter;
            if (toastPresenter == null)
            {
                return;
            }

            toastPresenter.ShowModalIfTreeOpen(FirstTreeGuidanceToastText, OnFirstTreeGuidanceDismissed);
        }

        private void OnFirstTreeGuidanceDismissed()
        {
            hasDismissedTreeGuidanceThisGame = true;

            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                PlayerPrefs.SetInt(MutationTreeGuidanceSeenKey, 1);
                PlayerPrefs.Save();
            }
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
            layout.minHeight = Mathf.Max(layout.minHeight, StoreButtonMinHeight);
            layout.minWidth = Mathf.Max(layout.minWidth, StoreButtonMinWidth);

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

        private void CaptureOriginalControlScales()
        {
            if (spendPointsButton != null)
            {
                Vector3 currentScale = spendPointsButton.transform.localScale;
                if (!IsUsableScale(originalButtonScale))
                {
                    originalButtonScale = IsUsableScale(currentScale) ? currentScale : Vector3.one;
                }
            }

            if (mutationPointsCounterText != null)
            {
                Vector3 currentScale = mutationPointsCounterText.transform.localScale;
                if (!IsUsableScale(originalCounterScale))
                {
                    originalCounterScale = IsUsableScale(currentScale) ? currentScale : Vector3.one;
                }
            }
        }

        private void RestoreActionRowLayout()
        {
            RestoreSpendButtonLayout();
            RestoreStoreButtonLayout();

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.transform.localScale = originalCounterScale;
            }

            ForceLayoutRebuild(spendPointsButton != null ? spendPointsButton.transform.parent as RectTransform : null);
        }

        private void RestoreSpendButtonLayout()
        {
            if (spendPointsButton == null)
            {
                return;
            }

            spendPointsButton.gameObject.SetActive(true);
            spendPointsButton.transform.localScale = originalButtonScale;

            if (spendPointsButton.TryGetComponent<RectTransform>(out var rectTransform))
            {
                if (rectTransform.sizeDelta.y < SpendButtonMinHeight)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, SpendButtonMinHeight);
                }
            }

            var layout = spendPointsButton.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = spendPointsButton.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = Mathf.Max(layout.minWidth, SpendButtonMinWidth);
            layout.preferredWidth = Mathf.Max(layout.preferredWidth, SpendButtonMinWidth);
            layout.minHeight = Mathf.Max(layout.minHeight, SpendButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, SpendButtonMinHeight);
        }

        private void RestoreStoreButtonLayout()
        {
            if (storePointsButton == null)
            {
                return;
            }

            storePointsButton.gameObject.SetActive(true);

            var layout = storePointsButton.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = storePointsButton.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = Mathf.Max(layout.minWidth, StoreButtonMinWidth);
            layout.preferredWidth = Mathf.Max(layout.preferredWidth, StoreButtonMinWidth);
            layout.minHeight = Mathf.Max(layout.minHeight, StoreButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, StoreButtonMinHeight);
        }

        private static void ForceLayoutRebuild(RectTransform rowRect)
        {
            if (rowRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);

            if (rowRect.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        private static bool IsUsableScale(Vector3 scale)
        {
            return scale.x > 0f
                && scale.y > 0f
                && scale.z > 0f
                && !float.IsNaN(scale.x)
                && !float.IsNaN(scale.y)
                && !float.IsNaN(scale.z);
        }
    }
}
