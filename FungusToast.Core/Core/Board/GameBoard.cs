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

        public int CurrentRound { get; private set; } = 1;
        public int CurrentGrowthCycle { get; private set; } = 0;

        public int TotalTiles => Width * Height;

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
                for (int y = 0; y < Height; y++)
                    yield return Grid[x, y];
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
                    neighbors.Add(Grid[nx, ny]);
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
                return Grid[x, y];
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

                Players[playerId].ControlledTileIds.Add(tileId); // 🔥 Required for growth + logic
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
                player.ControlledTileIds.Remove(tileId);
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

        public List<BoardTile> GetAdjacentTiles(int tileId)
        {
            List<BoardTile> result = new();
            foreach (int neighborId in GetAdjacentTileIds(tileId))
            {
                var tile = GetTileById(neighborId);
                if (tile != null)
                    result.Add(tile);
            }
            return result;
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
                c.CellType == FungalCellType.Alive &&
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
            return AllTiles().Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Dead).ToList();
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

        public IEnumerable<FungalCell> AllLivingFungalCells()
        {
            return AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Alive)
                .Select(t => t.FungalCell!);
        }


        public IEnumerable<(BoardTile tile, FungalCell cell)> AllLivingFungalCellsWithTiles()
        {
            return AllTiles()
                .Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Alive)
                .Select(t => (t, t.FungalCell!));
        }

        public IEnumerable<BoardTile> AllToxinTiles()
        {
            return AllTiles().Where(t => t.FungalCell?.CellType == FungalCellType.Toxin);
        }

        public IEnumerable<FungalCell> AllToxinFungalCells()
        {
            return AllTiles()
                .Select(t => t.FungalCell)
                .Where(c => c != null && c.CellType == FungalCellType.Toxin)
                .Cast<FungalCell>();
        }

        public IEnumerable<BoardTile> GetAdjacentLivingTiles(int tileId, int? excludePlayerId = null)
        {
            foreach (int neighborId in GetAdjacentTileIds(tileId))
            {
                var tile = GetTileById(neighborId);
                if (tile == null || tile.FungalCell == null || tile.FungalCell.CellType != FungalCellType.Alive)
                    continue;

                var cell = tile.FungalCell;
                if (excludePlayerId.HasValue && cell.OwnerPlayerId == excludePlayerId.Value)
                    continue;

                yield return tile;
            }
        }

        public void ExpireToxinTiles(int currentGrowthCycle)
        {
            var allToxinTiles = AllToxinFungalCells().ToList(); // Snapshot to avoid collection issues
            foreach (var cell in allToxinTiles)
            {
                if (cell.HasToxinExpired(currentGrowthCycle))
                {
                    var tile = GetTileById(cell.TileId);
                    tile?.RemoveFungalCell(); // Clear the cell entirely from the board
                }
            }
        }

        public void IncrementRound()
        {
            CurrentRound++;
        }

        public List<int> GetTileLine(int startTileId, CardinalDirection direction, int length, bool includeStartingTile = false)
        {
            var result = new List<int>();
            int currentTileId = startTileId;

            if (includeStartingTile)
            {
                result.Add(currentTileId);
                if (result.Count >= length) return result;
            }

            for (int i = 0; i < length; i++)
            {
                int nextTileId = GetNeighborTileId(currentTileId, direction);
                if (nextTileId == -1) break; // Edge of board or invalid
                result.Add(nextTileId);
                if (result.Count >= length) break;
                currentTileId = nextTileId;
            }
            return result;
        }


        // You'll need a helper to get a neighbor tile ID in a given direction:
        public int GetNeighborTileId(int tileId, CardinalDirection direction)
        {
            // Example assuming a 2D grid and tileId is mapped row-major: tileId = y * width + x
            int x = tileId % Width;
            int y = tileId / Width;

            switch (direction)
            {
                case CardinalDirection.North: y -= 1; break;
                case CardinalDirection.South: y += 1; break;
                case CardinalDirection.East: x += 1; break;
                case CardinalDirection.West: x -= 1; break;
            }

            // Check for board bounds
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return -1;

            return y * Width + x;
        }

    }
}
