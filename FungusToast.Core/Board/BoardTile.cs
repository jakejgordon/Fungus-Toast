using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Board
{
    public class BoardTile
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int TileId { get; }

        public FungalCell? FungalCell { get; private set; }
        public bool IsOccupied => FungalCell != null;

        public BoardTile(int x, int y, int boardWidth)
        {
            X = x;
            Y = y;
            TileId = y * boardWidth + x;
        }

        public void PlaceFungalCell(FungalCell fungalCell)
        {
            FungalCell = fungalCell;
        }

        public void RemoveFungalCell()
        {
            FungalCell = null;
        }

        // Proxy accessors
        public bool IsAlive => FungalCell?.IsAlive == true;
        public bool IsDead => FungalCell?.IsDead == true;
        public bool IsToxin => FungalCell?.IsToxin == true;
        public bool IsReclaimable => FungalCell?.IsReclaimable == true;
        public bool IsResistant => FungalCell?.IsResistant == true;
        public FungalCellType? CellType => FungalCell?.CellType;

        public int OriginalOwnerPlayerId => FungalCell?.OriginalOwnerPlayerId ?? -1;

        public void ReclaimAsLiving(int newOwnerPlayerId)
        {
            FungalCell?.Reclaim(newOwnerPlayerId);
        }

        public int GrowthCycleAge
        {
            get => FungalCell?.GrowthCycleAge ?? 0;
            set
            {
                if (FungalCell != null)
                {
                    FungalCell.SetGrowthCycleAge(value);
                }
            }
        }

        public int DistanceTo(BoardTile other)
        {
            return Math.Abs(this.X - other.X) + Math.Abs(this.Y - other.Y);
        }

        /// <summary>
        /// Returns true if this tile is on the border of the board.
        /// </summary>
        public bool IsOnBorder(int boardWidth, int boardHeight)
        {
            return X == 0 || X == boardWidth - 1 || Y == 0 || Y == boardHeight - 1;
        }
    }
}
