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
        [Obsolete("Use GetAll() and filter by Id instead.")] public static Mycovariant JettingMycelium() { return JettingMyceliumI(); }
        [Obsolete("Use GetAll() and filter by Id instead.")] public static Mycovariant JettingMyceliumI() { return DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumIId); }
        [Obsolete("Use GetAll() and filter by Id instead.")] public static Mycovariant JettingMyceliumII() { return DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumIIId); }
        [Obsolete("Use GetAll() and filter by Id instead.")] public static Mycovariant JettingMyceliumIII() { return DirectionalMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.JettingMyceliumIIIId); }
        [Obsolete] public static Mycovariant JettingMyceliumNorth() { return JettingMyceliumI(); }
        [Obsolete] public static Mycovariant JettingMyceliumEast() { return JettingMyceliumI(); }
        [Obsolete] public static Mycovariant JettingMyceliumSouth() { return JettingMyceliumI(); }
        [Obsolete] public static Mycovariant JettingMyceliumWest() { return JettingMyceliumI(); }

        // Economy
        [Obsolete] public static Mycovariant PlasmidBounty() { return EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyId); }
        [Obsolete] public static Mycovariant PlasmidBountyII() { return EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyIIId); }
        [Obsolete] public static Mycovariant PlasmidBountyIII() { return EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PlasmidBountyIIIId); }
        [Obsolete] public static Mycovariant AscusWager() { return EconomyMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AscusWagerId); }

        // Reclamation
        [Obsolete] public static Mycovariant NecrophoricAdaptation() { return ReclamationMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.NecrophoricAdaptation); }
        [Obsolete] public static Mycovariant ReclamationRhizomorphs() { return ReclamationMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.ReclamationRhizomorphsId); }

        // Fungicide / Defense
        [Obsolete] public static Mycovariant NeutralizingMantle() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.NeutralizingMantleId); }
        [Obsolete] public static Mycovariant EnduringToxaphores() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.EnduringToxaphoresId); }
        [Obsolete] public static Mycovariant BallistosporeDischargeI() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIId); }
        [Obsolete] public static Mycovariant BallistosporeDischargeII() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIIId); }
        [Obsolete] public static Mycovariant BallistosporeDischargeIII() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.BallistosporeDischargeIIIId); }
        [Obsolete] public static Mycovariant CytolyticBurst() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CytolyticBurstId); }
        [Obsolete] public static Mycovariant ChemotacticMycotoxins() { return FungicideMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.ChemotacticMycotoxinsId); }

        // Resistance / Growth hybrid
        [Obsolete] public static Mycovariant MycelialBastionI() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIId); }
        [Obsolete] public static Mycovariant MycelialBastionII() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIIId); }
        [Obsolete] public static Mycovariant MycelialBastionIII() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.MycelialBastionIIIId); }
        [Obsolete] public static Mycovariant SurgicalInoculation() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.SurgicalInoculationId); }
        [Obsolete] public static Mycovariant HyphalResistanceTransfer() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.HyphalResistanceTransferId); }
        [Obsolete] public static Mycovariant SeptalAlarm() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.SeptalAlarmId); }
        [Obsolete] public static Mycovariant AggressotropicConduitI() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIId); }
        [Obsolete] public static Mycovariant AggressotropicConduitII() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIIId); }
        [Obsolete] public static Mycovariant AggressotropicConduitIII() { return ResistanceMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.AggressotropicConduitIIIId); }

        // Growth
        [Obsolete] public static Mycovariant PerimeterProliferator() { return GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.PerimeterProliferatorId); }
        [Obsolete] public static Mycovariant CornerConduitI() { return GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIId); }
        [Obsolete] public static Mycovariant CornerConduitII() { return GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIIId); }
        [Obsolete] public static Mycovariant CornerConduitIII() { return GrowthMycovariantFactory.CreateAll().First(m => m.Id == MycovariantIds.CornerConduitIIIId); }
    }
}
