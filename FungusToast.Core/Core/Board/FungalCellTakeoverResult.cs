using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Core.Board
{
    public enum FungalCellTakeoverResult
    {
        None,
        Parasitized,  // Took from a living enemy cell
        Reclaimed,    // Took a dead tile
        AlreadyOwned, // Already owned by this player
        KilledOwn,
        CatabolicGrowth, // converted a toxin to a living cell
        Invalid,
    }

}
