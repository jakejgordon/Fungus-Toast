using FungusToast.Core.Board;
using FungusToast.Unity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FungusToast.Unity.Events
{
    /// <summary>
    /// Responsible for subscribing Unity UI event handlers to the GameBoard.
    /// These handlers update visuals, animations, and presentation.
    /// </summary>
    public static class GameUIEventSubscriber
    {
        /// <summary>
        /// Subscribes all UI event handlers to the GameBoard.
        /// Pass in references to required UI managers/components as needed.
        /// </summary>
        public static void Subscribe(GameBoard board, GameUIManager uiManager)
        {
            // Example: board.CellColonized += (playerId, tileId) => uiManager.ShowColonizeAnimation(tileId, playerId);
            // board.CellDeath += (sender, args) => uiManager.ShowDeathEffect(args.TileId, args.OwnerPlayerId);
        }

        /// <summary>
        /// Unsubscribes all UI event handlers from the GameBoard.
        /// </summary>
        public static void Unsubscribe(GameBoard board, GameUIManager uiManager)
        {
            // Example: board.CellColonized -= (playerId, tileId) => uiManager.ShowColonizeAnimation(tileId, playerId);
            // board.CellDeath -= (sender, args) => uiManager.ShowDeathEffect(args.TileId, args.OwnerPlayerId);
        }

        // Optionally, store actual delegate references if needed for unsubscribing.
    }
}
