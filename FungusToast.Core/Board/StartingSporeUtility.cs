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
        /// <param name="rng">Random number generator for shuffling player order</param>
        public static void PlaceStartingSpores(GameBoard board, List<Player> players, Random rng)
        {
            float radius = Math.Min(board.Width, board.Height) * 0.35f;
            float centerX = board.Width / 2f;
            float centerY = board.Height / 2f;

            // Create a list of shuffled player indices for variety
            var shuffledPlayerIndices = Enumerable.Range(0, players.Count)
                .OrderBy(_ => rng.Next())
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * (float)Math.PI * 2f / players.Count;
                int px = Math.Clamp((int)Math.Round(centerX + radius * Math.Cos(angle)), 0, board.Width - 1);
                int py = Math.Clamp((int)Math.Round(centerY + radius * Math.Sin(angle)), 0, board.Height - 1);
                
                // Use PlaceInitialSpore to ensure proper resistant spore placement
                board.PlaceInitialSpore(shuffledPlayerIndices[i], px, py);
            }
        }
    }
}