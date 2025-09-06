using System;

namespace FungusToast.Core.Growth
{
    /// <summary>
    /// Represents the source/reason why a fungal cell was created or became alive.
    /// This complements DeathReason by tracking the origin of living cells.
    /// </summary>
    public enum GrowthSource
    {
        /// <summary>
        /// Initial spore placement at game start
        /// </summary>
        InitialSpore,
        
        /// <summary>
        /// Normal orthogonal growth (cardinal directions)
        /// </summary>
        HyphalOutgrowth,
        
        /// <summary>
        /// Diagonal growth from tendril mutations
        /// </summary>
        TendrilOutgrowth,
        
        /// <summary>
        /// Reclaimed own dead cell via Regenerative Hyphae
        /// </summary>
        RegenerativeHyphae,
        
        /// <summary>
        /// Converted from enemy death via Necrotoxic Conversion
        /// </summary>
        NecrotoxicConversion,
        
        /// <summary>
        /// Temporary surge growth from Hyphal Surge
        /// </summary>
        HyphalSurge,
        
        /// <summary>
        /// Growth from Jetting Mycelium mycovariant
        /// </summary>
        JettingMycelium,
        
        /// <summary>
        /// Growth from Hyphal Vectoring surge
        /// </summary>
        HyphalVectoring,
        
        /// <summary>
        /// Growth from Surgical Inoculation mycovariant
        /// </summary>
        SurgicalInoculation,
        
        /// <summary>
        /// Spore-on-death growth from Necrosporulation
        /// </summary>
        Necrosporulation,
        
        /// <summary>
        /// Reclaimed from Necrophytic Bloom effect
        /// </summary>
        NecrophyticBloom,
        
        /// <summary>
        /// Infiltrated enemy dead cell via Necrohyphal Infiltration
        /// </summary>
        NecrohyphalInfiltration,
        
        /// <summary>
        /// Moved via Creeping Mold mutation
        /// </summary>
        CreepingMold,
        
        /// <summary>
        /// Resurrection from Catabolic Rebirth
        /// </summary>
        CatabolicRebirth,
        
        /// <summary>
        /// Spore drop from Ballistospore Discharge mycovariant
        /// </summary>
        Ballistospore,
        
        /// <summary>
        /// Spore drop from Mycotoxin Tracer mutation
        /// </summary>
        MycotoxinTracer,
        
        /// <summary>
        /// Manual placement (editor/testing)
        /// </summary>
        Manual,
        
        /// <summary>
        /// Unknown or legacy source
        /// </summary>
        Unknown,
        SporicidalBloom,
        
        /// <summary>
        /// Placed via Mimetic Resilience surge
        /// </summary>
        MimeticResilience,
        
        /// <summary>
        /// Toxin created from Cytolytic Burst mycovariant
        /// </summary>
        CytolyticBurst,
        
        /// <summary>
        /// Toxin created from Putrefactive Cascade mutation
        /// </summary>
        PutrefactiveCascade,
        
        /// <summary>
        /// Resistance granted via Chitin Fortification surge
        /// </summary>
        ChitinFortification
    }
}