using System;

namespace FungusToast.Core.Events
{
    public class CatabolicRebirthEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public int RebornCellCount { get; }
        public int X { get; }
        public int Y { get; }

        public CatabolicRebirthEventArgs(int playerId, int rebornCellCount, int x, int y)
        {
            PlayerId = playerId;
            RebornCellCount = rebornCellCount;
            X = x;
            Y = y;
        }
    }
} 