using System.Collections.Generic;

namespace FungusToast.Core
{
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public TileState[,] Grid { get; }
        public List<PlayerData> Players { get; }

        public GameBoard(int width, int height, int playerCount)
        {
            Width = width;
            Height = height;
            Grid = new TileState[width, height];
            Players = new List<PlayerData>();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Grid[x, y] = new TileState(x, y);

            for (int i = 0; i < playerCount; i++)
                Players.Add(new PlayerData(i, $"Mold {i + 1}"));
        }

        public IEnumerable<TileState> GetNeighbors(TileState tile)
        {
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };

            for (int d = 0; d < 4; d++)
            {
                int nx = tile.X + dx[d];
                int ny = tile.Y + dy[d];

                if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
                    yield return Grid[nx, ny];
            }
        }

        public void PlaceInitialSpore(int playerId, int x, int y)
        {
            var tile = Grid[x, y];
            tile.OwnerId = playerId;
            tile.Status = TileStatus.Occupied;
            tile.Age = 0;
        }
    }
}

