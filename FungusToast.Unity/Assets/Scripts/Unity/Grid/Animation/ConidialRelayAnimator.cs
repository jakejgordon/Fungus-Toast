using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Animation
{
    internal sealed class ConidialRelayAnimator
    {
        private readonly GridVisualizer viz;

        public ConidialRelayAnimator(GridVisualizer viz)
        {
            this.viz = viz;
        }

        public IEnumerator Play(int playerId, int sourceTileId, int destinationTileId, bool preserveSourceCell = false)
        {
            var board = viz.ActiveBoard;
            if (board == null)
            {
                yield break;
            }

            var moldTile = viz.GetTileForPlayer(playerId);
            var moldSprite = moldTile != null ? moldTile.sprite : null;
            var shieldSprite = viz.goldShieldOverlayTile != null ? viz.goldShieldOverlayTile.sprite : null;
            if (moldSprite == null || shieldSprite == null)
            {
                yield break;
            }

            var referenceTilemap = viz.overlayTilemap != null ? viz.overlayTilemap : viz.moldTilemap;
            if (referenceTilemap == null)
            {
                yield break;
            }

            var sourceCell = ToCell(board, sourceTileId);
            var destinationCell = ToCell(board, destinationTileId);
            var destinationMoldColor = viz.moldTilemap != null && viz.moldTilemap.HasTile(destinationCell)
                ? viz.moldTilemap.GetColor(destinationCell)
                : Color.white;
            var destinationOverlayColor = viz.overlayTilemap != null && viz.overlayTilemap.HasTile(destinationCell)
                ? viz.overlayTilemap.GetColor(destinationCell)
                : Color.white;

            HideDestination(destinationCell);

            var compositeRoot = BuildComposite(referenceTilemap, moldSprite, shieldSprite);
            if (compositeRoot == null)
            {
                RestoreDestination(destinationCell, destinationMoldColor, destinationOverlayColor);
                yield break;
            }

            viz.BeginAnimation();
            try
            {
                yield return SourceEmphasis(compositeRoot, sourceCell, referenceTilemap, preserveSourceCell);
                yield return ArcFlight(compositeRoot, sourceCell, destinationCell, referenceTilemap);
                yield return Landing(compositeRoot, destinationCell, referenceTilemap);
            }
            finally
            {
                Object.Destroy(compositeRoot);
                RestoreDestination(destinationCell, destinationMoldColor, destinationOverlayColor);
                viz.EndAnimation();
            }
        }

        private IEnumerator SourceEmphasis(GameObject compositeRoot, Vector3Int sourceCell, Tilemap tilemap, bool preserveSourceCell)
        {
            float duration = UI.UIEffectConstants.ConidialRelaySourceEmphasisDurationSeconds;
            Vector3 sourceWorld = CellCenterWorld(tilemap, sourceCell);
            Vector3 startScale = Vector3.one * (preserveSourceCell ? 0.45f : UI.UIEffectConstants.ConidialRelaySourceStartScale);
            Vector3 endScale = Vector3.one * UI.UIEffectConstants.ConidialRelayLiftScale;
            float liftOffset = UI.UIEffectConstants.ConidialRelayLiftYOffset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                compositeRoot.transform.position = sourceWorld + Vector3.up * Mathf.Lerp(0f, liftOffset, eased);
                compositeRoot.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                compositeRoot.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, UI.UIEffectConstants.ConidialRelaySourceSpinDegrees, eased));
                yield return null;
            }
        }

        private IEnumerator ArcFlight(GameObject compositeRoot, Vector3Int sourceCell, Vector3Int destinationCell, Tilemap tilemap)
        {
            float duration = UI.UIEffectConstants.ConidialRelayArcDurationSeconds;
            Vector3 startWorld = CellCenterWorld(tilemap, sourceCell) + Vector3.up * UI.UIEffectConstants.ConidialRelayLiftYOffset;
            Vector3 endWorld = CellCenterWorld(tilemap, destinationCell);

            float distanceTiles = Vector2.Distance(
                new Vector2(sourceCell.x, sourceCell.y),
                new Vector2(destinationCell.x, destinationCell.y));
            float arcHeight = UI.UIEffectConstants.ConidialRelayArcBaseHeightWorld
                + (distanceTiles * UI.UIEffectConstants.ConidialRelayArcHeightPerTile * tilemap.cellSize.y);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float heightFactor = 4f * t * (1f - t);
                Vector3 position = Vector3.Lerp(startWorld, endWorld, t) + Vector3.up * (heightFactor * arcHeight);
                compositeRoot.transform.position = position;

                float scale = t <= 0.5f
                    ? Mathf.Lerp(UI.UIEffectConstants.ConidialRelayLiftScale, UI.UIEffectConstants.ConidialRelayPeakScale, t / 0.5f)
                    : Mathf.Lerp(UI.UIEffectConstants.ConidialRelayPeakScale, UI.UIEffectConstants.ConidialRelayDescentScale, (t - 0.5f) / 0.5f);
                compositeRoot.transform.localScale = Vector3.one * scale;

                float rotation = Mathf.Lerp(
                    UI.UIEffectConstants.ConidialRelaySourceSpinDegrees,
                    UI.UIEffectConstants.ConidialRelayArcSpinDegrees,
                    t);
                compositeRoot.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                yield return null;
            }
        }

        private IEnumerator Landing(GameObject compositeRoot, Vector3Int destinationCell, Tilemap tilemap)
        {
            float duration = UI.UIEffectConstants.ConidialRelayLandingDurationSeconds;
            float impactDuration = duration * UI.UIEffectConstants.ConidialRelayLandingImpactPortion;
            float settleDuration = duration - impactDuration;
            Vector3 destinationWorld = CellCenterWorld(tilemap, destinationCell);

            float elapsed = 0f;
            while (elapsed < impactDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / impactDuration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                float scaleX = Mathf.Lerp(UI.UIEffectConstants.ConidialRelayDescentScale, UI.UIEffectConstants.ConidialRelayLandingStretchX, eased);
                float scaleY = Mathf.Lerp(UI.UIEffectConstants.ConidialRelayDescentScale, UI.UIEffectConstants.ConidialRelayLandingStretchY, eased);
                compositeRoot.transform.position = destinationWorld;
                compositeRoot.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                compositeRoot.transform.localRotation = Quaternion.identity;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float scaleX = Mathf.Lerp(UI.UIEffectConstants.ConidialRelayLandingStretchX, 1f, eased);
                float scaleY = Mathf.Lerp(UI.UIEffectConstants.ConidialRelayLandingStretchY, 1f, eased);
                compositeRoot.transform.position = destinationWorld;
                compositeRoot.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                yield return null;
            }

            compositeRoot.transform.position = destinationWorld;
            compositeRoot.transform.localScale = Vector3.one;
        }

        private GameObject BuildComposite(Tilemap tilemap, Sprite moldSprite, Sprite shieldSprite)
        {
            var root = new GameObject("ConidialRelayComposite");
            root.transform.SetParent(tilemap.transform, false);

            var mold = new GameObject("Mold");
            mold.transform.SetParent(root.transform, false);
            var moldRenderer = mold.AddComponent<SpriteRenderer>();
            moldRenderer.sprite = moldSprite;

            var shield = new GameObject("Shield");
            shield.transform.SetParent(root.transform, false);
            var shieldRenderer = shield.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = shieldSprite;
            shield.transform.localScale = Vector3.one * UI.UIEffectConstants.ConidialRelayShieldScale;

            var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                moldRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                moldRenderer.sortingOrder = tilemapRenderer.sortingOrder + 9;
                shieldRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                shieldRenderer.sortingOrder = tilemapRenderer.sortingOrder + 10;
            }

            return root;
        }

        private void HideDestination(Vector3Int destinationCell)
        {
            if (viz.moldTilemap != null && viz.moldTilemap.HasTile(destinationCell))
            {
                var color = viz.moldTilemap.GetColor(destinationCell);
                color.a = 0f;
                viz.moldTilemap.SetColor(destinationCell, color);
            }

            if (viz.overlayTilemap != null && viz.overlayTilemap.HasTile(destinationCell))
            {
                var color = viz.overlayTilemap.GetColor(destinationCell);
                color.a = 0f;
                viz.overlayTilemap.SetColor(destinationCell, color);
            }
        }

        private void RestoreDestination(Vector3Int destinationCell, Color moldColor, Color overlayColor)
        {
            if (viz.moldTilemap != null && viz.moldTilemap.HasTile(destinationCell))
            {
                viz.moldTilemap.SetColor(destinationCell, moldColor);
            }

            if (viz.overlayTilemap != null && viz.overlayTilemap.HasTile(destinationCell))
            {
                viz.overlayTilemap.SetColor(destinationCell, overlayColor);
            }
        }

        private Vector3Int ToCell(FungusToast.Core.Board.GameBoard board, int tileId)
        {
            var (x, y) = board.GetXYFromTileId(tileId);
            return new Vector3Int(x, y, 0);
        }

        private Vector3 CellCenterWorld(Tilemap tilemap, Vector3Int cell)
        {
            Vector3 world = tilemap.CellToWorld(cell);
            Vector3 cellSize = tilemap.cellSize;
            return world + new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        }
    }
}