using FungusToast.Core.Board;
using FungusToast.Core.Death;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Death
{
    public static class ToxinHelper
    {
        public static void ConvertToToxin(GameBoard board, int tileId, int expirationCycle)
        {
            var cell = board.GetCell(tileId);

            if (cell != null)
            {
                if (cell.IsAlive)
                    throw new InvalidOperationException("Cannot convert a living cell to toxin. Kill it first.");

                cell.MarkAsToxin(expirationCycle);
            }
            else
            {
                var toxin = new FungalCell(-1, tileId);
                toxin.MarkAsToxin(expirationCycle);
                board.RegisterCell(toxin);
            }

            var tile = board.GetTileById(tileId);
            tile?.PlaceToxin(cell?.OwnerPlayerId ?? -1, expirationCycle);
        }


        /// <summary>
        /// Cleanly kills and toxifies a living cell in one step.
        /// </summary>
        public static void KillAndToxify(GameBoard board, int tileId, int expirationCycle, DeathReason reason)
        {
            var cell = board.GetCell(tileId);

            if (cell == null || !cell.IsAlive)
                return;

            cell.Kill(reason);
            board.RemoveControlFromPlayer(tileId);
            cell.MarkAsToxin(expirationCycle);

            var tile = board.GetTileById(tileId);
            tile?.PlaceToxin(cell.OwnerPlayerId, expirationCycle);
        }
    }

}
