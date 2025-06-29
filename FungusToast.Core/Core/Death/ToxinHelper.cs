﻿using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Players;
using System;

namespace FungusToast.Core.Death
{
    public static class ToxinHelper
    {
        /// <summary>
        /// Converts the cell at the specified tile to a toxin, or creates a new toxin cell if empty.
        /// This method respects proper event firing via PlaceFungalCell.
        /// </summary>
        public static void ConvertToToxin(GameBoard board, int tileId, int expirationCycle, Player? owner = null)
        {
            // Fire ToxinPlaced event to allow for neutralization
            var toxinPlacedArgs = new ToxinPlacedEventArgs(tileId, owner?.PlayerId ?? -1);
            board.OnToxinPlaced(toxinPlacedArgs);
            
            // If neutralized, don't place the toxin
            if (toxinPlacedArgs.Neutralized)
                return;

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell != null)
            {
                if (cell.IsAlive)
                    throw new InvalidOperationException("Cannot convert a living cell to toxin. Kill it first.");

                cell.ConvertToToxin(expirationCycle, owner);
                board.PlaceFungalCell(cell); // fires events!
            }
            else
            {
                var toxin = new FungalCell(owner?.PlayerId, tileId, expirationCycle);
                board.PlaceFungalCell(toxin); // fires events!
            }
        }

        /// <summary>
        /// Kills a living cell (if present) and then converts it to toxin.
        /// This method respects proper event firing via PlaceFungalCell.
        /// </summary>
        public static void KillAndToxify(GameBoard board, int tileId, int expirationCycle, DeathReason reason, Player? owner = null)
        {
            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;

            if (cell == null || !cell.IsAlive)
                return;

            // 1. Kill the cell via board, so OnCellDeath fires!
            board.KillFungalCell(cell, reason, owner?.PlayerId);

            // 2. Fire ToxinPlaced event to allow for neutralization
            var toxinPlacedArgs = new ToxinPlacedEventArgs(tileId, owner?.PlayerId ?? -1);
            board.OnToxinPlaced(toxinPlacedArgs);
            
            // If neutralized, don't place the toxin
            if (toxinPlacedArgs.Neutralized)
                return;

            // 3. Now convert the cell to toxin (state change)
            cell.ConvertToToxin(expirationCycle, owner);

            // 4. Place the toxin cell on the board
            board.PlaceFungalCell(cell); // will fire toxin events as needed
        }
    }
}
