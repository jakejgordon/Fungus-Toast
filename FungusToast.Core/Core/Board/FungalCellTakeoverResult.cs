using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Core.Board
{
    public enum FungalCellTakeoverResult
    {
        AlreadyOwned,
        Infested,       // Used to be Parasitized
        Reclaimed,
        CatabolicGrowth,
        Invalid,
        InvalidBecauseResistant,
        Parasitized
    }
}
