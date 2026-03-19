using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Animation
{
    internal sealed class CompositeLaunchArcAnimator
    {
        private readonly GridVisualizer viz;

        private readonly struct TileVisibilityState
        {
            public TileVisibilityState(Vector3Int cell, bool hasMoldTile, Color moldColor, bool hasOverlayTile, Color overlayColor)
            {
                Cell = cell;
                HasMoldTile = hasMoldTile;
                MoldColor = moldColor;
                HasOverlayTile = hasOverlayTile;
                OverlayColor = overlayColor;
            }

            public Vector3Int Cell { get; }
            public bool HasMoldTile { get; }
            public Color MoldColor { get; }
            public bool HasOverlayTile { get; }
            public Color OverlayColor { get; }
        }

        public CompositeLaunchArcAnimator(GridVisualizer viz)
        {
            this.viz = viz;
        }

        public IEnumerator Play(
            int playerId,
            int sourceTileId,
            int destinationTileId,
            bool preserveSourceCell = false,
            Sprite overlaySprite = null,
            float overlayScale = UI.UIEffectConstants.ConidialRelayShieldScale,
            bool restoreBoardStateOnFinish = false,
            float durationScale = 1f,
            bool allowOverlayFallback = true)
        {
            var board = viz.ActiveBoard;
            if (board == null)
            {
                yield break;
            }

            var destinationCell = ToCell(board, destinationTileId);
            var destinationState = CaptureTileVisibility(destinationCell);
            HideTile(destinationState);

            yield return PlaySegment(
                playerId,
                sourceTileId,
                destinationTileId,
                destinationState,
                preserveSourceCell,
                overlaySprite,
                overlayScale,
                restoreBoardStateOnFinish,
                revealDestinationOnFinish: false,
                emphasizeSource: true,
                durationScale,
                allowOverlayFallback);
        }

        public IEnumerator PlaySequence(
            int playerId,
            int sourceTileId,
            IReadOnlyList<int> destinationTileIds,
            bool preserveSourceCell = true,
            Sprite overlaySprite = null,
            float overlayScale = UI.UIEffectConstants.ConidialRelayShieldScale,
            bool restoreBoardStateOnFinish = false,
            float durationScale = 1f,
            bool allowOverlayFallback = true)
        {
            var board = viz.ActiveBoard;
            if (board == null || destinationTileIds == null || destinationTileIds.Count == 0)
            {
                yield break;
            }

            var orderedDestinations = destinationTileIds
                .Where(tileId => tileId >= 0)
                .Distinct()
                .ToList();
            if (orderedDestinations.Count == 0)
            {
                yield break;
            }

            var hiddenStates = orderedDestinations
                .Select(tileId => CaptureTileVisibility(ToCell(board, tileId)))
                .ToDictionary(state => state.Cell, state => state);

            foreach (var state in hiddenStates.Values)
            {
                HideTile(state);
            }

            int currentSourceTileId = sourceTileId;
            for (int index = 0; index < orderedDestinations.Count; index++)
            {
                int destinationTileId = orderedDestinations[index];
                var destinationState = hiddenStates[ToCell(board, destinationTileId)];

                yield return PlaySegment(
                    playerId,
                    currentSourceTileId,
                    destinationTileId,
                    destinationState,
                    preserveSourceCell,
                    overlaySprite,
                    overlayScale,
                    restoreBoardStateOnFinish,
                    revealDestinationOnFinish: true,
                    emphasizeSource: true,
                    durationScale,
                    allowOverlayFallback);

                currentSourceTileId = destinationTileId;
            }
        }

        public IEnumerator PlayBatch(
            int playerId,
            IReadOnlyList<(int sourceTileId, int destinationTileId)> moves,
            bool preserveSourceCell = false,
            Sprite overlaySprite = null,
            float overlayScale = UI.UIEffectConstants.ConidialRelayShieldScale,
            bool restoreBoardStateOnFinish = false,
            float durationScale = 1f,
            bool allowOverlayFallback = true)
        {
            if (moves == null || moves.Count == 0)
            {
                yield break;
            }

            var orderedMoves = moves
                .Where(move => move.sourceTileId >= 0 && move.destinationTileId >= 0 && move.sourceTileId != move.destinationTileId)
                .Distinct()
                .ToList();
            if (orderedMoves.Count == 0)
            {
                yield break;
            }

            int remaining = orderedMoves.Count;
            foreach (var move in orderedMoves)
            {
                viz.StartCoroutine(PlayWithCompletion(
                    playerId,
                    move.sourceTileId,
                    move.destinationTileId,
                    preserveSourceCell,
                    overlaySprite,
                    overlayScale,
                    restoreBoardStateOnFinish,
                    durationScale,
                    allowOverlayFallback,
                    () => remaining--));
            }

            while (remaining > 0)
            {
                yield return null;
            }
        }

        private IEnumerator PlaySegment(
            int playerId,
            int sourceTileId,
            int destinationTileId,
            TileVisibilityState destinationState,
            bool preserveSourceCell,
            Sprite overlaySprite,
            float overlayScale,
            bool restoreBoardStateOnFinish,
            bool revealDestinationOnFinish,
            bool emphasizeSource,
            float durationScale,
            bool allowOverlayFallback)
        {
            var board = viz.ActiveBoard;
            if (board == null)
            {
                yield break;
            }

            var moldTile = viz.GetTileForPlayer(playerId);
            var moldSprite = moldTile != null ? moldTile.sprite : null;
            var projectileOverlaySprite = overlaySprite ?? (allowOverlayFallback && viz.goldShieldOverlayTile != null ? viz.goldShieldOverlayTile.sprite : null);
            if (moldSprite == null)
            {
                RestoreTile(destinationState, destinationTileId, restoreBoardStateOnFinish);
                yield break;
            }

            var referenceTilemap = viz.overlayTilemap != null ? viz.overlayTilemap : viz.moldTilemap;
            if (referenceTilemap == null)
            {
                RestoreTile(destinationState, destinationTileId, restoreBoardStateOnFinish);
                yield break;
            }

            var sourceCell = ToCell(board, sourceTileId);
            var destinationCell = destinationState.Cell;

            var compositeRoot = BuildComposite(referenceTilemap, moldSprite, projectileOverlaySprite, overlayScale);
            if (compositeRoot == null)
            {
                RestoreTile(destinationState, destinationTileId, restoreBoardStateOnFinish);
                yield break;
            }

            viz.BeginAnimation();
            try
            {
                if (emphasizeSource)
                {
                    yield return SourceEmphasis(compositeRoot, sourceCell, referenceTilemap, preserveSourceCell, durationScale);
                }
                else
                {
                    compositeRoot.transform.position = CellCenterWorld(referenceTilemap, sourceCell);
                    compositeRoot.transform.localScale = Vector3.one * (preserveSourceCell ? 0.45f : UI.UIEffectConstants.ConidialRelaySourceStartScale);
                    compositeRoot.transform.localRotation = Quaternion.identity;
                }

                yield return ArcFlight(compositeRoot, sourceCell, destinationCell, referenceTilemap, durationScale);
                yield return Landing(compositeRoot, destinationCell, referenceTilemap, durationScale);
            }
            finally
            {
                Object.Destroy(compositeRoot);
                if (revealDestinationOnFinish)
                {
                    viz.RevealPreAnimationPreviewTile(destinationTileId);
                    viz.RenderTileFromBoard(destinationTileId);
                }
                else
                {
                    RestoreTile(destinationState, destinationTileId, restoreBoardStateOnFinish);
                }
                viz.EndAnimation();
            }
        }

        private IEnumerator PlayWithCompletion(
            int playerId,
            int sourceTileId,
            int destinationTileId,
            bool preserveSourceCell,
            Sprite overlaySprite,
            float overlayScale,
            bool restoreBoardStateOnFinish,
            float durationScale,
            bool allowOverlayFallback,
            System.Action onComplete)
        {
            try
            {
                yield return Play(
                    playerId,
                    sourceTileId,
                    destinationTileId,
                    preserveSourceCell,
                    overlaySprite,
                    overlayScale,
                    restoreBoardStateOnFinish,
                    durationScale,
                    allowOverlayFallback);
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator SourceEmphasis(GameObject compositeRoot, Vector3Int sourceCell, Tilemap tilemap, bool preserveSourceCell, float durationScale)
        {
            float duration = ScaleDuration(UI.UIEffectConstants.ConidialRelaySourceEmphasisDurationSeconds, durationScale);
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

        private IEnumerator ArcFlight(GameObject compositeRoot, Vector3Int sourceCell, Vector3Int destinationCell, Tilemap tilemap, float durationScale)
        {
            float duration = ScaleDuration(UI.UIEffectConstants.ConidialRelayArcDurationSeconds, durationScale);
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

        private IEnumerator Landing(GameObject compositeRoot, Vector3Int destinationCell, Tilemap tilemap, float durationScale)
        {
            float duration = ScaleDuration(UI.UIEffectConstants.ConidialRelayLandingDurationSeconds, durationScale);
            float impactDuration = duration * UI.UIEffectConstants.ConidialRelayLandingImpactPortion;
            float settleDuration = Mathf.Max(0.0001f, duration - impactDuration);
            Vector3 destinationWorld = CellCenterWorld(tilemap, destinationCell);

            float elapsed = 0f;
            while (elapsed < impactDuration)
            {
                elapsed += Time.deltaTime;
                float t = impactDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / impactDuration);
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

        private GameObject BuildComposite(Tilemap tilemap, Sprite moldSprite, Sprite overlaySprite, float overlayScale)
        {
            var root = new GameObject("CompositeLaunchArc");
            root.transform.SetParent(tilemap.transform, false);

            var mold = new GameObject("Mold");
            mold.transform.SetParent(root.transform, false);
            var moldRenderer = mold.AddComponent<SpriteRenderer>();
            moldRenderer.sprite = moldSprite;

            SpriteRenderer shieldRenderer = null;
            if (overlaySprite != null)
            {
                var shield = new GameObject("Overlay");
                shield.transform.SetParent(root.transform, false);
                shieldRenderer = shield.AddComponent<SpriteRenderer>();
                shieldRenderer.sprite = overlaySprite;
                shield.transform.localScale = Vector3.one * overlayScale;
            }

            var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                moldRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                moldRenderer.sortingOrder = tilemapRenderer.sortingOrder + 9;
                if (shieldRenderer != null)
                {
                    shieldRenderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                    shieldRenderer.sortingOrder = tilemapRenderer.sortingOrder + 10;
                }
            }

            return root;
        }

        private TileVisibilityState CaptureTileVisibility(Vector3Int cell)
        {
            bool hasMoldTile = viz.moldTilemap != null && viz.moldTilemap.HasTile(cell);
            bool hasOverlayTile = viz.overlayTilemap != null && viz.overlayTilemap.HasTile(cell);
            return new TileVisibilityState(
                cell,
                hasMoldTile,
                hasMoldTile ? viz.moldTilemap.GetColor(cell) : Color.white,
                hasOverlayTile,
                hasOverlayTile ? viz.overlayTilemap.GetColor(cell) : Color.white);
        }

        private void HideTile(TileVisibilityState state)
        {
            if (state.HasMoldTile && viz.moldTilemap != null)
            {
                var color = viz.moldTilemap.GetColor(state.Cell);
                color.a = 0f;
                viz.moldTilemap.SetColor(state.Cell, color);
            }

            if (state.HasOverlayTile && viz.overlayTilemap != null)
            {
                var color = viz.overlayTilemap.GetColor(state.Cell);
                color.a = 0f;
                viz.overlayTilemap.SetColor(state.Cell, color);
            }
        }

        private void RestoreTile(TileVisibilityState state, int tileId, bool restoreBoardStateOnFinish)
        {
            if (restoreBoardStateOnFinish)
            {
                viz.RenderTileFromBoard(tileId);
                return;
            }

            if (state.HasMoldTile && viz.moldTilemap != null)
            {
                viz.moldTilemap.SetColor(state.Cell, state.MoldColor);
            }

            if (state.HasOverlayTile && viz.overlayTilemap != null)
            {
                viz.overlayTilemap.SetColor(state.Cell, state.OverlayColor);
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

        private static float ScaleDuration(float baseDuration, float durationScale)
        {
            return Mathf.Max(0.0001f, baseDuration * Mathf.Max(0.01f, durationScale));
        }
    }
}