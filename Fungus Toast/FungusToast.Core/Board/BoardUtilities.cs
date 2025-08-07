namespace FungusToast.Core.Board
{
    public static class BoardUtilities
    {
        /// <summary>
        /// Checks if a tile is within the specified distance from any edge of the board.
        /// </summary>
        /// <param name="tile">The tile to check</param>
        /// <param name="width">Board width</param>
        /// <param name="height">Board height</param>
        /// <param name="distance">Maximum distance from edge (0 = exactly on border)</param>
        /// <returns>True if the tile is within distance tiles of any edge</returns>
        public static bool IsWithinEdgeDistance(BoardTile tile, int width, int height, int distance)
        {
            return tile.X < distance || tile.Y < distance || 
                   (width - tile.X - 1) < distance || (height - tile.Y - 1) < distance;
        }

        /// <summary>
        /// Checks if a tile is exactly on the border of the board.
        /// </summary>
        public static bool IsOnBorder(BoardTile tile, int width, int height)
        {
            return IsWithinEdgeDistance(tile, width, height, 1);
        }
    }
}