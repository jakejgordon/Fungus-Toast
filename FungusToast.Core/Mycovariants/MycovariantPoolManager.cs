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
        private List<Mycovariant> _temporarilyRemovedMycovariants = new(); // Track temporarily removed mycovariants for testing

        /// <summary>
        /// Initializes the pools. Call at the start of the draft phase.
        /// Important: This should only be called once per game, not per draft round.
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
                
            // Do NOT clear _draftedNonUniversalIds here - it should persist across draft rounds
            
            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[PoolManager] Initialized pool with {_availablePool.Count} non-universal and {_universalPool.Count} universal mycovariants");
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
            // Exclude mycovariants already owned by this player (prevent duplicate picks for non-universal only)
            var ownedIds = new HashSet<int>(player.PlayerMycovariants.Select(pm => pm.MycovariantId));

            // All available (unique, not drafted) + all universals (which can be drafted multiple times)
            var eligible = new List<Mycovariant>();

            // Non-universal mycovariants: exclude those already owned by this player OR already drafted by any player
            var availableNonUniversal = _availablePool.Where(m => 
                !ownedIds.Contains(m.Id) && 
                !_draftedNonUniversalIds.Contains(m.Id)).ToList();
            eligible.AddRange(availableNonUniversal);

            // Universals: ALWAYS available to everyone (players can draft the same universal multiple times)
            // This ensures we always have at least 3 universal mycovariants available for replacement
            eligible.AddRange(_universalPool);

            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Player {player.PlayerId} eligible: {availableNonUniversal.Count} non-universal + {_universalPool.Count} universal = {eligible.Count} total");

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
                // Universal mycovariants are never removed from the pool and can be drafted by all players
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Universal mycovariant '{picked.Name}' (ID: {picked.Id}) drafted but remains in pool");
                return; // Don't remove from universal pool, still available for future drafts
            }

            _availablePool.RemoveAll(m => m.Id == picked.Id);
            _draftedNonUniversalIds.Add(picked.Id); // Track that this non-universal has been drafted
            
            FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Non-universal mycovariant '{picked.Name}' (ID: {picked.Id}) drafted and permanently removed from pool");
            
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
            // Get all unique mycovariants that weren't drafted by ANY player in ANY previous draft
            var undraftedUnique = allMycovariants
                .Where(m => !m.IsUniversal && !_draftedNonUniversalIds.Contains(m.Id))
                .ToList();

            // Only add back mycovariants that are not already in the available pool
            var currentPoolIds = new HashSet<int>(_availablePool.Select(m => m.Id));
            var toAdd = undraftedUnique.Where(m => !currentPoolIds.Contains(m.Id)).ToList();

            // Add them back to the available pool
            _availablePool.AddRange(toAdd);

            // Shuffle the pool to randomize the order for future drafts
            _availablePool = _availablePool.OrderBy(_ => rng.Next()).ToList();
        }

        /// <summary>
        /// Temporarily removes a mycovariant from the available pool (for testing mode).
        /// The mycovariant can be restored later with RestoreToPool.
        /// </summary>
        public void TemporarilyRemoveFromPool(int mycovariantId)
        {
            var toRemove = _availablePool.Where(m => m.Id == mycovariantId).ToList();
            foreach (var mycovariant in toRemove)
            {
                _availablePool.Remove(mycovariant);
                _temporarilyRemovedMycovariants.Add(mycovariant);
                FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Temporarily removed '{mycovariant.Name}' (ID: {mycovariantId}) from pool");
            }
        }

        /// <summary>
        /// Restores a temporarily removed mycovariant back to the available pool (for testing mode).
        /// </summary>
        public void RestoreToPool(int mycovariantId)
        {
            var toRestore = _temporarilyRemovedMycovariants.Where(m => m.Id == mycovariantId).ToList();
            foreach (var mycovariant in toRestore)
            {
                _temporarilyRemovedMycovariants.Remove(mycovariant);
                // Only restore if it hasn't been permanently drafted
                if (!_draftedNonUniversalIds.Contains(mycovariantId))
                {
                    _availablePool.Add(mycovariant);
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Restored '{mycovariant.Name}' (ID: {mycovariantId}) to pool");
                }
                else
                {
                    FungusToast.Core.Logging.CoreLogger.Log?.Invoke($"[Pool] Cannot restore '{mycovariant.Name}' (ID: {mycovariantId}) - it was permanently drafted");
                }
            }
        }

        /// <summary>
        /// Gets a summary of the current pool state for debugging/logging.
        /// </summary>
        public string GetPoolSummary()
        {
            return $"Available: {_availablePool.Count}, Universal: {_universalPool.Count}, Drafted: {_draftedNonUniversalIds.Count}, TempRemoved: {_temporarilyRemovedMycovariants.Count}";
        }
    }

}
