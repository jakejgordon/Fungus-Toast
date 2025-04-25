using UnityEngine;
using UnityEngine.Tilemaps;
using FungusToast.Core;

namespace FungusToast.Grid
{
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Tilemap & Tiles")]
        public Tilemap tilemap;
        public Tile baseTile;
        public Tile deadTile;
        public Tile[] playerMoldTiles; // Indexed by Player ID

        public void RenderBoard(GameBoard board)
        {
            tilemap.ClearAllTiles();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileState tile = board.Grid[x, y];
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    switch (tile.Status)
                    {
                        case TileStatus.Empty:
                            tilemap.SetTile(pos, baseTile);
                            break;

                        case TileStatus.Occupied:
                            if (tile.OwnerId.HasValue && tile.OwnerId.Value < playerMoldTiles.Length)
                                tilemap.SetTile(pos, playerMoldTiles[tile.OwnerId.Value]);
                            else
                                tilemap.SetTile(pos, baseTile); // fallback
                            break;

                        case TileStatus.Dead:
                            tilemap.SetTile(pos, deadTile);
                            break;
                    }
                }
            }
        }
    }
}
