using System.Collections.Generic;
using System.Linq;

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
                MycovariantFactory.PlasmidBountyII(),
                MycovariantFactory.PlasmidBountyIII(),
                
                // --- Passive/Defensive options ---
                MycovariantFactory.NeutralizingMantle(),
                MycovariantFactory.MycelialBastionI(),
                MycovariantFactory.MycelialBastionII(),
                MycovariantFactory.MycelialBastionIII(),
                MycovariantFactory.SurgicalInoculation(),

                // --- Growth/Edge options ---
                MycovariantFactory.PerimeterProliferator(),
                
                // --- Resistance/Defense options ---
                MycovariantFactory.HyphalResistanceTransfer(),

                MycovariantFactory.EnduringToxaphores(),
                
                // --- Reclamation/Recovery options ---
                MycovariantFactory.ReclamationRhizomorphs(),
            };
        }

        public static Mycovariant GetById(int id) => All.First(m => m.Id == id);
    }
}
