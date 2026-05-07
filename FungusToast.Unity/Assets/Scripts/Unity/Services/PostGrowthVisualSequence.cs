using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI; // added for UIEffectConstants

namespace FungusToast.Unity
{
    public class PostGrowthVisualSequence
    {
        private readonly GameManager gameManager;
        private readonly GridVisualizer grid;
        private readonly System.Func<bool> shouldSkipPresentation;
        private readonly System.Action startDecayPhase;

        private readonly Dictionary<int, List<int>> regenReclaimBuffer = new();
        private readonly List<int> postGrowthResistanceTiles = new();
        private readonly List<int> postGrowthHrtNewResistantTiles = new();
        private readonly List<int> crustalCallusResistanceTiles = new();
        private readonly List<int> aegisHyphaeResistanceTiles = new();
        private readonly List<GameBoard.DirectedVectorSurgeEventArgs> directedVectorSurges = new();
        private HashSet<int> resistantBaseline = new();
        private bool sequenceRunning = false;
        private GameBoard registeredBoard;

        public PostGrowthVisualSequence(GameManager gm, GridVisualizer grid, System.Func<bool> skipPresentationFlag, System.Action startDecayPhase)
        {
            gameManager = gm;
            this.grid = grid;
            shouldSkipPresentation = skipPresentationFlag;
            this.startDecayPhase = startDecayPhase;
        }

        public void Register(GameBoard board)
        {
            Unregister();
            if (board == null)
            {
                return;
            }

            board.PostGrowthPhase += OnPostGrowthPhase_StartSequence;
            board.PostGrowthPhaseCompleted += OnPostGrowthPhaseCompleted_CaptureHrt;
            board.ResistanceAppliedBatch += OnResistanceAppliedBatch_Buffer;
            board.DirectedVectorSurge += OnDirectedVectorSurge_Buffer;
            registeredBoard = board;
        }

        public void Unregister(GameBoard board = null)
        {
            var boardToUnregister = board ?? registeredBoard;
            if (boardToUnregister == null)
            {
                return;
            }

            boardToUnregister.PostGrowthPhase -= OnPostGrowthPhase_StartSequence;
            boardToUnregister.PostGrowthPhaseCompleted -= OnPostGrowthPhaseCompleted_CaptureHrt;
            boardToUnregister.ResistanceAppliedBatch -= OnResistanceAppliedBatch_Buffer;
            boardToUnregister.DirectedVectorSurge -= OnDirectedVectorSurge_Buffer;

            if (ReferenceEquals(registeredBoard, boardToUnregister))
            {
                registeredBoard = null;
            }
        }

        public void ResetForGameTransition(GameBoard board = null)
        {
            Unregister(board);
            ClearBuffers();
            resistantBaseline.Clear();
            sequenceRunning = false;
        }

        private void OnResistanceAppliedBatch_Buffer(int playerId, GrowthSource source, IReadOnlyList<int> tileIds)
        {
            if (shouldSkipPresentation() || tileIds == null || tileIds.Count == 0)
            {
                return;
            }

            if (source == GrowthSource.ChitinFortification || source == GrowthSource.SurgicalInoculation)
            {
                return;
            }

            IReadOnlyList<int> filteredTileIds = source == GrowthSource.CrustalCallus
                ? FilterToNewlyGrownResistantTiles(tileIds)
                : tileIds;
            if (filteredTileIds.Count == 0)
            {
                return;
            }

            var buffer = source switch
            {
                GrowthSource.AegisHyphae => aegisHyphaeResistanceTiles,
                GrowthSource.CrustalCallus => crustalCallusResistanceTiles,
                _ => postGrowthResistanceTiles
            };
            foreach (var tileId in filteredTileIds)
            {
                if (!buffer.Contains(tileId))
                {
                    buffer.Add(tileId);
                }
            }

            if (source == GrowthSource.CrustalCallus)
            {
                grid.DeferResistanceOverlayReveal(filteredTileIds);
            }
        }

        private IReadOnlyList<int> FilterToNewlyGrownResistantTiles(IReadOnlyList<int> tileIds)
        {
            var filtered = new List<int>(tileIds.Count);
            for (int i = 0; i < tileIds.Count; i++)
            {
                int tileId = tileIds[i];
                var tile = gameManager.Board.GetTileById(tileId);
                var cell = tile?.FungalCell;
                if (cell?.IsAlive == true && cell.IsResistant && cell.IsNewlyGrown)
                {
                    filtered.Add(tileId);
                }
            }

            return filtered;
        }

