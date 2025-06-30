using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    public class ToxinExpiredEventArgs : EventArgs
    {
        public int TileId { get; }
        public int? ToxinOwnerPlayerId { get; }

        public ToxinExpiredEventArgs(int tileId, int? toxinOwnerPlayerId)
        {
            TileId = tileId;
            ToxinOwnerPlayerId = toxinOwnerPlayerId;
        }
    }
}
