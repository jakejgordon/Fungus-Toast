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

        private int phaseCycle = 0; // 1–5 for UI display

        public int CurrentCycle => phaseCycle;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.players = players;
            this.gridVisualizer = gridVisualizer;
            this.processor = new GrowthPhaseProcessor(board, players, rng);
            isRunning = false;
        }

        public void StartGrowthPhase()
        {
            if (isRunning) return;

            isRunning = true;
            phaseCycle = 0;

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(1);
            GameManager.Instance.GameUI.PhaseBanner.Show("Growth Phase Begins!", 2f);

            StartCoroutine(RunNextCycle());
        }

        private IEnumerator RunNextCycle()
        {
            if (phaseCycle >= GameBalance.TotalGrowthCycles)
            {
                Debug.Log("🌾 Growth complete. Preparing for decay phase...");
                isRunning = false;
                GameManager.Instance.StartDecayPhase();
                yield break;
            }

            phaseCycle++;
            board.IncrementGrowthCycle(); // GLOBAL counter

            Debug.Log($"🌿 Growth Cycle {phaseCycle}/{GameBalance.TotalGrowthCycles}");

            processor.ExecuteSingleCycle();
            MutationEffectProcessor.ApplyStartOfTurnEffects(board, players, rng);
            gridVisualizer.RenderBoard(board);

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(phaseCycle);
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(players);

            yield return new WaitForSeconds(GameBalance.TimeBetweenGrowthCycles);
            StartCoroutine(RunNextCycle());
        }
    }
}
