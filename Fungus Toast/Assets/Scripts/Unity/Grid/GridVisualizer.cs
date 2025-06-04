using UnityEngine;
using UnityEngine.Tilemaps;
using FungusToast.Core;
using FungusToast.Core.Board;
using System.Collections.Generic;

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
                    BoardTile boardTile = board.Grid[x, y];
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    // Always render the toast base
                    toastTilemap.SetTile(pos, baseTile);
                    toastTilemap.SetTileFlags(pos, TileFlags.None);
                    toastTilemap.SetColor(pos, Color.white);

                    // Determine overlay
                    TileBase overlayTile = null;
                    Color overlayColor = Color.white;

                    if ((boardTile.FungalCell != null && boardTile.FungalCell.IsToxin))
                    {
                        var toxinCell = boardTile.FungalCell;
                        int? ownerIdNullable = toxinCell?.OwnerPlayerId;

                        if (ownerIdNullable is int ownerId && ownerId >= 0 && ownerId < playerMoldTiles.Length)
                        {
                            overlayTile = playerMoldTiles[ownerId];
                            overlayColor = Color.black * 0.8f;
                        }

                        // Add toxin icon overlay on top
                        overlayTilemap.SetTile(pos, toxinOverlayTile);
                        overlayTilemap.SetTileFlags(pos, TileFlags.None);
                        overlayTilemap.SetColor(pos, Color.white);
                    }
                    else if (boardTile.IsOccupied)
                    {
                        var fungalCell = boardTile.FungalCell;

                        if (fungalCell.IsAlive)
                        {
                            int? playerIdNullable = fungalCell.OwnerPlayerId;
                            if (playerIdNullable is int playerId && playerId >= 0 && playerId < playerMoldTiles.Length)
                            {
                                overlayTile = playerMoldTiles[playerId];
                                overlayColor = Color.white;
                            }
                        }
                        else
                        {
                            overlayTile = deadTile;
                            overlayColor = Color.white;
                        }
                    }

                    if (overlayTile != null)
                    {
                        overlayTilemap.SetTile(pos, overlayTile);
                        overlayTilemap.SetTileFlags(pos, TileFlags.None);
                        overlayTilemap.SetColor(pos, overlayColor);
                        overlayTilemap.RefreshTile(pos);
                    }
                }
            }
        }

        public Tile GetTileForPlayer(int playerId)
        {
            if (playerMoldTiles != null && playerId >= 0 && playerId < playerMoldTiles.Length)
            {
                return playerMoldTiles[playerId];
            }

            Debug.LogWarning($"No tile found for Player ID {playerId}.");
            return null;
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
                if (tile.FungalCell != null &&
                    tile.FungalCell.OwnerPlayerId == playerId &&
                    tile.FungalCell.IsAlive)
                {
                    Vector3Int pos = new Vector3Int(tile.X, tile.Y, 0);

                    overlayTilemap.SetTile(pos, solidHighlightTile);
                    overlayTilemap.SetTileFlags(pos, TileFlags.None);
                    overlayTilemap.SetColor(pos, Color.white);
                    overlayTilemap.RefreshTile(pos);

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
                BoardTile boardTile = board.Grid[pos.x, pos.y];
                TileBase overlayTile = null;
                Color overlayColor = Color.white;

                if ((boardTile.FungalCell != null && boardTile.FungalCell.IsToxin))
                {
                    var toxinCell = boardTile.FungalCell;
                    int? ownerIdNullable = toxinCell?.OwnerPlayerId;

                    if (ownerIdNullable is int ownerId && ownerId >= 0 && ownerId < playerMoldTiles.Length)
                    {
                        overlayTile = playerMoldTiles[ownerId];
                        overlayColor = Color.black * 0.8f;
                    }

                    overlayTilemap.SetTile(pos, toxinOverlayTile);
                    overlayTilemap.SetTileFlags(pos, TileFlags.None);
                    overlayTilemap.SetColor(pos, Color.white);
                }
                else if (boardTile.IsOccupied)
                {
                    var fungalCell = boardTile.FungalCell;

                    if (fungalCell.IsAlive)
                    {
                        int? playerIdNullable = fungalCell.OwnerPlayerId;
                        if (playerIdNullable is int playerId && playerId >= 0 && playerId < playerMoldTiles.Length)
                        {
                            overlayTile = playerMoldTiles[playerId];
                            overlayColor = Color.white;
                        }
                    }
                    else
                    {
                        overlayTile = deadTile;
                        overlayColor = Color.white;
                    }
                }

                // Apply the correct overlay tile
                overlayTilemap.SetTile(pos, overlayTile);
                overlayTilemap.SetTileFlags(pos, TileFlags.None);
                overlayTilemap.SetColor(pos, overlayColor);
                overlayTilemap.RefreshTile(pos);
            }

            highlightedPositions.Clear();
        }

    }
}
