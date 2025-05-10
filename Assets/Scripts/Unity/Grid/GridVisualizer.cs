using UnityEngine;
using UnityEngine.Tilemaps;
using FungusToast.Core;
using FungusToast.Core.Board; // For BoardTile and FungalCell
using System.Collections.Generic;

namespace FungusToast.Unity.Grid
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Tilemap & Tiles")]
        public Tilemap tilemap;
        public Tile baseTile;            // For empty toast spaces
        public Tile deadTile;            // For dead mold
        public Tile[] playerMoldTiles;   // For living mold, indexed by PlayerId

        private GameBoard board;
        private List<Vector3Int> highlightedPositions = new List<Vector3Int>();

        [Header("Highlight Settings")]
        [SerializeField] private Tile solidHighlightTile;

        public void Initialize(GameBoard board)
        {
            this.board = board;
        }

        public void RenderBoard(GameBoard board)
        {
            tilemap.ClearAllTiles();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    BoardTile boardTile = board.Grid[x, y];
                    Vector3Int tilemapPosition = new Vector3Int(x, y, 0);

                    if (!boardTile.IsOccupied)
                    {
                        tilemap.SetTile(tilemapPosition, baseTile);
                    }
                    else
                    {
                        FungalCell fungalCell = boardTile.FungalCell;

                        if (fungalCell.IsAlive)
                        {
                            int playerId = fungalCell.OwnerPlayerId;
                            if (playerId >= 0 && playerId < playerMoldTiles.Length)
                            {
                                tilemap.SetTile(tilemapPosition, playerMoldTiles[playerId]);
                            }
                            else
                            {
                                tilemap.SetTile(tilemapPosition, baseTile);
                            }
                        }
                        else
                        {
                            tilemap.SetTile(tilemapPosition, deadTile);
                        }
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
            Debug.Log($"✨ HighlightPlayerTiles called for Player {playerId}");

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

                    tilemap.SetTile(pos, solidHighlightTile); // visually replace the tile
                    tilemap.SetTileFlags(pos, TileFlags.None);
                    tilemap.SetColor(pos, Color.white); // ensure no residual tint
                    tilemap.RefreshTile(pos);

                    highlightedPositions.Add(pos);
                    Debug.Log($"✅ Highlighted tile at {pos}");
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
                var tile = board.Grid[pos.x, pos.y];
                TileBase tileToRestore;

                if (!tile.IsOccupied)
                {
                    tileToRestore = baseTile;
                }
                else if (!tile.FungalCell.IsAlive)
                {
                    tileToRestore = deadTile;
                }
                else
                {
                    int playerId = tile.FungalCell.OwnerPlayerId;
                    tileToRestore = (playerId >= 0 && playerId < playerMoldTiles.Length)
                        ? playerMoldTiles[playerId]
                        : baseTile;
                }

                tilemap.SetTile(pos, tileToRestore);
                tilemap.SetTileFlags(pos, TileFlags.None);
                tilemap.SetColor(pos, Color.white); // Reset any tint that might have been applied
                tilemap.RefreshTile(pos);
            }

            highlightedPositions.Clear();
        }


    }
}
