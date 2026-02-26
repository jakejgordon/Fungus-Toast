using System;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Orchestrates active campaign lifecycle and exposes current level spec.
    /// </summary>
    public class CampaignController
    {
        private readonly CampaignProgression progression;
        public CampaignState State { get; private set; }

        public CampaignController(CampaignProgression progression)
        {
            this.progression = progression;
        }

        public bool HasActiveRun => State != null;
        public CampaignProgression.LevelSpec CurrentLevelSpec => (State != null && State.levelIndex < progression.MaxLevels) ? progression.Get(State.levelIndex) : null;
        public BoardPreset CurrentBoardPreset => CurrentLevelSpec?.boardPreset;

        public void StartNew()
        {
            if (progression.MaxLevels == 0) throw new InvalidOperationException("CampaignProgression has no levels defined.");
            var firstSpec = progression.Get(0);
            var preset = firstSpec.boardPreset;
            if (preset == null) throw new InvalidOperationException("Level0 has no BoardPreset assigned.");
            State = new CampaignState
            {
                runId = Guid.NewGuid().ToString(),
                levelIndex = 0,
                unlockedMutationTierMax = preset.mutationTierMax,
                boardPresetId = preset.presetId,
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                boardWidth = preset.boardWidth,
                boardHeight = preset.boardHeight
            };
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] New campaign started. RunId={State.runId} Preset={preset.presetId}");
        }

        public void Resume()
        {
            var loaded = CampaignSaveService.Load();
            if (loaded == null)
            {
                Debug.LogWarning("[CampaignController] Resume requested but no save exists. Starting new.");
                StartNew();
                return;
            }
            State = loaded;
            Debug.Log($"[CampaignController] Resumed campaign RunId={State.runId} Level={State.levelIndex} PresetId={State.boardPresetId}");
        }

        public void Delete()
        {
            CampaignSaveService.Delete();
            State = null;
        }

        /// <summary>
        /// Called by GameManager when a campaign level ends. Handles victory progression or defeat reset.
        /// </summary>
        public void OnGameFinished(bool victory)
        {
            if (State == null)
            {
                return;
            }
            if (!victory)
            {
                ResetRunAfterDefeat();
                return;
            }

            // Victory path
            int nextIndex = State.levelIndex + 1;
            if (nextIndex >= progression.MaxLevels)
            {
                // Final victory – leave state as is (completed flag deferred to later iteration)
                CampaignSaveService.Save(State);
                Debug.Log($"[CampaignController] Campaign completed! RunId={State.runId} Levels={progression.MaxLevels}");
                return;
            }
            AdvanceToNextLevel();
        }

        /// <summary>
        /// Advance to the next level (mid-run victory).
        /// </summary>
        private void AdvanceToNextLevel()
        {
            int targetIndex = State.levelIndex + 1;
            if (targetIndex >= progression.MaxLevels)
            {
                Debug.LogWarning("[CampaignController] AdvanceToNextLevel called but already at final level.");
                return;
            }
            var spec = progression.Get(targetIndex);
            if (spec.boardPreset == null)
            {
                Debug.LogError($"[CampaignController] Level {targetIndex} has no BoardPreset – aborting advance.");
                return;
            }
            var preset = spec.boardPreset;
            State.levelIndex = targetIndex;
            State.boardPresetId = preset.presetId;
            State.unlockedMutationTierMax = preset.mutationTierMax;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            // Seed retained across victories for reproducibility
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Advanced to level {State.levelIndex}. Preset={preset.presetId}");
        }

        /// <summary>
        /// Reset the run after defeat (player returns to mode select with fresh level0 state).
        /// </summary>
        private void ResetRunAfterDefeat()
        {
            if (progression.MaxLevels == 0)
            {
                Debug.LogError("[CampaignController] Cannot reset campaign – no levels defined.");
                return;
            }
            var firstSpec = progression.Get(0);
            if (firstSpec.boardPreset == null)
            {
                Debug.LogError("[CampaignController] Cannot reset campaign – Level0 preset missing.");
                return;
            }
            var preset = firstSpec.boardPreset;
            State.runId = Guid.NewGuid().ToString();
            State.levelIndex = 0;
            State.traitStacks.Clear(); // Future trait persistence – cleared on defeat
            State.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            State.boardPresetId = preset.presetId;
            State.unlockedMutationTierMax = preset.mutationTierMax;
            State.boardWidth = preset.boardWidth;
            State.boardHeight = preset.boardHeight;
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] Run reset after defeat. New RunId={State.runId} Preset={preset.presetId}");
        }
    }
}
