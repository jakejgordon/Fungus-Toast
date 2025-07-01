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

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        [Header("Board Settings")]
        public int boardWidth = 20;
        public int boardHeight = 20;
        public int playerCount = 2;

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

        public GameBoard Board { get; private set; }
        public GameUIManager GameUI => gameUIManager;
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new();
        private Player humanPlayer;

        private bool isInDraftPhase = false;

        private Dictionary<(int playerId, int mutationId), List<int>> FirstUpgradeRounds = new();

        private void Awake()
        {
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
            ShowStartGamePanel();
        }

        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            Board = new GameBoard(boardWidth, boardHeight, playerCount);

            GameRulesEventSubscriber.SubscribeAll(Board, players, rng, null);
            GameUIEventSubscriber.Subscribe(Board, gameUIManager);
            AnalyticsEventSubscriber.Subscribe(Board, null);

            InitializePlayersWithHumanFirst();

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            // === ACTIVATE ALL UI PANELS FIRST ===
            gameUIManager.LeftSidebar?.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            mycovariantDraftController?.gameObject.SetActive(false);

            // === THEN initialize and show children/buttons ===
            gameUIManager.MutationUIManager.Initialize(Board.Players[0]);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);

            gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
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
            var aiStrategies = AIRoster.GetRandomProvenStrategies(playerCount - 1);

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
                if (icon != null) gameUIManager.PlayerUIBinder.AssignIcon(p, icon);
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
            float radius = Mathf.Min(boardWidth, boardHeight) * 0.35f;
            Vector2 center = new Vector2(boardWidth / 2f, boardHeight / 2f);

            // Create a list of shuffled player indices
            List<int> shuffledPlayerIndices = Enumerable.Range(0, players.Count)
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / players.Count;
                int px = Mathf.Clamp(Mathf.RoundToInt(center.x + radius * Mathf.Cos(angle)), 0, boardWidth - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(center.y + radius * Mathf.Sin(angle)), 0, boardHeight - 1);
                Board.PlaceInitialSpore(shuffledPlayerIndices[i], px, py);
            }

            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f; // ratio to percent
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);
        }


        public void StartGrowthPhase()
        {
            // Disable Spend Points before leaving mutation phase
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
                gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f);
                phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;

            decayPhaseRunner.Initialize(Board, Board.Players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase(growthPhaseRunner.FailedGrowthsByPlayerId, rng);
        }

        public void OnRoundComplete()
        {
            if (gameEnded) return;

            foreach (var player in Board.Players)
                player.TickDownActiveSurges();

            CheckForEndgameCondition();
            if (gameEnded) return;

            Board.IncrementRound();

            // === 1. Trigger draft phase if this is the right round ===
            if (Board.CurrentRound == MycovariantGameBalance.MycovariantSelectionTriggerRound)
            {
                StartMycovariantDraftPhase();
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
                gameUIManager.RightSidebar?.SetEndgameCountdownText("<b><color=#FF0000>Final Round!</color></b>");
            else
                gameUIManager.RightSidebar?.SetEndgameCountdownText($"<b><color=#FFA500>Endgame in {roundsRemainingUntilGameEnd} rounds</color></b>");
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

            TurnEngine.AssignMutationPoints(Board, Board.Players, allMutations, rng);

            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            var rng = new System.Random();

            foreach (var p in Board.Players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList(), Board, rng);
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

            // Build the draft pool as appropriate for your game logic
            var draftPool = MycovariantDraftManager.BuildDraftPool(Board, Board.Players);

            // Create and initialize the pool manager
            var poolManager = new MycovariantPoolManager();
            poolManager.InitializePool(draftPool, rng);

            // Determine draft order (example: fewest living cells goes first)
            var draftOrder = Board.Players
                .OrderBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ToList();

            // Start the draft UI/controller
            mycovariantDraftController.StartDraft(
                Board.Players, poolManager, draftOrder, rng, MycovariantGameBalance.MycovariantSelectionDraftSize);

            // Show a phase banner (optional, for player feedback)
            gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase!", 2f);
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
            
            // Start a coroutine to delay the next round start, ensuring UI is properly activated
            StartCoroutine(DelayedStartNextRound());
        }

        private IEnumerator DelayedStartNextRound()
        {
            // Wait for end of frame to ensure GameObject activation is processed
            yield return new WaitForEndOfFrame();

            // AssignMutationPoints(); // Removed to prevent double-award
            StartNextRound();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked); // Or however you add it to the player
            // If the mycovariant triggers an instant effect, resolve that here.
            var playerMyco = player.PlayerMycovariants.LastOrDefault(pm => pm.MycovariantId == picked.Id);
            if (playerMyco != null && picked.ApplyEffect != null)
            {
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[UnityDraft] Triggering ApplyEffect for {picked.Name} (Id={picked.Id}) for PlayerId={playerMyco.PlayerId}");
                picked.ApplyEffect.Invoke(playerMyco, Board, rng, null);
                playerMyco.MarkTriggered();
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

    }
}
