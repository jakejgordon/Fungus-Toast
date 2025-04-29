using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Grid;
using FungusToast.Game.Phases;

namespace FungusToast.Game
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

        [SerializeField] private MutationUIManager mutationUIManager;
        [SerializeField] private MutationManager mutationManager;
        [SerializeField] private GrowthPhaseRunner growthPhaseRunner;

        public static GameManager Instance { get; private set; }

        public GameBoard Board { get; private set; }

        private List<Player> players = new List<Player>();
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

            if (growthPhaseRunner == null)
            {
                Debug.LogError("GrowthPhaseRunner not assigned to GameManager!");
            }

            SetupBoard();
            SetupUI();
        }

        private void SetupPlayers()
        {
            players.Clear();

            humanPlayer = new Player(
                playerId: 0,
                playerName: "Human",
                playerType: PlayerTypeEnum.Human,
                aiType: AITypeEnum.Random
            );

            players.Add(humanPlayer);

            for (int i = 1; i < playerCount; i++)
            {
                Player aiPlayer = new Player(
                    playerId: i,
                    playerName: $"AI Player {i}",
                    playerType: PlayerTypeEnum.AI,
                    aiType: AITypeEnum.Random
                );
                players.Add(aiPlayer);
            }
        }

        private void SetupBoard()
        {
            Board.PlaceInitialSpore(0, 2, 2);

            if (playerCount > 1)
            {
                Board.PlaceInitialSpore(1, boardWidth - 3, boardHeight - 3);
            }

            gridVisualizer.RenderBoard(Board);
        }

        private void SetupUI()
        {
            if (mutationUIManager != null)
            {
                mutationUIManager.Initialize(humanPlayer);
                mutationUIManager.SetSpendPointsButtonVisible(true);
                mutationUIManager.PopulateRootMutations();
            }
            else
            {
                Debug.LogError("MutationUIManager reference not assigned in GameManager!");
            }
        }

        public void InitializeGame(int count)
        {
            playerCount = count;

            SetupPlayers();
            Board = new GameBoard(boardWidth, boardHeight, playerCount);

            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            mutationUIManager.Initialize(humanPlayer);
            mutationUIManager.SetSpendPointsButtonVisible(true);
            mutationUIManager.PopulateRootMutations();
        }

        public void PlaceStartingSpores()
        {
            float radius = Mathf.Min(boardWidth, boardHeight) * 0.35f;
            Vector2 center = new Vector2(boardWidth / 2f, boardHeight / 2f);

            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / players.Count;
                float x = center.x + radius * Mathf.Cos(angle);
                float y = center.y + radius * Mathf.Sin(angle);

                int px = Mathf.Clamp(Mathf.RoundToInt(x), 0, boardWidth - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(y), 0, boardHeight - 1);

                Board.PlaceInitialSpore(i, px, py);
            }
        }

        public void SpendAllMutationPointsForAIPlayers()
        {
            foreach (Player player in players)
            {
                if (player.PlayerType == PlayerTypeEnum.AI)
                {
                    while (player.MutationPoints > 0)
                    {
                        SpendMutationPointRandomly(player);
                    }
                }
            }

            Debug.Log("All AI players have spent their mutation points.");

            StartGrowthPhase();
        }

        private void SpendMutationPointRandomly(Player player)
        {
            player.MutationPoints--;

            Debug.Log($"AI Player {player.PlayerId} spent 1 mutation point.");
            // TODO: Future - actually pick random mutations instead of just burning points
        }

        private void StartGrowthPhase()
        {
            if (growthPhaseRunner != null)
            {
                growthPhaseRunner.Initialize(Board, players, gridVisualizer);
                growthPhaseRunner.StartGrowthPhase();
            }
            else
            {
                Debug.LogError("GrowthPhaseRunner is missing. Cannot start Growth Phase!");
            }
        }

        public void OnGrowthPhaseComplete()
        {
            mutationManager.ResetMutationPoints(players);

            Debug.Log("All players have received new mutation points.");

            // Restart the Mutation Phase for the human
            mutationUIManager.Initialize(humanPlayer);
            mutationUIManager.SetSpendPointsButtonVisible(true);
            mutationUIManager.PopulateRootMutations();
        }

    }
}
