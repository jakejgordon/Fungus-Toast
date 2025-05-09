﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using FungusToast.Core.AI;
using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Cameras;
using FungusToast.Unity.Phases;
using FungusToast.Unity.UI;

namespace FungusToast.Unity
{
    public class GameManager : MonoBehaviour
    {
        /* ─────────── Inspector ─────────── */
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

        /* ─────────── State ─────────── */
        private bool isCountdownActive = false;
        private int roundsRemainingUntilGameEnd = 0;
        private bool gameEnded = false;

        public GameBoard Board { get; private set; }
        public GameUIManager GameUI => gameUIManager;
        public static GameManager Instance { get; private set; }

        private readonly List<Player> players = new();
        private Player humanPlayer;

        /* ───────────────────────────────────────────────────────── */
        #region Unity Lifecycle
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
        #endregion

        /* ───────────────────────────────────────────────────────── */
        #region Setup
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
        #endregion

        /* ───────────────────────────────────────────────────────── */
        #region Phase Flow
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

            SetGamePhaseText("Decay Phase");
            DeathEngine.ExecuteDeathCycle(Board, players);
            gridVisualizer.RenderBoard(Board);
            gameUIManager.RightSidebar?.UpdatePlayerSummaries(players);

            StartCoroutine(FinishDecayPhaseAfterDelay(1f));
        }

        private IEnumerator FinishDecayPhaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (gameEnded) yield break;

            CheckForEndgameCondition();
            if (gameEnded) yield break;

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
        #endregion

        /* ───────────────────────────────────────────────────────── */
        #region End-game Logic
        private void CheckForEndgameCondition()
        {
            int totalTiles = Board.Width * Board.Height;
            int occupiedTiles = Board.GetAllCells().Count;
            float ratio = (float)occupiedTiles / totalTiles;

            if (!isCountdownActive && ratio >= GameBalance.GameEndTileOccupancyThreshold)
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

            // Disable interactive UI
            gameUIManager.MutationUIManager.gameObject.SetActive(false);
            gameUIManager.RightSidebar.gameObject.SetActive(false);
            gameUIManager.LeftSidebar.gameObject.SetActive(false);

            gameUIManager.EndGamePanel.gameObject.SetActive(true);

            // Show results
            gameUIManager.EndGamePanel.ShowResults(ranked, Board);
        }
        #endregion

        /* ───────────────────────────────────────────────────────── */
        #region Utility
        private void AssignMutationPoints()
        {
            foreach (var p in players)
            {
                int baseIncome = p.GetMutationPointIncome();
                int bonus = p.GetBonusMutationPoints();
                p.MutationPoints = baseIncome + bonus;
                p.TryTriggerAutoUpgrade(mutationManager.AllMutations.Values.ToList());
            }
            gameUIManager.MutationUIManager?.RefreshAllMutationButtons();
        }

        public void SetGamePhaseText(string label)
        {
            if (gamePhaseText != null) gamePhaseText.text = label;
        }
        #endregion

        /* ───────────────────────────────────────────────────────── */
        /*  PUBLIC HELPERS EXPECTED BY UI_MutationManager + PreGameUI */
        /* ───────────────────────────────────────────────────────── */

        /// <summary>
        /// Called by the pre-game menu. Re-initialises everything for N players.
        /// </summary>
        public void InitializeGame(int numberOfPlayers)
        {
            // clear any previous session
            gameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;

            playerCount = numberOfPlayers;

            // fresh board + players
            SetupPlayers();
            Board = new GameBoard(boardWidth, boardHeight, playerCount);
            gridVisualizer.Initialize(Board);
            PlaceStartingSpores();
            gridVisualizer.RenderBoard(Board);

            mutationManager.ResetMutationPoints(players);

            // refresh UI
            gameUIManager.MutationUIManager.Initialize(humanPlayer);
            gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
            SetGamePhaseText("Mutation Phase");
        }

        /// <summary>
        /// Lets all AI players dump their mutation points at once, then starts the Growth Phase.
        /// Used by UI_MutationManager.
        /// </summary>
        public void SpendAllMutationPointsForAIPlayers()
        {
            foreach (var p in players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI)
                    p.MutationStrategy?.SpendMutationPoints(p, mutationManager.GetAllMutations().ToList());
            }

            Debug.Log("All AI players have spent their mutation points.");
            StartGrowthPhase();
        }

        /// <summary>
        /// Spawns each player’s initial spore in a circle around board center.
        /// </summary>
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
