using FungusToast.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FungusToast.Unity.Grid
{
    public partial class GridVisualizer
    {
        private const int RotVisualVariantCount = 5;
        private Tile[] rotVisualTiles;

        private bool RenderRotTile(BoardTile tile, Vector3Int position)
        {
            if (tile == null || !tile.HasRot || moldTilemap == null) return false;
            EnsureRotVisualTiles();
            // Stable coordinate variation: presentation never consumes the gameplay RNG.
            int variant = (int)(((uint)position.x * 73856093u ^ (uint)position.y * 19349663u) % RotVisualVariantCount);
            moldTilemap.SetTile(position, rotVisualTiles[variant]);
            moldTilemap.SetTileFlags(position, TileFlags.None);
            moldTilemap.SetColor(position, Color.white);
            moldTilemap.SetTransformMatrix(position, Matrix4x4.identity);
            overlayTilemap?.SetTile(position, null);
            return true;
        }

        private void EnsureRotVisualTiles()
        {
            if (rotVisualTiles != null) return;
            rotVisualTiles = new Tile[RotVisualVariantCount];
            for (int i = 0; i < rotVisualTiles.Length; i++)
            {
                // Resource Tile assets reference the canonical art in Sprites/Tiles/Rot.
                string path = $"Rot/rot_{i + 1:00}";
                Tile authoredTile = Resources.Load<Tile>(path);
                Sprite sprite = authoredTile != null ? authoredTile.sprite : null;
                if (sprite == null) Debug.LogError($"[GridVisualizer] Missing rot Tile or sprite: Resources/{path}.asset");
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = $"Rot visual {i + 1}";
                tile.hideFlags = HideFlags.DontSave;
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                rotVisualTiles[i] = tile;
            }
        }

        private void DisposeRotVisualTiles()
        {
            if (rotVisualTiles == null) return;
            foreach (var tile in rotVisualTiles) if (tile != null) Destroy(tile);
            rotVisualTiles = null;
        }
    }
}
