using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public class MycovariantPoolManager
    {
        private List<Mycovariant> _availablePool = new();
        private List<Mycovariant> _universalPool = new();
        private HashSet<int> _draftedNonUniversalIds = new(); // Track all non-universal mycovariants that have been drafted

        /// <summary>
        /// Initializes the pools. Call at the start of the draft phase.
        /// </summary>
        public void InitializePool(List<Mycovariant> all, Random rng)
        {
            _availablePool = all
                .Where(m => !m.IsUniversal)
                .OrderBy(_ => rng.Next())
                .ToList();

            _universalPool = all
                .Where(m => m.IsUniversal)
                .ToList();
        }

        /// <summary>
        /// Draws up to <paramref name="count"/> choices from the available pool, filling with universals if needed.
        /// Choices are removed from the pool when drawn.
        /// </summary>
        public List<Mycovariant> DrawChoices(int count, Random rng)
        {
            var choices = _availablePool.Take(count).ToList();
            _availablePool.RemoveAll(m => choices.Contains(m));

            if (choices.Count < count)
            {
                // Shuffle universal pool using provided rng
                var universals = _universalPool
                    .OrderBy(_ => rng.Next())
                    .Take(count - choices.Count)
                    .ToList();
                choices.AddRange(universals);
            }

            return choices;
        }

        /// <summary>
        /// Returns all mycovariants currently available for drafting by the player.
        /// By default, excludes any already owned by the player and any non-universal mycovariants already drafted by any player.
        /// </summary>
        public List<Mycovariant> GetEligibleMycovariantsForPlayer(Player player)
        {
            // Exclude mycovariants already owned by this player (prevent duplicate picks)
            var ownedIds = new HashSet<int>(player.PlayerMycovariants.Select(pm => pm.MycovariantId));

            // All available (unique, not drafted) + all universals (which can be drafted multiple times)
            var eligible = new List<Mycovariant>();

            // Non-universal mycovariants: exclude those already owned by this player OR already drafted by any player
            eligible.AddRange(_availablePool.Where(m => 
                !ownedIds.Contains(m.Id) && 
                !_draftedNonUniversalIds.Contains(m.Id)));

            // Universals: can be drafted by everyone, but still avoid duplicates on the same player
            eligible.AddRange(_universalPool.Where(m => !ownedIds.Contains(m.Id)));

            return eligible;
        }

        /// <summary>
        /// Removes a drafted mycovariant from the available pool if it is not universal.
        /// Universal mycovariants remain available to all players.
        /// </summary>
        public void RemoveFromPool(Mycovariant picked)
        {
            if (picked.IsUniversal)
                return; // Don't remove universals, still available for others

            _availablePool.RemoveAll(m => m.Id == picked.Id);
            _draftedNonUniversalIds.Add(picked.Id); // Track that this non-universal has been drafted
            // If you have additional pools (e.g., by rarity/type), remove from them as needed
        }

        public bool IsExhausted => !_availablePool.Any();
    }

}
