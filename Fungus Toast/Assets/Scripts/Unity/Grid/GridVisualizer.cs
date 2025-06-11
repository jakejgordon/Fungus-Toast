using UnityEngine;
using UnityEngine.Tilemaps;
using FungusToast.Core;
using FungusToast.Core.Board;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Unity.Grid
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Tilemaps")]
        public Tilemap toastTilemap;      // Base toast layer
        public Tilemap moldTilemap;       // Player mold layer
        public Tilemap overlayTilemap;    // Toxin overlays and highlights
        public Tilemap HoverTileMap;      // Mouseover highlights

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile;

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void RenderBoard(GameBoard board)
        {
            toastTilemap.ClearAllTiles();
            moldTilemap.ClearAllTiles();
            overlayTilemap.ClearAllTiles();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    BoardTile tile = board.Grid[x, y];

                    toastTilemap.SetTile(pos, baseTile);
                    toastTilemap.SetTileFlags(pos, TileFlags.None);
                    toastTilemap.SetColor(pos, Color.white);

                    RenderFungalCellOverlay(tile, pos);
                }
            }
        }

        public void HighlightPlayerTiles(int playerId)
        {
            HoverTileMap.ClearAllTiles();

            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell?.CellType == FungalCellType.Alive &&
                    tile.FungalCell.OwnerPlayerId == playerId)
                {
                    Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);
                    HoverTileMap.SetTile(pos, solidHighlightTile);
                    HoverTileMap.SetTileFlags(pos, TileFlags.None);
                    HoverTileMap.SetColor(pos, Color.white);
                    HoverTileMap.RefreshTile(pos);
                }
            }
        }

        public void ClearHighlights()
        {
            HoverTileMap.ClearAllTiles();
        }

        private void RenderFungalCellOverlay(BoardTile tile, Vector3Int pos)
        {
            TileBase moldTile = null;
            TileBase overlayTile = null;
            Color moldColor = Color.white;
            Color overlayColor = Color.white;

            var cell = tile.FungalCell;
            if (cell == null)
                return;

            switch (cell.CellType)
            {
                case FungalCellType.Alive:
                    if (cell.OwnerPlayerId is int idA && idA >= 0 && idA < playerMoldTiles.Length)
                    {
                        moldTile = playerMoldTiles[idA];
                        moldColor = Color.white;
                    }
                    break;
                case FungalCellType.Dead:
                    overlayTile = deadTile;
                    overlayColor = Color.white;
                    break;
                case FungalCellType.Toxin:
                    if (cell.OwnerPlayerId is int idT && idT >= 0 && idT < playerMoldTiles.Length)
                    {
                        // Optionally show faint mold color under the toxin overlay
                        moldTile = playerMoldTiles[idT];
                        moldColor = new Color(1f, 1f, 1f, 0.4f); // 40% opacity
                    }
                    overlayTile = toxinOverlayTile;
                    overlayColor = Color.white;
                    break;
                default:
                    // Unoccupied or unknown: nothing to render
                    return;
            }

            if (moldTile != null)
            {
                moldTilemap.SetTile(pos, moldTile);
                moldTilemap.SetTileFlags(pos, TileFlags.None);
                moldTilemap.SetColor(pos, moldColor);
                moldTilemap.RefreshTile(pos);
            }

            if (overlayTile != null)
            {
                SetOverlayTile(pos, overlayTile, overlayColor);
            }
        }

        private void SetOverlayTile(Vector3Int pos, TileBase tile, Color color)
        {
            overlayTilemap.SetTile(pos, tile);
            overlayTilemap.SetTileFlags(pos, TileFlags.None);
            overlayTilemap.SetColor(pos, color);
            overlayTilemap.RefreshTile(pos);
        }

        public Tile GetTileForPlayer(int playerId)
        {
            if (playerMoldTiles != null && playerId >= 0 && playerId < playerMoldTiles.Length)
                return playerMoldTiles[playerId];

            Debug.LogWarning($"No tile found for Player ID {playerId}.");
            return null;
        }
    }
}
