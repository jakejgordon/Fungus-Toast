using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Grid;
using FungusToast.Game.Phases;
using FungusToast.AI;
using FungusToast.Core.Config;
using System.Linq;
using TMPro;
using FungusToast.Core.Growth;

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

        [SerializeField] private MutationManager mutationManager;
        [SerializeField] private GrowthPhaseRunner growthPhaseRunner;
        [SerializeField] private GameUIManager gameUIManager;
        [SerializeField] private TextMeshProUGUI gamePhaseText;

        public GameUIManager GameUI => gameUIManager;

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
                new GrowthThenDefenseSpendingStrategy()
            };

            for (int i = 1; i < playerCount; i++)
            {
                var aiPlayer = new Player(i, $"AI Player {i}", PlayerTypeEnum.AI, AITypeEnum.Random);
                aiPlayer.SetBaseMutationPoints(baseMP);
                aiPlayer.SetMutationStrategy(strategyPool[Random.Range(0, strategyPool.Length)]);
                players.Add(aiPlayer);
            }

            foreach (var player in players)
            {
                Sprite icon = gridVisualizer.GetTileForPlayer(player.PlayerId)?.sprite;
                if (icon != null)
                    gameUIManager.PlayerUIBinder.AssignIcon(player, icon);
                else
                    Debug.LogWarning($"⚠️ No icon found for player {player.PlayerId}");
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
            else
            {
                Debug.LogError("MutationUIManager reference not assigned in GameManager!");
            }
        }

        public void InitializeGame(int numberOfPlayers)
        {
            playerCount = numberOfPlayers;

            SetupPlayers();
            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            SetGamePhaseText("Mutation Phase");
        }

        public void PlaceStartingSpores()
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

        public void SpendAllMutationPointsForAIPlayers()
        {
            foreach (Player player in players)
            {
                if (player.PlayerType == PlayerTypeEnum.AI)
                    player.MutationStrategy?.SpendMutationPoints(player, mutationManager.GetAllMutations().ToList());
            }

            Debug.Log("All AI players have spent their mutation points.");
            StartGrowthPhase();
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

        public void StartDecayPhase()
        {
            SetGamePhaseText("Decay Phase");
            Debug.Log("💀 Running Death Cycle...");
            DeathEngine.ExecuteDeathCycle(Board, players);
            gridVisualizer.RenderBoard(Board);
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

            StartCoroutine(FinishDecayPhaseAfterDelay(1f));
        }

        private System.Collections.IEnumerator FinishDecayPhaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnGrowthPhaseComplete(); // Resets mutation points and starts Mutation Phase again
        }

        public void OnGrowthPhaseComplete()
        {
            AssignMutationPoints();
            Debug.Log("All players have received new mutation points.");

            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            gameUIManager.MoldProfilePanel?.Refresh();
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

            SetGamePhaseText("Mutation Phase");
        }

        public void AssignMutationPoints()
        {
            foreach (var player in players)
            {
                int baseIncome = player.GetMutationPointIncome();
                int bonus = player.GetBonusMutationPoints();
                player.MutationPoints = baseIncome + bonus;
                Debug.Log($"🌱 Player {player.PlayerId} assigned {player.MutationPoints} MP (base: {baseIncome}, bonus: {bonus})");

                // Trigger Mutator Phenotype auto-upgrade if owned
                player.TryTriggerAutoUpgrade();
            }

            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
        }


        public void SetGamePhaseText(string phaseLabel)
        {
            if (gamePhaseText != null)
                gamePhaseText.text = phaseLabel;
        }
    }
}
