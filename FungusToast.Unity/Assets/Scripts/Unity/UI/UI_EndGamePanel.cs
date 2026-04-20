using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using System.Linq;
using TMPro;
using FungusToast.Unity.UI.Tooltips;
using System.Globalization;
using System;
using FungusToast.Unity.UI.Campaign;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.Endgame;
using FungusToast.Unity.Input;
using FungusToast.Unity.UI.Testing;
using UnityEngine.EventSystems;
using FungusToast.Core.Campaign;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using static FungusToast.Unity.Campaign.MoldinessProgression;

namespace FungusToast.Unity.UI
{
    public enum DefeatCarryoverEntryMode
    {
        ImmediateLossScreen,
        DeferredResumePrompt,
    }

    public class UI_EndGamePanel : MonoBehaviour
    {
        private const float OutcomeLabelAnchorMinX = 0.08f;
        private const float OutcomeLabelAnchorMaxX = 0.92f;
        private const float OutcomeLabelAnchorMinY = 0.89f;
        private const float OutcomeLabelAnchorMaxY = 0.975f;
        private const float OutcomeBackdropAnchorMinX = 0.04f;
        private const float OutcomeBackdropAnchorMaxX = 0.96f;
        private const float OutcomeBackdropAnchorMinY = 0.875f;
        private const float OutcomeBackdropAnchorMaxY = 0.99f;
        private const float CampaignOutcomeSpacerPreferredHeight = 94f;
        private const float CampaignOutcomeSpacerMinHeight = 88f;
        private const int CampaignOutcomeSubtitleFontSize = 26;
        private const float EndGameOverlayHorizontalInset = 120f;
        private const float PendingMoldinessRewardPanelWidth = 950f;
        private const float EndGameConfirmationOverlayHorizontalInset = 220f;
        private const float EndGameOverlayVerticalInset = 18f;
        private const float EndGameConfirmationOverlayVerticalInset = 76f;
        private const float EndGameContentHorizontalInset = 28f;
        private const float EndGameContentTopInset = 30f;
        private const float EndGameContentTopInsetWithOutcome = 126f;
        private const float EndGameConfirmationContentTopInsetWithOutcome = 162f;
        private const float EndGameContentBottomInset = 110f;
        private const float EndGameConfirmationContentBottomInset = 56f;
        private const float EndGameActionBarHeight = 60f;
        private const float EndGameActionBarBottomInset = 24f;
        private const float EndGameRailWidth = 340f;
        private const float EndGameRailGap = 18f;
        private const float EndGameActionButtonMinWidth = 220f;
        private const float EndGameActionButtonPreferredWidth = 280f;
        private const float EndGameLegacyHeaderHeight = 42f;
        private const float EndGameResultsScrollMinHeight = 220f;
        private const float EndGameTestingRailMinimumCardWidth = 1480f;
        private const float EndGameResultsHeaderHorizontalPadding = 18f;
        private const float EndGameResultsRankWidth = 60f;
        private const float EndGameResultsIconWidth = 52f;
        private const float EndGameResultsMetricWidth = 92f;
        private const float EndGameResultsDetailsWidth = 108f;
        private const float EndGameConfirmationPrimaryButtonWidth = 500f;
        private const float EndGameConfirmationCompactButtonWidth = 330f;
        private const float EndGameConfirmationButtonHeight = 56f;
        private const float EndGameConfirmationStackWidth = 500f;
        private const float EndGameConfirmationStackSpacing = 14f;
        private const float DetailsCardPreferredWidth = 820f;
        private const float DetailsCardPreferredHeight = 900f;
        private const float DetailsCardMinWidth = 680f;
        private const float DetailsCardMinHeight = 640f;
        private const float DetailsCardSideBuffer = 72f;
        private const float DetailsCardVerticalBuffer = 34f;
        private const float DetailsDismissButtonSize = 44f;
        private const float DetailsHeaderIconSize = 56f;
        private const float DetailsSectionSpacing = 12f;

        /* ─────────── Inspector ─────────── */
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private UI_GameEndPlayerResultsRow playerResultRowPrefab;
        [SerializeField] private Button continueButton; // campaign mid-run victory only
        [SerializeField] private Button exitButton; // always available to return to mode select
        [SerializeField] private Button playAgainButton; // solo / hotseat replay
        [SerializeField] private TextMeshProUGUI outcomeLabel; // dynamic outcome messaging
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image resultsCardBackground;
        [SerializeField] private Image outcomeBackdrop;
        [SerializeField] private Sprite pendingRewardBreadSprite;

        // Façade reference — set by GameManager so we don't need GameManager.Instance
        private GameUIManager gameUI;
        private System.Action onCampaignResume;
        private System.Action onExitToModeSelect;
        private bool requiresAdaptationBeforeContinue;
        private bool requiresDefeatCarryoverSelection;
        private bool requiresMoldinessRewardSelection;
        private bool hasPendingDefeatCarryoverEvent;
        private int defeatCarryoverSelectionCapacity;
        private string selectedMoldinessRewardId;
        private readonly HashSet<string> selectedDefeatCarryoverAdaptationIds = new();
        private readonly Dictionary<string, Image> defeatCarryoverOptionImages = new();
        private readonly List<Image> moldinessRewardOptionBackgrounds = new();
        private readonly List<MoldinessRewardOptionVisual> moldinessRewardOptionVisuals = new();
        private readonly List<AdaptationDefinition> pendingDefeatCarryoverOptions = new();
        private bool returnToCampaignMenuAfterMoldinessReward;
        private bool showPostAdaptationConfirmationAfterMoldinessRewardSelection;
        private DefeatCarryoverEntryMode pendingDefeatCarryoverEntryMode = DefeatCarryoverEntryMode.ImmediateLossScreen;
        private CampaignVictorySnapshot cachedCampaignVictorySnapshot;
        private TextMeshProUGUI defeatCarryoverSelectionStatusLabel;
        private readonly List<Image> moldinessSummaryToastTiles = new();
        private readonly List<Component> legacyResultsHeaderCandidates = new();
        private RectTransform endGameLayoutContainer;
        private RectTransform endGameContentShellRoot;
        private RectTransform endGameMainColumnRoot;
        private RectTransform endGameTestingRailRoot;
        private LayoutElement endGameTestingRailLayoutElement;
        private RectTransform endGameTestingRailMirrorSpacerRoot;
        private LayoutElement endGameTestingRailMirrorSpacerLayoutElement;
        private RectTransform endGameActionBarRoot;
        private RectTransform endGameResultsScrollRoot;
        private RectTransform endGameResultsViewportRoot;
        private ScrollRect endGameResultsScrollRect;
        private TextMeshProUGUI legacyResultsTitleText;
        private RectTransform endGamePostAdaptationRoot;
        private DevelopmentTestingCardController postVictoryTestingCardController;
        private bool runtimeLayoutRefreshQueued;
        private bool isRefreshingRuntimeLayout;
        private bool resetResultsScrollPositionOnNextLayout;
        private bool postVictoryTestingRailRequestedVisible;
        private bool postVictoryTestingRailVisible;
        private bool showPostAdaptationConfirmationState;

        private bool UseVerticalActionStack => showPostAdaptationConfirmationState || requiresMoldinessRewardSelection;

        private sealed class MoldinessRewardOptionVisual
        {
            public string RewardId;
            public Image Background;
            public Image FillOverlay;
            public Outline FillOverlayOutline;
            public Outline Outline;
            public Image BadgeBackground;
        }

        // Post-victory campaign testing controls (runtime-built to avoid scene dependency).
        private GameObject postVictoryTestingRoot;
        private Button postVictoryTestingToggleButton = null;
        private TMP_Dropdown postVictoryMycovariantDropdown;
        private GameObject postVictoryMycovariantRow = null;
        private TMP_Dropdown postVictoryAdaptationDropdown;
        private GameObject postVictoryAdaptationRow = null;
        private Button postVictoryFastForwardButton = null;
        private Button postVictorySkipToEndButton = null;
        private Button postVictoryForcedResultButton = null;
        private bool postVictoryTestingEnabled;
        private bool postVictorySkipToEnd;
        private int postVictoryFastForwardRounds;
        private ForcedGameResultMode postVictoryForcedResult = ForcedGameResultMode.Natural;
        private int? postVictoryForcedMycovariantId;
        private string postVictoryForcedAdaptationId = string.Empty;
        private List<AdaptationDefinition> postVictorySortedAdaptations = new();
        private GameObject detailsOverlayRoot;
        private CanvasGroup detailsOverlayCanvasGroup;
        private Image detailsOverlayBackground;
        private Image detailsCardBackgroundImage;
        private Image pendingRewardBreadBackground;
        private TextMeshProUGUI detailsTitleText;
        private TextMeshProUGUI detailsSubtitleText;
        private Image detailsPlayerIconImage;
        private Button detailsCloseButton;
        private ScrollRect detailsScrollRect;
        private RectTransform detailsScrollContent;
        private RectTransform detailsCardRect;
        private Button detailsBackdropButton;
        private EndgamePlayerStatisticsSnapshot currentPlayerStatistics = EndgamePlayerStatisticsSnapshot.Empty;

        /// <summary>
        /// Call once after the panel is created to wire up dependencies without reaching
        /// into GameManager.Instance.
        /// </summary>
        public void SetDependencies(GameUIManager ui, System.Action campaignResume, System.Action exitToModeSelect)
        {
            gameUI = ui;
            onCampaignResume = campaignResume;
            onExitToModeSelect = exitToModeSelect;
        }

