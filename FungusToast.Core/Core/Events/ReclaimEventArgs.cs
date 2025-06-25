using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    public class ReclaimEventArgs : EventArgs
    {
        public int TileId { get; set; }
        public int PlayerId { get; set; }
        public bool WasSuccessful { get; set; }
    }

}