        private void OnDirectedVectorSurge_Buffer(GameBoard.DirectedVectorSurgeEventArgs e)
        {
            if (shouldSkipPresentation() || e == null || e.AffectedTileCount <= 0)
            {
                return;
            }

            directedVectorSurges.Add(e);
        }

        private void OnPostGrowthPhase_StartSequence()
        {
            if (shouldSkipPresentation()) return;
            resistantBaseline = new HashSet<int>(gameManager.Board.AllTiles().Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant).Select(t => t.TileId));
        }

        private void OnPostGrowthPhaseCompleted_CaptureHrt()
        {
            if (shouldSkipPresentation())
            {
                postGrowthHrtNewResistantTiles.Clear();
                if (!sequenceRunning)
                {
                    sequenceRunning = true;
                    gameManager.StartCoroutine(RunSequence());
                }
                return;
            }

            var alreadyBufferedResistanceTiles = GetBufferedResistanceTileIds();
            var now = gameManager.Board.AllTiles().Where(t => t.FungalCell?.IsAlive == true && t.FungalCell.IsResistant).Select(t => t.TileId).ToList();
            postGrowthHrtNewResistantTiles.Clear();
            foreach (var id in now)
            {
                if (!resistantBaseline.Contains(id) && !alreadyBufferedResistanceTiles.Contains(id))
                {
                    postGrowthHrtNewResistantTiles.Add(id);
                }
            }
            if (!sequenceRunning) { sequenceRunning = true; gameManager.StartCoroutine(RunSequence()); }
        }

        private HashSet<int> GetBufferedResistanceTileIds()
        {
            var bufferedTileIds = new HashSet<int>();

            foreach (var tileId in postGrowthResistanceTiles)
            {
                bufferedTileIds.Add(tileId);
            }

            foreach (var tileId in crustalCallusResistanceTiles)
            {
                bufferedTileIds.Add(tileId);
            }

            foreach (var tileId in aegisHyphaeResistanceTiles)
            {
                bufferedTileIds.Add(tileId);
            }

            return bufferedTileIds;
        }

