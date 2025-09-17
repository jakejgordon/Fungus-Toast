using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Board;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI; // added for UIEffectConstants

namespace FungusToast.Unity
{
    public class PostGrowthVisualSequence
    {
        private readonly GameManager gameManager;
        private readonly GridVisualizer grid;
        private readonly System.Func<bool> isFastForwarding;
        private readonly System.Action startDecayPhase;

        private readonly Dictionary<int, List<int>> regenReclaimBuffer = new();
        private readonly List<int> postGrowthResistanceTiles = new();
        private readonly List<int> postGrowthHrtNewResistantTiles = new();
        private HashSet<int> resistantBaseline = new();
        private bool sequenceRunning = false;

        public PostGrowthVisualSequence(GameManager gm, GridVisualizer grid, System.Func<bool> fastForwardFlag, System.Action startDecayPhase)
        { gameManager = gm; this.grid = grid; isFastForwarding = fastForwardFlag; this.startDecayPhase = startDecayPhase; }

        public void Register(GameBoard board)
        {
            board.PostGrowthPhase += OnPostGrowthPhase_StartSequence;
            board.PostGrowthPhaseCompleted += OnPostGrowthPhaseCompleted_CaptureHrt;
        }

        private void OnPostGrowthPhase_StartSequence()
        {
            if (isFastForwarding()) return;
            resistantBaseline = new HashSet<int>(gameManager.Board.AllTiles().Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant).Select(t => t.TileId));
        }

        private void OnPostGrowthPhaseCompleted_CaptureHrt()
        {
            if (isFastForwarding()) { postGrowthHrtNewResistantTiles.Clear(); return; }
            var now = gameManager.Board.AllTiles().Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant).Select(t => t.TileId).ToList();
            postGrowthHrtNewResistantTiles.Clear(); foreach (var id in now) if (!resistantBaseline.Contains(id)) postGrowthHrtNewResistantTiles.Add(id);
            if (!sequenceRunning) { sequenceRunning = true; gameManager.StartCoroutine(RunSequence()); }
        }

        private IEnumerator RunSequence()
        {
            if (isFastForwarding()) { ClearBuffers(); sequenceRunning = false; startDecayPhase(); yield break; }
            // (Currently regen + resistance placeholders; actual population of buffers would be wired similarly to original code)
            if (regenReclaimBuffer.Count > 0)
            {
                foreach (var kvp in regenReclaimBuffer)
                {
                    var ids = kvp.Value; if (ids.Count == 0) continue; grid.PlayRegenerativeHyphaeReclaimBatch(ids, 1f, UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds);
                }
                yield return grid.WaitForAllAnimations();
                regenReclaimBuffer.Clear();
            }
            if (postGrowthResistanceTiles.Count > 0)
            {
                grid.PlayResistancePulseBatchScaled(postGrowthResistanceTiles, 0.5f); yield return grid.WaitForAllAnimations(); postGrowthResistanceTiles.Clear();
            }
            bool anyPlayerHasHrt = gameManager.Board.Players.Any(p => p.GetMycovariant(MycovariantIds.HyphalResistanceTransferId) != null);
            if (anyPlayerHasHrt && postGrowthHrtNewResistantTiles.Count > 0)
            {
                grid.PlayResistancePulseBatchScaled(postGrowthHrtNewResistantTiles, 0.35f); yield return grid.WaitForAllAnimations();
            }
            postGrowthHrtNewResistantTiles.Clear(); sequenceRunning = false; startDecayPhase();
        }

        private void ClearBuffers()
        {
            regenReclaimBuffer.Clear(); postGrowthResistanceTiles.Clear(); postGrowthHrtNewResistantTiles.Clear();
        }
    }
}
