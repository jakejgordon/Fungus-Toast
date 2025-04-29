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

        public void PlaceInitialSpore(int playerId, int x, int y)
        {
            BoardTile tile = Grid[x, y];

            if (!tile.IsOccupied)
            {
                tile.PlaceFungalCell(new FungalCell(playerId));
            }
        }
    }
}
