using System.Collections.Generic;
using FungusToast.Core.Board;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid.Helpers
{
    /// <summary>Owns a continuous, shader-animated rot surface; never changes board state.</summary>
    internal sealed class RotSurfaceRenderer
    {
        private readonly Tilemap tilemap;
        private readonly GameObject surface;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MeshRenderer renderer;
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector2> localUvs = new();
        private readonly List<Vector2> boardUvs = new();
        private readonly List<Vector4> exposedEdges = new();
        private readonly List<Vector4> missingDiagonals = new();
        private readonly List<int> triangles = new();
        private GameBoard renderedBoard;
        private bool dirty = true;

        public bool IsAvailable => material != null;

        public RotSurfaceRenderer(Tilemap tilemap)
        {
            this.tilemap = tilemap;
            // Resources keeps the shader in player builds without scene/material wiring.
            var shader = Resources.Load<Shader>("Rot/RotSurface");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("[RotSurfaceRenderer] Rot/RotSurface shader missing or unsupported; using static rot tiles.");
                return;
            }
            material = new Material(shader) { name = "Rot surface (runtime)", hideFlags = HideFlags.DontSave };
            for (int i = 1; i <= 5; i++)
            {
                var art = Resources.Load<Tile>($"Rot/rot_{i:00}");
                if (art == null || art.sprite == null)
                {
                    Debug.LogError($"[RotSurfaceRenderer] Missing rot art: Resources/Rot/rot_{i:00}.asset");
                    Object.Destroy(material);
                    material = null;
                    return;
                }
                // These canonical sprites are full-image slices, not atlas-packed sprites.
                material.SetTexture($"_Rot{i}", art.sprite.texture);
            }
            surface = new GameObject("Rot surface") { layer = tilemap.gameObject.layer };
            surface.transform.SetParent(tilemap.transform, false);
            mesh = new Mesh { name = "Rot surface mesh", hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var reference = tilemap.GetComponent<TilemapRenderer>();
            if (reference != null)
            {
                renderer.sortingLayerID = reference.sortingLayerID;
                renderer.sortingOrder = reference.sortingOrder;
            }
            renderer.enabled = false;
        }

        public void Invalidate() => dirty = true;

        public void Clear()
        {
            renderedBoard = null;
            dirty = true;
            if (renderer != null) renderer.enabled = false;
            if (mesh != null) mesh.Clear();
        }

        public void Refresh(GameBoard board)
        {
            if (!IsAvailable) return;
            if (!ReferenceEquals(renderedBoard, board)) dirty = true;
            if (!dirty) return;
            dirty = false;
            renderedBoard = board;
            vertices.Clear(); localUvs.Clear(); boardUvs.Clear(); exposedEdges.Clear(); missingDiagonals.Clear(); triangles.Clear();
            if (board != null)
            {
                foreach (var tile in board.AllTiles())
                {
                    if (!tile.HasRot || tile.IsBlocked) continue;
                    int x = tile.X, y = tile.Y;
                    var edges = new Vector4(
                        HasRot(board, x - 1, y) ? 0f : 1f,
                        HasRot(board, x + 1, y) ? 0f : 1f,
                        HasRot(board, x, y - 1) ? 0f : 1f,
                        HasRot(board, x, y + 1) ? 0f : 1f);
                    var diagonals = new Vector4(
                        HasRot(board, x - 1, y - 1) ? 0f : 1f,
                        HasRot(board, x + 1, y - 1) ? 0f : 1f,
                        HasRot(board, x - 1, y + 1) ? 0f : 1f,
                        HasRot(board, x + 1, y + 1) ? 0f : 1f);
                    int first = vertices.Count;
                    AddVertex(x, y, 0f, 0f, edges, diagonals);
                    AddVertex(x, y, 1f, 0f, edges, diagonals);
                    AddVertex(x, y, 1f, 1f, edges, diagonals);
                    AddVertex(x, y, 0f, 1f, edges, diagonals);
                    triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
                    triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
                }
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, localUvs);
            mesh.SetUVs(1, boardUvs);
            mesh.SetUVs(2, exposedEdges);
            mesh.SetUVs(3, missingDiagonals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            renderer.enabled = vertices.Count != 0;
        }

        private static bool HasRot(GameBoard board, int x, int y)
        {
            var tile = board.GetTile(x, y);
            return tile != null && tile.HasRot && !tile.IsBlocked;
        }

        private void AddVertex(int x, int y, float u, float v, Vector4 edges, Vector4 diagonals)
        {
            vertices.Add(tilemap.CellToLocalInterpolated(new Vector3(x + u, y + v, 0f)));
            localUvs.Add(new Vector2(u, v));
            boardUvs.Add(new Vector2(x + u, y + v));
            exposedEdges.Add(edges);
            missingDiagonals.Add(diagonals);
        }

        public void Dispose()
        {
            if (surface != null) Object.Destroy(surface);
            if (mesh != null) Object.Destroy(mesh);
            if (material != null) Object.Destroy(material);
        }
    }
}
