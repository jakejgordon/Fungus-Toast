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

            GameManager.Instance.GameUI.PhaseProgressTracker?.AdvanceToNextGrowthCycle(1);
            GameManager.Instance.GameUI.PhaseBanner.Show("Growth Phase Begins!", 2f);

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
                // === Post-Growth Mutation Effects ===
                board.OnPostGrowthPhase();

                // Apply Hyphal Resistance Transfer effect after growth phase
                MycovariantEffectProcessor.OnPostGrowthPhase_HyphalResistanceTransfer(board, board.Players, rng, observer);

                // NOW signal completion so listeners (e.g. GameManager OnPostGrowthPhaseCompleted_CaptureHrt) run
                board.OnPostGrowthPhaseCompleted();

                // Sort player summary rows at the end of growth phase
                GameManager.Instance.GameUI.RightSidebar?.SortPlayerSummaryRows(board.Players);

                isRunning = false;

                // IMPORTANT: Do NOT start decay phase directly here.
                // GameManager's post-growth visual sequence coroutine will call StartDecayPhase()
                yield break;
            }

            phaseCycle++;
            PlayGrowthCycleStartSound();

            var failedThisCycle = processor.ExecuteSingleCycle(roundContext);
            MergeFailedGrowths(failedThisCycle);
            gridVisualizer.RenderBoard(board);

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

            yield return new WaitForSeconds(UIEffectConstants.TimeBetweenGrowthCycles);

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

    }
}
