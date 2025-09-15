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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Board Settings")] 
        public int boardWidth = 20;
        public int boardHeight = 20;
        public int playerCount = 2;

        [Header("Testing Mode")] 
        public bool testingModeEnabled = false;
        public int? testingMycovariantId = null;
        public bool testingModeForceHumanFirst = true;
        public int fastForwardRounds = 0;
        public bool testingSkipToEndgameAfterFastForward = false; // Skip to end after FF

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
        public GameObject SelectionPromptPanel;
        public TextMeshProUGUI SelectionPromptText;
        #endregion

        #region State Fields
        private bool isCountdownActive = false;
        private int roundsRemainingUntilGameEnd = 0;
        private bool gameEnded = false;

        private System.Random rng;
        private MycovariantPoolManager persistentPoolManager;

        public GameBoard Board { get; private set; }
        public GameUIManager GameUI => gameUIManager;
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new();
        private Player humanPlayer;

        private bool isInDraftPhase = false;
        public bool IsDraftPhaseActive => isInDraftPhase;

        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        public bool IsTestingModeEnabled => testingModeEnabled;
        public int? TestingMycovariantId => testingMycovariantId;

        // Buffers for post-growth visual sequencing
        private readonly Dictionary<int, List<int>> _regenReclaimBuffer = new();
        private readonly List<int> _postGrowthResistanceTiles = new(); // (currently not externally populated in this excerpt)
        private readonly List<int> _postGrowthHrtNewResistantTiles = new();
        private HashSet<int> _resistantBaseline = new();
        private bool _postGrowthSequenceRunning = false;

        // NEW: Flag to suppress all Unity-side animations & visual coroutines while fast-forwarding
        private bool isFastForwarding = false;
        public bool IsFastForwarding => isFastForwarding;
        private int _fastForwardTargetRound = 0;

        private bool _fastForwardStarted = false; // ensure single start
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
            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            rng = new System.Random();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                ShowStartGamePanel();
            }
        }
        #endregion

        #region Event Handlers / Buffering
        // Buffer Regenerative Hyphae reclaims for post-growth sequence (when not fast-forwarding)
        private void OnCellReclaimed_RegenerativeHyphae(int playerId, int tileId, GrowthSource source)
        {
            if (source != GrowthSource.RegenerativeHyphae) return;
            if (!_regenReclaimBuffer.TryGetValue(playerId, out var list))
            {
                list = new List<int>();
                _regenReclaimBuffer[playerId] = list;
            }
            list.Add(tileId);
        }
        #endregion

        #region Initialization
        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            Board = new GameBoard(boardWidth, boardHeight, playerCount);

            // Subscribe early for baseline capture BEFORE other PostGrowthPhase handlers
            Board.PostGrowthPhase += OnPostGrowthPhase_StartSequence;
            Board.PostGrowthPhaseCompleted += OnPostGrowthPhaseCompleted_CaptureHrt;

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, gameUIManager.GameLogRouter);
            GameUIEventSubscriber.Subscribe(Board, gameUIManager);
            AnalyticsEventSubscriber.Subscribe(Board, gameUIManager.GameLogRouter);

            Board.ResistanceAppliedBatch += OnResistanceAppliedBatchBuffered;
            Board.CellReclaimed += OnCellReclaimed_RegenerativeHyphae;

            InitializePlayersWithHumanFirst();

            // Persistent draft pool
            var allMycovariants = MycovariantRepository.All.ToList();
            persistentPoolManager = new MycovariantPoolManager();
            persistentPoolManager.InitializePool(allMycovariants, rng);

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            StartCoroutine(PlayStartingSporeIntroAndContinue());
            mutationManager.ResetMutationPoints(players);

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);

            cameraCenterer?.CaptureInitialFraming();

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
            gameUIManager.MutationUIManager.Initialize(Board.Players[0]);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MoldProfileRoot?.Initialize(Board.Players[0], Board.Players);

            if (testingModeEnabled)
            {
                // Do not start fast-forward here (prevents duplicate); wait until intro coroutine finishes setup
                if (fastForwardRounds <= 0 && testingMycovariantId.HasValue)
                {
                    StartMycovariantDraftPhase();
                }
                else if (fastForwardRounds <= 0)
                {
                    gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
                }
            }
            else
            {
                gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
            }

            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
        }

        private void InitializePlayersWithHumanFirst()
        {
            players.Clear();
            int baseMP = GameBalance.StartingMutationPoints;

            humanPlayer = new Player(0, "Human", PlayerTypeEnum.Human, AITypeEnum.Random);
            humanPlayer.SetBaseMutationPoints(baseMP);
            players.Add(humanPlayer);

            var aiStrategies = AIRoster.GetStrategies(playerCount - 1, StrategySetEnum.Proven)
                                      .OrderBy(_ => UnityEngine.Random.value)
                                      .ToList();
            for (int i = 0; i < aiStrategies.Count; i++)
            {
                int playerId = i + 1;
                var ai = new Player(playerId, $"AI Player {playerId}", PlayerTypeEnum.AI, AITypeEnum.Random);
                ai.SetBaseMutationPoints(baseMP);
                ai.SetMutationStrategy(aiStrategies[i]);
                players.Add(ai);
            }

            foreach (var p in players)
            {
                var icon = gridVisualizer.GetTileForPlayer(p.PlayerId)?.sprite;
                if (icon != null) gameUIManager.PlayerUIBinder.AssignIcon(p, icon);
            }

            Board.Players.Clear();
            Board.Players.AddRange(players);
            humanPlayer = Board.Players[0];

            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(Board.Players);
            subscribeToPlayerMutationEvents();
        }

        private void subscribeToPlayerMutationEvents()
        {
            foreach (var p in Board.Players)
                p.MutationsChanged += OnPlayerMutationsChanged;
        }

        private void OnPlayerMutationsChanged(Player player)
        {
            if (player == humanPlayer)
            {
                if (isFastForwarding) return; // suppress per-upgrade UI refresh during fast-forward
                gameUIManager.MoldProfileRoot?.Refresh();
            }
        }
        #endregion

        #region Phase Control
        private void PlaceStartingSpores()
        {
            StartingSporeUtility.PlaceStartingSpores(Board, players, rng);
            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);
        }

        public void StartGrowthPhase()
        {
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
            gameUIManager.GameLogRouter?.OnPhaseStart("Growth");

            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
                gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f);
                phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
                Board.OnPreGrowthPhase();
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;
            gameUIManager.GameLogRouter?.OnPhaseStart("Decay");
            decayPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase(growthPhaseRunner.FailedGrowthsByPlayerId, rng, gameUIManager.GameLogRouter);
        }

        public void OnRoundComplete()
        {
            if (gameEnded) return;

            gameUIManager.GameLogRouter?.OnRoundComplete(Board.CurrentRound, Board);
            foreach (var p in Board.Players) p.TickDownActiveSurges();

            CheckForEndgameCondition();
            if (gameEnded) return;

            Board.IncrementRound();

            if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound))
            {
                StartCoroutine(DelayedStartDraft());
                return;
            }

            StartNextRound();

            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f;
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);
            gameUIManager.RightSidebar.UpdateRandomDecayChance(round);

            foreach (var player in Board.Players)
            {
                foreach (var pm in player.PlayerMutations.Values)
                {
                    if (!pm.FirstUpgradeRound.HasValue) continue;
                    var key = (player.PlayerId, pm.MutationId);
                    if (!FirstUpgradeRounds.ContainsKey(key))
                        FirstUpgradeRounds[key] = new List<int>();
                    FirstUpgradeRounds[key].Add(pm.FirstUpgradeRound.Value);
                }
            }
        }

        public void StartNextRound()
        {
            if (gameEnded) return;
            gameUIManager.GameLogRouter?.OnRoundStart(Board.CurrentRound);
            AssignMutationPoints();
            gameUIManager.MutationUIManager.StartNewMutationPhase();
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(true);
            gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();
            gameUIManager.MoldProfileRoot?.Refresh();
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
            gameUIManager.GameLogRouter?.OnPhaseStart("Mutation");
            gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f);
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
        }
        #endregion

        #region Endgame / Countdown
        private void CheckForEndgameCondition()
        {
            if (!isCountdownActive && Board.ShouldTriggerEndgame())
            {
                isCountdownActive = true;
                roundsRemainingUntilGameEnd = GameBalance.TurnsAfterEndGameTileOccupancyThresholdMet;
                UpdateCountdownUI();
            }
            else if (isCountdownActive)
            {
                roundsRemainingUntilGameEnd--;
                if (roundsRemainingUntilGameEnd <= 0) EndGame(); else UpdateCountdownUI();
            }
        }

        private void UpdateCountdownUI()
        {
            if (!isCountdownActive)
            {
                gameUIManager.RightSidebar?.SetEndgameCountdownText(null);
                return;
            }
            if (roundsRemainingUntilGameEnd == 1)
            {
                gameUIManager.RightSidebar?.SetEndgameCountdownText("<b><color=#FF0000>Final Round!</color></b>");
                gameUIManager.GameLogRouter?.OnEndgameTriggered(1);
            }
            else
            {
                gameUIManager.RightSidebar?.SetEndgameCountdownText($"<b><color=#FFA500>Endgame in {roundsRemainingUntilGameEnd} rounds</color></b>");
                gameUIManager.GameLogRouter?.OnEndgameTriggered(roundsRemainingUntilGameEnd);
            }
        }

        private void EndGame()
        {
            if (gameEnded) return;
            gameEnded = true;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            var ranked = Board.Players
                .OrderByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ThenByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive))
                .ToList();

            var winner = ranked.FirstOrDefault();
            if (winner != null)
                gameUIManager.GameLogRouter?.OnGameEnd(winner.PlayerName);

            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.RightSidebar.gameObject.SetActive(true);
            gameUIManager.LeftSidebar.gameObject.SetActive(false);
            gameUIManager.EndGamePanel.gameObject.SetActive(true);
            gameUIManager.EndGamePanel.ShowResults(ranked, Board);

            foreach (var ((playerId, mutationId), rounds) in FirstUpgradeRounds)
            {
                double avg = rounds.Average();
                int min = rounds.Min();
                int max = rounds.Max();
                Console.WriteLine($"Player {playerId} | Mutation {mutationId} | Avg First Acquired: {avg:F1} | Min: {min} | Max: {max}");
            }
        }
        #endregion

        #region Mutation Points
        private void AssignMutationPoints()
        {
            var allMutations = mutationManager.AllMutations.Values.ToList();
            var rngLocal = new System.Random();
            TurnEngine.AssignMutationPoints(Board, Board.Players, allMutations, rngLocal, gameUIManager.GameLogRouter);
            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
            gameUIManager.MoldProfileRoot?.Refresh();
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            foreach (var p in Board.Players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList(), Board, rng, gameUIManager.GameLogRouter);
            }
            StartGrowthPhase();
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
            TooltipManager.Instance?.CancelAll();

            List<Player> draftOrder;
            if (testingModeEnabled && testingModeForceHumanFirst)
            {
                draftOrder = Board.Players
                    .OrderBy(p => p.PlayerType == PlayerTypeEnum.Human ? 0 : 1)
                    .ThenBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ToList();
                testingModeForceHumanFirst = false; // only once
            }
            else
            {
                draftOrder = Board.Players
                    .OrderBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ThenBy(p => p.PlayerId)
                    .ToList();
            }

            mycovariantDraftController.StartDraft(
                Board.Players,
                persistentPoolManager,
                draftOrder,
                rng,
                MycovariantGameBalance.MycovariantSelectionDraftSize);

            if (testingModeEnabled)
            {
                var testingMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId);
                var mycoName = testingMyco?.Name ?? "Unknown";
                gameUIManager.PhaseBanner.Show($"Testing: {mycoName}", 2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart(mycoName);
            }
            else
            {
                gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase!", 2f);
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
            TooltipManager.Instance?.CancelAll();
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            mycovariantDraftController.gameObject.SetActive(false);

            if (testingModeEnabled) StartNextRound(); else StartCoroutine(DelayedStartNextRound());
        }

        private IEnumerator DelayedStartNextRound()
        {
            yield return new WaitForSeconds(1f);
            StartNextRound();
        }

        private IEnumerator DelayedStartDraft()
        {
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(WaitForFadeInAnimationsToComplete());
            StartMycovariantDraftPhase();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked);
            var playerMyco = player.PlayerMycovariants.LastOrDefault(pm => pm.MycovariantId == picked.Id);
            if (playerMyco != null && picked.AutoMarkTriggered)
                picked.ApplyEffect?.Invoke(playerMyco, Board, rng, gameUIManager.GameLogRouter);
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);
        }
        #endregion

        #region UI Helpers
        public void ShowStartGamePanel()
        {
            if (gameUIManager != null)
            {
                gameUIManager.LeftSidebar?.gameObject.SetActive(false);
                gameUIManager.RightSidebar?.gameObject.SetActive(false);
                gameUIManager.MutationUIManager?.gameObject.SetActive(false);
                gameUIManager.EndGamePanel?.gameObject.SetActive(false);
            }
            if (startGamePanel != null)
                startGamePanel.gameObject.SetActive(true);
        }

        public void ShowSelectionPrompt(string message)
        {
            SelectionPromptPanel.SetActive(true);
            SelectionPromptText.text = message;
        }

        public void HideSelectionPrompt() => SelectionPromptPanel.SetActive(false);
        #endregion

        #region Testing Mode API
        public void EnableTestingMode(int? mycovariantId, int fastForwardRounds = 0, bool skipToEndgameAfterFastForward = false)
        {
            testingModeEnabled = true;
            testingMycovariantId = mycovariantId;
            testingModeForceHumanFirst = mycovariantId.HasValue;
            this.fastForwardRounds = fastForwardRounds;
            testingSkipToEndgameAfterFastForward = skipToEndgameAfterFastForward;
        }

        public void DisableTestingMode()
        {
            testingModeEnabled = false;
            testingMycovariantId = null;
            testingModeForceHumanFirst = false;
            fastForwardRounds = 0;
            testingSkipToEndgameAfterFastForward = false;
        }
        #endregion

        #region Fast Forward (Animation Suppression)
        private IEnumerator FastForwardRounds()
        {
            gameUIManager.GameLogRouter.EnableSilentMode();
            isFastForwarding = true;

            int startingRound = Board.CurrentRound; // inclusive
            int requestedValue = fastForwardRounds;
            // Treat input as target round if it is greater than current; otherwise as number of rounds to advance
            bool treatAsTargetRound = requestedValue > startingRound;
            int targetRound = treatAsTargetRound ? requestedValue : (startingRound + requestedValue);
            if (targetRound < startingRound) targetRound = startingRound; // safety
            int desiredRounds = targetRound - startingRound;
            int iterations = 0;

            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[FF] Begin fast-forward: start={startingRound} requested={requestedValue} target={targetRound} desiredRounds={desiredRounds}");

            try
            {
                var originalHumanType = humanPlayer.PlayerType;
                var originalHumanStrategy = humanPlayer.MutationStrategy;
                IMutationSpendingStrategy persistentStrategy = originalHumanStrategy ?? AIRoster.GetStrategies(1, StrategySetEnum.Proven).FirstOrDefault();
                humanPlayer.SetPlayerType(PlayerTypeEnum.AI);
                humanPlayer.SetMutationStrategy(persistentStrategy);

                while (Board.CurrentRound < targetRound && iterations < desiredRounds && !gameEnded)
                {
                    yield return RunSilentGrowthPhase();
                    yield return RunSilentDecayPhase();
                    yield return RunSilentMutationPhase();

                    foreach (var p in Board.Players) p.TickDownActiveSurges();
                    Board.IncrementRound();
                    iterations++;

                    if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound))
                    {
                        RunSilentDraftForAllPlayers(gameUIManager.GameLogRouter);
                    }
                }

                humanPlayer.SetPlayerType(originalHumanType);
                humanPlayer.SetMutationStrategy(originalHumanStrategy);
                isFastForwarding = false;

                gridVisualizer.RenderBoard(Board, true); // no animations
                gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players);
                float occupancy = Board.GetOccupiedTileRatio() * 100f;
                gameUIManager.RightSidebar?.SetRoundAndOccupancy(Board.CurrentRound, occupancy);
                gameUIManager.MoldProfileRoot?.ApplyDeferredRefreshIfNeeded();

                int actualAdvanced = Board.CurrentRound - startingRound;
                if (actualAdvanced != desiredRounds)
                {
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[FF][WARN] mismatch desired={desiredRounds} actual={actualAdvanced} start={startingRound} end={Board.CurrentRound}");
                }
                else
                {
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[FF] complete advanced={actualAdvanced} finalRound={Board.CurrentRound}");
                }

                if (testingSkipToEndgameAfterFastForward)
                {
                    gameUIManager.GameLogRouter.DisableSilentMode();
                    EndGame();
                    yield break;
                }

                if (testingMycovariantId.HasValue)
                {
                    StartMycovariantDraftPhase();
                }
                else
                {
                    string bannerMsg = treatAsTargetRound ? $"Reached Round {Board.CurrentRound}" : $"Fast-forwarded {actualAdvanced} rounds";
                    gameUIManager.PhaseBanner.Show(bannerMsg, 2f);
                    // Start the mutation phase for the target round (do NOT increment round again)
                    StartNextRound();
                }
            }
            finally
            {
                isFastForwarding = false; // safety
                gameUIManager.GameLogRouter.DisableSilentMode();
            }
        }

        private void RunSilentDraftForAllPlayers(ISimulationObserver observer)
        {
            Func<Player, List<Mycovariant>, Mycovariant> customSelectionCallback = null;

            if (testingModeEnabled && testingMycovariantId.HasValue)
            {
                var testingMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId.Value);
                if (testingMyco != null && !testingMyco.IsUniversal)
                {
                    persistentPoolManager.TemporarilyRemoveFromPool(testingMycovariantId.Value);
                    customSelectionCallback = (player, choices) =>
                    {
                        var available = choices.Where(m => m.Id != testingMycovariantId.Value).ToList();
                        if (available.Count == 0) available = choices;
                        return available
                            .OrderByDescending(m => m.GetBaseAIScore(player, Board))
                            .ThenBy(_ => rng.Next())
                            .First();
                    };
                }
            }

            MycovariantDraftManager.RunDraft(
                Board.Players,
                persistentPoolManager,
                Board,
                rng,
                observer,
                MycovariantGameBalance.MycovariantSelectionDraftSize,
                customSelectionCallback);

            if (testingModeEnabled && testingMycovariantId.HasValue)
            {
                var testingMyco = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId.Value);
                if (testingMyco != null && !testingMyco.IsUniversal)
                    persistentPoolManager.RestoreToPool(testingMycovariantId.Value);
            }
        }

        private IEnumerator RunSilentMutationPhase()
        {
            var allMutations = mutationManager.AllMutations.Values.ToList();
            TurnEngine.AssignMutationPoints(Board, Board.Players, allMutations, rng, gameUIManager.GameLogRouter);
            foreach (var player in Board.Players)
            {
                player.MutationStrategy?.SpendMutationPoints(player, allMutations, Board, rng, gameUIManager.GameLogRouter);
            }
            yield return null;
        }

        private IEnumerator RunSilentGrowthPhase()
        {
            var processor = new GrowthPhaseProcessor(Board, Board.Players, rng, gameUIManager.GameLogRouter);
            for (int cycle = 1; cycle <= GameBalance.TotalGrowthCycles; cycle++)
            {
                Board.IncrementGrowthCycle();
                processor.ExecuteSingleCycle(Board.CurrentRoundContext);
            }
            Board.OnPostGrowthPhase();
            yield return null;
        }

        private IEnumerator RunSilentDecayPhase()
        {
            var emptyFailed = new Dictionary<int, int>();
            DeathEngine.ExecuteDeathCycle(Board, emptyFailed, rng, gameUIManager.GameLogRouter);
            yield return null;
        }

        private IEnumerator WaitForFadeInAnimationsToComplete()
        {
            if (gridVisualizer == null) yield break;
            while (gridVisualizer.HasActiveAnimations) yield return null;
        }
        #endregion

        #region Visual Event Hooks (Suppressed During Fast-Forward)
        private void OnResistanceAppliedBatchBuffered(int playerId, GrowthSource source, IReadOnlyList<int> tileIds)
        {
            if (isFastForwarding) return; // suppress visuals
            gridVisualizer.RenderBoard(Board);
            gridVisualizer.PlayResistancePulseBatchScaled(tileIds, 0.5f);
        }

        private void OnPostGrowthPhase_StartSequence()
        {
            if (isFastForwarding) return; // skip baseline capture
            _resistantBaseline = new HashSet<int>(Board.AllTiles()
                .Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant)
                .Select(t => t.TileId));
        }

        private void OnPostGrowthPhaseCompleted_CaptureHrt()
        {
            if (isFastForwarding)
            {
                _postGrowthHrtNewResistantTiles.Clear();
                return;
            }

            var allResistantNow = Board.AllTiles()
                .Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant)
                .Select(t => t.TileId)
                .ToList();

            _postGrowthHrtNewResistantTiles.Clear();
            foreach (var id in allResistantNow)
                if (!_resistantBaseline.Contains(id))
                    _postGrowthHrtNewResistantTiles.Add(id);

            if (!_postGrowthSequenceRunning)
            {
                _postGrowthSequenceRunning = true;
                StartCoroutine(RunPostGrowthVisualSequence());
            }
        }

        private IEnumerator RunPostGrowthVisualSequence()
        {
            if (isFastForwarding)
            {
                _regenReclaimBuffer.Clear();
                _postGrowthResistanceTiles.Clear();
                _postGrowthHrtNewResistantTiles.Clear();
                _postGrowthSequenceRunning = false;
                StartDecayPhase();
                yield break;
            }

            // Phase 1: Regenerative Hyphae
            if (_regenReclaimBuffer.Count > 0)
            {
                foreach (var kvp in _regenReclaimBuffer)
                {
                    var ids = kvp.Value;
                    if (ids.Count == 0) continue;
                    gridVisualizer.PlayRegenerativeHyphaeReclaimBatch(ids, 1f, UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds);
                }
                yield return gridVisualizer.WaitForAllAnimations();
                _regenReclaimBuffer.Clear();
            }

            // Phase 2: Resistance pulses (if any were buffered elsewhere)
            if (_postGrowthResistanceTiles.Count > 0)
            {
                gridVisualizer.PlayResistancePulseBatchScaled(_postGrowthResistanceTiles, 0.5f);
                yield return gridVisualizer.WaitForAllAnimations();
                _postGrowthResistanceTiles.Clear();
            }

            // Phase 3: HRT spread
            bool anyPlayerHasHrt = Board.Players.Any(p => p.GetMycovariant(MycovariantIds.HyphalResistanceTransferId) != null);
            if (anyPlayerHasHrt && _postGrowthHrtNewResistantTiles.Count > 0)
            {
                gridVisualizer.PlayResistancePulseBatchScaled(_postGrowthHrtNewResistantTiles, 0.35f);
                yield return gridVisualizer.WaitForAllAnimations();
            }

            _postGrowthHrtNewResistantTiles.Clear();
            _postGrowthSequenceRunning = false;
            StartDecayPhase();
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

            if (startingIds.Count > 0 && !testingModeEnabled)
                yield return gridVisualizer.PlayStartingSporeArrivalAnimation(startingIds);

            mutationManager.ResetMutationPoints(players);

            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);
            cameraCenterer?.CaptureInitialFraming();

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
            gameUIManager.MutationUIManager.Initialize(Board.Players[0]);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MoldProfileRoot?.Initialize(Board.Players[0], Board.Players);

            // Decide what to do after intro based on testing flags
            if (testingModeEnabled)
            {
                if (fastForwardRounds > 0 && !_fastForwardStarted)
                {
                    _fastForwardStarted = true;
                    StartCoroutine(FastForwardRounds());
                }
                else if (testingMycovariantId.HasValue)
                {
                    StartMycovariantDraftPhase();
                }
                else
                {
                    gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
                }
            }
            else
            {
                gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
            }

            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(Board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(Board.CurrentRound);
        }
        #endregion
    }
}
