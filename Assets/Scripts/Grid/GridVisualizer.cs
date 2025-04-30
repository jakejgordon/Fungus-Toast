using UnityEngine;
using UnityEngine.Tilemaps;
using FungusToast.Core;
using FungusToast.Core.Board; // <-- For BoardTile and FungalCell

namespace FungusToast.Grid
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Tilemap & Tiles")]
        public Tilemap tilemap;
        public Tile baseTile;            // For empty toast spaces
        public Tile deadTile;            // For dead mold
        public Tile[] playerMoldTiles;   // For living mold, indexed by PlayerId

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
                        // Empty Tile
                        tilemap.SetTile(tilemapPosition, baseTile);
                    }
                    else
                    {
                        FungalCell fungalCell = boardTile.FungalCell;

                        if (fungalCell.IsAlive)
                        {
                            // Living mold
                            int playerId = fungalCell.OwnerPlayerId;
                            if (playerId >= 0 && playerId < playerMoldTiles.Length)
                            {
                                tilemap.SetTile(tilemapPosition, playerMoldTiles[playerId]);
                            }
                            else
                            {
                                tilemap.SetTile(tilemapPosition, baseTile); // Fallback if no player tile assigned
                            }
                        }
                        else
                        {
                            // Dead mold
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

    }
}
