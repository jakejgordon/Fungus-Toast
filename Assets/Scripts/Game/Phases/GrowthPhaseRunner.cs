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

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
        }

        public void StartGrowthPhase()
        {
            StartCoroutine(GrowthPhaseCoroutine());
        }

        private IEnumerator GrowthPhaseCoroutine()
        {
            for (int cycle = 0; cycle < totalGrowthCycles; cycle++)
            {
                Debug.Log($"Starting Growth Cycle {cycle + 1}/{totalGrowthCycles}");

                GrowthEngine.ExecuteGrowthCycle(board, players);
                gridVisualizer.RenderBoard(board);

                yield return new WaitForSeconds(timeBetweenCycles);
            }

            Debug.Log("Growth Phase complete. Proceeding to Victory Check...");
            ProceedToVictoryCheck();
        }

        private void ProceedToVictoryCheck()
        {
            // Placeholder: Hook your Victory Check logic here
            Debug.Log("Victory Check not implemented yet!");
        }
    }
}
