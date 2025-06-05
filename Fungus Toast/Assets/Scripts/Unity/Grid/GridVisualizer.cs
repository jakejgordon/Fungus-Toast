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
        public Tilemap overlayTilemap;    // Fungal overlays and toxins

        [Header("Tiles")]
        public Tile baseTile;             // Toast base
        public Tile deadTile;             // Dead mold
        public Tile[] playerMoldTiles;    // Living mold, indexed by PlayerId
        public Tile toxinOverlayTile;     // Toxin icon overlay
        [SerializeField] private Tile solidHighlightTile; // Highlighting

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void RenderBoard(GameBoard board)
        {
            toastTilemap.ClearAllTiles();
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
            if (board == null || solidHighlightTile == null)
            {
                Debug.LogError("❌ Board or Solid Highlight Tile not assigned!");
                return;
            }

            highlightedPositions.Clear();

            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell?.IsAlive == true &&
                    tile.FungalCell.OwnerPlayerId == playerId)
                {
                    Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);
                    SetOverlayTile(pos, solidHighlightTile, Color.white);
                    highlightedPositions.Add(pos);
                }
            }

            if (highlightedPositions.Count == 0)
            {
                Debug.LogWarning($"⚠️ No living fungal cells found for Player {playerId} to highlight.");
            }
        }

        public void ClearHighlights()
        {
            if (board == null || highlightedPositions == null)
                return;

            foreach (var pos in highlightedPositions)
            {
                BoardTile tile = board.Grid[pos.x, pos.y];
                RenderFungalCellOverlay(tile, pos);
            }

            highlightedPositions.Clear();
        }

        private void RenderFungalCellOverlay(BoardTile tile, Vector3Int pos)
        {
            TileBase overlayTile = null;
            Color overlayColor = Color.white;

            if (tile.FungalCell?.IsToxin == true)
            {
                int? ownerId = tile.FungalCell.OwnerPlayerId;
                if (ownerId is int id && id >= 0 && id < playerMoldTiles.Length)
                {
                    overlayTile = playerMoldTiles[id];
                    overlayColor = Color.black * 0.8f;
                    SetOverlayTile(pos, overlayTile, overlayColor);
                }

                // Toxin icon always goes on top
                SetOverlayTile(pos, toxinOverlayTile, Color.white);
                return;
            }

            if (tile.IsOccupied)
            {
                var cell = tile.FungalCell;
                if (cell?.IsAlive == true)
                {
                    int? playerId = cell.OwnerPlayerId;
                    if (playerId is int id && id >= 0 && id < playerMoldTiles.Length)
                    {
                        overlayTile = playerMoldTiles[id];
                    }
                }
                else
                {
                    overlayTile = deadTile;
                }
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
