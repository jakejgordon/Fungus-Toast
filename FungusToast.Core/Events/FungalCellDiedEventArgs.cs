using FungusToast.Core.Board;
using FungusToast.Core.Death;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    public class FungalCellDiedEventArgs : EventArgs
    {
        public int TileId { get; }
        public int OwnerPlayerId { get; }
        public DeathReason Reason { get; }
        public int? KillerPlayerId { get; }
        public FungalCell Cell { get; }
        public int? AttackerTileId { get; }

        // Original constructor for backward compatibility
        public FungalCellDiedEventArgs(int tileId, int ownerPlayerId, DeathReason reason, int? killerPlayerId, FungalCell cell)
            : this(tileId, ownerPlayerId, reason, killerPlayerId, cell, null)
        {
        }

        // New constructor with AttackerTileId
        public FungalCellDiedEventArgs(int tileId, int ownerPlayerId, DeathReason reason, int? killerPlayerId, FungalCell cell, int? attackerTileId)
        {
            TileId = tileId;
            OwnerPlayerId = ownerPlayerId;
            Reason = reason;
            KillerPlayerId = killerPlayerId;
            Cell = cell;
            AttackerTileId = attackerTileId;
        }
    }

}
