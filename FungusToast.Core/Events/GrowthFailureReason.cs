using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    public enum GrowthFailureReason
    {
        None,                // Growth succeeded (for success events)
        TileOccupied,        // Target tile already has a living/dead cell
        ToxinPresent,        // Target tile is toxic
        NotAdjacent,         // Source and target are not adjacent
        OutOfBounds,         // Target tile is off the board
        BlockedByMutation,   // Prevented by enemy mutation/mycovariant
        InvalidTarget,       // Some other game logic reason
        Unknown,
        OccupiedByResistantCell
    }

}
