using FungusToast.Core;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Cameras;
using FungusToast.Unity.Events;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Phases;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.GameStart;
using FungusToast.Unity.UI.MycovariantDraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using FungusToast.Core.Metrics;

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        [Header("Board Settings")]
        public int boardWidth = 20;
        public int boardHeight = 20;
        public int playerCount = 2;

        [Header("Testing Mode")]
        public bool testingModeEnabled = false;
        public int? testingMycovariantId = null;
        public bool testingModeForceHumanFirst = true;
        public int fastForwardRounds = 0;

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

        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        public bool IsTestingModeEnabled => testingModeEnabled;
        public int? TestingMycovariantId => testingMycovariantId;

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
            // Only show StartGamePanel when the game is actually running (not in edit mode)
            if (Application.isPlaying)
            {
                ShowStartGamePanel();
            }
        }

        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            Board = new GameBoard(boardWidth, boardHeight, playerCount);

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, gameUIManager.GameLogRouter);
            GameUIEventSubscriber.Subscribe(Board, gameUIManager);
            AnalyticsEventSubscriber.Subscribe(Board, gameUIManager.GameLogRouter);

            InitializePlayersWithHumanFirst();

            // Initialize the persistent pool manager once per game
            var allMycovariants = MycovariantRepository.All.ToList();
            persistentPoolManager = new MycovariantPoolManager();
            persistentPoolManager.InitializePool(allMycovariants, rng);

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            // === ACTIVATE ALL UI PANELS FIRST ===
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);

            // === Initialize Game Log ===
            if (gameUIManager.GameLogManager != null && gameUIManager.GameLogPanel != null)
            {
                gameUIManager.GameLogManager.Initialize(Board);
                gameUIManager.GameLogPanel.Initialize(gameUIManager.GameLogManager);
            }
            
            // === Initialize Global Game Log ===
            if (gameUIManager.GlobalGameLogManager != null && gameUIManager.GlobalGameLogPanel != null)
            {
                gameUIManager.GlobalGameLogManager.Initialize(Board);
                gameUIManager.GlobalGameLogPanel.Initialize(gameUIManager.GlobalGameLogManager);
            }

            // === THEN initialize and show children/buttons ===
            gameUIManager.MutationUIManager.Initialize(Board.Players[0]);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);

            if (testingModeEnabled)
            {
                if (fastForwardRounds > 0)
                {
                    StartCoroutine(FastForwardRounds());
                }
                else if (testingMycovariantId.HasValue)
                {
                    // Start draft immediately in testing mode only if a specific mycovariant is selected
                    StartMycovariantDraftPhase();
                }
                else
                {
                    // Testing mode enabled but no mycovariant selected and no fast-forward - start normal game
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

            // --- Set the GridVisualizer on the RightSidebar before initializing player summaries
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
        }

        private void InitializePlayersWithHumanFirst()
        {
            players.Clear();
            int baseMP = GameBalance.StartingMutationPoints;

            // Add human player first
            humanPlayer = new Player(0, "Human", PlayerTypeEnum.Human, AITypeEnum.Random);
            humanPlayer.SetBaseMutationPoints(baseMP);
            players.Add(humanPlayer);

            // Get AI strategies from AIRoster
            var aiStrategies = AIRoster.GetStrategies(playerCount - 1, StrategySetEnum.Proven);

            // Shuffle for variety (if AIRoster didn't already do so)
            aiStrategies = aiStrategies.OrderBy(_ => UnityEngine.Random.value).ToList();

            // Create AI players with shuffled order and assign strategy
            for (int i = 0; i < aiStrategies.Count; i++)
            {
                int playerId = i + 1; // AI player IDs start at 1
                var aiPlayer = new Player(playerId, $"AI Player {playerId}", PlayerTypeEnum.AI, AITypeEnum.Random);
                aiPlayer.SetBaseMutationPoints(baseMP);
                aiPlayer.SetMutationStrategy(aiStrategies[i]);
                players.Add(aiPlayer);
            }

            // Re-assign icons and initialize UI panels
            foreach (var p in players)
            {
                var icon = gridVisualizer.GetTileForPlayer(p.PlayerId)?.sprite;
                if (icon != null) 
                {
                    gameUIManager.PlayerUIBinder.AssignIcon(p, icon);
                }
            }

            // First populate Board.Players
            Board.Players.Clear();
            Board.Players.AddRange(players);
            
            // Update humanPlayer to reference the canonical player object
            humanPlayer = Board.Players[0];

            // Now initialize UI panels with the correct references
            gameUIManager.MoldProfilePanel?.Initialize(Board.Players[0], Board.Players);
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(Board.Players);
        }

        private void PlaceStartingSpores()
        {
            // Use the shared starting spore placement utility from FungusToast.Core
            StartingSporeUtility.PlaceStartingSpores(Board, players, rng);

            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f; // ratio to percent
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);
        }

        public void StartGrowthPhase()
        {
            // Disable Spend Points before leaving mutation phase
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            // Notify logs of phase start using unified router
            gameUIManager.GameLogRouter?.OnPhaseStart("Growth");

            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
                gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f);
                phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
                
                // 🔧 CRITICAL FIX: Add the missing PreGrowthPhase event for Chitin Fortification and other pre-growth effects
                Board.OnPreGrowthPhase();
                
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;

            // Notify logs of phase start using unified router
            gameUIManager.GameLogRouter?.OnPhaseStart("Decay");

            decayPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase(growthPhaseRunner.FailedGrowthsByPlayerId, rng);
        }

        public void OnRoundComplete()
        {
            if (gameEnded) return;

            // Notify both logs of round completion using unified router
            gameUIManager.GameLogRouter?.OnRoundComplete(Board.CurrentRound, Board);

            foreach (var player in Board.Players)
                player.TickDownActiveSurges();

            CheckForEndgameCondition();
            if (gameEnded) return;

            Board.IncrementRound();

            // === 1. Trigger draft phase if this is a draft round ===
            if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound))
            {
                // IMPORTANT: Wait for any ongoing fade-in animations to complete before starting draft
                // This ensures newly grown cells are fully visible when highlighted for selection
                StartCoroutine(DelayedStartDraft());
                return; // Prevent starting mutation phase until draft is complete!
            }

            // === 2. Otherwise, start the next mutation phase as usual ===
            StartNextRound();

            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f; // ratio to percent
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);

            foreach (var player in Board.Players)
            {
                foreach (var pm in player.PlayerMutations.Values)
                {
                    if (pm.FirstUpgradeRound.HasValue)
                    {
                        var key = (player.PlayerId, pm.MutationId);
                        if (!FirstUpgradeRounds.ContainsKey(key))
                            FirstUpgradeRounds[key] = new List<int>();
                        FirstUpgradeRounds[key].Add(pm.FirstUpgradeRound.Value);
                    }
                }
            }
        }

        public void StartNextRound()
        {
            if (gameEnded) return;

            // Notify logs of round start using unified router
            gameUIManager.GameLogRouter?.OnRoundStart(Board.CurrentRound);

            AssignMutationPoints();
            
            // At the true start of a new mutation phase, reset humanTurnEnded
            gameUIManager.MutationUIManager.StartNewMutationPhase();
            
            // Don't initialize immediately - just set up the UI state
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(true);

            // Refresh the spend points button to show updated mutation points
            gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();

            gameUIManager.MoldProfilePanel?.Refresh();
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players);

            // Notify logs of phase start using unified router
            gameUIManager.GameLogRouter?.OnPhaseStart("Mutation");

            gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f);

            UpdatePhaseProgressTrackerLabel();

            phaseProgressTracker?.HighlightMutationPhase();
        }

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
                if (roundsRemainingUntilGameEnd <= 0)
                {
                    EndGame();
                }
                else
                {
                    UpdateCountdownUI();
                }
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

            // Notify global log of game end using unified router
            var winner = ranked.FirstOrDefault();
            if (winner != null)
            {
                gameUIManager.GameLogRouter?.OnGameEnd(winner.PlayerName);
            }

            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.RightSidebar.gameObject.SetActive(false);
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

        private void AssignMutationPoints()
        {
            var allMutations = mutationManager.AllMutations.Values.ToList();
            var rng = new System.Random();

            TurnEngine.AssignMutationPoints(Board, Board.Players, allMutations, rng, gameUIManager.GameLogRouter);

            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            foreach (var p in Board.Players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList(), Board, rng, gameUIManager.GameLogRouter);
            }

            Debug.Log("All AI players have spent their mutation points.");
            StartGrowthPhase();
        }

        private void UpdatePhaseProgressTrackerLabel()
        {
            if (isInDraftPhase)
            {
                phaseProgressTracker?.SetMutationPhaseLabel("DRAFT");
            }
            else
            {
                phaseProgressTracker?.SetMutationPhaseLabel("MUTATION");
            }
        }

        public void StartMycovariantDraftPhase()
        {
            isInDraftPhase = true;

            // In testing mode, ensure the testing mycovariant is available
            if (testingModeEnabled && testingMycovariantId.HasValue)
            {
                var testingMycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId.Value);
                if (testingMycovariant != null)
                {
                    // The testing mycovariant should already be in the persistent pool manager
                    // No need to modify the pool here
                }
            }

            // Determine draft order
            List<Player> draftOrder;
            if (testingModeEnabled && testingModeForceHumanFirst)
            {
                // In testing mode with human first, ensure human player goes first
                draftOrder = Board.Players
                    .OrderBy(p => p.PlayerType == PlayerTypeEnum.Human ? 0 : 1)
                    .ThenBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ToList();
            }
            else
            {
                // Normal draft order: fewest living cells goes first
                draftOrder = Board.Players
                    .OrderBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                    .ToList();
            }

            // Start the draft UI/controllers using the persistent pool manager
            mycovariantDraftController.StartDraft(
                Board.Players, persistentPoolManager, draftOrder, rng, MycovariantGameBalance.MycovariantSelectionDraftSize);

            // Show a phase banner (optional, for player feedback)
            if (testingModeEnabled)
            {
                var testingMycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId);
                var mycovariantName = testingMycovariant?.Name ?? "Unknown";
                gameUIManager.PhaseBanner.Show($"Testing: {mycovariantName}", 2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart(mycovariantName);
            }
            else
            {
                gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase!", 2f);
                gameUIManager.GameLogRouter?.OnDraftPhaseStart();
            }
            phaseProgressTracker?.HighlightDraftPhase();

            // Hide mutation UI during draft
            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.RightSidebar?.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);

            // Show draft UI
            mycovariantDraftController.gameObject.SetActive(true);
        }

        public void OnMycovariantDraftComplete()
        {
            isInDraftPhase = false;
            
            // Re-enable UI elements that were hidden during draft
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            
            // Hide draft UI
            mycovariantDraftController.gameObject.SetActive(false);
            
            // In testing mode, start the next round immediately
            if (testingModeEnabled)
            {
                StartNextRound();
            }
            else
            {
                // Start a coroutine to delay the next round start, ensuring UI is properly activated
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
            yield return new WaitForSeconds(2.5f); // Wait for banner to show
            
            // CRITICAL FIX: Ensure fade-in animations complete before draft highlighting
            yield return StartCoroutine(WaitForFadeInAnimationsToComplete());
            
            StartMycovariantDraftPhase();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked);
            
            // Apply the core effect for mycovariants with AutoMarkTriggered = true
            // These are passive/instant effects that don't require UI selection
            // Active mycovariants (AutoMarkTriggered = false) are handled by MycovariantEffectResolver
            var playerMyco = player.PlayerMycovariants.LastOrDefault(pm => pm.MycovariantId == picked.Id);
            if (playerMyco != null && picked.AutoMarkTriggered)
            {
                picked.ApplyEffect?.Invoke(playerMyco, Board, rng, gameUIManager.GameLogRouter);
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] Applied core effect for AutoMarkTriggered mycovariant {picked.Name} (Id={picked.Id}) for PlayerId={playerMyco.PlayerId}");
            }
            else if (playerMyco != null)
            {
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] Skipping ApplyEffect for interactive mycovariant {picked.Name} (Id={picked.Id}) - will be handled by UI layer");
            }
            
            // Optionally update UI
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);
        }

        public void ShowStartGamePanel()
        {
            // Hide other major panels if desired (optional for clarity)
            if (gameUIManager != null)
            {
                gameUIManager.LeftSidebar?.gameObject.SetActive(false);
                gameUIManager.RightSidebar?.gameObject.SetActive(false);
                gameUIManager.MutationUIManager?.gameObject.SetActive(false);
                gameUIManager.EndGamePanel?.gameObject.SetActive(false);
            }
            // Show the start panel
            if (startGamePanel != null)
                startGamePanel.gameObject.SetActive(true);
        }

        public void ShowSelectionPrompt(string message)
        {
            SelectionPromptPanel.SetActive(true);
            SelectionPromptText.text = message;
        }
        public void HideSelectionPrompt()
        {
            SelectionPromptPanel.SetActive(false);
        }

        // === TESTING MODE METHODS ===
        public void EnableTestingMode(int? mycovariantId, int fastForwardRounds = 0)
        {
            testingModeEnabled = true;
            testingMycovariantId = mycovariantId;
            testingModeForceHumanFirst = mycovariantId.HasValue; // Only force human first when testing a specific mycovariant
            this.fastForwardRounds = fastForwardRounds;
        }

        public void DisableTestingMode()
        {
            testingModeEnabled = false;
            testingMycovariantId = null;
            testingModeForceHumanFirst = false; // Reset the flag when disabling testing mode
            fastForwardRounds = 0;
        }

        private IEnumerator FastForwardRounds()
        {
            // Store original human player type and strategy
            var originalHumanType = humanPlayer.PlayerType;
            var originalHumanStrategy = humanPlayer.MutationStrategy;

            for (int round = 1; round <= fastForwardRounds; round++)
            {
                // Silent growth phase
                yield return StartCoroutine(RunSilentGrowthPhase());
                // Silent decay phase
                yield return StartCoroutine(RunSilentDecayPhase());

                // --- Temporarily make human an AI for mutation spending ---
                var tempType = humanPlayer.PlayerType;
                var tempStrat = humanPlayer.MutationStrategy;
                humanPlayer.SetPlayerType(PlayerTypeEnum.AI);
                var aiStrategy = AIRoster.GetStrategies(1, StrategySetEnum.Proven).FirstOrDefault();
                humanPlayer.SetMutationStrategy(aiStrategy);

                // Silent mutation phase (auto-spend for all players)
                yield return StartCoroutine(RunSilentMutationPhase());

                // Restore human player type and strategy
                humanPlayer.SetPlayerType(tempType);
                humanPlayer.SetMutationStrategy(tempStrat);

                // Increment round
                Board.IncrementRound();

                // If this is a draft round, run a silent draft for all players (including human as AI)
                if (MycovariantGameBalance.MycovariantSelectionTriggerRounds.Contains(Board.CurrentRound))
                {
                    // Temporarily make human an AI for the draft
                    var originalType = humanPlayer.PlayerType;
                    var originalStrat = humanPlayer.MutationStrategy;
                    humanPlayer.SetPlayerType(PlayerTypeEnum.AI);
                    var draftAIStrategy = AIRoster.GetStrategies(1, StrategySetEnum.Proven).FirstOrDefault();
                    humanPlayer.SetMutationStrategy(draftAIStrategy);

                    RunSilentDraftForAllPlayers(gameUIManager.GameLogRouter);

                    // Restore human player type and strategy
                    humanPlayer.SetPlayerType(originalType);
                    humanPlayer.SetMutationStrategy(originalStrat);
                }
            }

            // Update the board visualization after fast-forward
            gridVisualizer.RenderBoard(Board);
            
            // CRITICAL FIX: Wait for fade-in animations to complete before starting draft
            // This ensures newly grown cells are fully visible when highlighted
            yield return StartCoroutine(WaitForFadeInAnimationsToComplete());
            
            // Update UI elements to reflect the new board state
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(Board.Players);
            int currentRound = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f;
            gameUIManager.RightSidebar?.SetRoundAndOccupancy(currentRound, occupancy);

            // Only trigger a UI draft at the end of fast forward if a specific mycovariant is selected
            if (testingMycovariantId.HasValue)
            {
                StartMycovariantDraftPhase();
            }
            else
            {
                // No specific mycovariant selected - start normal mutation phase
                gameUIManager.PhaseBanner.Show($"Fast-forwarded {fastForwardRounds} rounds", 2f);
                StartNextRound();
            }
        }

        private void RunSilentDraftForAllPlayers(ISimulationObserver observer)
        {
            // Create a custom draft function that excludes the testing mycovariant for AI players during silent drafts
            Func<Player, List<Mycovariant>, Mycovariant> customSelectionCallback = null;
            
            if (testingModeEnabled && testingMycovariantId.HasValue)
            {
                // Find the testing mycovariant to check if it's universal
                var testingMycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId.Value);
                
                if (testingMycovariant != null && !testingMycovariant.IsUniversal)
                {
                    // If the testing mycovariant is non-universal, temporarily remove it from the pool during silent drafts
                    // This prevents AI players from receiving it in their draft choices
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[SilentDraft] Temporarily removing non-universal testing mycovariant '{testingMycovariant.Name}' (ID: {testingMycovariantId.Value}) from pool during silent draft");
                    persistentPoolManager.TemporarilyRemoveFromPool(testingMycovariantId.Value);
                    
                    // Create custom selection callback for any edge cases where it might still appear
                    customSelectionCallback = (player, choices) =>
                    {
                        // Filter out the testing mycovariant for AI players as a safety net
                        var availableChoices = choices.Where(m => m.Id != testingMycovariantId.Value).ToList();
                        
                        if (availableChoices.Count == 0)
                        {
                            // Fallback: if no other choices available, pick randomly from all choices
                            availableChoices = choices;
                        }
                        
                        // AI pick: highest AI score from available choices
                        return availableChoices
                            .OrderByDescending(m => m.GetBaseAIScore(player, Board))
                            .ThenBy(_ => rng.Next())
                            .First();
                    };
                }
            }
            
            // Use the persistent pool manager for silent drafts to maintain state consistency
            MycovariantDraftManager.RunDraft(
                Board.Players,
                persistentPoolManager,
                Board,
                rng,
                observer,
                MycovariantGameBalance.MycovariantSelectionDraftSize,
                customSelectionCallback // Use custom callback to exclude testing mycovariant from AI
            );
            
            // After silent draft is complete, restore the testing mycovariant to the pool
            if (testingModeEnabled && testingMycovariantId.HasValue)
            {
                var testingMycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == testingMycovariantId.Value);
                if (testingMycovariant != null && !testingMycovariant.IsUniversal)
                {
                    persistentPoolManager.RestoreToPool(testingMycovariantId.Value);
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[SilentDraft] Restored testing mycovariant '{testingMycovariant.Name}' (ID: {testingMycovariantId.Value}) to pool after silent draft");
                }
            }
        }

        private void SpendMutationPointsForAllPlayers(List<Mutation> allMutations, GameBoard board, System.Random rng)
        {
            foreach (var player in board.Players)
            {
                var strategy = player.MutationStrategy;
                if (strategy != null)
                {
                    strategy.SpendMutationPoints(player, allMutations, board, rng, gameUIManager.GameLogRouter);
                }
            }
        }

        private IEnumerator RunSilentMutationPhase()
        {
            // Assign mutation points to all players
            var allMutations = mutationManager.AllMutations.Values.ToList();
            TurnEngine.AssignMutationPoints(Board, Board.Players, allMutations, rng, gameUIManager.GameLogRouter);
            SpendMutationPointsForAllPlayers(allMutations, Board, rng);
            yield return null; // One frame delay
        }

        private IEnumerator RunSilentGrowthPhase()
        {
            var processor = new GrowthPhaseProcessor(Board, Board.Players, rng);

            for (int cycle = 1; cycle <= GameBalance.TotalGrowthCycles; cycle++)
            {
                Board.IncrementGrowthCycle();
                processor.ExecuteSingleCycle(Board.CurrentRoundContext);
            }

            // Post-growth effects
            Board.OnPostGrowthPhase();
            
            yield return null; // One frame delay
        }

        private IEnumerator RunSilentDecayPhase()
        {
            // Use empty failed growths since we're not tracking them in silent mode
            var emptyFailedGrowths = new Dictionary<int, int>();
            DeathEngine.ExecuteDeathCycle(Board, emptyFailedGrowths, rng, null);
            yield return null; // One frame delay
        }

        /// <summary>
        /// Waits for all fade-in animations to complete to ensure newly grown cells are fully visible.
        /// This is crucial when transitioning from fast-forward to interactive phases like drafts.
        /// </summary>
        private IEnumerator WaitForFadeInAnimationsToComplete()
        {
            // Check if the GridVisualizer has any active fade-in animations
            if (gridVisualizer == null) yield break;
            
            // Wait for the duration of fade-in animations plus a small buffer
            float fadeInDuration = UIEffectConstants.CellGrowthFadeInDurationSeconds;
            float bufferTime = 0.1f; // Small buffer to ensure completion
            
            yield return new WaitForSeconds(fadeInDuration + bufferTime);
            
            // Additional safety: Clear all IsNewlyGrown flags to prevent any lingering transparency issues
            foreach (var tile in Board.AllTiles())
            {
                if (tile.FungalCell?.IsNewlyGrown == true)
                {
                    tile.FungalCell.ClearNewlyGrownFlag();
                }
            }
            
            // Force a final render to ensure all cells are at full opacity
            gridVisualizer.RenderBoard(Board);
        }
    }
}
