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
using FungusToast.Core.Persistence;
using FungusToast.Unity.Cameras;
using FungusToast.Unity.Events;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Phases;
using FungusToast.Unity.Save;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.GameStart;
using FungusToast.Unity.UI.MycovariantDraft;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Hotseat;
using FungusToast.Unity.UI.GameLog; // added for EnablePlayerSpecificFiltering
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using FungusToast.Unity.Campaign; // NEW: campaign namespace
using FungusToast.Unity.Endgame;

#nullable enable

namespace FungusToast.Unity
{
    public static class AlphaDataResetService
    {
        private const string AppliedResetTokenKey = "System.AlphaDataResetToken";
        private const string CurrentResetToken = "alpha-reset-2026-04-05";

        public static void ApplyIfNeeded()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var appliedToken = PlayerPrefs.GetString(AppliedResetTokenKey, string.Empty);
            if (string.Equals(appliedToken, CurrentResetToken, StringComparison.Ordinal))
            {
                return;
            }

            Debug.Log($"[AlphaDataReset] Applying one-time alpha data reset token '{CurrentResetToken}'.");

            try
            {
                ClearPersistentDataDirectory();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AlphaDataReset] Failed to fully clear persistent data directory: {exception.Message}\n{exception}");
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString(AppliedResetTokenKey, CurrentResetToken);
            PlayerPrefs.Save();

            Debug.Log("[AlphaDataReset] Cleared PlayerPrefs and persistent data for alpha compatibility.");
        }

        private static void ClearPersistentDataDirectory()
        {
            var persistentDataPath = Application.persistentDataPath;
            if (string.IsNullOrWhiteSpace(persistentDataPath) || !Directory.Exists(persistentDataPath))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(persistentDataPath))
            {
                File.Delete(filePath);
            }

