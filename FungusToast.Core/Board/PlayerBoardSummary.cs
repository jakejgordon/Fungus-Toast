namespace FungusToast.Core.Board
{
    /// <summary>
    /// Represents cell count summary for a player.
    /// Contains living, resistant, dead, and toxin cell counts for efficient UI updates.
    /// </summary>
    public class PlayerBoardSummary
    {
        public int PlayerId { get; }
        public int LivingCells { get; }
        public int ResistantCells { get; }
        public int DeadCells { get; }
        public int ToxinCells { get; }

        public PlayerBoardSummary(int playerId, int livingCells, int resistantCells, int deadCells, int toxinCells)
        {
            PlayerId = playerId;
            LivingCells = livingCells;
            ResistantCells = resistantCells;
            DeadCells = deadCells;
            ToxinCells = toxinCells;
        }
    }
}