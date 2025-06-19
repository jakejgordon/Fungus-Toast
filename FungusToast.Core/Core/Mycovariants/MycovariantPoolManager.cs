using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public class MycovariantPoolManager
    {
        private List<Mycovariant> _availablePool = new();

        public void InitializePool(List<Mycovariant> all, Random rng)
        {
            _availablePool = all.OrderBy(_ => rng.Next()).ToList();
        }

        public List<Mycovariant> DrawChoices(int count)
        {
            var choices = _availablePool.Take(count).ToList();
            _availablePool.RemoveAll(m => choices.Contains(m));
            return choices;
        }

        public bool IsExhausted => !_availablePool.Any();
    }
}
