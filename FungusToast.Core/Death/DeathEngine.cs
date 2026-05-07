using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Logging;
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
            var stopwatch = Stopwatch.StartNew();
            List<Player> shuffledPlayers = board.Players.OrderBy(_ => rng.NextDouble()).ToList();
            LogPhaseTiming($"DeathEngine: shuffled player order in {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds)} ms.");

            // Cache the occupied tile ratio and decay phase context at the start of the decay phase
            var stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            board.UpdateCachedOccupiedTileRatio();
            LogPhaseTiming($"DeathEngine: UpdateCachedOccupiedTileRatio took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            board.UpdateCachedDecayPhaseContext();
            LogPhaseTiming($"DeathEngine: UpdateCachedDecayPhaseContext took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            // Fire consolidated DecayPhase event for all decay-phase mutations (including Mycotoxin Tracer)
            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            board.OnDecayPhase(failedGrowthsByPlayerId);
            LogPhaseTiming($"DeathEngine: OnDecayPhase callbacks took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            EvaluateProbabilisticDeaths(board, shuffledPlayers, rng, simulationObserver);
            LogPhaseTiming($"DeathEngine: EvaluateProbabilisticDeaths took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            ResolveNecrophyticBloomComposting(board, shuffledPlayers, rng, simulationObserver);
            LogPhaseTiming($"DeathEngine: ResolveNecrophyticBloomComposting took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            // Clear the decay phase context at the end
            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            board.ClearCachedDecayPhaseContext();
            LogPhaseTiming($"DeathEngine: ClearCachedDecayPhaseContext took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");
            LogPhaseTiming($"DeathEngine total took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds)} ms.");
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
            var stopwatch = Stopwatch.StartNew();
            double stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            var livingCellCounts = players.ToDictionary(
                p => p.PlayerId,
                p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));
            LogPhaseTiming($"DeathEngine: living cell counts snapshot took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
            List<BoardTile> livingNonResistantTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true, IsResistant: false })
                .ToList();
            LogPhaseTiming($"DeathEngine: built living non-resistant tile list ({livingNonResistantTiles.Count} tiles) in {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds - stepStartMs)} ms.");

            int tilesVisited = 0;
            int tilesRevalidatedAway = 0;
            int missingOwners = 0;
            int protectedLastCellSkips = 0;
            int deathRolls = 0;
            int deathCount = 0;
            int saprophageConsumedCount = 0;
            int sporeTriggers = 0;
            double calculateDeathChanceMs = 0d;
            double killCellMs = 0d;
            double sporeTriggerMs = 0d;
            double putrefactiveAttributionMs = 0d;
            double ownerLookupMs = 0d;

            foreach (BoardTile tile in livingNonResistantTiles)
            {
                tilesVisited++;

                // Re-validate at iteration time: reactive effects (MarginalClamp, NecrotoxicConversion,
                // SaprophageRing, Necrosporulation, etc.) fired during earlier iterations can null out,
                // kill, or replace a tile's cell before we process it.
                FungalCell? cell = tile.FungalCell;
                if (cell == null || !cell.IsAlive || cell.IsResistant || cell.OwnerPlayerId == null)
                {
                    tilesRevalidatedAway++;
                    continue;
                }

                stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
                Player? owner = players.FirstOrDefault(p => p.PlayerId == cell.OwnerPlayerId);
                ownerLookupMs += stopwatch.Elapsed.TotalMilliseconds - stepStartMs;
                if (owner == null)
                {
                    missingOwners++;
                    continue;
                }

                // Prevent killing a player's last cell
                if (!livingCellCounts.TryGetValue(owner.PlayerId, out int livingCount) || livingCount <= 1)
                {
                    protectedLastCellSkips++;
                    continue;
                }

                double roll = rng.NextDouble();
                deathRolls++;

                stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
                var deathResult = MutationEffectCoordinator.CalculateDeathChance(owner, cell, board, players, roll, rng, simulationObserver);
                calculateDeathChanceMs += stopwatch.Elapsed.TotalMilliseconds - stepStartMs;

                if (deathResult.ShouldDie)
                {
                    if (AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(board, owner, cell, deathResult))
                    {
                        saprophageConsumedCount++;
                        livingCellCounts[owner.PlayerId]--;
                        continue;
                    }

                    // Pass both killerPlayerId and attackerTileId to the board so the event can record them
                    stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
                    board.KillFungalCell(cell, deathResult.Reason!.Value, deathResult.KillerPlayerId, deathResult.AttackerTileId);
                    killCellMs += stopwatch.Elapsed.TotalMilliseconds - stepStartMs;
                    deathCount++;
                    livingCellCounts[owner.PlayerId]--;

                    // Putrefactive Mycotoxin: attribute in observer if needed
                    if (deathResult.Reason == DeathReason.PutrefactiveMycotoxin)
                    {
                        stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
                        AttributePutrefactiveMycotoxinKill(cell, board, players, simulationObserver);
                        putrefactiveAttributionMs += stopwatch.Elapsed.TotalMilliseconds - stepStartMs;
                    }

                    // Trigger spore on death, if relevant
                    stepStartMs = stopwatch.Elapsed.TotalMilliseconds;
                    board.TryTriggerSporeOnDeath(owner, rng, simulationObserver);
                    sporeTriggerMs += stopwatch.Elapsed.TotalMilliseconds - stepStartMs;
                    sporeTriggers++;
                }
                // NOTE: Cell aging is now handled in AgeCells method, not here
            }

            LogPhaseTiming($"DeathEngine: EvaluateProbabilisticDeaths visited {tilesVisited} tiles; revalidated away {tilesRevalidatedAway}; owner misses {missingOwners}; protected last-cell skips {protectedLastCellSkips}; rolls {deathRolls}; deaths {deathCount}; Saprophage saves {saprophageConsumedCount}; spore triggers {sporeTriggers}.");
            LogPhaseTiming($"DeathEngine: owner lookup {FormatElapsedMs(ownerLookupMs)} ms, CalculateDeathChance {FormatElapsedMs(calculateDeathChanceMs)} ms, KillFungalCell {FormatElapsedMs(killCellMs)} ms, Putrefactive attribution {FormatElapsedMs(putrefactiveAttributionMs)} ms, TryTriggerSporeOnDeath {FormatElapsedMs(sporeTriggerMs)} ms.");
            LogPhaseTiming($"DeathEngine: EvaluateProbabilisticDeaths total took {FormatElapsedMs(stopwatch.Elapsed.TotalMilliseconds)} ms.");
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

        // Define FT_PHASE_TIMING to re-enable these troubleshooting logs.
        [Conditional("FT_PHASE_TIMING")]
        private static void LogPhaseTiming(string message)
        {
            if (CoreLogger.Log == null)
            {
                return;
            }

            CoreLogger.Log($"[PhaseTiming] {message}");
        }

        private static string FormatElapsedMs(double elapsedMilliseconds)
            => elapsedMilliseconds.ToString("F1");
    }
}
