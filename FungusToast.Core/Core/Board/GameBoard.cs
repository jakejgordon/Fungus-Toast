using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public BoardTile[,] Grid { get; }
        public List<Player> Players { get; }

        private readonly Dictionary<int, FungalCell> tileIdToCell = new();

        public int CurrentGrowthCycle { get; private set; } = 0;

        public GameBoard(int width, int height, int playerCount)
        {
            Width = width;
            Height = height;
            Grid = new BoardTile[width, height];
            Players = new List<Player>(playerCount);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new BoardTile(x, y, width);
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
            List<BoardTile> neighbors = new();
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

        public List<BoardTile> GetOrthogonalNeighbors(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetOrthogonalNeighbors(x, y);
        }

        public BoardTile? GetTile(int x, int y)
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
                int tileId = y * Width + x;
                var cell = new FungalCell(playerId, tileId);
                tile.PlaceFungalCell(cell);
                tileIdToCell[tileId] = cell;
            }
        }

        public FungalCell? GetCell(int tileId)
        {
            tileIdToCell.TryGetValue(tileId, out var cell);
            return cell;
        }

        public void PlaceFungalCell(FungalCell cell)
        {
            tileIdToCell[cell.TileId] = cell;
            var (x, y) = GetXYFromTileId(cell.TileId);
            Grid[x, y].PlaceFungalCell(cell);
        }

        public void RemoveControlFromPlayer(int tileId)
        {
            foreach (var player in Players)
            {
                player.ControlledTileIds.Remove(tileId);
            }
        }

        public List<int> GetAdjacentTileIds(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            List<int> neighbors = new();

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

        public (int x, int y) GetXYFromTileId(int tileId)
        {
            int x = tileId % Width;
            int y = tileId / Width;
            return (x, y);
        }

        public List<FungalCell> GetAllCells()
        {
            return tileIdToCell.Values.ToList();
        }

        public List<FungalCell> GetAllCellsOwnedBy(int playerId)
        {
            return tileIdToCell.Values.Where(c => c.OwnerPlayerId == playerId).ToList();
        }

        public bool SpawnSporeForPlayer(Player player, int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            var tile = GetTile(x, y);

            if (tile == null || tile.IsOccupied)
                return false;

            var cell = new FungalCell(player.PlayerId, tileId);
            tile.PlaceFungalCell(cell);
            tileIdToCell[tileId] = cell;

            player.ControlledTileIds.Add(tileId);
            return true;
        }

        public int CountReclaimedCellsByPlayer(int playerId)
        {
            return tileIdToCell.Values.Count(c =>
                c.IsAlive &&
                c.OwnerPlayerId == playerId &&
                c.OriginalOwnerPlayerId == playerId &&
                c.ReclaimCount > 0);
        }

        public BoardTile? GetTileById(int tileId)
        {
            var (x, y) = GetXYFromTileId(tileId);
            return GetTile(x, y);
        }

        public List<int> GetAllTileIds() => tileIdToCell.Keys.ToList();

        public List<BoardTile> GetDeadTiles()
        {
            return AllTiles().Where(t => t.FungalCell != null && !t.FungalCell.IsAlive).ToList();
        }

        public float GetOccupiedTileRatio()
        {
            int total = Width * Height;
            int occupied = AllTiles().Count(t => t.FungalCell != null);
            return (float)occupied / total;
        }

        public bool ShouldTriggerEndgame()
        {
            return GetOccupiedTileRatio() >= GameBalance.GameEndTileOccupancyThreshold;
        }

        public void IncrementGrowthCycle()
        {
            CurrentGrowthCycle++;
        }
    }
}
