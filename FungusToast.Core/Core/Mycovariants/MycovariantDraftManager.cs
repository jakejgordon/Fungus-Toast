using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantDraftManager
    {
        /// <summary>
        /// Runs the Mycovariant draft phase for all players, in the correct order.
        /// For AI, picks randomly. For human, calls the selectionCallback (if provided).
        /// </summary>
        /// <param name="players">All players in the game.</param>
        /// <param name="poolManager">The Mycovariant pool manager (should be initialized for this draft).</param>
        /// <param name="board">The game board.</param>
        /// <param name="rng">Random source.</param>
        /// <param name="choicesCount">How many choices per player (usually 3).</param>
        /// <param name="humanSelectionCallback">
        /// Optional: callback invoked for human selection, signature:
        /// (Player player, List&lt;Mycovariant&gt; choices) => Mycovariant (should return a picked one)
        /// </param>
        /// <param name="observer">Optional: Simulation observer for effect tracking/logging.</param>
        public static void RunDraft(
           List<Player> players,
           MycovariantPoolManager poolManager,
           GameBoard board,
           Random rng,
           int choicesCount = 3,
           Func<Player, List<Mycovariant>, Mycovariant>? humanSelectionCallback = null,
           ISimulationObserver? observer = null)
        {
            // Sort by fewest living cells (tiebreaker: lowest playerId)
            var draftOrder = players
                .OrderBy(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ThenBy(p => p.PlayerId)
                .ToList();

            foreach (var player in draftOrder)
            {
                var choices = GetDraftChoices(player, poolManager, choicesCount, rng);
                if (choices.Count == 0)
                    continue;

                Mycovariant picked;
                if (player.PlayerType == PlayerTypeEnum.Human && humanSelectionCallback != null)
                {
                    picked = humanSelectionCallback(player, choices);
                }
                else
                {
                    picked = choices[rng.Next(choices.Count)];
                }

                if (picked == null)
                    continue; // Defensive: can happen if callback returns null

                // Use encapsulated add
                player.AddMycovariant(picked);

                // Only remove from pool if not universal (i.e., not always available)
                if (!picked.IsUniversal)
                    poolManager.RemoveFromPool(picked);


                // Resolve instant/on-acquire effect
                var playerMyco = player.PlayerMycovariants.LastOrDefault(pm => pm.MycovariantId == picked.Id);
                if (playerMyco != null && picked.ApplyEffect != null)
                {
                    picked.ApplyEffect.Invoke(playerMyco, board, rng, observer);
                    playerMyco.MarkTriggered();
                }
                else if (playerMyco != null)
                {
                }
            }
        }


        /// <summary>
        /// Returns a random set of draft choices for a player, honoring pool eligibility and uniqueness.
        /// </summary>
        public static List<Mycovariant> GetDraftChoices(
            Player player,
            MycovariantPoolManager poolManager,
            int choicesCount,
            Random rng,
            int? forcedMycovariantId = null
        )
        {
            var eligible = poolManager.GetEligibleMycovariantsForPlayer(player);
            var shuffled = eligible.OrderBy(x => rng.Next()).ToList();
            var choices = shuffled.Take(choicesCount).ToList();

            // Only force for human player and if a forced ID is provided
            if (forcedMycovariantId.HasValue && player.PlayerType == PlayerTypeEnum.Human)
            {
                var forced = MycovariantRepository.All.FirstOrDefault(m => m.Id == forcedMycovariantId.Value);
                if (forced != null && !choices.Contains(forced))
                {
                    if (choices.Count < choicesCount)
                        choices.Add(forced);
                    else
                        choices[0] = forced;
                }
            }

            return choices;
        }

        /// <summary>
        /// Builds the set of draft-eligible mycovariants for the upcoming draft phase.
        /// By default, returns all unlocked and available mycovariants that are not already owned by any player.
        /// Customize here for round, rarity, or board-specific filtering.
        /// </summary>
        public static List<Mycovariant> BuildDraftPool(GameBoard board, List<Player> players)
        {
            // 1. Get all possible mycovariants (update repository accessor if needed)
            var allMycovariants = MycovariantRepository.All.ToList();

            // 2. Optionally, filter out mycovariants already drafted (if no duplicates allowed)
            var draftedIds = players
                .SelectMany(p => p.PlayerMycovariants)
                .Select(pm => pm.MycovariantId)
                .ToHashSet();

            // 3. Exclude any banned or scenario-restricted mycovariants (add logic if needed)
            // For now, assume no banned list, but this is where it would go.

            // 4. Optionally, filter by round/board state/unlocks here (customize as needed)
            // e.g. unlock special mycovariants after round 5, or based on board status
            // For now, we include all.

            // 5. Compose the eligible pool
            var draftPool = allMycovariants
                .Where(m => !draftedIds.Contains(m.Id))
                .ToList();

            return draftPool;
        }
    }
}
