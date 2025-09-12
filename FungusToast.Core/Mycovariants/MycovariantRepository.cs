using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Provides access to all defined Mycovariant objects in the game.
    /// </summary>
    public static class MycovariantRepository
    {
        private static List<Mycovariant>? _all;
        public static List<Mycovariant> All => _all ??= BuildAll();

        private static List<Mycovariant> BuildAll()
        {
            // Delegate construction to the refactored categorized factories
            return MycovariantFactory.GetAll().ToList();
        }

        public static Mycovariant GetById(int id) => All.First(m => m.Id == id);
    }
}
