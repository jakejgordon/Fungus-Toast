using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Board
{
    /// <summary>
    /// Utility for placing starting spores in a strategic circular pattern.
    /// Consolidates the duplicated spore placement logic between Unity GameManager and GameSimulator.
    /// </summary>
    public static class StartingSporeUtility
    {
        /// <summary>
        /// Places starting spores for all players in a circular pattern around the board center.
        /// This ensures fair starting positions and consistent behavior between Unity and simulation.
        /// </summary>
        /// <param name="board">Game board to place spores on</param>
        /// <param name="players">List of players to place spores for</param>
        /// <param name="rng">Random number generator for optional shuffling of player order</param>
        /// <param name="shufflePlayerOrder">Whether to shuffle player-to-position assignment before placement</param>
        public static void PlaceStartingSpores(GameBoard board, List<Player> players, Random rng, bool shufflePlayerOrder = true)
        {
            float radius = Math.Min(board.Width, board.Height) * 0.35f;
            float centerX = board.Width / 2f;
            float centerY = board.Height / 2f;

            var playerIndices = Enumerable.Range(0, players.Count).ToList();
            if (shufflePlayerOrder)
            {
                playerIndices = playerIndices
                    .OrderBy(_ => rng.Next())
                    .ToList();
            }

            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * (float)Math.PI * 2f / players.Count;
                int px = Math.Clamp((int)Math.Round(centerX + radius * Math.Cos(angle)), 0, board.Width - 1);
                int py = Math.Clamp((int)Math.Round(centerY + radius * Math.Sin(angle)), 0, board.Height - 1);
                
                // Use PlaceInitialSpore to ensure proper resistant spore placement
                board.PlaceInitialSpore(playerIndices[i], px, py);
            }
        }
    }
}