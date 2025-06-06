using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Death
{
    public static class ToxinHelper
    {
        public static void ConvertToToxin(GameBoard board, int tileId, int expirationCycle, Player? owner = null)
        {
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell != null)
            {
                if (cell.IsAlive)
                    throw new InvalidOperationException("Cannot convert a living cell to toxin. Kill it first.");

                cell.ConvertToToxin(expirationCycle, owner);
                board.PlaceFungalCell(cell);
            }
            else
            {
                var toxin = new FungalCell(owner?.PlayerId, tileId, expirationCycle);
                board.PlaceFungalCell(toxin);
            }
        }

        public static void KillAndToxify(GameBoard board, int tileId, int expirationCycle, DeathReason reason, Player? owner = null)
        {
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell == null || !cell.IsAlive)
                return;

            cell.Kill(reason);
            cell.ConvertToToxin(expirationCycle, owner);
            board.RemoveControlFromPlayer(tileId);
            board.PlaceFungalCell(cell);
        }

    }
}