            foreach (var directoryPath in Directory.GetDirectories(persistentDataPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    public class GameManager : MonoBehaviour
    {
        private const string AlphaMutationOnboardingSeenKey = "Onboarding.AlphaMutationPhaseSeen";
        private const string AlphaMutationOnboardingBannerText = "Goal: control the largest share of the toast.\nSpend mutation points for upgrades now or store them to save for stronger upgrades later.\nAfter that, your colony grows automatically.";
        private const string TropicLysisDisplayName = "Tropic Lysis";

        #region Inspector Fields

        [Header("Board Settings")] 
        public int boardWidth =160; 
        public int boardHeight =160; 
        public int playerCount =2;

        [Header("Testing Mode")] 
        public bool testingModeEnabled = false; 
        public int? testingMycovariantId = null; 
        public int testingCampaignLevelIndex = 0;
        public string testingForcedAdaptationId = string.Empty;
        public List<string> testingForcedStartingAdaptationIds = new();
        public bool testingModeForceHumanFirst = true; 
        public ForcedGameResultMode testingForcedGameResult = ForcedGameResultMode.Natural;
        public bool testingForceMoldinessRewards = false;
        public int fastForwardRounds =0; 
        public bool testingSkipToEndgameAfterFastForward = false;
        public bool testingTreatAsFirstGame = false;

        [Header("References")] 
        public GridVisualizer gridVisualizer = null!; 
        public CameraCenterer cameraCenterer = null!; 
        [SerializeField] private MutationManager mutationManager = null!; 
        [SerializeField] private GrowthPhaseRunner growthPhaseRunner = null!; 
        [SerializeField] private GameUIManager gameUIManager = null!; 
        [SerializeField] private DecayPhaseRunner decayPhaseRunner = null!; 
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker = null!; 
        [SerializeField] private MycovariantDraftController mycovariantDraftController = null!; 
        [SerializeField] private UI_StartGamePanel startGamePanel = null!; 
        [SerializeField] private UI_HotseatTurnPrompt hotseatTurnPrompt = null!; 
        public GameObject SelectionPromptPanel = null!; 
        public TextMeshProUGUI SelectionPromptText = null!; 
        [SerializeField] private Button selectionPromptCancelButton = null!;
        [SerializeField] private TextMeshProUGUI selectionPromptCancelButtonText = null!;
        [SerializeField] private GameObject modeSelectPanel = null!; // NEW: root of mode select UI
        [SerializeField] private AudioClip? mutationPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float mutationPhaseStartVolume = 1f;
        [SerializeField] private AudioClip? growthPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float growthPhaseStartVolume = 1f;
        [SerializeField] private AudioClip? decayPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float decayPhaseStartVolume = 1f;
        [SerializeField] private AudioClip? draftPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float draftPhaseStartVolume = 1f;
        [SerializeField] private AudioClip? startingSporeDropClip = null;
        [SerializeField, Range(0f, 1f)] private float startingSporeDropVolume = 1f;
        [SerializeField] private AudioClip? jettingMyceliumLaunchClip = null;
        [SerializeField, Range(0f, 1f)] private float jettingMyceliumLaunchVolume = 1f;
        [SerializeField] private AudioClip? jettingMyceliumSprayLoopClip = null;
        [SerializeField, Range(0f, 1f)] private float jettingMyceliumSprayLoopVolume = 0.85f;
        [SerializeField] private AudioClip? jettingMyceliumReleaseClip = null;
        [SerializeField, Range(0f, 1f)] private float jettingMyceliumReleaseVolume = 0.9f;
        [SerializeField, Min(0f)] private float jettingMyceliumLoopFadeOutSeconds = 0.05f;
        [SerializeField] private AudioClip? gameplayMusicClip = null;
        [SerializeField] private AudioClip[] additionalGameplayMusicClips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 1f;
        [SerializeField, Min(0f)] private float gameplayMusicInitialDelaySeconds = 10f;
        [SerializeField, Min(0f)] private float gameplayMusicReplayDelaySeconds = 5f;
        [SerializeField, Min(0f)] private float gameplayMusicFadeInSeconds = 1f;
        [SerializeField] private AudioMixerGroup? gameplayMusicMixerGroup = null;
        [Header("Title Track")]
        [SerializeField] private AudioClip? titleTrackClip = null;
        [SerializeField, Range(0f, 1f)] private float titleTrackVolume = 1f;
        [SerializeField] private AudioMixerGroup? titleTrackMixerGroup = null;

        [Header("Hotseat Config")] 
        public int configuredHumanPlayerCount =1; 
        public int ConfiguredHumanPlayerCount => configuredHumanPlayerCount; 
        private readonly List<int> configuredHumanMoldIndices = new();
        public void SetHotseatConfig(int humanCount) => SetHotseatConfig(humanCount, null);
        public void SetHotseatConfig(int humanCount, IReadOnlyList<int>? humanMoldIndices)
        {
            configuredHumanPlayerCount = Mathf.Max(1, humanCount);
            configuredHumanMoldIndices.Clear();

            if (humanMoldIndices == null)
            {
                return;
            }

            for (int i = 0; i < humanMoldIndices.Count; i++)
            {
                configuredHumanMoldIndices.Add(humanMoldIndices[i]);
            }
        }

        [Header("Campaign Config")] 
        public CampaignProgression? campaignProgression; // assign ScriptableObject in inspector
        public CampaignProgression? CampaignProgression => campaignProgression;

        [Header("Magnifier")]
        [SerializeField] private MagnifyingGlassFollowMouse? magnifyingGlass; // new serialized reference

        // NEW: current mode
        public GameMode CurrentGameMode { get; private set; } = GameMode.Hotseat;
        private CampaignController? campaignController; // lazy created

        #endregion

        #region State Fields / Services

        private bool gameEnded = false; 
        private System.Random rng = null!; 
        private MycovariantPoolManager persistentPoolManager = null!;

        public GameBoard Board { get; private set; } = null!;
        public GameUIManager GameUI => gameUIManager; 
        public SpecialEventPresentationService SpecialEventPresentationService => specialEventPresentationService;
        public CampaignController? CampaignController => campaignController;
        public static GameManager Instance { get; private set; } = null!;

        private readonly List<Player> players = new(); 
        private readonly List<Player> humanPlayers = new(); 
        private Player humanPlayer = null!; // primary

        private bool isInDraftPhase = false; 
        public bool IsDraftPhaseActive => isInDraftPhase; 
        private int lastCompletedMycovariantDraftRound = -1;
        private bool activeDraftCountsTowardRoundCompletion;
        private bool isMycovariantDraftChainActive;
        private bool humanDraftedMycovariantThisDraftChain;
        public int LastCompletedMycovariantDraftRound => lastCompletedMycovariantDraftRound;
        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        public bool IsTestingModeEnabled => testingModeEnabled; 
        public int? TestingMycovariantId => testingMycovariantId;
        public int TestingCampaignLevelIndex => testingCampaignLevelIndex;
        public string TestingForcedAdaptationId => testingForcedAdaptationId;
        public IReadOnlyList<string> TestingForcedStartingAdaptationIds => testingForcedStartingAdaptationIds;
        public ForcedGameResultMode TestingForcedGameResult => testingForcedGameResult;
        public bool TestingForceMoldinessRewards => testingForceMoldinessRewards;
        public bool ShouldForceFirstGameExperience => testingModeEnabled && testingTreatAsFirstGame;

        private bool isFastForwarding = false; 
        public bool IsFastForwarding => isFastForwarding; 
        private bool _fastForwardStarted = false;
        private bool initialMutationPointsAssigned = false;
        private bool skipMutationPointAssignmentForRoundStart;
        private bool pendingAlphaMutationOnboarding;
        private float nextUiStuckCheckTime;
        private bool hasApplicationFocus = true;
        private int currentLevelGameplaySeed;
        private int? pendingGameplaySeed;

        // Services
        private PlayerInitializer playerInitializer = null!; 
        private HotseatTurnManager hotseatTurnManager = null!; 
        private FastForwardService fastForwardService = null!; 
        private PostGrowthVisualSequence postGrowthVisualSequence = null!;
        private EndgameService endgameService = null!;
        private EndgamePlayerStatisticsTracker endgamePlayerStatisticsTracker = null!;
        private MutationPointService mutationPointService = null!;
        private SpecialEventPresentationService specialEventPresentationService = null!;
        private BackgroundMusicService backgroundMusicService = null!;
        private SoundEffectService soundEffectService = null!;
        private PauseMenuService pauseMenuService = null!;
        private SelectionPromptService selectionPromptService = null!;
        private GameTransitionService gameTransitionService = null!;
        private GameStartService gameStartService = null!;
        private PlayerPerspectiveService playerPerspectiveService = null!;
        private PlayerMoldAssignmentService playerMoldAssignmentService = null!;

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
            AlphaDataResetService.ApplyIfNeeded();
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
                pauseMenuService?.ForceClose();
                gameTransitionService?.ShowStartGamePanel();
                // Start the title/menu music when in start/menu screens
                backgroundMusicService?.StartTitleMusic();
            }
        }

        private void Update()
        {
            pauseMenuService?.HandleInput();

            // Safety net: recover from rare transition bugs where all gameplay UI roots end up hidden.
            if (Time.unscaledTime < nextUiStuckCheckTime)
            {
                return;
            }

            nextUiStuckCheckTime = Time.unscaledTime + 0.5f;
            RecoverGameplayUiIfStuck();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            hasApplicationFocus = hasFocus;

            if (!Application.isPlaying)
            {
                return;
            }

            if (!hasFocus)
            {
                backgroundMusicService?.Pause();
                return;
            }

            backgroundMusicService?.Resume();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnApplicationFocus(!pauseStatus);
        }

        public void PlayJettingMyceliumVolleySound()
        {
            soundEffectService?.PlayOneShot(jettingMyceliumLaunchClip, jettingMyceliumLaunchVolume);
            soundEffectService?.PlayLoop(jettingMyceliumSprayLoopClip, jettingMyceliumSprayLoopVolume);
        }

        public void StopJettingMyceliumVolleySound()
        {
            soundEffectService?.StopLoop(jettingMyceliumLoopFadeOutSeconds);
            soundEffectService?.PlayOneShot(jettingMyceliumReleaseClip, jettingMyceliumReleaseVolume);
        }

        #endregion

        #region Bootstrap

        private void BootstrapServices()
        {
            soundEffectService = new SoundEffectService(gameObject);
            pauseMenuService = new PauseMenuService(
                gameObject,
                gameUIManager,
                CanOpenPauseMenu,
                TryCancelActiveSelection,
                () => gameUIManager?.MutationUIManager?.ForceCloseTreePanel(),
                ReturnToMainMenu,
                RestartCurrentLevel,
                QuitGame,
                SkipToNextTrack,
                GetCurrentGameplayTrackName,
                GetNextGameplayTrackName);
            pauseMenuService.Initialize();
            selectionPromptService = new SelectionPromptService(
                SelectionPromptPanel,
                SelectionPromptText,
                selectionPromptCancelButton,
                selectionPromptCancelButtonText);
            gameTransitionService = new GameTransitionService(
                gameUIManager,
                modeSelectPanel,
                startGamePanel,
                () => Board,
                () => backgroundMusicService?.StopGameplayMusic(),
                UnsubscribeFromPlayerMutationEvents,
                currentBoard =>
                {
                    GameRulesEventSubscriber.UnsubscribeAll(currentBoard);
                    GameUIEventSubscriber.Unsubscribe(currentBoard, gameUIManager);
                    endgamePlayerStatisticsTracker?.Detach();
                    var observer = gameUIManager?.GameLogRouter;
                    if (observer != null)
                    {
                        AnalyticsEventSubscriber.Unsubscribe(currentBoard, observer);
                    }
                },
                StopAllCoroutines,
                mycovariantDraftController,
                growthPhaseRunner,
                decayPhaseRunner,
                gridVisualizer,
                specialEventPresentationService,
                postGrowthVisualSequence,
                pauseMenuService,
                SaveCurrentRunForResume,
                ResetManagerStateForMainMenuReturn);
            gameStartService = new GameStartService(
                () => campaignController,
                gridVisualizer,
                gameUIManager,
                modeSelectPanel,
                startGamePanel,
                mycovariantDraftController,
                mode => CurrentGameMode = mode,
                () => CurrentGameMode,
                () => rng,
                () => testingModeEnabled,
                () => testingForcedAdaptationId,
                () => playerCount,
                () => currentLevelGameplaySeed,
                (width, height) =>
                {
                    boardWidth = width;
                    boardHeight = height;
                },
                (humanCount, humanMoldIndices) => SetHotseatConfig(humanCount, humanMoldIndices),
                playerCount => playerMoldAssignmentService?.ApplyConfiguredPlayerMoldAssignments(playerCount),
                seed => pendingGameplaySeed = seed,
                InitializeGame,
                InitializeRestoredGame,
                StopAllCoroutines);
            playerPerspectiveService = new PlayerPerspectiveService(gameUIManager);
            playerMoldAssignmentService = new PlayerMoldAssignmentService(
                gridVisualizer,
                () => configuredHumanPlayerCount,
                () => configuredHumanMoldIndices);
            backgroundMusicService = new BackgroundMusicService(this, transform, () => !hasApplicationFocus);
            // Configure both gameplay and title music
            backgroundMusicService.ConfigureTitleTrack(titleTrackClip, titleTrackVolume, titleTrackMixerGroup);
            ConfigureBackgroundMusicService();

            playerInitializer = new PlayerInitializer(
                gridVisualizer,
                gameUIManager,
                () => configuredHumanPlayerCount,
                () => CurrentGameMode,
                () => campaignController?.CurrentBoardPreset,
                () => campaignController?.CurrentResolvedAiStrategyNames,
                () => configuredHumanMoldIndices,
                () => testingForcedStartingAdaptationIds);
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
            endgamePlayerStatisticsTracker = new EndgamePlayerStatisticsTracker();
            gameUIManager.GameLogRouter.SetEndgamePlayerStatisticsTracker(endgamePlayerStatisticsTracker);
            endgameService = new EndgameService(
                gameUIManager,
                () => Board,
                () => humanPlayer,
                () => CurrentGameMode,
                () => campaignController,
                () => campaignProgression,
                () => endgamePlayerStatisticsTracker.CreateSnapshot(Board?.Players ?? players),
                () => FirstUpgradeRounds,
                () => testingModeEnabled,
                () => testingForcedGameResult,
                () => testingForceMoldinessRewards);
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
            gameStartService?.StartHotseatGame(numberOfPlayers);
        }

        public void StartCampaignNew(int humanMoldIndex = 0)
        {
            gameStartService?.StartCampaignNew(humanMoldIndex);
        }

        public void StartCampaignResume()
        {
            gameStartService?.StartCampaignResume();
        }

        public void StartHotseatResume()
        {
            gameStartService?.StartHotseatResume();
        }

        public void RestartCurrentLevel()
        {
            gameStartService?.RestartCurrentLevel();
        }

        public void ShowPendingCampaignMoldinessRewardFromMainMenu()
        {
            gameStartService?.ShowPendingCampaignMoldinessRewardFromMainMenu();
        }

        // Accessor for external panels
        public bool HasCampaignSave() => gameStartService != null && gameStartService.HasCampaignSave();
        public bool HasSoloSave() => gameStartService != null && gameStartService.HasSoloSave();
        public bool HasResumableCampaignSave() => gameStartService != null && gameStartService.HasResumableCampaignSave();
        public bool IsCampaignAwaitingAdaptationSelection() =>
            gameStartService != null && gameStartService.IsCampaignAwaitingAdaptationSelection();
        public bool HasPendingCampaignMoldinessUnlockOnSavedRun()
        {
            return gameStartService != null && gameStartService.HasPendingCampaignMoldinessUnlockOnSavedRun();
        }

        public bool ResetCampaignMoldinessProgression()
        {
            return campaignController != null && campaignController.ResetMoldinessProgression();
        }

        public bool TryStartCampaignAdaptationDraft(Action onSelectionComplete)
        {
            return gameStartService != null && gameStartService.TryStartCampaignAdaptationDraft(onSelectionComplete);
        }

        public void InitializeGame(int numberOfPlayers)
        {
            if (!PrepareGameplayInitialization(numberOfPlayers, "Preparing the toast…"))
            {
                return;
            }

            var ui = gameUIManager;

            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            ui.SetBoard(Board); // expose board to UI components via façade
            rng = new System.Random(ResolveGameplaySeedForNewSession());
            postGrowthVisualSequence.Register(Board); // board post-growth events

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, ui.GameLogRouter);
            GameUIEventSubscriber.Subscribe(Board, ui, specialEventPresentationService);
            AnalyticsEventSubscriber.Subscribe(Board, ui.GameLogRouter);

            playerMoldAssignmentService?.ApplyConfiguredPlayerMoldAssignments(playerCount);

            playerInitializer.InitializePlayers(Board, players, humanPlayers, out humanPlayer, playerCount);
            ApplyCampaignAdaptations();
            SubscribeToPlayerMutationEvents();

            persistentPoolManager = new MycovariantPoolManager();
            persistentPoolManager.InitializePool(GetInitialMycovariantPool(), rng);

            mutationManager.ResetMutationPoints(players);
            initialMutationPointsAssigned = true;

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            endgamePlayerStatisticsTracker.Attach(Board);
            gridVisualizer.RenderBoard(Board);
            playerPerspectiveService?.InitializeGameplayPerspective(humanPlayer, Board, players, gridVisualizer);

            // Ensure any title/menu music is stopped when starting gameplay music.
            backgroundMusicService?.StopTitleMusic();
            backgroundMusicService?.StartGameplayMusic();
            StartCoroutine(PlayStartingSporeIntroAndContinue());

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            pauseMenuService?.SetGameplayVisibility(true);
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

        public void InitializeRestoredGame(RoundStartRuntimeSnapshot snapshot, RandomStateSnapshot? randomState, int gameplaySeed)
        {
            if (snapshot == null)
            {
                Debug.LogError("[GameManager] Cannot restore game: snapshot is missing.");
                return;
            }

            if (!PrepareGameplayInitialization(snapshot.Players?.Count ?? 0, "Reloading autosave…"))
            {
                return;
            }

            currentLevelGameplaySeed = gameplaySeed;
            pendingGameplaySeed = null;

            var restored = RoundStartRuntimeSnapshotFactory.Restore(
                snapshot,
                ResolveMutationStrategyFromSnapshot,
                MycovariantRepository.All);

            Board = restored.Board;
            persistentPoolManager = restored.MycovariantPoolManager ?? new MycovariantPoolManager();
            gameUIManager.SetBoard(Board);
            rng = randomState != null
                ? RandomStateSerialization.Restore(randomState)
                : new System.Random(gameplaySeed);
            postGrowthVisualSequence.Register(Board);

            players.Clear();
            players.AddRange(Board.Players.OrderBy(player => player.PlayerId));
            humanPlayers.Clear();
            humanPlayers.AddRange(players.Where(player => player.PlayerType == PlayerTypeEnum.Human).OrderBy(player => player.PlayerId));
            if (humanPlayers.Count == 0)
            {
                Debug.LogError("[GameManager] Cannot restore game: no human player was found in the snapshot.");
                gameUIManager.LoadingScreen?.FadeOut();
                return;
            }

            humanPlayer = humanPlayers[0];

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, gameUIManager.GameLogRouter);
            GameUIEventSubscriber.Subscribe(Board, gameUIManager, specialEventPresentationService);
            AnalyticsEventSubscriber.Subscribe(Board, gameUIManager.GameLogRouter);

            playerMoldAssignmentService?.ApplyConfiguredPlayerMoldAssignments(playerCount);
            AssignPlayerIconsToUiBinder(players);
            SubscribeToPlayerMutationEvents();

            initialMutationPointsAssigned = true;
            skipMutationPointAssignmentForRoundStart = true;

            gridVisualizer.Initialize(Board);
            endgamePlayerStatisticsTracker.Attach(Board);
            gridVisualizer.RenderBoard(Board, suppressAnimations: true);
            playerPerspectiveService?.InitializeGameplayPerspective(humanPlayer, Board, players, gridVisualizer);

            backgroundMusicService?.StopTitleMusic();
            backgroundMusicService?.StartGameplayMusic();

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            pauseMenuService?.SetGameplayVisibility(true);
            mycovariantDraftController?.gameObject.SetActive(false);
            cameraCenterer?.CaptureInitialFraming();
            InitGameLogs();

            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.SetPerspectivePlayer(humanPlayer);
            gameUIManager.RightSidebar?.SetRoundAndOccupancy(Board.CurrentRound, Board.GetOccupiedTileRatio() * 100f);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);