        private IEnumerator RunSequence()
        {
            double sequenceStart = Time.realtimeSinceStartupAsDouble;
            if (shouldSkipPresentation()) { ClearBuffers(); sequenceRunning = false; LogPhaseTiming($"Post-growth sequence skipped after {FormatElapsedMs(sequenceStart)} ms."); startDecayPhase(); yield break; }

            double stepStart = Time.realtimeSinceStartupAsDouble;
            yield return grid.WaitForAllAnimations();
            LogPhaseTiming($"Post-growth sequence: waited {FormatElapsedMs(stepStart)} ms for in-flight animations before buffered effects.");

            stepStart = Time.realtimeSinceStartupAsDouble;
            grid.RenderBoard(gameManager.Board, suppressAnimations: true);
            LogPhaseTiming($"Post-growth sequence: suppress-animation render took {FormatElapsedMs(stepStart)} ms.");

            // (Currently regen + resistance placeholders; actual population of buffers would be wired similarly to original code)
            if (regenReclaimBuffer.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                foreach (var kvp in regenReclaimBuffer)
                {
                    var ids = kvp.Value; if (ids.Count == 0) continue; grid.PlayRegenerativeHyphaeReclaimBatch(ids, 1f, UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds);
                }
                yield return grid.WaitForAllAnimations();
                LogPhaseTiming($"Post-growth sequence: regenerative reclaim batch ({regenReclaimBuffer.Sum(entry => entry.Value.Count)} tiles) took {FormatElapsedMs(stepStart)} ms.");
                regenReclaimBuffer.Clear();
            }
            if (directedVectorSurges.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                bool multipleSurges = directedVectorSurges.Count > 1;
                foreach (var surge in directedVectorSurges)
                {
                    if (surge.AffectedTileCount <= 0)
                    {
                        continue;
                    }

                    grid.PlayDirectedVectorSurgePresentation(surge.PlayerId, surge.OriginTileId, surge.AffectedTileIds);
                    gameManager.GameUI?.GameLogRouter?.RecordDirectedVectorSurge(surge.PlayerId, surge.AffectedTileCount);

                    if (!multipleSurges)
                    {
                        gameManager.GameUI?.PhaseBanner?.Show(
                            "Chemotactic vectoring!",
                            UIEffectConstants.DirectedVectorBannerHoldSeconds);
                    }
                }

                if (multipleSurges)
                {
                    gameManager.GameUI?.PhaseBanner?.Show(
                        "Chemotactic vectoring!",
                        UIEffectConstants.DirectedVectorBannerHoldSeconds);
                }

                directedVectorSurges.Clear();
                LogPhaseTiming($"Post-growth sequence: directed vector surge presentation took {FormatElapsedMs(stepStart)} ms.");
            }
            if (postGrowthResistanceTiles.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                int tileCount = postGrowthResistanceTiles.Count;
                grid.PlayResistancePulseBatchScaled(postGrowthResistanceTiles, 0.5f); yield return grid.WaitForAllAnimations(); postGrowthResistanceTiles.Clear();
                LogPhaseTiming($"Post-growth sequence: general resistance pulses ({tileCount} tiles) took {FormatElapsedMs(stepStart)} ms.");
            }
            if (crustalCallusResistanceTiles.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                int tileCount = crustalCallusResistanceTiles.Count;
                gameManager.GameUI?.GameLogRouter?.RecordCrustalCallusResistance(gameManager.GetPrimaryHumanInternal().PlayerId, crustalCallusResistanceTiles.Count);
                grid.PlayResistancePulseBatchScaled(crustalCallusResistanceTiles, 0.5f); yield return grid.WaitForAllAnimations(); crustalCallusResistanceTiles.Clear();
                LogPhaseTiming($"Post-growth sequence: Crustal Callus resistance pulses ({tileCount} tiles) took {FormatElapsedMs(stepStart)} ms.");
            }
            if (aegisHyphaeResistanceTiles.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                int tileCount = aegisHyphaeResistanceTiles.Count;
                gameManager.GameUI?.PhaseBanner?.Show(
                    aegisHyphaeResistanceTiles.Count == 1
                        ? "Aegis Hyphae fortifies 1 new cell!"
                        : $"Aegis Hyphae fortifies {aegisHyphaeResistanceTiles.Count} new cells!",
                    UIEffectConstants.AegisHyphaeBannerHoldSeconds);
                gameManager.GameUI?.GameLogRouter?.RecordAegisHyphaeResistance(gameManager.GetPrimaryHumanInternal().PlayerId, aegisHyphaeResistanceTiles.Count);
                grid.PlayResistancePulseBatchScaled(aegisHyphaeResistanceTiles, 0.65f); yield return grid.WaitForAllAnimations(); aegisHyphaeResistanceTiles.Clear();
                LogPhaseTiming($"Post-growth sequence: Aegis Hyphae resistance pulses ({tileCount} tiles) took {FormatElapsedMs(stepStart)} ms.");
            }
            bool anyPlayerHasHrt = gameManager.Board.Players.Any(p => p.GetMycovariant(MycovariantIds.HyphalResistanceTransferId) != null);
            if (anyPlayerHasHrt && postGrowthHrtNewResistantTiles.Count > 0)
            {
                stepStart = Time.realtimeSinceStartupAsDouble;
                int tileCount = postGrowthHrtNewResistantTiles.Count;
                grid.PlayResistancePulseBatchScaled(postGrowthHrtNewResistantTiles, 0.35f); yield return grid.WaitForAllAnimations();
                LogPhaseTiming($"Post-growth sequence: Hyphal Resistance Transfer pulses ({tileCount} tiles) took {FormatElapsedMs(stepStart)} ms.");
            }
            postGrowthHrtNewResistantTiles.Clear(); sequenceRunning = false; LogPhaseTiming($"Post-growth sequence total before decay start took {FormatElapsedMs(sequenceStart)} ms."); startDecayPhase();
        }

        private void ClearBuffers()
        {
            grid.RevealDeferredResistanceOverlays(crustalCallusResistanceTiles);
            regenReclaimBuffer.Clear(); postGrowthResistanceTiles.Clear(); postGrowthHrtNewResistantTiles.Clear(); crustalCallusResistanceTiles.Clear(); aegisHyphaeResistanceTiles.Clear(); directedVectorSurges.Clear();
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
