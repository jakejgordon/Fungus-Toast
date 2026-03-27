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
    /// All stateful triggers are tracked per-game via GameBoard.
    /// </summary>
    public static class DeathEngine
    {
        /// <summary>
        /// Executes the Decay Phase for the board.
        /// All cell state changes are routed through GameBoard methods to trigger events and observers.
        /// </summary>
        /// <param name="board">Current game board.</param>
        /// <param name="failedGrowthsByPlayerId">Dictionary of failed growth attempts by player ID.</param>
        /// <param name="rng">Random number generator (pass in for thread safety & testability).</param>
        /// <param name="simulationObserver">Observer for analytics/UI updates.</param>
        public static void ExecuteDeathCycle(
            GameBoard board,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            List<Player> shuffledPlayers = board.Players.OrderBy(_ => rng.NextDouble()).ToList();

            // Cache the occupied tile ratio and decay phase context at the start of the decay phase
            board.UpdateCachedOccupiedTileRatio();
            board.UpdateCachedDecayPhaseContext();

            // Fire consolidated DecayPhase event for all decay-phase mutations (including Mycotoxin Tracer)
            board.OnDecayPhase(failedGrowthsByPlayerId);

            EvaluateProbabilisticDeaths(board, shuffledPlayers, rng, simulationObserver);
            ResolveNecrophyticBloomComposting(board, shuffledPlayers, rng, simulationObserver);

            // Clear the decay phase context at the end
            board.ClearCachedDecayPhaseContext();
        }

        /// <summary>
        /// Handles all living cells, rolling for probabilistic deaths.
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
                    if (AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(board, owner, cell, deathResult))
                    {
                        livingCellCounts[owner.PlayerId]--;
                        continue;
                    }

                    // Pass both killerPlayerId and attackerTileId to the board so the event can record them
                    board.KillFungalCell(cell, deathResult.Reason!.Value, deathResult.KillerPlayerId, deathResult.AttackerTileId);
                    livingCellCounts[owner.PlayerId]--;

                    // Putrefactive Mycotoxin: attribute in observer if needed
                    if (deathResult.Reason == DeathReason.PutrefactiveMycotoxin)
                    {
                        AttributePutrefactiveMycotoxinKill(cell, board, players, simulationObserver);
                    }

                    // Trigger spore on death, if relevant
                    board.TryTriggerSporeOnDeath(owner, rng, simulationObserver);
                }
                // NOTE: Cell aging is now handled in AgeCells method, not here
            }
        }

        private static void ResolveNecrophyticBloomComposting(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            foreach (var player in players)
            {
                if (player.GetMutationLevel(MutationIds.NecrophyticBloom) <= 0)
                {
                    continue;
                }

                GeneticDriftMutationProcessor.ResolveNecrophyticBloomComposting(player, board, rng, simulationObserver);
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
                    observer.RecordAttributedKill(neighborOwnerId, DeathReason.PutrefactiveMycotoxin, 1);
                }
            }
        }
    }
}