            if (cameraCenterer != null)
            {
                cameraCenterer.CenterCameraInstant();
                cameraCenterer.CaptureInitialFraming();
            }

            MagnifyingGlassFollowMouse.gameStarted = true;
            if (magnifyingGlass != null)
            {
                magnifyingGlass.ApplyBoardSizeGate(Board.Width, Board.Height);
            }

            StartCoroutine(PlayRestoredGameIntroAndContinue());
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

        private void AssignPlayerIconsToUiBinder(IEnumerable<Player> playersToBind)
        {
            if (gameUIManager?.PlayerUIBinder == null || gridVisualizer == null || playersToBind == null)
            {
                return;
            }

            gameUIManager.PlayerUIBinder.ClearIcons();

            foreach (var player in playersToBind)
            {
                var icon = gridVisualizer.GetTileForPlayer(player.PlayerId)?.sprite;
                if (icon != null)
                {
                    gameUIManager.PlayerUIBinder.AssignIcon(player, icon);
                }
            }
        }

        private void SubscribeToPlayerMutationEvents()
        {
            foreach (var p in Board.Players)
            {
                p.MutationsChanged += OnPlayerMutationsChanged;
            }
        }

        private void UnsubscribeFromPlayerMutationEvents()
        {
            foreach (var p in players)
            {
                p.MutationsChanged -= OnPlayerMutationsChanged;
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

        private List<Mycovariant> GetInitialMycovariantPool()
        {
            var allMycovariants = MycovariantRepository.All.ToList();
            if (CurrentGameMode != GameMode.Campaign || campaignController == null)
            {
                return allMycovariants;
            }

            return campaignController.GetEligibleMycovariantsForCampaignDraft(allMycovariants);
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
            var edgeOffsets = players
                .Select(player => player.MutationStrategy is ParameterizedSpendingStrategy parameterized ? parameterized.StartingSporeEdgeOffset : 0)
                .ToArray();
            StartingSporeUtility.PlaceStartingSpores(Board, players, rng, edgeOffsets: edgeOffsets);
            if (ShouldPlaceStartingNutrientPatches())
            {
                NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(
                    Board,
                    players,
                    rng,
                    gameUIManager.GameLogRouter,
                    GetAllowedCampaignNutrientPatchTypes());
            }

            int round = Board.CurrentRound;
            float occ = Board.GetOccupiedTileRatio() *100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
        }

        private bool ShouldPlaceStartingNutrientPatches()
        {
            if (CurrentGameMode != GameMode.Campaign)
            {
                return true;
            }

            return campaignController?.CurrentLevelSpec?.enableNutrientPatches ?? true;
        }

        private IReadOnlyCollection<NutrientPatchType>? GetAllowedCampaignNutrientPatchTypes()
        {
            if (CurrentGameMode != GameMode.Campaign)
            {
                return null;
            }

            return campaignController?.CurrentLevelSpec?.allowedNutrientPatchTypes;
        }

        public void StartGrowthPhase()
        {
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
            gameUIManager.GameLogRouter?.OnPhaseStart("Growth");
            gameUIManager.GameLogManager?.OnLogSegmentStart("GrowthPhase");
            growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gridVisualizer.ClearNewlyGrownFlagsForNextGrowthPhase();
            soundEffectService?.PlayOneShot(growthPhaseStartClip, growthPhaseStartVolume);
            gameUIManager.PhaseBanner.Show("Growth Phase Begins!",2f);
            phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
            StartCoroutine(BeginGrowthPhaseAfterPreGrowthEffects());
        }

        private IEnumerator BeginGrowthPhaseAfterPreGrowthEffects()
        {
            var chitinFortificationTileIds = new List<int>();
            var conduitProjections = new List<GameBoard.ConduitProjectionEventArgs>();

            void BufferPreGrowthResistanceAnimation(int playerId, GrowthSource source, IReadOnlyList<int> tileIds)
            {
                if (source != GrowthSource.ChitinFortification || tileIds == null || tileIds.Count == 0)
                {
                    return;
                }

                foreach (var tileId in tileIds)
                {
                    if (!chitinFortificationTileIds.Contains(tileId))
                    {
                        chitinFortificationTileIds.Add(tileId);
                    }
                }
            }

            void BufferConduitProjection(GameBoard.ConduitProjectionEventArgs e)
            {
                if (e == null || e.AffectedTileIds == null || e.AffectedTileIds.Count == 0)
                {
                    return;
                }

                conduitProjections.Add(e);
            }

            Board.ResistanceAppliedBatch += BufferPreGrowthResistanceAnimation;
            Board.ConduitProjection += BufferConduitProjection;
            try
            {
                Board.OnPreGrowthPhase();
            }
            finally
            {
                Board.ResistanceAppliedBatch -= BufferPreGrowthResistanceAnimation;
                Board.ConduitProjection -= BufferConduitProjection;
            }

            if (!isFastForwarding && (chitinFortificationTileIds.Count > 0 || conduitProjections.Count > 0))
            {
                if (chitinFortificationTileIds.Count > 0)
                {
                    gridVisualizer.DeferResistanceOverlayReveal(chitinFortificationTileIds);
                }

                if (conduitProjections.Count > 0)
                {
                    gridVisualizer.RegisterPreAnimationHiddenPreviewTiles(
                        conduitProjections
                            .SelectMany(projection => projection.AffectedTileIds)
                            .Where(tileId => tileId >= 0)
                            .Distinct());
                }

                gridVisualizer.RenderBoard(Board, suppressAnimations: true);

                if (chitinFortificationTileIds.Count > 0)
                {
                    gridVisualizer.PlayResistancePulseBatchScaled(chitinFortificationTileIds, 0.5f);
                    yield return gridVisualizer.WaitForAllAnimations();
                }

                for (int i = 0; i < conduitProjections.Count; i++)
                {
                    StartCoroutine(gridVisualizer.PlayConduitProjectionPresentation(conduitProjections[i]));

                    if (i < conduitProjections.Count - 1)
                    {
                        yield return new WaitForSeconds(UIEffectConstants.ConduitProjectionParallelStaggerSeconds);
                    }
                }

                yield return gridVisualizer.WaitForAllAnimations();
            }

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
            soundEffectService?.PlayOneShot(decayPhaseStartClip, decayPhaseStartVolume);
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
            Board.SynchronizeChemobeaconsWithSurges(Board.Players);
            CheckForEndgameCondition();
            if (endgameService.GameEnded)
            {
                return;
            }
            Board.IncrementRound();
            int round = Board.CurrentRound;
            float occ = Board.GetOccupiedTileRatio() *100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ);
            if (HasQueuedDraftPhaseForCurrentRound())
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

            var board = Board!;
            var ui = gameUIManager;
            if (ui == null)
            {
                Debug.LogError("[GameManager] Cannot start mutation phase: GameUIManager reference is missing.");
                return;
            }

            Player? activeHuman = null;
            if (humanPlayers.Count > 0)
            {
                activeHuman = humanPlayers[0];
                SetActiveHumanPlayer(activeHuman);
            }

            ui.GameLogRouter?.OnRoundStart(board.CurrentRound);
            ui.GameLogManager?.OnLogSegmentStart("MutationPhaseStart");

            bool shouldAssignMutationPoints = !skipMutationPointAssignmentForRoundStart
                && !(board.CurrentRound == 1 && initialMutationPointsAssigned);
            skipMutationPointAssignmentForRoundStart = false;

            if (shouldAssignMutationPoints)
            {
                AssignMutationPoints();
            }
            else
            {
                board.OnMutationPhaseStart();
            }

            PersistRoundStartAutosave();

            if (activeHuman != null)
            {
                ui.GameLogManager?.EmitPendingSegmentSummariesFor(activeHuman.PlayerId);
            }

            pendingAlphaMutationOnboarding = ShouldQueueAlphaMutationOnboarding();

            hotseatTurnManager.BeginHumanMutationPhase();

            // Fail-safe: campaign continuation can traverse custom UI steps; ensure mutation controls are re-armed.
            if (activeHuman != null)
            {
                ui.MutationUIManager?.SetSpendPointsButtonVisible(true);
                ui.MutationUIManager?.RefreshSpendPointsButtonUI();
                ui.MutationUIManager?.SetSpendPointsButtonInteractable(activeHuman.MutationPoints > 0);
            }

            ui.RightSidebar?.UpdatePlayerSummaries(board.Players);
            if (humanPlayers.Count > 0)
            {
                ui.RightSidebar?.TryShowScoreboardWinConditionCoachmark(board.CurrentRound);
            }
            ui.RightSidebar?.UpdateRandomDecayChance(board.CurrentRound);
            ui.GameLogRouter?.OnPhaseStart("Mutation");
            if (!pendingAlphaMutationOnboarding)
            {
                soundEffectService?.PlayOneShot(mutationPhaseStartClip, mutationPhaseStartVolume);
                gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f);
            }
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
            bool wasGameEnded = gameEnded;
            endgameService.CheckForEndgameCondition();
            // Sync local flag for backward compatibility with existing checks
            gameEnded = endgameService.GameEnded;
            if (!wasGameEnded && gameEnded)
            {
                ClearInProgressSaveAfterGameEnded();
            }
        }

