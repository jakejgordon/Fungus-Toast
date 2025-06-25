using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Events
{
    /// <summary>
    /// Event arguments for a fungal growth attempt.
    /// </summary>
    public class GrowthAttemptEventArgs : EventArgs
    {
        /// <summary>Player attempting to grow.</summary>
        public int PlayerId { get; }
        /// <summary>Source/parent tile for growth.</summary>
        public int SourceTileId { get; }
        /// <summary>Target tile for growth.</summary>
        public int TargetTileId { get; }
        /// <summary>
        /// Set to true by event listeners to prevent the growth attempt (used in pre-event).
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// If set, indicates why the growth failed (used in post-event).
        /// </summary>
        public GrowthFailureReason FailureReason { get; set; }

        public GrowthAttemptEventArgs(int playerId, int sourceTileId, int targetTileId)
        {
            PlayerId = playerId;
            SourceTileId = sourceTileId;
            TargetTileId = targetTileId;
            Cancel = false;
            FailureReason = GrowthFailureReason.None;
        }
    }


}
