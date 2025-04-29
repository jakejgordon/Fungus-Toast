using FungusToast.Core.Players;

namespace FungusToast.Core.Board
{
    public class BoardTile
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public FungalCell FungalCell { get; private set; }

        public BoardTile(int x, int y)
        {
            X = x;
            Y = y;
            FungalCell = null;
        }

        public bool IsOccupied => FungalCell != null;

        public void PlaceFungalCell(FungalCell fungalCell)
        {
            FungalCell = fungalCell;
        }

        public void RemoveFungalCell()
        {
            FungalCell = null;
        }
    }
}
