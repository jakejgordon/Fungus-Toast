using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Handles all mutation effects related to the MycelialSurges category.
    /// </summary>
    public static class MycelialSurgeMutationProcessor
    {
        /// <summary>
        /// Processes Hyphal Vectoring surge effect for all eligible players.
        /// </summary>
        public static void ProcessHyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.HyphalVectoring);
                if (level <= 0 || !player.IsSurgeActive(MutationIds.HyphalVectoring))
                    continue;

                int centerX = board.Width / 2;
                int centerY = board.Height / 2;
                int totalTiles = GameBalance.HyphalVectoringBaseTiles +
                                 level * GameBalance.HyphalVectoringTilesPerLevel;

                var origin = HyphalVectoringHelper.TrySelectHyphalVectorOrigin(player, board, rng, centerX, centerY, totalTiles);

                if (origin == null)
                {
                    WriteLine($"[HyphalVectoring] Player {player.PlayerId}: no valid origin found.");
                    continue;
                }

                // Outcome tallies
                int infested = 0;
                int reclaimed = 0;
                int catabolicGrowth = 0;
                int alreadyOwned = 0;
                int colonized = 0;
                int invalid = 0;

                int placed = 0;
                int currentTileId = origin.Value.tile.TileId;
                int dx = Math.Sign(centerX - origin.Value.tile.X);
                int dy = Math.Sign(centerY - origin.Value.tile.Y);

                for (int i = 0; i < totalTiles; i++)
                {
                    var (x, y) = board.GetXYFromTileId(currentTileId);
                    // Step towards the center
                    x += dx;
                    y += dy;
                    if (x < 0 || y < 0 || x >= board.Width || y >= board.Height)
                        break;
                    int targetTileId = y * board.Width + x;
                    var targetTile = board.GetTileById(targetTileId);
                    if (targetTile == null) { invalid++; continue; }

                    var prevCell = targetTile.FungalCell;
                    if (prevCell != null && prevCell.IsAlive && prevCell.OwnerPlayerId == player.PlayerId)
                    {
                        // Skip over friendly living mold
                        alreadyOwned++;
                        currentTileId = targetTileId;
                        continue;
                    }

                    FungalCellTakeoverResult takeoverResult;
                    if (prevCell != null)
                    {
                        // Use board.TakeoverCell to handle both cell state and board updates.
                        takeoverResult = board.TakeoverCell(targetTileId, player.PlayerId, allowToxin: true, GrowthSource.HyphalVectoring, players: board.Players, rng: rng, observer: observer);
                        switch (takeoverResult)
                        {
                            case FungalCellTakeoverResult.Infested: infested++; break;
                            case FungalCellTakeoverResult.Reclaimed: reclaimed++; break;
                            case FungalCellTakeoverResult.CatabolicGrowth: catabolicGrowth++; break;
                            case FungalCellTakeoverResult.AlreadyOwned: alreadyOwned++; break;
                            case FungalCellTakeoverResult.Invalid: invalid++; break;
                        }
                    }
                    else
                    {
                        // Place a new living cell if empty
                        var newCell = new FungalCell(player.PlayerId, targetTileId, GrowthSource.HyphalVectoring);
                        board.PlaceFungalCell(newCell); // Use board.PlaceFungalCell instead of targetTile.PlaceFungalCell for proper tracking
                        colonized++;
                    }

                    placed++;
                    currentTileId = targetTileId;
                }

                // Report results to simulation observer, if available
                if (observer != null)
                {
                    if (infested > 0) observer.ReportHyphalVectoringInfested(player.PlayerId, infested);
                    if (reclaimed > 0) observer.ReportHyphalVectoringReclaimed(player.PlayerId, reclaimed);
                    if (catabolicGrowth > 0) observer.ReportHyphalVectoringCatabolicGrowth(player.PlayerId, catabolicGrowth);
                    if (alreadyOwned > 0) observer.ReportHyphalVectoringAlreadyOwned(player.PlayerId, alreadyOwned);
                    if (colonized > 0) observer.ReportHyphalVectoringColonized(player.PlayerId, colonized);
                    if (invalid > 0) observer.ReportHyphalVectoringInvalid(player.PlayerId, invalid);
                }

                if (placed > 0)
                    observer?.RecordHyphalVectoringGrowth(player.PlayerId, placed);
            }
        }

        /// <summary>
        /// Handles Chitin Fortification effect at the start of Growth Phase.
        /// </summary>
        public static void OnPreGrowthPhase_ChitinFortification(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.ChitinFortification);
                if (level <= 0 || !player.IsSurgeActive(MutationIds.ChitinFortification))
                    continue;

                // Get all living cells owned by this player
                var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(cell => cell.IsAlive && !cell.IsResistant) // Don't double-fortify already resistant cells
                    .ToList();

                if (livingCells.Count == 0)
                    continue;

                // Calculate how many cells to fortify based on level
                int cellsToFortify = level * GameBalance.ChitinFortificationCellsPerLevel;
                cellsToFortify = Math.Min(cellsToFortify, livingCells.Count); // Don't exceed available cells

                // Randomly select cells to make resistant
                var cellsToFortifyList = new List<FungalCell>();
                for (int i = 0; i < cellsToFortify; i++)
                {
                    int randomIndex = rng.Next(livingCells.Count);
                    cellsToFortifyList.Add(livingCells[randomIndex]);
                    livingCells.RemoveAt(randomIndex); // Ensure no duplicates
                }

                // Make selected cells resistant
                foreach (var cell in cellsToFortifyList)
                {
                    cell.MakeResistant();
                }

                // Track the effect for simulation
                if (cellsToFortify > 0)
                {
                    observer?.RecordChitinFortificationCellsFortified(player.PlayerId, cellsToFortify);
                }
            }
        }

        // Phase event handlers
        public static void OnPostGrowthPhase_HyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                if (player.GetMutationLevel(MutationIds.HyphalVectoring) > 0)
                {
                    ProcessHyphalVectoring(board, players, rng, observer);
                }
            }
        }

        /// <summary>
        /// Handles Mimetic Resilience effect at the end of Growth Phase.
        /// Targets players with significant cell advantage and board control, placing resistant cells adjacent to their resistant cells.
        /// Prioritizes infesting enemy cells over empty placements, and processes one cell per qualifying enemy player.
        /// </summary>
        public static void OnPostGrowthPhase_MimeticResilience(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.MimeticResilience);
                if (level <= 0 || !player.IsSurgeActive(MutationIds.MimeticResilience))
                    continue;

                // Find eligible target players (those with significant advantage)
                var targetPlayers = FindMimeticResilienceTargets(player, players, board);
                if (targetPlayers.Count == 0)
                    continue;

                int totalInfestations = 0;
                int totalDrops = 0;

                // Process each eligible target player separately
                foreach (var targetPlayer in targetPlayers)
                {
                    // Find resistant cells belonging to the target player
                    var targetResistantCells = board.GetAllCellsOwnedBy(targetPlayer.PlayerId)
                        .Where(cell => cell.IsAlive && cell.IsResistant)
                        .ToList();

                    if (targetResistantCells.Count == 0)
                        continue; // No resistant cells to copy from

                    // Select a random resistant cell from the target
                    var sourceResistantCell = targetResistantCells[rng.Next(targetResistantCells.Count)];
                    var sourceTile = board.GetTileById(sourceResistantCell.TileId);

                    // Find adjacent tiles and categorize them by priority
                    var adjacentTiles = board.GetOrthogonalNeighbors(sourceTile.TileId);
                    
                    // Priority 1: Enemy cells (not resistant) that can be infested
                    var infestationTargets = adjacentTiles
                        .Where(tile => tile.FungalCell != null && 
                                     tile.FungalCell.IsAlive && 
                                     tile.FungalCell.OwnerPlayerId != player.PlayerId &&
                                     !tile.FungalCell.IsResistant)
                        .ToList();
                    
                    // Priority 2: Empty tiles for colonization
                    var colonizationTargets = adjacentTiles
                        .Where(tile => tile.FungalCell == null)
                        .ToList();
                    
                    // Priority 3: Dead cells for reclamation (including own dead cells)
                    var reclamationTargets = adjacentTiles
                        .Where(tile => tile.FungalCell != null && 
                                     !tile.FungalCell.IsAlive)
                        .ToList();

                    BoardTile? selectedTile = null;
                    bool isInfestation = false;

                    // Try to infest an enemy cell first (priority)
                    if (infestationTargets.Count > 0)
                    {
                        selectedTile = infestationTargets[rng.Next(infestationTargets.Count)];
                        isInfestation = true;
                    }
                    // Fall back to empty tiles
                    else if (colonizationTargets.Count > 0)
                    {
                        selectedTile = colonizationTargets[rng.Next(colonizationTargets.Count)];
                        isInfestation = false;
                    }
                    // Fall back to dead cells
                    else if (reclamationTargets.Count > 0)
                    {
                        selectedTile = reclamationTargets[rng.Next(reclamationTargets.Count)];
                        isInfestation = false;
                    }

                    if (selectedTile == null)
                        continue; // No valid placement locations

                    // Handle the placement based on tile state
                    if (selectedTile.FungalCell != null)
                    {
                        // Use TakeoverCell for existing cells (both living and dead)
                        var takeoverResult = board.TakeoverCell(
                            selectedTile.TileId, 
                            player.PlayerId, 
                            allowToxin: false, // Mimetic Resilience doesn't take over toxins
                            GrowthSource.MimeticResilience,
                            players: players, 
                            rng: rng, 
                            observer: observer);

                        // Make the cell resistant after successful takeover
                        if (takeoverResult == FungalCellTakeoverResult.Infested ||
                            takeoverResult == FungalCellTakeoverResult.Reclaimed ||
                            takeoverResult == FungalCellTakeoverResult.CatabolicGrowth)
                        {
                            selectedTile.FungalCell?.MakeResistant();
                            
                            // Track the specific effect type
                            if (takeoverResult == FungalCellTakeoverResult.Infested)
                            {
                                totalInfestations++;
                            }
                            else
                            {
                                totalDrops++;
                            }
                        }
                    }
                    else
                    {
                        // Place a new resistant cell on empty tile
                        var newCell = new FungalCell(player.PlayerId, selectedTile.TileId, GrowthSource.MimeticResilience);
                        newCell.MakeResistant(); // Apply resistance immediately
                        selectedTile.PlaceFungalCell(newCell);
                        totalDrops++;
                    }
                }

                // Record the aggregated effects for this player
                if (totalInfestations > 0)
                    observer?.RecordMimeticResilienceInfestations(player.PlayerId, totalInfestations);
                if (totalDrops > 0)
                    observer?.RecordMimeticResilienceDrops(player.PlayerId, totalDrops);
            }
        }

        /// <summary>
        /// Finds players that meet the criteria for Mimetic Resilience targeting.
        /// </summary>
        private static List<Player> FindMimeticResilienceTargets(Player mimicPlayer, List<Player> allPlayers, GameBoard board)
        {
            var mimicLivingCount = board.GetAllCellsOwnedBy(mimicPlayer.PlayerId).Count(c => c.IsAlive);
            var totalBoardTiles = board.Width * board.Height;
            var targetPlayers = new List<Player>();

            foreach (var candidate in allPlayers)
            {
                if (candidate.PlayerId == mimicPlayer.PlayerId)
                    continue; // Can't target self

                var candidateLivingCount = board.GetAllCellsOwnedBy(candidate.PlayerId).Count(c => c.IsAlive);
                var candidateBoardControl = (float)candidateLivingCount / totalBoardTiles;

                // Check if candidate meets both criteria
                bool hasSignificantAdvantage = candidateLivingCount >= mimicLivingCount * (1.0f + GameBalance.MimeticResilienceMinimumCellAdvantageThreshold);
                bool hasBoardControl = candidateBoardControl >= GameBalance.MimeticResilienceMinimumBoardControlThreshold;

                if (hasSignificantAdvantage && hasBoardControl)
                {
                    targetPlayers.Add(candidate);
                }
            }

            return targetPlayers;
        }
    }}