        private void EndGame()
        {
            bool wasGameEnded = gameEnded;
            endgameService.EndGame();
            gameEnded = endgameService.GameEnded;
            if (!wasGameEnded && gameEnded)
            {
                ClearInProgressSaveAfterGameEnded();
            }
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

        public void TryShowPendingAlphaMutationOnboarding(Player player)
        {
            if (!pendingAlphaMutationOnboarding || player == null || player.MutationPoints <= 0)
            {
                return;
            }

            pendingAlphaMutationOnboarding = false;
            gameUIManager.PhaseBanner.Show(AlphaMutationOnboardingBannerText, 5.5f);
            if (!ShouldForceFirstGameExperience)
            {
                PlayerPrefs.SetInt(AlphaMutationOnboardingSeenKey, 1);
                PlayerPrefs.Save();
            }
        }

        private bool ShouldQueueAlphaMutationOnboarding()
        {
            if (ShouldForceFirstGameExperience)
            {
                return Board != null
                    && Board.CurrentRound == 1
                    && humanPlayers.Count > 0
                    && !isFastForwarding;
            }

            return Board != null
                && Board.CurrentRound == 1
                && humanPlayers.Count > 0
                && !isFastForwarding
                && !testingModeEnabled
                && PlayerPrefs.GetInt(AlphaMutationOnboardingSeenKey, 0) == 0;
        }

        #endregion

        #region Draft Phase

        private void UpdatePhaseProgressTrackerLabel()
        {
            phaseProgressTracker?.SetMutationPhaseLabel(isInDraftPhase ? "DRAFT" : "MUTATION");
        }

        private bool HasQueuedDraftPhaseForCurrentRound()
        {
            if (Board == null)
            {
                return false;
            }

            return Board.HasPendingHypervariationDrafts
                || (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound)
                    && lastCompletedMycovariantDraftRound != Board.CurrentRound);
        }

