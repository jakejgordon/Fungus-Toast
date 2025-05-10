using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Growth;
using FungusToast.Grid;
using FungusToast.Game; // To access GameManager

namespace FungusToast.Core.Phases
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
                StartCoroutine(RunFullGrowthPhase());
            }
        }

        private IEnumerator RunFullGrowthPhase()
        {
            isRunning = true;

            Debug.Log("🌱 Growth Phase Starting...");
            GameManager.Instance.SetGamePhaseText("Growth Phase (Cycle 1/" + totalGrowthCycles + ")");

            for (int cycle = 0; cycle < totalGrowthCycles; cycle++)
            {
                Debug.Log($"🌿 Growth Cycle {cycle + 1}/{totalGrowthCycles}");

                GrowthEngine.ExecuteGrowthCycle(board, players);
                gridVisualizer.RenderBoard(board);

                GameManager.Instance.SetGamePhaseText($"Growth Phase (Cycle {cycle + 1}/{totalGrowthCycles})");

                yield return new WaitForSeconds(timeBetweenCycles);
            }

            Debug.Log("🌾 Growth Cycles complete. Preparing for decay phase...");
            isRunning = false;

            GameManager.Instance.StartDecayPhase(); // Now defers to GameManager to handle decay
        }
    }
}
