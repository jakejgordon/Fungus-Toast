using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Phases
{
    public class GrowthPhaseRunner : MonoBehaviour
    {
        private GrowthPhaseProcessor processor;
        private GameBoard board;
        private GridVisualizer gridVisualizer;
        private bool isRunning = false;
        private System.Random rng = new();
        private RoundContext roundContext;
        private ISimulationObserver observer;
        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        private int phaseCycle = 0; // 1–5 for UI display

        public int CurrentCycle => phaseCycle;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
            this.gridVisualizer = gridVisualizer;
            this.observer = GameManager.Instance.GameUI.GameLogRouter;
            this.processor = new GrowthPhaseProcessor(board, players, rng, observer);
            isRunning = false;
        }

        public void StartGrowthPhase()
        {
            if (isRunning) return;

            isRunning = true;
            phaseCycle = 0;

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(1);
            GameManager.Instance.GameUI.PhaseBanner.Show("Growth Phase Begins!", 2f);

            roundContext = new RoundContext();

            StartCoroutine(RunNextCycle());
        }

        private IEnumerator RunNextCycle()
        {
            if (phaseCycle >= GameBalance.TotalGrowthCycles)
            {
                // === Post-Growth Mutation Effects ===
                board.OnPostGrowthPhase();

                // Apply Hyphal Resistance Transfer effect after growth phase
                MycovariantEffectProcessor.OnPostGrowthPhase_HyphalResistanceTransfer(board, board.Players, rng, observer);

                // NOW signal completion so listeners (e.g. GameManager OnPostGrowthPhaseCompleted_CaptureHrt) run
                board.OnPostGrowthPhaseCompleted();

                // Sort player summary rows at the end of growth phase
                GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(board.Players);

                isRunning = false;
                GameManager.Instance.StartDecayPhase();
                yield break;
            }

            phaseCycle++;
            board.IncrementGrowthCycle(); // GLOBAL counter

            var failedThisCycle = processor.ExecuteSingleCycle(roundContext);
            MergeFailedGrowths(failedThisCycle);
            gridVisualizer.RenderBoard(board);

            // Update Occupancy in sidebar after each growth cycle
            int round = GameManager.Instance.Board.CurrentRound;
            float occupancy = GameManager.Instance.Board.GetOccupiedTileRatio() * 100f;
            GameManager.Instance.GameUI.RightSidebar.SetRoundAndOccupancy(round, occupancy);

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(phaseCycle);
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(board.Players);

            yield return new WaitForSeconds(UIEffectConstants.TimeBetweenGrowthCycles);
            StartCoroutine(RunNextCycle());
        }

        private void MergeFailedGrowths(Dictionary<int, int> failedThisCycle)
        {
            foreach (var kvp in failedThisCycle)
            {
                if (FailedGrowthsByPlayerId.ContainsKey(kvp.Key))
                    FailedGrowthsByPlayerId[kvp.Key] += kvp.Value;
                else
                    FailedGrowthsByPlayerId[kvp.Key] = kvp.Value;
            }
        }

    }
}
