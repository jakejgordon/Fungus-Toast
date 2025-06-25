using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Core.Events
{
    public class ToxinPlacedEventArgs : EventArgs
    {
        public int TileId { get; }
        public int PlacingPlayerId { get; }
        public bool Neutralized { get; set; }

        public ToxinPlacedEventArgs(int tileId, int placingPlayerId)
        {
            TileId = tileId;
            PlacingPlayerId = placingPlayerId;
            Neutralized = false;
        }
    }
}
