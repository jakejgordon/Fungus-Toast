using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Provides centralized accessors for commonly reused Mycovariant synergy groupings.
    /// Helps keep MycovariantFactory cleaner and avoids manual list duplication.
    /// </summary>
    public static class MycovariantSynergyListFactory
    {
        // ----------------- Resistance Group -----------------
        private static readonly List<int> ResistanceGroup = new()
        {
            MycovariantIds.MycelialBastionIId,
            MycovariantIds.MycelialBastionIIId,
            MycovariantIds.MycelialBastionIIIId,
            MycovariantIds.HyphalResistanceTransferId,
            MycovariantIds.SurgicalInoculationId,
            // Aggressotropic Conduit tiers (last cell becomes Resistant)
            MycovariantIds.AggressotropicConduitIId,
            MycovariantIds.AggressotropicConduitIIId,
            MycovariantIds.AggressotropicConduitIIIId
        };

        /// <summary>
        /// Returns all resistance-related mycovariant IDs.
        /// </summary>
        public static List<int> GetResistanceSynergyMycovariantIds() => new List<int>(ResistanceGroup);

        /// <summary>
        /// Returns all resistance-related mycovariant IDs except the provided one (avoids self-synergy entries).
        /// </summary>
        public static List<int> GetResistanceSynergyMycovariantIdsExcluding(int excludeId) =>
            ResistanceGroup.Where(id => id != excludeId).ToList();

        // ----------------- Reclamation Group -----------------
        private static readonly List<int> ReclamationGroup = new()
        {
            MycovariantIds.NecrophoricAdaptation,
            MycovariantIds.ReclamationRhizomorphsId
        };

        public static List<int> GetReclamationSynergyMycovariantIds() => new List<int>(ReclamationGroup);
        public static List<int> GetReclamationSynergyMycovariantIdsExcluding(int excludeId) =>
            ReclamationGroup.Where(id => id != excludeId).ToList();

        // ----------------- Toxin (Enhancement / Persistence / Mobility) Group -----------------
        // Focuses on toxin longevity / relocation – core enablers other toxin deployers commonly reference.
        private static readonly List<int> ToxinEnhancementGroup = new()
        {
            MycovariantIds.ChemotacticMycotoxinsId,
            MycovariantIds.EnduringToxaphoresId
        };

        public static List<int> GetToxinSynergyMycovariantIds() => new List<int>(ToxinEnhancementGroup);
        public static List<int> GetToxinSynergyMycovariantIdsExcluding(int excludeId) =>
            ToxinEnhancementGroup.Where(id => id != excludeId).ToList();
    }
}
