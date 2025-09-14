using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Grid.Animation
{
    /// <summary>
    /// Plays an introductory animation at game start where each player's starting resistant cell
    /// arcs in from above the board center and lands with the existing resistant drop effect.
    /// Leverages the existing ArcProjectileHelper + ResistantDropAnimation used by Surgical Inoculation.
    /// </summary>
    internal class StartingSporeArrivalAnimator
    {
        private readonly GridVisualizer _viz;
        public StartingSporeArrivalAnimator(GridVisualizer viz) => _viz = viz;

        /// <summary>
        /// Public entry point. Launches staggered arrival animations for all supplied starting tileIds.
        /// </summary>
        public IEnumerator Play(IEnumerable<int> startingTileIds)
        {
            var ids = startingTileIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0) yield break;
            var board = _viz.ActiveBoard; if (board == null) yield break;

            // Require a shield sprite (all starting spores are resistant)
            var shieldSprite = _viz.goldShieldOverlayTile != null ? _viz.goldShieldOverlayTile.sprite : null;
            if (shieldSprite == null) yield break;

            // Spawn point: above board center (Y offset proportional to board size)
            int startX = board.Width / 2;
            int startY = board.Height + Mathf.CeilToInt(board.Height * 0.35f); // off-board above
            var startCell = new Vector3Int(startX, startY, 0);

            // Launch each with a small stagger so player can track multiple arrivals
            foreach (var tileId in ids)
            {
                _viz.StartCoroutine(AnimateSingleArrival(tileId, startCell, shieldSprite));
                yield return new WaitForSeconds(0.18f); // stagger
            }
            // Wait for all underlying animations (arcs + drops) to finish
            yield return _viz.WaitForAllAnimations();
        }

        private IEnumerator AnimateSingleArrival(int tileId, Vector3Int startCell, Sprite sprite)
        {
            var board = _viz.ActiveBoard; if (board == null) yield break;
            var (tx, ty) = board.GetXYFromTileId(tileId);
            var endCell = new Vector3Int(tx, ty, 0);

            // Make sure the board is currently rendered so tilemaps have entries to adjust
            // (InitializeGame already called RenderBoard before invoking us.)
            _viz.BeginAnimation();
            try
            {
                // Hide the existing tiles' visual alpha so the projectile/landing feels like a spawn
                if (_viz.moldTilemap.HasTile(endCell))
                {
                    var c = _viz.moldTilemap.GetColor(endCell); c.a = 0f; _viz.moldTilemap.SetColor(endCell, c);
                }
                if (_viz.overlayTilemap.HasTile(endCell))
                {
                    var c = _viz.overlayTilemap.GetColor(endCell); c.a = 0f; _viz.overlayTilemap.SetColor(endCell, c);
                }

                // Arc parameters reuse Surgical Inoculation constants, with a slightly higher base height to emphasize entry
                float duration = UI.UIEffectConstants.SurgicalInoculationArcDurationSeconds;
                float baseHeight = UI.UIEffectConstants.SurgicalInoculationArcBaseHeightWorld + 1.5f;
                float perTileHeight = UI.UIEffectConstants.SurgicalInoculationArcHeightPerTile;
                float scalePerHeight = UI.UIEffectConstants.SurgicalInoculationArcScalePerHeightTile;

                yield return _viz.ArcHelper.AnimateArc(
                    startCell,
                    endCell,
                    sprite,
                    duration,
                    baseHeight,
                    perTileHeight,
                    scalePerHeight);

                // Landing + shield squash/settle
                yield return _viz.ResistantDropAnimation(tileId, 0.5f);

                // Ensure final alpha restoration (in case drop animation did not restore mold alpha fully)
                if (_viz.moldTilemap.HasTile(endCell))
                {
                    var c = _viz.moldTilemap.GetColor(endCell); c.a = 1f; _viz.moldTilemap.SetColor(endCell, c);
                }
                if (_viz.overlayTilemap.HasTile(endCell))
                {
                    var c = _viz.overlayTilemap.GetColor(endCell); c.a = 1f; _viz.overlayTilemap.SetColor(endCell, c);
                }
            }
            finally
            {
                _viz.EndAnimation();
            }
        }
    }
}
