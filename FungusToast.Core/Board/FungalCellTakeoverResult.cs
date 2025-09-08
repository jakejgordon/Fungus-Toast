using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Board
{
    public enum FungalCellTakeoverResult
    {
        AlreadyOwned,
        Infested,       // Used to be Parasitized
        Reclaimed,
        Overgrown,      // Replaced a toxin with a living cell (preferred term)
        Invalid,
        InvalidBecauseResistant
    }
}
