using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Unity.UI;
using System;
using System.Collections.Generic;

namespace FungusToast.Unity.Events
{
    /// <summary>
    /// Responsible for subscribing Unity UI event handlers to the GameBoard.
    /// These handlers update visuals, animations, and presentation.
    /// </summary>
    public static class GameUIEventSubscriber
    {
        private static readonly Dictionary<GameBoard, EventHandler<SpecialBoardEventArgs>> specialEventHandlers = new();

        /// <summary>
        /// Subscribes all UI event handlers to the GameBoard.
        /// Pass in references to required UI managers/components as needed.
        /// </summary>
        public static void Subscribe(GameBoard board, GameUIManager uiManager, SpecialEventPresentationService specialEventPresentationService)
        {
            if (board == null || specialEventPresentationService == null)
            {
                return;
            }

            Unsubscribe(board, uiManager);

            EventHandler<SpecialBoardEventArgs> specialEventHandler = (_, args) =>
            {
                specialEventPresentationService.Enqueue(args);
            };

            board.SpecialBoardEventTriggered += specialEventHandler;
            specialEventHandlers[board] = specialEventHandler;
        }

        /// <summary>
        /// Unsubscribes all UI event handlers from the GameBoard.
        /// </summary>
        public static void Unsubscribe(GameBoard board, GameUIManager uiManager)
        {
            if (board == null)
            {
                return;
            }

            if (specialEventHandlers.TryGetValue(board, out var specialEventHandler))
            {
                board.SpecialBoardEventTriggered -= specialEventHandler;
                specialEventHandlers.Remove(board);
            }
        }
    }
}
