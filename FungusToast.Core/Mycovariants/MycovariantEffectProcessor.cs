using FungusToast.Core.Board;
using FungusToast.Core.Config;
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
using FungusToast.Core.Growth;

public static class MycovariantEffectProcessor
{
    public static void ResolveJettingMycelium(
        PlayerMycovariant playerMyco,
        Player player,
        GameBoard board,
        int tileId,
        CardinalDirection direction,
        Random rng,
        ISimulationObserver? observer = null)
    {
        int playerId = playerMyco.PlayerId;

        int livingLength = MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles;

        // Debug logging to help verify direction fix
        var (sourceX, sourceY) = board.GetXYFromTileId(tileId);
        FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[JettingMycelium] Resolving {direction} cone from tile ({sourceX}, {sourceY}) for player {playerId}");

        // Get the straight line for living cells (first 4 tiles)
        var livingLine = board.GetTileLine(tileId, direction, livingLength, includeStartingTile: false);
        
        // Get the cone pattern for toxins
        var toxinCone = board.GetTileCone(tileId, direction);

        // Debug logging to show the effect pattern
        FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[JettingMycelium] Living line: {livingLine.Count} tiles, Toxin cone: {toxinCone.Count} tiles");

        // Outcome tallies
        int infested = 0;
        int reclaimed = 0;
        int catabolicGrowth = 0;
        int alreadyOwned = 0;
        int poisoned = 0; // Replaces "toxified"
        int colonized = 0;
        int invalid = 0;

        List<FungalCell> affectedCells = new();

        // 1. For each tile in the living line, create or replace with a living cell for the player
        for (int i = 0; i < livingLine.Count && i < livingLength; i++)
        {
            var targetTile = board.GetTileById(livingLine[i]);
            if (targetTile == null) { invalid++; continue; }

            var prevCell = targetTile.FungalCell;

            if (prevCell == null)
            {
                // Place a new living cell
                var newCell = new FungalCell(playerId, livingLine[i], FungusToast.Core.Growth.GrowthSource.JettingMycelium);
                targetTile.PlaceFungalCell(newCell);
                board.Players[playerId].ControlledTileIds.Add(livingLine[i]);
                colonized++;
                affectedCells.Add(newCell);
            }
            else
            {
                // Use board.TakeoverCell to handle both cell state and board updates.
                var takeoverResult = board.TakeoverCell(livingLine[i], playerId, allowToxin: true, GrowthSource.JettingMycelium, players: board.Players, rng: rng, observer: observer);

                switch (takeoverResult)
                {
                    case FungalCellTakeoverResult.Infested: infested++; break;
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

        // 2. For each tile in the toxin cone, convert to toxin
        foreach (int coneTileId in toxinCone)
        {
            var targetTile = board.GetTileById(coneTileId);
            if (targetTile == null) { invalid++; continue; }

            // Use ToxinHelper to get proper expiration age with all bonuses
            int toxinLifespan = ToxinHelper.GetToxinExpirationAge(
                player, 
                MycovariantGameBalance.DefaultJettingMyceliumToxinGrowthCycleDuration);

            var prevCell = targetTile.FungalCell;
            if (prevCell == null || prevCell.IsDead)
            {
                // Toxify empty or dead cell (call it "toxified" = "placed toxin")
                ToxinHelper.ConvertToToxin(board, coneTileId, toxinLifespan, GrowthSource.JettingMycelium, player);
                poisoned++; // toxified → poisoned (for simulation output)
            }
            else if (prevCell.IsAlive && prevCell.OwnerPlayerId != playerId)
            {
                // Properly kill and toxify using board-level logic
                ToxinHelper.KillAndToxify(board, coneTileId, toxinLifespan, DeathReason.JettingMycelium, player);
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

            // Check if the toxin tile is orthogonally adjacent to any of this player's living cells
            var adjacentTiles = board.GetOrthogonalNeighbors(toxinTileId);
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

        // Determine max cells based on which Mycelial Bastion tier this is
        int maxCellsAllowed = playerMyco.MycovariantId switch
        {
            MycovariantIds.MycelialBastionIId => MycovariantGameBalance.MycelialBastionIMaxResistantCells,
            MycovariantIds.MycelialBastionIIId => MycovariantGameBalance.MycelialBastionIIMaxResistantCells,
            MycovariantIds.MycelialBastionIIIId => MycovariantGameBalance.MycelialBastionIIIMaxResistantCells,
            _ => MycovariantGameBalance.MycelialBastionIMaxResistantCells // fallback
        };

        int maxCells = Math.Min(maxCellsAllowed, livingCells.Count);
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

    // For AI: auto-selects a target as before
    public static void ResolveSurgicalInoculationAI(
        PlayerMycovariant playerMyco,
        GameBoard board,
        Random rng,
        ISimulationObserver? observer)
    {
        var player = board.Players.FirstOrDefault(
            p => p.PlayerId == playerMyco.PlayerId);
        if (player == null)
            return;

        // 1. Gather all enemy living cells
        var enemyLivingCells = board.GetAllCells()
            .Where(cell => cell != null
                && cell.IsAlive
                && cell.OwnerPlayerId != player.PlayerId
                && !cell.IsResistant)
            .ToList();

        // 2. Score each by number of orthogonally adjacent open spaces
        int BestScore(FungalCell cell)
        {
            var adj = board.GetOrthogonalNeighbors(cell.TileId);
            return adj.Count(
                t => t.FungalCell == null || t.FungalCell.IsDead);
        }

        var bestEnemyCell = enemyLivingCells
            .Select(cell => new {
                Cell = cell,
                Score = BestScore(cell)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(_ => rng.Next()) // randomize ties
            .FirstOrDefault();

        int? targetTileId = null;
        if (bestEnemyCell != null)
        {
            targetTileId = bestEnemyCell.Cell.TileId;
        }
        else
        {
            // 3. If no enemy living cells, pick any open tile (not already Resistant)
            var openTiles = board.AllTiles()
                .Where(tile => (tile.FungalCell == null || tile.FungalCell.IsDead)
                    && (tile.FungalCell == null || !tile.FungalCell.IsResistant))
                .ToList();
            if (openTiles.Count > 0)
            {
                targetTileId = openTiles[rng.Next(openTiles.Count)].TileId;
            }
        }

        if (targetTileId.HasValue)
        {
            ApplySurgicalInoculationToTile(playerMyco, board, player.PlayerId, targetTileId.Value, observer);
        }
        playerMyco.MarkTriggered();
    }

    // For human: apply to a specific tile
    public static void ResolveSurgicalInoculationHuman(
        PlayerMycovariant playerMyco,
        GameBoard board,
        int playerId,
        int tileId,
        ISimulationObserver? observer)
    {
        ApplySurgicalInoculationToTile(playerMyco, board, playerId, tileId, observer);
        playerMyco.MarkTriggered();
    }

    // Shared logic for applying the effect to a tile
    private static void ApplySurgicalInoculationToTile(
        PlayerMycovariant playerMyco,
        GameBoard board,
        int playerId,
        int tileId,
        ISimulationObserver? observer)
    {
        var targetTile = board.GetTileById(tileId);
        if (targetTile != null)
        {
            var prevCell = targetTile.FungalCell;
            if (prevCell == null)
            {
                // Place new Resistant cell using board method
                var newCell = new FungalCell(playerId, tileId, GrowthSource.SurgicalInoculation);
                newCell.MakeResistant();
                board.PlaceFungalCell(newCell);
                observer?.RecordSurgicalInoculationDrop(playerId, 1);
                playerMyco.IncrementEffectCount(
                    MycovariantEffectType.Drops,
                    1);
            }
            else if (!prevCell.IsResistant)
            {
                // Take over (alive, dead, or toxin) and make Resistant
                prevCell.Takeover(playerId, allowToxin: true);
                prevCell.MakeResistant();
                board.PlaceFungalCell(prevCell);
                observer?.RecordSurgicalInoculationDrop(playerId, 1);
                playerMyco.IncrementEffectCount(
                    MycovariantEffectType.Drops,
                    1);
            }
            playerMyco.IncrementEffectCount(
                MycovariantEffectType.ResistantCellPlaced,
                1);
        }
    }

    /// <summary>
    /// Handles the Hyphal Resistance Transfer effect when a Resistant cell is placed.
    /// Checks if the player has this mycovariant and applies the chance-based transfer to adjacent cells.
    /// </summary>
    public static void OnResistantCellPlaced(
        GameBoard board,
        int playerId,
        int tileId,
        ISimulationObserver? observer = null)
    {
        var player = board.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        // Check if this player has Hyphal Resistance Transfer
        var playerMyco = player.GetMycovariant(MycovariantIds.HyphalResistanceTransferId);
        if (playerMyco == null) return;

        var rng = new Random(); // Create a new RNG instance for this effect
        var adjacentTiles = board.GetAdjacentTiles(tileId);
        int transferredCount = 0;

        foreach (var adjacentTile in adjacentTiles)
        {
            var adjacentCell = adjacentTile.FungalCell;
            
            // Check if adjacent cell is owned by the same player, is alive, and not already resistant
            if (adjacentCell != null && 
                adjacentCell.OwnerPlayerId == playerId && 
                adjacentCell.IsAlive && 
                !adjacentCell.IsResistant)
            {
                // Apply chance-based transfer
                if (rng.NextDouble() < MycovariantGameBalance.HyphalResistanceTransferChance)
                {
                    adjacentCell.MakeResistant();
                    transferredCount++;
                }
            }
        }

        // Record the effect if any transfers occurred
        if (transferredCount > 0)
        {
            playerMyco.IncrementEffectCount(MycovariantEffectType.ResistantTransfers, transferredCount);
            observer?.RecordHyphalResistanceTransfer(playerId, transferredCount);
        }
    }

    /// <summary>
    /// Applies Hyphal Resistance Transfer after the growth phase for all players with the mycovariant.
    /// For each living, non-resistant cell adjacent to a resistant cell, applies the transfer chance.
    /// </summary>
    public static void OnPostGrowthPhase_HyphalResistanceTransfer(
        GameBoard board,
        List<Player> players,
        Random rng,
        ISimulationObserver? observer = null)
    {
        foreach (var player in players)
        {
            var playerMyco = player.GetMycovariant(MycovariantIds.HyphalResistanceTransferId);
            if (playerMyco == null) continue;
            // Get all of this player's living resistant cells (typically very few)
            var resistantCells = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive && c.IsResistant)
                .ToList();

            if (resistantCells.Count == 0) continue; // No resistant cells to transfer from

            int transferredCount = 0;
            var processedCells = new HashSet<int>(); // Track cells we've already processed

            // For each resistant cell, check its adjacent tiles for transfer candidates
            foreach (var resistantCell in resistantCells)
            {
                var adjacentTiles = board.GetAdjacentTiles(resistantCell.TileId);
                foreach (var adjacentTile in adjacentTiles)
                {
                    var adjacentCell = adjacentTile.FungalCell;
                    if (adjacentCell == null || 
                        adjacentCell.OwnerPlayerId != player.PlayerId ||
                        !adjacentCell.IsAlive ||
                        adjacentCell.IsResistant ||
                        processedCells.Contains(adjacentCell.TileId))
                        continue;

                    // Mark as processed to avoid double-processing if multiple resistant cells are adjacent
                    processedCells.Add(adjacentCell.TileId);

                    if (rng.NextDouble() < MycovariantGameBalance.HyphalResistanceTransferChance)
                    {
                        adjacentCell.MakeResistant();
                        transferredCount++;
                    }
                }
            }
            if (transferredCount > 0)
            {
                playerMyco.IncrementEffectCount(MycovariantEffectType.ResistantTransfers, transferredCount);
                observer?.RecordHyphalResistanceTransfer(player.PlayerId, transferredCount);
            }
        }
    }

    public static void OnToxinPlaced_EnduringToxaphores(
        ToxinPlacedEventArgs eventArgs,
        GameBoard board,
        List<Player> players,
        ISimulationObserver? observer = null)
    {
        int playerId = eventArgs.PlacingPlayerId;
        if (playerId < 0) return;
        var player = players.First(p => p.PlayerId == playerId);
        if (player == null) return;
        var playerMyco = player.GetMycovariant(MycovariantIds.EnduringToxaphoresId);
        if (playerMyco == null) return;

        // Find the toxin cell just placed
        var tile = board.GetTileById(eventArgs.TileId);
        var cell = tile?.FungalCell;
        if (cell == null || !cell.IsToxin) return;

        int extension = MycovariantGameBalance.EnduringToxaphoresNewToxinExtension;
        cell.ToxinExpirationAge += extension;
        observer?.RecordEnduringToxaphoresExtendedCycles(playerId, extension);
    }

    public static void OnAcquisition_EnduringToxaphores(
        Player player,
        GameBoard board,
        ISimulationObserver? observer = null)
    {
        var playerMyco = player.GetMycovariant(MycovariantIds.EnduringToxaphoresId);
        if (playerMyco == null) return;

        // Extend all existing toxins by Y cycles at acquisition
        int extension = MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension;
        int totalExtended = 0;

        foreach (var cell in board.AllToxinFungalCells())
        {
            if (cell.OwnerPlayerId == player.PlayerId)
            {
                cell.ToxinExpirationAge += extension;
                totalExtended += extension;
            }
        }

        if (totalExtended > 0)
        {
            observer?.RecordEnduringToxaphoresExistingExtensions(player.PlayerId, totalExtended);
        }
    }

    /// <summary>
    /// Handles the Necrophoric Adaptation effect when a cell dies.
    /// If the player has Necrophoric Adaptation, there's a chance to reclaim an adjacent dead tile.
    /// This method also considers ReclamationRhizomorphs for second attempts.
    /// </summary>
    public static void OnCellDeath_NecrophoricAdaptation(
        GameBoard board,
        int playerId,
        int diedTileId,
        List<Player> players,
        Random rng,
        ISimulationObserver? observer = null)
    {
        var player = players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        // Check if this player has Necrophoric Adaptation
        var playerMyco = player.GetMycovariant(MycovariantIds.NecrophoricAdaptation);
        if (playerMyco == null) return;

        // Get orthogonally adjacent tiles that contain dead cells (not empty tiles)
        var adjacentTiles = board.GetOrthogonalNeighbors(diedTileId);
        var deadAdjacentTiles = adjacentTiles
            .Where(tile => tile.FungalCell != null && tile.FungalCell.IsDead)
            .ToList();

        if (deadAdjacentTiles.Count == 0) return;

        // Try the reclamation attempt
        bool success = TryNecrophoricReclamation(board, playerId, deadAdjacentTiles, rng, playerMyco, observer);
        
        // If failed and player has Reclamation Rhizomorphs, try again
        if (!success)
        {
            var rhizomorphsMyco = player.GetMycovariant(MycovariantIds.ReclamationRhizomorphsId);
            if (rhizomorphsMyco != null && 
                rng.NextDouble() < MycovariantGameBalance.ReclamationRhizomorphsSecondAttemptChance)
            {
                TryNecrophoricReclamation(board, playerId, deadAdjacentTiles, rng, playerMyco, observer);
                
                // Record the second attempt from Reclamation Rhizomorphs
                rhizomorphsMyco.IncrementEffectCount(MycovariantEffectType.SecondReclamationAttempts, 1);
                observer?.RecordReclamationRhizomorphsSecondAttempt(playerId, 1);
            }
        }
    }

    /// <summary>
    /// Attempts a single necrophoric reclamation attempt.
    /// </summary>
    private static bool TryNecrophoricReclamation(
        GameBoard board,
        int playerId,
        List<BoardTile> deadAdjacentTiles,
        Random rng,
        PlayerMycovariant playerMyco,
        ISimulationObserver? observer)
    {
        if (rng.NextDouble() < MycovariantGameBalance.NecrophoricAdaptationReclamationChance)
        {
            // Success! Pick a random dead adjacent tile to reclaim
            var targetTile = deadAdjacentTiles[rng.Next(deadAdjacentTiles.Count)];
            
            // Use board.TakeoverCell instead of direct PlaceFungalCell to handle all board state properly
            var result = board.TakeoverCell(
                targetTile.TileId, 
                playerId, 
                allowToxin: false, // Only reclaim dead cells, not toxins
                GrowthSource.NecrohyphalInfiltration, // Use appropriate growth source for necrophoric reclamation
                board.Players, 
                rng, 
                observer);
            
            // Check if the takeover was successful
            if (result == FungalCellTakeoverResult.Reclaimed)
            {
                // Record the effect
                playerMyco.IncrementEffectCount(MycovariantEffectType.NecrophoricAdaptationReclamations, 1);
                observer?.RecordNecrophoricAdaptationReclamation(playerId, 1);
                return true;
            }
        }
        
        return false;
    }
}
