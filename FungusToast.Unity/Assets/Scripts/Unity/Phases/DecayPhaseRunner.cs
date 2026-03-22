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
        private int runVersion;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            runVersion++;

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
            this.specialEventPresentationService = null;
            septalAlarmResistanceTiles.Clear();
        }

        public void ResetForGameTransition()
        {
            runVersion++;
            StopAllCoroutines();

            if (subscribedResistanceBoard != null)
            {
                subscribedResistanceBoard.ResistanceAppliedBatch -= OnResistanceAppliedBatch_Buffer;
                subscribedResistanceBoard = null;
            }

            board = null;
            gridVisualizer = null;
            specialEventPresentationService = null;
            septalAlarmResistanceTiles.Clear();
        }

        public void StartDecayPhase(
            Dictionary<int, int> failedGrowthsByPlayerId,
            System.Random rng,
            ISimulationObserver simulationObserver,
            SpecialEventPresentationService specialEventPresentationService)
        {
            this.specialEventPresentationService = specialEventPresentationService;
            septalAlarmResistanceTiles.Clear();
            runVersion++;
            int activeRunVersion = runVersion;
            StartCoroutine(RunDecayPhase(
                failedGrowthsByPlayerId,
                rng,
                simulationObserver,
                board,
                gridVisualizer,
                this.specialEventPresentationService,
                activeRunVersion
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
            ISimulationObserver simulationObserver,
            GameBoard activeBoard,
            GridVisualizer activeGridVisualizer,
            SpecialEventPresentationService activeSpecialEventPresentationService,
            int activeRunVersion)
        {
            if (activeRunVersion != runVersion || activeBoard == null || activeGridVisualizer == null)
            {
                yield break;
            }

            DeathEngine.ExecuteDeathCycle(activeBoard, failedGrowthsByPlayerId, rng, simulationObserver);
            activeBoard.OnPostDecayPhase();

            yield return new WaitForSeconds(UIEffectConstants.TimeBeforeDecayRender);

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            activeGridVisualizer.RenderBoard(activeBoard);

            if (septalAlarmResistanceTiles.Count > 0)
            {
                activeGridVisualizer.PlayResistancePulseBatchScaled(septalAlarmResistanceTiles, 0.45f);
                yield return activeGridVisualizer.WaitForAllAnimations();
                septalAlarmResistanceTiles.Clear();
            }

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(activeBoard.Players);
            GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(activeBoard.Players);

            bool hadSpecialEvents = activeSpecialEventPresentationService != null && activeSpecialEventPresentationService.HasPendingEvents;
            if (hadSpecialEvents)
            {
                yield return activeSpecialEventPresentationService.PresentPendingAfterDecayRender();
            }
            else
            {
                yield return new WaitForSeconds(UIEffectConstants.TimeAfterDecayRender);
            }

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            GameManager.Instance.OnRoundComplete();
        }

    }

}
