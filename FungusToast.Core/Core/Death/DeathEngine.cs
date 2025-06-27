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
    /// All stateful triggers (such as Necrophytic Bloom activation) are tracked per-game via GameBoard.
    /// </summary>
    public static class DeathEngine
    {
        /// <summary>
        /// Executes the Decay Phase for the board.
        /// All cell state changes are routed through GameBoard methods to trigger events and observers.
        /// </summary>
        /// <param name="board">Current game board.</param>
        /// <param name="players">List of all players (in play order).</param>
        /// <param name="failedGrowthsByPlayerId">Dictionary of failed growth attempts by player ID.</param>
        /// <param name="rng">Random number generator (pass in for thread safety & testability).</param>
        /// <param name="simulationObserver">Optional observer for analytics/UI updates.</param>
        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? simulationObserver = null)
        {
            // Expire toxins before decay begins
            board.ExpireToxinTiles(board.CurrentGrowthCycle);
            List<Player> shuffledPlayers = players.OrderBy(_ => rng.NextDouble()).ToList();

            // Fire DecayPhaseWithFailedGrowths event for Mycotoxin Tracer and other decay-phase mutations that need failed growth data
            board.OnDecayPhaseWithFailedGrowths(failedGrowthsByPlayerId);
            
            // Fire DecayPhase event for Sporocidal Bloom, Mycotoxin Potentiation, and other decay-phase mutations
            board.OnDecayPhase();
            
            ApplyNecrophyticBloomTrigger(shuffledPlayers, board, rng, simulationObserver);
            EvaluateProbabilisticDeaths(board, shuffledPlayers, rng, simulationObserver);
        }

        private static void ApplyPerTurnSporeEffects(
            List<Player> players,
            GameBoard board,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? simulationObserver)
        {
            // Mycotoxin Tracer is now handled via DecayPhaseWithFailedGrowths event
        }

        /// <summary>
        /// Handles the board occupancy trigger for Necrophytic Bloom.
        /// For each player with the mutation, fires an initial burst of spores for *each* dead cell they have.
        /// </summary>
        private static void ApplyNecrophyticBloomTrigger(
            List<Player> players,
            GameBoard board,
            Random rng,
            ISimulationObserver? simulationObserver = null)
        {
            float occupiedPercent = board.GetOccupiedTileRatio();

            if (!board.NecrophyticBloomActivated &&
                occupiedPercent >= GameBalance.NecrophyticBloomActivationThreshold)
            {
                board.NecrophyticBloomActivated = true;

                foreach (var p in players)
                {
                    if (p.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        MutationEffectProcessor.TriggerNecrophyticBloomInitialBurst(
                            p, board, rng, simulationObserver);
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
            Random rng,
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

                // Prevent killing a player's last cell
                if (livingCellCounts[owner.PlayerId] <= 1)
                    continue;

                double roll = rng.NextDouble();
                (float _, DeathReason? reason, int? killerPlayerId) =
                    MutationEffectProcessor.CalculateDeathChance(owner, cell, board, players, roll);

                if (reason.HasValue)
                {
                    // Pass killerPlayerId to the board so the event can record it
                    board.KillFungalCell(cell, reason.Value, killerPlayerId);
                    livingCellCounts[owner.PlayerId]--;

                    // Attribute Age/Randomness deaths to observer
                    if (simulationObserver != null)
                    {
                        if (reason.Value == DeathReason.Age || reason.Value == DeathReason.Randomness)
                        {
                            simulationObserver.RecordCellDeath(owner.PlayerId, reason.Value, 1);
                        }
                    }

                    // Putrefactive Mycotoxin: attribute in observer if needed
                    if (reason.Value == DeathReason.PutrefactiveMycotoxin && simulationObserver != null)
                    {
                        AttributePutrefactiveMycotoxinKill(cell, board, players, simulationObserver);
                    }

                    // Trigger spore on death, if relevant
                    board.TryTriggerSporeOnDeath(owner, rng, simulationObserver);

                    // Per-death Necrophytic Bloom effect
                    if (board.NecrophyticBloomActivated &&
                        owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        float occupiedPercent = board.GetOccupiedTileRatio();
                        MutationEffectProcessor.TriggerNecrophyticBloomOnCellDeath(
                            owner, board, rng, occupiedPercent, simulationObserver);
                    }
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(owner, cell);
                }
            }
        }

        /// <summary>
        /// Attributes toxin-related kills to the observer for analytics.
        /// </summary>
        private static void AttributePutrefactiveMycotoxinKill(
             FungalCell deadCell,
             GameBoard board,
             List<Player> players,
             ISimulationObserver? observer = null)
        {
            if (observer == null) return;

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
