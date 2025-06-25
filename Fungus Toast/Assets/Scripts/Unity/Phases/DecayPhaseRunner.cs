using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.Phases
{
    public class DecayPhaseRunner : MonoBehaviour
    {
        private GameBoard board;
        private List<Player> players;
        private GridVisualizer gridVisualizer;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.players = players ?? throw new ArgumentNullException(nameof(players));
            this.gridVisualizer = gridVisualizer ?? throw new ArgumentNullException(nameof(gridVisualizer));
        }

        public void StartDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver = null)
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
            ISimulationObserver simulationObserver = null)
        {
            Debug.Log("💀 Decay Phase Starting...");

            DeathEngine.ExecuteDeathCycle(board, players, failedGrowthsByPlayerId, rng, simulationObserver);

            yield return new WaitForSeconds(GameBalance.TimeBeforeDecayRender);

            gridVisualizer.RenderBoard(board);

            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(players);

            yield return new WaitForSeconds(GameBalance.TimeAfterDecayRender);

            GameManager.Instance.OnRoundComplete();
        }

    }

}
