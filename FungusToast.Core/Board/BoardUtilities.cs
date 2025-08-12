using FungusToast.Core.Players;

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

        /// <summary>
        /// Categorizes players by colony size relative to the current player.
        /// Returns players with larger and smaller colonies than the current player.
        /// </summary>
        /// <param name="currentPlayer">The player to compare against</param>
        /// <param name="allPlayers">All players in the game</param>
        /// <param name="board">The game board</param>
        /// <returns>Tuple containing lists of players with larger and smaller colonies</returns>
        public static (List<Player> largerColonies, List<Player> smallerColonies) CategorizePlayersByColonySize(
            Player currentPlayer,
            List<Player> allPlayers,
            GameBoard board)
        {
            int currentPlayerLivingCells = board.GetAllCellsOwnedBy(currentPlayer.PlayerId).Count(c => c.IsAlive);

            var largerColonies = new List<Player>();
            var smallerColonies = new List<Player>();

            foreach (var otherPlayer in allPlayers.Where(p => p.PlayerId != currentPlayer.PlayerId))
            {
                int otherPlayerLivingCells = board.GetAllCellsOwnedBy(otherPlayer.PlayerId).Count(c => c.IsAlive);
                if (otherPlayerLivingCells > currentPlayerLivingCells)
                {
                    largerColonies.Add(otherPlayer);
                }
                else
                {
                    smallerColonies.Add(otherPlayer);
                }
            }

            return (largerColonies, smallerColonies);
        }
    }
}