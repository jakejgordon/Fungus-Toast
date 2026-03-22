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
        private sealed class SubscriptionSet
        {
            public EventHandler<FungalCellDiedEventArgs> CellDeathHandler;
            public GameBoard.NecrotoxicConversionEventHandler NecrotoxicConversionHandler;
            public GameBoard.NutrientPatchConsumedEventHandler NutrientPatchConsumedHandler;
        }

        private static readonly Dictionary<GameBoard, SubscriptionSet> subscriptionsByBoard = new();

        /// <summary>
        /// Subscribes all analytics event handlers to the GameBoard.
        /// Pass in a reporting observer or logger as needed (nullable for production).
        /// </summary>
        public static void Subscribe(GameBoard board, ISimulationObserver observer)
        {
            if (board == null || observer == null) return; // No observer, no events to subscribe

            Unsubscribe(board, observer);

            var subscriptions = new SubscriptionSet();

            // Example: board.CellDeath += (sender, args) => observer.RecordCellDeath(args.OwnerPlayerId, args.Reason, 1);
            // board.CellColonized += (playerId, tileId) => observer.RecordColonization(playerId, tileId);

            subscriptions.CellDeathHandler = (sender, e) =>
            {
                // call observer methods here
                observer.RecordCellDeath(e.OwnerPlayerId, e.Reason, 1);
            };
            board.CellDeath += subscriptions.CellDeathHandler;

            subscriptions.NecrotoxicConversionHandler = (playerId, tileId, oldOwnerId) =>
            {
                observer.RecordNecrotoxicConversionReclaim(playerId, 1);
            };
            board.NecrotoxicConversion += subscriptions.NecrotoxicConversionHandler;

            subscriptions.NutrientPatchConsumedHandler = (playerId, nutrientTileId, destinationTileId, patchType, rewardType, rewardAmount) =>
            {
                observer.RecordNutrientPatchConsumed(playerId, nutrientTileId, patchType, rewardType, rewardAmount);
            };
            board.NutrientPatchConsumed += subscriptions.NutrientPatchConsumedHandler;

            subscriptionsByBoard[board] = subscriptions;
        }

        /// <summary>
        /// Unsubscribes all analytics event handlers from the GameBoard.
        /// </summary>
        public static void Unsubscribe(GameBoard board, ISimulationObserver observer)
        {
            if (board == null || !subscriptionsByBoard.TryGetValue(board, out var subscriptions))
            {
                return;
            }

            board.CellDeath -= subscriptions.CellDeathHandler;
            board.NecrotoxicConversion -= subscriptions.NecrotoxicConversionHandler;
            board.NutrientPatchConsumed -= subscriptions.NutrientPatchConsumedHandler;
            subscriptionsByBoard.Remove(board);
        }
    }
}
