using FungusToast.Core.Board;
using FungusToast.Core.Death;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Core.Events
{
    public class FungalCellDiedEventArgs : EventArgs
    {
        public int TileId { get; }
        public int OwnerPlayerId { get; }
        public DeathReason Reason { get; }
        public int? KillerPlayerId { get; }
        public FungalCell Cell { get; }

        public FungalCellDiedEventArgs(int tileId, int ownerPlayerId, DeathReason reason, int? killerPlayerId, FungalCell cell)
        {
            TileId = tileId;
            OwnerPlayerId = ownerPlayerId;
            Reason = reason;
            KillerPlayerId = killerPlayerId;
            Cell = cell;
        }
    }

}
