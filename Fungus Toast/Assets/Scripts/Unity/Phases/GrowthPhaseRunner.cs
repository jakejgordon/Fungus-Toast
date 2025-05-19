using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;
using FungusToast.Unity.Grid;
using FungusToast.Core;
using FungusToast.Core.Config;
using FungusToast.Core.Board;

namespace FungusToast.Unity.Phases
{
    public class GrowthPhaseRunner : MonoBehaviour
    {
        private GrowthPhaseProcessor processor;
        private GameBoard board;
        private List<Player> players;
        private GridVisualizer gridVisualizer;
        private bool isRunning = false;
        private System.Random rng = new();

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
            this.processor = new GrowthPhaseProcessor(board, players, rng);
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
            GameManager.Instance.SetGamePhaseText("Growth Phase (Cycle 1/" + GameBalance.TotalGrowthCycles + ")");
            Debug.Log("🌱 Growth Phase Starting...");

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                Debug.Log($"🌿 Growth Cycle {cycle + 1}/{GameBalance.TotalGrowthCycles}");

                processor.ExecuteSingleCycle();
                MutationEffectProcessor.ApplyStartOfTurnEffects(board, players, rng);
                gridVisualizer.RenderBoard(board);

                GameManager.Instance.SetGamePhaseText($"Growth Phase (Cycle {cycle + 1}/{GameBalance.TotalGrowthCycles})");

                yield return new WaitForSeconds(GameBalance.TimeBetweenGrowthCycles);
            }

            Debug.Log("🌾 Growth Cycles complete. Preparing for decay phase...");
            isRunning = false;

            GameManager.Instance.StartDecayPhase();
        }
    }
}
