using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FungusToast.Core.Events
{
    public enum SpecialBoardEventKind
    {
        ConidialRelayTriggered = 1,
        MycotoxicLashTriggered = 2,
        RetrogradeBloomTriggered = 3,
        SaprophageRingTriggered = 4,
        MarginalClampTriggered = 5
    }

    public sealed class SpecialBoardEventArgs : EventArgs
    {
        public SpecialBoardEventKind EventKind { get; }
        public int PlayerId { get; }
        public int SourceTileId { get; }
        public int DestinationTileId { get; }
        public IReadOnlyList<int> AffectedTileIds { get; }

        public SpecialBoardEventArgs(
            SpecialBoardEventKind eventKind,
            int playerId,
            int sourceTileId,
            int destinationTileId,
            IEnumerable<int>? affectedTileIds = null)
        {
            EventKind = eventKind;
            PlayerId = playerId;
            SourceTileId = sourceTileId;
            DestinationTileId = destinationTileId;
            AffectedTileIds = new ReadOnlyCollection<int>((affectedTileIds ?? Enumerable.Empty<int>()).ToList());
        }
    }
}