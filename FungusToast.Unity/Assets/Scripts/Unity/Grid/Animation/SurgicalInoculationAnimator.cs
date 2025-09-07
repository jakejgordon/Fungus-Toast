using FungusToast.Unity.Grid.Animation;
using System.Collections;
using UnityEngine;
using FungusToast.Unity.UI;

namespace FungusToast.Unity.Grid.Animation
{
    internal class SurgicalInoculationAnimator
    {
        private readonly GridVisualizer _viz;
        public SurgicalInoculationAnimator(GridVisualizer viz) => _viz = viz;

        public IEnumerator RunArcAndDrop(int playerId, int targetTileId, Sprite sprite)
        {
            var board = _viz.ActiveBoard;
            if (board == null || sprite == null)
            {
                yield return _viz.ResistantDropAnimation(targetTileId);
                yield break;
            }
            if (playerId < 0 || playerId >= board.Players.Count)
            {
                yield return _viz.ResistantDropAnimation(targetTileId);
                yield break;
            }
            var player = board.Players[playerId];
            if (!player.StartingTileId.HasValue)
            {
                yield return _viz.ResistantDropAnimation(targetTileId);
                yield break;
            }
            int startId = player.StartingTileId.Value;
            var (sx, sy) = board.GetXYFromTileId(startId);
            var (tx, ty) = board.GetXYFromTileId(targetTileId);
            var startCell = new Vector3Int(sx, sy, 0);
            var endCell = new Vector3Int(tx, ty, 0);

            _viz.BeginAnimation();
            try
            {
                yield return _viz.ArcHelper.AnimateArc(
                    startCell,
                    endCell,
                    sprite,
                    UIEffectConstants.SurgicalInoculationArcDurationSeconds,
                    UIEffectConstants.SurgicalInoculationArcBaseHeightWorld,
                    UIEffectConstants.SurgicalInoculationArcHeightPerTile,
                    UIEffectConstants.SurgicalInoculationArcScalePerHeightTile);
            }
            finally
            {
                _viz.EndAnimation();
            }
            yield return _viz.ResistantDropAnimation(targetTileId);
        }
    }
}
