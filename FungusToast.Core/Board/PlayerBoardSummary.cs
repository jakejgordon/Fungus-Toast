namespace FungusToast.Core.Board
{
    /// <summary>
    /// Represents cell count summary for a player.
    /// Contains living, dead, and toxin cell counts for efficient UI updates.
    /// </summary>
    public class PlayerBoardSummary
    {
        public int PlayerId { get; }
        public int LivingCells { get; }
        public int DeadCells { get; }
        public int ToxinCells { get; }

        public PlayerBoardSummary(int playerId, int livingCells, int deadCells, int toxinCells)
        {
            PlayerId = playerId;
            LivingCells = livingCells;
            DeadCells = deadCells;
            ToxinCells = toxinCells;
        }
    }
}