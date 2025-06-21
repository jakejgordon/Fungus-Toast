using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Cameras;
using FungusToast.Unity.Phases;
using FungusToast.Unity.UI;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.AI;
using FungusToast.Core.Growth;
using FungusToast.Core.Death;
using FungusToast.Core.Phases;
using FungusToast.Core.Board;
using FungusToast.Unity.UI.MycovariantDraft;

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


        private bool isCountdownActive = false;
        private int roundsRemainingUntilGameEnd = 0;
        private bool gameEnded = false;

        private System.Random rng;

        public GameBoard Board { get; private set; }
        public GameUIManager GameUI => gameUIManager;
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new();
        private Player humanPlayer;

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
            InitializeGame(playerCount);
        }

        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);

            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            InitializePlayersWithHumanFirst();

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.PhaseBanner.Show("New Game Settings", 2f);
            phaseProgressTracker?.ResetTracker();
            UpdatePhaseProgressTrackerLabel();
            phaseProgressTracker?.HighlightMutationPhase();


            gameUIManager.MutationUIManager.gameObject.SetActive(true);

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

            var growthAndResilienceMax3HighTier = new ParameterizedSpendingStrategy(
                strategyName: "GrowthResilience_Max3_HighTier",
                maxTier: MutationTier.Tier3,
                prioritizeHighTier: true,
                priorityMutationCategories: new List<MutationCategory>
                {
                    MutationCategory.Growth,
                    MutationCategory.CellularResilience
                });

            var regenerativeHyphaeFocus = new ParameterizedSpendingStrategy(
                strategyName: "Regenerative Hyphae Focus",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.RegenerativeHyphae });

            var powerMutations1 = new ParameterizedSpendingStrategy(
                strategyName: "Power Mutations 1",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.AdaptiveExpression, MutationIds.NecrohyphalInfiltration, MutationIds.RegenerativeHyphae });

            var mutatorGrowth = new ParameterizedSpendingStrategy(
                strategyName: "Mutator Growth",
                prioritizeHighTier: true,
                targetMutationIds: new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.CreepingMold });

            // Define and shuffle AI players
            var strategyPool = new IMutationSpendingStrategy[]
            {
                new RandomMutationSpendingStrategy(),
                growthAndResilienceMax3HighTier,
                regenerativeHyphaeFocus,
                powerMutations1,
                mutatorGrowth
            };

            var aiDefinitions = new List<IMutationSpendingStrategy>();
            for (int i = 1; i < playerCount; i++)
            {
                aiDefinitions.Add(strategyPool[Random.Range(0, strategyPool.Length)]);
            }

            aiDefinitions = aiDefinitions.OrderBy(_ => Random.value).ToList();

            // Create AI players with shuffled order
            for (int i = 0; i < aiDefinitions.Count; i++)
            {
                int playerId = i + 1; // start AI player IDs at 1
                var aiPlayer = new Player(playerId, $"AI Player {playerId}", PlayerTypeEnum.AI, AITypeEnum.Random);
                aiPlayer.SetBaseMutationPoints(baseMP);
                aiPlayer.SetMutationStrategy(aiDefinitions[i]);
                players.Add(aiPlayer);
            }

            // Re-assign icons and initialize UI panels
            foreach (var p in players)
            {
                var icon = gridVisualizer.GetTileForPlayer(p.PlayerId)?.sprite;
                if (icon != null) gameUIManager.PlayerUIBinder.AssignIcon(p, icon);
            }

            gameUIManager.MoldProfilePanel?.Initialize(humanPlayer, players);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);

            Board.Players.Clear();
            Board.Players.AddRange(players);
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
                growthPhaseRunner.Initialize(Board, players, gridVisualizer);
                gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f);
                phaseProgressTracker?.AdvanceToNextGrowthCycle(Board.CurrentGrowthCycle);
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;

            decayPhaseRunner.Initialize(Board, players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase(growthPhaseRunner.FailedGrowthsByPlayerId);
        }

        public void OnRoundComplete()
        {
            if (gameEnded) return;

            foreach (var player in players)
                player.TickDownActiveSurges();

            CheckForEndgameCondition();
            if (gameEnded) return;

            // === 1. Trigger draft phase if this is the right round ===
            if (Board.CurrentRound == MycovariantGameBalance.MycovariantSelectionTriggerRound)
            {
                StartMycovariantDraftPhase();
                return; // Prevent starting mutation phase until draft is complete!
            }

            // === 2. Otherwise, start the next mutation phase as usual ===
            StartNextRound();

            Board.IncrementRound();

            int round = Board.CurrentRound;
            float occupancy = Board.GetOccupiedTileRatio() * 100f; // ratio to percent
            gameUIManager.RightSidebar.SetRoundAndOccupancy(round, occupancy);
        }

        public void StartNextRound()
        {
            if (gameEnded) return;

            AssignMutationPoints();
            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(true);

            gameUIManager.MoldProfilePanel?.Refresh();
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

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

            var ranked = players
                .OrderByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ThenByDescending(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive))
                .ToList();

            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.RightSidebar.gameObject.SetActive(false);
            gameUIManager.LeftSidebar.gameObject.SetActive(false);

            gameUIManager.EndGamePanel.gameObject.SetActive(true);
            gameUIManager.EndGamePanel.ShowResults(ranked, Board);
        }

        private void AssignMutationPoints()
        {
            var allMutations = mutationManager.AllMutations.Values.ToList();
            var rng = new System.Random();

            TurnEngine.AssignMutationPoints(Board, players, allMutations, rng);

            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            var rng = new System.Random();

            foreach (var p in players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList(), Board, rng);
            }

            Debug.Log("All AI players have spent their mutation points.");
            StartGrowthPhase();
        }

        private void UpdatePhaseProgressTrackerLabel()
        {
            if (Board.CurrentRound == MycovariantGameBalance.MycovariantSelectionTriggerRound)
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
            // Build the pool here—however you want!
            var draftPool = MycovariantDraftManager.BuildDraftPool(Board, players);

            // Create and initialize the pool manager
            var poolManager = new MycovariantPoolManager();
            poolManager.InitializePool(draftPool, rng);

            // Example: order by fewest living cells, or your draft order logic
            var draftOrder = players
                .OrderBy(p => Board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ToList();

            mycovariantDraftController.StartDraft(
                players, poolManager, draftOrder, rng, MycovariantGameBalance.MycovariantSelectionDraftSize);

            gameUIManager.PhaseBanner.Show("Mycovariant Draft Phase", 2f);
        }



        public void OnMycovariantDraftComplete()
        {
            // Proceed to next round and phase
            StartNextRound();
        }

        public void ResolveMycovariantDraftPick(Player player, Mycovariant picked)
        {
            player.AddMycovariant(picked); // Or however you add it to the player
                                           // If the mycovariant triggers an instant effect, resolve that here.
                                           // Optionally update UI
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);
        }

    }
}
