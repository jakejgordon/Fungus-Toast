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

        private int currentCycle = 0;
        public int CurrentCycle => currentCycle;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
            this.processor = new GrowthPhaseProcessor(board, players, rng);
            currentCycle = 0;
            isRunning = false;
        }

        public void StartGrowthPhase()
        {
            if (isRunning) return;

            isRunning = true;
            currentCycle = 0;

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(1);
            GameManager.Instance.GameUI.PhaseBanner.Show("Growth Phase Begins!", 2f);

            StartCoroutine(RunNextCycle());
        }

        private IEnumerator RunNextCycle()
        {
            if (currentCycle >= GameBalance.TotalGrowthCycles)
            {
                Debug.Log("🌾 Growth complete. Preparing for decay phase...");
                isRunning = false;
                GameManager.Instance.StartDecayPhase();
                yield break;
            }

            currentCycle++;
            Debug.Log($"🌿 Growth Cycle {currentCycle}/{GameBalance.TotalGrowthCycles}");

            processor.ExecuteSingleCycle();
            MutationEffectProcessor.ApplyStartOfTurnEffects(board, players, rng);
            gridVisualizer.RenderBoard(board);

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(currentCycle);

            // Optional: update right sidebar player stats per cycle
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(players);

            yield return new WaitForSeconds(GameBalance.TimeBetweenGrowthCycles);
            StartCoroutine(RunNextCycle());
        }
    }
}
