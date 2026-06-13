using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using UnityEngine.Tilemaps;
using FungusToast.Unity.Grid;
using FungusToast.Unity;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Onboarding;
using System.Linq;
using FungusToast.Core.Metrics;

namespace FungusToast.Unity.UI.MutationTree
{
    public class UI_MutationManager : MonoBehaviour
    {
        private const string SpendPointsTooltipText = "Open your upgrades and spend your mutation points now.";
        private const string StorePointsTooltipText = "Store your unspent mutation points.\nThey carry over to the next turn,\nso you can save for stronger upgrades.";
        private const string NormalSpeedTooltipText = "Standard pacing keeps the full growth and decay presentation sequence.";
        private const string TimeLapseTooltipText = "Skips most animations and speeds up Growth Cycles to reduce time between turns.";
        private const float SpendButtonMinWidth = 220f;
        private const float SpendButtonMinHeight = 40f;
        private const float StoreButtonMinWidth = 220f;
        private const float StoreButtonMinHeight = 36f;
        private const float PresentationSpeedButtonMinWidth = 220f;
        private const float PresentationSpeedButtonMinHeight = 36f;
        private const float HeaderControlsHeight = 40f;
        private const float HeaderControlsHorizontalInset = 16f;
        private const float HeaderControlsSpacing = 12f;
        private const float HeaderButtonIconSize = 18f;
        private const float HeaderButtonContentSpacing = 8f;
        private const float HeaderButtonHorizontalPadding = 18f;
        private const float MutationPanelMaxWidth = 1125f;
        private const float MutationPanelTopInsetPadding = 6f;
        private const float TimeLapseCoachmarkWidth = 340f;
        private const float TimeLapseCoachmarkHeight = 168f;
        private const float TimeLapseCoachmarkHorizontalOffset = 5f;
        private const float TimeLapseCoachmarkVerticalOffset = -12f;
        private const float StorePointsCoachmarkWidth = 360f;
        private const float StorePointsCoachmarkHeight = 190f;
        private const float StorePointsCoachmarkHorizontalOffset = 5f;
        private const float StorePointsCoachmarkVerticalOffset = -12f;

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
        [SerializeField] private Sprite storePointsButtonIcon;
        [SerializeField] private Sprite presentationSpeedButtonIcon;
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
        private bool hasDismissedAlphaMutationIntroThisGame;
        private bool hasDismissedTreeGuidanceThisGame;
        private TooltipTrigger spendPointsTooltipTrigger;
        private TooltipTrigger presentationSpeedTooltipTrigger;
        private AudioSource soundEffectAudioSource;
        private Button presentationSpeedButton;
        private TextMeshProUGUI presentationSpeedButtonText;
        private Image storePointsButtonIconImage;
        private Image presentationSpeedButtonIconImage;
        private RectTransform timeLapseCoachmarkRoot;
        private CanvasGroup timeLapseCoachmarkCanvasGroup;
        private TextMeshProUGUI timeLapseCoachmarkTitleTextLabel;
        private TextMeshProUGUI timeLapseCoachmarkBodyTextLabel;
        private Button timeLapseCoachmarkCloseButton;
        private RectTransform storePointsCoachmarkRoot;
        private CanvasGroup storePointsCoachmarkCanvasGroup;
        private TextMeshProUGUI storePointsCoachmarkTitleTextLabel;
        private TextMeshProUGUI storePointsCoachmarkBodyTextLabel;
        private Button storePointsCoachmarkCloseButton;
        private RectTransform headerControlsRowRect;
        private RectTransform headerLeftSlotRect;
        private RectTransform headerCenterSlotRect;
        private RectTransform headerRightSlotRect;

        private Player humanPlayer;
        private bool humanTurnEnded = false;
        private List<MutationNodeUI> mutationButtons = new();
        private Dictionary<int, List<int>> directDependentsByMutationId = new();
        private Mutation hoveredMutation;
        private Player hoveredMutationPlayer;
        private PendingTargetedSurgeSelection pendingTargetedSurgeSelection;
        private Vector2 lastKnownParentSize = new(-1f, -1f);
        private int lastKnownScreenWidth = -1;
        private int lastKnownScreenHeight = -1;
        private bool hasDismissedTimeLapseCoachmarkThisGame;
        private bool hasDismissedStorePointsCoachmarkThisGame;

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
            RefreshPresentationSpeedModeUI();
            RefreshResponsiveMutationPanelLayout();
            StartCoroutine(RefreshResponsiveMutationPanelLayoutNextFrame());
        }

        private void OnDisable()
        {
            hoveredMutation = null;
            hoveredMutationPlayer = null;
            HideTimeLapseCoachmarkImmediate(false);
            HideStorePointsCoachmarkImmediate(false);
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

            EnsurePresentationSpeedButton();
            EnsureHeaderControlsRow();
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
            StyleSpendPointsButton();

            if (presentationSpeedButton != null)
            {
                StylePresentationSpeedButton();
            }

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.color = MutationTreeColors.PrimaryText;
                mutationPointsCounterText.alignment = TextAlignmentOptions.MidlineLeft;
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
            hasDismissedAlphaMutationIntroThisGame = false;
            hasDismissedTreeGuidanceThisGame = false;
            hasDismissedTimeLapseCoachmarkThisGame = false;
            hasDismissedStorePointsCoachmarkThisGame = false;
            HideTimeLapseCoachmarkImmediate(true);
            HideStorePointsCoachmarkImmediate(true);

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

            SetSpendPointsButtonText("No Points Available");

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.text = "Mutation Points: 0";
            }

            ResetPulse();
            RefreshPresentationSpeedModeUI();
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

