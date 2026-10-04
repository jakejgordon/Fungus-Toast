namespace FungusToast.Core.ContentProfiles
{
    /// <summary>
    /// A job a mutation or Mycovariant does on the board. Authoring metadata for the
    /// content-to-strategy coverage review; it never changes gameplay or AI behavior.
    /// Definitions and the rules for adding a value: docs/second-level/AI_CONTENT_TAGS.md.
    /// </summary>
    public enum ContentCapability
    {
        // Territory
        /// <summary>Raises ordinary growth chance with no board condition.</summary>
        BaseGrowth,
        /// <summary>Adds or strengthens diagonal (Tendril) growth.</summary>
        DiagonalGrowth,
        /// <summary>Growth bonus gated on a local board condition, which the profile names in Needs or Uses.</summary>
        ConditionalGrowth,
        /// <summary>Places living cells away from the colony's growing edge (lines, runners, jumps).</summary>
        RemotePlacement,
        /// <summary>Moves existing cells rather than adding new ones.</summary>
        Repositioning,

        // Survival
        /// <summary>Keeps own living cells alive through decay or age.</summary>
        DecayResistance,
        /// <summary>Makes own cells Resistant.</summary>
        ResistantCells,
        /// <summary>Clears toxins near own cells.</summary>
        ToxinCleanup,

        // Death and corpses
        /// <summary>Turns own dead cells back into living cells.</summary>
        SelfReclamation,
        /// <summary>Turns enemy dead cells or kills into own living cells.</summary>
        CorpseCapture,
        /// <summary>Removes own dead cells before enemies can reclaim them.</summary>
        CorpseDenial,
        /// <summary>Turns dead cells into nutrient patches.</summary>
        Composting,

        // Offense
        /// <summary>Puts new toxins on the board.</summary>
        ToxinPlacement,
        /// <summary>Makes own toxins last longer.</summary>
        ToxinLongevity,
        /// <summary>Moves own toxins toward enemies.</summary>
        ToxinMobility,
        /// <summary>Kills enemy living cells.</summary>
        DirectKill,
        /// <summary>Aims its effect at the strongest colony.</summary>
        LeaderFocus,

        // Economy
        /// <summary>Grants mutation points.</summary>
        PointIncome,
        /// <summary>Grants mutation levels without spending points.</summary>
        FreeUpgrades,
        /// <summary>Reshapes the colony's own mutation tree.</summary>
        TreePivot,
    }
}
