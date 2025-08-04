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
    /// Performs no mutation mathematics—delegates that to MutationEffectCoordinator.
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
        /// <param name="simulationObserver">Observer for analytics/UI updates.</param>
        /// <param name="tracking">Optional tracking context for simulation.</param>
        public static void ExecuteDeathCycle(
            GameBoard board,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            List<Player> shuffledPlayers = board.Players.OrderBy(_ => rng.NextDouble()).ToList();

            // Fire consolidated DecayPhase event for all decay-phase mutations (including Mycotoxin Tracer)
            board.OnDecayPhase(failedGrowthsByPlayerId);
            
            ApplyNecrophyticBloomTrigger(shuffledPlayers, board, rng, simulationObserver);
            EvaluateProbabilisticDeaths(board, shuffledPlayers, rng, simulationObserver);
        }

        /*
        private static void ApplyPerTurnSporeEffects(
            List<Player> players,
            GameBoard board,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            // Mycotoxin Tracer is now handled via DecayPhaseWithFailedGrowths event
        }
        */

        /// <summary>
        /// Handles the board occupancy trigger for Necrophytic Bloom.
        /// For each player with the mutation, fires an initial burst of spores for *each* dead cell they have.
        /// </summary>
        private static void ApplyNecrophyticBloomTrigger(
            List<Player> players,
            GameBoard board,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            float occupiedPercent = board.GetOccupiedTileRatio();

            if (!board.NecrophyticBloomActivated &&
                occupiedPercent >= GameBalance.NecrophyticBloomActivationThreshold)
            {
                board.NecrophyticBloomActivated = true;
                board.OnNecrophyticBloomActivatedEvent();
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
            ISimulationObserver simulationObserver)
        {
            var livingCellCounts = players.ToDictionary(
                p => p.PlayerId,
                p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));

            List<BoardTile> livingNonResistantTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true, IsResistant: false })
                .ToList();

            foreach (BoardTile tile in livingNonResistantTiles)
            {
                FungalCell cell = tile.FungalCell!;
                Player owner = players.First(p => p.PlayerId == cell.OwnerPlayerId);

                // Prevent killing a player's last cell
                if (livingCellCounts[owner.PlayerId] <= 1)
                    continue;

                double roll = rng.NextDouble();
                var deathResult = MutationEffectCoordinator.CalculateDeathChance(owner, cell, board, players, roll, rng, simulationObserver);

                if (deathResult.ShouldDie)
                {
                    // Pass both killerPlayerId and attackerTileId to the board so the event can record them
                    board.KillFungalCell(cell, deathResult.Reason!.Value, deathResult.KillerPlayerId, deathResult.AttackerTileId);
                    livingCellCounts[owner.PlayerId]--;

                    // Attribute Age/Randomness deaths to observer
                    if (deathResult.Reason == DeathReason.Age || deathResult.Reason == DeathReason.Randomness)
                    {
                        simulationObserver.RecordCellDeath(owner.PlayerId, deathResult.Reason.Value, 1);
                    }

                    // Putrefactive Mycotoxin: attribute in observer if needed
                    if (deathResult.Reason == DeathReason.PutrefactiveMycotoxin)
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
                        // Apply Necrophytic Bloom on each cell death
                        GeneticDriftMutationProcessor.TriggerNecrophyticBloomOnCellDeath(owner, board, rng, occupiedPercent, simulationObserver);
                    }
                }
                // NOTE: Cell aging is now handled in AgeCells method, not here
            }
        }

        /// <summary>
        /// Attributes toxin-related kills to the observer for analytics.
        /// </summary>
        private static void AttributePutrefactiveMycotoxinKill(
             FungalCell deadCell,
             GameBoard board,
             List<Player> players,
             ISimulationObserver observer)
        {
            if (observer == null) return;

            foreach (var neighbor in board.GetOrthogonalNeighbors(deadCell.TileId))
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
