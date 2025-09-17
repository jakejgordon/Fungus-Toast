using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Board Settings")] public int boardWidth = 160; public int boardHeight = 160; public int playerCount = 2;
        [Header("Testing Mode")] public bool testingModeEnabled = false; public int? testingMycovariantId = null; public bool testingModeForceHumanFirst = true; public int fastForwardRounds = 0; public bool testingSkipToEndgameAfterFastForward = false;
        [Header("References")] public GridVisualizer gridVisualizer; public CameraCenterer cameraCenterer; [SerializeField] private MutationManager mutationManager; [SerializeField] private GrowthPhaseRunner growthPhaseRunner; [SerializeField] private GameUIManager gameUIManager; [SerializeField] private DecayPhaseRunner decayPhaseRunner; [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker; [SerializeField] private MycovariantDraftController mycovariantDraftController; [SerializeField] private UI_StartGamePanel startGamePanel; [SerializeField] private UI_HotseatTurnPrompt hotseatTurnPrompt; public GameObject SelectionPromptPanel; public TextMeshProUGUI SelectionPromptText;
        [Header("Hotseat Config")] public int configuredHumanPlayerCount = 1; public int ConfiguredHumanPlayerCount => configuredHumanPlayerCount; public void SetHotseatConfig(int humanCount) { configuredHumanPlayerCount = Mathf.Max(1, humanCount); }
        #endregion

        #region State Fields / Services
        private bool isCountdownActive = false; private int roundsRemainingUntilGameEnd = 0; private bool gameEnded = false; private System.Random rng; private MycovariantPoolManager persistentPoolManager;
        public GameBoard Board { get; private set; } public GameUIManager GameUI => gameUIManager; public static GameManager Instance { get; private set; }
        private readonly List<Player> players = new(); private readonly List<Player> humanPlayers = new(); private Player humanPlayer; // primary
        private bool isInDraftPhase = false; public bool IsDraftPhaseActive => isInDraftPhase; private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();
        public bool IsTestingModeEnabled => testingModeEnabled; public int? TestingMycovariantId => testingMycovariantId;
        private bool isFastForwarding = false; public bool IsFastForwarding => isFastForwarding; private bool _fastForwardStarted = false;
        private bool initialMutationPointsAssigned = false;

        // Services
        private PlayerInitializer playerInitializer; private HotseatTurnManager hotseatTurnManager; private FastForwardService fastForwardService; private PostGrowthVisualSequence postGrowthVisualSequence;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            FungusToast.Core.Logging.CoreLogger.Log = Debug.Log;
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            rng = new System.Random();
            BootstrapServices();
        }
        private void Start() { if (Application.isPlaying) ShowStartGamePanel(); }
        #endregion

        #region Bootstrap
        private void BootstrapServices()
        {
            playerInitializer = new PlayerInitializer(gridVisualizer, gameUIManager, () => configuredHumanPlayerCount);
            hotseatTurnManager = new HotseatTurnManager(gameUIManager, hotseatTurnPrompt, humanPlayers, () => humanPlayer, () => isFastForwarding, () => testingModeEnabled, OnAllHumansFinishedMutationTurn);
            fastForwardService = new FastForwardService(this, () => isFastForwarding, v => isFastForwarding = v, () => gameEnded);
            postGrowthVisualSequence = new PostGrowthVisualSequence(this, gridVisualizer, () => isFastForwarding, StartDecayPhase);
        }
        #endregion

        #region Initialization
        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false; isCountdownActive = false; roundsRemainingUntilGameEnd = 0; playerCount = numberOfPlayers; gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            rng = new System.Random();
            postGrowthVisualSequence.Register(Board); // board post-growth events

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, gameUIManager.GameLogRouter); GameUIEventSubscriber.Subscribe(Board, gameUIManager); AnalyticsEventSubscriber.Subscribe(Board, gameUIManager.GameLogRouter);

            // Player init
            playerInitializer.InitializePlayers(Board, players, humanPlayers, out humanPlayer, playerCount);
            SubscribeToPlayerMutationEvents();

            // Draft pool
            persistentPoolManager = new MycovariantPoolManager(); persistentPoolManager.InitializePool(MycovariantRepository.All.ToList(), rng);

            gridVisualizer.Initialize(Board); PlaceStartingSpores(); gridVisualizer.RenderBoard(Board);
            StartCoroutine(PlayStartingSporeIntroAndContinue()); mutationManager.ResetMutationPoints(players);
            initialMutationPointsAssigned = true;

            gameUIManager.LeftSidebar?.gameObject.SetActive(true); gameUIManager.RightSidebar?.gameObject.SetActive(true); gameUIManager.MutationUIManager.gameObject.SetActive(true); mycovariantDraftController?.gameObject.SetActive(false); cameraCenterer?.CaptureInitialFraming();
            InitGameLogs();

            if (testingModeEnabled)
            {
                if (fastForwardRounds <= 0 && testingMycovariantId.HasValue) StartMycovariantDraftPhase(); else if (fastForwardRounds <= 0) gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
            }
            else gameUIManager.PhaseBanner.Show("New Game Settings", 2f);

            phaseProgressTracker?.ResetTracker(); UpdatePhaseProgressTrackerLabel(); phaseProgressTracker?.HighlightMutationPhase(); gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer); gameUIManager.RightSidebar?.InitializePlayerSummaries(players); gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer); gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
        }
        private void InitGameLogs()
        {
            if (gameUIManager.GameLogManager != null && gameUIManager.GameLogPanel != null) { gameUIManager.GameLogManager.Initialize(Board); gameUIManager.GameLogPanel.Initialize(gameUIManager.GameLogManager); }
            if (gameUIManager.GlobalGameLogManager != null && gameUIManager.GlobalGameLogPanel != null) { gameUIManager.GlobalGameLogManager.Initialize(Board); gameUIManager.GlobalGameLogPanel.Initialize(gameUIManager.GlobalGameLogManager); }
        }
        private void SubscribeToPlayerMutationEvents() { foreach (var p in Board.Players) p.MutationsChanged += OnPlayerMutationsChanged; }
        private void OnPlayerMutationsChanged(Player p) { if (p == humanPlayer && !isFastForwarding) gameUIManager.MoldProfileRoot?.Refresh(); }
        #endregion

        #region Phase Control
        private void PlaceStartingSpores() { StartingSporeUtility.PlaceStartingSpores(Board, players, rng); int round = Board.CurrentRound; float occ = Board.GetOccupiedTileRatio() * 100f; gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ); }
        public void StartGrowthPhase() { gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false); gameUIManager.GameLogRouter?.OnPhaseStart("Growth"); growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer); gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f); phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle); Board.OnPreGrowthPhase(); growthPhaseRunner.StartGrowthPhase(); }
        public void StartDecayPhase() { if (gameEnded) return; gameUIManager.GameLogRouter?.OnPhaseStart("Decay"); decayPhaseRunner.Initialize(Board, Board.Players, gridVisualizer); gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f); phaseProgressTracker?.HighlightDecayPhase(); decayPhaseRunner.StartDecayPhase(growthPhaseRunner.FailedGrowthsByPlayerId, rng, gameUIManager.GameLogRouter); }
        public void OnRoundComplete() { if (gameEnded) return; gameUIManager.GameLogRouter?.OnRoundComplete(Board.CurrentRound, Board); foreach (var p in Board.Players) p.TickDownActiveSurges(); CheckForEndgameCondition(); if (gameEnded) return; Board.IncrementRound(); if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound)) { StartCoroutine(DelayedStartDraft()); return; } StartNextRound(); int round = Board.CurrentRound; float occ = Board.GetOccupiedTileRatio() * 100f; gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occ); gameUIManager.RightSidebar.UpdateRandomDecayChance(round); TrackFirstUpgradeRounds(); }
        private void TrackFirstUpgradeRounds() { foreach (var player in Board.Players) foreach (var pm in player.PlayerMutations.Values) { if (!pm.FirstUpgradeRound.HasValue) continue; var key = (player.PlayerId, pm.MutationId); if (!FirstUpgradeRounds.ContainsKey(key)) FirstUpgradeRounds[key] = new List<int>(); FirstUpgradeRounds[key].Add(pm.FirstUpgradeRound.Value); } }
        public void StartNextRound() { if (gameEnded) return; gameUIManager.GameLogRouter?.OnRoundStart(Board.CurrentRound); if (!(Board.CurrentRound == 1 && initialMutationPointsAssigned)) AssignMutationPoints(); hotseatTurnManager.BeginHumanMutationPhase(); gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players); gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound); gameUIManager.GameLogRouter?.OnPhaseStart("Mutation"); gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f); UpdatePhaseProgressTrackerLabel(); phaseProgressTracker?.HighlightMutationPhase(); }
        #endregion

        #region Hotseat Callbacks
        public void OnHumanMutationTurnFinished(Player player) { hotseatTurnManager.HandleHumanTurnFinished(player); }
        private void OnAllHumansFinishedMutationTurn() { SpendAllMutationPointsForAIPlayers(); }
        #endregion

        #region Endgame / Countdown
        private void CheckForEndgameCondition() { if (!isCountdownActive && Board.ShouldTriggerEndgame()) { isCountdownActive = true; roundsRemainingUntilGameEnd = GameBalance.TurnsAfterEndGameTileOccupancyThresholdMet; UpdateCountdownUI(); } else if (isCountdownActive) { roundsRemainingUntilGameEnd--; if (roundsRemainingUntilGameEnd <= 0) EndGame(); else UpdateCountdownUI(); } }
        private void UpdateCountdownUI() { if (!isCountdownActive) { gameUIManager.RightSidebar?.SetEndgameCountdownText(null); return; } if (roundsRemainingUntilGameEnd == 1) { gameUIManager.RightSidebar?.SetEndgameCountdownText("<b><color=#FF0000>Final Round!</color></b>"); gameUIManager.GameLogRouter?.OnEndgameTriggered(1); } else { gameUIManager.RightSidebar?.SetEndgameCountdownText($"<b><color=#FFA500>Endgame in {roundsRemainingUntilGameEnd} rounds</color></b>"); gameUIManager.GameLogRouter?.OnEndgameTriggered(roundsRemainingUntilGameEnd); } }
        private void EndGame() { if (gameEnded) return; gameEnded = true; gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false); var ranked = Board.Players.OrderByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive)).ThenByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive)).ToList(); var winner = ranked.FirstOrDefault(); if (winner != null) gameUIManager.GameLogRouter?.OnGameEnd(winner.PlayerName); gameUIManager.MutationUIManager.gameObject.SetActive(false); gameUIManager.RightSidebar.gameObject.SetActive(true); gameUIManager.LeftSidebar.gameObject.SetActive(false); gameUIManager.EndGamePanel.gameObject.SetActive(true); gameUIManager.EndGamePanel.ShowResults(ranked, Board); foreach (var ((pid, mid), rounds) in FirstUpgradeRounds) { double avg = rounds.Average(); int min = rounds.Min(); int max = rounds.Max(); Console.WriteLine($"Player {pid} | Mutation {mid} | Avg First Acquired: {avg:F1} | Min: {min} | Max: {max}"); } }
        #endregion

        #region Mutation Points / AI
        private void AssignMutationPoints() { var all = mutationManager.AllMutations.Values.ToList(); var localRng = new System.Random(); TurnEngine.AssignMutationPoints(Board, Board.Players, all, localRng, gameUIManager.GameLogRouter); gameUIManager.MutationUIManager?.RefreshAllMutationButtons(); gameUIManager.MoldProfileRoot?.Refresh(); }
        public void SpendAllMutationPointsForAIPlayers() { var all = mutationManager.GetAllMutations().ToList(); foreach (var p in Board.Players) if (p.PlayerType == PlayerTypeEnum.AI) p.MutationStrategy?.SpendMutationPoints(p, all, Board, rng, gameUIManager.GameLogRouter); StartGrowthPhase(); }
        #endregion

        #region Draft Phase
        private void UpdatePhaseProgressTrackerLabel() { phaseProgressTracker?.SetMutationPhaseLabel(isInDraftPhase ? "DRAFT" : "MUTATION"); }
        public void StartMycovariantDraftPhase() { isInDraftPhase = true; TooltipManager.Instance?.CancelAll(); var order = testingModeEnabled && testingModeForceHumanFirst ? Board.Players.OrderBy(p => p.PlayerType == PlayerTypeEnum.Human ? 0 : 1).ThenBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive)).ToList() : Board.Players.OrderBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive)).ThenBy(p => p.PlayerId).ToList(); if (testingModeEnabled && testingModeForceHumanFirst) testingModeForceHumanFirst = false; mycovariantDraftController.StartDraft(Board.Players, persistentPoolManager, order, rng, MycovariantGameBalance.MycovariantSelectionDraftSize); if (testingModeEnabled) { var tMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId); var name = tMyco?.Name ?? "Unknown"; gameUIManager.PhaseBanner.Show($"Testing: {name}", 2f); gameUIManager.GameLogRouter?.OnDraftPhaseStart(name); } else { gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase!", 2f); gameUIManager.GameLogRouter?.OnDraftPhaseStart(); } phaseProgressTracker?.HighlightDraftPhase(); gameUIManager.MutationUIManager.gameObject.SetActive(false); gameUIManager.LeftSidebar?.gameObject.SetActive(false); mycovariantDraftController.gameObject.SetActive(true); }
        public void OnMycovariantDraftComplete() { isInDraftPhase = false; TooltipManager.Instance?.CancelAll(); gameUIManager.MutationUIManager.gameObject.SetActive(true); gameUIManager.RightSidebar?.gameObject.SetActive(true); gameUIManager.LeftSidebar?.gameObject.SetActive(true); mycovariantDraftController.gameObject.SetActive(false); if (testingModeEnabled) StartNextRound(); else StartCoroutine(DelayedStartNextRound()); }
        private IEnumerator DelayedStartNextRound() { yield return new WaitForSeconds(1f); StartNextRound(); }
        private IEnumerator DelayedStartDraft() { yield return new WaitForSeconds(2.5f); yield return StartCoroutine(fastForwardService.WaitForFadeInAnimationsToComplete(gridVisualizer)); StartMycovariantDraftPhase(); }
        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked) { player.AddMycovariant(picked); var pm = player.PlayerMycovariants.LastOrDefault(x => x.MycovariantId == picked.Id); if (pm != null && picked.AutoMarkTriggered) picked.ApplyEffect?.Invoke(pm, Board, rng, gameUIManager.GameLogRouter); gameUIManager.RightSidebar?.UpdatePlayerSummaries(players); }
        #endregion

        #region UI Helpers
        public void ShowStartGamePanel() { if (gameUIManager != null) { gameUIManager.LeftSidebar?.gameObject.SetActive(false); gameUIManager.RightSidebar?.gameObject.SetActive(false); gameUIManager.MutationUIManager?.gameObject.SetActive(false); gameUIManager.EndGamePanel?.gameObject.SetActive(false); } if (startGamePanel != null) startGamePanel.gameObject.SetActive(true); }
        public void ShowSelectionPrompt(string message) { SelectionPromptPanel.SetActive(true); SelectionPromptText.text = message; }
        public void HideSelectionPrompt() => SelectionPromptPanel.SetActive(false);
        #endregion

        #region Testing Mode API / Fast Forward
        public void EnableTestingMode(int? mycovariantId, int fastForwardRounds = 0, bool skipToEndgameAfterFastForward = false) { testingModeEnabled = true; testingMycovariantId = mycovariantId; testingModeForceHumanFirst = mycovariantId.HasValue; this.fastForwardRounds = fastForwardRounds; testingSkipToEndgameAfterFastForward = skipToEndgameAfterFastForward; }
        public void DisableTestingMode() { testingModeEnabled = false; testingMycovariantId = null; testingModeForceHumanFirst = false; fastForwardRounds = 0; testingSkipToEndgameAfterFastForward = false; }
        internal void RequestFastForwardIfNeeded() { if (testingModeEnabled && fastForwardRounds > 0 && !_fastForwardStarted) { _fastForwardStarted = true; fastForwardService.StartFastForward(fastForwardRounds, testingSkipToEndgameAfterFastForward, testingMycovariantId); } }
        #endregion

        #region Intro Coroutine
        private IEnumerator PlayStartingSporeIntroAndContinue() { var startingIds = Board.Players.Select(p => p.StartingTileId).Where(id => id.HasValue).Select(id => id!.Value).ToList(); if (startingIds.Count > 0 && !testingModeEnabled) yield return gridVisualizer.PlayStartingSporeArrivalAnimation(startingIds); /* removed duplicate reset to avoid double MP */ gameUIManager.LeftSidebar?.gameObject.SetActive(true); gameUIManager.RightSidebar?.gameObject.SetActive(true); gameUIManager.MutationUIManager.gameObject.SetActive(true); mycovariantDraftController?.gameObject.SetActive(false); cameraCenterer?.CaptureInitialFraming(); InitGameLogs(); gameUIManager.MoldProfileRoot?.Initialize(Board.Players[0], Board.Players); if (testingModeEnabled) { if (fastForwardRounds > 0 && !_fastForwardStarted) { _fastForwardStarted = true; fastForwardService.StartFastForward(fastForwardRounds, testingSkipToEndgameAfterFastForward, testingMycovariantId); } else if (testingMycovariantId.HasValue) { StartMycovariantDraftPhase(); yield break; } else gameUIManager.PhaseBanner.Show("New Game Settings", 2f); } else gameUIManager.PhaseBanner.Show("New Game Settings", 2f); phaseProgressTracker?.ResetTracker(); UpdatePhaseProgressTrackerLabel(); phaseProgressTracker?.HighlightMutationPhase(); gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer); gameUIManager.RightSidebar?.InitializePlayerSummaries(players); gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer); gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound); if (!(testingModeEnabled && (fastForwardRounds > 0 || testingMycovariantId.HasValue))) { Debug.Log("[GameManager] Starting initial round mutation phase via StartNextRound()"); StartNextRound(); } }
        #endregion

        #region Internal Accessors For Services
        internal MutationManager GetPrivateMutationManager() => mutationManager;
        internal Player GetPrimaryHumanInternal() => humanPlayer;
        internal System.Random GetRngInternal() => rng;
        internal MycovariantPoolManager GetPersistentPoolInternal() => persistentPoolManager;
        internal void TriggerEndGameInternal() => EndGame();
        #endregion
    }
}
