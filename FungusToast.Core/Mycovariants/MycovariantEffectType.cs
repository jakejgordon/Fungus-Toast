namespace FungusToast.Core.Mycovariants
{
    public enum MycovariantEffectType
    {
        Infested,
        Reclaimed,
        CatabolicGrowth,
        Colonized,
        Poisoned,
        Toxified,
        MpBonus,
        Neutralized,
        Neutralizations,
        Bastioned,
        ResistantCellPlaced,
        Drops,
        PerimeterProliferation,
        FortifiedCells,
        ResistantDrops,
        ResistantTransfers,
        ExtendedCycles,
        ExistingExtensions,
        SecondReclamationAttempts,
        BallistosporeDischarge,
        NecrophoricAdaptationReclamations, // Reclamations triggered by NecrophoricAdaptation
        CytolyticBurstToxins, // Toxins created by Cytolytic Burst
        CytolyticBurstKills, // Cells killed by Cytolytic Burst toxins
        Relocations, // Toxins relocated by Chemotactic Mycotoxins
        // Corner Conduit specific counts
        CornerConduitInfestations,
        CornerConduitColonizations,
        CornerConduitReclaims,
        CornerConduitToxinsReplaced,
        // Aggressotropic Conduit (enemy-tracking conduit) specific counts
        AggressotropicConduitInfestations,
        AggressotropicConduitColonizations,
        AggressotropicConduitReclaims,
        AggressotropicConduitToxinsReplaced,
        AggressotropicConduitResistantPlacements,
        // Add others as needed
    }
}
