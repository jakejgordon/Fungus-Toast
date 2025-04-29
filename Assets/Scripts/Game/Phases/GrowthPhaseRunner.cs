using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Core.Growth;
using FungusToast.Grid;

namespace FungusToast.Game.Phases
{
    public class GrowthPhaseRunner : MonoBehaviour
    {
        public int totalGrowthCycles = 5;
        public float timeBetweenCycles = 1f;

        private GameBoard board;
        private List<Player> players;
        private GridVisualizer gridVisualizer;
        private bool isRunning = false;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
        }

        public void StartGrowthPhase()
        {
            if (!isRunning)
            {
                StartCoroutine(RunGrowthCycles());
            }
        }

        private IEnumerator RunGrowthCycles()
        {
            isRunning = true;

            for (int cycle = 0; cycle < totalGrowthCycles; cycle++)
            {
                Debug.Log($"🌱 Starting Growth Cycle {cycle + 1}/{totalGrowthCycles}");

                GrowthEngine.ExecuteGrowthCycle(board, players);
                gridVisualizer.RenderBoard(board);

                yield return new WaitForSeconds(timeBetweenCycles);
            }

            isRunning = false;

            Debug.Log("🌾 Growth Phase complete. Resetting mutation points.");
            GameManager.Instance.OnGrowthPhaseComplete();
        }
    }
}
