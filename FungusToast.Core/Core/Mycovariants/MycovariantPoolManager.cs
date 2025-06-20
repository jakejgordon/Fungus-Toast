using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public class MycovariantPoolManager
    {
        private List<Mycovariant> _availablePool = new();
        private List<Mycovariant> _universalPool = new();

        public void InitializePool(List<Mycovariant> all, Random rng)
        {
            _availablePool = all
                .Where(m => !m.IsUniversal)
                .OrderBy(_ => rng.Next())
                .ToList();

            _universalPool = all
                .Where(m => m.IsUniversal)
                .ToList(); // No shuffle—universal choices are always the same for all players
        }

        public List<Mycovariant> DrawChoices(int count)
        {
            var choices = _availablePool.Take(count).ToList();

            // Remove selected unique ones from the pool (but not universals)
            _availablePool.RemoveAll(m => choices.Contains(m));

            // Fill with universal if needed
            if (choices.Count < count)
            {
                // You may want to randomize which universals are shown if there are several
                var universals = _universalPool
                    .OrderBy(_ => Guid.NewGuid()) // Shuffle
                    .Take(count - choices.Count)
                    .ToList();
                choices.AddRange(universals);
            }

            return choices;
        }

        public bool IsExhausted => !_availablePool.Any();
    }
}
