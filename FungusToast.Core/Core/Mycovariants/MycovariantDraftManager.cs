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
        /// <param name="pool">The Mycovariant pool manager (should be initialized for this draft).</param>
        /// <param name="board">The game board.</param>
        /// <param name="rng">Random source.</param>
        /// <param name="humanSelectionCallback">
        /// Optional: callback invoked for human selection, signature:
        /// (Player player, List&lt;Mycovariant&gt; choices) => Mycovariant (should return a picked one)
        /// </param>
        public static void RunDraft(
            List<Player> players,
            MycovariantPoolManager pool,
            GameBoard board,
            Random rng,
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
                var choices = pool.DrawChoices(3);

                Mycovariant? picked = null;

                if (player.PlayerType == PlayerTypeEnum.Human && humanSelectionCallback != null)
                {
                    // Defer to UI/callback for human
                    picked = humanSelectionCallback(player, choices);
                }
                else
                {
                    // AI: pick randomly
                    picked = choices[rng.Next(choices.Count)];
                }

                if (picked != null)
                {
                    // Attach the mycovariant to the player
                    var playerMyco = new PlayerMycovariant(player.PlayerId, picked.Id, picked);
                    player.Mycovariants.Add(playerMyco);

                    // If effect is instant, resolve now
                    picked.ApplyEffect?.Invoke(playerMyco, board, rng, observer);
                }
            }
        }
    }
}
