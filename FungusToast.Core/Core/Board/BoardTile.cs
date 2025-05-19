using FungusToast.Core.Players;

namespace FungusToast.Core.Board
{
    public class BoardTile
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public FungalCell? FungalCell { get; private set; }

        public int TileId { get; }
        public bool IsOccupied => FungalCell != null;

        public BoardTile(int x, int y, int boardWidth)
        {
            X = x;
            Y = y;
            TileId = y * boardWidth + x;
            FungalCell = null;
        }

        public void PlaceFungalCell(FungalCell fungalCell)
        {
            FungalCell = fungalCell;
        }

        public void RemoveFungalCell()
        {
            FungalCell = null;
        }

        // ✅ Proxy accessors to simplify logic
        public bool IsAlive => FungalCell != null && FungalCell.IsAlive;
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
    }
}
