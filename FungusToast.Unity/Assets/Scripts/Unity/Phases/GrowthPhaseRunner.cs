using FungusToast.Core;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using FungusToast.Unity;
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
        [SerializeField] private AudioClip growthCycleStartClip = null;
        [SerializeField, Range(0f, 1f)] private float growthCycleStartVolume = 1f;

        private GrowthPhaseProcessor processor;
        private GameBoard board;
        private GridVisualizer gridVisualizer;
        private bool isRunning = false;
        private System.Random rng = new();
        private RoundContext roundContext;
        private ISimulationObserver observer;
        private SpecialEventPresentationService specialEventPresentationService;
        public Dictionary<int, int> FailedGrowthsByPlayerId { get; private set; } = new();
        private int runVersion = 0;
        private AudioSource soundEffectAudioSource;

        private int phaseCycle = 0; // 1–5 for UI display

        public int CurrentCycle => phaseCycle;

        public void Initialize(GameBoard board, List<Player> players, GridVisualizer gridVisualizer)
        {
            runVersion++;
            this.board = board;
            this.gridVisualizer = gridVisualizer;
            EnsureSoundEffectAudioSource();
            this.observer = GameManager.Instance.GameUI.GameLogRouter;
            this.specialEventPresentationService = GameManager.Instance.SpecialEventPresentationService;
            this.processor = new GrowthPhaseProcessor(board, players, rng, observer);
            isRunning = false;
            phaseCycle = 0;
            FailedGrowthsByPlayerId.Clear();
            roundContext = null;
        }

        public void ResetForGameTransition()
        {
            runVersion++;
            StopAllCoroutines();

            isRunning = false;
            phaseCycle = 0;
            FailedGrowthsByPlayerId.Clear();
            roundContext = null;
            processor = null;
            board = null;
            gridVisualizer = null;
            observer = null;
            specialEventPresentationService = null;
        }

        public void StartGrowthPhase()
        {
            if (isRunning) return;

            runVersion++;
            int activeRunVersion = runVersion;
            isRunning = true;
            phaseCycle = 0;

            roundContext = new RoundContext();

            StartCoroutine(RunNextCycle(activeRunVersion));
        }

        private IEnumerator RunNextCycle(int activeRunVersion)
        {
            if (activeRunVersion != runVersion || board == null || gridVisualizer == null)
            {
                yield break;
            }

            if (phaseCycle >= GameBalance.TotalGrowthCycles)
            {
                double phaseCompletionStart = Time.realtimeSinceStartupAsDouble;

                // === Post-Growth Mutation Effects ===
                double stepStart = phaseCompletionStart;
                board.OnPostGrowthPhase();
                LogPhaseTiming($"Growth completion: OnPostGrowthPhase took {FormatElapsedMs(stepStart)} ms.");

                // Apply Hyphal Resistance Transfer effect after growth phase
                stepStart = Time.realtimeSinceStartupAsDouble;
                MycovariantEffectProcessor.OnPostGrowthPhase_HyphalResistanceTransfer(board, board.Players, rng, observer);
                LogPhaseTiming($"Growth completion: Hyphal Resistance Transfer processing took {FormatElapsedMs(stepStart)} ms.");

                // NOW signal completion so listeners (e.g. GameManager OnPostGrowthPhaseCompleted_CaptureHrt) run
                stepStart = Time.realtimeSinceStartupAsDouble;
                board.OnPostGrowthPhaseCompleted();
                LogPhaseTiming($"Growth completion: OnPostGrowthPhaseCompleted callbacks took {FormatElapsedMs(stepStart)} ms.");

                // Sort player summary rows at the end of growth phase
                stepStart = Time.realtimeSinceStartupAsDouble;
                GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(board.Players);
                LogPhaseTiming($"Growth completion: sidebar sort took {FormatElapsedMs(stepStart)} ms.");
                LogPhaseTiming($"Growth completion total before decay handoff took {FormatElapsedMs(phaseCompletionStart)} ms.");

                isRunning = false;

                // IMPORTANT: Do NOT start decay phase directly here.
                // GameManager's post-growth visual sequence coroutine will call StartDecayPhase()
                yield break;
            }

            phaseCycle++;
            PlayGrowthCycleStartSound();
            bool fastPresentationMode = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode;

            var failedThisCycle = processor.ExecuteSingleCycle(roundContext);
            MergeFailedGrowths(failedThisCycle);
            gridVisualizer.RenderBoard(board, suppressAnimations: fastPresentationMode);
            float cycleDelaySeconds = GameManager.Instance != null
                ? GameManager.Instance.GetRoundPresentationDelaySeconds(UIEffectConstants.TimeBetweenGrowthCycles)
                : UIEffectConstants.TimeBetweenGrowthCycles;
            yield return new WaitForSeconds(cycleDelaySeconds);

            if (specialEventPresentationService != null && specialEventPresentationService.HasPendingImmediateEvents)
            {
                yield return specialEventPresentationService.PresentPendingImmediate();
                gridVisualizer.RenderBoard(board, suppressAnimations: true);
            }

            // Update Occupancy in sidebar after each growth cycle
            int round = GameManager.Instance.Board.CurrentRound;
            float occupancy = GameManager.Instance.Board.GetOccupiedTileRatio() * 100f;
            GameManager.Instance.GameUI.RightSidebar.SetRoundAndOccupancy(round, occupancy);

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(phaseCycle);
            GameManager.Instance.GameUI.RightSidebar?.UpdatePlayerSummaries(board.Players);

            yield return new WaitForSeconds(cycleDelaySeconds);

            if (activeRunVersion != runVersion)
            {
                yield break;
            }

            StartCoroutine(RunNextCycle(activeRunVersion));
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

        private void EnsureSoundEffectAudioSource()
        {
            if (soundEffectAudioSource != null)
            {
                return;
            }

            soundEffectAudioSource = GetComponent<AudioSource>();
            if (soundEffectAudioSource == null)
            {
                soundEffectAudioSource = gameObject.AddComponent<AudioSource>();
            }

            soundEffectAudioSource.playOnAwake = false;
            soundEffectAudioSource.loop = false;
            soundEffectAudioSource.spatialBlend = 0f;
        }

        private void PlayGrowthCycleStartSound()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                if (gameManager.IsFastForwarding)
                {
                    return;
                }

                if (gameManager.IsFastRoundPresentationMode && phaseCycle > 1)
                {
                    return;
                }
            }

            if (growthCycleStartClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(growthCycleStartVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(growthCycleStartClip, effectiveVolume);
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
