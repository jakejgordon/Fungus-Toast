using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FungusToast.Unity.UI;

namespace FungusToast.Unity.Grid.Animation
{
    internal class RegenerativeHyphaeReclaimAnimator
    {
        private readonly GridVisualizer _viz;
        public RegenerativeHyphaeReclaimAnimator(GridVisualizer viz) => _viz = viz;

        // Single public entry point (no simplified flag). explicitTotalSeconds <= 0 uses constant.
        public void PlayBatch(IReadOnlyList<int> tileIds, float scaleMultiplier, float explicitTotalSeconds)
        {
            if (tileIds == null || tileIds.Count == 0) return;
            float appliedTotal = explicitTotalSeconds > 0 ? explicitTotalSeconds : UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds;
            foreach (var id in tileIds)
            {
                _viz.StartCoroutine(Reclaim(id, scaleMultiplier, appliedTotal));
            }
        }

        // Unified (former lite) animation
        private IEnumerator Reclaim(int tileId, float scaleMult, float totalDuration)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (x, y) = board.GetXYFromTileId(tileId); var pos = new Vector3Int(x, y, 0);
            if (!_viz.moldTilemap.HasTile(pos)) yield break;

            float baseRise = UIEffectConstants.RegenerativeHyphaeRiseDurationSeconds;
            float baseSwap = UIEffectConstants.RegenerativeHyphaeFadeSwapDurationSeconds;
            float totalBase = baseRise + baseSwap; if (totalBase <= 0) totalBase = 1f;
            float riseDur = totalDuration * (baseRise / totalBase);
            float swapDur = totalDuration * (baseSwap / totalBase);

            float maxScale = Mathf.Lerp(1f, UIEffectConstants.RegenerativeHyphaeMaxScale, 0.55f) * Mathf.Max(0.1f, scaleMult);
            float lift = UIEffectConstants.RegenerativeHyphaeLiftOffset * 0.7f * Mathf.Max(0.1f, scaleMult);

            Color startOverlay = _viz.overlayTilemap.HasTile(pos) ? _viz.overlayTilemap.GetColor(pos) : Color.clear;
            Color startMold = _viz.moldTilemap.GetColor(pos);

            _viz.BeginAnimation();
            try
            {
                float t = 0f; // Rise
                while (t < riseDur)
                {
                    t += Time.deltaTime; float u = Mathf.Clamp01(t / riseDur); float ease = 1f - (1f - u) * (1f - u);
                    float s = Mathf.Lerp(1f, maxScale, ease); float yOff = Mathf.Lerp(0f, lift, ease);
                    ApplyComposite(pos, new Vector3(0f, yOff, 0f), new Vector3(s, s, 1f));
                    yield return null;
                }
                t = 0f; // Swap/settle merge
                while (t < swapDur)
                {
                    t += Time.deltaTime; float u = Mathf.Clamp01(t / swapDur); float ease = 1f - (1f - u) * (1f - u);
                    if (_viz.overlayTilemap.HasTile(pos)) { var oc = _viz.overlayTilemap.GetColor(pos); oc.a = Mathf.Lerp(startOverlay.a, 0f, ease); _viz.overlayTilemap.SetColor(pos, oc); }
                    if (_viz.moldTilemap.HasTile(pos)) { var mc = _viz.moldTilemap.GetColor(pos); mc.a = Mathf.Lerp(startMold.a, 1f, ease); _viz.moldTilemap.SetColor(pos, mc); }
                    float s2 = Mathf.Lerp(maxScale, 1f, ease * 0.6f); float y2 = Mathf.Lerp(lift, 0f, ease);
                    ApplyComposite(pos, new Vector3(0f, y2, 0f), new Vector3(s2, s2, 1f));
                    yield return null;
                }
                ApplyComposite(pos, Vector3.zero, Vector3.one);
            }
            finally { _viz.EndAnimation(); }
        }

        private void ApplyComposite(Vector3Int pos, Vector3 localOffset, Vector3 localScale)
        {
            var m = Matrix4x4.TRS(localOffset, Quaternion.identity, localScale);
            if (_viz.moldTilemap.HasTile(pos)) _viz.moldTilemap.SetTransformMatrix(pos, m);
            if (_viz.overlayTilemap.HasTile(pos)) _viz.overlayTilemap.SetTransformMatrix(pos, m);
        }
    }
}
