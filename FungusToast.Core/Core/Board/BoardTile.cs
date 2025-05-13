using FungusToast.Core.Players;

namespace FungusToast.Core.Board
{
    public class BoardTile
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public FungalCell? FungalCell { get; private set; }

        // Add TileId as a derived property (non-breaking)
        public int TileId => Y * GameBoardWidth + X;

        public bool IsOccupied => FungalCell != null;

        // Backed static width value to compute tile IDs consistently
        private static int GameBoardWidth = 0;

        // Called during board construction
        public static void SetBoardWidth(int width)
        {
            GameBoardWidth = width;
        }

        public BoardTile(int x, int y)
        {
            X = x;
            Y = y;
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
    }
}