            Tile tile = gridVisualizer.GetMoldIconTileForPlayer(player.PlayerId);
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
            RefreshPresentationSpeedModeUI();
        }

        public void OnSpendPointsClicked()
        {
            if (isSliding || pendingTargetedSurgeSelection != null || ShouldSuppressSpendPointsButton()) return;

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
                bool shouldShow = visible && !ShouldSuppressSpendPointsButton();
                spendPointsButton.gameObject.SetActive(shouldShow);
                if (shouldShow)
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

            hoveredMutation = null;
            hoveredMutationPlayer = null;

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
            RefreshSpendPointsButtonUI();
            RefreshAllMutationButtons();
            SetMutationChoiceLocked(true);
            ForceCloseTreePanel();

            bool resolved = false;
            bool success = false;
            var observer = gameManager.GameUI.GameLogRouter;
            int projectedLevel = Math.Min(humanPlayer.GetMutationLevel(mutation.Id) + 1, mutation.MaxLevel);

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
                    pendingTargetedSurgeSelection = null;
                    RefreshSpendPointsButtonUI();
                    RefreshAllMutationButtons();
                    SetMutationChoiceLocked(false);
                    resolved = true;
                },
                "Select one empty, non-nutrient tile to place your Chemobeacon.",
                showCancelButton: true,
                cancelButtonLabel: "Cancel (Esc)"
            );

            TileSelectionController.Instance.SetHoverPreviewCallback(tileId =>
            {
                if (gridVisualizer == null || tileId < 0)
                {
                    gridVisualizer?.ClearChemotacticBeaconPreview();
                    return;
                }

                var previewTileIds = ChemotacticBeaconHelper.GetProjectedGrowthTileIds(humanPlayer, board, tileId, projectedLevel);
                if (previewTileIds.Count == 0)
                {
                    gridVisualizer.ClearChemotacticBeaconPreview();
                    return;
                }

                gridVisualizer.ShowChemotacticBeaconPreview(previewTileIds);
            });

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

            if (ShouldSuppressSpendPointsButton())
            {
                spendPointsButton.gameObject.SetActive(false);
                spendPointsButton.interactable = false;
                buttonOutline.enabled = false;

                if (mutationPointsCounterText != null)
                    mutationPointsCounterText.text = $"Mutation Points: {humanPlayer.MutationPoints}";

                return;
            }

            if (pendingTargetedSurgeSelection != null)
            {
                spendPointsButton.interactable = false;
                SetSpendPointsButtonText("Select Tile");
                buttonOutline.enabled = false;

                if (mutationPointsCounterText != null)
                    mutationPointsCounterText.text = $"Mutation Points: {humanPlayer.MutationPoints}";

                return;
            }

            int points = humanPlayer.MutationPoints;
            if (points > 0)
            {
                spendPointsButton.interactable = true;
                SetSpendPointsButtonText($"Spend {points} Points!");
                buttonOutline.enabled = true;
            }
            else
            {
                spendPointsButton.interactable = false;
                SetSpendPointsButtonText("No Points Available");
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
            TryShowTimeLapseCoachmark();
            TryShowStorePointsCoachmark();
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
            HideTimeLapseCoachmarkImmediate(false);
            HideStorePointsCoachmarkImmediate(false);

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

            ReapplyHoveredMutationState();

            // Also refresh category investment summaries
            if (mutationTreeBuilder != null && humanPlayer != null)
                mutationTreeBuilder.UpdateCategoryInvestmentSummaries(mutationButtons, humanPlayer);
        }

        public Mutation GetMutationById(int id)
        {
            return mutationManager?.GetMutationById(id);
        }

        public void HandleMutationNodeHover(Mutation mutation, Player player)
        {
            hoveredMutation = mutation;
            hoveredMutationPlayer = player;
            ReapplyHoveredMutationState();
        }

        public void HandleMutationNodeHoverExit(Mutation mutation)
        {
            if (hoveredMutation != null && mutation != null && hoveredMutation.Id != mutation.Id)
            {
                return;
            }

            hoveredMutation = null;
            hoveredMutationPlayer = null;
            ClearAllHighlights();
            ClearProjectedCost();
        }

        private void ReapplyHoveredMutationState()
        {
            if (hoveredMutation == null || hoveredMutationPlayer == null)
            {
                return;
            }

            bool isLocked = hoveredMutation.Prerequisites.Any(prereq => hoveredMutationPlayer.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel);
            if (isLocked)
            {
                HighlightUnmetPrerequisites(hoveredMutation, hoveredMutationPlayer);
            }
            else
            {
                HighlightDirectDependents(hoveredMutation);
            }

            int currentLevel = hoveredMutationPlayer.GetMutationLevel(hoveredMutation.Id);
            bool isMaxed = currentLevel >= hoveredMutation.MaxLevel;
            if (isMaxed)
            {
                ClearProjectedCost();
                return;
            }

            int cost = hoveredMutationPlayer.GetMutationPointCost(hoveredMutation);
            ShowProjectedCost(cost);
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
            float headerTopInset = GetKnownHeaderTopInset();
            if (headerTopInset > 0f)
            {
                return headerTopInset + MutationPanelTopInsetPadding;
            }

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

                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.anchorMin.y < 0.9f || child.anchorMax.y < 0.9f)
                {
                    continue;
                }

                float childHeight = child == headerControlsRowRect
                    ? HeaderControlsHeight
                    : child.rect.height;
                float childTopInset = Mathf.Max(0f, -child.anchoredPosition.y + (childHeight * child.pivot.y));
                topInset = Mathf.Max(topInset, childTopInset);
            }

            return topInset > 0f ? topInset + MutationPanelTopInsetPadding : 45f;
        }

        private float GetKnownHeaderTopInset()
        {
            if (TryGetTopInsetForRect(headerControlsRowRect, HeaderControlsHeight, out float headerRowInset))
            {
                return headerRowInset;
            }

            float legacyInset = 0f;
            if (TryGetTopInsetForRect(mutationPointsCounterText != null ? mutationPointsCounterText.rectTransform : null, fallbackHeight: 40f, out float pointsInset))
            {
                legacyInset = Mathf.Max(legacyInset, pointsInset);
            }

            if (TryGetTopInsetForRect(storePointsButton != null ? storePointsButton.transform as RectTransform : null, fallbackHeight: StoreButtonMinHeight, out float storeInset))
            {
                legacyInset = Mathf.Max(legacyInset, storeInset);
            }

            if (TryGetTopInsetForRect(presentationSpeedButton != null ? presentationSpeedButton.transform as RectTransform : null, fallbackHeight: PresentationSpeedButtonMinHeight, out float timeLapseInset))
            {
                legacyInset = Mathf.Max(legacyInset, timeLapseInset);
            }

            return legacyInset;
        }

        private static bool TryGetTopInsetForRect(RectTransform rectTransform, float fallbackHeight, out float topInset)
        {
            topInset = 0f;
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
            {
                return false;
            }

            float rectHeight = rectTransform.rect.height > 0f ? rectTransform.rect.height : fallbackHeight;
            topInset = Mathf.Max(0f, -rectTransform.anchoredPosition.y + (rectHeight * rectTransform.pivot.y));
            return topInset > 0f;
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

            if (IsTimeLapseCoachmarkVisible())
            {
                PositionTimeLapseCoachmark();
            }

            if (IsStorePointsCoachmarkVisible())
            {
                PositionStorePointsCoachmark();
            }

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
                spendPointsButton.interactable = interactable && !ShouldSuppressSpendPointsButton();
        }

        private bool ShouldSuppressSpendPointsButton()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                return false;
            }

            bool endgameVisible = manager.GameUI?.EndGamePanel != null
                && manager.GameUI.EndGamePanel.gameObject.activeInHierarchy;

            return endgameVisible || manager.IsDraftOverlayVisible;
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
                Tile tile = gridVisualizer.GetMoldIconTileForPlayer(player.PlayerId);
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
            SetSpendPointsButtonText(player.MutationPoints > 0 ? $"Spend {player.MutationPoints} Points!" : "No Points Available");
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
            mutationPointsCounterText.text = $"Mutation Points: {current}  <color=#{UIStyleTokens.ToHtmlRgb(UIStyleTokens.Text.Muted)}>→ {projected}</color>";
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

        private void EnsurePresentationSpeedButton()
        {
            if (presentationSpeedButton != null)
            {
                return;
            }

            Button templateButton = storePointsButton != null ? storePointsButton : spendPointsButton;
            if (templateButton == null)
            {
                return;
            }

            GameObject buttonObject = Instantiate(templateButton.gameObject, templateButton.transform.parent);
            buttonObject.name = "PhaseSpeedButton";
            buttonObject.transform.SetSiblingIndex(templateButton.transform.GetSiblingIndex() + 1);

            presentationSpeedButton = buttonObject.GetComponent<Button>();
            presentationSpeedButtonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);

            presentationSpeedButton.onClick.RemoveAllListeners();
            presentationSpeedButton.onClick.AddListener(OnPresentationSpeedButtonClicked);

            StylePresentationSpeedButton();
            WirePresentationSpeedTooltip();
            RefreshPresentationSpeedModeUI();
        }

        private void EnsureHeaderControlsRow()
        {
            if (mutationTreeRect == null)
            {
                CacheMutationPanelLayoutReferences();
            }

            if (mutationTreeRect == null || mutationPointsCounterText == null || storePointsButton == null || presentationSpeedButton == null)
            {
                return;
            }

            if (headerControlsRowRect == null)
            {
                var rowObject = new GameObject("UI_MutationHeaderControlsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                headerControlsRowRect = rowObject.GetComponent<RectTransform>();
                headerControlsRowRect.SetParent(mutationTreeRect, false);
            }

            ConfigureHeaderControlsRowRect();
            ConfigureHeaderControlsRowLayout();

            headerLeftSlotRect ??= CreateHeaderSlot("UI_MutationHeaderLeftSlot", flexibleWidth: 1f, preferredWidth: 0f);
            headerCenterSlotRect ??= CreateHeaderSlot("UI_MutationHeaderCenterSlot", flexibleWidth: 0f, preferredWidth: StoreButtonMinWidth);
            headerRightSlotRect ??= CreateHeaderSlot("UI_MutationHeaderRightSlot", flexibleWidth: 0f, preferredWidth: PresentationSpeedButtonMinWidth);

            ConfigureHeaderSlotLayout(headerLeftSlotRect, flexibleWidth: 1f, preferredWidth: 0f);
            ConfigureHeaderSlotLayout(headerCenterSlotRect, flexibleWidth: 0f, preferredWidth: StoreButtonMinWidth);
            ConfigureHeaderSlotLayout(headerRightSlotRect, flexibleWidth: 0f, preferredWidth: PresentationSpeedButtonMinWidth);

            MoveLabelToHeaderLeftSlot();
            MoveButtonToHeaderCenterSlot(storePointsButton);
            MoveButtonToHeaderRightSlot(presentationSpeedButton);
            RefreshHeaderActionButtonWidths();
        }

        private void ConfigureHeaderControlsRowRect()
        {
            if (headerControlsRowRect == null || mutationTreeRect == null)
            {
                return;
            }

            headerControlsRowRect.SetParent(mutationTreeRect, false);
            headerControlsRowRect.SetAsLastSibling();
            headerControlsRowRect.anchorMin = new Vector2(0f, 1f);
            headerControlsRowRect.anchorMax = new Vector2(1f, 1f);
            headerControlsRowRect.pivot = new Vector2(0.5f, 0.5f);
            headerControlsRowRect.anchoredPosition = new Vector2(0f, -20f);
            headerControlsRowRect.sizeDelta = new Vector2(-(HeaderControlsHorizontalInset * 2f), HeaderControlsHeight);
            headerControlsRowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, HeaderControlsHeight);
        }

        private void ConfigureHeaderControlsRowLayout()
        {
            if (headerControlsRowRect == null)
            {
                return;
            }

            var rowLayout = headerControlsRowRect.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout == null)
            {
                rowLayout = headerControlsRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            rowLayout.padding = new RectOffset(0, 0, 0, 0);
            rowLayout.spacing = HeaderControlsSpacing;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
        }

        private RectTransform CreateHeaderSlot(string name, float flexibleWidth, float preferredWidth)
        {
            var slotObject = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            var slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.SetParent(headerControlsRowRect, false);
            ConfigureHeaderSlotLayout(slotRect, flexibleWidth, preferredWidth);

            return slotRect;
        }

        private void ConfigureHeaderSlotLayout(RectTransform slotRect, float flexibleWidth, float preferredWidth)
        {
            if (slotRect == null)
            {
                return;
            }

            slotRect.anchorMin = Vector2.zero;
            slotRect.anchorMax = Vector2.one;
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = Vector2.zero;
            slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, HeaderControlsHeight);

            var layoutElement = slotRect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = slotRect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.flexibleWidth = flexibleWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minHeight = HeaderControlsHeight;
            layoutElement.preferredHeight = HeaderControlsHeight;
            layoutElement.flexibleHeight = 0f;
        }

        private void MoveLabelToHeaderLeftSlot()
        {
            var labelRect = mutationPointsCounterText != null ? mutationPointsCounterText.rectTransform : null;
            if (labelRect == null || headerLeftSlotRect == null)
            {
                return;
            }

            labelRect.SetParent(headerLeftSlotRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            mutationPointsCounterText.alignment = TextAlignmentOptions.MidlineLeft;
            mutationPointsCounterText.enableAutoSizing = false;
            FungusToast.Unity.UI.TMPOverflowUtility.SetSafeEllipsis(mutationPointsCounterText);
            mutationPointsCounterText.raycastTarget = false;
        }

        private void MoveButtonToHeaderCenterSlot(Button button)
        {
            if (button == null || headerCenterSlotRect == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.SetParent(headerCenterSlotRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void MoveButtonToHeaderRightSlot(Button button)
        {
            if (button == null || headerRightSlotRect == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.SetParent(headerRightSlotRect, false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
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

        private void WirePresentationSpeedTooltip()
        {
            if (presentationSpeedButton == null)
            {
                return;
            }

            presentationSpeedTooltipTrigger = presentationSpeedButton.GetComponent<TooltipTrigger>();
            if (presentationSpeedTooltipTrigger == null)
            {
                presentationSpeedTooltipTrigger = presentationSpeedButton.gameObject.AddComponent<TooltipTrigger>();
            }

            presentationSpeedTooltipTrigger.SetAutoPlacementOffsetX(60f);
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
            var toastPresenter = GameManager.Instance?.GameUI?.MutationTreeToastPresenter;
            if (toastPresenter == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            int currentRound = gameManager?.Board?.CurrentRound ?? 0;
            bool shouldShowAlphaMutationIntro = NewPlayerTooltipRules.ShouldQueueAlphaMutationPhaseIntro(
                forceFirstGame,
                currentRound,
                humanPlayer != null ? 1 : 0,
                isFastForwarding,
                gameManager != null && gameManager.IsTestingModeEnabled)
                && !hasDismissedAlphaMutationIntroThisGame;

            if (shouldShowAlphaMutationIntro)
            {
                NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.AlphaMutationPhaseIntro);
                toastPresenter.ShowModalIfTreeOpen(
                    definition.Title,
                    definition.Body,
                    OnAlphaMutationIntroDismissed);
                return;
            }

            if (!NewPlayerTooltipRules.ShouldShowMutationTreeGuidance(forceFirstGame, hasDismissedTreeGuidanceThisGame))
            {
                return;
            }

            NewPlayerTooltipDefinition treeGuidanceDefinition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.MutationTreeGuidance);
            toastPresenter.ShowModalIfTreeOpen(
                treeGuidanceDefinition.Title,
                treeGuidanceDefinition.Body,
                OnFirstTreeGuidanceDismissed);
        }

        private void OnAlphaMutationIntroDismissed()
        {
            hasDismissedAlphaMutationIntroThisGame = true;

            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.AlphaMutationPhaseIntro);
            }
        }

        private void OnFirstTreeGuidanceDismissed()
        {
            hasDismissedTreeGuidanceThisGame = true;

            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.MutationTreeGuidance);
            }
        }

        private void TryShowTimeLapseCoachmark()
        {
            if (!isTreeOpen || mutationTreePanel == null || !mutationTreePanel.activeInHierarchy || presentationSpeedButton == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            int currentRound = gameManager?.Board?.CurrentRound ?? 0;
            if (!NewPlayerTooltipRules.ShouldShowTimeLapseModeIntro(
                    forceFirstGame,
                    currentRound,
                    hasDismissedTimeLapseCoachmarkThisGame,
                    isFastForwarding))
            {
                return;
            }

            EnsureTimeLapseCoachmarkUi();
            if (timeLapseCoachmarkRoot == null || timeLapseCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.TimeLapseModeIntro);
            timeLapseCoachmarkTitleTextLabel.text = definition.Title;
            timeLapseCoachmarkBodyTextLabel.text = definition.Body;
            PositionTimeLapseCoachmark();
            timeLapseCoachmarkRoot.gameObject.SetActive(true);
            timeLapseCoachmarkRoot.SetAsLastSibling();
            timeLapseCoachmarkCanvasGroup.alpha = 1f;
            timeLapseCoachmarkCanvasGroup.blocksRaycasts = true;
            timeLapseCoachmarkCanvasGroup.interactable = true;
        }

        private void TryShowStorePointsCoachmark()
        {
            if (!isTreeOpen || mutationTreePanel == null || !mutationTreePanel.activeInHierarchy || storePointsButton == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            int currentRound = gameManager?.Board?.CurrentRound ?? 0;
            if (!NewPlayerTooltipRules.ShouldShowStoreMutationPointsIntro(
                    forceFirstGame,
                    currentRound,
                    hasDismissedStorePointsCoachmarkThisGame,
                    isFastForwarding))
            {
                return;
            }

            EnsureStorePointsCoachmarkUi();
            if (storePointsCoachmarkRoot == null || storePointsCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.StoreMutationPointsIntro);
            storePointsCoachmarkTitleTextLabel.text = definition.Title;
            storePointsCoachmarkBodyTextLabel.text = definition.Body;
            PositionStorePointsCoachmark();
            storePointsCoachmarkRoot.gameObject.SetActive(true);
            storePointsCoachmarkRoot.SetAsLastSibling();
            storePointsCoachmarkCanvasGroup.alpha = 1f;
            storePointsCoachmarkCanvasGroup.blocksRaycasts = true;
            storePointsCoachmarkCanvasGroup.interactable = true;
        }

        private void EnsureTimeLapseCoachmarkUi()
        {
            if (timeLapseCoachmarkRoot != null)
            {
                return;
            }

            Transform parent = rootCanvas != null
                ? rootCanvas.transform
                : mutationTreeRect?.GetComponentInParent<Canvas>()?.rootCanvas?.transform;
            if (parent == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_TimeLapseCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(parent, false);

            timeLapseCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            timeLapseCoachmarkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            timeLapseCoachmarkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            timeLapseCoachmarkRoot.pivot = new Vector2(0f, 1f);
            timeLapseCoachmarkRoot.sizeDelta = new Vector2(TimeLapseCoachmarkWidth, TimeLapseCoachmarkHeight);

            timeLapseCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            timeLapseCoachmarkCanvasGroup.alpha = 0f;
            timeLapseCoachmarkCanvasGroup.blocksRaycasts = false;
            timeLapseCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Accent.Spore, 0.14f);
            backgroundColor.a = 0.97f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-52f, -12f);

            timeLapseCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            timeLapseCoachmarkTitleTextLabel.text = string.Empty;
            timeLapseCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            timeLapseCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            timeLapseCoachmarkTitleTextLabel.fontSize = 22f;
            timeLapseCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            timeLapseCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            FungusToast.Unity.UI.TMPOverflowUtility.SetSafeEllipsis(timeLapseCoachmarkTitleTextLabel);
            timeLapseCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -50f);

            timeLapseCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            timeLapseCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            timeLapseCoachmarkBodyTextLabel.fontSize = 17f;
            timeLapseCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            timeLapseCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            timeLapseCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            timeLapseCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            timeLapseCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(timeLapseCoachmarkCloseButton);
            timeLapseCoachmarkCloseButton.onClick.RemoveAllListeners();
            timeLapseCoachmarkCloseButton.onClick.AddListener(OnTimeLapseCoachmarkDismissed);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);
            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                timeLapseCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                timeLapseCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void EnsureStorePointsCoachmarkUi()
        {
            if (storePointsCoachmarkRoot != null)
            {
                return;
            }

            Transform parent = rootCanvas != null
                ? rootCanvas.transform
                : mutationTreeRect?.GetComponentInParent<Canvas>()?.rootCanvas?.transform;
            if (parent == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_StorePointsCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(parent, false);

            storePointsCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            storePointsCoachmarkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            storePointsCoachmarkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            storePointsCoachmarkRoot.pivot = new Vector2(0f, 1f);
            storePointsCoachmarkRoot.sizeDelta = new Vector2(StorePointsCoachmarkWidth, StorePointsCoachmarkHeight);

            storePointsCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            storePointsCoachmarkCanvasGroup.alpha = 0f;
            storePointsCoachmarkCanvasGroup.blocksRaycasts = false;
            storePointsCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Accent.Spore, 0.14f);
            backgroundColor.a = 0.97f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-52f, -12f);

            storePointsCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            storePointsCoachmarkTitleTextLabel.text = string.Empty;
            storePointsCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            storePointsCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            storePointsCoachmarkTitleTextLabel.fontSize = 22f;
            storePointsCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            storePointsCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            FungusToast.Unity.UI.TMPOverflowUtility.SetSafeEllipsis(storePointsCoachmarkTitleTextLabel);
            storePointsCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -50f);

            storePointsCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            storePointsCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            storePointsCoachmarkBodyTextLabel.fontSize = 17f;
            storePointsCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            storePointsCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            storePointsCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            storePointsCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            storePointsCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(storePointsCoachmarkCloseButton);
            storePointsCoachmarkCloseButton.onClick.RemoveAllListeners();
            storePointsCoachmarkCloseButton.onClick.AddListener(OnStorePointsCoachmarkDismissed);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);
            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                storePointsCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                storePointsCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void PositionTimeLapseCoachmark()
        {
            RectTransform anchorRect = presentationSpeedButton != null ? presentationSpeedButton.transform as RectTransform : null;
            RectTransform parentRect = timeLapseCoachmarkRoot != null ? timeLapseCoachmarkRoot.parent as RectTransform : null;
            Canvas canvas = rootCanvas != null ? rootCanvas.rootCanvas : presentationSpeedButton?.GetComponentInParent<Canvas>()?.rootCanvas;
            if (anchorRect == null || parentRect == null || canvas == null || timeLapseCoachmarkRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            Vector3 rightCenterWorld = (corners[2] + corners[3]) * 0.5f;

            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                timeLapseCoachmarkRoot,
                parentRect,
                canvas,
                rightCenterWorld,
                new Vector2(TimeLapseCoachmarkHorizontalOffset, TimeLapseCoachmarkVerticalOffset),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void PositionStorePointsCoachmark()
        {
            RectTransform anchorRect = storePointsButton != null ? storePointsButton.transform as RectTransform : null;
            RectTransform parentRect = storePointsCoachmarkRoot != null ? storePointsCoachmarkRoot.parent as RectTransform : null;
            Canvas canvas = rootCanvas != null ? rootCanvas.rootCanvas : storePointsButton?.GetComponentInParent<Canvas>()?.rootCanvas;
            if (anchorRect == null || parentRect == null || canvas == null || storePointsCoachmarkRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            Vector3 rightCenterWorld = (corners[2] + corners[3]) * 0.5f;

            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                storePointsCoachmarkRoot,
                parentRect,
                canvas,
                rightCenterWorld,
                new Vector2(StorePointsCoachmarkHorizontalOffset, StorePointsCoachmarkVerticalOffset),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void OnTimeLapseCoachmarkDismissed()
        {
            hasDismissedTimeLapseCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.TimeLapseModeIntro);
            }

            HideTimeLapseCoachmarkImmediate(false);
        }

        private void OnStorePointsCoachmarkDismissed()
        {
            hasDismissedStorePointsCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.StoreMutationPointsIntro);
            }

            HideStorePointsCoachmarkImmediate(false);
        }

        private void HideTimeLapseCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedTimeLapseCoachmarkThisGame = false;
            }

            if (timeLapseCoachmarkCanvasGroup != null)
            {
                timeLapseCoachmarkCanvasGroup.alpha = 0f;
                timeLapseCoachmarkCanvasGroup.blocksRaycasts = false;
                timeLapseCoachmarkCanvasGroup.interactable = false;
            }

            if (timeLapseCoachmarkRoot != null)
            {
                timeLapseCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        private void HideStorePointsCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedStorePointsCoachmarkThisGame = false;
            }

            if (storePointsCoachmarkCanvasGroup != null)
            {
                storePointsCoachmarkCanvasGroup.alpha = 0f;
                storePointsCoachmarkCanvasGroup.blocksRaycasts = false;
                storePointsCoachmarkCanvasGroup.interactable = false;
            }

            if (storePointsCoachmarkRoot != null)
            {
                storePointsCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        private bool IsTimeLapseCoachmarkVisible()
        {
            return timeLapseCoachmarkRoot != null
                && timeLapseCoachmarkCanvasGroup != null
                && timeLapseCoachmarkRoot.gameObject.activeSelf
                && timeLapseCoachmarkCanvasGroup.alpha > 0f;
        }

        private bool IsStorePointsCoachmarkVisible()
        {
            return storePointsCoachmarkRoot != null
                && storePointsCoachmarkCanvasGroup != null
                && storePointsCoachmarkRoot.gameObject.activeSelf
                && storePointsCoachmarkCanvasGroup.alpha > 0f;
        }

        /// <summary>
        /// Styles the Store Points button with explicit colors and a storage icon
        /// so it stands out against the dark panel header.
        /// </summary>
        private void StyleStorePointsButton()
        {
            UIStyleTokens.Button.ApplySecondaryMenuAction(
                storePointsButton,
                StoreButtonMinWidth,
                preferredHeight: StoreButtonMinHeight,
                minHeight: StoreButtonMinHeight);
            UIStyleTokens.Button.SetButtonLabelColor(storePointsButton, UIStyleTokens.Text.Primary);

            var colors = storePointsButton.colors;
            colors.normalColor = UIStyleTokens.Surface.PanelElevated;
            colors.highlightedColor = Color.Lerp(UIStyleTokens.Surface.PanelElevated, UIStyleTokens.Accent.Spore, 0.55f);
            colors.pressedColor = UIStyleTokens.Surface.PanelPrimary;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            storePointsButton.transition = Selectable.Transition.ColorTint;
            storePointsButton.colors = colors;

            var outline = storePointsButton.GetComponent<Outline>();
            if (outline == null)
            {
                outline = storePointsButton.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            var layout = storePointsButton.GetComponent<LayoutElement>();
            if (layout == null)
                layout = storePointsButton.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(layout.minHeight, StoreButtonMinHeight);
            layout.minWidth = Mathf.Max(layout.minWidth, StoreButtonMinWidth);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, StoreButtonMinHeight);
            layout.preferredWidth = Mathf.Max(layout.preferredWidth, StoreButtonMinWidth);

            ConfigureHeaderActionButtonContent(
                storePointsButton,
                ref storePointsButtonIconImage,
                "Store Mutation Points",
                "StoreMutationPointsButtonContent",
                "StoreMutationPointsButtonIcon",
                storePointsButtonIcon,
                UIStyleTokens.Text.Primary,
                UIStyleTokens.Accent.Spore);
            RefreshHeaderActionButtonWidths();
        }

        private void StylePresentationSpeedButton()
        {
            if (presentationSpeedButton == null)
            {
                return;
            }

            var legacyStoreContentRoot = presentationSpeedButton.transform.Find("StoreMutationPointsButtonContent");
            if (legacyStoreContentRoot != null)
            {
                Destroy(legacyStoreContentRoot.gameObject);
            }

            UIStyleTokens.Button.ApplySecondaryMenuAction(
                presentationSpeedButton,
                PresentationSpeedButtonMinWidth,
                preferredHeight: PresentationSpeedButtonMinHeight,
                minHeight: PresentationSpeedButtonMinHeight);
            UIStyleTokens.Button.SetButtonLabelColor(presentationSpeedButton, UIStyleTokens.Text.Primary);

            var colors = presentationSpeedButton.colors;
            colors.normalColor = UIStyleTokens.Surface.PanelElevated;
            colors.highlightedColor = Color.Lerp(UIStyleTokens.Surface.PanelElevated, UIStyleTokens.Accent.Spore, 0.58f);
            colors.pressedColor = UIStyleTokens.Surface.PanelPrimary;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            presentationSpeedButton.transition = Selectable.Transition.ColorTint;
            presentationSpeedButton.colors = colors;

            var outline = presentationSpeedButton.GetComponent<Outline>();
            if (outline == null)
            {
                outline = presentationSpeedButton.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            var layout = presentationSpeedButton.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = presentationSpeedButton.gameObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = Mathf.Max(layout.minHeight, PresentationSpeedButtonMinHeight);
            layout.minWidth = Mathf.Max(layout.minWidth, PresentationSpeedButtonMinWidth);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, PresentationSpeedButtonMinHeight);
            layout.preferredWidth = Mathf.Max(layout.preferredWidth, PresentationSpeedButtonMinWidth);

            presentationSpeedButtonText = ConfigureHeaderActionButtonContent(
                presentationSpeedButton,
                ref presentationSpeedButtonIconImage,
                "Time-Lapse",
                "TimeLapseButtonContent",
                "TimeLapseButtonIcon",
                presentationSpeedButtonIcon,
                UIStyleTokens.Text.Primary,
                UIStyleTokens.Text.Primary);
            RefreshHeaderActionButtonWidths();
        }

        private void StyleSpendPointsButton()
        {
            if (spendPointsButton == null)
            {
                return;
            }

            var colors = spendPointsButton.colors;
            colors.normalColor = UIStyleTokens.Button.BackgroundSelected;
            colors.highlightedColor = Color.Lerp(UIStyleTokens.Button.BackgroundSelected, Color.white, 0.8f);
            colors.pressedColor = UIStyleTokens.Button.BackgroundPressed;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            spendPointsButton.transition = Selectable.Transition.ColorTint;
            spendPointsButton.colors = colors;

            if (buttonOutline != null)
            {
                buttonOutline.enabled = spendPointsButton.interactable;
                buttonOutline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.7f);
                buttonOutline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private void RefreshHeaderActionButtonWidths()
        {
            UpdateHeaderActionButtonWidth(storePointsButton, headerCenterSlotRect, StoreButtonMinWidth);
            UpdateHeaderActionButtonWidth(presentationSpeedButton, headerRightSlotRect, PresentationSpeedButtonMinWidth);
        }

        private void SetSpendPointsButtonText(string text)
        {
            if (spendPointsButtonText == null)
            {
                return;
            }

            spendPointsButtonText.text = text;
            RefreshSpendPointsButtonWidth();
        }

        private void RefreshSpendPointsButtonWidth()
        {
            if (spendPointsButton == null || spendPointsButtonText == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float preferredWidth = SpendButtonMinWidth;
            float labelWidth = FungusToast.Unity.UI.TMPOverflowUtility.GetPreferredWidthWithoutEllipsis(
                spendPointsButtonText,
                spendPointsButtonText.text);
            preferredWidth = Mathf.Max(preferredWidth, Mathf.Ceil(labelWidth) + (HeaderButtonHorizontalPadding * 2f));

            var layout = spendPointsButton.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = spendPointsButton.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = preferredWidth;
            layout.preferredWidth = preferredWidth;
            layout.minHeight = Mathf.Max(layout.minHeight, SpendButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, SpendButtonMinHeight);

            if (spendPointsButton.TryGetComponent<RectTransform>(out var buttonRect))
            {
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SpendButtonMinHeight);
            }

            ForceLayoutRebuild(spendPointsButton.transform.parent as RectTransform);
        }

        private void UpdateHeaderActionButtonWidth(Button button, RectTransform slotRect, float minWidth)
        {
            if (button == null || slotRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float preferredWidth = minWidth;
            var contentRoot = GetHeaderActionButtonContentRoot(button);
            if (contentRoot != null)
            {
                float contentWidth = 0f;
                var contentRect = contentRoot;
                var label = contentRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    contentWidth += FungusToast.Unity.UI.TMPOverflowUtility.GetPreferredWidthWithoutEllipsis(
                        label,
                        label.text);
                }

                var icon = contentRoot.GetComponentInChildren<Image>(true);
                if (icon != null && icon.gameObject.activeSelf)
                {
                    contentWidth += HeaderButtonIconSize;
                    if (label != null)
                    {
                        contentWidth += HeaderButtonContentSpacing;
                    }
                }

                contentWidth = Mathf.Max(contentWidth, LayoutUtility.GetPreferredWidth(contentRect));
                preferredWidth = Mathf.Max(minWidth, Mathf.Ceil(contentWidth) + (HeaderButtonHorizontalPadding * 2f));
            }

            var slotLayout = slotRect.GetComponent<LayoutElement>();
            if (slotLayout == null)
            {
                slotLayout = slotRect.gameObject.AddComponent<LayoutElement>();
            }

            slotLayout.minWidth = preferredWidth;
            slotLayout.preferredWidth = preferredWidth;
            slotLayout.flexibleWidth = 0f;

            var buttonLayout = button.GetComponent<LayoutElement>();
            if (buttonLayout == null)
            {
                buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            }

            buttonLayout.minWidth = preferredWidth;
            buttonLayout.preferredWidth = preferredWidth;

            if (button.TryGetComponent<RectTransform>(out var buttonRect))
            {
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
                var preferredHeight = button == presentationSpeedButton
                    ? PresentationSpeedButtonMinHeight
                    : StoreButtonMinHeight;
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
            }
        }

        private RectTransform GetHeaderActionButtonContentRoot(Button button)
        {
            if (button == null)
            {
                return null;
            }

            if (button == storePointsButton)
            {
                return button.transform.Find("StoreMutationPointsButtonContent") as RectTransform;
            }

            if (button == presentationSpeedButton)
            {
                return button.transform.Find("TimeLapseButtonContent") as RectTransform;
            }

            return button.GetComponentsInChildren<HorizontalLayoutGroup>(true)
                .FirstOrDefault(layout => layout != null && layout.transform.parent == button.transform)
                ?.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI ConfigureHeaderActionButtonContent(
            Button button,
            ref Image iconImage,
            string labelText,
            string contentRootName,
            string iconObjectName,
            Sprite iconSprite,
            Color labelColor,
            Color iconColor)
        {
            if (button == null)
            {
                return null;
            }

            var contentRoot = button.transform.Find(contentRootName) as RectTransform;
            if (contentRoot == null)
            {
                var contentRootObject = new GameObject(contentRootName, typeof(RectTransform));
                contentRoot = contentRootObject.GetComponent<RectTransform>();
                contentRoot.SetParent(button.transform, false);
                contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
                contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
                contentRoot.pivot = new Vector2(0.5f, 0.5f);
                contentRoot.anchoredPosition = Vector2.zero;
            }

            var contentLayout = contentRoot.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (contentLayout == null)
            {
                contentLayout = contentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            contentLayout.spacing = HeaderButtonContentSpacing;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            var fitter = contentRoot.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI label = contentRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                label = button.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .FirstOrDefault(candidate => candidate.transform != contentRoot);
            }

            if (label == null)
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.transform.SetParent(contentRoot, false);
            label.gameObject.name = "Label";
            label.text = labelText;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 0.5f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            FungusToast.Unity.UI.TMPOverflowUtility.SetSafeEllipsis(label);
            label.alignment = TextAlignmentOptions.Center;
            label.color = labelColor;
            label.margin = Vector4.zero;
            label.raycastTarget = false;

            var labelLayout = label.GetComponent<LayoutElement>();
            if (labelLayout == null)
            {
                labelLayout = label.gameObject.AddComponent<LayoutElement>();
            }

            labelLayout.minHeight = 24f;
            labelLayout.preferredHeight = 24f;
            labelLayout.flexibleWidth = 0f;
            labelLayout.flexibleHeight = 0f;

            iconImage = EnsureHeaderActionButtonIcon(contentRoot, iconObjectName, iconImage, iconSprite, iconColor);
            if (iconImage != null)
            {
                iconImage.transform.SetSiblingIndex(0);
            }

            label.transform.SetSiblingIndex(iconImage != null ? 1 : 0);
            return label;
        }

        private Image EnsureHeaderActionButtonIcon(
            RectTransform contentRoot,
            string iconObjectName,
            Image existingIconImage,
            Sprite iconSprite,
            Color iconColor)
        {
            if (contentRoot == null)
            {
                return null;
            }

            if (iconSprite == null)
            {
                if (existingIconImage != null)
                {
                    existingIconImage.gameObject.SetActive(false);
                }

                return existingIconImage;
            }

            if (existingIconImage == null)
            {
                var iconObject = new GameObject(iconObjectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(contentRoot, false);
                existingIconImage = iconObject.GetComponent<Image>();
            }

            existingIconImage.gameObject.SetActive(true);
            existingIconImage.gameObject.name = iconObjectName;
            existingIconImage.sprite = iconSprite;
            existingIconImage.color = iconColor;
            existingIconImage.preserveAspect = true;
            existingIconImage.raycastTarget = false;

            var iconLayout = existingIconImage.GetComponent<LayoutElement>();
            if (iconLayout == null)
            {
                iconLayout = existingIconImage.gameObject.AddComponent<LayoutElement>();
            }

            iconLayout.minWidth = HeaderButtonIconSize;
            iconLayout.preferredWidth = HeaderButtonIconSize;
            iconLayout.minHeight = HeaderButtonIconSize;
            iconLayout.preferredHeight = HeaderButtonIconSize;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;
            return existingIconImage;
        }

        private void OnPresentationSpeedButtonClicked()
        {
            GameManager.Instance?.CycleRoundPresentationSpeedMode();
            RefreshPresentationSpeedModeUI();
        }

        public void RefreshPresentationSpeedModeUI()
        {
            if (presentationSpeedButton == null)
            {
                return;
            }

            RoundPresentationSpeedMode mode = GameManager.Instance != null
                ? GameManager.Instance.RoundPresentationSpeedMode
                : RoundPresentationSpeedMode.Normal;

            if (presentationSpeedButtonText != null)
            {
                presentationSpeedButtonText.text = mode == RoundPresentationSpeedMode.TimeLapse
                    ? "Time-Lapse: On"
                    : "Time-Lapse: Off";
                presentationSpeedButtonText.fontStyle = FontStyles.Bold;
            }

            RefreshHeaderActionButtonWidths();

            if (presentationSpeedTooltipTrigger != null)
            {
                presentationSpeedTooltipTrigger.SetStaticText(
                    mode == RoundPresentationSpeedMode.TimeLapse
                        ? TimeLapseTooltipText
                        : NormalSpeedTooltipText);
            }

            if (mode == RoundPresentationSpeedMode.TimeLapse && IsTimeLapseCoachmarkVisible())
            {
                OnTimeLapseCoachmarkDismissed();
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
            EnsureHeaderControlsRow();
            RestoreSpendButtonLayout();
            RestoreStoreButtonLayout();
            RestorePresentationSpeedButtonLayout();

            if (mutationPointsCounterText != null)
            {
                mutationPointsCounterText.transform.localScale = originalCounterScale;
            }

            RefreshHeaderActionButtonWidths();
            ForceLayoutRebuild(headerControlsRowRect);
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

            layout.minHeight = Mathf.Max(layout.minHeight, SpendButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, SpendButtonMinHeight);

            RefreshSpendPointsButtonWidth();
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
            layout.minHeight = Mathf.Max(layout.minHeight, StoreButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, StoreButtonMinHeight);
        }

        private void RestorePresentationSpeedButtonLayout()
        {
            if (presentationSpeedButton == null)
            {
                return;
            }

            presentationSpeedButton.gameObject.SetActive(true);

            var layout = presentationSpeedButton.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = presentationSpeedButton.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = Mathf.Max(layout.minWidth, PresentationSpeedButtonMinWidth);
            layout.minHeight = Mathf.Max(layout.minHeight, PresentationSpeedButtonMinHeight);
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, PresentationSpeedButtonMinHeight);
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
