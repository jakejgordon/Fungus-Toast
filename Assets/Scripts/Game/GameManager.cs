using UnityEngine;
using FungusToast.Core;
using FungusToast.Grid;
using FungusToast.Game;
using FungusToast.Core.Player; // <-- NEW: using for Player class

namespace FungusToast.Game
{
    public class GameManager : MonoBehaviour
    {
        public int boardWidth = 20;
        public int boardHeight = 20;
        public int playerCount = 2;

        public GridVisualizer gridVisualizer;

        public static GameManager Instance { get; private set; }
        public GameBoard Board { get; private set; }
        public CameraCenterer cameraCenterer;

        [SerializeField] private MutationUIManager mutationUIManager;
        [SerializeField] private MutationManager mutationManager;

        private Player humanPlayer; // Human player reference

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
            SetupUI();
        }

        private void SetupPlayers()
        {
            // Create a basic human player for now
            humanPlayer = new Player(
                playerId: 0,
                playerName: "Human",
                playerType: PlayerTypeEnum.Human,
                aiType: AITypeEnum.Random // Irrelevant for humans
            );

            // Example: Starting mutation points
            humanPlayer.MutationPoints = 5;
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

            // Recreate Players
            SetupPlayers();

            // Reset the Board
            Board = new GameBoard(boardWidth, boardHeight, playerCount);

            // Place starting spores
            PlaceStartingSpores();

            // Render the board
            gridVisualizer.RenderBoard(Board);

            // Reset mutation system
            mutationManager.ResetMutationPoints();

            // Reconnect UI
            mutationUIManager.Initialize(humanPlayer);
            mutationUIManager.SetSpendPointsButtonVisible(true);
            mutationUIManager.PopulateRootMutations();
        }

        //-- This places players roughly in a circle around the toast, spaced out evenly no matter the count (2, 3, 6, 8, etc.)
        public void PlaceStartingSpores()
        {
            float radius = Mathf.Min(boardWidth, boardHeight) * 0.35f;
            Vector2 center = new Vector2(boardWidth / 2f, boardHeight / 2f);

            for (int i = 0; i < playerCount; i++)
            {
                float angle = i * Mathf.PI * 2f / playerCount;
                float x = center.x + radius * Mathf.Cos(angle);
                float y = center.y + radius * Mathf.Sin(angle);

                int px = Mathf.Clamp(Mathf.RoundToInt(x), 0, boardWidth - 1);
                int py = Mathf.Clamp(Mathf.RoundToInt(y), 0, boardHeight - 1);

                Board.PlaceInitialSpore(i, px, py);
            }
        }
    }
}
