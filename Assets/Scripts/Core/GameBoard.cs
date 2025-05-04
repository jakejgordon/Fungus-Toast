using System.Collections.Generic;
using FungusToast.Core.Board;  // <-- for Tile and FungalCell
using FungusToast.Core.Players;

namespace FungusToast.Core
{
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public BoardTile[,] Grid { get; }
        public List<Player> Players { get; } // <-- Updated: full Player objects now

        private Dictionary<int, FungalCell> tileIdToCell = new();

        public GameBoard(int width, int height, int playerCount)
        {
            Width = width;
            Height = height;
            Grid = new BoardTile[width, height];
            Players = new List<Player>(playerCount); // <-- Players will be assigned externally now

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new BoardTile(x, y);
                }
            }
        }

        public IEnumerable<BoardTile> AllTiles()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return Grid[x, y];
                }
            }
        }

        public List<BoardTile> GetOrthogonalNeighbors(int x, int y)
        {
            List<BoardTile> neighbors = new List<BoardTile>();

            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };

            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];

                if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                {
                    neighbors.Add(Grid[nx, ny]);
                }
            }

            return neighbors;
        }

        public BoardTile GetTile(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                return Grid[x, y];
            }
            return null;
        }

        public void PlaceInitialSpore(int playerId, int x, int y)
        {
            BoardTile tile = Grid[x, y];

            if (!tile.IsOccupied)
            {
                int tileId = y * Width + x; // consistent unique ID
                var cell = new FungalCell(playerId, tileId);
                tile.PlaceFungalCell(cell);
                tileIdToCell[tileId] = cell;
            }
        }

        public FungalCell GetCell(int tileId)
        {
            if (tileIdToCell.TryGetValue(tileId, out var cell))
            {
                return cell;
            }

            return null;
        }

        public void RegisterCell(FungalCell cell)
        {
            if (cell != null)
            {
                tileIdToCell[cell.TileId] = cell;
            }
        }

        public List<int> GetAdjacentTileIds(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            List<int> neighbors = new List<int>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                    {
                        int neighborId = ny * Width + nx;
                        neighbors.Add(neighborId);
                    }
                }
            }

            return neighbors;
        }

        private (int x, int y) GetXYFromTileId(int tileId)
        {
            int x = tileId % Width;
            int y = tileId / Width;
            return (x, y);
        }
    }
}
