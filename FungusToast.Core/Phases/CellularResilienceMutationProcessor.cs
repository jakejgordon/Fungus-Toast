using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Growth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Handles all mutation effects related to the CellularResilience category.
    /// </summary>
    public static class CellularResilienceMutationProcessor
    {
        /// <summary>
        /// Advances or resets cell age based on player's age reset threshold.
        /// </summary>
        public static void AdvanceOrResetCellAge(Player player, FungalCell cell)
        {
            int resetAt = player.GetSelfAgeResetThreshold();
            if (cell.GrowthCycleAge >= resetAt)
                cell.ResetGrowthCycleAge();
            else
                cell.IncrementGrowthAge();
        }

        /// <summary>
        /// Handles the Regenerative Hyphae effect - reclaims own dead cells adjacent to living cells.
        /// </summary>
        public static void OnPostGrowthPhase_RegenerativeHyphae(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            var attempted = new HashSet<int>();
            foreach (Player p in players)
            {
                float reclaimChance = p.GetMutationEffect(MutationType.ReclaimOwnDeadCells);
                if (reclaimChance <= 0f) continue;
                foreach (FungalCell cell in board.GetAllCellsOwnedBy(p.PlayerId))
                {
                    foreach (BoardTile n in board.GetOrthogonalNeighbors(cell.TileId))
                    {
                        FungalCell? dead = n.FungalCell;
                        if (dead is null || dead.IsAlive || dead.IsToxin) continue;
                        if (dead.OwnerPlayerId != p.PlayerId) continue;
                        if (!attempted.Add(dead.TileId)) continue;
                        
                        // Try to reclaim the dead cell using the helper
                        bool success = ReclaimCellHelper.TryReclaimDeadCell(
                            board, p, dead.TileId, reclaimChance, rng, GrowthSource.RegenerativeHyphae, observer);
                        if (success)
                        {
                            observer?.RecordRegenerativeHyphaeReclaim(p.PlayerId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles Necrohyphal Infiltration - attempts to reclaim adjacent dead enemy cells.
        /// </summary>
        public static bool TryNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null)
        {
            int necroLevel = owner.GetMutationLevel(MutationIds.NecrohyphalInfiltration);
            if (necroLevel <= 0)
                return false;

            double baseChance = GameBalance.NecrohyphalInfiltrationChancePerLevel * necroLevel;
            double cascadeChance = GameBalance.NecrohyphalInfiltrationCascadeChancePerLevel * necroLevel;

            // Find adjacent dead enemy cells
            var deadEnemyNeighbors = board
                .GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                .Where(tile =>
                    tile.IsOccupied &&
                    tile.FungalCell != null &&
                    tile.FungalCell.IsDead &&
                    tile.FungalCell.OwnerPlayerId.HasValue &&
                    tile.FungalCell.OwnerPlayerId.Value != owner.PlayerId)
                .ToList();

            Shuffle(deadEnemyNeighbors, rng);

            foreach (var deadTile in deadEnemyNeighbors)
            {
                // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
                bool success = ReclaimCellHelper.TryReclaimDeadCell(
                    board, owner, deadTile.TileId, (float)baseChance, rng, GrowthSource.NecrohyphalInfiltration, observer);
                if (success)
                {
                    // Track which tiles have already been reclaimed
                    var alreadyReclaimed = new HashSet<int> { deadTile.TileId };

                    // Cascade infiltrations
                    int cascadeCount = CascadeNecrohyphalInfiltration(
                        board, deadTile, owner, rng, (float)cascadeChance, alreadyReclaimed);

                    // Record: 1 infiltration for the initial, cascades for the rest
                    observer?.RecordNecrohyphalInfiltration(owner.PlayerId, 1);
                    if (cascadeCount > 0)
                        observer?.RecordNecrohyphalInfiltrationCascade(owner.PlayerId, cascadeCount);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles Catabolic Rebirth effect when toxins expire.
        /// </summary>
        public static void OnToxinExpired_CatabolicRebirth(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            // Check adjacent tiles for dead cells
            var adjacentTiles = board.GetOrthogonalNeighbors(eventArgs.TileId);
            var resurrectionsByPlayer = new Dictionary<int, int>();

            foreach (var adjacentTile in adjacentTiles)
            {
                if (adjacentTile.FungalCell == null || !adjacentTile.FungalCell.IsDead)
                    continue;

                // Find the owner of the dead cell
                int? deadCellOwnerId = adjacentTile.FungalCell.OwnerPlayerId;
                if (!deadCellOwnerId.HasValue)
                    continue;

                var deadCellOwner = players.FirstOrDefault(p => p.PlayerId == deadCellOwnerId.Value);
                if (deadCellOwner == null)
                    continue;

                // Check if the dead cell's owner has Catabolic Rebirth
                int level = deadCellOwner.GetMutationLevel(MutationIds.CatabolicRebirth);
                if (level <= 0)
                    continue;

                float chance = level * GameBalance.CatabolicRebirthResurrectionChancePerLevel;
                
                // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
                bool success = ReclaimCellHelper.TryReclaimDeadCell(
                    board, deadCellOwner, adjacentTile.TileId, (float)chance, rng, GrowthSource.CatabolicRebirth, observer);
                if (success)
                {
                    // Track resurrections by player
                    if (!resurrectionsByPlayer.ContainsKey(deadCellOwner.PlayerId))
                        resurrectionsByPlayer[deadCellOwner.PlayerId] = 0;
                    resurrectionsByPlayer[deadCellOwner.PlayerId]++;
                }
            }

            // Report resurrections for each player
            foreach (var (playerId, count) in resurrectionsByPlayer)
            {
                observer?.RecordCatabolicRebirthResurrection(playerId, count);
                
                // Fire the CatabolicRebirth event
                var (x, y) = board.GetXYFromTileId(eventArgs.TileId);
                var rebirthArgs = new CatabolicRebirthEventArgs(playerId, count, x, y);
                board.OnCatabolicRebirth(rebirthArgs);
            }
        }

        private static int CascadeNecrohyphalInfiltration(
           GameBoard board,
           BoardTile sourceTile,
           Player owner,
           Random rng,
           float cascadeChance,
           HashSet<int> alreadyReclaimed,
           ISimulationObserver? observer = null)
        {
            int cascadeCount = 0;
            var toCheck = new Queue<BoardTile>();
            toCheck.Enqueue(sourceTile);

            while (toCheck.Count > 0)
            {
                var currentTile = toCheck.Dequeue();

                var nextTargets = board
                    .GetOrthogonalNeighbors(currentTile.X, currentTile.Y)
                    .Where(tile =>
                        tile.IsOccupied &&
                        tile.FungalCell != null &&
                        tile.FungalCell.IsDead &&
                        tile.FungalCell.OwnerPlayerId.HasValue &&
                        tile.FungalCell.OwnerPlayerId.Value != owner.PlayerId &&
                        !alreadyReclaimed.Contains(tile.TileId))
                    .ToList();

                Shuffle(nextTargets, rng);

                foreach (var deadTile in nextTargets)
                {
                    // Try to reclaim the dead cell using the helper (supports Reclamation Rhizomorphs retry)
                    bool success = ReclaimCellHelper.TryReclaimDeadCell(
                        board, owner, deadTile.TileId, (float)cascadeChance, rng, GrowthSource.NecrohyphalInfiltration, observer);
                    if (success)
                    {
                        alreadyReclaimed.Add(deadTile.TileId);

                        cascadeCount++;
                        toCheck.Enqueue(deadTile);
                    }
                }
            }

            return cascadeCount;
        }

        // Utility: Fisher-Yates shuffle
        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}