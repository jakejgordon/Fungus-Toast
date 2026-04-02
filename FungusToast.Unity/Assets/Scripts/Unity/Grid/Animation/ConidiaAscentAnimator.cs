using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Animation
{
    internal sealed class ConidiaAscentAnimator
    {
        private readonly GridVisualizer viz;

        private readonly struct TileVisibilitySnapshot
        {
            public TileVisibilitySnapshot(Vector3Int cell, bool hasMoldTile, Color moldColor, bool hasOverlayTile, Color overlayColor)
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

        public ConidiaAscentAnimator(GridVisualizer viz)
        {
            this.viz = viz;
        }

        public IEnumerator Play(int playerId, int sourceTileId, int destinationTileId, IReadOnlyList<int> deadZoneTileIds)
        {
            var board = viz.ActiveBoard;
            var referenceTilemap = viz.moldTilemap != null ? viz.moldTilemap : viz.overlayTilemap;
            var playerTile = viz.GetTileForPlayer(playerId);
            if (board == null || referenceTilemap == null || playerTile?.sprite == null)
            {
                yield break;
            }

            var sourceTileIds = GetBlockTileIds(board, sourceTileId, 2);
            var destinationTileIds = GetBlockTileIds(board, destinationTileId, 2);
            if (sourceTileIds.Count != 4 || destinationTileIds.Count != 4)
            {
                yield break;
            }

            var sourceSnapshots = CaptureSnapshots(board, sourceTileIds);
            if (sourceSnapshots.Count == 0)
            {
                yield break;
            }

            var sourceCenter = ComputeClusterCenterWorld(referenceTilemap, board, sourceTileIds);
            var destinationCenter = ComputeClusterCenterWorld(referenceTilemap, board, destinationTileIds);
            var offscreenPoint = ComputeOffscreenPoint(referenceTilemap, board, sourceCenter, destinationCenter);
            GameObject payload = BuildPayload(referenceTilemap, board, sourceTileIds, playerTile.sprite, sourceCenter);
            if (payload == null)
            {
                yield break;
            }

            List<TileVisibilitySnapshot> destinationSnapshots = null;
            bool boardStateRevealed = false;

            viz.BeginAnimation();
            HideSnapshots(sourceSnapshots);

            try
            {
                float launchElapsed = 0f;
                while (launchElapsed < UI.UIEffectConstants.ConidiaAscentLaunchDurationSeconds)
                {
                    launchElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(launchElapsed / UI.UIEffectConstants.ConidiaAscentLaunchDurationSeconds);
                    float eased = EaseInCubic(t);

                    if (!boardStateRevealed && launchElapsed >= UI.UIEffectConstants.ConidiaAscentDeadZoneRevealDelaySeconds)
                    {
                        viz.RenderBoard(board, suppressAnimations: true);
                        destinationSnapshots = CaptureSnapshots(board, destinationTileIds);
                        HideSnapshots(destinationSnapshots);
                        boardStateRevealed = true;
                    }

                    float sway = Mathf.Sin(t * Mathf.PI * 0.8f) * UI.UIEffectConstants.ConidiaAscentAscentSwayWorld;
                    payload.transform.position = Vector3.Lerp(sourceCenter, offscreenPoint, eased) + new Vector3(sway, 0f, 0f);

                    float scale = Mathf.Lerp(1f, UI.UIEffectConstants.ConidiaAscentAscentMaxScale, eased);
                    payload.transform.localScale = new Vector3(scale, scale, 1f);
                    payload.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, UI.UIEffectConstants.ConidiaAscentAscentTiltDegrees, eased));
                    yield return null;
                }

                if (!boardStateRevealed)
                {
                    viz.RenderBoard(board, suppressAnimations: true);
                    destinationSnapshots = CaptureSnapshots(board, destinationTileIds);
                    HideSnapshots(destinationSnapshots);
                    boardStateRevealed = true;
                }

                float returnElapsed = 0f;
                while (returnElapsed < UI.UIEffectConstants.ConidiaAscentReturnDurationSeconds)
                {
                    returnElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(returnElapsed / UI.UIEffectConstants.ConidiaAscentReturnDurationSeconds);
                    float eased = EaseOutCubic(t);

                    Vector3 linear = Vector3.Lerp(offscreenPoint, destinationCenter, eased);
                    linear.y += 4f * t * (1f - t) * UI.UIEffectConstants.ConidiaAscentReturnArcHeightWorld;
                    payload.transform.position = linear;

                    float scale = Mathf.Lerp(UI.UIEffectConstants.ConidiaAscentAscentMaxScale * 0.94f, 1f, t);
                    payload.transform.localScale = new Vector3(scale, scale, 1f);
                    payload.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(UI.UIEffectConstants.ConidiaAscentAscentTiltDegrees, 0f, eased));
                    yield return null;
                }

                yield return RevealDestination(destinationSnapshots);
                yield return PlayLandingSettle(payload, destinationCenter);

                if (deadZoneTileIds != null && deadZoneTileIds.Count > 0)
                {
                    viz.RenderBoard(board, suppressAnimations: true);
                }
            }
            finally
            {
                if (payload != null)
                {
                    Object.Destroy(payload);
                }

                if (!boardStateRevealed)
                {
                    RestoreSnapshots(sourceSnapshots);
                }
                else if (viz.ActiveBoard != null)
                {
                    viz.RenderBoard(viz.ActiveBoard, suppressAnimations: true);
                }

                viz.EndAnimation();
            }
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - t;
            return 1f - (inverse * inverse * inverse);
        }

        private GameObject BuildPayload(Tilemap referenceTilemap, GameBoard board, IReadOnlyList<int> sourceTileIds, Sprite moldSprite, Vector3 sourceCenter)
        {
            var root = new GameObject("ConidiaAscentPayload");
            root.transform.SetParent(referenceTilemap.transform, false);
            root.transform.position = sourceCenter;

            var tilemapRenderer = referenceTilemap.GetComponent<TilemapRenderer>();
            Vector3 localSourceCenter = referenceTilemap.transform.InverseTransformPoint(sourceCenter);
            foreach (int tileId in sourceTileIds)
            {
                var child = new GameObject($"PayloadTile_{tileId}");
                child.transform.SetParent(root.transform, false);
                Vector3 localCellCenter = referenceTilemap.transform.InverseTransformPoint(GetCellCenterWorld(referenceTilemap, board, tileId));
                child.transform.localPosition = localCellCenter - localSourceCenter;

                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = moldSprite;
                if (tilemapRenderer != null)
                {
                    renderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                    renderer.sortingOrder = tilemapRenderer.sortingOrder + 18;
                }
            }

            return root;
        }

        private List<TileVisibilitySnapshot> CaptureSnapshots(GameBoard board, IReadOnlyList<int> tileIds)
        {
            var snapshots = new List<TileVisibilitySnapshot>(tileIds.Count);
            foreach (int tileId in tileIds.Distinct())
            {
                snapshots.Add(CaptureSnapshot(ToCell(board, tileId)));
            }

            return snapshots;
        }

        private TileVisibilitySnapshot CaptureSnapshot(Vector3Int cell)
        {
            bool hasMoldTile = viz.moldTilemap != null && viz.moldTilemap.HasTile(cell);
            bool hasOverlayTile = viz.overlayTilemap != null && viz.overlayTilemap.HasTile(cell);
            return new TileVisibilitySnapshot(
                cell,
                hasMoldTile,
                hasMoldTile ? viz.moldTilemap.GetColor(cell) : Color.white,
                hasOverlayTile,
                hasOverlayTile ? viz.overlayTilemap.GetColor(cell) : Color.white);
        }

        private void HideSnapshots(IReadOnlyList<TileVisibilitySnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            foreach (var snapshot in snapshots)
            {
                SetTileAlpha(snapshot, 0f);
            }
        }

        private void RestoreSnapshots(IReadOnlyList<TileVisibilitySnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            foreach (var snapshot in snapshots)
            {
                if (snapshot.HasMoldTile && viz.moldTilemap != null)
                {
                    viz.moldTilemap.SetTileFlags(snapshot.Cell, TileFlags.None);
                    viz.moldTilemap.SetColor(snapshot.Cell, snapshot.MoldColor);
                }

                if (snapshot.HasOverlayTile && viz.overlayTilemap != null)
                {
                    viz.overlayTilemap.SetTileFlags(snapshot.Cell, TileFlags.None);
                    viz.overlayTilemap.SetColor(snapshot.Cell, snapshot.OverlayColor);
                }
            }
        }

        private IEnumerator RevealDestination(IReadOnlyList<TileVisibilitySnapshot> destinationSnapshots)
        {
            if (destinationSnapshots == null || destinationSnapshots.Count == 0)
            {
                yield break;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, UI.UIEffectConstants.ConidiaAscentDestinationRevealDurationSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                foreach (var snapshot in destinationSnapshots)
                {
                    SetTileAlpha(snapshot, t);
                }

                yield return null;
            }

            RestoreSnapshots(destinationSnapshots);
        }

        private void SetTileAlpha(TileVisibilitySnapshot snapshot, float alphaMultiplier)
        {
            if (snapshot.HasMoldTile && viz.moldTilemap != null)
            {
                viz.moldTilemap.SetTileFlags(snapshot.Cell, TileFlags.None);
                var color = snapshot.MoldColor;
                color.a = snapshot.MoldColor.a * alphaMultiplier;
                viz.moldTilemap.SetColor(snapshot.Cell, color);
            }

            if (snapshot.HasOverlayTile && viz.overlayTilemap != null)
            {
                viz.overlayTilemap.SetTileFlags(snapshot.Cell, TileFlags.None);
                var color = snapshot.OverlayColor;
                color.a = snapshot.OverlayColor.a * alphaMultiplier;
                viz.overlayTilemap.SetColor(snapshot.Cell, color);
            }
        }

        private IEnumerator PlayLandingSettle(GameObject payload, Vector3 destinationCenter)
        {
            float popElapsed = 0f;
            while (popElapsed < UI.UIEffectConstants.ConidiaAscentLandingPopDurationSeconds)
            {
                popElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(popElapsed / UI.UIEffectConstants.ConidiaAscentLandingPopDurationSeconds);
                float scale = Mathf.Lerp(1f, UI.UIEffectConstants.ConidiaAscentLandingPopScale, t);
                payload.transform.position = destinationCenter;
                payload.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            float settleElapsed = 0f;
            while (settleElapsed < UI.UIEffectConstants.ConidiaAscentLandingSettleDurationSeconds)
            {
                settleElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(settleElapsed / UI.UIEffectConstants.ConidiaAscentLandingSettleDurationSeconds);
                float scale = Mathf.Lerp(UI.UIEffectConstants.ConidiaAscentLandingPopScale, 0.92f, t);
                payload.transform.position = destinationCenter;
                payload.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private Vector3 ComputeClusterCenterWorld(Tilemap referenceTilemap, GameBoard board, IReadOnlyList<int> tileIds)
        {
            Vector3 total = Vector3.zero;
            for (int index = 0; index < tileIds.Count; index++)
            {
                total += GetCellCenterWorld(referenceTilemap, board, tileIds[index]);
            }

            return total / Mathf.Max(1, tileIds.Count);
        }

        private Vector3 ComputeOffscreenPoint(Tilemap referenceTilemap, GameBoard board, Vector3 sourceCenter, Vector3 destinationCenter)
        {
            Vector3 topCellCenter = referenceTilemap.GetCellCenterWorld(new Vector3Int(board.Width / 2, board.Height - 1, 0));
            float offscreenY = topCellCenter.y + (referenceTilemap.cellSize.y * 0.75f) + UI.UIEffectConstants.ConidiaAscentOffscreenHeightWorld;
            float offscreenX = Mathf.Lerp(sourceCenter.x, destinationCenter.x, 0.45f);
            return new Vector3(offscreenX, offscreenY, sourceCenter.z);
        }

        private Vector3 GetCellCenterWorld(Tilemap referenceTilemap, GameBoard board, int tileId)
        {
            return referenceTilemap.GetCellCenterWorld(ToCell(board, tileId));
        }

        private static Vector3Int ToCell(GameBoard board, int tileId)
        {
            var (x, y) = board.GetXYFromTileId(tileId);
            return new Vector3Int(x, y, 0);
        }

        private List<int> GetBlockTileIds(GameBoard board, int anchorTileId, int blockSize)
        {
            var (startX, startY) = board.GetXYFromTileId(anchorTileId);
            var tileIds = new List<int>(blockSize * blockSize);
            for (int y = startY; y < startY + blockSize; y++)
            {
                for (int x = startX; x < startX + blockSize; x++)
                {
                    if (x < 0 || y < 0 || x >= board.Width || y >= board.Height)
                    {
                        continue;
                    }

                    tileIds.Add((y * board.Width) + x);
                }
            }

            return tileIds;
        }
    }
}