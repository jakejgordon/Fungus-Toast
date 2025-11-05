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
    }
}
