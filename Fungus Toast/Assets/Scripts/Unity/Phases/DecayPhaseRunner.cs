using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Phases
{
    public class DecayPhaseRunner : MonoBehaviour
    {
        private GameBoard board;
        private GridVisualizer gridVisualizer;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.gridVisualizer = gridVisualizer ?? throw new ArgumentNullException(nameof(gridVisualizer));
        }

        public void StartDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver)
        {
            StartCoroutine(RunDecayPhase(
                failedGrowthsByPlayerId,
                rng,
                simulationObserver
            ));
        }


        private IEnumerator RunDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver)
        {

            DeathEngine.ExecuteDeathCycle(board, failedGrowthsByPlayerId, rng, simulationObserver);

            yield return new WaitForSeconds(UIEffectConstants.TimeBeforeDecayRender);

            gridVisualizer.RenderBoard(board);

            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(board.Players);
            GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(board.Players);

            yield return new WaitForSeconds(UIEffectConstants.TimeAfterDecayRender);

            GameManager.Instance.OnRoundComplete();
        }

    }

}
