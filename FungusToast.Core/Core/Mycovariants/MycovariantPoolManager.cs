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
        private HashSet<int> _currentDraftUniversalIds = new(); // Track universal mycovariants offered in current draft round

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

            // Clear the current draft tracking for universal mycovariants
            _currentDraftUniversalIds.Clear();
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

            // Universals: can be drafted by everyone, but avoid duplicates on the same player AND avoid duplicates in current draft
            eligible.AddRange(_universalPool.Where(m => 
                !ownedIds.Contains(m.Id) && 
                !_currentDraftUniversalIds.Contains(m.Id)));

            return eligible;
        }

        /// <summary>
        /// Removes a drafted mycovariant from the available pool if it is not universal.
        /// Universal mycovariants remain available to all players.
        /// </summary>
        public void RemoveFromPool(Mycovariant picked)
        {
            if (picked.IsUniversal)
            {
                // Track that this universal mycovariant is being offered in the current draft
                _currentDraftUniversalIds.Add(picked.Id);
                return; // Don't remove from universal pool, still available for future drafts
            }

            _availablePool.RemoveAll(m => m.Id == picked.Id);
            _draftedNonUniversalIds.Add(picked.Id); // Track that this non-universal has been drafted
            // If you have additional pools (e.g., by rarity/type), remove from them as needed
        }

        public bool IsExhausted => !_availablePool.Any();

        /// <summary>
        /// Returns undrafted unique mycovariants to the pool for future drafts.
        /// Call this at the end of each draft phase to ensure undrafted unique mycovariants
        /// are available in subsequent drafts.
        /// </summary>
        /// <param name="allMycovariants">All possible mycovariants in the game</param>
        /// <param name="rng">Random source for shuffling</param>
        public void ReturnUndraftedToPool(List<Mycovariant> allMycovariants, Random rng)
        {
            // Get all unique mycovariants that weren't drafted
            var undraftedUnique = allMycovariants
                .Where(m => !m.IsUniversal && !_draftedNonUniversalIds.Contains(m.Id))
                .ToList();

            // Add them back to the available pool
            _availablePool.AddRange(undraftedUnique);

            // Shuffle the pool to randomize the order for future drafts
            _availablePool = _availablePool.OrderBy(_ => rng.Next()).ToList();
        }

        /// <summary>
        /// Gets a summary of the current pool state for debugging/logging.
        /// </summary>
        public string GetPoolSummary()
        {
            return $"Available: {_availablePool.Count}, Universal: {_universalPool.Count}, Drafted: {_draftedNonUniversalIds.Count}, Current Draft Universals: {_currentDraftUniversalIds.Count}";
        }

        /// <summary>
        /// Tracks a mycovariant as being offered in the current draft without removing it from the pool.
        /// This prevents duplicates within the same draft round.
        /// </summary>
        public void TrackAsOffered(Mycovariant mycovariant)
        {
            if (mycovariant.IsUniversal)
            {
                _currentDraftUniversalIds.Add(mycovariant.Id);
            }
            else
            {
                // For non-universal, we don't need to track separately since they're removed from pool
                // But we can add defensive tracking if needed
            }
        }
    }

}
