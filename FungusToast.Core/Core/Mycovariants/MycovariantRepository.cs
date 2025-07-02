using System.Collections.Generic;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Provides access to all defined Mycovariant objects in the game.
    /// </summary>
    public static class MycovariantRepository
    {
        // Backing field for lazy-initialized, cached list
        private static List<Mycovariant>? _all;

        /// <summary>
        /// Gets the canonical, immutable list of all mycovariants available in the game.
        /// This list is constructed once and then cached.
        /// </summary>
        public static List<Mycovariant> All => _all ??= BuildAll();

        /// <summary>
        /// Internal factory for building the canonical mycovariant list.
        /// Add all your mycovariant factories here.
        /// </summary>
        private static List<Mycovariant> BuildAll()
        {
            return new List<Mycovariant>
            {
                // Example mycovariants (replace/add as needed):
                MycovariantFactory.JettingMyceliumNorth(),
                MycovariantFactory.JettingMyceliumEast(),
                MycovariantFactory.JettingMyceliumSouth(),
                MycovariantFactory.JettingMyceliumWest(),

                // --- Universal/Fallback options ---
                MycovariantFactory.PlasmidBounty(),
                
                // --- Passive/Defensive options ---
                MycovariantFactory.NeutralizingMantle(),
                MycovariantFactory.MycelialBastion(),
                MycovariantFactory.SurgicalInoculation()
                // Add additional universal Mycovariants as created
            };
        }
    }
}