        /* ─────────── Unity ─────────── */
        private void Awake()
        {
            if (playerResultRowPrefab == null)
                Debug.LogError("UI_EndGamePanel: PlayerResultRowPrefab reference is missing!");

            ApplyStyle();
            ApplyTooltips();

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueCampaign);
            else
                Debug.LogWarning("UI_EndGamePanel: ContinueButton reference is missing (campaign mid-run victories will have no continue).");

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnReturnToMainMenu);
            else
                Debug.LogWarning("UI_EndGamePanel: PlayAgainButton reference is missing (player cannot return to main menu).");

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitGame);
            else
                Debug.LogWarning("UI_EndGamePanel: ExitButton reference is missing (player cannot exit results).");

            EnsureDetailsModal();
            EnsurePendingRewardBreadBackground();

            HideInstant();
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsDetailsModalOpen || !UnityInputAdapter.WasEscapePressedThisFrame())
            {
                return;
            }

            HidePlayerDetails();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !runtimeLayoutRefreshQueued || isRefreshingRuntimeLayout)
            {
                return;
            }

            ProcessPendingRuntimeEndGameLayoutRefresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || isRefreshingRuntimeLayout)
            {
                return;
            }

            RefreshRuntimeEndGameLayout();
        }

        private void ApplyStyle()
        {
            if (panelBackground == null)
            {
                panelBackground = GetComponent<Image>();
            }

            if (panelBackground != null)
            {
                panelBackground.color = UIStyleTokens.Surface.OverlayDim;
            }

            ApplyPendingRewardBackgroundMode(false);

            if (resultsCardBackground != null)
            {
                resultsCardBackground.color = UIStyleTokens.Surface.PanelPrimary;
            }

            UIStyleTokens.Button.ApplyStyle(continueButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.ApplyStyle(exitButton);
            UIStyleTokens.Button.ApplyStyle(playAgainButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.SetButtonLabelColor(continueButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(exitButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(playAgainButton, UIStyleTokens.Button.TextDefault);

            EnsureRuntimeLayoutScaffold();
            EnsureButtonLayout(continueButton);
            EnsureButtonLayout(exitButton);
            EnsureButtonLayout(playAgainButton);
            EnsureActionButtonsShareContainer();
            EnsureButtonContainerLayout();
            EnsurePostVictoryTestingControls();
            EnsureDetailsModal();
            UpdatePostVictoryTestingLabels();

            if (outcomeLabel != null)
            {
                EnsureOutcomePlacement();
                outcomeLabel.color = UIStyleTokens.Text.Primary;
                outcomeLabel.enableAutoSizing = true;
                outcomeLabel.fontSizeMax = 52f;
                outcomeLabel.fontSizeMin = 24f;
                outcomeLabel.textWrappingMode = TextWrappingModes.Normal;
                outcomeLabel.overflowMode = TextOverflowModes.Overflow;
                outcomeLabel.alignment = TextAlignmentOptions.Center;

                if (outcomeLabel.rectTransform != null)
                {
                    var labelRect = outcomeLabel.rectTransform;
                    labelRect.anchorMin = new Vector2(OutcomeLabelAnchorMinX, OutcomeLabelAnchorMinY);
                    labelRect.anchorMax = new Vector2(OutcomeLabelAnchorMaxX, OutcomeLabelAnchorMaxY);
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.anchoredPosition = Vector2.zero;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 30f);
            SetOutcomeBannerVisibility(false);
            ApplyControlReadabilityOverrides();
            RefreshRuntimeEndGameLayout();
        }

        private void ApplyTooltips()
        {
            EnsureTooltip(playAgainButton, "Return to the main menu to start a new game.");
            EnsureTooltip(exitButton, "Close the game.");
            EnsureTooltip(continueButton, "Advance to the next campaign level.");
        }

        /* ─────────── Public API (generic solo / hotseat) ─────────── */
        public void ShowResults(List<Player> ranked, GameBoard board, EndgamePlayerStatisticsSnapshot playerStatistics = null)
        {
            SetPostAdaptationConfirmationState(false);
            ShowResultsInternal(ranked, board, playerStatistics, useCampaignTopSpacer: false);
            SetLegacyResultsHeaderVisibility(true);
            SetOutcomeBannerVisibility(false);
            ApplyControlReadabilityOverrides();
            // Solo / hotseat baseline: only exit button (continue hidden)
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (exitButton != null) exitButton.gameObject.SetActive(true);
            if (playAgainButton != null) playAgainButton.gameObject.SetActive(true);
            if (outcomeLabel != null) outcomeLabel.text = ""; // no special messaging
        }

        /// <summary>
        /// Extended results display including campaign outcome context.
        /// </summary>
        public void ShowResultsWithOutcome(
            List<Player> ranked,
            GameBoard board,
            EndgamePlayerStatisticsSnapshot playerStatistics,
            bool isCampaign,
            bool victory,
            bool finalLevel,
            bool hasNextLevel,
            int lostLevelDisplay,
            int completedLevelDisplay,
            bool adaptationPending,
            CampaignVictorySnapshot campaignSnapshot = null,
            IReadOnlyList<AdaptationDefinition> defeatCarryoverOptions = null,
            int defeatCarryoverCapacity = 0)
        {
            SetPostAdaptationConfirmationState(false);
            requiresAdaptationBeforeContinue = false;
            requiresDefeatCarryoverSelection = false;
            requiresMoldinessRewardSelection = false;
            hasPendingDefeatCarryoverEvent = false;
            selectedMoldinessRewardId = null;
            selectedDefeatCarryoverAdaptationIds.Clear();
            defeatCarryoverSelectionCapacity = 0;
            pendingDefeatCarryoverOptions.Clear();
            pendingDefeatCarryoverEntryMode = DefeatCarryoverEntryMode.ImmediateLossScreen;
            cachedCampaignVictorySnapshot = null;
            showPostAdaptationConfirmationAfterMoldinessRewardSelection = false;
            if (!isCampaign)
            {
                ShowResultsInternal(ranked, board, playerStatistics, useCampaignTopSpacer: true);
                // fallback to base behavior
                SetLegacyResultsHeaderVisibility(true);
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                if (exitButton != null) exitButton.gameObject.SetActive(true);
                if (outcomeLabel != null) outcomeLabel.text = "";
                SetOutcomeBannerVisibility(false);
                UpdatePostVictoryTestingVisibility(false);
                return;
            }

            int levelDisplay = victory ? completedLevelDisplay : lostLevelDisplay;
            var presentationSnapshot = EnsureCampaignOutcomePresentationSnapshot(campaignSnapshot, victory, levelDisplay);
            cachedCampaignVictorySnapshot = presentationSnapshot;
            ShowCampaignOutcomeRows(ranked, board, playerStatistics, presentationSnapshot, victory);
            SetLegacyResultsHeaderVisibility(false);
            SetOutcomeBannerVisibility(true);

            // Campaign messaging
            if (outcomeLabel != null)
            {
                if (!victory)
                {
                    // defeat – show lost level index (1-based)
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Danger)}><b>Campaign lost</b></color>\n" +
                        $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Level {lostLevelDisplay}</color></size>";
                }
                else if (finalLevel)
                {
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Campaign complete</b></color>\n" +
                        $"<size={CampaignOutcomeSubtitleFontSize}><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Congratulations you mycelial mastermind! You won the campaign!</color></size>";
                }
                else
                {
                    // mid-run victory
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Level {completedLevelDisplay} cleared</b></color>\n" +
                        $"<size={CampaignOutcomeSubtitleFontSize}><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Select an Adaptation to continue the campaign.</color></size>";
                }
            }

            // Buttons
            requiresAdaptationBeforeContinue = adaptationPending;
            requiresDefeatCarryoverSelection = false;
            requiresMoldinessRewardSelection = false;
            selectedMoldinessRewardId = null;
            selectedDefeatCarryoverAdaptationIds.Clear();
            moldinessRewardOptionBackgrounds.Clear();
            moldinessRewardOptionVisuals.Clear();
            defeatCarryoverSelectionCapacity = 0;
            if (continueButton != null)
                continueButton.gameObject.SetActive(victory && !finalLevel && hasNextLevel);
            if (exitButton != null)
                exitButton.gameObject.SetActive(true);
            if (playAgainButton != null)
                playAgainButton.gameObject.SetActive(!requiresAdaptationBeforeContinue);

            if (!victory)
            {
                ConfigurePendingDefeatCarryoverEvent(defeatCarryoverOptions, defeatCarryoverCapacity, DefeatCarryoverEntryMode.ImmediateLossScreen);

                if (hasPendingDefeatCarryoverEvent)
                {
                    if (continueButton != null)
                    {
                        continueButton.gameObject.SetActive(true);
                        SetButtonLabel(continueButton, "Preserve Spores for Next Run");
                    }

                    if (playAgainButton != null)
                    {
                        playAgainButton.gameObject.SetActive(true);
                        SetButtonLabel(playAgainButton, "Main Menu");
                    }
                }
                else if (playAgainButton != null)
                {
                    SetButtonLabel(playAgainButton, "Main Menu");
                }
            }

            if (continueButton != null)
            {
                SetButtonLabel(continueButton, requiresAdaptationBeforeContinue ? "Select Adaptation" : "Continue Campaign");
            }

            if (!victory && hasPendingDefeatCarryoverEvent && continueButton != null)
            {
                SetButtonLabel(continueButton, "Preserve Spores for Next Run");
            }

            ApplyControlReadabilityOverrides();

            bool canContinueToNextLevel = victory && !finalLevel && hasNextLevel;
            UpdatePostVictoryTestingVisibility(canContinueToNextLevel && !requiresAdaptationBeforeContinue);
        }

        /* ─────────── Internal Row Builder ─────────── */
        private void ShowResultsInternal(List<Player> ranked, GameBoard board, EndgamePlayerStatisticsSnapshot playerStatistics, bool useCampaignTopSpacer)
        {
            PreparePanelForContentBuild();
            HidePlayerDetails();
            currentPlayerStatistics = playerStatistics ?? EndgamePlayerStatisticsSnapshot.Empty;

            /* clear previous rows */
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            if (useCampaignTopSpacer)
            {
                BuildCampaignTopSpacer();
            }

            BuildResultsHeader();

            var summaries = BoardUtilities.GetPlayerBoardSummaries(ranked, board);

            /* build rows */
            int rank = 1;
            foreach (var p in ranked)
            {
                var row = Instantiate(playerResultRowPrefab, resultsContainer);
                var summary = summaries[p.PlayerId];
                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetIcon(p)
                    : GameManager.Instance.GameUI.PlayerUIBinder.GetIcon(p);

                int capturedRank = rank;
                Player capturedPlayer = p;
                Sprite capturedIcon = icon;

                row.Populate(
                    rank,
                    icon,
                    p.PlayerName,
                    summary.LivingCells,
                    summary.ResistantCells,
                    summary.DeadCells,
                    summary.ToxinCells,
                    () => ShowPlayerDetails(capturedPlayer, capturedRank, capturedIcon));
                rank++;
            }

            ApplyControlReadabilityOverrides();
            resetResultsScrollPositionOnNextLayout = true;
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("UI_EndGamePanel is still inactive – coroutine skipped.");
                return;
            }

            /* fade-in */
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f, 0.25f));
        }

        private void ShowCampaignOutcomeRows(
            List<Player> ranked,
            GameBoard board,
            EndgamePlayerStatisticsSnapshot playerStatistics,
            CampaignVictorySnapshot campaignSnapshot,
            bool victory)
        {
            PreparePanelForContentBuild();
            HidePlayerDetails();
            currentPlayerStatistics = playerStatistics ?? EndgamePlayerStatisticsSnapshot.Empty;

            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            BuildCampaignTopSpacer();

            var contentColumns = new GameObject("UI_CampaignVictoryContentColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            contentColumns.transform.SetParent(resultsContainer, false);

            var columnsLayout = contentColumns.GetComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 16f;
            columnsLayout.padding = new RectOffset(0, 0, 0, 0);
            columnsLayout.childAlignment = TextAnchor.UpperCenter;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;

            var columnsElement = contentColumns.GetComponent<LayoutElement>();
            columnsElement.flexibleWidth = 1f;
            columnsElement.preferredHeight = -1f;

            var resultsColumn = new GameObject("UI_CampaignVictoryResultsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ContentSizeFitter));
            resultsColumn.transform.SetParent(contentColumns.transform, false);

            var resultsColumnLayout = resultsColumn.GetComponent<VerticalLayoutGroup>();
            resultsColumnLayout.spacing = 6f;
            resultsColumnLayout.padding = new RectOffset(0, 0, 0, 0);
            resultsColumnLayout.childAlignment = TextAnchor.UpperCenter;
            resultsColumnLayout.childControlWidth = true;
            resultsColumnLayout.childControlHeight = true;
            resultsColumnLayout.childForceExpandWidth = true;
            resultsColumnLayout.childForceExpandHeight = false;

            var resultsColumnElement = resultsColumn.GetComponent<LayoutElement>();
            resultsColumnElement.flexibleWidth = 1f;
            resultsColumnElement.preferredWidth = 0f;

            var resultsColumnFitter = resultsColumn.GetComponent<ContentSizeFitter>();
            resultsColumnFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultsColumnFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            BuildResultsHeader(resultsColumn.transform);

            var summaries = BoardUtilities.GetPlayerBoardSummaries(ranked, board);
            int rank = 1;
            foreach (var player in ranked)
            {
                var row = Instantiate(playerResultRowPrefab, resultsColumn.transform);
                var summary = summaries[player.PlayerId];
                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetIcon(player)
                    : GameManager.Instance.GameUI.PlayerUIBinder.GetIcon(player);

                int capturedRank = rank;
                Player capturedPlayer = player;
                Sprite capturedIcon = icon;

                row.Populate(
                    rank,
                    icon,
                    player.PlayerName,
                    summary.LivingCells,
                    summary.ResistantCells,
                    summary.DeadCells,
                    summary.ToxinCells,
                    () => ShowPlayerDetails(capturedPlayer, capturedRank, capturedIcon));
                rank++;
            }

            BuildCampaignMoldinessSummaryContent(campaignSnapshot, victory, contentColumns.transform);

            ApplyControlReadabilityOverrides();
            resetResultsScrollPositionOnNextLayout = true;
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("UI_EndGamePanel is still inactive – coroutine skipped.");
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f, 0.25f));
        }

        private static CampaignVictorySnapshot EnsureCampaignOutcomePresentationSnapshot(CampaignVictorySnapshot snapshot, bool victory, int levelDisplay)
        {
            snapshot ??= new CampaignVictorySnapshot();
            snapshot.clearedLevelDisplay = levelDisplay;

            var moldinessState = GameManager.Instance?.CampaignController?.State?.moldiness;
            var moldinessSnapshot = GetSnapshot(moldinessState);

            snapshot.moldinessAwarded = victory ? GetRewardForClearedLevel(levelDisplay) : 0;
            snapshot.moldinessProgressAfterAward = moldinessSnapshot.CurrentProgress;
            snapshot.moldinessThresholdAfterAward = moldinessSnapshot.CurrentThreshold;
            snapshot.moldinessTierAfterAward = moldinessSnapshot.CurrentTierIndex;
            snapshot.pendingMoldinessUnlockCount = moldinessSnapshot.PendingUnlockCount;

            if (!victory)
            {
                snapshot.moldinessProgressBeforeAward = moldinessSnapshot.CurrentProgress;
                snapshot.moldinessTierBeforeAward = moldinessSnapshot.CurrentTierIndex;
            }

            return snapshot;
        }

        public void ShowCampaignPendingDefeatCarryoverSelection(
            IReadOnlyList<AdaptationDefinition> options,
            int selectionCapacity,
            DefeatCarryoverEntryMode entryMode = DefeatCarryoverEntryMode.ImmediateLossScreen)
        {
            SetPostAdaptationConfirmationState(false);
            ShowDefeatCarryoverSelectionRows(options, selectionCapacity);
            SetLegacyResultsHeaderVisibility(false);
            SetOutcomeBannerVisibility(true);
            ApplyPendingRewardBackgroundMode(false);
            requiresAdaptationBeforeContinue = false;
            requiresDefeatCarryoverSelection = true;
            requiresMoldinessRewardSelection = false;
            hasPendingDefeatCarryoverEvent = false;
            selectedMoldinessRewardId = null;
            defeatCarryoverSelectionCapacity = Mathf.Max(0, Mathf.Min(selectionCapacity, options?.Count ?? 0));
            selectedDefeatCarryoverAdaptationIds.Clear();
            defeatCarryoverOptionImages.Clear();
            moldinessRewardOptionBackgrounds.Clear();
            moldinessRewardOptionVisuals.Clear();
            pendingDefeatCarryoverOptions.Clear();
            pendingDefeatCarryoverEntryMode = entryMode;

            if (outcomeLabel != null)
            {
                string subtitle = entryMode == DefeatCarryoverEntryMode.DeferredResumePrompt
                    ? $"These spores are waiting from your last failed campaign. Choose {defeatCarryoverSelectionCapacity} adaptation{Pluralize(defeatCarryoverSelectionCapacity)} to carry into your next run."
                    : $"Choose {defeatCarryoverSelectionCapacity} adaptation{Pluralize(defeatCarryoverSelectionCapacity)} to carry into your next run.";
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Warning)}><b>Preserve your spores</b></color>\n" +
                    $"<size={CampaignOutcomeSubtitleFontSize}><color=#{ToHex(UIStyleTokens.Text.Secondary)}>{subtitle}</color></size>";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Confirm Carryover");
            }

            RefreshDefeatCarryoverSelectionUi();

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
                SetButtonLabel(playAgainButton, "Main Menu");
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            UpdatePostVictoryTestingVisibility(false);
        }

        public void ShowCampaignPendingMoldinessRewardSelection(CampaignVictorySnapshot snapshot, IReadOnlyList<MoldinessUnlockDefinition> offers)
        {
            ShowCampaignPendingMoldinessRewardSelection(
                snapshot,
                offers,
                returnToCampaignMenuAfterSelection: false,
                showAdaptationConfirmationAfterSelection: false);
        }

        public void ShowCampaignPendingMoldinessRewardSelection(CampaignVictorySnapshot snapshot, IReadOnlyList<MoldinessUnlockDefinition> offers, bool returnToCampaignMenuAfterSelection)
        {
            ShowCampaignPendingMoldinessRewardSelection(
                snapshot,
                offers,
                returnToCampaignMenuAfterSelection,
                showAdaptationConfirmationAfterSelection: false);
        }

        public void ShowCampaignPendingMoldinessRewardSelection(
            CampaignVictorySnapshot snapshot,
            IReadOnlyList<MoldinessUnlockDefinition> offers,
            bool returnToCampaignMenuAfterSelection,
            bool showAdaptationConfirmationAfterSelection)
        {
            SetPostAdaptationConfirmationState(false);
            if (snapshot == null)
            {
                return;
            }

            ShowMoldinessRewardSelectionRows(snapshot, offers);
            SetLegacyResultsHeaderVisibility(false);
            SetOutcomeBannerVisibility(true);
            ApplyPendingRewardBackgroundMode(true);
            requiresAdaptationBeforeContinue = false;
            requiresDefeatCarryoverSelection = false;
            requiresMoldinessRewardSelection = true;
            returnToCampaignMenuAfterMoldinessReward = returnToCampaignMenuAfterSelection;
            showPostAdaptationConfirmationAfterMoldinessRewardSelection = showAdaptationConfirmationAfterSelection;
            cachedCampaignVictorySnapshot = snapshot;
            selectedMoldinessRewardId = null;
            defeatCarryoverSelectionCapacity = 0;
            selectedDefeatCarryoverAdaptationIds.Clear();
            moldinessRewardOptionBackgrounds.Clear();
            moldinessRewardOptionVisuals.Clear();

            if (outcomeLabel != null)
            {
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Warning)}><b>Moldiness threshold reached</b></color>\n" +
                    $"<size={CampaignOutcomeSubtitleFontSize}><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Choose one moldiness reward before the normal adaptation draft.</color></size>";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = false;
                SetButtonLabel(continueButton, "Choose Moldiness Reward");
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
                SetButtonLabel(playAgainButton, "Main Menu");
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
                SetButtonLabel(exitButton, "Exit Game");
            }

            UpdatePostVictoryTestingVisibility(false);
        }

        public void ShowCampaignPendingVictorySnapshot(CampaignVictorySnapshot snapshot)
        {
            SetPostAdaptationConfirmationState(false);
            if (snapshot == null)
            {
                return;
            }

            cachedCampaignVictorySnapshot = snapshot;
            showPostAdaptationConfirmationAfterMoldinessRewardSelection = false;
            ShowSnapshotRows(snapshot);
            SetLegacyResultsHeaderVisibility(false);
            SetOutcomeBannerVisibility(true);
            ApplyPendingRewardBackgroundMode(false);
            requiresAdaptationBeforeContinue = true;
            requiresDefeatCarryoverSelection = false;
            requiresMoldinessRewardSelection = false;
            defeatCarryoverSelectionCapacity = 0;
            selectedDefeatCarryoverAdaptationIds.Clear();

            if (outcomeLabel != null)
            {
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Level {snapshot.clearedLevelDisplay} cleared</b></color>\n" +
                    $"<size={CampaignOutcomeSubtitleFontSize}><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Select an Adaptation to continue the campaign.</color></size>";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Select Adaptation");
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(false);
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            UpdatePostVictoryTestingVisibility(false);
        }

        private void ShowSnapshotRows(CampaignVictorySnapshot snapshot)
        {
            PreparePanelForContentBuild();
            HidePlayerDetails();
            currentPlayerStatistics = EndgamePlayerStatisticsSnapshot.Empty;

            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            BuildCampaignTopSpacer();

            var contentColumns = new GameObject("UI_CampaignVictoryContentColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            contentColumns.transform.SetParent(resultsContainer, false);

            var columnsLayout = contentColumns.GetComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 16f;
            columnsLayout.padding = new RectOffset(0, 0, 0, 0);
            columnsLayout.childAlignment = TextAnchor.UpperCenter;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;

            var columnsElement = contentColumns.GetComponent<LayoutElement>();
            columnsElement.flexibleWidth = 1f;
            columnsElement.preferredHeight = -1f;

            var resultsColumn = new GameObject("UI_CampaignVictoryResultsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ContentSizeFitter));
            resultsColumn.transform.SetParent(contentColumns.transform, false);

            var resultsColumnLayout = resultsColumn.GetComponent<VerticalLayoutGroup>();
            resultsColumnLayout.spacing = 6f;
            resultsColumnLayout.padding = new RectOffset(0, 0, 0, 0);
            resultsColumnLayout.childAlignment = TextAnchor.UpperCenter;
            resultsColumnLayout.childControlWidth = true;
            resultsColumnLayout.childControlHeight = true;
            resultsColumnLayout.childForceExpandWidth = true;
            resultsColumnLayout.childForceExpandHeight = false;

            var resultsColumnElement = resultsColumn.GetComponent<LayoutElement>();
            resultsColumnElement.flexibleWidth = 1f;
            resultsColumnElement.preferredWidth = 0f;

            var resultsColumnFitter = resultsColumn.GetComponent<ContentSizeFitter>();
            resultsColumnFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultsColumnFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            BuildResultsHeader(resultsColumn.transform);

            for (int i = 0; i < snapshot.rows.Count; i++)
            {
                var rowData = snapshot.rows[i];
                var row = Instantiate(playerResultRowPrefab, resultsColumn.transform);

                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetPlayerIcon(rowData.playerId)
                    : GameManager.Instance?.GameUI?.PlayerUIBinder?.GetPlayerIcon(rowData.playerId);

                var player = ResolvePlayerForDetails(rowData.playerId);
                int capturedRank = rowData.rank;
                Sprite capturedIcon = icon;

                row.Populate(
                    rowData.rank,
                    icon,
                    rowData.playerName,
                    rowData.livingCells,
                    rowData.resistantCells,
                    rowData.deadCells,
                    rowData.toxinCells,
                    player != null ? () => ShowPlayerDetails(player, capturedRank, capturedIcon) : null);
            }

            BuildCampaignMoldinessSummaryContent(snapshot, victory: true, contentColumns.transform);

            ApplyControlReadabilityOverrides();
            resetResultsScrollPositionOnNextLayout = true;
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void ShowDefeatCarryoverSelectionRows(IReadOnlyList<AdaptationDefinition> options, int selectionCapacity)
        {
            PreparePanelForContentBuild();
            HidePlayerDetails();
            currentPlayerStatistics = EndgamePlayerStatisticsSnapshot.Empty;

            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            BuildCampaignTopSpacer();
            BuildDefeatCarryoverSelectionContent(options, selectionCapacity);

            ApplyControlReadabilityOverrides();
            resetResultsScrollPositionOnNextLayout = true;
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void ShowMoldinessRewardSelectionRows(CampaignVictorySnapshot snapshot, IReadOnlyList<MoldinessUnlockDefinition> offers)
        {
            PreparePanelForContentBuild();
            HidePlayerDetails();
            currentPlayerStatistics = EndgamePlayerStatisticsSnapshot.Empty;

            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            moldinessRewardOptionBackgrounds.Clear();
            moldinessRewardOptionVisuals.Clear();
            BuildCampaignTopSpacer();
            BuildMoldinessRewardSelectionContent(snapshot, offers);

            ApplyControlReadabilityOverrides();
            resetResultsScrollPositionOnNextLayout = true;
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void BuildDefeatCarryoverSelectionContent(IReadOnlyList<AdaptationDefinition> options, int selectionCapacity)
        {
            if (resultsContainer == null)
            {
                return;
            }

            var root = new GameObject("UI_DefeatCarryoverSelectionRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            root.transform.SetParent(resultsContainer, false);

            var rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 18f;
            rootLayout.padding = new RectOffset(18, 18, 8, 8);
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            var title = CreateCarryoverInfoText(root.transform,
                "Choose the adaptations you want to preserve for your next campaign run.",
                28f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;

            var subtitle = CreateCarryoverInfoText(root.transform,
                $"Selected 0 / {Mathf.Max(0, selectionCapacity)}. Click icons to choose your carryover adaptations. Hover to inspect details.",
                22f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            subtitle.alignment = TextAlignmentOptions.Center;
            defeatCarryoverSelectionStatusLabel = subtitle;

            var gridRoot = new GameObject("UI_DefeatCarryoverSelectionGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridRoot.transform.SetParent(root.transform, false);

            var gridRect = gridRoot.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);

            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 112f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Clamp(options?.Count ?? 1, 1, 4);
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = gridRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            if (options != null)
            {
                foreach (var adaptation in options)
                {
                    CreateDefeatCarryoverOptionButton(gridRoot.transform, adaptation, selectionCapacity);
                }
            }
        }

        private void CreateDefeatCarryoverOptionButton(Transform parent, AdaptationDefinition adaptation, int selectionCapacity)
        {
            if (adaptation == null || parent == null)
            {
                return;
            }

            var optionObject = new GameObject($"UI_DefeatCarryover_{adaptation.Id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            optionObject.transform.SetParent(parent, false);

            var optionImage = optionObject.GetComponent<Image>();
            optionImage.color = Color.white;
            optionImage.sprite = AdaptationArtRepository.GetIcon(adaptation);
            optionImage.type = Image.Type.Simple;
            optionImage.preserveAspect = true;

            var layout = optionObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 112f;
            layout.minWidth = 112f;
            layout.preferredHeight = 112f;
            layout.minHeight = 112f;

            var provider = optionObject.AddComponent<AdaptationTooltipProvider>();
            provider.Initialize(adaptation);

            var tooltipTrigger = optionObject.AddComponent<TooltipTrigger>();
            tooltipTrigger.SetDynamicProvider(provider);
            tooltipTrigger.SetAutoPlacementOffsetX(20f);

            var button = optionObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = null;
            button.onClick.AddListener(() => ToggleDefeatCarryoverSelection(adaptation.Id, optionImage, selectionCapacity));

            defeatCarryoverOptionImages[adaptation.Id] = optionImage;
            UpdateDefeatCarryoverOptionVisual(optionImage, false);
        }

        private void ToggleDefeatCarryoverSelection(string adaptationId, Image optionImage, int selectionCapacity)
        {
            if (string.IsNullOrWhiteSpace(adaptationId))
            {
                return;
            }

            int requiredSelectionCount = Mathf.Max(0, selectionCapacity);
            bool isSelected = selectedDefeatCarryoverAdaptationIds.Contains(adaptationId);
            if (isSelected)
            {
                selectedDefeatCarryoverAdaptationIds.Remove(adaptationId);
                UpdateDefeatCarryoverOptionVisual(optionImage, false);
                RefreshDefeatCarryoverSelectionUi();
                return;
            }

            if (selectedDefeatCarryoverAdaptationIds.Count >= requiredSelectionCount)
            {
                if (requiredSelectionCount == 1 && selectedDefeatCarryoverAdaptationIds.Count == 1)
                {
                    string currentlySelectedId = selectedDefeatCarryoverAdaptationIds.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(currentlySelectedId)
                        && !string.Equals(currentlySelectedId, adaptationId, StringComparison.Ordinal))
                    {
                        selectedDefeatCarryoverAdaptationIds.Remove(currentlySelectedId);
                        if (defeatCarryoverOptionImages.TryGetValue(currentlySelectedId, out var previousImage))
                        {
                            UpdateDefeatCarryoverOptionVisual(previousImage, false);
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            selectedDefeatCarryoverAdaptationIds.Add(adaptationId);
            UpdateDefeatCarryoverOptionVisual(optionImage, true);
            RefreshDefeatCarryoverSelectionUi();
        }

        private static void UpdateDefeatCarryoverOptionVisual(Image optionImage, bool isSelected)
        {
            if (optionImage == null)
            {
                return;
            }

            optionImage.color = Color.white;
            optionImage.material = null;

            var outline = optionImage.GetComponent<Outline>();
            if (outline == null)
            {
                outline = optionImage.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = isSelected
                ? new Color(UIStyleTokens.State.Success.r, UIStyleTokens.State.Success.g, UIStyleTokens.State.Success.b, 0.95f)
                : new Color(UIStyleTokens.Text.Primary.r, UIStyleTokens.Text.Primary.g, UIStyleTokens.Text.Primary.b, 0f);
            outline.effectDistance = isSelected ? new Vector2(4f, -4f) : Vector2.zero;
        }

        private void RefreshDefeatCarryoverSelectionUi()
        {
            int selectedCount = selectedDefeatCarryoverAdaptationIds.Count;
            int requiredCount = Mathf.Max(0, defeatCarryoverSelectionCapacity);

            if (defeatCarryoverSelectionStatusLabel != null)
            {
                defeatCarryoverSelectionStatusLabel.text = requiredCount > 0
                    ? $"Selected {selectedCount} / {requiredCount}. Click icons to choose exactly {requiredCount} carryover adaptation{Pluralize(requiredCount)}."
                    : "No carryover adaptations are available for this run.";
                defeatCarryoverSelectionStatusLabel.color = selectedCount >= requiredCount
                    ? UIStyleTokens.State.Success
                    : UIStyleTokens.Text.Secondary;
            }

            if (continueButton != null)
            {
                continueButton.interactable = selectedCount == requiredCount;
            }
        }

        private static TextMeshProUGUI CreateCarryoverInfoText(Transform parent, string text, float fontSize, Color color, FontStyles fontStyle)
        {
            var textObject = new GameObject("UI_DefeatCarryoverInfoText", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var layout = textObject.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredWidth = 300f;
            layout.minWidth = 300f;
            layout.preferredHeight = -1f;
            layout.minHeight = fontSize + 12f;

            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, rect.sizeDelta.y);

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.overflowMode = TextOverflowModes.Overflow;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private void BuildCampaignMoldinessSummaryContent(CampaignVictorySnapshot snapshot, bool victory, Transform parentOverride = null)
        {
            var parent = parentOverride ?? resultsContainer;
            if (parent == null || snapshot == null)
            {
                return;
            }

            var root = new GameObject("UI_CampaignMoldinessSummaryRoot", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ContentSizeFitter));
            root.transform.SetParent(parent, false);

            var background = root.GetComponent<Image>();
            var backgroundColor = UIStyleTokens.Surface.PanelSecondary;
            backgroundColor.a = 0.5f;
            background.color = backgroundColor;
            background.raycastTarget = false;

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var element = root.GetComponent<LayoutElement>();
            element.flexibleWidth = 0f;
            element.preferredWidth = 320f;
            element.minWidth = 300f;
            element.minHeight = 180f;

            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            int currentLevel = snapshot.moldinessTierAfterAward + 1;
            int progressAfter = Mathf.Clamp(snapshot.moldinessProgressAfterAward, 0, Math.Max(1, snapshot.moldinessThresholdAfterAward));
            int threshold = Math.Max(1, snapshot.moldinessThresholdAfterAward);

            var title = CreateCarryoverInfoText(root.transform,
                victory ? $"+{snapshot.moldinessAwarded} Moldiness" : "Moldiness progression",
                24f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;

            var status = CreateCarryoverInfoText(root.transform,
                $"Moldiness Level {currentLevel}  •  {progressAfter} / {threshold} to next threshold",
                18f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            status.alignment = TextAlignmentOptions.Center;

            BuildMoldinessToastGrid(root.transform, progressAfter, threshold);

            string thresholdMessage = snapshot.pendingMoldinessUnlockCount > 0
                ? $"Threshold reached. {snapshot.pendingMoldinessUnlockCount} moldiness reward{Pluralize(snapshot.pendingMoldinessUnlockCount)} pending."
                : (snapshot.moldinessAwarded > 0
                    ? "No new threshold crossed this run."
                    : "No moldiness gained this run.");

            var detail = CreateCarryoverInfoText(root.transform,
                thresholdMessage,
                16f,
                snapshot.pendingMoldinessUnlockCount > 0 ? UIStyleTokens.State.Warning : UIStyleTokens.Text.Secondary,
                snapshot.pendingMoldinessUnlockCount > 0 ? FontStyles.Bold : FontStyles.Normal);
            detail.alignment = TextAlignmentOptions.Center;
        }

        private void BuildMoldinessToastGrid(Transform parent, int progress, int threshold)
        {
            if (parent == null)
            {
                return;
            }

            moldinessSummaryToastTiles.Clear();
            var gridRoot = new GameObject("UI_CampaignMoldinessSummaryToastGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            gridRoot.transform.SetParent(parent, false);

            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(threshold)), 3, 8);
            int rows = Mathf.Max(1, Mathf.CeilToInt(threshold / (float)columns));
            float maxGridWidth = 220f;
            float maxGridHeight = 112f;
            float spacing = threshold <= 12 ? 6f : 4f;
            float cellWidth = Mathf.Clamp((maxGridWidth - ((columns - 1) * spacing)) / columns, 14f, 34f);
            float cellHeight = Mathf.Clamp((maxGridHeight - ((rows - 1) * spacing)) / rows, 14f, 34f);

            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            grid.spacing = new Vector2(spacing, spacing);
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = gridRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var element = gridRoot.GetComponent<LayoutElement>();
            element.minWidth = maxGridWidth;
            element.preferredWidth = maxGridWidth;
            element.minHeight = Mathf.Max(56f, (rows * cellHeight) + ((rows - 1) * spacing));
            element.preferredHeight = -1f;

            int tileCount = Math.Max(1, threshold);
            int filledCount = Mathf.Clamp(progress, 0, tileCount);
            var orderedIndices = Enumerable.Range(0, tileCount)
                .OrderBy(index => GetEndgameToastTileSortKey(index, tileCount))
                .ThenBy(index => index)
                .ToList();
            var filledIndices = new HashSet<int>(orderedIndices.Take(filledCount));

            for (int i = 0; i < tileCount; i++)
            {
                var tileObject = new GameObject($"UI_CampaignMoldinessSummaryTile_{i + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                tileObject.transform.SetParent(gridRoot.transform, false);

                var image = tileObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = filledIndices.Contains(i) ? UIStyleTokens.Accent.Lichen : UIStyleTokens.Surface.PanelPrimary;
                moldinessSummaryToastTiles.Add(image);

                var tileLayout = tileObject.GetComponent<LayoutElement>();
                tileLayout.minWidth = cellWidth;
                tileLayout.preferredWidth = cellWidth;
                tileLayout.minHeight = cellHeight;
                tileLayout.preferredHeight = cellHeight;
            }
        }

        private static float GetEndgameToastTileSortKey(int index, int threshold)
        {
            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(threshold)), 3, 8);
            int row = index / columns;
            int column = index % columns;
            float centerColumn = (columns - 1) * 0.5f;
            float columnDistance = Mathf.Abs(column - centerColumn);
            return (row * 10f) + columnDistance + ((index * 37) % 11) * 0.01f;
        }

        private void BuildMoldinessRewardSelectionContent(CampaignVictorySnapshot snapshot, IReadOnlyList<MoldinessUnlockDefinition> offers)
        {
            if (resultsContainer == null)
            {
                return;
            }

            var root = new GameObject("UI_MoldinessRewardSelectionRoot", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ContentSizeFitter));
            root.transform.SetParent(resultsContainer, false);

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            var rootBackground = root.GetComponent<Image>();
            var rootBackgroundColor = UIStyleTokens.Surface.PanelSecondary;
            rootBackgroundColor.a = 0.42f;
            rootBackground.color = rootBackgroundColor;
            rootBackground.raycastTarget = false;

            var rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12f;
            rootLayout.padding = new RectOffset(16, 16, 14, 14);
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            var rootElement = root.GetComponent<LayoutElement>();
            rootElement.flexibleHeight = 1f;
            rootElement.flexibleWidth = 1f;
            rootElement.minHeight = 240f;

            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var info = CreateCarryoverInfoText(root.transform,
                "Win campaign games to unlock rewards and permanent improvements that benefit all future campaign runs.",
                18f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            info.alignment = TextAlignmentOptions.Center;
            info.enableAutoSizing = true;
            info.fontSizeMax = 18f;
            info.fontSizeMin = 16f;

            if (offers == null || offers.Count == 0)
            {
                var emptyText = CreateCarryoverInfoText(root.transform,
                    "No moldiness rewards are currently available for this threshold.",
                    22f,
                    UIStyleTokens.State.Warning,
                    FontStyles.Italic);
                emptyText.alignment = TextAlignmentOptions.Center;
                return;
            }

            var offersColumn = new GameObject("UI_MoldinessRewardSelectionOffers", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ContentSizeFitter));
            offersColumn.transform.SetParent(root.transform, false);

            var offersRect = offersColumn.GetComponent<RectTransform>();
            offersRect.anchorMin = new Vector2(0f, 1f);
            offersRect.anchorMax = new Vector2(1f, 1f);
            offersRect.pivot = new Vector2(0.5f, 1f);
            offersRect.sizeDelta = Vector2.zero;

            var offersLayout = offersColumn.GetComponent<VerticalLayoutGroup>();
            offersLayout.spacing = 6f;
            offersLayout.childAlignment = TextAnchor.UpperCenter;
            offersLayout.childControlWidth = true;
            offersLayout.childControlHeight = true;
            offersLayout.childForceExpandWidth = false;
            offersLayout.childForceExpandHeight = false;

            var offersElement = offersColumn.GetComponent<LayoutElement>();
            offersElement.flexibleWidth = 0f;
            offersElement.preferredWidth = 930f;
            offersElement.minWidth = 930f;
            offersElement.preferredHeight = -1f;

            var offersFitter = offersColumn.GetComponent<ContentSizeFitter>();
            offersFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            offersFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            foreach (var offer in offers)
            {
                CreateMoldinessRewardOptionButton(offersColumn.transform, offer);
            }
        }

        private void CreateMoldinessRewardOptionButton(Transform parent, MoldinessUnlockDefinition offer)
        {
            if (offer == null || parent == null)
            {
                return;
            }

            var buttonObject = new GameObject($"UI_MoldinessReward_{offer.Id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 88f;
            layout.preferredHeight = 88f;
            layout.flexibleWidth = 1f;

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;
            background.raycastTarget = true;
            moldinessRewardOptionBackgrounds.Add(background);

            var fillOverlayObject = new GameObject("FillOverlay", typeof(RectTransform), typeof(Image));
            fillOverlayObject.transform.SetParent(buttonObject.transform, false);
            fillOverlayObject.transform.SetAsFirstSibling();
            var fillOverlayRect = fillOverlayObject.GetComponent<RectTransform>();
            fillOverlayRect.anchorMin = Vector2.zero;
            fillOverlayRect.anchorMax = Vector2.one;
            fillOverlayRect.offsetMin = new Vector2(3f, 3f);
            fillOverlayRect.offsetMax = new Vector2(-3f, -3f);
            var fillOverlay = fillOverlayObject.GetComponent<Image>();
            var selectedTint = UIStyleTokens.Button.BackgroundSelected;
            selectedTint.a = 0.18f;
            fillOverlay.color = selectedTint;
            fillOverlay.raycastTarget = false;
            fillOverlay.enabled = false;
            fillOverlayObject.SetActive(false);
            var fillOverlayOutline = fillOverlayObject.AddComponent<Outline>();
            fillOverlayOutline.effectColor = new Color(UIStyleTokens.Button.BackgroundSelected.r, UIStyleTokens.Button.BackgroundSelected.g, UIStyleTokens.Button.BackgroundSelected.b, 1f);
            fillOverlayOutline.effectDistance = new Vector2(2f, -2f);
            fillOverlayOutline.enabled = false;

            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(offer.AccentColor.r, offer.AccentColor.g, offer.AccentColor.b, 0.35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(buttonObject.transform, false);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = GetMoldinessRewardIcon(offer);
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(56f, 56f);

            var badgeObject = new GameObject("CategoryBadge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(buttonObject.transform, false);
            var badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-10f, -7f);
            badgeRect.sizeDelta = new Vector2(300f, 26f);
            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.color = new Color(offer.AccentColor.r, offer.AccentColor.g, offer.AccentColor.b, 0.18f);

            var visual = new MoldinessRewardOptionVisual
            {
                RewardId = offer.Id,
                Background = background,
                FillOverlay = fillOverlay,
                FillOverlayOutline = fillOverlayOutline,
                Outline = outline,
                BadgeBackground = badgeImage
            };

            moldinessRewardOptionVisuals.Add(visual);

            var badgeLabel = CreateCarryoverInfoText(badgeObject.transform, offer.CategoryLabel ?? string.Empty, 16f, offer.AccentColor, FontStyles.Bold);
            var badgeLabelLayout = badgeLabel.GetComponent<LayoutElement>();
            if (badgeLabelLayout != null)
            {
                badgeLabelLayout.minWidth = 300f;
                badgeLabelLayout.preferredWidth = 300f;
                badgeLabelLayout.flexibleWidth = 0f;
            }
            var badgeLabelRect = badgeLabel.GetComponent<RectTransform>();
            if (badgeLabelRect != null)
            {
                badgeLabelRect.sizeDelta = new Vector2(300f, badgeLabelRect.sizeDelta.y);
            }
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.enableAutoSizing = true;
            badgeLabel.fontSizeMax = 16f;
            badgeLabel.fontSizeMin = 14f;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(buttonObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(80f, -34f);
            titleRect.offsetMax = new Vector2(-316f, -8f);
            var title = titleObject.GetComponent<TextMeshProUGUI>();
            title.text = offer.DisplayName;
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            title.color = UIStyleTokens.Text.Primary;
            title.alignment = TextAlignmentOptions.Left;
            title.enableAutoSizing = true;
            title.fontSizeMax = 22f;
            title.fontSizeMin = 18f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.maxVisibleLines = 1;

            var descriptionObject = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descriptionObject.transform.SetParent(buttonObject.transform, false);
            var descriptionRect = descriptionObject.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.pivot = new Vector2(0f, 0.5f);
            descriptionRect.offsetMin = new Vector2(80f, 10f);
            descriptionRect.offsetMax = new Vector2(-12f, -37f);
            var description = descriptionObject.GetComponent<TextMeshProUGUI>();
            description.text = offer.Description;
            description.fontSize = 17f;
            description.fontStyle = FontStyles.Normal;
            description.color = UIStyleTokens.Text.Secondary;
            description.alignment = TextAlignmentOptions.TopLeft;
            description.enableAutoSizing = true;
            description.fontSizeMax = 17f;
            description.fontSizeMin = 16f;
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Ellipsis;
            description.maxVisibleLines = 2;

            var tooltipTrigger = buttonObject.GetComponent<TooltipTrigger>();
            if (tooltipTrigger == null)
            {
                tooltipTrigger = buttonObject.AddComponent<TooltipTrigger>();
            }

            if (offer.Type == MoldinessUnlockType.UnlockAdaptation && AdaptationRepository.TryGetById(offer.AdaptationId, out var adaptation))
            {
                var provider = buttonObject.AddComponent<AdaptationTooltipProvider>();
                provider.Initialize(adaptation);
                tooltipTrigger.SetDynamicProvider(provider);
                tooltipTrigger.SetAutoPlacementOffsetX(24f);
            }
            else
            {
                var provider = buttonObject.AddComponent<MoldinessRewardTooltipProvider>();
                provider.Initialize(offer);
                tooltipTrigger.SetDynamicProvider(provider);
                tooltipTrigger.SetAutoPlacementOffsetX(24f);
            }

            fillOverlayObject.transform.SetAsLastSibling();

            var button = buttonObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);
            var colors = button.colors;
            colors.normalColor = UIStyleTokens.Surface.PanelElevated;
            colors.highlightedColor = UIStyleTokens.Surface.PanelElevated;
            colors.pressedColor = UIStyleTokens.Surface.PanelElevated;
            colors.selectedColor = UIStyleTokens.Surface.PanelElevated;
            button.colors = colors;
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectMoldinessReward(visual));
        }

        private static Sprite GetMoldinessRewardIcon(MoldinessUnlockDefinition offer)
        {
            if (offer == null)
            {
                return AdaptationArtRepository.GetIcon(null);
            }

            if (offer.Type == MoldinessUnlockType.UnlockAdaptation && AdaptationRepository.TryGetById(offer.AdaptationId, out var adaptation))
            {
                return AdaptationArtRepository.GetIcon(adaptation);
            }

            return ProceduralIconUtility.CreateSprite(
                $"MoldinessReward_{offer.Id}",
                Color.Lerp(offer.AccentColor, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                offer.AccentColor,
                (texture, accent, highlight) =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int centerX = 10 + (i * 10);
                        ProceduralIconUtility.FillCircle(texture, centerX, 12, 3 + i, accent);
                        ProceduralIconUtility.FillCircle(texture, centerX - 2, 22, 2 + i, accent);
                    }

                    for (int y = 8; y < 12; y++)
                    {
                        for (int x = 8; x < 28; x++)
                        {
                            texture.SetPixel(x, y, highlight);
                        }
                    }
                },
                40);
        }

        private void SelectMoldinessReward(MoldinessRewardOptionVisual clickedVisual)
        {
            if (clickedVisual == null)
            {
                return;
            }

            bool togglingOff = string.Equals(selectedMoldinessRewardId, clickedVisual.RewardId, StringComparison.Ordinal);
            selectedMoldinessRewardId = togglingOff ? null : clickedVisual.RewardId;

            for (int i = 0; i < moldinessRewardOptionVisuals.Count; i++)
            {
                ApplyMoldinessRewardOptionVisualState(moldinessRewardOptionVisuals[i], isSelected: false);
            }

            if (!togglingOff)
            {
                ApplyMoldinessRewardOptionVisualState(clickedVisual, isSelected: true);
            }

            EventSystem.current?.SetSelectedGameObject(null);

            if (continueButton != null)
            {
                continueButton.interactable = !togglingOff;
                SetButtonLabel(continueButton, togglingOff ? "Choose Moldiness Reward" : "Claim Moldiness Reward");
            }
        }

        private static void ApplyMoldinessRewardOptionVisualState(MoldinessRewardOptionVisual visual, bool isSelected)
        {
            if (visual == null)
            {
                return;
            }

            if (visual.Background != null)
            {
                visual.Background.color = UIStyleTokens.Surface.PanelElevated;
            }

            if (visual.FillOverlay != null)
            {
                if (isSelected)
                {
                    var selectedTint = UIStyleTokens.Button.BackgroundSelected;
                    selectedTint.a = 0.18f;
                    visual.FillOverlay.color = selectedTint;
                    visual.FillOverlay.enabled = true;
                    visual.FillOverlay.gameObject.SetActive(true);
                }
                else
                {
                    visual.FillOverlay.enabled = false;
                    visual.FillOverlay.gameObject.SetActive(false);
                }
            }

            if (visual.FillOverlayOutline != null)
            {
                visual.FillOverlayOutline.enabled = isSelected;
            }

            if (visual.Outline != null)
            {
                visual.Outline.effectColor = isSelected
                    ? new Color(UIStyleTokens.Button.BackgroundSelected.r, UIStyleTokens.Button.BackgroundSelected.g, UIStyleTokens.Button.BackgroundSelected.b, 1f)
                    : new Color(UIStyleTokens.Text.Muted.r, UIStyleTokens.Text.Muted.g, UIStyleTokens.Text.Muted.b, 0.45f);
                visual.Outline.effectDistance = isSelected ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
            }

            if (visual.BadgeBackground != null)
            {
                visual.BadgeBackground.color = isSelected
                    ? new Color(UIStyleTokens.Button.BackgroundSelected.r, UIStyleTokens.Button.BackgroundSelected.g, UIStyleTokens.Button.BackgroundSelected.b, 0.42f)
                    : new Color(UIStyleTokens.Surface.PanelPrimary.r, UIStyleTokens.Surface.PanelPrimary.g, UIStyleTokens.Surface.PanelPrimary.b, 0.65f);
            }
        }

        /* ─────────── Buttons / Helpers ─────────── */
        private void OnClose()
        {
            // legacy close (non-campaign) – keep ability to just hide panel
            HideInstant();

            // Re-enable the right sidebar so players can see summaries after closing results
            var sidebar = gameUI?.RightSidebar ?? GameManager.Instance?.GameUI?.RightSidebar;
            if (sidebar != null)
            {
                sidebar.gameObject.SetActive(true);
            }
        }

        private void OnContinueCampaign()
        {
            if (hasPendingDefeatCarryoverEvent)
            {
                ShowCampaignPendingDefeatCarryoverSelection(
                    pendingDefeatCarryoverOptions,
                    defeatCarryoverSelectionCapacity,
                    pendingDefeatCarryoverEntryMode);
                return;
            }

            if (requiresDefeatCarryoverSelection)
            {
                var manager = GameManager.Instance;
                var campaignController = manager?.CampaignController;
                if (campaignController == null)
                {
                    return;
                }

                bool confirmed = campaignController.TryConfirmDefeatCarryoverSelection(selectedDefeatCarryoverAdaptationIds.ToList());
                if (!confirmed)
                {
                    return;
                }

                requiresDefeatCarryoverSelection = false;
                HideInstant();
                if (onExitToModeSelect != null)
                    onExitToModeSelect();
                else
                    manager?.ReturnToMainMenu();
                return;
            }

            if (requiresMoldinessRewardSelection)
            {
                var manager = GameManager.Instance;
                var campaignController = manager?.CampaignController;
                if (campaignController == null || string.IsNullOrWhiteSpace(selectedMoldinessRewardId))
                {
                    return;
                }

                bool applied = campaignController.TryApplyMoldinessUnlock(selectedMoldinessRewardId);
                if (!applied)
                {
                    return;
                }

                requiresMoldinessRewardSelection = false;
                selectedMoldinessRewardId = null;
                bool returnToCampaignMenu = returnToCampaignMenuAfterMoldinessReward;
                bool showPostAdaptationConfirmation = showPostAdaptationConfirmationAfterMoldinessRewardSelection;
                returnToCampaignMenuAfterMoldinessReward = false;
                showPostAdaptationConfirmationAfterMoldinessRewardSelection = false;

                if (returnToCampaignMenu)
                {
                    HideInstant();
                    if (onExitToModeSelect != null)
                        onExitToModeSelect();
                    else
                        manager?.ReturnToMainMenu();
                    return;
                }

                if (showPostAdaptationConfirmation)
                {
                    ShowCampaignAdaptationSecuredConfirmation();
                    return;
                }

                HideInstant();
                bool started = manager.TryStartCampaignAdaptationDraft(OnCampaignAdaptationSelected);
                if (!started)
                {
                    manager.StartCampaignResume();
                }
                return;
            }

            if (requiresAdaptationBeforeContinue)
            {
                var manager = GameManager.Instance;
                HideInstant();

                bool started = manager != null && manager.TryStartCampaignAdaptationDraft(OnCampaignAdaptationSelected);
                if (!started)
                {
                    requiresAdaptationBeforeContinue = false;
                    ApplyPostVictoryTestingSettings(manager);
                    if (onCampaignResume != null)
                        onCampaignResume();
                    else
                        manager?.StartCampaignResume();
                }
                return;
            }

            // Mid-run victory continue path
            ApplyPostVictoryTestingSettings(GameManager.Instance);
            HideInstant();
            if (onCampaignResume != null)
                onCampaignResume();
            else
                GameManager.Instance?.StartCampaignResume();
        }

        private void OnCampaignAdaptationSelected()
        {
            requiresAdaptationBeforeContinue = false;

            var manager = GameManager.Instance;
            var campaignController = manager?.CampaignController;
            if (campaignController != null && campaignController.HasPendingMoldinessUnlockChoice)
            {
                var snapshot = cachedCampaignVictorySnapshot;
                if (snapshot == null
                    && (!campaignController.TryGetPendingMoldinessRewardSnapshot(out snapshot) || snapshot == null))
                {
                    manager?.StartCampaignResume();
                    return;
                }

                snapshot.pendingMoldinessUnlockCount = campaignController.State?.moldiness?.pendingUnlockTriggers?.Count ?? snapshot.pendingMoldinessUnlockCount;
                var offers = campaignController.GetPendingMoldinessUnlockOffers(new System.Random(campaignController.State?.seed ?? 0), 3);
                ShowCampaignPendingMoldinessRewardSelection(
                    snapshot,
                    offers,
                    returnToCampaignMenuAfterSelection: false,
                    showAdaptationConfirmationAfterSelection: true);
                return;
            }

            ShowCampaignAdaptationSecuredConfirmation();
        }

        private void ShowCampaignAdaptationSecuredConfirmation()
        {
            requiresAdaptationBeforeContinue = false;
            showPostAdaptationConfirmationAfterMoldinessRewardSelection = false;

            if (outcomeLabel != null)
            {
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Adaptation secured</b></color>\n" +
                    $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Continue when you are ready for the next level.</color></size>";
            }

            SetOutcomeBannerVisibility(true);
            SetLegacyResultsHeaderVisibility(false);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Continue Campaign");
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
                SetButtonLabel(playAgainButton, "Main Menu");
            }

            SetPostAdaptationConfirmationState(true);
            UpdatePostVictoryTestingVisibility(continueButton != null && continueButton.gameObject.activeSelf);
            ApplyControlReadabilityOverrides();
            RefreshRuntimeEndGameLayout();

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void OnReturnToMainMenu()
        {
            HideInstant();
            if (onExitToModeSelect != null)
                onExitToModeSelect();
            else
                GameManager.Instance?.ReturnToMainMenu();
        }

        private void OnExitGame()
        {
            HideInstant();
            GameManager.Instance?.QuitGame();
        }

        private void HideInstant()
        {
            StopAllCoroutines();
            HidePlayerDetails();
            SetPostAdaptationConfirmationState(false);
            ApplyPendingRewardBackgroundMode(false);
            hasPendingDefeatCarryoverEvent = false;
            requiresDefeatCarryoverSelection = false;
            requiresMoldinessRewardSelection = false;
            returnToCampaignMenuAfterMoldinessReward = false;
            showPostAdaptationConfirmationAfterMoldinessRewardSelection = false;
            selectedMoldinessRewardId = null;
            defeatCarryoverSelectionCapacity = 0;
            selectedDefeatCarryoverAdaptationIds.Clear();
            pendingDefeatCarryoverOptions.Clear();
            pendingDefeatCarryoverEntryMode = DefeatCarryoverEntryMode.ImmediateLossScreen;
            cachedCampaignVictorySnapshot = null;
            moldinessRewardOptionBackgrounds.Clear();
            moldinessRewardOptionVisuals.Clear();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void ConfigurePendingDefeatCarryoverEvent(
            IReadOnlyList<AdaptationDefinition> options,
            int selectionCapacity,
            DefeatCarryoverEntryMode entryMode)
        {
            pendingDefeatCarryoverOptions.Clear();
            if (options != null)
            {
                pendingDefeatCarryoverOptions.AddRange(options.Where(option => option != null));
            }

            defeatCarryoverSelectionCapacity = Mathf.Max(0, Mathf.Min(selectionCapacity, pendingDefeatCarryoverOptions.Count));
            hasPendingDefeatCarryoverEvent = defeatCarryoverSelectionCapacity > 0 && pendingDefeatCarryoverOptions.Count > 0;
            pendingDefeatCarryoverEntryMode = entryMode;
        }

        private void EnsurePendingRewardBreadBackground()
        {
            if (pendingRewardBreadBackground != null)
            {
                return;
            }

            var backgroundObject = new GameObject("UI_PendingRewardBreadBackground", typeof(RectTransform), typeof(Image));
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(transform, false);
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.SetAsFirstSibling();

            pendingRewardBreadBackground = backgroundObject.GetComponent<Image>();
            pendingRewardBreadBackground.raycastTarget = false;
            pendingRewardBreadBackground.preserveAspect = true;
            pendingRewardBreadBackground.color = Color.white;
#if UNITY_EDITOR
            if (pendingRewardBreadSprite == null)
            {
                pendingRewardBreadSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Bread Backgrounds/white_bread_1024x1024.png");
            }
#endif
            pendingRewardBreadBackground.sprite = pendingRewardBreadSprite;
            pendingRewardBreadBackground.enabled = false;
        }

        private void ApplyPendingRewardBackgroundMode(bool enabled)
        {
            if (panelBackground != null)
            {
                panelBackground.color = enabled ? UIStyleTokens.Surface.PanelPrimary : UIStyleTokens.Surface.OverlayDim;
            }

            EnsurePendingRewardBreadBackground();
            if (pendingRewardBreadBackground != null)
            {
                pendingRewardBreadBackground.enabled = false;
            }
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = text;
                return;
            }

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.text = text;
            }
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        private void BuildResultsHeader(Transform parentOverride = null)
        {
            var parent = parentOverride ?? resultsContainer;
            if (parent == null)
            {
                return;
            }

            var header = new GameObject("UI_GameEndResultsHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(parent, false);

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8f;
            layout.padding = new RectOffset((int)EndGameResultsHeaderHorizontalPadding, (int)EndGameResultsHeaderHorizontalPadding, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 36f;

            CreateHeaderCell(header.transform, string.Empty, EndGameResultsRankWidth, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, string.Empty, EndGameResultsIconWidth, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, "Player", 210f, TextAlignmentOptions.Left, true);
            CreateHeaderCell(header.transform, "Alive", EndGameResultsMetricWidth, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Resistant", EndGameResultsMetricWidth, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Dead", EndGameResultsMetricWidth, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Toxins", EndGameResultsMetricWidth, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Details", EndGameResultsDetailsWidth, TextAlignmentOptions.Center, false);
        }

        private Player ResolvePlayerForDetails(int playerId)
        {
            var board = gameUI?.Board ?? GameManager.Instance?.GameUI?.Board;
            if (board?.Players == null)
            {
                return null;
            }

            return board.Players.FirstOrDefault(player => player.PlayerId == playerId);
        }

        private void EnsureDetailsModal()
        {
            if (detailsOverlayRoot != null)
            {
                return;
            }

            detailsOverlayRoot = new GameObject("UI_EndGameDetailsOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            detailsOverlayRoot.transform.SetParent(transform, false);
            detailsOverlayRoot.transform.SetAsLastSibling();
            EnsureIgnoreParentLayout(detailsOverlayRoot);

            var overlayRect = detailsOverlayRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            detailsOverlayCanvasGroup = detailsOverlayRoot.GetComponent<CanvasGroup>();
            detailsOverlayBackground = detailsOverlayRoot.GetComponent<Image>();
            detailsOverlayBackground.color = new Color(UIStyleTokens.Surface.OverlayDim.r, UIStyleTokens.Surface.OverlayDim.g, UIStyleTokens.Surface.OverlayDim.b, 0.88f);
            detailsOverlayBackground.raycastTarget = true;

            detailsBackdropButton = detailsOverlayRoot.GetComponent<Button>();
            detailsBackdropButton.transition = Selectable.Transition.None;
            detailsBackdropButton.onClick.RemoveAllListeners();
            detailsBackdropButton.onClick.AddListener(HidePlayerDetails);

            var cardObject = new GameObject("UI_EndGameDetailsCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            cardObject.transform.SetParent(detailsOverlayRoot.transform, false);
            EnsureIgnoreParentLayout(cardObject);

            detailsCardBackgroundImage = cardObject.GetComponent<Image>();
            var cardColor = UIStyleTokens.Surface.PanelPrimary;
            cardColor.a = 0.98f;
            detailsCardBackgroundImage.color = cardColor;

            detailsCardRect = cardObject.GetComponent<RectTransform>();
            detailsCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailsCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailsCardRect.pivot = new Vector2(0.5f, 0.5f);
            detailsCardRect.anchoredPosition = Vector2.zero;
            UpdateDetailsCardSize();

            var cardLayout = cardObject.GetComponent<VerticalLayoutGroup>();
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.spacing = 14f;
            cardLayout.padding = new RectOffset(22, 22, 22, 22);

            var cardElement = cardObject.GetComponent<LayoutElement>();
            cardElement.ignoreLayout = true;

            BuildDetailsHeader(cardObject.transform);
            BuildDetailsScrollArea(cardObject.transform);

            detailsOverlayRoot.SetActive(false);
            detailsOverlayCanvasGroup.alpha = 0f;
            detailsOverlayCanvasGroup.interactable = false;
            detailsOverlayCanvasGroup.blocksRaycasts = false;
        }

        private void BuildDetailsHeader(Transform cardTransform)
        {
            var header = new GameObject("UI_EndGameDetailsHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(cardTransform, false);

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 16f;

            var layoutElement = header.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 76f;
            layoutElement.minHeight = 72f;

            var iconObject = new GameObject("UI_EndGameDetailsPlayerIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(header.transform, false);

            detailsPlayerIconImage = iconObject.GetComponent<Image>();
            detailsPlayerIconImage.preserveAspect = true;

            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = DetailsHeaderIconSize;
            iconLayout.preferredHeight = DetailsHeaderIconSize;
            iconLayout.minWidth = DetailsHeaderIconSize;
            iconLayout.minHeight = DetailsHeaderIconSize;

            var textRoot = new GameObject("UI_EndGameDetailsTextRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textRoot.transform.SetParent(header.transform, false);

            var textLayout = textRoot.GetComponent<VerticalLayoutGroup>();
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandHeight = false;
            textLayout.childForceExpandWidth = true;
            textLayout.spacing = 4f;

            var textRootLayout = textRoot.GetComponent<LayoutElement>();
            textRootLayout.flexibleWidth = 1f;

            detailsTitleText = CreateTextLabel(textRoot.transform, "UI_EndGameDetailsTitle", 31f, FontStyles.Bold, UIStyleTokens.Text.Primary, TextAlignmentOptions.Left);
            detailsSubtitleText = CreateTextLabel(textRoot.transform, "UI_EndGameDetailsSubtitle", 18f, FontStyles.Normal, UIStyleTokens.Text.Secondary, TextAlignmentOptions.Left);
            detailsSubtitleText.enableAutoSizing = true;
            detailsSubtitleText.fontSizeMax = 18f;
            detailsSubtitleText.fontSizeMin = 14f;

            detailsCloseButton = CreateDetailsDismissButton(header.transform);
        }

        private void BuildDetailsScrollArea(Transform cardTransform)
        {
            var scrollObject = new GameObject("UI_EndGameDetailsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(cardTransform, false);

            var scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 220f;

            var scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color(UIStyleTokens.Surface.PanelSecondary.r, UIStyleTokens.Surface.PanelSecondary.g, UIStyleTokens.Surface.PanelSecondary.b, 0.22f);
            scrollImage.raycastTarget = true;

            detailsScrollRect = scrollObject.GetComponent<ScrollRect>();
            detailsScrollRect.horizontal = false;
            detailsScrollRect.vertical = true;
            detailsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            detailsScrollRect.scrollSensitivity = 24f;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);

            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            detailsScrollContent = contentObject.GetComponent<RectTransform>();
            detailsScrollContent.anchorMin = new Vector2(0f, 1f);
            detailsScrollContent.anchorMax = new Vector2(1f, 1f);
            detailsScrollContent.pivot = new Vector2(0.5f, 1f);
            detailsScrollContent.anchoredPosition = Vector2.zero;
            detailsScrollContent.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = DetailsSectionSpacing;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            detailsScrollRect.viewport = viewportRect;
            detailsScrollRect.content = detailsScrollContent;
        }

        private Button CreateDetailsDismissButton(Transform parent)
        {
            var buttonObject = new GameObject("UI_EndGameDetailsCloseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HidePlayerDetails);
            UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);

            var layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = DetailsDismissButtonSize;
            layoutElement.preferredHeight = DetailsDismissButtonSize;
            layoutElement.minWidth = DetailsDismissButtonSize;
            layoutElement.minHeight = DetailsDismissButtonSize;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "×";
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMax = 30f;
            text.fontSizeMin = 20f;
            text.color = UIStyleTokens.Text.Primary;
            text.raycastTarget = false;

            EnsureTooltip(button, "Close this details view.");
            return button;
        }

        private void ShowPlayerDetails(Player player, int rank, Sprite icon)
        {
            if (player == null)
            {
                return;
            }

            EnsureDetailsModal();
            if (detailsOverlayRoot == null || detailsScrollContent == null)
            {
                return;
            }

            detailsOverlayRoot.transform.SetAsLastSibling();
            UpdateDetailsCardSize();
            detailsOverlayRoot.SetActive(true);
            detailsOverlayCanvasGroup.alpha = 1f;
            detailsOverlayCanvasGroup.interactable = true;
            detailsOverlayCanvasGroup.blocksRaycasts = true;

            if (detailsPlayerIconImage != null)
            {
                detailsPlayerIconImage.sprite = icon;
                detailsPlayerIconImage.enabled = icon != null;
            }

            int mutationCount = player.PlayerMutations.Values.Count(pm => pm.CurrentLevel > 0);
            int mycovariantCount = player.PlayerMycovariants?.Count ?? 0;
            int adaptationCount = player.PlayerAdaptations?.Count ?? 0;

            if (detailsTitleText != null)
            {
                detailsTitleText.text = $"{player.PlayerName} Details";
            }

            if (detailsSubtitleText != null)
            {
                detailsSubtitleText.text = $"Rank {rank}  •  {mutationCount} mutation{Pluralize(mutationCount)}  •  {mycovariantCount} mycovariant{Pluralize(mycovariantCount)}  •  {adaptationCount} adaptation{Pluralize(adaptationCount)}";
            }

            ClearDetailsScrollContent();
            BuildMutationSection(player);
            BuildMycovariantSection(player);
            BuildAdaptationSection(player);
            BuildOtherStatisticsSection(currentPlayerStatistics.GetPlayerStatistics(player.PlayerId));
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailsScrollContent);
            detailsScrollRect.verticalNormalizedPosition = 1f;
        }

        private void HidePlayerDetails()
        {
            if (detailsOverlayRoot == null || detailsOverlayCanvasGroup == null)
            {
                return;
            }

            detailsOverlayCanvasGroup.alpha = 0f;
            detailsOverlayCanvasGroup.interactable = false;
            detailsOverlayCanvasGroup.blocksRaycasts = false;
            detailsOverlayRoot.SetActive(false);
        }

        public bool BlocksGameplayCameraInput => gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.blocksRaycasts;

        public bool IsDetailsModalOpen => detailsOverlayRoot != null && detailsOverlayRoot.activeSelf && detailsOverlayCanvasGroup != null && detailsOverlayCanvasGroup.blocksRaycasts;

        private void UpdateDetailsCardSize()
        {
            if (detailsOverlayRoot == null || detailsCardRect == null)
            {
                return;
            }

            var overlayRect = detailsOverlayRoot.GetComponent<RectTransform>();
            if (overlayRect == null)
            {
                return;
            }

            float availableWidth = Mathf.Max(DetailsCardMinWidth, overlayRect.rect.width - (DetailsCardSideBuffer * 2f));
            float availableHeight = Mathf.Max(DetailsCardMinHeight, overlayRect.rect.height - (DetailsCardVerticalBuffer * 2f));

            float width = Mathf.Clamp(DetailsCardPreferredWidth, DetailsCardMinWidth, availableWidth);
            float height = Mathf.Clamp(DetailsCardPreferredHeight, DetailsCardMinHeight, availableHeight);

            detailsCardRect.sizeDelta = new Vector2(width, height);
        }

        private void ClearDetailsScrollContent()
        {
            if (detailsScrollContent == null)
            {
                return;
            }

            for (int i = detailsScrollContent.childCount - 1; i >= 0; i--)
            {
                var child = detailsScrollContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void BuildMutationSection(Player player)
        {
            var mutations = player.PlayerMutations.Values
                .Where(pm => pm != null && pm.CurrentLevel > 0 && pm.Mutation != null)
                .OrderBy(pm => pm.FirstUpgradeRound ?? int.MaxValue)
                .ThenBy(pm => pm.Mutation.TierNumber)
                .ThenBy(pm => pm.Mutation.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var section = CreateDetailsSectionContainer($"Mutations ({mutations.Count})");
            if (mutations.Count == 0)
            {
                CreateEmptyStateLabel(section, "No mutations acquired this game.");
                return;
            }

            for (int i = 0; i < mutations.Count; i++)
            {
                CreateMutationEntry(section, mutations[i]);
            }
        }

        private void BuildMycovariantSection(Player player)
        {
            int count = player.PlayerMycovariants?.Count ?? 0;
            var section = CreateDetailsSectionContainer($"Mycovariants ({count})");
            if (count == 0)
            {
                CreateEmptyStateLabel(section, "No mycovariants drafted.");
                return;
            }

            for (int i = 0; i < player.PlayerMycovariants.Count; i++)
            {
                CreateMycovariantEntry(section, player.PlayerMycovariants[i]);
            }
        }

        private void BuildAdaptationSection(Player player)
        {
            int count = player.PlayerAdaptations?.Count ?? 0;
            var section = CreateDetailsSectionContainer($"Adaptations ({count})");
            if (count == 0)
            {
                CreateEmptyStateLabel(section, "No adaptations acquired.");
                return;
            }

            for (int i = 0; i < player.PlayerAdaptations.Count; i++)
            {
                CreateAdaptationEntry(section, player.PlayerAdaptations[i]);
            }
        }

        private readonly struct OtherStatisticDescriptor
        {
            public OtherStatisticDescriptor(string label, int value, string tooltip)
            {
                Label = label;
                Value = value;
                Tooltip = tooltip;
            }

            public string Label { get; }
            public int Value { get; }
            public string Tooltip { get; }
        }

        private void BuildOtherStatisticsSection(EndgamePlayerStatistics statistics)
        {
            var descriptors = new List<OtherStatisticDescriptor>
            {
                new OtherStatisticDescriptor(
                    "Spent Mutation Points",
                    statistics.SpentMutationPoints,
                    "Total mutation points this player spent on mutation upgrades and surge activations during the game."),
                new OtherStatisticDescriptor(
                    "Tiles Colonized",
                    statistics.TilesColonized,
                    "Colonize: place a new living cell in an empty tile."),
                new OtherStatisticDescriptor(
                    "Tiles Toxified",
                    statistics.TilesToxified,
                    "Toxify: place toxin in an empty or dead tile."),
                new OtherStatisticDescriptor(
                    "Cells Reclaimed",
                    statistics.CellsReclaimed,
                    "Reclaim: place a new living cell over any dead cell, restoring it to living status by occupying that tile."),
                new OtherStatisticDescriptor(
                    "Cells Overgrown",
                    statistics.CellsOvergrown,
                    "Overgrow: place a new living cell over a toxin tile, removing the toxin tile by growing into it."),
                new OtherStatisticDescriptor(
                    "Cells Infested",
                    statistics.CellsInfested,
                    "Infest: place a new living cell over an enemy living cell, killing it and taking the tile."),
                new OtherStatisticDescriptor(
                    "Cells Poisoned",
                    statistics.CellsPoisoned,
                    "Poison: place toxin over a living cell, killing it and converting it into a toxin tile.")
            };

            var section = CreateDetailsSectionContainer("Other Statistics");
            for (int i = 0; i < descriptors.Count; i++)
            {
                CreateOtherStatisticEntry(section, descriptors[i]);
            }
        }

        private void CreateOtherStatisticEntry(Transform parent, OtherStatisticDescriptor descriptor)
        {
            var root = CreateDetailsEntryCard(parent, "OtherStatisticEntry");
            var row = CreateHorizontalContainer(root.transform, "StatisticRow", 12f);
            row.childAlignment = TextAnchor.MiddleLeft;

            var nameLabel = CreateTextLabel(row.transform, "Name", 19f, FontStyles.Bold, UIStyleTokens.Text.Primary, TextAlignmentOptions.Left);
            nameLabel.text = descriptor.Label;
            var nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            var valueLabel = CreateTextLabel(row.transform, "Value", 19f, FontStyles.Bold, UIStyleTokens.State.Success, TextAlignmentOptions.Right);
            valueLabel.text = descriptor.Value.ToString();

            AttachStaticTooltip(root.gameObject, descriptor.Tooltip);
        }

        private RectTransform CreateDetailsSectionContainer(string title)
        {
            var sectionObject = new GameObject($"UI_EndGameDetailsSection_{title}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            sectionObject.transform.SetParent(detailsScrollContent, false);

            var background = sectionObject.GetComponent<Image>();
            var color = UIStyleTokens.Surface.PanelSecondary;
            color.a = 0.72f;
            background.color = color;
            background.raycastTarget = false;

            var layout = sectionObject.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(14, 14, 12, 12);

            var layoutElement = sectionObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;

            CreateTextLabel(sectionObject.transform, "SectionTitle", 22f, FontStyles.Bold, UIStyleTokens.Text.Primary, TextAlignmentOptions.Left).text = title;
            return sectionObject.GetComponent<RectTransform>();
        }

        private void CreateEmptyStateLabel(Transform parent, string text)
        {
            var label = CreateTextLabel(parent, "EmptyState", 18f, FontStyles.Italic, UIStyleTokens.Text.Muted, TextAlignmentOptions.Left);
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMax = 18f;
            label.fontSizeMin = 13f;
            label.textWrappingMode = TextWrappingModes.Normal;
        }

        private void CreateMutationEntry(Transform parent, PlayerMutation playerMutation)
        {
            var root = CreateDetailsEntryCard(parent, "MutationEntry");
            var topRow = CreateHorizontalContainer(root.transform, "TopRow", 12f);
            topRow.childAlignment = TextAnchor.MiddleLeft;

            var nameLabel = CreateTextLabel(topRow.transform, "Name", 20f, FontStyles.Bold, UIStyleTokens.Text.Primary, TextAlignmentOptions.Left);
            nameLabel.text = playerMutation.Mutation.Name;
            var nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            var levelLabel = CreateTextLabel(topRow.transform, "Level", 17f, FontStyles.Bold, UIStyleTokens.State.Success, TextAlignmentOptions.Right);
            levelLabel.text = $"Lv {playerMutation.CurrentLevel}/{playerMutation.Mutation.MaxLevel}";

            var metaLabel = CreateTextLabel(root.transform, "Meta", 16f, FontStyles.Normal, UIStyleTokens.Text.Secondary, TextAlignmentOptions.Left);
            metaLabel.text = BuildMutationMetaText(playerMutation);
            metaLabel.enableAutoSizing = true;
            metaLabel.fontSizeMax = 16f;
            metaLabel.fontSizeMin = 12f;
            metaLabel.textWrappingMode = TextWrappingModes.Normal;

            AttachStaticTooltip(root.gameObject, BuildMutationTooltip(playerMutation));
        }

        private void CreateMycovariantEntry(Transform parent, PlayerMycovariant playerMycovariant)
        {
            var root = CreateIconDetailsEntry(parent, "MycovariantEntry", MycovariantArtRepository.GetIcon(playerMycovariant.Mycovariant));
            PopulateIconEntryText(
                root,
                playerMycovariant.Mycovariant.Name,
                BuildMycovariantMetaText(playerMycovariant));

            AttachStaticTooltip(root.gameObject, BuildMycovariantTooltip(playerMycovariant));
        }

        private void CreateAdaptationEntry(Transform parent, PlayerAdaptation playerAdaptation)
        {
            var root = CreateIconDetailsEntry(parent, "AdaptationEntry", AdaptationArtRepository.GetIcon(playerAdaptation.Adaptation));
            PopulateIconEntryText(
                root,
                playerAdaptation.Adaptation.Name,
                BuildAdaptationMetaText(playerAdaptation));

            AttachStaticTooltip(root.gameObject, BuildAdaptationTooltip(playerAdaptation));
        }

        private RectTransform CreateIconDetailsEntry(Transform parent, string name, Sprite icon)
        {
            var root = CreateDetailsEntryCard(parent, name);
            var row = CreateHorizontalContainer(root.transform, "EntryRow", 12f);
            row.childAlignment = TextAnchor.UpperLeft;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(row.transform, false);
            var image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.enabled = icon != null;

            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 40f;
            iconLayout.preferredHeight = 40f;
            iconLayout.minWidth = 40f;
            iconLayout.minHeight = 40f;

            var textRoot = new GameObject("TextRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textRoot.transform.SetParent(row.transform, false);
            var textLayout = textRoot.GetComponent<VerticalLayoutGroup>();
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandHeight = false;
            textLayout.childForceExpandWidth = true;
            textLayout.spacing = 3f;

            var textRootLayout = textRoot.GetComponent<LayoutElement>();
            textRootLayout.flexibleWidth = 1f;

            return root.GetComponent<RectTransform>();
        }

        private void PopulateIconEntryText(RectTransform root, string title, string meta)
        {
            if (root == null)
            {
                return;
            }

            var textRoot = root.GetComponentsInChildren<VerticalLayoutGroup>(true)
                .Select(group => group.transform)
                .FirstOrDefault(transform => string.Equals(transform.name, "TextRoot", StringComparison.Ordinal));
            if (textRoot == null)
            {
                return;
            }

            var titleLabel = CreateTextLabel(textRoot, "Title", 20f, FontStyles.Bold, UIStyleTokens.Text.Primary, TextAlignmentOptions.Left);
            titleLabel.text = title;

            var metaLabel = CreateTextLabel(textRoot, "Meta", 16f, FontStyles.Normal, UIStyleTokens.Text.Secondary, TextAlignmentOptions.Left);
            metaLabel.text = meta;
            metaLabel.enableAutoSizing = true;
            metaLabel.fontSizeMax = 16f;
            metaLabel.fontSizeMin = 12f;
            metaLabel.textWrappingMode = TextWrappingModes.Normal;
        }

        private GameObject CreateDetailsEntryCard(Transform parent, string name)
        {
            var entryObject = new GameObject($"UI_EndGameDetails{name}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            entryObject.transform.SetParent(parent, false);

            var background = entryObject.GetComponent<Image>();
            var color = UIStyleTokens.Surface.PanelPrimary;
            color.a = 0.8f;
            background.color = color;
            background.raycastTarget = true;

            var layout = entryObject.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(12, 12, 10, 10);

            var layoutElement = entryObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = 58f;

            return entryObject;
        }

        private HorizontalLayoutGroup CreateHorizontalContainer(Transform parent, string name, float spacing)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = spacing;
            return layout;
        }

        private TextMeshProUGUI CreateTextLabel(Transform parent, string name, float fontSize, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private void AttachStaticTooltip(GameObject target, string text)
        {
            if (target == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var trigger = target.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<TooltipTrigger>();
            }

            trigger.SetStaticText(text);
        }

        private static string BuildMutationMetaText(PlayerMutation playerMutation)
        {
            string tier = $"Tier {playerMutation.Mutation.TierNumber}";
            string category = FormatEnumLabel(playerMutation.Mutation.Category.ToString());
            string roundText = playerMutation.FirstUpgradeRound.HasValue
                ? $"First taken in round {playerMutation.FirstUpgradeRound.Value}"
                : "Acquisition round unavailable";

            if (playerMutation.Mutation.IsSurge)
            {
                return $"{tier} • {category} • Surge • {roundText}";
            }

            return $"{tier} • {category} • {roundText}";
        }

        private static string BuildMycovariantMetaText(PlayerMycovariant playerMycovariant)
        {
            string type = FormatEnumLabel(playerMycovariant.Mycovariant.Type.ToString());
            string category = FormatEnumLabel(playerMycovariant.Mycovariant.Category.ToString());
            string triggerState = playerMycovariant.HasTriggered ? "Triggered" : "Ready";
            return $"{type} • {category} • {triggerState}";
        }

        private static string BuildAdaptationMetaText(PlayerAdaptation playerAdaptation)
        {
            return playerAdaptation.HasTriggered
                ? "Campaign Adaptation • Triggered"
                : "Campaign Adaptation";
        }

        private static string BuildMutationTooltip(PlayerMutation playerMutation)
        {
            string flavor = string.IsNullOrWhiteSpace(playerMutation.Mutation.FlavorText)
                ? string.Empty
                : $"\n\n<i>{playerMutation.Mutation.FlavorText}</i>";
            string surge = playerMutation.Mutation.IsSurge
                ? $"\nSurge Duration: {playerMutation.Mutation.SurgeDuration} round{Pluralize(playerMutation.Mutation.SurgeDuration)}"
                : string.Empty;

            return $"<b>{playerMutation.Mutation.Name}</b>\n<i>{BuildMutationMetaText(playerMutation)}</i>\n\n{playerMutation.Mutation.Description}{surge}{flavor}";
        }

        private static string BuildMycovariantTooltip(PlayerMycovariant playerMycovariant)
        {
            string flavor = string.IsNullOrWhiteSpace(playerMycovariant.Mycovariant.FlavorText)
                ? string.Empty
                : $"\n\n<i>{playerMycovariant.Mycovariant.FlavorText}</i>";
            return $"<b>{playerMycovariant.Mycovariant.Name}</b>\n<i>{BuildMycovariantMetaText(playerMycovariant)}</i>\n\n{playerMycovariant.Mycovariant.Description}{flavor}";
        }

        private static string BuildAdaptationTooltip(PlayerAdaptation playerAdaptation)
        {
            return $"<b>{playerAdaptation.Adaptation.Name}</b>\n<i>{BuildAdaptationMetaText(playerAdaptation)}</i>\n\n{playerAdaptation.Adaptation.Description}";
        }

        private static string FormatEnumLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(raw.Length + 8);
            for (int i = 0; i < raw.Length; i++)
            {
                char current = raw[i];
                if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(raw[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static string Pluralize(int count)
        {
            return count == 1 ? string.Empty : "s";
        }

        private void PreparePanelForContentBuild()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void SetPostAdaptationConfirmationState(bool visible)
        {
            showPostAdaptationConfirmationState = visible;
        }

        private void BuildCampaignTopSpacer()
        {
            if (resultsContainer == null)
            {
                return;
            }

            if (resultsContainer.Find("UI_CampaignOutcomeTopSpacer") != null)
            {
                return;
            }

            var spacer = new GameObject("UI_CampaignOutcomeTopSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(resultsContainer, false);

            var layout = spacer.GetComponent<LayoutElement>();
            layout.preferredHeight = CampaignOutcomeSpacerPreferredHeight;
            layout.minHeight = CampaignOutcomeSpacerMinHeight;
            layout.flexibleHeight = 0f;
        }

        private void EnsureRuntimeLayoutScaffold()
        {
            if (resultsCardBackground == null || resultsContainer == null)
            {
                return;
            }

            var panelRect = GetComponent<RectTransform>();
            if (panelRect != null)
            {
                float horizontalInset = showPostAdaptationConfirmationState
                    ? EndGameConfirmationOverlayHorizontalInset
                    : EndGameOverlayHorizontalInset;

                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = new Vector2(horizontalInset, 0f);
                panelRect.offsetMax = new Vector2(-horizontalInset, 0f);
                panelRect.localScale = Vector3.one;
            }

            endGameLayoutContainer = resultsCardBackground.transform.parent as RectTransform;
            if (endGameLayoutContainer != null)
            {
                float verticalInset = showPostAdaptationConfirmationState
                    ? EndGameConfirmationOverlayVerticalInset
                    : EndGameOverlayVerticalInset;

                endGameLayoutContainer.anchorMin = Vector2.zero;
                endGameLayoutContainer.anchorMax = Vector2.one;
                endGameLayoutContainer.pivot = new Vector2(0.5f, 0.5f);
                endGameLayoutContainer.anchoredPosition = Vector2.zero;
                endGameLayoutContainer.offsetMin = new Vector2(0f, verticalInset);
                endGameLayoutContainer.offsetMax = new Vector2(0f, -verticalInset);
                endGameLayoutContainer.localScale = Vector3.one;

                var layout = endGameLayoutContainer.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                }
            }

            var cardRect = resultsCardBackground.rectTransform;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            cardRect.localScale = Vector3.one;

            var cardLayout = resultsCardBackground.GetComponent<VerticalLayoutGroup>();
            if (cardLayout != null)
            {
                cardLayout.enabled = false;
            }

            if (endGameContentShellRoot == null)
            {
                var shell = new GameObject("UI_EndGameContentShell", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                shell.transform.SetParent(resultsCardBackground.transform, false);
                endGameContentShellRoot = shell.GetComponent<RectTransform>();
            }

            endGameContentShellRoot.SetParent(resultsCardBackground.transform, false);
            endGameContentShellRoot.anchorMin = Vector2.zero;
            endGameContentShellRoot.anchorMax = Vector2.one;
            endGameContentShellRoot.pivot = new Vector2(0.5f, 0.5f);
            endGameContentShellRoot.anchoredPosition = Vector2.zero;
            endGameContentShellRoot.localScale = Vector3.one;

            var shellLayout = endGameContentShellRoot.GetComponent<HorizontalLayoutGroup>();
            shellLayout.childAlignment = TextAnchor.UpperLeft;
            shellLayout.childControlWidth = true;
            shellLayout.childControlHeight = true;
            shellLayout.childForceExpandWidth = false;
            shellLayout.childForceExpandHeight = true;
            shellLayout.spacing = EndGameRailGap;
            shellLayout.padding = new RectOffset(0, 0, 0, 0);

            if (endGameTestingRailMirrorSpacerRoot == null)
            {
                var spacer = new GameObject("UI_EndGameTestingRailMirrorSpacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.transform.SetParent(endGameContentShellRoot, false);
                endGameTestingRailMirrorSpacerRoot = spacer.GetComponent<RectTransform>();
            }

            endGameTestingRailMirrorSpacerRoot.SetParent(endGameContentShellRoot, false);
            endGameTestingRailMirrorSpacerRoot.localScale = Vector3.one;
            endGameTestingRailMirrorSpacerLayoutElement = endGameTestingRailMirrorSpacerRoot.GetComponent<LayoutElement>();
            endGameTestingRailMirrorSpacerLayoutElement.flexibleWidth = 0f;
            endGameTestingRailMirrorSpacerLayoutElement.flexibleHeight = 1f;
            endGameTestingRailMirrorSpacerLayoutElement.minWidth = 0f;
            endGameTestingRailMirrorSpacerLayoutElement.preferredWidth = 0f;

            if (endGameMainColumnRoot == null)
            {
                var mainColumn = new GameObject("UI_EndGameMainColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                mainColumn.transform.SetParent(endGameContentShellRoot, false);
                endGameMainColumnRoot = mainColumn.GetComponent<RectTransform>();
            }

            endGameMainColumnRoot.SetParent(endGameContentShellRoot, false);
            endGameMainColumnRoot.localScale = Vector3.one;

            var mainColumnLayout = endGameMainColumnRoot.GetComponent<VerticalLayoutGroup>();
            mainColumnLayout.childAlignment = TextAnchor.UpperCenter;
            mainColumnLayout.childControlWidth = true;
            mainColumnLayout.childControlHeight = true;
            mainColumnLayout.childForceExpandWidth = true;
            mainColumnLayout.childForceExpandHeight = false;
            mainColumnLayout.spacing = 12f;
            mainColumnLayout.padding = new RectOffset(0, 0, 0, 0);

            var mainColumnElement = endGameMainColumnRoot.GetComponent<LayoutElement>();
            mainColumnElement.flexibleWidth = 1f;
            mainColumnElement.flexibleHeight = 1f;
            mainColumnElement.minWidth = 560f;

            if (endGameTestingRailRoot == null)
            {
                var rail = new GameObject("UI_EndGameTestingRail", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                rail.transform.SetParent(endGameContentShellRoot, false);
                endGameTestingRailRoot = rail.GetComponent<RectTransform>();
            }

            endGameTestingRailRoot.SetParent(endGameContentShellRoot, false);
            endGameTestingRailRoot.localScale = Vector3.one;

            var railLayout = endGameTestingRailRoot.GetComponent<VerticalLayoutGroup>();
            railLayout.childAlignment = TextAnchor.UpperCenter;
            railLayout.childControlWidth = true;
            railLayout.childControlHeight = true;
            railLayout.childForceExpandWidth = false;
            railLayout.childForceExpandHeight = false;
            railLayout.spacing = 0f;
            railLayout.padding = new RectOffset(0, 0, 0, 0);

            endGameTestingRailLayoutElement = endGameTestingRailRoot.GetComponent<LayoutElement>();
            endGameTestingRailLayoutElement.flexibleWidth = 0f;
            endGameTestingRailLayoutElement.flexibleHeight = 1f;
            endGameTestingRailLayoutElement.minWidth = EndGameRailWidth;
            endGameTestingRailLayoutElement.preferredWidth = EndGameRailWidth;

            if (endGameActionBarRoot == null)
            {
                var actionBar = new GameObject("UI_EndGameActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                actionBar.transform.SetParent(resultsCardBackground.transform, false);
                endGameActionBarRoot = actionBar.GetComponent<RectTransform>();
            }

            endGameActionBarRoot.SetParent(resultsCardBackground.transform, false);
            endGameActionBarRoot.anchorMin = new Vector2(0f, 0f);
            endGameActionBarRoot.anchorMax = new Vector2(1f, 0f);
            endGameActionBarRoot.pivot = new Vector2(0.5f, 0f);
            endGameActionBarRoot.anchoredPosition = new Vector2(0f, EndGameActionBarBottomInset);
            endGameActionBarRoot.sizeDelta = new Vector2(0f, EndGameActionBarHeight);
            endGameActionBarRoot.offsetMin = new Vector2(EndGameContentHorizontalInset, endGameActionBarRoot.offsetMin.y);
            endGameActionBarRoot.offsetMax = new Vector2(-EndGameContentHorizontalInset, endGameActionBarRoot.offsetMax.y);
            endGameActionBarRoot.localScale = Vector3.one;

            var actionBarLayout = endGameActionBarRoot.GetComponent<HorizontalLayoutGroup>();
            actionBarLayout.childAlignment = TextAnchor.MiddleCenter;
            actionBarLayout.childControlWidth = true;
            actionBarLayout.childControlHeight = true;
            actionBarLayout.childForceExpandWidth = false;
            actionBarLayout.childForceExpandHeight = false;
            actionBarLayout.spacing = 18f;
            actionBarLayout.padding = new RectOffset(0, 0, 0, 0);

            if (endGamePostAdaptationRoot == null)
            {
                var confirmationRoot = new GameObject("UI_EndGamePostAdaptationRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                confirmationRoot.transform.SetParent(endGameMainColumnRoot, false);
                endGamePostAdaptationRoot = confirmationRoot.GetComponent<RectTransform>();
            }

            endGamePostAdaptationRoot.SetParent(endGameMainColumnRoot, false);
            endGamePostAdaptationRoot.localScale = Vector3.one;

            var confirmationLayout = endGamePostAdaptationRoot.GetComponent<VerticalLayoutGroup>();
            confirmationLayout.childAlignment = TextAnchor.UpperCenter;
            confirmationLayout.childControlWidth = true;
            confirmationLayout.childControlHeight = true;
            confirmationLayout.childForceExpandWidth = false;
            confirmationLayout.childForceExpandHeight = false;
            confirmationLayout.spacing = EndGameConfirmationStackSpacing;
            confirmationLayout.padding = new RectOffset(0, 0, requiresMoldinessRewardSelection ? 18 : 0, 0);

            var confirmationElement = endGamePostAdaptationRoot.GetComponent<LayoutElement>();
            confirmationElement.minWidth = EndGameConfirmationStackWidth;
            confirmationElement.preferredWidth = EndGameConfirmationStackWidth;
            confirmationElement.flexibleWidth = 0f;
            confirmationElement.flexibleHeight = 0f;
            confirmationElement.minHeight = 0f;
            confirmationElement.preferredHeight = -1f;

            EnsureScrollableResultsContent();
            EnsureLegacyResultsTitlePlacement();
        }

        private void EnsureLegacyResultsTitlePlacement()
        {
            if (endGameMainColumnRoot == null)
            {
                return;
            }

            if (legacyResultsTitleText == null)
            {
                var existing = resultsCardBackground != null
                    ? resultsCardBackground.transform.Find("UI_GameEndResultsHeaderText") as RectTransform
                    : null;
                legacyResultsTitleText = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            }

            if (legacyResultsTitleText == null)
            {
                return;
            }

            if (legacyResultsTitleText.transform.parent != endGameMainColumnRoot)
            {
                legacyResultsTitleText.transform.SetParent(endGameMainColumnRoot, false);
            }

            legacyResultsTitleText.transform.SetSiblingIndex(0);
            legacyResultsTitleText.enableAutoSizing = true;
            legacyResultsTitleText.fontSizeMax = 30f;
            legacyResultsTitleText.fontSizeMin = 22f;
            legacyResultsTitleText.alignment = TextAlignmentOptions.Center;
            legacyResultsTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            legacyResultsTitleText.overflowMode = TextOverflowModes.Ellipsis;

            var element = legacyResultsTitleText.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = legacyResultsTitleText.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = EndGameLegacyHeaderHeight;
            element.preferredHeight = EndGameLegacyHeaderHeight;
            element.flexibleWidth = 1f;
        }

        private void EnsureScrollableResultsContent()
        {
            if (endGameMainColumnRoot == null || resultsContainer == null)
            {
                return;
            }

            if (endGameResultsScrollRoot == null)
            {
                var scrollView = new GameObject("UI_EndGameResultsScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
                scrollView.transform.SetParent(endGameMainColumnRoot, false);
                endGameResultsScrollRoot = scrollView.GetComponent<RectTransform>();
            }

            endGameResultsScrollRoot.SetParent(endGameMainColumnRoot, false);
            endGameResultsScrollRoot.localScale = Vector3.one;

            var scrollImage = endGameResultsScrollRoot.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.001f);
            scrollImage.raycastTarget = true;

            var scrollLayoutElement = endGameResultsScrollRoot.GetComponent<LayoutElement>();
            scrollLayoutElement.flexibleHeight = 1f;
            scrollLayoutElement.flexibleWidth = 1f;
            scrollLayoutElement.minHeight = EndGameResultsScrollMinHeight;

            if (endGameResultsViewportRoot == null)
            {
                var viewport = new GameObject("UI_EndGameResultsViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewport.transform.SetParent(endGameResultsScrollRoot, false);
                endGameResultsViewportRoot = viewport.GetComponent<RectTransform>();
            }

            endGameResultsViewportRoot.SetParent(endGameResultsScrollRoot, false);
            endGameResultsViewportRoot.anchorMin = Vector2.zero;
            endGameResultsViewportRoot.anchorMax = Vector2.one;
            endGameResultsViewportRoot.pivot = new Vector2(0.5f, 0.5f);
            endGameResultsViewportRoot.anchoredPosition = Vector2.zero;
            endGameResultsViewportRoot.offsetMin = Vector2.zero;
            endGameResultsViewportRoot.offsetMax = Vector2.zero;
            endGameResultsViewportRoot.localScale = Vector3.one;

            var viewportImage = endGameResultsViewportRoot.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = false;

            var viewportMask = endGameResultsViewportRoot.GetComponent<Mask>();
            if (viewportMask != null)
            {
                viewportMask.enabled = false;
            }

            var viewportRectMask = endGameResultsViewportRoot.GetComponent<RectMask2D>();
            if (viewportRectMask == null)
            {
                viewportRectMask = endGameResultsViewportRoot.gameObject.AddComponent<RectMask2D>();
            }

            if (resultsContainer.parent != endGameResultsViewportRoot)
            {
                resultsContainer.SetParent(endGameResultsViewportRoot, false);
            }

            if (resultsContainer is RectTransform resultsRect)
            {
                resultsRect.anchorMin = new Vector2(0f, 1f);
                resultsRect.anchorMax = new Vector2(1f, 1f);
                resultsRect.pivot = new Vector2(0.5f, 1f);
                resultsRect.anchoredPosition = Vector2.zero;
                resultsRect.offsetMin = Vector2.zero;
                resultsRect.offsetMax = Vector2.zero;
                resultsRect.sizeDelta = Vector2.zero;
                resultsRect.localScale = Vector3.one;

                var contentElement = resultsRect.GetComponent<LayoutElement>();
                if (contentElement == null)
                {
                    contentElement = resultsRect.gameObject.AddComponent<LayoutElement>();
                }

                contentElement.minWidth = 0f;
                contentElement.preferredWidth = -1f;
                contentElement.flexibleWidth = 1f;
                contentElement.minHeight = 0f;
                contentElement.preferredHeight = -1f;
                contentElement.flexibleHeight = 0f;

                var contentLayout = resultsRect.GetComponent<VerticalLayoutGroup>();
                if (contentLayout != null)
                {
                    contentLayout.padding = new RectOffset(0, 0, 8, 8);
                    contentLayout.childAlignment = TextAnchor.UpperLeft;
                    contentLayout.childControlWidth = true;
                    contentLayout.childControlHeight = true;
                    contentLayout.childForceExpandWidth = true;
                    contentLayout.childForceExpandHeight = false;
                    contentLayout.spacing = 6f;
                }

                var contentFitter = resultsRect.GetComponent<ContentSizeFitter>();
                if (contentFitter != null)
                {
                    contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                endGameResultsScrollRect = endGameResultsScrollRoot.GetComponent<ScrollRect>();
                endGameResultsScrollRect.viewport = endGameResultsViewportRoot;
                endGameResultsScrollRect.content = resultsRect;
                endGameResultsScrollRect.horizontal = false;
                endGameResultsScrollRect.vertical = true;
                endGameResultsScrollRect.movementType = ScrollRect.MovementType.Clamped;
                endGameResultsScrollRect.scrollSensitivity = 28f;
            }
        }

        private void RefreshRuntimeEndGameLayout()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            runtimeLayoutRefreshQueued = true;
        }

        private void ProcessPendingRuntimeEndGameLayoutRefresh()
        {
            if (resultsCardBackground == null || resultsContainer == null)
            {
                runtimeLayoutRefreshQueued = false;
                return;
            }

            runtimeLayoutRefreshQueued = false;
            isRefreshingRuntimeLayout = true;

            try
            {
                EnsureRuntimeLayoutScaffold();
                ApplyPostVictoryTestingRailVisibility(reloadConfiguration: false);
                EnsureActionButtonsShareContainer();

                var shellLayout = endGameContentShellRoot != null ? endGameContentShellRoot.GetComponent<HorizontalLayoutGroup>() : null;
                bool showMirroredRailSpacer = showPostAdaptationConfirmationState && postVictoryTestingRailVisible;
                if (shellLayout != null)
                {
                    shellLayout.childAlignment = (showPostAdaptationConfirmationState || requiresMoldinessRewardSelection)
                        ? TextAnchor.UpperCenter
                        : TextAnchor.UpperLeft;
                }

                if (endGameTestingRailMirrorSpacerRoot != null)
                {
                    endGameTestingRailMirrorSpacerRoot.gameObject.SetActive(showMirroredRailSpacer);
                    endGameTestingRailMirrorSpacerRoot.SetSiblingIndex(0);
                }

                if (endGameTestingRailMirrorSpacerLayoutElement != null)
                {
                    endGameTestingRailMirrorSpacerLayoutElement.minWidth = showMirroredRailSpacer ? EndGameRailWidth : 0f;
                    endGameTestingRailMirrorSpacerLayoutElement.preferredWidth = showMirroredRailSpacer ? EndGameRailWidth : 0f;
                }

                if (endGameMainColumnRoot != null)
                {
                    endGameMainColumnRoot.SetSiblingIndex(showMirroredRailSpacer ? 1 : 0);
                }

                if (endGameTestingRailRoot != null)
                {
                    endGameTestingRailRoot.SetSiblingIndex(showMirroredRailSpacer ? 2 : 1);
                }

                bool hasActionButtons = IsAnyActionButtonVisible();
                float topInset;
                if (outcomeLabel != null && outcomeLabel.gameObject.activeSelf)
                {
                    topInset = showPostAdaptationConfirmationState
                        ? EndGameConfirmationContentTopInsetWithOutcome
                        : EndGameContentTopInsetWithOutcome;
                }
                else
                {
                    topInset = EndGameContentTopInset;
                }

                bool useBottomActionBar = hasActionButtons && !UseVerticalActionStack;
                float bottomInset = useBottomActionBar
                    ? EndGameContentBottomInset
                    : UseVerticalActionStack
                        ? EndGameConfirmationContentBottomInset
                        : EndGameActionBarBottomInset;

                if (endGameContentShellRoot != null)
                {
                    endGameContentShellRoot.offsetMin = new Vector2(EndGameContentHorizontalInset, bottomInset);
                    endGameContentShellRoot.offsetMax = new Vector2(-EndGameContentHorizontalInset, -topInset);
                }

                if (endGameActionBarRoot != null)
                {
                    endGameActionBarRoot.gameObject.SetActive(useBottomActionBar);
                }

                if (endGameResultsScrollRoot != null)
                {
                    endGameResultsScrollRoot.gameObject.SetActive(!showPostAdaptationConfirmationState);
                }

                if (endGamePostAdaptationRoot != null)
                {
                    endGamePostAdaptationRoot.gameObject.SetActive(UseVerticalActionStack);
                }

                if (endGameMainColumnRoot != null)
                {
                    if (legacyResultsTitleText != null)
                    {
                        legacyResultsTitleText.transform.SetSiblingIndex(0);
                    }

                    if (requiresMoldinessRewardSelection)
                    {
                        if (endGameResultsScrollRoot != null)
                        {
                            endGameResultsScrollRoot.SetSiblingIndex(1);
                        }

                        if (endGamePostAdaptationRoot != null)
                        {
                            endGamePostAdaptationRoot.SetSiblingIndex(2);
                        }
                    }
                    else if (showPostAdaptationConfirmationState)
                    {
                        if (endGamePostAdaptationRoot != null)
                        {
                            endGamePostAdaptationRoot.SetSiblingIndex(1);
                        }
                    }
                }

                if (endGameTestingRailLayoutElement != null)
                {
                    endGameTestingRailLayoutElement.minWidth = postVictoryTestingRailVisible ? EndGameRailWidth : 0f;
                    endGameTestingRailLayoutElement.preferredWidth = postVictoryTestingRailVisible ? EndGameRailWidth : 0f;
                }

                if (endGameMainColumnRoot != null)
                {
                    var mainColumnElement = endGameMainColumnRoot.GetComponent<LayoutElement>();
                    if (mainColumnElement != null)
                    {
                        if (showPostAdaptationConfirmationState)
                        {
                            mainColumnElement.minWidth = EndGameConfirmationStackWidth;
                            mainColumnElement.preferredWidth = EndGameConfirmationStackWidth;
                            mainColumnElement.flexibleWidth = 0f;
                            mainColumnElement.flexibleHeight = 0f;
                        }
                        else if (requiresMoldinessRewardSelection)
                        {
                            mainColumnElement.minWidth = PendingMoldinessRewardPanelWidth;
                            mainColumnElement.preferredWidth = PendingMoldinessRewardPanelWidth;
                            mainColumnElement.flexibleWidth = 0f;
                            mainColumnElement.flexibleHeight = 1f;
                        }
                        else
                        {
                            mainColumnElement.minWidth = 560f;
                            mainColumnElement.preferredWidth = -1f;
                            mainColumnElement.flexibleWidth = 1f;
                            mainColumnElement.flexibleHeight = 1f;
                        }
                    }
                }

                ConfigureActionBarButtonLayout(playAgainButton);
                ConfigureActionBarButtonLayout(continueButton);
                ConfigureActionBarButtonLayout(exitButton);

                Canvas.ForceUpdateCanvases();

                if (endGameContentShellRoot != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(endGameContentShellRoot);
                }

                if (endGameMainColumnRoot != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(endGameMainColumnRoot);
                }

                if (resultsContainer is RectTransform resultsRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(resultsRect);

                    float targetContentHeight = LayoutUtility.GetPreferredHeight(resultsRect);
                    if (targetContentHeight > 0f)
                    {
                        resultsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetContentHeight);
                    }

                    if (endGameResultsViewportRoot != null)
                    {
                        float viewportWidth = endGameResultsViewportRoot.rect.width;
                        if (viewportWidth > 0f)
                        {
                            resultsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
                        }
                    }

                    resultsRect.anchoredPosition = Vector2.zero;
                }

                if (endGameResultsScrollRoot != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(endGameResultsScrollRoot);
                }

                if (endGameActionBarRoot != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(endGameActionBarRoot);
                }

                if (endGameResultsScrollRect != null && resetResultsScrollPositionOnNextLayout)
                {
                    endGameResultsScrollRect.StopMovement();
                    endGameResultsScrollRect.verticalNormalizedPosition = 1f;
                    resetResultsScrollPositionOnNextLayout = false;
                }
                else if (endGameResultsScrollRect == null)
                {
                    resetResultsScrollPositionOnNextLayout = false;
                }
            }
            finally
            {
                isRefreshingRuntimeLayout = false;
            }
        }

        private bool IsAnyActionButtonVisible()
        {
            return IsButtonVisible(playAgainButton) || IsButtonVisible(continueButton) || IsButtonVisible(exitButton);
        }

        private static bool IsButtonVisible(Button button)
        {
            return button != null && button.gameObject.activeSelf;
        }

        private static void ConfigureActionBarButtonLayout(Button button)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = 56f;
            layout.minHeight = 52f;
            layout.preferredWidth = EndGameActionButtonPreferredWidth;
            layout.minWidth = EndGameActionButtonMinWidth;
            layout.flexibleWidth = 0f;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static TMP_Dropdown FindDropdownTemplate()
        {
            return FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
        }

        private static DevelopmentTestingConfiguration BuildTestingConfigurationFromGameManager(GameManager manager)
        {
            bool isEnabled = manager != null && manager.IsTestingModeEnabled;
            bool skipToEnd = isEnabled && manager.testingSkipToEndgameAfterFastForward;
            ForcedGameResultMode forcedResult = skipToEnd && manager != null
                ? manager.TestingForcedGameResult
                : ForcedGameResultMode.Natural;
            string forcedAdaptationId = skipToEnd && manager != null
                ? manager.TestingForcedAdaptationId
                : string.Empty;

            return new DevelopmentTestingConfiguration(
                isEnabled,
                null,
                manager?.TestingMycovariantId,
                manager != null ? Mathf.Max(0, manager.fastForwardRounds) : 0,
                skipToEnd,
                false,
                forcedResult,
                manager != null && manager.TestingForceMoldinessRewards,
                manager != null ? manager.TestingCampaignLevelIndex : 0,
                forcedAdaptationId,
                manager?.TestingForcedStartingAdaptationIds);
        }

        private void CreateHeaderCell(Transform parent, string text, float preferredWidth, TextAlignmentOptions alignment, bool flexible)
        {
            var cell = new GameObject($"UI_GameEndHeader_{text}", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            cell.transform.SetParent(parent, false);

            var layout = cell.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = flexible ? 1f : -1f;

            var label = cell.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = UIStyleTokens.Text.Primary;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 21f;
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 14f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void EnsureButtonLayout(Button button)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = 56f;
            layout.minHeight = 52f;
            layout.preferredWidth = 380f;
            layout.minWidth = 320f;
            layout.flexibleWidth = 0f;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
                rect.pivot = new Vector2(0.5f, rect.pivot.y);
            }
        }

        private void EnsurePostVictoryTestingControls()
        {
            EnsureRuntimeLayoutScaffold();

            if (playAgainButton == null || endGameTestingRailRoot == null)
            {
                return;
            }

            if (postVictoryTestingCardController != null)
            {
                postVictoryTestingRoot = postVictoryTestingCardController.RootObject;
                return;
            }

            var buttonTemplate = exitButton != null ? exitButton : (continueButton != null ? continueButton : playAgainButton);
            if (buttonTemplate == null)
            {
                return;
            }

            postVictoryTestingCardController = new DevelopmentTestingCardController(new DevelopmentTestingCardOptions
            {
                Parent = endGameTestingRailRoot,
                ButtonTemplate = buttonTemplate,
                DropdownTemplate = FindDropdownTemplate(),
                SupportsBoardSizeOverride = false,
                SupportsForcedAdaptation = true,
                SupportsForceMoldinessRewards = true,
                SupportsFirstGameToggle = false,
                CardName = "UI_EndGameTestingCard",
                ControlPrefix = "UI_EndGameTesting",
                LogPrefix = "UI_EndGamePanel",
                LayoutInvalidated = RefreshRuntimeEndGameLayout,
                CardWidth = EndGameRailWidth,
                SettingWidth = EndGameRailWidth - 24f
            });
            postVictoryTestingCardController.Build();
            postVictoryTestingRoot = postVictoryTestingCardController.RootObject;

            if (postVictoryTestingRoot != null)
            {
                postVictoryTestingRoot.SetActive(false);
            }

            UpdatePostVictoryTestingVisibility(false);
        }

        private Button CreatePostVictorySettingButton(Transform newParent, string name, UnityEngine.Events.UnityAction action)
        {
            var template = exitButton != null ? exitButton : continueButton;
            if (template == null)
            {
                return null;
            }

            var clone = Instantiate(template.gameObject, newParent);
            clone.name = name;

            var button = clone.GetComponent<Button>();
            if (button == null)
            {
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.interactable = true;

            UIStyleTokens.Button.ApplyStyle(button);

            EnsureButtonLayout(button);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = 42f;
                layout.minHeight = 40f;
                layout.preferredWidth = 440f;
                layout.minWidth = 320f;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = 28f;
                label.fontSizeMin = 18f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = UIStyleTokens.Button.TextDefault;
            }

            return button;
        }

        private GameObject CreatePostVictoryMycovariantRow(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            TMP_Dropdown template = FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
            if (template == null)
            {
                Debug.LogWarning("UI_EndGamePanel: Unable to create Mycovariant dropdown because no TMP_Dropdown template was found in scene.");
                return null;
            }

            var row = new GameObject("UI_PostVictoryMycovariantRow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            var rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.spacing = 4f;
            rowLayout.padding = new RectOffset(4, 4, 2, 2);

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 86f;
            rowElement.minHeight = 80f;

            var labelObj = new GameObject("UI_PostVictoryMycovariantLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObj.transform.SetParent(row.transform, false);
            var label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Mycovariant";
            label.color = UIStyleTokens.Text.Primary;
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 15f;
            label.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.GetComponent<LayoutElement>();
            labelLayout.preferredHeight = 28f;
            labelLayout.minHeight = 24f;

            var dropdownObj = Instantiate(template.gameObject, row.transform);
            dropdownObj.name = "UI_PostVictoryMycovariantDropdown";
            postVictoryMycovariantDropdown = dropdownObj.GetComponent<TMP_Dropdown>();
            if (postVictoryMycovariantDropdown != null)
            {
                postVictoryMycovariantDropdown.onValueChanged.RemoveAllListeners();
                postVictoryMycovariantDropdown.onValueChanged.AddListener(OnPostVictoryMycovariantDropdownChanged);
            }

            var dropdownLayout = dropdownObj.GetComponent<LayoutElement>();
            if (dropdownLayout == null)
            {
                dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
            }

            dropdownLayout.preferredHeight = 44f;
            dropdownLayout.minHeight = 40f;
            dropdownLayout.preferredWidth = 440f;
            dropdownLayout.minWidth = 320f;

            PopulatePostVictoryMycovariantDropdown();
            return row;
        }

        private GameObject CreatePostVictoryAdaptationRow(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            TMP_Dropdown template = FindAnyObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
            if (template == null)
            {
                Debug.LogWarning("UI_EndGamePanel: Unable to create Adaptation dropdown because no TMP_Dropdown template was found in scene.");
                return null;
            }

            var row = new GameObject("UI_PostVictoryAdaptationRow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            var rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.spacing = 4f;
            rowLayout.padding = new RectOffset(4, 4, 2, 2);

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 86f;
            rowElement.minHeight = 80f;

            var labelObj = new GameObject("UI_PostVictoryAdaptationLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObj.transform.SetParent(row.transform, false);
            var label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Adaptation";
            label.color = UIStyleTokens.Text.Primary;
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 15f;
            label.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.GetComponent<LayoutElement>();
            labelLayout.preferredHeight = 28f;
            labelLayout.minHeight = 24f;

            var dropdownObj = Instantiate(template.gameObject, row.transform);
            dropdownObj.name = "UI_PostVictoryAdaptationDropdown";
            postVictoryAdaptationDropdown = dropdownObj.GetComponent<TMP_Dropdown>();
            if (postVictoryAdaptationDropdown != null)
            {
                postVictoryAdaptationDropdown.onValueChanged.RemoveAllListeners();
                postVictoryAdaptationDropdown.onValueChanged.AddListener(OnPostVictoryAdaptationDropdownChanged);
            }

            var dropdownLayout = dropdownObj.GetComponent<LayoutElement>();
            if (dropdownLayout == null)
            {
                dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
            }

            dropdownLayout.preferredHeight = 44f;
            dropdownLayout.minHeight = 40f;
            dropdownLayout.preferredWidth = 440f;
            dropdownLayout.minWidth = 320f;

            PopulatePostVictoryAdaptationDropdown();
            return row;
        }

        private void PopulatePostVictoryMycovariantDropdown()
        {
            if (postVictoryMycovariantDropdown == null)
            {
                return;
            }

            var options = new List<string> { "None" };
            var all = FungusToast.Core.Mycovariants.MycovariantRepository.All;
            for (int i = 0; i < all.Count; i++)
            {
                options.Add($"{all[i].Name} (ID: {all[i].Id})");
            }

            postVictoryMycovariantDropdown.ClearOptions();
            postVictoryMycovariantDropdown.AddOptions(options);
            postVictoryMycovariantDropdown.value = 0;
            postVictoryMycovariantDropdown.RefreshShownValue();

            if (postVictoryMycovariantDropdown.captionText != null)
            {
                postVictoryMycovariantDropdown.captionText.color = UIStyleTokens.Button.TextDefault;
            }

            if (postVictoryMycovariantDropdown.itemText != null)
            {
                postVictoryMycovariantDropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            ApplyDropdownReadability(postVictoryMycovariantDropdown);
        }

        private void PopulatePostVictoryAdaptationDropdown()
        {
            if (postVictoryAdaptationDropdown == null)
            {
                return;
            }

            postVictorySortedAdaptations = AdaptationRepository.All
                .OrderBy(adaptation => adaptation.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(adaptation => adaptation.Id, StringComparer.Ordinal)
                .ToList();

            var options = new List<string> { "None" };
            for (int i = 0; i < postVictorySortedAdaptations.Count; i++)
            {
                var adaptation = postVictorySortedAdaptations[i];
                options.Add($"{adaptation.Name} (ID: {adaptation.Id})");
            }

            postVictoryAdaptationDropdown.ClearOptions();
            postVictoryAdaptationDropdown.AddOptions(options);
            postVictoryAdaptationDropdown.value = 0;
            postVictoryAdaptationDropdown.RefreshShownValue();

            if (postVictoryAdaptationDropdown.captionText != null)
            {
                postVictoryAdaptationDropdown.captionText.color = UIStyleTokens.Button.TextDefault;
            }

            if (postVictoryAdaptationDropdown.itemText != null)
            {
                postVictoryAdaptationDropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            ApplyDropdownReadability(postVictoryAdaptationDropdown);
        }

        private void OnPostVictoryMycovariantDropdownChanged(int index)
        {
            if (index <= 0)
            {
                postVictoryForcedMycovariantId = null;
                return;
            }

            var all = FungusToast.Core.Mycovariants.MycovariantRepository.All;
            int mapped = index - 1;
            if (mapped >= 0 && mapped < all.Count)
            {
                postVictoryForcedMycovariantId = all[mapped].Id;
            }
            else
            {
                postVictoryForcedMycovariantId = null;
            }
        }

        private void OnPostVictoryAdaptationDropdownChanged(int index)
        {
            if (index <= 0)
            {
                postVictoryForcedAdaptationId = string.Empty;
                return;
            }

            int mapped = index - 1;
            if (mapped >= 0 && mapped < postVictorySortedAdaptations.Count)
            {
                postVictoryForcedAdaptationId = postVictorySortedAdaptations[mapped].Id;
            }
            else
            {
                postVictoryForcedAdaptationId = string.Empty;
            }
        }

        private void UpdatePostVictoryTestingVisibility(bool visible)
        {
            EnsurePostVictoryTestingControls();
            postVictoryTestingRailRequestedVisible = visible && postVictoryTestingCardController != null;
            ApplyPostVictoryTestingRailVisibility(reloadConfiguration: true);

            ApplyControlReadabilityOverrides();
            RefreshRuntimeEndGameLayout();
        }

        private bool ShouldShowTestingRailForCurrentWidth()
        {
            return postVictoryTestingRailRequestedVisible
                && resultsCardBackground != null
                && resultsCardBackground.rectTransform.rect.width >= EndGameTestingRailMinimumCardWidth;
        }

        private void ApplyPostVictoryTestingRailVisibility(bool reloadConfiguration)
        {
            bool showTestingRail = ShouldShowTestingRailForCurrentWidth();

            if (endGameTestingRailRoot != null)
            {
                endGameTestingRailRoot.gameObject.SetActive(showTestingRail);
            }

            if (postVictoryTestingRoot != null)
            {
                postVictoryTestingRoot.SetActive(showTestingRail);
            }

            if (showTestingRail && postVictoryTestingCardController != null && (reloadConfiguration || !postVictoryTestingRailVisible))
            {
                postVictoryTestingCardController.LoadConfiguration(BuildTestingConfigurationFromGameManager(GameManager.Instance));
            }

            postVictoryTestingRailVisible = showTestingRail;
        }

        private void EnsurePostVictoryControlOrder()
        {
            if (playAgainButton == null || postVictoryTestingRoot == null)
            {
                return;
            }

            var parent = playAgainButton.transform.parent;
            if (parent == null)
            {
                return;
            }

            if (continueButton != null && continueButton.transform.parent != parent)
            {
                continueButton.transform.SetParent(parent, false);
            }

            if (exitButton != null && exitButton.transform.parent != parent)
            {
                exitButton.transform.SetParent(parent, false);
            }

            int nextIndex = playAgainButton.transform.GetSiblingIndex() + 1;
            postVictoryTestingRoot.transform.SetSiblingIndex(nextIndex);
            nextIndex++;

            if (continueButton != null)
            {
                continueButton.transform.SetSiblingIndex(nextIndex);
                nextIndex++;
            }

            if (exitButton != null)
            {
                exitButton.transform.SetSiblingIndex(nextIndex);
            }
        }

        private void SyncPostVictoryTestingDefaultsFromGameManager()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            postVictoryTestingEnabled = manager.IsTestingModeEnabled;
            postVictoryFastForwardRounds = Mathf.Max(0, manager.fastForwardRounds);
            postVictorySkipToEnd = manager.testingSkipToEndgameAfterFastForward;
            postVictoryForcedResult = manager.TestingForcedGameResult;
            postVictoryForcedMycovariantId = manager.TestingMycovariantId;
            postVictoryForcedAdaptationId = manager.TestingForcedAdaptationId;

            if (!postVictorySkipToEnd && postVictoryForcedResult != ForcedGameResultMode.Natural)
            {
                postVictoryForcedResult = ForcedGameResultMode.Natural;
            }

            if (!postVictorySkipToEnd)
            {
                postVictoryForcedAdaptationId = string.Empty;
            }
        }

        private void UpdatePostVictoryTestingLabels()
        {
            postVictoryTestingCardController?.RefreshVisualState();
            RefreshRuntimeEndGameLayout();
        }

        private void OnPostVictoryTestingToggled()
        {
            postVictoryTestingEnabled = !postVictoryTestingEnabled;

            if (!postVictoryTestingEnabled)
            {
                postVictoryFastForwardRounds = 0;
                postVictorySkipToEnd = false;
                postVictoryForcedResult = ForcedGameResultMode.Natural;
                postVictoryForcedMycovariantId = null;
                postVictoryForcedAdaptationId = string.Empty;
            }

            if (postVictoryFastForwardButton != null)
                postVictoryFastForwardButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantRow != null)
                postVictoryMycovariantRow.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantDropdown != null)
                postVictoryMycovariantDropdown.interactable = postVictoryTestingEnabled;

            if (postVictoryAdaptationRow != null)
                postVictoryAdaptationRow.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            if (postVictoryAdaptationDropdown != null)
                postVictoryAdaptationDropdown.interactable = postVictoryTestingEnabled && postVictorySkipToEnd;

            if (postVictorySkipToEndButton != null)
                postVictorySkipToEndButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryForcedResultButton != null)
                postVictoryForcedResultButton.gameObject.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            if (!postVictorySkipToEnd)
            {
                postVictoryForcedAdaptationId = string.Empty;
            }

            UpdatePostVictoryTestingLayoutHeight();
            UpdatePostVictoryTestingLabels();
            ApplyControlReadabilityOverrides();
        }

        private void OnPostVictoryFastForwardCycle()
        {
            postVictoryFastForwardRounds = DevelopmentTestingFastForwardPresets.GetNext(postVictoryFastForwardRounds);

            UpdatePostVictoryTestingLabels();
        }

        private void OnPostVictorySkipToEndToggled()
        {
            postVictorySkipToEnd = !postVictorySkipToEnd;
            if (postVictorySkipToEnd)
            {
                postVictoryForcedResult = ForcedGameResultMode.ForcedWin;
            }
            else
            {
                postVictoryForcedResult = ForcedGameResultMode.Natural;
                postVictoryForcedAdaptationId = string.Empty;
            }

            if (postVictoryForcedResultButton != null)
                postVictoryForcedResultButton.gameObject.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            if (postVictoryAdaptationRow != null)
                postVictoryAdaptationRow.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            if (postVictoryAdaptationDropdown != null)
                postVictoryAdaptationDropdown.interactable = postVictoryTestingEnabled && postVictorySkipToEnd;

            UpdatePostVictoryTestingLayoutHeight();
            UpdatePostVictoryTestingLabels();
        }

        private void UpdatePostVictoryTestingLayoutHeight()
        {
            if (postVictoryTestingRoot == null)
            {
                return;
            }

            var rootElement = postVictoryTestingRoot.GetComponent<LayoutElement>();
            if (rootElement == null)
            {
                return;
            }

            float height = 16f; // top/bottom padding budget
            if (postVictoryTestingToggleButton != null && postVictoryTestingToggleButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictoryMycovariantRow != null && postVictoryMycovariantRow.activeSelf) height += 86f + 6f;
            if (postVictoryAdaptationRow != null && postVictoryAdaptationRow.activeSelf) height += 86f + 6f;
            if (postVictoryFastForwardButton != null && postVictoryFastForwardButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictorySkipToEndButton != null && postVictorySkipToEndButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictoryForcedResultButton != null && postVictoryForcedResultButton.gameObject.activeSelf) height += 42f + 6f;

            height = Mathf.Max(56f, height);
            rootElement.preferredHeight = height;
            rootElement.minHeight = height;
        }

        private void ApplyControlReadabilityOverrides()
        {
            UIStyleTokens.Button.SetButtonLabelColor(continueButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(exitButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(playAgainButton, UIStyleTokens.Button.TextDefault);

            UIStyleTokens.Button.SetButtonLabelColor(postVictoryTestingToggleButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictoryFastForwardButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictorySkipToEndButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictoryForcedResultButton, UIStyleTokens.Button.TextDefault);

            if (detailsCloseButton != null)
            {
                UIStyleTokens.Button.ApplyPanelSecondaryStyle(detailsCloseButton);
            }

            ApplyDropdownReadability(postVictoryMycovariantDropdown);
            ApplyDropdownReadability(postVictoryAdaptationDropdown);
            ApplyResultsHeaderReadabilityOverrides();
        }

        private void ApplyResultsHeaderReadabilityOverrides()
        {
            var tmpLabels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpLabels.Length; i++)
            {
                var label = tmpLabels[i];
                if (!IsResultsHeaderLabel(label, out bool isTitle))
                {
                    continue;
                }

                label.color = UIStyleTokens.Text.Primary;
                label.fontStyle |= FontStyles.Bold;
            }

            var legacyLabels = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyLabels.Length; i++)
            {
                var label = legacyLabels[i];
                if (!IsResultsHeaderLabel(label, out bool isTitle))
                {
                    continue;
                }

                label.color = UIStyleTokens.Text.Primary;
                label.fontStyle = FontStyle.Bold;
            }
        }

        private static bool IsResultsHeaderLabel(TMP_Text label, out bool isTitle)
        {
            isTitle = false;
            if (label == null)
            {
                return false;
            }

            return TryClassifyResultsHeader(label.name, label.text, out isTitle);
        }

        private static bool IsResultsHeaderLabel(Text label, out bool isTitle)
        {
            isTitle = false;
            if (label == null)
            {
                return false;
            }

            return TryClassifyResultsHeader(label.name, label.text, out isTitle);
        }

        private static bool TryClassifyResultsHeader(string name, string text, out bool isTitle)
        {
            isTitle = false;

            if (IsResultsTitleName(name) || IsResultsTitleText(text))
            {
                isTitle = true;
                return true;
            }

            return IsResultsColumnHeaderName(name) || IsResultsColumnHeaderText(text);
        }

        private static bool IsResultsTitleName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return name.IndexOf("GameEndResultsHeaderText", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("GameResults", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsResultsTitleText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.IndexOf("Game Results", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsResultsColumnHeaderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return name.IndexOf("GameEndHeader_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("AliveHeaderText", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ResistantHeaderText", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("DeadHeaderText", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ToxinHeaderText", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("PlayerSummariesPanelHeaderRow", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsResultsColumnHeaderText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return string.Equals(text, "Player", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Alive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Resistant", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Dead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Toxins", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Details", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyDropdownReadability(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = UIStyleTokens.Button.TextDefault;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            var labels = dropdown.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                if (label.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    label.color = UIStyleTokens.Text.Disabled;
                }
                else
                {
                    label.color = UIStyleTokens.Button.TextDefault;
                }
            }
        }

        private void OnPostVictoryForcedResultCycle()
        {
            postVictoryForcedResult = postVictoryForcedResult switch
            {
                ForcedGameResultMode.Natural => ForcedGameResultMode.ForcedWin,
                ForcedGameResultMode.ForcedWin => ForcedGameResultMode.ForcedLoss,
                _ => ForcedGameResultMode.Natural
            };

            UpdatePostVictoryTestingLabels();
        }

        private void ApplyPostVictoryTestingSettings(GameManager manager)
        {
            if (manager == null || postVictoryTestingCardController == null)
            {
                return;
            }

            postVictoryTestingCardController.ApplyToGameManager(manager);
        }

        private static string FormatForcedResult(ForcedGameResultMode mode)
        {
            return mode switch
            {
                ForcedGameResultMode.ForcedWin => "Forced Win",
                ForcedGameResultMode.ForcedLoss => "Forced Loss",
                _ => "Natural"
            };
        }

        private void EnsureActionButtonsShareContainer()
        {
            EnsureRuntimeLayoutScaffold();

            var targetParent = UseVerticalActionStack ? endGamePostAdaptationRoot : endGameActionBarRoot;
            if (targetParent == null)
            {
                return;
            }

            if (playAgainButton != null && playAgainButton.transform.parent != targetParent)
            {
                playAgainButton.transform.SetParent(targetParent, false);
            }

            if (continueButton != null && continueButton.transform.parent != targetParent)
            {
                continueButton.transform.SetParent(targetParent, false);
            }

            if (exitButton != null && exitButton.transform.parent != targetParent)
            {
                exitButton.transform.SetParent(targetParent, false);
            }

            int nextIndex = 0;
            if (UseVerticalActionStack)
            {
                if (continueButton != null)
                {
                    continueButton.transform.SetSiblingIndex(nextIndex);
                    nextIndex++;
                }

                if (playAgainButton != null)
                {
                    playAgainButton.transform.SetSiblingIndex(nextIndex);
                    nextIndex++;
                }

                if (exitButton != null)
                {
                    exitButton.transform.SetSiblingIndex(nextIndex);
                }
            }
            else
            {
                if (playAgainButton != null)
                {
                    playAgainButton.transform.SetSiblingIndex(nextIndex);
                    nextIndex++;
                }

                if (continueButton != null)
                {
                    continueButton.transform.SetSiblingIndex(nextIndex);
                    nextIndex++;
                }

                if (exitButton != null)
                {
                    exitButton.transform.SetSiblingIndex(nextIndex);
                }
            }

            if (UseVerticalActionStack)
            {
                ConfigureVerticalActionStackButtonLayout(continueButton);
                ConfigureVerticalActionStackButtonLayout(playAgainButton);
                ConfigureVerticalActionStackButtonLayout(exitButton);
            }
            else
            {
                ConfigureActionBarButtonLayout(playAgainButton);
                ConfigureActionBarButtonLayout(continueButton);
                ConfigureActionBarButtonLayout(exitButton);
            }
        }

        private void ConfigureVerticalActionStackButtonLayout(Button button)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            bool useCompactWidth = button == exitButton;
            float buttonWidth = useCompactWidth ? EndGameConfirmationCompactButtonWidth : EndGameConfirmationPrimaryButtonWidth;

            layout.preferredHeight = EndGameConfirmationButtonHeight;
            layout.minHeight = EndGameConfirmationButtonHeight - 4f;
            layout.preferredWidth = buttonWidth;
            layout.minWidth = buttonWidth;
            layout.flexibleWidth = 0f;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.enableAutoSizing = true;
                label.fontSizeMax = 28f;
                label.fontSizeMin = 18f;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private void EnsureButtonContainerLayout()
        {
            if (endGameActionBarRoot == null)
            {
                return;
            }

            var layout = endGameActionBarRoot.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        private void EnsureOutcomePlacement()
        {
            if (outcomeLabel == null || resultsCardBackground == null)
            {
                return;
            }

            var desiredParent = resultsCardBackground.transform;
            if (outcomeLabel.transform.parent != desiredParent)
            {
                outcomeLabel.transform.SetParent(desiredParent, false);
                outcomeLabel.transform.SetAsLastSibling();
            }

            if (outcomeBackdrop == null)
            {
                var existing = desiredParent.Find("UI_EndGameOutcomeBackdrop");
                if (existing != null)
                {
                    outcomeBackdrop = existing.GetComponent<Image>();
                }
            }

            if (outcomeBackdrop == null)
            {
                var backdropObject = new GameObject("UI_EndGameOutcomeBackdrop", typeof(RectTransform), typeof(Image));
                backdropObject.transform.SetParent(desiredParent, false);
                backdropObject.transform.SetSiblingIndex(outcomeLabel.transform.GetSiblingIndex());
                outcomeBackdrop = backdropObject.GetComponent<Image>();
            }

            EnsureIgnoreParentLayout(outcomeLabel.gameObject);

            if (outcomeBackdrop != null)
            {
                EnsureIgnoreParentLayout(outcomeBackdrop.gameObject);

                var backdropColor = UIStyleTokens.Surface.PanelSecondary;
                backdropColor.a = 0.92f;
                outcomeBackdrop.color = backdropColor;

                var backdropRect = outcomeBackdrop.rectTransform;
                backdropRect.anchorMin = new Vector2(OutcomeBackdropAnchorMinX, OutcomeBackdropAnchorMinY);
                backdropRect.anchorMax = new Vector2(OutcomeBackdropAnchorMaxX, OutcomeBackdropAnchorMaxY);
                backdropRect.pivot = new Vector2(0.5f, 0.5f);
                backdropRect.anchoredPosition = Vector2.zero;
                backdropRect.offsetMin = Vector2.zero;
                backdropRect.offsetMax = Vector2.zero;

                // Keep backdrop behind label while staying above base card background.
                if (outcomeLabel != null)
                {
                    outcomeBackdrop.transform.SetSiblingIndex(Mathf.Max(0, outcomeLabel.transform.GetSiblingIndex() - 1));
                    outcomeLabel.transform.SetAsLastSibling();
                }
            }
        }

        private static void EnsureIgnoreParentLayout(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
        }

        private void SetOutcomeBannerVisibility(bool visible)
        {
            if (outcomeLabel != null)
            {
                outcomeLabel.gameObject.SetActive(visible);
            }

            if (outcomeBackdrop != null)
            {
                outcomeBackdrop.gameObject.SetActive(visible);
            }

            RefreshRuntimeEndGameLayout();
        }

        private static void EnsureTooltip(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<TooltipTrigger>();
            }

            trigger.SetStaticText(text);
        }

        private static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        private void SetLegacyResultsHeaderVisibility(bool visible)
        {
            CacheLegacyResultsHeaderCandidates();
            for (int i = 0; i < legacyResultsHeaderCandidates.Count; i++)
            {
                var candidate = legacyResultsHeaderCandidates[i];
                if (candidate == null)
                {
                    continue;
                }

                candidate.gameObject.SetActive(visible);
            }

            RefreshRuntimeEndGameLayout();
        }

        private void CacheLegacyResultsHeaderCandidates()
        {
            if (legacyResultsHeaderCandidates.Count > 0)
            {
                return;
            }

            var tmpLabels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpLabels.Length; i++)
            {
                var label = tmpLabels[i];
                if (label == null || label == outcomeLabel)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(label.text) &&
                    label.text.IndexOf("Game Results", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    legacyResultsHeaderCandidates.Add(label);
                }
            }

            var tmpLegacyLabels = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < tmpLegacyLabels.Length; i++)
            {
                var label = tmpLegacyLabels[i];
                if (label == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(label.text) &&
                    label.text.IndexOf("Game Results", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    legacyResultsHeaderCandidates.Add(label);
                }
            }
        }
    }
}