        private bool TryStartQueuedDraftPhaseForCurrentRound()
        {
            if (Board == null)
            {
                return false;
            }

            while (Board.TryDequeuePendingHypervariationDraftPlayerId(out int playerId))
            {
                Player? draftPlayer = Board.Players.FirstOrDefault(player => player.PlayerId == playerId);
                if (draftPlayer == null)
                {
                    continue;
                }

                StartMycovariantDraftPhase(
                    customDraftOrder: new List<Player> { draftPlayer },
                    phaseBannerMessage: "Hypervariation Draft!",
                    draftTitle: "Hypervariation Draft",
                    draftBlurb: "Hypervariation has destabilized this colony. Draft one mycovariant for the player who claimed the patch.",
                    draftStartMessage: $"Hypervariation draft triggered. Only {draftPlayer.PlayerName} drafts this round.",
                    humanTurnBannerText: "Your Hypervariation draft awaits!",
                    aiTurnBannerPrefix: "Hypervariation Drafting",
                    countsTowardRoundCompletion: false);
                return true;
            }

            if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound)
                && lastCompletedMycovariantDraftRound != Board.CurrentRound)
            {
                StartMycovariantDraftPhase();
                return true;
            }

            return false;
        }

        public void StartMycovariantDraftPhase()
        {
            StartMycovariantDraftPhase(null, null, null, null, null, null, null, true);
        }

        private void StartMycovariantDraftPhase(
            List<Player>? customDraftOrder,
            string? phaseBannerMessage,
            string? draftTitle,
            string? draftBlurb,
            string? draftStartMessage,
            string? humanTurnBannerText,
            string? aiTurnBannerPrefix,
            bool countsTowardRoundCompletion)
        {
            if (!isMycovariantDraftChainActive)
            {
                isMycovariantDraftChainActive = true;
                humanDraftedMycovariantThisDraftChain = false;
            }

            isInDraftPhase = true;
            activeDraftCountsTowardRoundCompletion = countsTowardRoundCompletion;
            RefreshRightSidebarTopStats();
            TooltipManager.Instance?.CancelAll();
            // Mark draft phase segment boundary so prior aggregation (e.g., decay phase) is queued
            gameUIManager.GameLogManager?.OnLogSegmentStart("DraftPhase");
            var order = customDraftOrder ?? (testingModeEnabled && testingModeForceHumanFirst
                ? Board.Players
                    .OrderBy(p => p.PlayerType == PlayerTypeEnum.Human ?0 :1)
                    .ThenBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ToList()
                : MycovariantDraftManager.BuildDraftOrder(Board.Players, Board));
            if (customDraftOrder == null && testingModeEnabled && testingModeForceHumanFirst)
            {
                testingModeForceHumanFirst = false;
            }
            mycovariantDraftController.StartDraft(
                Board.Players,
                persistentPoolManager,
                order,
                rng,
                MycovariantGameBalance.MycovariantSelectionDraftSize,
                draftTitle,
                draftBlurb,
                draftStartMessage,
                humanTurnBannerText,
                aiTurnBannerPrefix);
            if (countsTowardRoundCompletion && testingModeEnabled)
            {
                var tMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId);
                var name = tMyco?.Name ?? "Unknown";
                soundEffectService?.PlayOneShot(draftPhaseStartClip, draftPhaseStartVolume);
                gameUIManager.PhaseBanner.Show($"Testing: {name}",2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart(name);
            }
            else
            {
                soundEffectService?.PlayOneShot(draftPhaseStartClip, draftPhaseStartVolume);
                gameUIManager.PhaseBanner.Show(phaseBannerMessage ?? "Mycovariant Draft Phase!",2f);
                if (countsTowardRoundCompletion)
                {
                    gameUIManager.GameLogRouter?.OnDraftPhaseStart();
                }
            }
            phaseProgressTracker?.HighlightDraftPhase();
            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            mycovariantDraftController.gameObject.SetActive(true);
        }

        public void OnMycovariantDraftComplete()
        {
            isInDraftPhase = false;
            if (activeDraftCountsTowardRoundCompletion)
            {
                lastCompletedMycovariantDraftRound = Board?.CurrentRound ?? -1;
            }
            activeDraftCountsTowardRoundCompletion = false;
            RefreshRightSidebarTopStats();
            TooltipManager.Instance?.CancelAll();
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            mycovariantDraftController.gameObject.SetActive(false);
            if (TryStartQueuedDraftPhaseForCurrentRound())
            {
                return;
            }

            gameUIManager.GameLogRouter?.DisableSilentMode();
            isMycovariantDraftChainActive = false;
            bool shouldResolveTropicLysis = ShouldResolveTropicLysisAfterDraftChain();
            humanDraftedMycovariantThisDraftChain = false;

            if (shouldResolveTropicLysis)
            {
                StartCoroutine(ResolvePostDraftEffectsThenAdvance());
                return;
            }

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
            TryStartQueuedDraftPhaseForCurrentRound();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked);
            if (player.PlayerType == PlayerTypeEnum.Human)
            {
                humanDraftedMycovariantThisDraftChain = true;
            }

            var pm = player.PlayerMycovariants.LastOrDefault(x => x.MycovariantId == picked.Id);
            if (pm != null && picked.AutoMarkTriggered)
            {
                picked.ApplyEffect?.Invoke(pm, Board, rng, gameUIManager.GameLogRouter);
            }
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);
        }

        private bool ShouldResolveTropicLysisAfterDraftChain()
        {
            return Board != null
                && humanPlayer != null
                && humanDraftedMycovariantThisDraftChain
                && humanPlayer.HasAdaptation(AdaptationIds.TropicLysis);
        }

        private IEnumerator ResolvePostDraftEffectsThenAdvance()
        {
            var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(
                humanPlayer,
                Board,
                gameUIManager.GameLogRouter);

            gameUIManager.GameLogRouter?.DisableSilentMode();
            gameUIManager.GameLogManager?.RecordVisibleTropicLysisEffect(
                humanPlayer != null ? humanPlayer.PlayerId : result.PlayerId,
                result.EnemyLivingCellsCleared,
                result.CorpsesCleared,
                result.ToxinsCleared);

            if (result.AnyCleared)
            {
                RefreshRightSidebarTopStats();
                gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

                if (gridVisualizer != null)
                {
                    yield return StartCoroutine(
                        gridVisualizer.PlayTropicLysisAnimation(
                            result.StartingTileId,
                            result.ToastDestinationTileId,
                            result.AffectedTileIds,
                            TropicLysisDisplayName));

                    gridVisualizer.RenderBoard(Board, suppressAnimations: true);
                }

                StartNextRound();
                yield break;
            }

            if (!testingModeEnabled)
            {
                yield return new WaitForSeconds(1f);
            }

            StartNextRound();
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
            gameTransitionService?.ShowStartGamePanel();
        }

        public void ReturnToMainMenu()
        {
            TryCancelActiveSelection();
            gameTransitionService?.ReturnToMainMenu();
        }

        public void QuitGame()
        {
            gameTransitionService?.QuitGame();
        }

        private void ResetManagerStateForMainMenuReturn()
        {
            gameEnded = false;
            isInDraftPhase = false;
            isFastForwarding = false;
            _fastForwardStarted = false;
            initialMutationPointsAssigned = false;
            skipMutationPointAssignmentForRoundStart = false;
            currentLevelGameplaySeed = 0;
            pendingGameplaySeed = null;
            endgameService?.Reset();

            FirstUpgradeRounds?.Clear();
            players.Clear();
            humanPlayers.Clear();
            humanPlayer = null!;
            Board = null!;
            // Start title/menu music when returning to main menu
            backgroundMusicService?.StartTitleMusic();
        }

        public void ShowSelectionPrompt(string message, bool showCancelButton = false, string cancelButtonLabel = "Cancel", Action? onCancel = null)
        {
            selectionPromptService?.Show(message, showCancelButton, cancelButtonLabel, onCancel);
        }

        public void HideSelectionPrompt()
        {
            selectionPromptService?.Hide();
        }

        public void SetActiveHumanPlayer(Player player)
        {
            playerPerspectiveService?.SetActiveHumanPlayer(player, Board, humanPlayers.Count, activePlayer => humanPlayer = activePlayer);
        }

        #endregion

        #region Testing Mode API / Fast Forward

        public void EnableTestingMode(
            int? mycovariantId,
            int fastForwardRounds =0,
            bool skipToEndgameAfterFastForward = false,
            bool forceFirstGame = false,
            ForcedGameResultMode forcedGameResult = ForcedGameResultMode.Natural,
            bool forceMoldinessRewards = false,
            int campaignLevelIndex = 0,
            string forcedAdaptationId = "",
            IReadOnlyList<string>? forcedStartingAdaptationIds = null)
        {
            testingModeEnabled = true;
            testingMycovariantId = mycovariantId;
            testingCampaignLevelIndex = Math.Max(0, campaignLevelIndex);
            testingForcedAdaptationId = forcedAdaptationId ?? string.Empty;
            testingForcedStartingAdaptationIds = forcedStartingAdaptationIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? new List<string>();
            testingModeForceHumanFirst = mycovariantId.HasValue;
            testingForcedGameResult = forcedGameResult;
            testingForceMoldinessRewards = skipToEndgameAfterFastForward
                && forcedGameResult == ForcedGameResultMode.ForcedWin
                && forceMoldinessRewards;
            this.fastForwardRounds = fastForwardRounds;
            testingSkipToEndgameAfterFastForward = skipToEndgameAfterFastForward;
            testingTreatAsFirstGame = forceFirstGame;
        }

        public void DisableTestingMode()
        {
            testingModeEnabled = false;
            testingMycovariantId = null;
            testingCampaignLevelIndex = 0;
            testingForcedAdaptationId = string.Empty;
            testingForcedStartingAdaptationIds = new List<string>();
            testingModeForceHumanFirst = false;
            testingForcedGameResult = ForcedGameResultMode.Natural;
            testingForceMoldinessRewards = false;
            fastForwardRounds =0;
            testingSkipToEndgameAfterFastForward = false;
            testingTreatAsFirstGame = false;
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

        private List<int> ResolveStartingSporeIntroTileIds()
        {
            if (Board == null)
            {
                return new List<int>();
            }

            List<int> playerStartingIds = Board.Players
                .Select(p => p.StartingTileId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (playerStartingIds.Count > 0)
            {
                return playerStartingIds;
            }

            List<int> boardStartingIds = Board.AllTiles()
                .Where(tile =>
                {
                    FungalCell? cell = tile.FungalCell;
                    return cell != null
                        && cell.IsAlive
                        && cell.IsResistant
                        && (cell.SourceOfGrowth ?? GrowthSource.Unknown) == GrowthSource.InitialSpore;
                })
                .Select(tile => tile.TileId)
                .Distinct()
                .OrderBy(tileId => tileId)
                .ToList();

            if (boardStartingIds.Count > 0)
            {
                Debug.LogWarning("[GameManager] Player starting tile metadata was missing; using board state to drive the starting spore intro.");
            }

            return boardStartingIds;
        }

        private bool ShouldSkipStartingSporeIntro()
        {
            if (!testingModeEnabled)
            {
                return false;
            }

            return fastForwardRounds > 0
                || testingSkipToEndgameAfterFastForward
                || testingForcedGameResult != ForcedGameResultMode.Natural
                || testingMycovariantId.HasValue;
        }

        private bool ShouldSkipTestingStartupEffects()
        {
            return testingModeEnabled && testingSkipToEndgameAfterFastForward;
        }

        private IEnumerator PlayStartingSporeIntroAndContinue()
        {
            yield return PlayGameplayEntryFlow(applyStartingSporeEffects: true, allowSkippingIntroForTesting: true);
        }

        private IEnumerator PlayRestoredGameIntroAndContinue()
        {
            yield return PlayGameplayEntryFlow(applyStartingSporeEffects: false, allowSkippingIntroForTesting: false);
        }

        private IEnumerator PlayGameplayEntryFlow(bool applyStartingSporeEffects, bool allowSkippingIntroForTesting)
        {
            List<int> startingIds = ResolveStartingSporeIntroTileIds();
            bool skipStartingSporeIntro = allowSkippingIntroForTesting && ShouldSkipStartingSporeIntro();
            bool skipTestingStartupEffects = ShouldSkipTestingStartupEffects();
            gameUIManager.LoadingScreen?.SetStatus("Spores are landing…");
            if (startingIds.Count > 0 && !skipStartingSporeIntro)
            {
                yield return gridVisualizer.PlayStartingSporeArrivalAnimation(
                    startingIds,
                    () => soundEffectService?.PlayOneShot(startingSporeDropClip, startingSporeDropVolume));
            }

            bool willFastForward = testingModeEnabled && fastForwardRounds > 0 && !_fastForwardStarted;
            if (!willFastForward)
            {
                gameUIManager.LoadingScreen?.FadeOut();
            }

            if (applyStartingSporeEffects && !skipTestingStartupEffects)
            {
                AdaptationEffectProcessor.OnStartingSporesEstablished(Board, players, rng);
            }

            if (skipTestingStartupEffects)
            {
                specialEventPresentationService?.Reset();
            }
            else if (specialEventPresentationService != null && specialEventPresentationService.HasPendingImmediateEvents)
            {
                yield return specialEventPresentationService.PresentPendingImmediate();
            }
            else
            {
                gridVisualizer.RenderBoard(Board, suppressAnimations: true);
            }

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);
            cameraCenterer?.CaptureInitialFraming();
            InitGameLogs();
            playerPerspectiveService?.InitializeGameplayPerspective(humanPlayer, Board, players, gridVisualizer);
            if (testingModeEnabled)
            {
                if (fastForwardRounds > 0 && !_fastForwardStarted)
                {
                    _fastForwardStarted = true;
                    fastForwardService.StartFastForward(fastForwardRounds, testingSkipToEndgameAfterFastForward, testingMycovariantId);
                }
                else if (testingSkipToEndgameAfterFastForward)
                {
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
                    gameUIManager.PhaseBanner.Show(CurrentGameMode == GameMode.Campaign ? "Campaign Level" : "New Game Settings", 2f);
                }
            }
            else
            {
                gameUIManager.PhaseBanner.Show(CurrentGameMode == GameMode.Campaign ? "Campaign Level" : "New Game Settings", 2f);
            }

            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.SetPerspectivePlayer(humanPlayer);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
            if (!(testingModeEnabled && (fastForwardRounds > 0 || testingMycovariantId.HasValue)))
            {
                StartNextRound();
            }
        }

        #endregion

        #region Internal Accessors For Services

        internal MutationManager GetPrivateMutationManager() => mutationManager;
        internal Player GetPrimaryHumanInternal() => humanPlayer;
        internal System.Random GetRngInternal() => rng;
        internal MycovariantPoolManager GetPersistentPoolInternal() => persistentPoolManager;

        private bool PrepareGameplayInitialization(int numberOfPlayers, string loadingMessage)
        {
            var ui = gameUIManager;
            if (ui == null)
            {
                Debug.LogError("[GameManager] Cannot initialize game: GameUIManager reference is missing.");
                return false;
            }

            gameTransitionService?.ResetRuntimeStateForGameTransition();
            ConfigureBackgroundMusicService();
            ui.MutationUIManager?.ResetForNewGameState();
            ui.EndGamePanel?.gameObject.SetActive(false);
            pauseMenuService?.ForceClose();

            ui.LoadingScreen?.Show(loadingMessage);
            gameEnded = false;
            isInDraftPhase = false;
            activeDraftCountsTowardRoundCompletion = false;
            isMycovariantDraftChainActive = false;
            humanDraftedMycovariantThisDraftChain = false;
            lastCompletedMycovariantDraftRound = -1;
            endgameService.Reset();
            endgamePlayerStatisticsTracker.Reset();
            specialEventPresentationService?.Reset();
            playerCount = numberOfPlayers;
            ui.MutationUIManager?.SetSpendPointsButtonInteractable(false);
            return true;
        }

        private int ResolveGameplaySeedForNewSession()
        {
            currentLevelGameplaySeed = pendingGameplaySeed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            pendingGameplaySeed = null;
            return currentLevelGameplaySeed;
        }

        private void PersistRoundStartAutosave()
        {
            if (Board == null || rng == null)
            {
                return;
            }

            var snapshot = RoundStartRuntimeSnapshotFactory.Export(Board, persistentPoolManager);
            var randomState = RandomStateSerialization.Capture(rng, currentLevelGameplaySeed);

            if (CurrentGameMode == GameMode.Campaign && campaignController != null && campaignController.HasActiveRun)
            {
                campaignController.SaveGameplayCheckpoint(snapshot, randomState, currentLevelGameplaySeed);
                return;
            }

            if (CurrentGameMode != GameMode.Hotseat)
            {
                return;
            }

            SoloGameSaveService.Save(new SoloGameSaveState
            {
                boardWidth = boardWidth,
                boardHeight = boardHeight,
                playerCount = playerCount,
                humanPlayerCount = configuredHumanPlayerCount,
                humanMoldIndices = configuredHumanMoldIndices.ToList(),
                gameplaySeed = currentLevelGameplaySeed,
                runtimeSnapshot = snapshot,
                randomState = randomState
            });
        }

        private void SaveCurrentRunForResume()
        {
            if (Board == null || rng == null || gameEnded)
            {
                return;
            }

            var snapshot = RoundStartRuntimeSnapshotFactory.Export(Board, persistentPoolManager);
            var randomState = RandomStateSerialization.Capture(rng, currentLevelGameplaySeed);

            if (CurrentGameMode == GameMode.Campaign && campaignController != null && campaignController.HasActiveRun)
            {
                campaignController.SaveGameplayCheckpoint(snapshot, randomState, currentLevelGameplaySeed);
                return;
            }

            if (CurrentGameMode != GameMode.Hotseat)
            {
                return;
            }

            SoloGameSaveService.Save(new SoloGameSaveState
            {
                boardWidth = boardWidth,
                boardHeight = boardHeight,
                playerCount = playerCount,
                humanPlayerCount = configuredHumanPlayerCount,
                humanMoldIndices = configuredHumanMoldIndices.ToList(),
                gameplaySeed = currentLevelGameplaySeed,
                runtimeSnapshot = snapshot,
                randomState = randomState
            });
        }

        private void ClearInProgressSaveAfterGameEnded()
        {
            if (CurrentGameMode == GameMode.Campaign)
            {
                campaignController?.ClearGameplayCheckpoint();
                return;
            }

            if (CurrentGameMode == GameMode.Hotseat)
            {
                SoloGameSaveService.Delete();
            }
        }

        private IMutationSpendingStrategy? ResolveMutationStrategyFromSnapshot(PlayerRuntimeSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.MutationStrategyName))
            {
                return null;
            }

            string strategyName = snapshot.MutationStrategyName;
            if (AIRoster.ProvenStrategiesByName.TryGetValue(strategyName, out var provenStrategy))
            {
                return provenStrategy;
            }

            if (AIRoster.TestingStrategiesByName.TryGetValue(strategyName, out var testingStrategy))
            {
                return testingStrategy;
            }

            string normalizedCampaignName = AIRoster.NormalizeCampaignStrategyName(strategyName);
            if (AIRoster.CampaignStrategiesByName.TryGetValue(normalizedCampaignName, out var campaignStrategy))
            {
                return campaignStrategy;
            }

            if (AIRoster.CampaignStrategiesByName.TryGetValue(strategyName, out campaignStrategy))
            {
                return campaignStrategy;
            }

            Debug.LogWarning($"[GameManager] Could not resolve saved mutation strategy '{strategyName}' during restore.");
            return null;
        }

        public bool IsPauseMenuOpen => pauseMenuService != null && pauseMenuService.IsOpen;

        internal void TriggerEndGameInternal()
        {
            bool wasGameEnded = gameEnded;
            endgameService.EndGame();
            gameEnded = endgameService.GameEnded;
            if (!wasGameEnded && gameEnded)
            {
                ClearInProgressSaveAfterGameEnded();
            }
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

        public void SkipToNextTrack()
        {
            backgroundMusicService?.SkipToNextTrack();
        }

        public string GetCurrentGameplayTrackName()
        {
            AudioClip? clip = backgroundMusicService?.GetCurrentGameplayTrack();
            return clip != null ? clip.name : string.Empty;
        }

        public string GetNextGameplayTrackName()
        {
            AudioClip? clip = backgroundMusicService?.GetNextGameplayTrack();
            return clip != null ? clip.name : string.Empty;
        }

        private void ConfigureBackgroundMusicService()
        {
            backgroundMusicService?.ConfigureGameplayMusic(
                BuildGameplayMusicPlaylist(),
                gameplayMusicVolume,
                gameplayMusicInitialDelaySeconds,
                gameplayMusicReplayDelaySeconds,
                gameplayMusicFadeInSeconds,
                gameplayMusicMixerGroup);
        }

        private AudioClip[] BuildGameplayMusicPlaylist()
        {
            List<AudioClip> playlist = new();

            if (gameplayMusicClip != null)
            {
                playlist.Add(gameplayMusicClip);
            }

            if (additionalGameplayMusicClips == null)
            {
                return playlist.ToArray();
            }

            foreach (AudioClip clip in additionalGameplayMusicClips)
            {
                if (clip == null || playlist.Contains(clip))
                {
                    continue;
                }

                playlist.Add(clip);
            }

            return playlist.ToArray();
        }

        #endregion
    }
}
