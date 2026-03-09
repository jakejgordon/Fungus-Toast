using System;
using System.Collections;
using System.Collections.Generic;
using FungusToast.Core.Events;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;

namespace FungusToast.Unity
{
    public class SpecialEventPresentationService
    {
        private readonly Func<GameUIManager> getGameUIManager;
        private readonly Func<GridVisualizer> getGridVisualizer;
        private readonly Func<Player> getHumanPlayer;
        private readonly Func<bool> isFastForwarding;
        private readonly Queue<SpecialBoardEventArgs> pendingEvents = new();

        private bool isPresenting;

        public SpecialEventPresentationService(
            Func<GameUIManager> getGameUIManager,
            Func<GridVisualizer> getGridVisualizer,
            Func<Player> getHumanPlayer,
            Func<bool> isFastForwarding)
        {
            this.getGameUIManager = getGameUIManager;
            this.getGridVisualizer = getGridVisualizer;
            this.getHumanPlayer = getHumanPlayer;
            this.isFastForwarding = isFastForwarding;
        }

        public bool HasPendingEvents => pendingEvents.Count > 0;

        public void Reset()
        {
            pendingEvents.Clear();
            isPresenting = false;
        }

        public void Enqueue(SpecialBoardEventArgs e)
        {
            if (e == null || isFastForwarding())
            {
                return;
            }

            var humanPlayer = getHumanPlayer();
            if (humanPlayer == null || e.PlayerId != humanPlayer.PlayerId)
            {
                return;
            }

            pendingEvents.Enqueue(e);
        }

        public IEnumerator PresentPendingAfterDecayRender()
        {
            if (isPresenting || pendingEvents.Count == 0 || isFastForwarding())
            {
                yield break;
            }

            isPresenting = true;
            try
            {
                while (pendingEvents.Count > 0)
                {
                    var specialEvent = pendingEvents.Dequeue();
                    yield return PresentSpecialEvent(specialEvent);
                }
            }
            finally
            {
                isPresenting = false;
            }
        }

        private IEnumerator PresentSpecialEvent(SpecialBoardEventArgs specialEvent)
        {
            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            switch (specialEvent.EventKind)
            {
                case SpecialBoardEventKind.ConidialRelayTriggered:
                    uiManager.PhaseBanner?.Show(
                        "Conidial Relay triggered!",
                        UIEffectConstants.ConidialRelayBannerHoldSeconds);
                    yield return gridVisualizer.PlayConidialRelayAnimation(
                        specialEvent.PlayerId,
                        specialEvent.SourceTileId,
                        specialEvent.DestinationTileId);
                    break;
            }
        }
    }
}