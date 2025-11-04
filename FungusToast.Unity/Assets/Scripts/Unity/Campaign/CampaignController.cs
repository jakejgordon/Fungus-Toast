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

        public void StartNew()
        {
            if (progression.MaxLevels == 0) throw new InvalidOperationException("CampaignProgression has no levels defined.");
            var first = progression.Get(0);
            State = new CampaignState
            {
                runId = Guid.NewGuid().ToString(),
                levelIndex = 0,
                unlockedMutationTierMax = first.mutationTierMax,
                boardPresetId = first.boardPresetId,
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            };
            CampaignSaveService.Save(State);
            Debug.Log($"[CampaignController] New campaign started. RunId={State.runId}");
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
            Debug.Log($"[CampaignController] Resumed campaign RunId={State.runId} Level={State.levelIndex}");
        }

        public void Delete()
        {
            CampaignSaveService.Delete();
            State = null;
        }
    }
}
