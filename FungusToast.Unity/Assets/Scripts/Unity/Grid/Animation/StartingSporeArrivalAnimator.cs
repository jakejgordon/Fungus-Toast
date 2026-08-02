using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Grid.Animation
{
    /// <summary>
    /// Intro animation for starting spores. Each spore uses the same non-spinning parabolic flight as
    /// Surgical Inoculation, entering from the nearest board edge before the rendered cell is revealed.
    /// </summary>
    internal class StartingSporeArrivalAnimator
    {
        private readonly GridVisualizer _viz;
        public StartingSporeArrivalAnimator(GridVisualizer viz) => _viz = viz;

        private const int EdgeLaunchOffsetCells = 3;
        private const float ArrivalStaggerSeconds = 0.18f;

        public IEnumerator Play(IEnumerable<int> startingTileIds, System.Action onSporeDropStarted = null)
        {
            var ids = startingTileIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0) yield break;
            var board = _viz.ActiveBoard; if (board == null) yield break;
            foreach (var tileId in ids)
            {
                onSporeDropStarted?.Invoke();
                _viz.StartCoroutine(AnimateSingleArrival(tileId));
                yield return new WaitForSeconds(ArrivalStaggerSeconds);
            }
            yield return _viz.WaitForAllAnimations();
        }

        private IEnumerator AnimateSingleArrival(int tileId)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (tx, ty) = board.GetXYFromTileId(tileId);
            var endCell = new Vector3Int(tx, ty, 0);

            Sprite moldSprite = null;
            var tile = board.GetTileById(tileId);
            int playerId = tile?.FungalCell?.OwnerPlayerId ?? -1;
            if (playerId >= 0)
            {
                var playerTile = _viz.GetMoldIconTileForPlayer(playerId);
                if (playerTile != null) moldSprite = playerTile.sprite;
            }

            if (moldSprite == null)
            {
                yield break;
            }

            _viz.BeginAnimation();
            try
            {
                // Hide the final state until the projectile lands. The board render restores every
                // overlay (including a starting-spore shield) after the simple cell flight completes.
                if (_viz.moldTilemap.HasTile(endCell))
                { var c = _viz.moldTilemap.GetColor(endCell); c.a = 0f; _viz.moldTilemap.SetColor(endCell, c); }
                if (_viz.overlayTilemap.HasTile(endCell))
                { var c = _viz.overlayTilemap.GetColor(endCell); c.a = 0f; _viz.overlayTilemap.SetColor(endCell, c); }

                yield return _viz.ArcHelper.AnimateArc(
                    GetNearestEdgeLaunchCell(board.Width, board.Height, endCell),
                    endCell,
                    moldSprite,
                    UI.UIEffectConstants.SurgicalInoculationArcDurationSeconds,
                    UI.UIEffectConstants.SurgicalInoculationArcBaseHeightWorld,
                    UI.UIEffectConstants.SurgicalInoculationArcHeightPerTile,
                    UI.UIEffectConstants.SurgicalInoculationArcScalePerHeightTile,
                    UI.UIEffectConstants.SurgicalInoculationArcPeakScale);

                _viz.RenderTileFromBoard(tileId);
            }
            finally { _viz.EndAnimation(); }
        }

        private static Vector3Int GetNearestEdgeLaunchCell(int boardWidth, int boardHeight, Vector3Int destinationCell)
        {
            int distanceToLeft = destinationCell.x;
            int distanceToRight = boardWidth - 1 - destinationCell.x;
            int distanceToBottom = destinationCell.y;
            int distanceToTop = boardHeight - 1 - destinationCell.y;
            int nearestDistance = Mathf.Min(distanceToLeft, distanceToRight, distanceToBottom, distanceToTop);

            if (nearestDistance == distanceToLeft)
            {
                return new Vector3Int(-EdgeLaunchOffsetCells, destinationCell.y, 0);
            }

            if (nearestDistance == distanceToRight)
            {
                return new Vector3Int(boardWidth - 1 + EdgeLaunchOffsetCells, destinationCell.y, 0);
            }

            if (nearestDistance == distanceToBottom)
            {
                return new Vector3Int(destinationCell.x, -EdgeLaunchOffsetCells, 0);
            }

            return new Vector3Int(destinationCell.x, boardHeight - 1 + EdgeLaunchOffsetCells, 0);
        }
    }
}
