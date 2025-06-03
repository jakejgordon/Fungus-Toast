using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using FungusToast.Core.Mutations;
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

        private bool isCountdownActive = false;
        private int roundsRemainingUntilGameEnd = 0;
        private bool gameEnded = false;

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
        }

        private void Start()
        {
            InitializeGame(playerCount);
            gameUIManager.PhaseBanner.Show("New Game Settings", 5f);
        }

        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;

            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            InitializePlayersWithHumanFirst();

            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f);
            phaseProgressTracker?.ResetTracker();
            phaseProgressTracker?.HighlightMutationPhase();

            gameUIManager.MutationUIManager.gameObject.SetActive(true);
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
                targetMutationIds: new List<int> { MutationIds.AdaptiveExpression, MutationIds.Necrosporulation, MutationIds.RegenerativeHyphae });

            // Define and shuffle AI players
            var strategyPool = new IMutationSpendingStrategy[]
            {
                new RandomMutationSpendingStrategy(),
                new GrowthThenDefenseSpendingStrategy(),
                growthAndResilienceMax3HighTier,
                regenerativeHyphaeFocus,
                powerMutations1
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
        }

        private void PlaceStartingSpores()
        {
            float radius = Mathf.Min(boardWidth, boardHeight) * 0.35f;
            Vector2 center = new Vector2(boardWidth / 2f, boardHeight / 2f);

            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / players.Count;
                int px = Mathf.Clamp(Mathf.RoundToInt(center.x + radius * Mathf.Cos(angle)), 0, boardWidth - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(center.y + radius * Mathf.Sin(angle)), 0, boardHeight - 1);
                Board.PlaceInitialSpore(i, px, py);
            }
        }

        public void StartGrowthPhase()
        {
            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, players, gridVisualizer);
                gameUIManager.PhaseBanner.Show("Growth Phase Begins!", 2f);
                phaseProgressTracker?.AdvanceToNextGrowthCycle(growthPhaseRunner.CurrentCycle);
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;

            decayPhaseRunner.Initialize(Board, players, gridVisualizer);
            gameUIManager.PhaseBanner.Show("Decay Phase Begins!", 2f);
            phaseProgressTracker?.HighlightDecayPhase();
            decayPhaseRunner.StartDecayPhase();
        }

        public void OnRoundComplete()
        {
            if (gameEnded) return;

            CheckForEndgameCondition();
            if (gameEnded) return;

            OnGrowthPhaseComplete();
        }

        public void OnGrowthPhaseComplete()
        {
            if (gameEnded) return;

            AssignMutationPoints();
            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MoldProfilePanel?.Refresh();
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

            gameUIManager.PhaseBanner.Show("Mutation Phase Begins!", 2f);
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
            foreach (var p in players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList(), Board);
            }

            Debug.Log("All AI players have spent their mutation points.");
            StartGrowthPhase();
        }
    }
}
