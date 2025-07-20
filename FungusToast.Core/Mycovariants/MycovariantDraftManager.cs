using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using FungusToast.Core.AI;
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
                    // AI selection - check if player has a mutation strategy with mycovariant preferences
                    if (player.MutationStrategy is ParameterizedSpendingStrategy paramStrategy)
                    {
                        picked = paramStrategy.SelectMycovariantFromChoices(player, choices, board);
                    }
                    else
                    {
                        // Fallback to random selection for other AI strategies
                        picked = choices[rng.Next(choices.Count)];
                    }
                }

                if (picked == null)
                    continue; // Defensive: can happen if callback returns null

                // Use encapsulated add
                player.AddMycovariant(picked);

                // Set AIScoreAtDraft for AI picks
                if (player.PlayerType == PlayerTypeEnum.AI)
                {
                    var aiPlayerMyco = player.PlayerMycovariants.LastOrDefault(pm => pm.MycovariantId == picked.Id);
                    if (aiPlayerMyco != null)
                    {
                        float score = picked.GetAIScore(player, board);
                        aiPlayerMyco.AIScoreAtDraft = score;
                    }
                }

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

            // Return undrafted unique mycovariants to the pool for future drafts
            var allMycovariants = MycovariantRepository.All;
            poolManager.ReturnUndraftedToPool(allMycovariants, rng);
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
            
            // Debug logging to help diagnose pool issues
            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Draft] Player {player.PlayerId} has {eligible.Count} eligible mycovariants. Pool summary: {poolManager.GetPoolSummary()}");
            
            // Ensure uniqueness by grouping by ID and taking only one of each
            var uniqueEligible = eligible.GroupBy(m => m.Id).Select(g => g.First()).ToList();
            
            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Draft] After uniqueness filter: {uniqueEligible.Count} unique eligible mycovariants");
            
            var shuffled = uniqueEligible.OrderBy(x => rng.Next()).ToList();
            var choices = shuffled.Take(choicesCount).ToList();

            // Only force for human player and if a forced ID is provided
            if (forcedMycovariantId.HasValue && player.PlayerType == PlayerTypeEnum.Human)
            {
                // First check if the forced mycovariant is actually eligible from the pool
                var forced = uniqueEligible.FirstOrDefault(m => m.Id == forcedMycovariantId.Value);
                
                if (forced != null && !choices.Contains(forced))
                {
                    // The testing mycovariant is eligible and not already in choices, add it
                    if (choices.Count < choicesCount)
                        choices.Add(forced);
                    else
                        choices[0] = forced;
                        
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Draft] Forced testing mycovariant '{forced.Name}' (ID: {forcedMycovariantId.Value}) into choices for human player");
                }
                else if (forced == null)
                {
                    // The testing mycovariant is not eligible (was drafted/removed), log this
                    var testingMycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == forcedMycovariantId.Value);
                    string mycovariantName = testingMycovariant?.Name ?? "Unknown";
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Draft] Testing mycovariant '{mycovariantName}' (ID: {forcedMycovariantId.Value}) is not eligible - was it already drafted by someone else?");
                }
            }

            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Draft] Final choices for player {player.PlayerId}: [{string.Join(", ", choices.Select(c => c.Name))}]");

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
