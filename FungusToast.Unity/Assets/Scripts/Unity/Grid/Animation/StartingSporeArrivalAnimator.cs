using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Grid.Animation
{
    /// <summary>
    /// Intro animation for starting resistant cells: arcs a composite (mold + shield) down then plays a composite drop
    /// (instead of the shield-only ResistantDropAnimation) so the mold icon remains visible beneath the shield throughout.
    /// </summary>
    internal class StartingSporeArrivalAnimator
    {
        private readonly GridVisualizer _viz;
        public StartingSporeArrivalAnimator(GridVisualizer viz) => _viz = viz;

        private const float ArcShieldRelativeScale = 0.45f; // reduced so mold sprite shows clearly underneath
        private const float ArcSpinTurns = 0.9f;            // total turns while traveling
        private const float ArcBaseExtraHeight = 1.5f;      // extra height over surgical inoculation arc
        private const float CompositeDropFinalScale = 0.5f; // matches prior shield-only call (0.5f)
        private const float ArcScaleGlobalMultiplier = 0.5f; // NEW: shrink arc visual (peak + entire profile) ~50%
        private const float DropStartScaleMultiplier = 0.5f; // NEW: shrink initial huge drop scale ~50%

        public IEnumerator Play(IEnumerable<int> startingTileIds)
        {
            var ids = startingTileIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0) yield break;
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var shieldSprite = _viz.goldShieldOverlayTile != null ? _viz.goldShieldOverlayTile.sprite : null;
            if (shieldSprite == null) yield break;

            int startX = board.Width / 2;
            int startY = board.Height + Mathf.CeilToInt(board.Height * 0.35f);
            var startCell = new Vector3Int(startX, startY, 0);

            foreach (var tileId in ids)
            {
                _viz.StartCoroutine(AnimateSingleArrival(tileId, startCell, shieldSprite));
                yield return new WaitForSeconds(0.18f);
            }
            yield return _viz.WaitForAllAnimations();
        }

        private IEnumerator AnimateSingleArrival(int tileId, Vector3Int startCell, Sprite shieldSprite)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (tx, ty) = board.GetXYFromTileId(tileId);
            var endCell = new Vector3Int(tx, ty, 0);

            Sprite moldSprite = null;
            var tile = board.GetTileById(tileId);
            int playerId = tile?.FungalCell?.OwnerPlayerId ?? -1;
            if (playerId >= 0)
            {
                var playerTile = _viz.GetTileForPlayer(playerId);
                if (playerTile != null) moldSprite = playerTile.sprite;
            }

            _viz.BeginAnimation();
            try
            {
                // Hide underlying tiles while composite animates
                if (_viz.moldTilemap.HasTile(endCell))
                { var c = _viz.moldTilemap.GetColor(endCell); c.a = 0f; _viz.moldTilemap.SetColor(endCell, c); }
                if (_viz.overlayTilemap.HasTile(endCell))
                { var c = _viz.overlayTilemap.GetColor(endCell); c.a = 0f; _viz.overlayTilemap.SetColor(endCell, c); }

                float arcDuration = UI.UIEffectConstants.SurgicalInoculationArcDurationSeconds;
                float baseHeight = UI.UIEffectConstants.SurgicalInoculationArcBaseHeightWorld + ArcBaseExtraHeight;
                float perTileHeight = UI.UIEffectConstants.SurgicalInoculationArcHeightPerTile;

                // Run arc retaining composite parent
                GameObject compositeParent = null;
                yield return AnimateCompositeArcRetained(startCell, endCell, moldSprite, shieldSprite, arcDuration, baseHeight, perTileHeight, p => compositeParent = p);

                // Safety: if arc failed fall back to legacy shield drop
                if (compositeParent == null)
                {
                    // Restore tiles then fallback drop (shield-only)
                    if (_viz.moldTilemap.HasTile(endCell)) { var c = _viz.moldTilemap.GetColor(endCell); c.a = 1f; _viz.moldTilemap.SetColor(endCell, c); }
                    if (_viz.overlayTilemap.HasTile(endCell)) { var c = _viz.overlayTilemap.GetColor(endCell); c.a = 0f; _viz.overlayTilemap.SetColor(endCell, c); }
                    yield return _viz.ResistantDropAnimation(tileId, CompositeDropFinalScale);
                }
                else
                {
                    // Perform composite drop animation (keeps mold visible behind shield)
                    yield return CompositeDropSequence(compositeParent, endCell, CompositeDropFinalScale);
                }

                // Restore underlying tile visuals (mold + shield overlay)
                if (_viz.moldTilemap.HasTile(endCell))
                { var c = _viz.moldTilemap.GetColor(endCell); c.a = 1f; _viz.moldTilemap.SetColor(endCell, c); }
                if (_viz.goldShieldOverlayTile != null)
                {
                    _viz.overlayTilemap.SetTile(endCell, _viz.goldShieldOverlayTile);
                    _viz.overlayTilemap.SetTileFlags(endCell, UnityEngine.Tilemaps.TileFlags.None);
                    _viz.overlayTilemap.SetColor(endCell, Color.white);
                }
            }
            finally { _viz.EndAnimation(); }
        }

        /// <summary>
        /// Arc animation that retains the composite parent GameObject (does not destroy it) and returns it via callback.
        /// </summary>
        private IEnumerator AnimateCompositeArcRetained(
            Vector3Int startCell,
            Vector3Int endCell,
            Sprite moldSprite,
            Sprite shieldSprite,
            float duration,
            float baseArcHeightWorld,
            float arcHeightPerTile,
            System.Action<GameObject> onReady)
        {
            if (_viz == null || shieldSprite == null) yield break;
            var refTilemap = _viz.overlayTilemap != null ? _viz.overlayTilemap : _viz.moldTilemap; if (refTilemap == null) yield break;

            var parent = new GameObject("StartingSporeCompositeArc");
            parent.transform.SetParent(refTilemap.transform, false);

            // Mold base
            var moldGO = new GameObject("MoldBase");
            moldGO.transform.SetParent(parent.transform, false);
            SpriteRenderer moldSR = null;
            if (moldSprite != null)
            {
                moldSR = moldGO.AddComponent<SpriteRenderer>();
                moldSR.sprite = moldSprite;
            }
            // Shield overlay (scaled down so mold visible)
            var shieldGO = new GameObject("ShieldOverlay");
            shieldGO.transform.SetParent(parent.transform, false);
            var shieldSR = shieldGO.AddComponent<SpriteRenderer>();
            shieldSR.sprite = shieldSprite;
            shieldGO.transform.localScale = new Vector3(ArcShieldRelativeScale, ArcShieldRelativeScale, 1f);

            // Sorting
            var tmr = refTilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmr != null)
            {
                if (moldSR != null)
                {
                    moldSR.sortingLayerID = tmr.sortingLayerID; moldSR.sortingOrder = tmr.sortingOrder + 9;
                }
                shieldSR.sortingLayerID = tmr.sortingLayerID; shieldSR.sortingOrder = tmr.sortingOrder + 10;
            }

            Vector3 startWorld = CellCenterWorld(refTilemap, startCell);
            Vector3 endWorld = CellCenterWorld(refTilemap, endCell);
            float distanceTiles = Vector2.Distance(new Vector2(startCell.x, startCell.y), new Vector2(endCell.x, endCell.y));
            float arcHeightWorld = baseArcHeightWorld + distanceTiles * arcHeightPerTile * refTilemap.cellSize.y;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime; float u = Mathf.Clamp01(t / duration);
                float hNorm = 4f * u * (1f - u); float height = hNorm * arcHeightWorld;
                parent.transform.position = Vector3.Lerp(startWorld, endWorld, u) + Vector3.up * height;

                float peakScale = Mathf.Max(1f, UI.UIEffectConstants.SurgicalInoculationArcPeakScale) * ArcScaleGlobalMultiplier;
                float scaleEase = 1f - Mathf.Pow(1f - hNorm, 2f);
                float scale = Mathf.Lerp(1f * ArcScaleGlobalMultiplier, peakScale, scaleEase);
                parent.transform.localScale = new Vector3(scale, scale, 1f);

                float angle = u * 360f * ArcSpinTurns;
                parent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

                yield return null;
            }
            // Position parent exactly at end cell center; reset rotation for drop continuity
            parent.transform.position = CellCenterWorld(refTilemap, endCell);
            parent.transform.localRotation = Quaternion.identity;
            onReady?.Invoke(parent);
        }

        /// <summary>
        /// Performs a composite drop (shield + mold) replicating ResistantDropAnimation phases.
        /// </summary>
        private IEnumerator CompositeDropSequence(GameObject parent, Vector3Int endCell, float finalScale)
        {
            if (parent == null) yield break;
            var posWorld = parent.transform.position;

            float total = UI.UIEffectConstants.SurgicalInoculationDropDurationSeconds;
            float dropT = Mathf.Clamp01(UI.UIEffectConstants.SurgicalInoculationDropPortion);
            float impactT = Mathf.Clamp01(UI.UIEffectConstants.SurgicalInoculationImpactPortion);
            float settleT = Mathf.Clamp01(UI.UIEffectConstants.SurgicalInoculationSettlePortion);
            float normSum = dropT + impactT + settleT; if (normSum <= 0f) normSum = 1f;
            dropT /= normSum; impactT /= normSum; settleT /= normSum;
            float dropDur = total * dropT; float impactDur = total * impactT; float settleDur = total * settleT;

            // Phase 1: Drop (from high offset + huge scale + spin -> finalScale)
            float startYOffset = UI.UIEffectConstants.SurgicalInoculationDropStartYOffset;
            float startScale = UI.UIEffectConstants.SurgicalInoculationDropStartScale * finalScale * DropStartScaleMultiplier; // reduced start size
            float spinTurns = UI.UIEffectConstants.SurgicalInoculationDropSpinTurns;

            float t = 0f;
            while (t < dropDur)
            {
                t += Time.deltaTime; float u = Mathf.Clamp01(t / dropDur); float eased = u * u * u;
                float yOff = Mathf.Lerp(startYOffset, 0f, eased);
                float s = Mathf.Lerp(startScale, finalScale, eased);
                float angle = Mathf.Lerp(0f, 360f * spinTurns, eased);
                parent.transform.position = new Vector3(posWorld.x, posWorld.y + yOff, posWorld.z);
                parent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                parent.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            // Phase 2: Impact squash (ring pulse) - we imitate ring but optional: call internal pulse via overlay tilemap if available
            float squashX = UI.UIEffectConstants.SurgicalInoculationImpactSquashX * finalScale;
            float squashY = UI.UIEffectConstants.SurgicalInoculationImpactSquashY * finalScale;
            t = 0f;
            // Trigger ring pulse via GridVisualizer private method wrapper (use coroutine if accessible)
            _viz.StartCoroutine(ImpactRingPulseWrapper(endCell));
            while (t < impactDur)
            {
                t += Time.deltaTime; float u = Mathf.Clamp01(t / impactDur); float eased = 1f - (1f - u) * (1f - u);
                float sx = Mathf.Lerp(finalScale, squashX, eased);
                float sy = Mathf.Lerp(finalScale, squashY, eased);
                parent.transform.position = posWorld; // ensure landed
                parent.transform.localRotation = Quaternion.identity;
                parent.transform.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            // Phase 3: Settle back to finalScale
            t = 0f;
            while (t < settleDur)
            {
                t += Time.deltaTime; float u = Mathf.Clamp01(t / settleDur);
                float sx = Mathf.Lerp(squashX, finalScale, u);
                float sy = Mathf.Lerp(squashY, finalScale, u);
                parent.transform.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            parent.transform.localScale = new Vector3(finalScale, finalScale, 1f);
            Object.Destroy(parent);
        }

        // Wrapper to trigger internal ring pulse effect (non-blocking). If inaccessible, silently ignores.
        private IEnumerator ImpactRingPulseWrapper(Vector3Int cell)
        {
            // Use overlay tilemap center for pulse visual (simulate what ResistantDropAnimation does)
            // We cannot directly call private ImpactRingPulse, so just perform a lightweight placeholder pulse or skip.
            // (If ring highlight helper is internal, we skip; goal is mainly composite drop look.)
            // Simple scale flicker pulse: quick flash alpha on overlay tile if exists.
            float dur = UI.UIEffectConstants.SurgicalInoculationRingPulseDurationSeconds * 0.6f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime; yield return null;
            }
        }

        private Vector3 CellCenterWorld(UnityEngine.Tilemaps.Tilemap tm, Vector3Int cell)
        { var baseWorld = tm.CellToWorld(cell); var cs = tm.cellSize; return baseWorld + new Vector3(cs.x * 0.5f, cs.y * 0.5f, 0f); }
    }
}
