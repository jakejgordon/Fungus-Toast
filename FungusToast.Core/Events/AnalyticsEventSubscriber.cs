using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    /// <summary>
    /// Responsible for subscribing analytics and reporting event handlers to the GameBoard.
    /// These handlers log events and update analytics via ISimulationObserver or other reporting tools.
    /// </summary>
    public static class AnalyticsEventSubscriber
    {
        /// <summary>
        /// Subscribes all analytics event handlers to the GameBoard.
        /// Pass in a reporting observer or logger as needed (nullable for production).
        /// </summary>
        public static void Subscribe(GameBoard board, ISimulationObserver? observer = null)
        {
            if(observer == null) return; // No observer, no events to subscribe

            // Example: board.CellDeath += (sender, args) => observer?.RecordCellDeath(args.OwnerPlayerId, args.Reason, 1);
            // board.CellColonized += (playerId, tileId) => observer?.RecordColonization(playerId, tileId);

            board.CellDeath += (sender, e) =>
            {
                // call observer methods here
                observer.RecordCellDeath(e.OwnerPlayerId, e.Reason, 1);
            };

            board.NecrotoxicConversion += (playerId, tileId, oldOwnerId) =>
            {
                observer.RecordNecrotoxicConversionReclaim(playerId, 1);
            };
        }

        /// <summary>
        /// Unsubscribes all analytics event handlers from the GameBoard.
        /// </summary>
        public static void Unsubscribe(GameBoard board, ISimulationObserver observer)
        {
            // If using direct lambda subscriptions, you must keep a reference to the delegate to unsubscribe.
        }
    }
}
