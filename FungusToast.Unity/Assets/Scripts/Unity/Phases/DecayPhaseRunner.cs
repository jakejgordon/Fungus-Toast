using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
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
        private SpecialEventPresentationService specialEventPresentationService;
        private readonly List<int> septalAlarmResistanceTiles = new();
        private GameBoard subscribedResistanceBoard;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            if (!ReferenceEquals(subscribedResistanceBoard, board))
            {
                if (subscribedResistanceBoard != null)
                {
                    subscribedResistanceBoard.ResistanceAppliedBatch -= OnResistanceAppliedBatch_Buffer;
                }

                board.ResistanceAppliedBatch += OnResistanceAppliedBatch_Buffer;
                subscribedResistanceBoard = board;
            }

            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.gridVisualizer = gridVisualizer ?? throw new ArgumentNullException(nameof(gridVisualizer));
        }

        public void StartDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver,
            SpecialEventPresentationService specialEventPresentationService)
        {
            this.specialEventPresentationService = specialEventPresentationService;
            septalAlarmResistanceTiles.Clear();
            StartCoroutine(RunDecayPhase(
                failedGrowthsByPlayerId,
                rng,
                simulationObserver
            ));
        }

        private void OnDestroy()
        {
            if (subscribedResistanceBoard != null)
            {
                subscribedResistanceBoard.ResistanceAppliedBatch -= OnResistanceAppliedBatch_Buffer;
                subscribedResistanceBoard = null;
            }
        }

        private void OnResistanceAppliedBatch_Buffer(int playerId, GrowthSource source, IReadOnlyList<int> tileIds)
        {
            if (source != GrowthSource.SeptalAlarm || tileIds == null || tileIds.Count == 0)
            {
                return;
            }

            foreach (var tileId in tileIds)
            {
                if (!septalAlarmResistanceTiles.Contains(tileId))
                {
                    septalAlarmResistanceTiles.Add(tileId);
                }
            }
        }


        private IEnumerator RunDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver)
        {

            DeathEngine.ExecuteDeathCycle(board, failedGrowthsByPlayerId, rng, simulationObserver);
            board.OnPostDecayPhase();

            yield return new WaitForSeconds(UIEffectConstants.TimeBeforeDecayRender);

            gridVisualizer.RenderBoard(board);

            if (septalAlarmResistanceTiles.Count > 0)
            {
                gridVisualizer.PlayResistancePulseBatchScaled(septalAlarmResistanceTiles, 0.45f);
                yield return gridVisualizer.WaitForAllAnimations();
                septalAlarmResistanceTiles.Clear();
            }

            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(board.Players);
            GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(board.Players);

            bool hadSpecialEvents = specialEventPresentationService != null && specialEventPresentationService.HasPendingEvents;
            if (hadSpecialEvents)
            {
                yield return specialEventPresentationService.PresentPendingAfterDecayRender();
            }
            else
            {
                yield return new WaitForSeconds(UIEffectConstants.TimeAfterDecayRender);
            }

            GameManager.Instance.OnRoundComplete();
        }

    }

}
