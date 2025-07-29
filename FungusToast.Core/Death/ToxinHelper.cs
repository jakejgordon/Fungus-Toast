using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Players;
using FungusToast.Core.Config;
using System;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Death
{
    public static class ToxinHelper
    {
        /// <summary>
        /// Converts the cell at the specified tile to a toxin, or creates a new toxin cell if empty.
        /// This method respects proper event firing via PlaceFungalCell.
        /// </summary>
        public static void ConvertToToxin(GameBoard board, int tileId, int toxinLifespan, GrowthSource growthSource = GrowthSource.Unknown, Player? owner = null)
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

                // Mark for toxin drop animation
                cell.MarkAsReceivingToxinDrop();
                
                cell.ConvertToToxin(toxinLifespan, owner);
                board.PlaceFungalCell(cell); // fires events!
            }
            else
            {
                var toxin = new FungalCell(owner?.PlayerId, tileId, growthSource, toxinLifespan);
                // Mark for toxin drop animation
                toxin.MarkAsReceivingToxinDrop();
                
                board.PlaceFungalCell(toxin); // fires events!
            }
        }

        /// <summary>
        /// Converts the cell at the specified tile to a toxin, or creates a new toxin cell if empty.
        /// This overload calculates the toxin lifespan automatically based on player mutations.
        /// </summary>
        public static void ConvertToToxin(GameBoard board, int tileId, GrowthSource source, Player? owner = null)
        {
            int toxinLifespan = GetToxinExpirationAge(owner);
            ConvertToToxin(board, tileId, toxinLifespan, source, owner);
        }

        /// <summary>
        /// Calculates the toxin expiration age for a player, including all bonuses, given a custom base duration.
        /// </summary>
        public static int GetToxinExpirationAge(Player? player, int baseToxinDuration)
        {
            if (player == null)
                return baseToxinDuration;
            
            int baseDuration = baseToxinDuration;
            int bonus = player.GetMutationLevel(Mutations.MutationIds.MycotoxinPotentiation) * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel;
            // Enduring Toxaphores synergy
            var myco = player.GetMycovariant != null ? player.GetMycovariant(Mycovariants.MycovariantIds.EnduringToxaphoresId) : null;
            int enduringBonus = myco != null ? MycovariantGameBalance.EnduringToxaphoresNewToxinExtension : 0;
            return baseDuration + bonus + enduringBonus;
        }

        /// <summary>
        /// Calculates the toxin expiration age for a player, including all bonuses. If player is null, uses default duration.
        /// </summary>
        public static int GetToxinExpirationAge(Player? player)
        {
            return GetToxinExpirationAge(player, GameBalance.DefaultToxinDuration);
        }

        /// <summary>
        /// Kills a living cell (if present) and then converts it to toxin.
        /// This method respects proper event firing via PlaceFungalCell.
        /// </summary>
        public static void KillAndToxify(GameBoard board, int tileId, int toxinLifespan, DeathReason reason, Player? owner = null)
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
            cell.ConvertToToxin(toxinLifespan, owner);

            // 4. Place the toxin cell on the board
            board.PlaceFungalCell(cell); // will fire toxin events as needed
        }
    }
}
