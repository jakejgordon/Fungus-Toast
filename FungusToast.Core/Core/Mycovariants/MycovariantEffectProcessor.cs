﻿using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

public static class MycovariantEffectProcessor
{
    public static void CheckAndTrigger(Player player, GameBoard board, ISimulationObserver? observer)
    {
        foreach (var playerMyco in player.PlayerMycovariants)
        {
            if (!playerMyco.HasTriggered &&
                playerMyco.Mycovariant.IsTriggerConditionMet?.Invoke(playerMyco, board) == true)
            {
                playerMyco.Mycovariant.ApplyEffect?.Invoke(playerMyco, board, new Random(), observer);
                playerMyco.MarkTriggered();
            }
        }
    }

    public static void ResolveJettingMycelium(
        PlayerMycovariant playerMyco,
        Player player,
        GameBoard board,
        int tileId,
        CardinalDirection direction,
        ISimulationObserver? observer = null)
    {
        int playerId = playerMyco.PlayerId;

        int livingLength = MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles;
        int toxinCount = MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles;
        int totalLength = livingLength + toxinCount;

        var line = board.GetTileLine(tileId, direction, totalLength, includeStartingTile: false);

        // Outcome tallies
        int infested = 0;
        int reclaimed = 0;
        int catabolicGrowth = 0;
        int alreadyOwned = 0;
        int poisoned = 0; // Replaces "toxified"
        int colonized = 0;
        int invalid = 0;

        List<FungalCell> affectedCells = new();

        // 1. For each tile in the line, up to livingLength, create or replace with a living cell for the player
        for (int i = 0; i < line.Count && i < livingLength; i++)
        {
            var targetTile = board.GetTileById(line[i]);
            if (targetTile == null) { invalid++; continue; }

            var prevCell = targetTile.FungalCell;

            if (prevCell == null)
            {
                // Place a new living cell
                var newCell = new FungalCell(playerId, line[i]);
                targetTile.PlaceFungalCell(newCell);
                board.Players[playerId].ControlledTileIds.Add(line[i]);
                colonized++;
                affectedCells.Add(newCell);
            }
            else
            {
                // Takeover replaces with living cell for the player (allowToxin = true to overwrite toxins too)
                var takeoverResult = prevCell.Takeover(playerId, allowToxin: true);

                switch (takeoverResult)
                {
                    case FungalCellTakeoverResult.Parasitized: infested++; break;
                    case FungalCellTakeoverResult.Reclaimed: reclaimed++; break;
                    case FungalCellTakeoverResult.CatabolicGrowth: catabolicGrowth++; break;
                    case FungalCellTakeoverResult.AlreadyOwned: alreadyOwned++; break;
                    case FungalCellTakeoverResult.InvalidBecauseResistant: invalid++; break;
                    case FungalCellTakeoverResult.Invalid: invalid++; break;
                }
                var updatedCell = targetTile.FungalCell;
                if (updatedCell != null && updatedCell.OwnerPlayerId == playerId && updatedCell.IsAlive)
                {
                    affectedCells.Add(updatedCell);
                }
            }
        }

        // 2. For the last N tiles (toxinCount), convert to toxin
        for (int i = livingLength; i < line.Count && i < totalLength; i++)
        {
            var targetTile = board.GetTileById(line[i]);
            if (targetTile == null) { invalid++; continue; }

            int baseDuration = MycovariantGameBalance.DefaultJettingMyceliumToxinGrowthCycleDuration;
            int bonus = player.GetMutationLevel(MutationIds.MycotoxinPotentiation) * GameBalance.MycotoxinPotentiationGrowthCycleExtensionPerLevel;
            int expirationCycle = board.CurrentGrowthCycle + baseDuration + bonus;

            var prevCell = targetTile.FungalCell;
            if (prevCell == null || prevCell.IsDead)
            {
                // Toxify empty or dead cell (call it "toxified" = "placed toxin")
                ToxinHelper.ConvertToToxin(board, line[i], expirationCycle, player);
                poisoned++; // toxified → poisoned (for simulation output)
            }
            else if (prevCell.IsAlive && prevCell.OwnerPlayerId != playerId)
            {
                // Properly kill and toxify using board-level logic
                ToxinHelper.KillAndToxify(board, line[i], expirationCycle, DeathReason.JettingMycelium, player);
                poisoned++;
            }
            // Do not overwrite your own living cell with toxin.
        }

        // 3. Record effect counts directly on the mycovariant for reporting
        playerMyco.IncrementEffectCount(MycovariantEffectType.Infested, infested);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Reclaimed, reclaimed);
        playerMyco.IncrementEffectCount(MycovariantEffectType.CatabolicGrowth, catabolicGrowth);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Colonized, colonized);
        playerMyco.IncrementEffectCount(MycovariantEffectType.Poisoned, poisoned);


        // 4. Report results to simulation observer, if available
        if (observer != null)
        {
            if (infested > 0) observer.ReportJettingMyceliumInfested(playerId, infested);
            if (reclaimed > 0) observer.ReportJettingMyceliumReclaimed(playerId, reclaimed);
            if (catabolicGrowth > 0) observer.ReportJettingMyceliumCatabolicGrowth(playerId, catabolicGrowth);
            if (alreadyOwned > 0) observer.ReportJettingMyceliumAlreadyOwned(playerId, alreadyOwned);
            if (poisoned > 0) observer.ReportJettingMyceliumPoisoned(playerId, poisoned);
            if (colonized > 0) observer.ReportJettingMyceliumColonized(playerId, colonized);
            if (invalid > 0) observer.ReportJettingMyceliumInvalid(playerId, invalid);
        }

        playerMyco.MarkTriggered();
    }

    /// <summary>
    /// Handles the Neutralizing Mantle effect in response to a toxin being placed.
    /// If the toxin is adjacent to a player's living cells and they have Neutralizing Mantle,
    /// there's a chance to neutralize (remove) the toxin before it's placed.
    /// </summary>
    public static void OnToxinPlaced_NeutralizingMantle(
        ToxinPlacedEventArgs eventArgs,
        GameBoard board,
        List<Player> players,
        Random rng,
        ISimulationObserver? observer = null)
    {
        int toxinTileId = eventArgs.TileId;
        int placingPlayerId = eventArgs.PlacingPlayerId;

        // Check all players for Neutralizing Mantle
        foreach (var player in players)
        {
            // Skip the player who is placing the toxin
            if (player.PlayerId == placingPlayerId)
                continue;

            // Check if this player has Neutralizing Mantle
            var playerMyco = player.GetMycovariant(MycovariantIds.NeutralizingMantleId);
            if (playerMyco == null)
                continue;

            // Check if the toxin tile is adjacent to any of this player's living cells
            var adjacentTiles = board.GetAdjacentTiles(toxinTileId);
            bool isAdjacentToLivingCell = adjacentTiles.Any(tile => 
                tile.FungalCell?.IsAlive == true && 
                tile.FungalCell.OwnerPlayerId == player.PlayerId);

            if (!isAdjacentToLivingCell)
                continue;

            // Check neutralization chance
            float neutralizeChance = MycovariantGameBalance.NeutralizingMantleNeutralizeChance;
            if (rng.NextDouble() < neutralizeChance)
            {
                // Neutralize the toxin
                eventArgs.Neutralized = true;
                
                // Record the effect
                playerMyco.IncrementEffectCount(MycovariantEffectType.Neutralized, 1);
                
                // Report to observer
                observer?.RecordNeutralizingMantleEffect(player.PlayerId, 1);
                
                // Only the first player to neutralize gets the effect
                break;
            }
        }
    }

    public static void ResolveMycelialBastion(
        PlayerMycovariant playerMyco,
        GameBoard board,
        Random rng,
        ISimulationObserver? observer)
    {
        // AI/simulation logic: auto-select up to the allowed number of living cells
        var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
        if (player == null) return;

        var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
            .Where(c => c.IsAlive && !c.IsResistant)
            .ToList();

        int maxCells = Math.Min(MycovariantGameBalance.MycelialBastionMaxResistantCells, livingCells.Count);
        if (maxCells == 0) return;

        // Randomly select up to maxCells
        var selected = livingCells.OrderBy(_ => rng.Next()).Take(maxCells).ToList();
        foreach (var cell in selected)
        {
            cell.MakeResistant();
            playerMyco.IncrementEffectCount(MycovariantEffectType.Bastioned, 1);
            observer?.RecordBastionedCells(player.PlayerId, 1);
        }

        playerMyco.MarkTriggered();
    }
}
