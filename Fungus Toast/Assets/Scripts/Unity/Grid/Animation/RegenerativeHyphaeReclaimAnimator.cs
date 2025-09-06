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

        public void PlayBatch(IReadOnlyList<int> tileIds, bool simplified, float scaleMultiplier, float explicitTotalSeconds)
        {
            if (tileIds == null || tileIds.Count == 0) return;
            foreach (var id in tileIds)
            {
                if (simplified) _viz.StartCoroutine(ReclaimLite(id, scaleMultiplier, explicitTotalSeconds));
                else _viz.StartCoroutine(ReclaimFull(id, scaleMultiplier, explicitTotalSeconds));
            }
        }

        private IEnumerator ReclaimFull(int tileId, float scaleMult, float explicitTotal)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (x, y) = board.GetXYFromTileId(tileId); var pos = new Vector3Int(x, y, 0);
            if (!_viz.moldTilemap.HasTile(pos)) yield break;

            float baseRise = UIEffectConstants.RegenerativeHyphaeRiseDurationSeconds;
            float baseSwap = UIEffectConstants.RegenerativeHyphaeFadeSwapDurationSeconds;
            float baseSettle = UIEffectConstants.RegenerativeHyphaeSettleDurationSeconds;
            float baseHold = UIEffectConstants.RegenerativeHyphaeHoldBaseSeconds;
            float totalBase = baseRise + baseHold + baseSwap + baseSettle; if (totalBase <= 0f) totalBase = 1f;
            float total = explicitTotal > 0 ? explicitTotal : UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds;

            float riseDur = total * (baseRise / totalBase);
            float holdDur = total * (baseHold / totalBase);
            float swapDur = total * (baseSwap / totalBase);
            float settleDur = total * (baseSettle / totalBase);

            float maxScale = UIEffectConstants.RegenerativeHyphaeMaxScale * Mathf.Max(0.1f, scaleMult);
            float overshoot = UIEffectConstants.RegenerativeHyphaeOvershootScale * Mathf.Max(0.1f, scaleMult);
            float lift = UIEffectConstants.RegenerativeHyphaeLiftOffset * Mathf.Max(0.1f, scaleMult);

            Color startMold = _viz.moldTilemap.GetColor(pos);
            Color startOverlay = _viz.overlayTilemap.HasTile(pos) ? _viz.overlayTilemap.GetColor(pos) : Color.clear;

            _viz.BeginAnimation();
            try
            {
                float t = 0f; // Rise
                while (t < riseDur)
                {
                    t += Time.deltaTime; float u = Mathf.Clamp01(t / riseDur); float easeOut = 1f - (1f - u) * (1f - u);
                    float s = Mathf.Lerp(1f, maxScale, easeOut); float yOff = Mathf.Lerp(0f, lift, easeOut);
                    ApplyComposite(pos, new Vector3(0f, yOff, 0f), new Vector3(s, s, 1f));
                    yield return null;
                }
                t = 0f; // Hold
                while (t < holdDur)
                {
                    t += Time.deltaTime; ApplyComposite(pos, new Vector3(0f, lift, 0f), new Vector3(maxScale, maxScale, 1f));
                    yield return null;
                }
                t = 0f; // Swap
                while (t < swapDur)
                {
                    t += Time.deltaTime; float u = Mathf.Clamp01(t / swapDur);
                    if (_viz.overlayTilemap.HasTile(pos)) { var oc = _viz.overlayTilemap.GetColor(pos); oc.a = Mathf.Lerp(startOverlay.a, 0f, u); _viz.overlayTilemap.SetColor(pos, oc); }
                    if (_viz.moldTilemap.HasTile(pos)) { var mc = _viz.moldTilemap.GetColor(pos); mc.a = Mathf.Lerp(startMold.a, 1f, u); _viz.moldTilemap.SetColor(pos, mc); }
                    yield return null;
                }
                t = 0f; // Settle bounce
                float settleStart = maxScale;
                while (t < settleDur)
                {
                    t += Time.deltaTime; float u = Mathf.Clamp01(t / settleDur); float mid = 0.5f; float s;
                    if (u < mid) { float u1 = u / mid; s = Mathf.Lerp(settleStart, overshoot, 1f - (1f - u1) * (1f - u1)); }
                    else { float u2 = (u - mid) / (1f - mid); s = Mathf.Lerp(overshoot, 1f, u2 * u2); }
                    float yOff = Mathf.Lerp(lift, 0f, u); ApplyComposite(pos, new Vector3(0f, yOff, 0f), new Vector3(s, s, 1f));
                    yield return null;
                }
                ApplyComposite(pos, Vector3.zero, Vector3.one);
            }
            finally { _viz.EndAnimation(); }
        }

        private IEnumerator ReclaimLite(int tileId, float scaleMult, float explicitTotal)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (x, y) = board.GetXYFromTileId(tileId); var pos = new Vector3Int(x, y, 0);
            if (!_viz.moldTilemap.HasTile(pos)) yield break;

            float baseRise = UIEffectConstants.RegenerativeHyphaeRiseDurationSeconds;
            float baseSwap = UIEffectConstants.RegenerativeHyphaeFadeSwapDurationSeconds;
            float totalBase = baseRise + baseSwap; if (totalBase <= 0) totalBase = 1f;
            float total = explicitTotal > 0 ? explicitTotal : UIEffectConstants.RegenerativeHyphaeReclaimTotalDurationSeconds;
            float riseDur = total * (baseRise / totalBase);
            float swapDur = total * (baseSwap / totalBase);

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
