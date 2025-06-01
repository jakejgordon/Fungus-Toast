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
        [SerializeField] private TextMeshProUGUI gamePhaseText;
        [SerializeField] private DecayPhaseRunner decayPhaseRunner;

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
            SetupPlayers();
            SetupBoard();
            gridVisualizer.Initialize(Board);
            SetupUI();
        }

        private void SetupPlayers()
        {
            players.Clear();
            int baseMP = GameBalance.StartingMutationPoints;

            humanPlayer = new Player(0, "Human", PlayerTypeEnum.Human, AITypeEnum.Random);
            humanPlayer.SetBaseMutationPoints(baseMP);
            players.Add(humanPlayer);

            var strategyPool = new IMutationSpendingStrategy[]
            {
                new RandomMutationSpendingStrategy(),
                new GrowthThenDefenseSpendingStrategy(),
                new SmartRandomMutationSpendingStrategy(),
                new MutationFocusedMutationSpendingStrategy()
            };

            for (int i = 1; i < playerCount; i++)
            {
                var ai = new Player(i, $"AI Player {i}", PlayerTypeEnum.AI, AITypeEnum.Random);
                ai.SetBaseMutationPoints(baseMP);
                ai.SetMutationStrategy(strategyPool[Random.Range(0, strategyPool.Length)]);
                players.Add(ai);
            }

            foreach (var p in players)
            {
                var icon = gridVisualizer.GetTileForPlayer(p.PlayerId)?.sprite;
                if (icon != null) gameUIManager.PlayerUIBinder.AssignIcon(p, icon);
            }

            gameUIManager.MoldProfilePanel?.Initialize(humanPlayer, players);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
        }

        private void SetupBoard()
        {
            Board.PlaceInitialSpore(0, 2, 2);
            if (playerCount > 1)
                Board.PlaceInitialSpore(1, boardWidth - 3, boardHeight - 3);

            gridVisualizer.RenderBoard(Board);
        }

        private void SetupUI()
        {
            if (gameUIManager.MutationUIManager != null)
            {
                gameUIManager.MutationUIManager.Initialize(humanPlayer);
                gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            }
        }

        public void StartGrowthPhase()
        {
            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, players, gridVisualizer);
                growthPhaseRunner.StartGrowthPhase();
            }
        }

        public void StartDecayPhase()
        {
            if (gameEnded) return;

            decayPhaseRunner.Initialize(Board, players, gridVisualizer);
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

            SetGamePhaseText("Mutation Phase");
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

        public void SetGamePhaseText(string label)
        {
            if (gamePhaseText != null) gamePhaseText.text = label;
        }


        public void InitializeGame(int numberOfPlayers)
        {
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;

            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            InitializeShuffledPlayers();

            SetupPlayers();
            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            SetGamePhaseText("Mutation Phase");

            gameUIManager.MutationUIManager.gameObject.SetActive(true);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(players);
        }

        private void InitializeShuffledPlayers()
        {
            players.Clear();
            var baseMP = GameBalance.StartingMutationPoints;

            // Step 1: Create list of player definitions (type + name)
            var playerDefinitions = new List<(PlayerTypeEnum type, string name, IMutationSpendingStrategy strategy)>
    {
        (PlayerTypeEnum.Human, "Human", null)
    };

            var strategyPool = new IMutationSpendingStrategy[]
            {
                new RandomMutationSpendingStrategy(),
                new GrowthThenDefenseSpendingStrategy(),
                new SmartRandomMutationSpendingStrategy(),
                new MutationFocusedMutationSpendingStrategy()
            };

            for (int i = 1; i < playerCount; i++)
            {
                var strategy = strategyPool[Random.Range(0, strategyPool.Length)];
                playerDefinitions.Add((PlayerTypeEnum.AI, $"AI Player {i}", strategy));
            }

            // Step 2: Shuffle definitions
            for (int i = 0; i < playerDefinitions.Count; i++)
            {
                int j = Random.Range(i, playerDefinitions.Count);
                (playerDefinitions[i], playerDefinitions[j]) = (playerDefinitions[j], playerDefinitions[i]);
            }

            // Step 3: Create players with shuffled roles and sequential IDs
            for (int i = 0; i < playerDefinitions.Count; i++)
            {
                var def = playerDefinitions[i];
                var player = new Player(i, def.name, def.type, AITypeEnum.Random);
                player.SetBaseMutationPoints(baseMP);
                if (def.type == PlayerTypeEnum.AI)
                    player.SetMutationStrategy(def.strategy);
                players.Add(player);
            }

            humanPlayer = players.First(p => p.PlayerType == PlayerTypeEnum.Human);
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

        
    }
}
