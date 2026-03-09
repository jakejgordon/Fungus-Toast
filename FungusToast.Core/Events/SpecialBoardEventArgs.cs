using System;

namespace FungusToast.Core.Events
{
    public enum SpecialBoardEventKind
    {
        ConidialRelayTriggered = 1
    }

    public sealed class SpecialBoardEventArgs : EventArgs
    {
        public SpecialBoardEventKind EventKind { get; }
        public int PlayerId { get; }
        public int SourceTileId { get; }
        public int DestinationTileId { get; }

        public SpecialBoardEventArgs(
            SpecialBoardEventKind eventKind,
            int playerId,
            int sourceTileId,
            int destinationTileId)
        {
            EventKind = eventKind;
            PlayerId = playerId;
            SourceTileId = sourceTileId;
            DestinationTileId = destinationTileId;
        }
    }
}