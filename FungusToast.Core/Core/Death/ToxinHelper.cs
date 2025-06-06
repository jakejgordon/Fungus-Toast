using FungusToast.Core.Board;
using System;

namespace FungusToast.Core.Death
{
    public static class ToxinHelper
    {
        public static void ConvertToToxin(GameBoard board, int tileId, int expirationCycle, int? ownerPlayerId = null)
        {
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell != null)
            {
                if (cell.IsAlive)
                    throw new InvalidOperationException("Cannot convert a living cell to toxin. Kill it first.");

                cell.ConvertToToxin(expirationCycle, ownerPlayerId);
                board.PlaceFungalCell(cell); // Ensure updated state is saved
            }
            else
            {
                var toxin = new FungalCell(ownerPlayerId, tileId, expirationCycle); // uses new constructor
                board.PlaceFungalCell(toxin);
            }
        }


        /// <summary>
        /// Cleanly kills and toxifies a living cell in one step.
        /// </summary>
        public static void KillAndToxify(GameBoard board, int tileId, int expirationCycle, DeathReason reason, int? ownerPlayerId = null)
        {
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell == null || !cell.IsAlive)
                return;

            cell.Kill(reason);
            cell.ConvertToToxin(expirationCycle, ownerPlayerId);
            board.RemoveControlFromPlayer(tileId);
            board.PlaceFungalCell(cell); // Save updated toxin state
        }
    }
}
