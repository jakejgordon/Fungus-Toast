using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Phases; 
using FungusToast.Core.Metrics;

namespace FungusToast.Core.Death
{
    /// <summary>
    /// Orchestrates the Decay Phase for the entire board.
    /// Performs no mutation mathematics—delegates that to MutationEffectProcessor.
    /// </summary>
    public static class DeathEngine
    {
        private static readonly Random Rng = new();

        // Tracks whether the 20%-occupied trigger for Necrophytic Bloom has fired.
        private static bool necrophyticActivated = false;

        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            ISimulationObserver? simulationObserver = null)
        {
            // Expire toxins before growth begins
            board.ExpireToxinTiles(board.CurrentGrowthCycle);
            List<Player> shuffledPlayers = players.OrderBy(_ => Rng.NextDouble()).ToList();

            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            Mutation sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            ApplyPerTurnSporeEffects(shuffledPlayers, board, sporocidalBloom, failedGrowthsByPlayerId, simulationObserver);
            ApplyNecrophyticBloomTrigger(shuffledPlayers, board, simulationObserver);
            MutationEffectProcessor.ApplyToxinAuraDeaths(board, players, Rng, simulationObserver);
            EvaluateProbabilisticDeaths(board, shuffledPlayers, simulationObserver);
        }

        private static void ApplyPerTurnSporeEffects(
            List<Player> players,
            GameBoard board,
            Mutation sporocidalBloom,
            Dictionary<int, int> failedGrowthsByPlayerId,
            ISimulationObserver? simulationObserver)
        {
            foreach (var p in players)
            {
                board.TryPlaceSporocidalSpores(p, Rng, sporocidalBloom, simulationObserver);

                int failedGrowths = failedGrowthsByPlayerId.TryGetValue(p.PlayerId, out var v) ? v : 0;
                MutationEffectProcessor.ApplyMycotoxinTracer(p, board, failedGrowths, Rng, simulationObserver);
            }
        }

        /// <summary>
        /// Handles the 20% board occupancy trigger for Necrophytic Bloom. 
        /// For each player with the mutation, fires an initial burst of spores for *each* dead cell they have.
        /// </summary>
        private static void ApplyNecrophyticBloomTrigger(
            List<Player> players,
            GameBoard board,
            ISimulationObserver? simulationObserver = null)
        {
            float occupiedPercent = board.GetOccupiedTileRatio();

            if (!necrophyticActivated && occupiedPercent >= 0.20f)
            {
                necrophyticActivated = true;

                foreach (var p in players)
                {
                    if (p.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        MutationEffectProcessor.TriggerNecrophyticBloomInitialBurst(
                            p, board, Rng, simulationObserver);
                    }
                }
            }
        }

        /// <summary>
        /// Handles all living cells, rolling for probabilistic deaths. After Necrophytic Bloom activates,
        /// each cell death for a player with the mutation triggers per-death spores with damping.
        /// </summary>
        private static void EvaluateProbabilisticDeaths(
            GameBoard board,
            List<Player> players,
            ISimulationObserver? simulationObserver = null)
        {
            var livingCellCounts = players.ToDictionary(
                p => p.PlayerId,
                p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));

            List<BoardTile> livingTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true })
                .ToList();

            foreach (BoardTile tile in livingTiles)
            {
                FungalCell cell = tile.FungalCell!;
                Player owner = players.First(p => p.PlayerId == cell.OwnerPlayerId);

                if (livingCellCounts[owner.PlayerId] <= 1)
                    continue;

                double roll = Rng.NextDouble();
                (float _, DeathReason? reason) =
                    MutationEffectProcessor.CalculateDeathChance(owner, cell, board, players, roll);

                if (reason.HasValue)
                {
                    board.KillFungalCell(cell, reason.Value);
                    livingCellCounts[owner.PlayerId]--;

                    // --- NEW: Attribute Age/Randomness deaths to observer
                    if (simulationObserver != null)
                    {
                        if (reason.Value == DeathReason.Age || reason.Value == DeathReason.Randomness)
                        {
                            simulationObserver.RecordCellDeath(owner.PlayerId, reason.Value, 1);
                        }
                    }

                    // Try Necrotoxic Conversion for toxin-based kills
                    MutationEffectProcessor.TryNecrotoxicConversion(
                        cell, board, players, Rng, simulationObserver);

                    if (reason.Value == DeathReason.PutrefactiveMycotoxin && simulationObserver != null)
                    {
                        AttributePutrefactiveMycotoxinKill(cell, board, players, simulationObserver);
                    }

                    board.TryTriggerSporeOnDeath(owner, Rng, simulationObserver);

                    // --- PER-DEATH Necrophytic Bloom effect ---
                    if (necrophyticActivated &&
                        owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        float occupiedPercent = board.GetOccupiedTileRatio();
                        MutationEffectProcessor.TriggerNecrophyticBloomOnCellDeath(
                            owner, board, Rng, occupiedPercent, simulationObserver);
                    }
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(owner, cell);
                }
            }
        }


        private static void AttributePutrefactiveMycotoxinKill(
             FungalCell deadCell,
             GameBoard board,
             List<Player> players,
             ISimulationObserver? observer = null)
        {
            if (observer == null) return;  // Safely do nothing if not tracking

            foreach (var neighbor in board.GetAdjacentTiles(deadCell.TileId))
            {
                var neighborCell = neighbor.FungalCell;
                if (neighborCell == null || !neighborCell.IsAlive) continue;
                int neighborOwnerId = neighborCell.OwnerPlayerId ?? -1;
                if (neighborOwnerId == deadCell.OwnerPlayerId) continue;

                var enemyPlayer = players.FirstOrDefault(p => p.PlayerId == neighborOwnerId);
                if (enemyPlayer == null) continue;

                if (enemyPlayer.GetMutationEffect(MutationType.AdjacentFungicide) > 0f)
                {
                    observer.RecordCellDeath(neighborOwnerId, DeathReason.PutrefactiveMycotoxin, 1);
                }
            }
        }



    }
}
