using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
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
        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();

        private int phaseCycle = 0; // 1–5 for UI display

        public int CurrentCycle => phaseCycle;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board;
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

            roundContext = new RoundContext();

            StartCoroutine(RunNextCycle());
        }

        private IEnumerator RunNextCycle()
        {
            if (phaseCycle >= GameBalance.TotalGrowthCycles)
            {
                Debug.Log("🌾 Growth complete. Preparing for decay phase...");

                // === Post-Growth Mutation Effects ===
                board.OnPostGrowthPhase();

                isRunning = false;
                GameManager.Instance.StartDecayPhase();
                yield break;
            }

            phaseCycle++;
            board.IncrementGrowthCycle(); // GLOBAL counter

            Debug.Log($"🌿 Growth Cycle {phaseCycle}/{GameBalance.TotalGrowthCycles}");

            var failedThisCycle = processor.ExecuteSingleCycle(roundContext);
            MergeFailedGrowths(failedThisCycle);
            gridVisualizer.RenderBoard(board);

            // Update Occupancy in sidebar after each growth cycle
            int round = GameManager.Instance.Board.CurrentRound;
            float occupancy = GameManager.Instance.Board.GetOccupiedTileRatio() * 100f;
            GameManager.Instance.GameUI.RightSidebar.SetRoundAndOccupancy(round, occupancy);


            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(phaseCycle);
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(board.Players);

            yield return new WaitForSeconds(GameBalance.TimeBetweenGrowthCycles);
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
