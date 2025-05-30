using FungusToast.Core.Board;
using FungusToast.Core.Death;
using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Core.Death
{
    public static class ToxinHelper
    {
        public static void ConvertToToxin(
            GameBoard board,
            int tileId,
            int expirationCycle,
            DeathReason? reason = null)
        {
            var cell = board.GetCell(tileId);

            if (cell != null)
            {
                if (cell.IsAlive)
                {
                    cell.Kill(reason ?? DeathReason.Unknown);
                    board.RemoveControlFromPlayer(tileId);
                }

                cell.MarkAsToxin(expirationCycle);
            }
            else
            {
                var toxin = new FungalCell(-1, tileId);
                toxin.MarkAsToxin(expirationCycle);
                board.PlaceCell(tileId, toxin);
            }

            var tile = board.GetTileById(tileId);
            tile?.PlaceToxin(cell?.OwnerPlayerId ?? -1, expirationCycle);
        }
    }

}
