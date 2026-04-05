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
using UnityEngine.Audio;
using UnityEngine.UI;
using FungusToast.Unity.Campaign; // NEW: campaign namespace

namespace FungusToast.Unity
{
    public static class SoundEffectsSettings
    {
        private const string EnabledKey = "Audio.Sfx.Enabled";
        private const string VolumeKey = "Audio.Sfx.Volume";

        private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private static bool loaded;
        private static bool enabled = true;
        private static float volume = 1f;

        public static bool Enabled
        {
            get
            {
                EnsureLoaded();
                return enabled;
            }
        }

        public static float Volume
        {
            get
            {
                EnsureLoaded();
                return volume;
            }
        }

        public static void ToggleEnabled()
        {
            SetEnabled(!Enabled);
        }

        public static void SetEnabled(bool value)
        {
            EnsureLoaded();
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void CycleVolumeForward()
        {
            EnsureLoaded();

            int currentIndex = 0;
            for (int index = 0; index < VolumeSteps.Length; index++)
            {
                if (Mathf.Approximately(VolumeSteps[index], volume))
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % VolumeSteps.Length;
            SetVolume(VolumeSteps[nextIndex]);
        }

        public static void SetVolume(float value)
        {
            EnsureLoaded();
            float clampedValue = Mathf.Clamp01(value);
            if (Mathf.Approximately(volume, clampedValue))
            {
                return;
            }

            volume = clampedValue;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetEffectiveVolume(float baseVolume)
        {
            EnsureLoaded();
            if (!enabled)
            {
                return 0f;
            }

            return Mathf.Clamp01(baseVolume) * volume;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            enabled = PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            loaded = true;
        }
    }

    public static class MusicSettings
    {
        private const string VolumeKey = "Audio.Music.Volume";

        private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private static bool loaded;
        private static float volume = 0.75f;

        public static float Volume
        {
            get
            {
                EnsureLoaded();
                return volume;
            }
        }

        public static void CycleVolumeForward()
        {
            EnsureLoaded();

            int currentIndex = 0;
            for (int index = 0; index < VolumeSteps.Length; index++)
            {
                if (Mathf.Approximately(VolumeSteps[index], volume))
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % VolumeSteps.Length;
            SetVolume(VolumeSteps[nextIndex]);
        }

        public static void SetVolume(float value)
        {
            EnsureLoaded();
            float clampedValue = Mathf.Clamp01(value);
            if (Mathf.Approximately(volume, clampedValue))
            {
                return;
            }

            volume = clampedValue;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetEffectiveVolume(float baseVolume)
        {
            EnsureLoaded();
            return Mathf.Clamp01(baseVolume) * volume;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.75f));
            loaded = true;
        }
    }

    public class GameManager : MonoBehaviour
    {
        private const string AlphaMutationOnboardingSeenKey = "Onboarding.AlphaMutationPhaseSeen";
        private const string AlphaMutationOnboardingBannerText = "Goal: control the largest share of the toast.\nSpend mutation points for upgrades now or store them to save for stronger upgrades later.\nAfter that, your colony grows automatically.";

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
        public bool testingTreatAsFirstGame = false;

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
        [SerializeField] private Button selectionPromptCancelButton;
        [SerializeField] private TextMeshProUGUI selectionPromptCancelButtonText;
        [SerializeField] private GameObject modeSelectPanel; // NEW: root of mode select UI
        [SerializeField] private AudioClip mutationPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float mutationPhaseStartVolume = 1f;
        [SerializeField] private AudioClip growthPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float growthPhaseStartVolume = 1f;
        [SerializeField] private AudioClip decayPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float decayPhaseStartVolume = 1f;
        [SerializeField] private AudioClip draftPhaseStartClip = null;
        [SerializeField, Range(0f, 1f)] private float draftPhaseStartVolume = 1f;
        [SerializeField] private AudioClip gameplayMusicClip = null;
        [SerializeField] private AudioClip[] additionalGameplayMusicClips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 1f;
        [SerializeField, Min(0f)] private float gameplayMusicInitialDelaySeconds = 10f;
        [SerializeField, Min(0f)] private float gameplayMusicReplayDelaySeconds = 5f;
        [SerializeField, Min(0f)] private float gameplayMusicFadeInSeconds = 1f;
        [SerializeField] private AudioMixerGroup gameplayMusicMixerGroup = null;

        [Header("Hotseat Config")] 
        public int configuredHumanPlayerCount =1; 
        public int ConfiguredHumanPlayerCount => configuredHumanPlayerCount; 
        private readonly List<int> configuredHumanMoldIndices = new();
        public void SetHotseatConfig(int humanCount) => SetHotseatConfig(humanCount, null);
        public void SetHotseatConfig(int humanCount, IReadOnlyList<int> humanMoldIndices)
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
        public SpecialEventPresentationService SpecialEventPresentationService => specialEventPresentationService;
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new(); 
        private readonly List<Player> humanPlayers = new(); 
        private Player humanPlayer; // primary

        private bool isInDraftPhase = false; 
        public bool IsDraftPhaseActive => isInDraftPhase; 
        private int lastCompletedMycovariantDraftRound = -1;
        private bool activeDraftCountsTowardRoundCompletion;
        public int LastCompletedMycovariantDraftRound => lastCompletedMycovariantDraftRound;
        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        public bool IsTestingModeEnabled => testingModeEnabled; 
        public int? TestingMycovariantId => testingMycovariantId;
        public ForcedGameResultMode TestingForcedGameResult => testingForcedGameResult;
        public bool ShouldForceFirstGameExperience => testingModeEnabled && testingTreatAsFirstGame;

        private bool isFastForwarding = false; 
        public bool IsFastForwarding => isFastForwarding; 
        private bool _fastForwardStarted = false;
        private bool initialMutationPointsAssigned = false;
        private bool pendingAlphaMutationOnboarding;
        private float nextUiStuckCheckTime;
        private bool isPauseMenuOpen;
        private bool hasApplicationFocus = true;
        private UI_PauseMenuPanel pauseMenuPanel;
        private AudioSource soundEffectAudioSource;

        // Services
        private PlayerInitializer playerInitializer; 
        private HotseatTurnManager hotseatTurnManager; 
        private FastForwardService fastForwardService; 
        private PostGrowthVisualSequence postGrowthVisualSequence;
        private EndgameService endgameService;
        private MutationPointService mutationPointService;
        private SpecialEventPresentationService specialEventPresentationService;
        private BackgroundMusicService backgroundMusicService;

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

        #endregion

        #region Bootstrap

        private void BootstrapServices()
        {
            EnsureSoundEffectAudioSource();
            BootstrapPauseMenu();
            backgroundMusicService = new BackgroundMusicService(this, transform, () => !hasApplicationFocus);
            ConfigureBackgroundMusicService();

            playerInitializer = new PlayerInitializer(
                gridVisualizer,
                gameUIManager,
                () => configuredHumanPlayerCount,
                () => CurrentGameMode,
                () => campaignController?.CurrentBoardPreset,
                () => campaignController?.CurrentResolvedAiStrategyNames);
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
            gridVisualizer?.ClearBoardMediumOverride();
            gridVisualizer?.ClearPlayerMoldAssignments();
            InitializeGame(numberOfPlayers);
        }

        public void StartCampaignNew(int humanMoldIndex = 0)
        {
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot start campaign: CampaignProgression not assigned.");
                return;
            }
            campaignController.StartNew(humanMoldIndex);
            CurrentGameMode = GameMode.Campaign;
            var preset = campaignController.CurrentBoardPreset;
            if (preset != null)
            {
                boardWidth = preset.boardWidth;
                boardHeight = preset.boardHeight;
            }
            gridVisualizer?.SetBoardMedium(preset?.boardMedium);
            int totalPlayers = 1 + (campaignController.GetCurrentAiPlayerCount());
            SetHotseatConfig(1, new[] { campaignController.HumanMoldIndex });
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
            gridVisualizer?.SetBoardMedium(preset?.boardMedium);
            int totalPlayers = 1 + campaignController.GetCurrentAiPlayerCount();
            SetHotseatConfig(1, new[] { campaignController.HumanMoldIndex });
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

                int totalPlayersFallback = 1 + campaignController.GetCurrentAiPlayerCount();
                SetHotseatConfig(1, new[] { campaignController.HumanMoldIndex });
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
            ResetRuntimeStateForGameTransition();
            ConfigureBackgroundMusicService();
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

            ApplyConfiguredPlayerMoldAssignments(playerCount);

            playerInitializer.InitializePlayers(Board, players, humanPlayers, out humanPlayer, playerCount);
            ApplyCampaignAdaptations();
            SubscribeToPlayerMutationEvents();

            persistentPoolManager = new MycovariantPoolManager();
            persistentPoolManager.InitializePool(MycovariantRepository.All.ToList(), rng);

            mutationManager.ResetMutationPoints(players);
            initialMutationPointsAssigned = true;

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);
            InitializeHumanSidebarUiForCurrentPlayer();

            backgroundMusicService?.StartGameplayMusic();
            StartCoroutine(PlayStartingSporeIntroAndContinue());

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

        private void ApplyConfiguredPlayerMoldAssignments(int totalPlayers)
        {
            if (gridVisualizer == null)
            {
                return;
            }

            var assignments = ResolveConfiguredPlayerMoldAssignments(totalPlayers);
            if (assignments.Count == 0)
            {
                gridVisualizer.ClearPlayerMoldAssignments();
                return;
            }

            gridVisualizer.SetPlayerMoldAssignments(assignments);
        }

        private List<int> ResolveConfiguredPlayerMoldAssignments(int totalPlayers)
        {
            var assignments = new List<int>();
            int availableMolds = gridVisualizer != null ? gridVisualizer.PlayerMoldTileCount : 0;
            if (availableMolds <= 0 || totalPlayers <= 0)
            {
                return assignments;
            }

            int humanCount = Mathf.Clamp(configuredHumanPlayerCount, 1, totalPlayers);
            var remainingMolds = Enumerable.Range(0, availableMolds).ToList();

            for (int humanIndex = 0; humanIndex < humanCount; humanIndex++)
            {
                int moldIndex = TakeConfiguredOrFallbackHumanMoldIndex(humanIndex, remainingMolds);
                assignments.Add(moldIndex);
                remainingMolds.Remove(moldIndex);
            }

            for (int playerIndex = humanCount; playerIndex < totalPlayers; playerIndex++)
            {
                if (remainingMolds.Count > 0)
                {
                    assignments.Add(remainingMolds[0]);
                    remainingMolds.RemoveAt(0);
                    continue;
                }

                assignments.Add(playerIndex % availableMolds);
            }

            return assignments;
        }

        private int TakeConfiguredOrFallbackHumanMoldIndex(int humanIndex, List<int> remainingMolds)
        {
            if (remainingMolds == null || remainingMolds.Count == 0)
            {
                return 0;
            }

            if (humanIndex < configuredHumanMoldIndices.Count)
            {
                int configuredMoldIndex = configuredHumanMoldIndices[humanIndex];
                if (remainingMolds.Contains(configuredMoldIndex))
                {
                    return configuredMoldIndex;
                }
            }

            return remainingMolds[0];
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
            PlayGrowthPhaseStartSound();
            gameUIManager.PhaseBanner.Show("Growth Phase Begins!",2f);
            phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
            StartCoroutine(BeginGrowthPhaseAfterPreGrowthEffects());
        }

        private IEnumerator BeginGrowthPhaseAfterPreGrowthEffects()
        {
            var chitinFortificationTileIds = new List<int>();

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

            Board.ResistanceAppliedBatch += BufferPreGrowthResistanceAnimation;
            try
            {
                Board.OnPreGrowthPhase();
            }
            finally
            {
                Board.ResistanceAppliedBatch -= BufferPreGrowthResistanceAnimation;
            }

            if (!isFastForwarding && chitinFortificationTileIds.Count > 0)
            {
                gridVisualizer.DeferResistanceOverlayReveal(chitinFortificationTileIds);
                gridVisualizer.RenderBoard(Board, suppressAnimations: true);
                gridVisualizer.PlayResistancePulseBatchScaled(chitinFortificationTileIds, 0.5f);
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
            PlayDecayPhaseStartSound();
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

            pendingAlphaMutationOnboarding = ShouldQueueAlphaMutationOnboarding();

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
            if (humanPlayers.Count > 0)
            {
                gameUIManager.RightSidebar?.TryShowScoreboardWinConditionCoachmark(Board.CurrentRound);
            }
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
            gameUIManager.GameLogRouter?.OnPhaseStart("Mutation");
            if (!pendingAlphaMutationOnboarding)
            {
                PlayMutationPhaseStartSound();
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

        private void PlayMutationPhaseStartSound()
        {
            if (mutationPhaseStartClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(mutationPhaseStartVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(mutationPhaseStartClip, effectiveVolume);
        }

        private void PlayGrowthPhaseStartSound()
        {
            if (growthPhaseStartClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(growthPhaseStartVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(growthPhaseStartClip, effectiveVolume);
        }

        private void PlayDecayPhaseStartSound()
        {
            if (decayPhaseStartClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(decayPhaseStartVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(decayPhaseStartClip, effectiveVolume);
        }

        private void PlayDraftPhaseStartSound()
        {
            if (draftPhaseStartClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(draftPhaseStartVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(draftPhaseStartClip, effectiveVolume);
        }

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
                PlayDraftPhaseStartSound();
                gameUIManager.PhaseBanner.Show($"Testing: {name}",2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart(name);
            }
            else
            {
                PlayDraftPhaseStartSound();
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
                if (gameUIManager.GlobalGameLogManager != null && gameUIManager.GlobalGameLogPanel != null)
                {
                    gameUIManager.GlobalGameLogPanel.Initialize(gameUIManager.GlobalGameLogManager);
                }
                if (gameUIManager.GameLogManager != null && gameUIManager.GameLogPanel != null)
                {
                    gameUIManager.GameLogPanel.Initialize(gameUIManager.GameLogManager);
                }
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
            ResetRuntimeStateForGameTransition();
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

        private void ResetRuntimeStateForGameTransition()
        {
            var currentBoard = Board;
            backgroundMusicService?.StopGameplayMusic();

            UnsubscribeFromPlayerMutationEvents();

            if (currentBoard != null)
            {
                postGrowthVisualSequence?.ResetForGameTransition(currentBoard);
                GameRulesEventSubscriber.UnsubscribeAll(currentBoard);
                GameUIEventSubscriber.Unsubscribe(currentBoard, gameUIManager);
                AnalyticsEventSubscriber.Unsubscribe(currentBoard, gameUIManager?.GameLogRouter);
            }

            StopAllCoroutines();
            mycovariantDraftController?.ResetForGameTransition();
            growthPhaseRunner?.ResetForGameTransition();
            decayPhaseRunner?.ResetForGameTransition();
            gridVisualizer?.ResetForGameTransition();
            specialEventPresentationService?.Reset();
            gameUIManager?.MutationUIManager?.ResetForNewGameState();
            gameUIManager?.MutationTreeToastPresenter?.ResetForGameTransition();
            gameUIManager?.GameLogManager?.ResetForGameTransition();
            gameUIManager?.GlobalGameLogManager?.ResetForGameTransition();
            gameUIManager?.ClearBoard();
            gameUIManager?.GameLogRouter?.DisableSilentMode();
            TooltipManager.Instance?.CancelAll();
        }

        public void QuitGame()
        {
            ForceClosePauseMenu();
            backgroundMusicService?.StopGameplayMusic();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowSelectionPrompt(string message, bool showCancelButton = false, string cancelButtonLabel = "Cancel", Action onCancel = null)
        {
            EnsureSelectionPromptCancelButton();
            SelectionPromptPanel.SetActive(true);
            SelectionPromptText.text = message;
            ConfigureSelectionPromptCancelButton(showCancelButton, cancelButtonLabel, onCancel);
        }

        public void HideSelectionPrompt()
        {
            ConfigureSelectionPromptCancelButton(false, "Cancel", null);
            SelectionPromptPanel.SetActive(false);
        }

        private void EnsureSelectionPromptCancelButton()
        {
            if (SelectionPromptPanel == null)
            {
                return;
            }

            if (selectionPromptCancelButton == null)
            {
                selectionPromptCancelButton = SelectionPromptPanel.GetComponentInChildren<Button>(true);
            }

            if (selectionPromptCancelButton == null)
            {
                var buttonObject = new GameObject("UI_SelectionPromptCancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.layer = SelectionPromptPanel.layer;
                buttonObject.transform.SetParent(SelectionPromptPanel.transform, false);

                var rectTransform = buttonObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(1f, 0.5f);
                rectTransform.anchorMax = new Vector2(1f, 0.5f);
                rectTransform.pivot = new Vector2(1f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-18f, 0f);
                rectTransform.sizeDelta = new Vector2(170f, 38f);

                var image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.16f, 0.12f, 0.1f, 0.92f);

                selectionPromptCancelButton = buttonObject.GetComponent<Button>();
                var colors = selectionPromptCancelButton.colors;
                colors.normalColor = image.color;
                colors.highlightedColor = new Color(0.26f, 0.2f, 0.16f, 1f);
                colors.pressedColor = new Color(0.12f, 0.09f, 0.07f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.16f, 0.12f, 0.1f, 0.45f);
                selectionPromptCancelButton.colors = colors;

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.layer = SelectionPromptPanel.layer;
                labelObject.transform.SetParent(buttonObject.transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                selectionPromptCancelButtonText = labelObject.GetComponent<TextMeshProUGUI>();
                selectionPromptCancelButtonText.fontSize = 22f;
                selectionPromptCancelButtonText.fontStyle = FontStyles.Bold;
                selectionPromptCancelButtonText.alignment = TextAlignmentOptions.Center;
                selectionPromptCancelButtonText.color = new Color(0.97f, 0.94f, 0.86f, 1f);
            }

            if (selectionPromptCancelButtonText == null && selectionPromptCancelButton != null)
            {
                selectionPromptCancelButtonText = selectionPromptCancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (SelectionPromptText != null)
            {
                SelectionPromptText.margin = new Vector4(18f, 0f, 200f, 0f);
            }
        }

        private void ConfigureSelectionPromptCancelButton(bool visible, string cancelButtonLabel, Action onCancel)
        {
            if (selectionPromptCancelButton == null)
            {
                return;
            }

            selectionPromptCancelButton.onClick.RemoveAllListeners();
            selectionPromptCancelButton.gameObject.SetActive(visible);
            selectionPromptCancelButton.interactable = visible;

            if (selectionPromptCancelButtonText != null)
            {
                selectionPromptCancelButtonText.text = cancelButtonLabel;
            }

            if (visible && onCancel != null)
            {
                selectionPromptCancelButton.onClick.AddListener(() => onCancel());
            }
        }

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
            bool forceFirstGame = false,
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
            testingTreatAsFirstGame = forceFirstGame;
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

            AdaptationEffectProcessor.OnStartingSporesEstablished(Board, players);
            if (specialEventPresentationService != null && specialEventPresentationService.HasPendingImmediateEvents)
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
            InitializeHumanSidebarUiForCurrentPlayer();
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

        private void InitializeHumanSidebarUiForCurrentPlayer()
        {
            if (humanPlayer == null || gameUIManager == null)
            {
                return;
            }

            gameUIManager.MoldProfileRoot?.Initialize(humanPlayer, Board?.Players);

            if (gameUIManager.MutationUIManager != null)
            {
                gameUIManager.MutationUIManager.ReinitializeForPlayer(humanPlayer, keepPanelClosed: true);
                gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
                gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();
                gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
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
                QuitGame,
                SkipToNextTrack,
                GetCurrentGameplayTrackName,
                GetNextGameplayTrackName);

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

            gameUIManager?.MutationUIManager?.ForceCloseTreePanel();
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

        public void SkipToNextTrack()
        {
            backgroundMusicService?.SkipToNextTrack();
        }

        public string GetCurrentGameplayTrackName()
        {
            AudioClip clip = backgroundMusicService?.GetCurrentGameplayTrack();
            return clip != null ? clip.name : string.Empty;
        }

        public string GetNextGameplayTrackName()
        {
            AudioClip clip = backgroundMusicService?.GetNextGameplayTrack();
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
