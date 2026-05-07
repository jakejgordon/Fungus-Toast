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

            double decayStart = Time.realtimeSinceStartupAsDouble;
            bool fastPresentationMode = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode;

            double stepStart = Time.realtimeSinceStartupAsDouble;
            DeathEngine.ExecuteDeathCycle(activeBoard, failedGrowthsByPlayerId, rng, simulationObserver);
            LogPhaseTiming($"Decay start: ExecuteDeathCycle took {FormatElapsedMs(stepStart)} ms.");

            stepStart = Time.realtimeSinceStartupAsDouble;
            activeBoard.OnPostDecayPhase();
            LogPhaseTiming($"Decay start: OnPostDecayPhase callbacks took {FormatElapsedMs(stepStart)} ms.");

            float preRenderDelaySeconds = GameManager.Instance != null
                ? GameManager.Instance.GetRoundPresentationDelaySeconds(UIEffectConstants.TimeBeforeDecayRender)
                : UIEffectConstants.TimeBeforeDecayRender;
            yield return new WaitForSeconds(preRenderDelaySeconds);

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            stepStart = Time.realtimeSinceStartupAsDouble;
            activeGridVisualizer.RenderBoard(activeBoard, suppressAnimations: fastPresentationMode);
            LogPhaseTiming($"Decay phase: render after death processing took {FormatElapsedMs(stepStart)} ms.");

			stepStart = Time.realtimeSinceStartupAsDouble;
			yield return activeGridVisualizer.WaitForAllAnimations();
			LogPhaseTiming($"Decay phase: waited {FormatElapsedMs(stepStart)} ms for render animations.");

            if (septalAlarmResistanceTiles.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                int tileCount = septalAlarmResistanceTiles.Count;
                activeGridVisualizer.PlayResistancePulseBatchScaled(septalAlarmResistanceTiles, 0.45f);
                yield return activeGridVisualizer.WaitForAllAnimations();
                septalAlarmResistanceTiles.Clear();
                LogPhaseTiming($"Decay phase: Septal Alarm pulses ({tileCount} tiles) took {FormatElapsedMs(stepStart)} ms.");
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
                stepStart = Time.realtimeSinceStartupAsDouble;
                yield return activeSpecialEventPresentationService.PresentPendingAfterDecayRender();
                LogPhaseTiming($"Decay phase: post-render special event presentation took {FormatElapsedMs(stepStart)} ms.");
            }
            else
            {
                float postRenderDelaySeconds = GameManager.Instance != null
                    ? GameManager.Instance.GetRoundPresentationDelaySeconds(UIEffectConstants.TimeAfterDecayRender)
                    : UIEffectConstants.TimeAfterDecayRender;
                yield return new WaitForSeconds(postRenderDelaySeconds);
            }

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            LogPhaseTiming($"Decay phase total before round completion took {FormatElapsedMs(decayStart)} ms.");
            GameManager.Instance.OnRoundComplete();
        }

        // Define FT_PHASE_TIMING to re-enable these troubleshooting logs.
        [System.Diagnostics.Conditional("FT_PHASE_TIMING")]
        private static void LogPhaseTiming(string message)
        {
            Debug.Log($"[PhaseTiming] {message}");
        }

        private static string FormatElapsedMs(double startTime)
            => ((Time.realtimeSinceStartupAsDouble - startTime) * 1000d).ToString("F1");

    }

}
