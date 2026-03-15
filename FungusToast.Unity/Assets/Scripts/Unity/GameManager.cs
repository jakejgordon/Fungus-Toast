using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Campaign;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using FungusToast.Unity.Cameras;
using FungusToast.Unity.Events;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Phases;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.GameStart;
using FungusToast.Unity.UI.MycovariantDraft;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Hotseat;
using FungusToast.Unity.UI.GameLog; // added for EnablePlayerSpecificFiltering
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using FungusToast.Unity.Campaign; // NEW: campaign namespace

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Board Settings")] 
        public int boardWidth =160; 
        public int boardHeight =160; 
        public int playerCount =2;

        [Header("Testing Mode")] 
        public bool testingModeEnabled = false; 
        public int? testingMycovariantId = null; 
        public string testingForcedAdaptationId = string.Empty;
        public bool testingModeForceHumanFirst = true; 
        public ForcedGameResultMode testingForcedGameResult = ForcedGameResultMode.Natural;
        public int fastForwardRounds =0; 
        public bool testingSkipToEndgameAfterFastForward = false;

        [Header("References")] 
        public GridVisualizer gridVisualizer; 
        public CameraCenterer cameraCenterer; 
        [SerializeField] private MutationManager mutationManager; 
        [SerializeField] private GrowthPhaseRunner growthPhaseRunner; 
        [SerializeField] private GameUIManager gameUIManager; 
        [SerializeField] private DecayPhaseRunner decayPhaseRunner; 
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker; 
        [SerializeField] private MycovariantDraftController mycovariantDraftController; 
        [SerializeField] private UI_StartGamePanel startGamePanel; 
        [SerializeField] private UI_HotseatTurnPrompt hotseatTurnPrompt; 
        public GameObject SelectionPromptPanel; 
        public TextMeshProUGUI SelectionPromptText; 
        [SerializeField] private GameObject modeSelectPanel; // NEW: root of mode select UI

        [Header("Hotseat Config")] 
        public int configuredHumanPlayerCount =1; 
        public int ConfiguredHumanPlayerCount => configuredHumanPlayerCount; 
        public void SetHotseatConfig(int humanCount) => configuredHumanPlayerCount = Mathf.Max(1, humanCount);

        [Header("Campaign Config")] 
        public CampaignProgression campaignProgression; // assign ScriptableObject in inspector

        [Header("Magnifier")]
        [SerializeField] private MagnifyingGlassFollowMouse magnifyingGlass; // new serialized reference

        // NEW: current mode
        public GameMode CurrentGameMode { get; private set; } = GameMode.Hotseat;
        private CampaignController campaignController; // lazy created

        #endregion

        #region State Fields / Services

        private bool isCountdownActive = false; 
        private int roundsRemainingUntilGameEnd =0; 
        private bool gameEnded = false; 
        private System.Random rng; 
        private MycovariantPoolManager persistentPoolManager;

        public GameBoard Board { get; private set; } 
        public GameUIManager GameUI => gameUIManager; 
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new(); 
        private readonly List<Player> humanPlayers = new(); 
        private Player humanPlayer; // primary

        private bool isInDraftPhase = false; 
        public bool IsDraftPhaseActive => isInDraftPhase; 
        private int lastCompletedMycovariantDraftRound = -1;
        public int LastCompletedMycovariantDraftRound => lastCompletedMycovariantDraftRound;
        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        public bool IsTestingModeEnabled => testingModeEnabled; 
        public int? TestingMycovariantId => testingMycovariantId;
        public ForcedGameResultMode TestingForcedGameResult => testingForcedGameResult;

        private bool isFastForwarding = false; 
        public bool IsFastForwarding => isFastForwarding; 
        private bool _fastForwardStarted = false;
        private bool initialMutationPointsAssigned = false;
        private float nextUiStuckCheckTime;
        private bool isPauseMenuOpen;
        private UI_PauseMenuPanel pauseMenuPanel;

        // Services
        private PlayerInitializer playerInitializer; 
        private HotseatTurnManager hotseatTurnManager; 
        private FastForwardService fastForwardService; 
        private PostGrowthVisualSequence postGrowthVisualSequence;
        private EndgameService endgameService;
        private MutationPointService mutationPointService;
        private SpecialEventPresentationService specialEventPresentationService;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            FungusToast.Core.Logging.CoreLogger.Log = Debug.Log;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            rng = new System.Random();
            BootstrapServices();
            // Create campaign controller if progression present
            if (campaignProgression != null)
            {
                campaignController = new CampaignController(campaignProgression);
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                ForceClosePauseMenu();
                ShowStartGamePanel();
            }
        }

        private void Update()
        {
            HandlePauseMenuInput();

            // Safety net: recover from rare transition bugs where all gameplay UI roots end up hidden.
            if (Time.unscaledTime < nextUiStuckCheckTime)
            {
                return;
            }

            nextUiStuckCheckTime = Time.unscaledTime + 0.5f;
            RecoverGameplayUiIfStuck();
        }

        #endregion

        #region Bootstrap

        private void BootstrapServices()
        {
            BootstrapPauseMenu();

            playerInitializer = new PlayerInitializer(
                gridVisualizer,
                gameUIManager,
                () => configuredHumanPlayerCount,
                () => CurrentGameMode,
                () => campaignController?.CurrentBoardPreset);
            hotseatTurnManager = new HotseatTurnManager(
                gameUIManager,
                hotseatTurnPrompt,
                humanPlayers,
                () => humanPlayer,
                () => isFastForwarding,
                () => testingModeEnabled,
                OnAllHumansFinishedMutationTurn);
            fastForwardService = new FastForwardService(this, () => isFastForwarding, v => isFastForwarding = v, () => endgameService.GameEnded);
            fastForwardService.SetProgressCallbacks(
                progress => { gameUIManager.LoadingScreen?.Show(progress); },
                () => { gameUIManager.LoadingScreen?.FadeOut(); });
            postGrowthVisualSequence = new PostGrowthVisualSequence(this, gridVisualizer, () => isFastForwarding, StartDecayPhase);
            endgameService = new EndgameService(
                gameUIManager,
                () => Board,
                () => humanPlayer,
                () => CurrentGameMode,
                () => campaignController,
                () => campaignProgression,
                () => FirstUpgradeRounds,
                () => testingModeEnabled,
                () => testingForcedGameResult);
            mutationPointService = new MutationPointService(
                gameUIManager,
                () => Board,
                () => mutationManager,
                () => rng,
                () => isFastForwarding,
                () => testingModeEnabled,
                () => fastForwardRounds,
                StartGrowthPhase);
            specialEventPresentationService = new SpecialEventPresentationService(
                () => gameUIManager,
                () => gridVisualizer,
                () => humanPlayer,
                () => isFastForwarding);
        }

        #endregion

        #region Initialization

        // PUBLIC API to start modes (called by future UI panels)
        public void StartHotseatGame(int numberOfPlayers)
        {
            CurrentGameMode = GameMode.Hotseat;
            InitializeGame(numberOfPlayers);
        }

        public void StartCampaignNew()
        {
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot start campaign: CampaignProgression not assigned.");
                return;
            }
            campaignController.StartNew();
            CurrentGameMode = GameMode.Campaign;
            var preset = campaignController.CurrentBoardPreset;
            if (preset != null)
            {
                boardWidth = preset.boardWidth;
                boardHeight = preset.boardHeight;
            }
            int totalPlayers =1 + (preset?.aiPlayers?.Count ??0);
            SetHotseatConfig(1);
            InitializeGame(totalPlayers);
        }

        public void StartCampaignResume()
        {
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot resume campaign: CampaignProgression not assigned.");
                return;
            }
            campaignController.Resume();
            CurrentGameMode = GameMode.Campaign;

            if (campaignController.IsAwaitingAdaptationSelection
                && campaignController.TryGetPendingVictorySnapshot(out var pendingSnapshot)
                && pendingSnapshot != null)
            {
                ShowPendingCampaignVictoryScreen(pendingSnapshot);
                return;
            }

            var preset = campaignController.CurrentBoardPreset;
            if (preset != null)
            {
                boardWidth = preset.boardWidth;
                boardHeight = preset.boardHeight;
            }
            int totalPlayers =1 + (preset?.aiPlayers?.Count ??0);
            SetHotseatConfig(1);
            InitializeGame(totalPlayers);
        }

        private void ShowPendingCampaignVictoryScreen(CampaignVictorySnapshot snapshot)
        {
            if (snapshot == null || gameUIManager?.EndGamePanel == null)
            {
                Debug.LogWarning("[GameManager] Pending campaign victory snapshot missing; continuing directly into gameplay.");
                var presetFallback = campaignController.CurrentBoardPreset;
                if (presetFallback != null)
                {
                    boardWidth = presetFallback.boardWidth;
                    boardHeight = presetFallback.boardHeight;
                }

                int totalPlayersFallback = 1 + (presetFallback?.aiPlayers?.Count ?? 0);
                SetHotseatConfig(1);
                InitializeGame(totalPlayersFallback);
                return;
            }

            StopAllCoroutines();
            mycovariantDraftController?.StopAllCoroutines();

            modeSelectPanel?.SetActive(false);
            startGamePanel?.gameObject.SetActive(false);

            gameUIManager.LoadingScreen?.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager?.gameObject.SetActive(false);

            gameUIManager.EndGamePanel.ShowCampaignPendingVictorySnapshot(snapshot);
        }

        // Accessor for external panels
        public bool HasCampaignSave() => campaignController != null && CampaignSaveService.Exists();
        public bool IsCampaignAwaitingAdaptationSelection() =>
            CurrentGameMode == GameMode.Campaign && campaignController != null && campaignController.IsAwaitingAdaptationSelection;

        public bool TryStartCampaignAdaptationDraft(Action onSelectionComplete)
        {
            if (CurrentGameMode != GameMode.Campaign || campaignController == null || !campaignController.IsAwaitingAdaptationSelection)
            {
                return false;
            }

            var choices = campaignController.GetAdaptationDraftChoices(
                rng,
                3,
                testingModeEnabled ? testingForcedAdaptationId : string.Empty);
            if (choices.Count == 0)
            {
                Debug.Log("[GameManager] No remaining adaptations; advancing campaign level without reward.");
                bool advanced = campaignController.TryAdvanceWithoutAdaptationReward();
                if (advanced)
                {
                    onSelectionComplete?.Invoke();
                }
                return advanced;
            }

            if (mycovariantDraftController == null)
            {
                Debug.LogError("[GameManager] Cannot start campaign adaptation draft: MycovariantDraftController is missing.");
                return false;
            }

            mycovariantDraftController.StartCampaignAdaptationDraft(
                choices,
                selected =>
                {
                    bool applied = campaignController.TrySelectAdaptationAndAdvance(selected.Id);
                    if (!applied)
                    {
                        Debug.LogError($"[GameManager] Failed to apply selected adaptation '{selected.Id}'.");
                        return;
                    }

                    onSelectionComplete?.Invoke();
                });

            return true;
        }

        public void InitializeGame(int numberOfPlayers)
        {
            // Clear any lingering scene coroutines/UI overlays from the previous game before bootstrapping a new board.
            StopAllCoroutines();
            mycovariantDraftController?.StopAllCoroutines();
            gameUIManager?.MutationUIManager?.ResetForNewGameState();
            gameUIManager.EndGamePanel?.gameObject.SetActive(false);
            ForceClosePauseMenu();

            gameUIManager.LoadingScreen?.Show("Preparing the toast…");
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd =0;
            lastCompletedMycovariantDraftRound = -1;
            endgameService.Reset();
            specialEventPresentationService?.Reset();
            playerCount = numberOfPlayers;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            gameUIManager.SetBoard(Board); // expose board to UI components via façade
            rng = new System.Random();
            postGrowthVisualSequence.Register(Board); // board post-growth events

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, gameUIManager.GameLogRouter);
            GameUIEventSubscriber.Subscribe(Board, gameUIManager, specialEventPresentationService);
            AnalyticsEventSubscriber.Subscribe(Board, gameUIManager.GameLogRouter);

            playerInitializer.InitializePlayers(Board, players, humanPlayers, out humanPlayer, playerCount);
            ApplyCampaignAdaptations();
            SubscribeToPlayerMutationEvents();

            persistentPoolManager = new MycovariantPoolManager();
            persistentPoolManager.InitializePool(MycovariantRepository.All.ToList(), rng);

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            StartCoroutine(PlayStartingSporeIntroAndContinue());
            mutationManager.ResetMutationPoints(players);
            initialMutationPointsAssigned = true;

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            pauseMenuPanel?.SetGameplayVisibility(true);
            mycovariantDraftController?.gameObject.SetActive(false);
            cameraCenterer?.CaptureInitialFraming();
            InitGameLogs();

            if (testingModeEnabled)
            {
                if (fastForwardRounds <=0 && testingMycovariantId.HasValue)
                {
                    StartMycovariantDraftPhase();
                }
                else if (fastForwardRounds <=0)
                {
                    gameUIManager.PhaseBanner.Show("New Game Settings",2f);
                }
            }
            else
            {
                gameUIManager.PhaseBanner.Show(CurrentGameMode == GameMode.Campaign ? "Campaign Level" : "New Game Settings",2f);
            }

            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.SetBoard(Board);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.SetPerspectivePlayer(humanPlayer);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);

            if (cameraCenterer != null)
            {
                cameraCenterer.CenterCameraInstant();
                cameraCenterer.CaptureInitialFraming();
            }

            // Magnifier control
            MagnifyingGlassFollowMouse.gameStarted = true; // ensure enabled for all modes
            if (magnifyingGlass != null)
            {
                magnifyingGlass.ApplyBoardSizeGate(Board.Width, Board.Height);
            }
        }

        private void InitGameLogs()
        {
            if (gameUIManager.GameLogManager != null && gameUIManager.GameLogPanel != null)
            {
                gameUIManager.GameLogManager.Initialize(Board);
                gameUIManager.GameLogPanel.Initialize(gameUIManager.GameLogManager);
            }
            if (gameUIManager.GlobalGameLogManager != null && gameUIManager.GlobalGameLogPanel != null)
            {
                gameUIManager.GlobalGameLogManager.Initialize(Board);
                gameUIManager.GlobalGameLogPanel.Initialize(gameUIManager.GlobalGameLogManager);
            }
        }

        private void SubscribeToPlayerMutationEvents()
        {
            foreach (var p in Board.Players)
            {
                p.MutationsChanged += OnPlayerMutationsChanged;
            }
        }

        private void ApplyCampaignAdaptations()
        {
            if (CurrentGameMode != GameMode.Campaign || campaignController == null || humanPlayer == null)
            {
                return;
            }

            foreach (var adaptation in campaignController.GetSelectedAdaptations())
            {
                humanPlayer.TryAddAdaptation(adaptation);
            }
        }

        private void OnPlayerMutationsChanged(Player p)
        {
            if (p == humanPlayer && !isFastForwarding)
            {
                gameUIManager.MoldProfileRoot?.Refresh();
            }
        }

        #endregion

        #region Phase Control

        private void PlaceStartingSpores()
        {
            StartingSporeUtility.PlaceStartingSpores(Board, players, rng);
            int round = Board.CurrentRound;
            float occ = Board.GetOccupiedTileRatio() *100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
        }

        public void StartGrowthPhase()
        {
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
            gameUIManager.GameLogRouter?.OnPhaseStart("Growth");
            gameUIManager.GameLogManager?.OnLogSegmentStart("GrowthPhase");
            growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gridVisualizer.ClearNewlyGrownFlagsForNextGrowthPhase();
            gameUIManager.PhaseBanner.Show("Growth Phase Begins!",2f);
            phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
            Board.OnPreGrowthPhase();
            growthPhaseRunner.StartGrowthPhase();
        }

        public void StartDecayPhase()
        {
            if (gameEnded)
            {
                return;
            }
            gameUIManager.GameLogRouter?.OnPhaseStart("Decay");
            gameUIManager.GameLogManager?.OnLogSegmentStart("DecayPhase");
            decayPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!",2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase(
                growthPhaseRunner.FailedGrowthsByPlayerId,
                rng,
                gameUIManager.GameLogRouter,
                specialEventPresentationService);
        }

        public void OnRoundComplete()
        {
            if (gameEnded)
            {
                return;
            }
            gameUIManager.GameLogRouter?.OnRoundComplete(Board.CurrentRound, Board);
            foreach (var p in Board.Players)
            {
                p.TickDownActiveSurges();
            }
            CheckForEndgameCondition();
            if (endgameService.GameEnded)
            {
                return;
            }
            Board.IncrementRound();
            int round = Board.CurrentRound;
            float occ = Board.GetOccupiedTileRatio() *100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
            if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound))
            {
                StartCoroutine(DelayedStartDraft());
                return;
            }
            StartNextRound();
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
            gameUIManager.RightSidebar.UpdateRandomDecayChance(round);
            TrackFirstUpgradeRounds();
        }

        private void TrackFirstUpgradeRounds()
        {
            foreach (var player in Board.Players)
            {
                foreach (var pm in player.PlayerMutations.Values)
                {
                    if (!pm.FirstUpgradeRound.HasValue)
                    {
                        continue;
                    }
                    var key = (player.PlayerId, pm.MutationId);
                    if (!FirstUpgradeRounds.ContainsKey(key))
                    {
                        FirstUpgradeRounds[key] = new List<int>();
                    }
                    FirstUpgradeRounds[key].Add(pm.FirstUpgradeRound.Value);
                }
            }
        }

        public void StartNextRound()
        {
            if (gameEnded)
            {
                return;
            }

            // Safety net: if a transition bug ever leaves opening points at zero,
            // reseed round-1 mutation points so the game can always progress.
            if (Board != null
                && Board.CurrentRound == 1
                && Board.Players.Count > 0
                && Board.Players.All(p => p.MutationPoints <= 0))
            {
                Debug.LogWarning("[GameManager] Round 1 started with zero mutation points for all players. Reapplying starting mutation points.");
                mutationManager.ResetMutationPoints(Board.Players);
            }

            gameUIManager.GameLogRouter?.OnRoundStart(Board.CurrentRound);
            gameUIManager.GameLogManager?.OnLogSegmentStart("MutationPhaseStart");

            if (!(Board.CurrentRound ==1 && initialMutationPointsAssigned))
            {
                AssignMutationPoints();
            }
            else
            {
                Board.OnMutationPhaseStart();
            }

            if (humanPlayers.Count >0)
            {
                SetActiveHumanPlayer(humanPlayers[0]);
                gameUIManager.GameLogManager?.EmitPendingSegmentSummariesFor(humanPlayers[0].PlayerId);
            }

            hotseatTurnManager.BeginHumanMutationPhase();

            // Fail-safe: campaign continuation can traverse custom UI steps; ensure mutation controls are re-armed.
            if (humanPlayers.Count > 0)
            {
                var activeHuman = humanPlayers[0];
                gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
                gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();
                gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(activeHuman.MutationPoints > 0);
            }

            gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
            gameUIManager.GameLogRouter?.OnPhaseStart("Mutation");
            gameUIManager.PhaseBanner.Show("Mutation Phase Begins!",2f);
            if (specialEventPresentationService != null && specialEventPresentationService.HasPendingImmediateEvents)
            {
                StartCoroutine(specialEventPresentationService.PresentPendingImmediate());
            }
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
        }

        #endregion

        #region Hotseat Callbacks

        public void OnHumanMutationTurnFinished(Player player)
        {
            hotseatTurnManager.HandleHumanTurnFinished(player);
        }

        private void OnAllHumansFinishedMutationTurn()
        {
            SpendAllMutationPointsForAIPlayers();
        }

        #endregion

        #region Endgame / Countdown

        private void CheckForEndgameCondition()
        {
            endgameService.CheckForEndgameCondition();
            // Sync local flag for backward compatibility with existing checks
            gameEnded = endgameService.GameEnded;
        }

        private void EndGame()
        {
            endgameService.EndGame();
            gameEnded = endgameService.GameEnded;
        }

        #endregion

        #region Mutation Points / AI

        private void AssignMutationPoints()
        {
            mutationPointService.AssignMutationPoints();
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            mutationPointService.SpendAllMutationPointsForAIPlayers();
        }

        #endregion

        #region Draft Phase

        private void UpdatePhaseProgressTrackerLabel()
        {
            phaseProgressTracker?.SetMutationPhaseLabel(isInDraftPhase ? "DRAFT" : "MUTATION");
        }

        public void StartMycovariantDraftPhase()
        {
            isInDraftPhase = true;
            RefreshRightSidebarTopStats();
            TooltipManager.Instance?.CancelAll();
            // Mark draft phase segment boundary so prior aggregation (e.g., decay phase) is queued
            gameUIManager.GameLogManager?.OnLogSegmentStart("DraftPhase");
            var order = testingModeEnabled && testingModeForceHumanFirst
                ? Board.Players
                    .OrderBy(p => p.PlayerType == PlayerTypeEnum.Human ?0 :1)
                    .ThenBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ToList()
                : MycovariantDraftManager.BuildDraftOrder(Board.Players, Board);
            if (testingModeEnabled && testingModeForceHumanFirst)
            {
                testingModeForceHumanFirst = false;
            }
            mycovariantDraftController.StartDraft(
                Board.Players,
                persistentPoolManager,
                order,
                rng,
                MycovariantGameBalance.MycovariantSelectionDraftSize);
            if (testingModeEnabled)
            {
                var tMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId);
                var name = tMyco?.Name ?? "Unknown";
                gameUIManager.PhaseBanner.Show($"Testing: {name}",2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart(name);
            }
            else
            {
                gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase!",2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart();
            }
            phaseProgressTracker?.HighlightDraftPhase();
            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            mycovariantDraftController.gameObject.SetActive(true);
        }

        public void OnMycovariantDraftComplete()
        {
            isInDraftPhase = false;
            lastCompletedMycovariantDraftRound = Board?.CurrentRound ?? -1;
            RefreshRightSidebarTopStats();
            TooltipManager.Instance?.CancelAll();
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            mycovariantDraftController.gameObject.SetActive(false);
            if (testingModeEnabled)
            {
                StartNextRound();
            }
            else
            {
                StartCoroutine(DelayedStartNextRound());
            }
        }

        private IEnumerator DelayedStartNextRound()
        {
            yield return new WaitForSeconds(1f);
            StartNextRound();
        }

        private IEnumerator DelayedStartDraft()
        {
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(fastForwardService.WaitForFadeInAnimationsToComplete(gridVisualizer));
            StartMycovariantDraftPhase();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked);
            var pm = player.PlayerMycovariants.LastOrDefault(x => x.MycovariantId == picked.Id);
            if (pm != null && picked.AutoMarkTriggered)
            {
                picked.ApplyEffect?.Invoke(pm, Board, rng, gameUIManager.GameLogRouter);
            }
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);
        }

        public void MarkMycovariantDraftCompleteForRound(int round)
        {
            lastCompletedMycovariantDraftRound = round;
        }

        private void RefreshRightSidebarTopStats()
        {
            if (Board == null || gameUIManager?.RightSidebar == null)
            {
                return;
            }

            int round = Board.CurrentRound;
            float occ = Board.GetOccupiedTileRatio() * 100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
            gameUIManager.RightSidebar.UpdateRandomDecayChance(round);
        }

        #endregion

        #region UI Helpers

        public void ShowStartGamePanel()
        {
            ForceClosePauseMenu();

            if (gameUIManager != null)
            {
                gameUIManager.LoadingScreen?.gameObject.SetActive(false);
                gameUIManager.LeftSidebar?.gameObject.SetActive(false);
                gameUIManager.RightSidebar?.gameObject.SetActive(false);
                gameUIManager.MutationUIManager?.gameObject.SetActive(false);
                gameUIManager.EndGamePanel?.gameObject.SetActive(false);
                gameUIManager.PauseMenuPanel?.SetGameplayVisibility(false);

                // Clear log data so stale entries don't persist into the next game
                gameUIManager.GlobalGameLogManager?.ClearLog();
                gameUIManager.GameLogManager?.ClearLog();
            }
            // Prefer showing mode select panel if present; fallback to legacy start panel
            if (modeSelectPanel != null)
            {
                modeSelectPanel.SetActive(true);
                if (startGamePanel != null)
                {
                    startGamePanel.gameObject.SetActive(false);
                }
            }
            else if (startGamePanel != null)
            {
                startGamePanel.gameObject.SetActive(true);
            }
        }

        public void ReturnToMainMenu()
        {
            ForceClosePauseMenu();
            StopAllCoroutines();
            TooltipManager.Instance?.CancelAll();

            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;
            isInDraftPhase = false;
            isFastForwarding = false;
            _fastForwardStarted = false;
            initialMutationPointsAssigned = false;
            endgameService?.Reset();

            FirstUpgradeRounds?.Clear();
            players.Clear();
            humanPlayers.Clear();
            humanPlayer = null;
            Board = null;

            gridVisualizer?.ClearAllHighlights();
            gridVisualizer?.ClearHoverEffect();

            ShowStartGamePanel();
        }

        public void QuitGame()
        {
            ForceClosePauseMenu();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowSelectionPrompt(string message)
        {
            SelectionPromptPanel.SetActive(true);
            SelectionPromptText.text = message;
        }

        public void HideSelectionPrompt() => SelectionPromptPanel.SetActive(false);

        public void SetActiveHumanPlayer(Player player)
        {
            if (player == null)
            {
                return;
            }
            var logManager = gameUIManager?.GameLogManager;
            var playerLogPanel = gameUIManager?.GameLogPanel;

            // 1. Update GameLogManager active player first so GetRecentEntries works during panel rebuild
            logManager?.SetActiveHumanPlayer(player.PlayerId, Board);
            // 1a. Emit any pending summaries (e.g., growth/decay/draft) for this player now that they are active (before rebuild)
            logManager?.EmitPendingSegmentSummariesFor(player.PlayerId);

            // 2. Enable filtering (this may trigger a rebuild) AFTER active id is set
            if (humanPlayers.Count > 1)
            {
                playerLogPanel?.EnablePlayerSpecificFiltering();
            }

            // 3. Set panel active player (header + rebuild if filtering)
            playerLogPanel?.SetActivePlayer(player.PlayerId, player.PlayerName);

            // 4. Update primary human reference
            humanPlayer = player;
            gameUIManager?.RightSidebar?.SetPerspectivePlayer(player);
        }

        #endregion

        #region Testing Mode API / Fast Forward

        public void EnableTestingMode(
            int? mycovariantId,
            int fastForwardRounds =0,
            bool skipToEndgameAfterFastForward = false,
            ForcedGameResultMode forcedGameResult = ForcedGameResultMode.Natural,
            string forcedAdaptationId = "")
        {
            testingModeEnabled = true;
            testingMycovariantId = mycovariantId;
            testingForcedAdaptationId = forcedAdaptationId ?? string.Empty;
            testingModeForceHumanFirst = mycovariantId.HasValue;
            testingForcedGameResult = forcedGameResult;
            this.fastForwardRounds = fastForwardRounds;
            testingSkipToEndgameAfterFastForward = skipToEndgameAfterFastForward;
        }

        public void DisableTestingMode()
        {
            testingModeEnabled = false;
            testingMycovariantId = null;
            testingForcedAdaptationId = string.Empty;
            testingModeForceHumanFirst = false;
            testingForcedGameResult = ForcedGameResultMode.Natural;
            fastForwardRounds =0;
            testingSkipToEndgameAfterFastForward = false;
        }

        internal void RequestFastForwardIfNeeded()
        {
            if (testingModeEnabled && fastForwardRounds >0 && !_fastForwardStarted)
            {
                _fastForwardStarted = true;
                fastForwardService.StartFastForward(fastForwardRounds, testingSkipToEndgameAfterFastForward, testingMycovariantId);
            }
        }

        #endregion

        #region Intro Coroutine

        private IEnumerator PlayStartingSporeIntroAndContinue()
        {
            var startingIds = Board.Players
                .Select(p => p.StartingTileId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            gameUIManager.LoadingScreen?.SetStatus("Spores are landing…");
            if (startingIds.Count >0 && !testingModeEnabled)
            {
                yield return gridVisualizer.PlayStartingSporeArrivalAnimation(startingIds);
            }
            // Only fade out loading screen if we are NOT about to fast-forward;
            // fast-forward will take over the loading screen with progress text.
            bool willFastForward = testingModeEnabled && fastForwardRounds > 0 && !_fastForwardStarted;
            if (!willFastForward)
                gameUIManager.LoadingScreen?.FadeOut();
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);
            cameraCenterer?.CaptureInitialFraming();
            InitGameLogs();
            gameUIManager.MoldProfileRoot?.Initialize(Board.Players[0], Board.Players);
            if (testingModeEnabled)
            {
                if (fastForwardRounds >0 && !_fastForwardStarted)
                {
                    _fastForwardStarted = true;
                    fastForwardService.StartFastForward(fastForwardRounds, testingSkipToEndgameAfterFastForward, testingMycovariantId);
                }
                else if (testingSkipToEndgameAfterFastForward)
                {
                    // Allow campaign menu testing to force immediate endgame even when fast-forward rounds are 0.
                    TriggerEndGameInternal();
                    yield break;
                }
                else if (testingMycovariantId.HasValue)
                {
                    StartMycovariantDraftPhase();
                    yield break;
                }
                else
                {
                    gameUIManager.PhaseBanner.Show("New Game Settings",2f);
                }
            }
            else
            {
                gameUIManager.PhaseBanner.Show("New Game Settings",2f);
            }
            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.SetPerspectivePlayer(humanPlayer);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
            if (!(testingModeEnabled && (fastForwardRounds >0 || testingMycovariantId.HasValue)))
            {
                Debug.Log("[GameManager] Starting initial round mutation phase via StartNextRound()");
                StartNextRound();
            }
        }

        #endregion

        #region Internal Accessors For Services

        internal MutationManager GetPrivateMutationManager() => mutationManager;
        internal Player GetPrimaryHumanInternal() => humanPlayer;
        internal System.Random GetRngInternal() => rng;
        internal MycovariantPoolManager GetPersistentPoolInternal() => persistentPoolManager;
        public bool IsPauseMenuOpen => isPauseMenuOpen;

        internal void TriggerEndGameInternal()
        {
            endgameService.EndGame();
            gameEnded = endgameService.GameEnded;
        }

        internal void ArmImmediateFinalRoundAfterFastForwardIfNeeded()
        {
            endgameService.TryArmImmediateFinalRoundCountdown();
            gameEnded = endgameService.GameEnded;
        }

        private void RecoverGameplayUiIfStuck()
        {
            if (Board == null || gameUIManager == null)
            {
                return;
            }

            bool inMenu = (modeSelectPanel != null && modeSelectPanel.activeInHierarchy)
                          || (startGamePanel != null && startGamePanel.gameObject.activeInHierarchy);
            if (inMenu)
            {
                return;
            }

            bool draftVisible = mycovariantDraftController != null && mycovariantDraftController.gameObject.activeInHierarchy;
            bool endgameVisible = gameUIManager.EndGamePanel != null && gameUIManager.EndGamePanel.gameObject.activeInHierarchy;
            if (draftVisible || endgameVisible)
            {
                return;
            }

            bool leftVisible = gameUIManager.LeftSidebar != null && gameUIManager.LeftSidebar.gameObject.activeInHierarchy;
            bool rightVisible = gameUIManager.RightSidebar != null && gameUIManager.RightSidebar.gameObject.activeInHierarchy;
            bool mutationVisible = gameUIManager.MutationUIManager != null && gameUIManager.MutationUIManager.gameObject.activeInHierarchy;

            if (leftVisible || rightVisible || mutationVisible)
            {
                return;
            }

            Debug.LogWarning("[GameManager] UI recovery triggered: all gameplay UI roots were hidden unexpectedly.");
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            if (gameUIManager.MutationUIManager != null)
            {
                gameUIManager.MutationUIManager.gameObject.SetActive(true);
                gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
                gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();
                if (humanPlayer != null)
                {
                    gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(humanPlayer.MutationPoints > 0);
                }
            }
        }

        private void BootstrapPauseMenu()
        {
            pauseMenuPanel = GetComponent<UI_PauseMenuPanel>();
            if (pauseMenuPanel == null)
            {
                pauseMenuPanel = gameObject.AddComponent<UI_PauseMenuPanel>();
            }

            pauseMenuPanel.SetDependencies(
                gameUIManager,
                OpenPauseMenu,
                ResumeGameplay,
                ReturnToMainMenu,
                QuitGame);

            gameUIManager?.RegisterPauseMenuPanel(pauseMenuPanel);
            pauseMenuPanel.SetGameplayVisibility(false);
        }

        private void HandlePauseMenuInput()
        {
            if (!Application.isPlaying || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (isPauseMenuOpen)
            {
                if (pauseMenuPanel != null && pauseMenuPanel.IsConfirming)
                {
                    pauseMenuPanel.CancelPendingAction();
                    return;
                }

                ResumeGameplay();
                return;
            }

            if (TryCancelActiveSelection())
            {
                return;
            }

            if (CanOpenPauseMenu())
            {
                OpenPauseMenu();
            }
        }

        private bool CanOpenPauseMenu()
        {
            if (Board == null || gameEnded || isInDraftPhase)
            {
                return false;
            }

            bool inMenu = (modeSelectPanel != null && modeSelectPanel.activeInHierarchy)
                          || (startGamePanel != null && startGamePanel.gameObject.activeInHierarchy);
            if (inMenu)
            {
                return false;
            }

            if (mycovariantDraftController != null && mycovariantDraftController.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (gameUIManager?.EndGamePanel != null && gameUIManager.EndGamePanel.gameObject.activeInHierarchy)
            {
                return false;
            }

            return true;
        }

        private bool TryCancelActiveSelection()
        {
            if (MultiCellSelectionController.Instance != null && MultiCellSelectionController.Instance.HasActiveSelection)
            {
                MultiCellSelectionController.Instance.CancelSelection();
                return true;
            }

            if (MultiTileSelectionController.Instance != null && MultiTileSelectionController.Instance.HasActiveSelection)
            {
                MultiTileSelectionController.Instance.CancelSelection();
                return true;
            }

            if (TileSelectionController.Instance != null && TileSelectionController.Instance.HasActiveSelection)
            {
                TileSelectionController.Instance.CancelSelection();
                return true;
            }

            return false;
        }

        private void OpenPauseMenu()
        {
            if (!CanOpenPauseMenu())
            {
                return;
            }

            isPauseMenuOpen = true;
            Time.timeScale = 0f;
            pauseMenuPanel?.Show();
        }

        private void ResumeGameplay()
        {
            ForceClosePauseMenu();
        }

        private void ForceClosePauseMenu()
        {
            isPauseMenuOpen = false;
            Time.timeScale = 1f;
            pauseMenuPanel?.Hide();
        }

        #endregion
    }
}
