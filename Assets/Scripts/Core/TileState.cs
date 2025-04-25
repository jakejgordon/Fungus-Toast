namespace FungusToast.Core
{
    public enum TileStatus
    {
        Empty,
        Occupied,
        Dead
    }

    public class TileState
    {
        public int X { get; }
        public int Y { get; }
        public int? OwnerId { get; set; }  // Null if unclaimed
        public TileStatus Status { get; set; }
        public int Age { get; set; } // Turns since claimed

        public TileState(int x, int y)
        {
            X = x;
            Y = y;
            Status = TileStatus.Empty;
            OwnerId = null;
            Age = 0;
        }
    }
}