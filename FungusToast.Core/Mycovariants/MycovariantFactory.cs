using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Aggregator facade for category-specific mycovariant factories.
    /// Provides backward-compatible method forwarders (marked obsolete) while
    /// encouraging consumers to enumerate via GetAll / repository.
    /// </summary>
    public static class MycovariantFactory
    {
        public static IEnumerable<Mycovariant> GetAll()
        {
            return DirectionalMycovariantFactory.CreateAll()
                .Concat(EconomyMycovariantFactory.CreateAll())
                .Concat(ResistanceMycovariantFactory.CreateAll())
                .Concat(GrowthMycovariantFactory.CreateAll())
                .Concat(FungicideMycovariantFactory.CreateAll())
                .Concat(ReclamationMycovariantFactory.CreateAll());
        }

        // Directional
        [Obsolete("Use GetAll() and filter by Id instead.")] public static Mycovariant JettingMyceliumNorth() => DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumNorthId);
        [Obsolete] public static Mycovariant JettingMyceliumEast() => DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumEastId);
        [Obsolete] public static Mycovariant JettingMyceliumSouth() => DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumSouthId);
        [Obsolete] public static Mycovariant JettingMyceliumWest() => DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumWestId);

        // Economy
        [Obsolete] public static Mycovariant PlasmidBounty() => EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyId);
        [Obsolete] public static Mycovariant PlasmidBountyII() => EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyIIId);
        [Obsolete] public static Mycovariant PlasmidBountyIII() => EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyIIIId);

        // Reclamation
        [Obsolete] public static Mycovariant NecrophoricAdaptation() => ReclamationMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.NecrophoricAdaptation);
        [Obsolete] public static Mycovariant ReclamationRhizomorphs() => ReclamationMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.ReclamationRhizomorphsId);

        // Fungicide / Defense
        [Obsolete] public static Mycovariant NeutralizingMantle() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.NeutralizingMantleId);
        [Obsolete] public static Mycovariant EnduringToxaphores() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.EnduringToxaphoresId);
        [Obsolete] public static Mycovariant BallistosporeDischargeI() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIId);
        [Obsolete] public static Mycovariant BallistosporeDischargeII() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIIId);
        [Obsolete] public static Mycovariant BallistosporeDischargeIII() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIIIId);
        [Obsolete] public static Mycovariant CytolyticBurst() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CytolyticBurstId);
        [Obsolete] public static Mycovariant ChemotacticMycotoxins() => FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.ChemotacticMycotoxinsId);

        // Resistance / Growth hybrid
        [Obsolete] public static Mycovariant MycelialBastionI() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIId);
        [Obsolete] public static Mycovariant MycelialBastionII() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIIId);
        [Obsolete] public static Mycovariant MycelialBastionIII() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIIIId);
        [Obsolete] public static Mycovariant SurgicalInoculation() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.SurgicalInoculationId);
        [Obsolete] public static Mycovariant HyphalResistanceTransfer() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.HyphalResistanceTransferId);
        [Obsolete] public static Mycovariant AggressotropicConduitI() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIId);
        [Obsolete] public static Mycovariant AggressotropicConduitII() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIIId);
        [Obsolete] public static Mycovariant AggressotropicConduitIII() => ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIIIId);

        // Growth
        [Obsolete] public static Mycovariant PerimeterProliferator() => GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PerimeterProliferatorId);
        [Obsolete] public static Mycovariant CornerConduitI() => GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIId);
        [Obsolete] public static Mycovariant CornerConduitII() => GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIIId);
        [Obsolete] public static Mycovariant CornerConduitIII() => GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIIIId);
    }
}
