using FungusToast.Unity.Grid.Animation;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using FungusToast.Unity.UI;

namespace FungusToast.Unity.Grid.Animation
{
    internal class SurgicalInoculationAnimator
    {
        internal readonly struct ArcAnimationSettings
        {
            public ArcAnimationSettings(float durationSeconds, float baseHeightWorld, float heightPerTile, float peakScale)
            {
                DurationSeconds = durationSeconds;
                BaseHeightWorld = baseHeightWorld;
                HeightPerTile = heightPerTile;
                PeakScale = peakScale;
            }

            public float DurationSeconds { get; }
            public float BaseHeightWorld { get; }
            public float HeightPerTile { get; }
            public float PeakScale { get; }
        }

        private readonly GridVisualizer _viz;
        private static readonly ArcAnimationSettings SurgicalArcSettings = new(
            UIEffectConstants.SurgicalInoculationArcDurationSeconds,
            UIEffectConstants.SurgicalInoculationArcBaseHeightWorld,
            UIEffectConstants.SurgicalInoculationArcHeightPerTile,
            UIEffectConstants.SurgicalInoculationArcPeakScale);

        private static readonly ArcAnimationSettings JettingArcSettings = new(
            UIEffectConstants.JettingMyceliumArcDurationSeconds,
            UIEffectConstants.JettingMyceliumArcBaseHeightWorld,
            UIEffectConstants.JettingMyceliumArcHeightPerTile,
            UIEffectConstants.JettingMyceliumArcPeakScale);

        public SurgicalInoculationAnimator(GridVisualizer viz) => _viz = viz;

        public IEnumerator RunArcAndDrop(int playerId, int targetTileId, Sprite sprite)
        {
            var board = _viz.ActiveBoard;
            if (board == null || sprite == null)
            {
                yield return _viz.ResistantDropAnimation(targetTileId, durationScale: UIEffectConstants.SurgicalInoculationDropDurationScale);
                yield break;
            }
            if (playerId < 0 || playerId >= board.Players.Count)
            {
                yield return _viz.ResistantDropAnimation(targetTileId, durationScale: UIEffectConstants.SurgicalInoculationDropDurationScale);
                yield break;
            }
            var player = board.Players[playerId];
            if (!player.StartingTileId.HasValue)
            {
                yield return _viz.ResistantDropAnimation(targetTileId, durationScale: UIEffectConstants.SurgicalInoculationDropDurationScale);
                yield break;
            }
            int startId = player.StartingTileId.Value;
            var (sx, sy) = board.GetXYFromTileId(startId);
            var (tx, ty) = board.GetXYFromTileId(targetTileId);
            var startCell = new Vector3Int(sx, sy, 0);
            var endCell = new Vector3Int(tx, ty, 0);

            yield return RunArc(startCell, endCell, sprite, SurgicalArcSettings, null);
        }

        public IEnumerator RunArcVolley(int sourceTileId, IReadOnlyList<int> targetTileIds, Sprite sprite, System.Action<int> onImpact = null)
        {
            var board = _viz.ActiveBoard;
            if (board == null || sprite == null || targetTileIds == null || targetTileIds.Count == 0)
            {
                yield break;
            }

            var (sx, sy) = board.GetXYFromTileId(sourceTileId);
            var startCell = new Vector3Int(sx, sy, 0);
            var uniqueTargetTileIds = new HashSet<int>();
            int activeArcs = 0;

            foreach (int targetTileId in targetTileIds)
            {
                if (targetTileId == sourceTileId || !uniqueTargetTileIds.Add(targetTileId))
                {
                    continue;
                }

                var (tx, ty) = board.GetXYFromTileId(targetTileId);
                var endCell = new Vector3Int(tx, ty, 0);
                activeArcs++;
                _viz.StartCoroutine(RunArcWithCompletion(startCell, endCell, sprite, JettingArcSettings, () =>
                {
                    onImpact?.Invoke(targetTileId);
                    activeArcs--;
                }));

                if (UIEffectConstants.JettingMyceliumArcVolleyStaggerSeconds > 0f)
                {
                    yield return new WaitForSeconds(UIEffectConstants.JettingMyceliumArcVolleyStaggerSeconds);
                }
            }

            while (activeArcs > 0)
            {
                yield return null;
            }
        }

        private IEnumerator RunArcWithCompletion(Vector3Int startCell, Vector3Int endCell, Sprite sprite, ArcAnimationSettings settings, System.Action onComplete)
        {
            try
            {
                yield return RunArc(startCell, endCell, sprite, settings, null);
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator RunArc(Vector3Int startCell, Vector3Int endCell, Sprite sprite, ArcAnimationSettings settings, System.Action onImpact)
        {
            if (sprite == null)
            {
                yield break;
            }

            _viz.BeginAnimation();
            try
            {
                yield return _viz.ArcHelper.AnimateArc(
                    startCell,
                    endCell,
                    sprite,
                    settings.DurationSeconds,
                    settings.BaseHeightWorld,
                    settings.HeightPerTile,
                    UIEffectConstants.SurgicalInoculationArcScalePerHeightTile,
                    settings.PeakScale);

                onImpact?.Invoke();
            }
            finally
            {
                _viz.EndAnimation();
            }
        }
    }
}
