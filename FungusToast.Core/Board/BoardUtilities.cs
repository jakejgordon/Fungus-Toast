using FungusToast.Core.Players;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Gets the living cell count for each player in a single pass over the board.
        /// This is more efficient than calling CategorizePlayersByColonySize multiple times.
        /// </summary>
        /// <param name="allPlayers">All players in the game</param>
        /// <param name="board">The game board</param>
        /// <returns>Dictionary mapping player ID to living cell count</returns>
        public static Dictionary<int, int> GetPlayerColonySizes(List<Player> allPlayers, GameBoard board)
        {
            var colonySizes = allPlayers.ToDictionary(p => p.PlayerId, p => 0);

            // Single pass over the board to count living cells for all players
            foreach (var tile in board.AllTiles())
            {
                var cell = tile.FungalCell;
                if (cell != null && cell.IsAlive && cell.OwnerPlayerId.HasValue)
                {
                    if (colonySizes.ContainsKey(cell.OwnerPlayerId.Value))
                    {
                        colonySizes[cell.OwnerPlayerId.Value]++;
                    }
                }
            }

            return colonySizes;
        }

        /// <summary>
        /// Builds colony size categorizations for all players using a single pass over the board.
        /// This is the most efficient way to populate categorizations for multiple players.
        /// </summary>
        /// <param name="allPlayers">All players in the game</param>
        /// <param name="board">The game board</param>
        /// <returns>Dictionary mapping player ID to their colony size categorization</returns>
        public static Dictionary<int, (List<Player> largerColonies, List<Player> smallerColonies)> BuildAllColonySizeCategorizations(
            List<Player> allPlayers, 
            GameBoard board)
        {
            // Get colony sizes for all players in a single pass
            var colonySizes = GetPlayerColonySizes(allPlayers, board);
            
            // Build categorizations for each player
            var categorizations = new Dictionary<int, (List<Player> largerColonies, List<Player> smallerColonies)>();
            
            foreach (var currentPlayer in allPlayers)
            {
                int currentPlayerLivingCells = colonySizes[currentPlayer.PlayerId];
                
                var largerColonies = new List<Player>();
                var smallerColonies = new List<Player>();
                
                foreach (var otherPlayer in allPlayers.Where(p => p.PlayerId != currentPlayer.PlayerId))
                {
                    int otherPlayerLivingCells = colonySizes[otherPlayer.PlayerId];
                    if (otherPlayerLivingCells > currentPlayerLivingCells)
                    {
                        largerColonies.Add(otherPlayer);
                    }
                    else
                    {
                        smallerColonies.Add(otherPlayer);
                    }
                }
                
                categorizations[currentPlayer.PlayerId] = (largerColonies, smallerColonies);
            }
            
            return categorizations;
        }

        /// <summary>
        /// Gets comprehensive cell count summaries for all players in a single pass over the board.
        /// This is more efficient than iterating over GetAllCellsOwnedBy for each player separately.
        /// </summary>
        /// <param name="allPlayers">All players in the game</param>
        /// <param name="board">The game board</param>
        /// <returns>Dictionary mapping player ID to their board summary</returns>
        public static Dictionary<int, PlayerBoardSummary> GetPlayerBoardSummaries(List<Player> allPlayers, GameBoard board)
        {
            // Initialize counters for all players
            var livingCounts = allPlayers.ToDictionary(p => p.PlayerId, p => 0);
            var deadCounts = allPlayers.ToDictionary(p => p.PlayerId, p => 0);
            var toxinCounts = allPlayers.ToDictionary(p => p.PlayerId, p => 0);

            // Single pass over the board to count all cell types for all players
            foreach (var tile in board.AllTiles())
            {
                var cell = tile.FungalCell;
                if (cell?.OwnerPlayerId.HasValue == true)
                {
                    int playerId = cell.OwnerPlayerId.Value;
                    
                    // Only count if this player is in our list
                    if (livingCounts.ContainsKey(playerId))
                    {
                        if (cell.IsAlive)
                        {
                            livingCounts[playerId]++;
                        }
                        else if (cell.IsDead)
                        {
                            deadCounts[playerId]++;
                        }
                        else if (cell.IsToxin)
                        {
                            toxinCounts[playerId]++;
                        }
                    }
                }
            }

            // Build summary objects
            var summaries = new Dictionary<int, PlayerBoardSummary>();
            foreach (var player in allPlayers)
            {
                summaries[player.PlayerId] = new PlayerBoardSummary(
                    player.PlayerId,
                    livingCounts[player.PlayerId],
                    deadCounts[player.PlayerId],
                    toxinCounts[player.PlayerId]
                );
            }

            return summaries;
        }
    }
}