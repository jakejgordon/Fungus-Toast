using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    Console.WriteLine($"[HyphalVectoring] Player {player.PlayerId}: no valid origin found.");
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
                            case FungalCellTakeoverResult.Overgrown: catabolicGrowth++; break;
                            case FungalCellTakeoverResult.AlreadyOwned: alreadyOwned++; break;
                            case FungalCellTakeoverResult.Invalid: invalid++; break;
                        }
                    }
                    else
                    {
                        // Place a new living cell if empty
                        var newCell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: targetTileId, source: GrowthSource.HyphalVectoring, lastOwnerPlayerId: null);
                        board.PlaceFungalCell(newCell); // Use board.PlaceFungalCell instead of targetTile.PlaceFungalCell for proper tracking
                        colonized++;
                    }

                    placed++;
                    currentTileId = targetTileId;
                }

                // Report results to simulation observer
                if (infested > 0) observer.ReportHyphalVectoringInfested(player.PlayerId, infested);
                if (reclaimed > 0) observer.ReportHyphalVectoringReclaimed(player.PlayerId, reclaimed);
                if (catabolicGrowth > 0) observer.ReportHyphalVectoringCatabolicGrowth(player.PlayerId, catabolicGrowth);
                if (alreadyOwned > 0) observer.ReportHyphalVectoringAlreadyOwned(player.PlayerId, alreadyOwned);
                if (colonized > 0) observer.ReportHyphalVectoringColonized(player.PlayerId, colonized);
                if (invalid > 0) observer.ReportHyphalVectoringInvalid(player.PlayerId, invalid);

                if (placed > 0)
                    observer.RecordHyphalVectoringGrowth(player.PlayerId, placed);
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
                    observer.RecordChitinFortificationCellsFortified(player.PlayerId, cellsToFortify);
                }

                // NEW: Fire batch resistance animation events for Chitin Fortification placements.
                if (cellsToFortifyList.Count > 0)
                {
                    var fortifiedIds = cellsToFortifyList.Select(c => c.TileId).ToList();
                    board.OnResistanceAppliedBatch(player.PlayerId, GrowthSource.ChitinFortification, fortifiedIds);
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
        /// Handles Mimetic Resilience effect at the end of Growth Phase (UPDATED LOGIC).
        /// Now also accumulates resistant placements and fires a single batch event per acting player.
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

                var targetPlayers = FindMimeticResilienceTargets_New(player, players, board);
                if (targetPlayers.Count == 0)
                    continue;

                int totalInfestations = 0;
                int totalDrops = 0;
                int radius = 1 + level; // dynamic Chebyshev radius

                // NEW: Collect all tileIds that became newly resistant due to Mimetic Resilience for this acting player
                var newlyResistantTileIds = new List<int>(64);

                foreach (var targetPlayer in targetPlayers)
                {
                    var targetResistantCells = board.GetAllCellsOwnedBy(targetPlayer.PlayerId)
                        .Where(c => c.IsAlive && c.IsResistant)
                        .ToList();
                    if (targetResistantCells.Count == 0)
                        continue;

                    // Shuffle source resistant cells to avoid positional bias
                    for (int i = targetResistantCells.Count - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        (targetResistantCells[i], targetResistantCells[j]) = (targetResistantCells[j], targetResistantCells[i]);
                    }

                    int successesForOpponent = 0; // X in probability reduction formula

                    foreach (var sourceCell in targetResistantCells)
                    {
                        if (successesForOpponent >= 20)
                            break; // cap per opponent

                        var sourceTile = board.GetTileById(sourceCell.TileId);
                        if (sourceTile == null)
                            continue;

                        // Collect candidate tiles within Chebyshev radius (includes diagonals)
                        var candidateTiles = GetTilesInChebyshevRange(board, sourceTile.X, sourceTile.Y, radius)
                            .Where(t => t.TileId != sourceTile.TileId) // exclude the source tile itself
                            .ToList();

                        // Priority buckets
                        var enemyLiving = candidateTiles.Where(t => t.FungalCell != null && t.FungalCell.IsAlive && !t.FungalCell.IsResistant && t.FungalCell.OwnerPlayerId != player.PlayerId).ToList();
                        var enemyToxins = candidateTiles.Where(t => t.FungalCell != null && t.FungalCell.CellType == FungalCellType.Toxin && t.FungalCell.OwnerPlayerId != player.PlayerId).ToList();
                        var empty = candidateTiles.Where(t => t.FungalCell == null).ToList();
                        var dead = candidateTiles.Where(t => t.FungalCell != null && !t.FungalCell.IsAlive).ToList();

                        if (enemyLiving.Count == 0 && enemyToxins.Count == 0 && empty.Count == 0 && dead.Count == 0)
                            continue; // no valid targets around this source in extended radius

                        float chance = 1f - 0.05f * successesForOpponent; // 100% - 5% * X
                        if (chance <= 0f)
                            break; // probability floor reached
                        if (rng.NextDouble() > chance)
                            continue; // failed roll

                        BoardTile targetTile = null;
                        if (enemyLiving.Count > 0) targetTile = enemyLiving[rng.Next(enemyLiving.Count)];
                        else if (enemyToxins.Count > 0) targetTile = enemyToxins[rng.Next(enemyToxins.Count)];
                        else if (empty.Count > 0) targetTile = empty[rng.Next(empty.Count)];
                        else if (dead.Count > 0) targetTile = dead[rng.Next(dead.Count)];
                        if (targetTile == null) continue;

                        bool placement = false;
                        if (targetTile.FungalCell != null)
                        {
                            bool allowToxin = targetTile.FungalCell.CellType == FungalCellType.Toxin; // allow toxin replacement
                            var takeover = board.TakeoverCell(targetTile.TileId, player.PlayerId, allowToxin, GrowthSource.MimeticResilience, players, rng, observer);
                            if (takeover == FungalCellTakeoverResult.Infested ||
                                takeover == FungalCellTakeoverResult.Reclaimed ||
                                takeover == FungalCellTakeoverResult.Overgrown)
                            {
                                targetTile.FungalCell?.MakeResistant();
                                if (targetTile.FungalCell != null)
                                    newlyResistantTileIds.Add(targetTile.TileId);
                                placement = true;
                                if (takeover == FungalCellTakeoverResult.Infested) totalInfestations++; else totalDrops++;
                            }
                        }
                        else
                        {
                            var newCell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: targetTile.TileId, source: GrowthSource.MimeticResilience, lastOwnerPlayerId: null);
                            newCell.MakeResistant();
                            board.PlaceFungalCell(newCell); // ensure dictionary + events stay consistent
                            newlyResistantTileIds.Add(targetTile.TileId);
                            placement = true;
                            totalDrops++;
                        }

                        if (placement)
                            successesForOpponent++;
                    }
                }

                if (totalInfestations > 0) observer.RecordMimeticResilienceInfestations(player.PlayerId, totalInfestations);
                if (totalDrops > 0) observer.RecordMimeticResilienceDrops(player.PlayerId, totalDrops);

                // Fire batch resistance event for visualization (Unity can animate pulses)
                if (newlyResistantTileIds.Count > 0)
                {
                    board.OnResistanceAppliedBatch(player.PlayerId, GrowthSource.MimeticResilience, newlyResistantTileIds);
                }
            }
        }

        /// <summary>
        /// Returns tiles within Chebyshev (king-move) distance <= radius from (x,y).
        /// </summary>
        private static IEnumerable<BoardTile> GetTilesInChebyshevRange(GameBoard board, int x, int y, int radius)
        {
            int minX = Math.Max(0, x - radius);
            int maxX = Math.Min(board.Width - 1, x + radius);
            int minY = Math.Max(0, y - radius);
            int maxY = Math.Min(board.Height - 1, y + radius);
            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    if (Math.Max(Math.Abs(cx - x), Math.Abs(cy - y)) <= radius)
                        yield return board.Grid[cx, cy];
                }
            }
        }

        /// <summary>
        /// New target selection helper for updated Mimetic Resilience logic.
        /// </summary>
        private static List<Player> FindMimeticResilienceTargets_New(Player actingPlayer, List<Player> players, GameBoard board)
        {
            float advantageThreshold = 1f + GameBalance.MimeticResilienceMinimumCellAdvantageThreshold;
            float controlThreshold = GameBalance.MimeticResilienceMinimumBoardControlThreshold;
            int actingLiving = board.GetAllCellsOwnedBy(actingPlayer.PlayerId).Count(c => c.IsAlive);
            int totalTiles = board.Width * board.Height;
            var result = new List<Player>();
            foreach (var p in players)
            {
                if (p.PlayerId == actingPlayer.PlayerId) continue;
                int oppLiving = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive);
                if (oppLiving <= 0) continue;
                if (actingLiving > 0 && oppLiving < actingLiving * advantageThreshold) continue;
                int oppControlled = board.GetAllCellsOwnedBy(p.PlayerId).Count;
                float controlFrac = (float)oppControlled / totalTiles;
                if (controlFrac < controlThreshold) continue;
                result.Add(p);
            }
            return result;
        }

        /// <summary>
        /// Gets the list of players that should be prioritized for targeting based on colony size.
        /// Returns players ordered by living cell count (descending), excluding the requesting player.
        /// </summary>
        public static List<Player> GetCompetitiveAntagonismTargets(Player requestingPlayer, List<Player> allPlayers, GameBoard board)
        {
            var playerCellCounts = allPlayers.ToDictionary(
                p => p.PlayerId,
                p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive)
            );

            int requestingPlayerCells = playerCellCounts[requestingPlayer.PlayerId];

            // Return players with more cells than the requesting player, ordered by cell count (descending)
            return allPlayers
                .Where(p => p.PlayerId != requestingPlayer.PlayerId && playerCellCounts[p.PlayerId] > requestingPlayerCells)
                .OrderByDescending(p => playerCellCounts[p.PlayerId])
                .ToList();
        }

        /// <summary>
        /// Checks if a player has Competitive Antagonism active.
        /// </summary>
        public static bool IsCompetitiveAntagonismActive(Player player)
        {
            return player.GetMutationLevel(MutationIds.CompetitiveAntagonism) > 0 &&
                   player.IsSurgeActive(MutationIds.CompetitiveAntagonism);
        }

        /// <summary>
        /// Gets the Competitive Antagonism level for a player if the surge is active.
        /// </summary>
        public static int GetCompetitiveAntagonismLevel(Player player)
        {
            if (!IsCompetitiveAntagonismActive(player))
                return 0;
            return player.GetMutationLevel(MutationIds.CompetitiveAntagonism);
        }
    }
}