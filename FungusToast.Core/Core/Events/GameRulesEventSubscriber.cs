using FungusToast.Core.Board;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    /// <summary>
    /// Responsible for subscribing core game rule event handlers to the GameBoard.
    /// These handlers implement core mutation logic and chained effects.
    /// </summary>
    public static class GameRulesEventSubscriber
    {
        /// <summary>
        /// Subscribes all core game rule event handlers to the given GameBoard.
        /// Call once during setup.
        /// </summary>
        public static void Subscribe(GameBoard board)
        {
            // Example: board.CellDeath += OnCellDeath_NecrotoxicConversion;
            // board.CellColonized += OnCellColonized_SomeMutation;
        }

        /// <summary>
        /// Unsubscribes all core game rule event handlers from the given GameBoard.
        /// Call during cleanup or scene unload if necessary.
        /// </summary>
        public static void Unsubscribe(GameBoard board)
        {
            // Example: board.CellDeath -= OnCellDeath_NecrotoxicConversion;
            // board.CellColonized -= OnCellColonized_SomeMutation;
        }

        // Example handler stub
        // private static void OnCellDeath_NecrotoxicConversion(object sender, FungalCellDiedEventArgs args) { /* ... */ }
    }
}
