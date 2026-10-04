namespace FungusToast.Core.ContentProfiles
{
    /// <summary>
    /// A board or game condition that content needs, uses, creates, or removes. Always read
    /// from the perspective of the colony that owns the content ("Own" is that colony).
    /// Definitions: docs/second-level/AI_CONTENT_TAGS.md.
    /// </summary>
    public enum BoardCondition
    {
        /// <summary>Own living cells dying (the event).</summary>
        OwnCellDeaths,
        /// <summary>Own dead cells on the board (the corpses).</summary>
        OwnDeadCells,
        /// <summary>Enemy dead cells on the board.</summary>
        EnemyDeadCells,
        /// <summary>Enemy living cells killed by this colony's own effects. Creating it also creates EnemyDeadCells.</summary>
        EnemyCellsKilledByYou,
        /// <summary>Own toxins on the board.</summary>
        OwnToxins,
        /// <summary>Enemy toxins near own cells.</summary>
        EnemyToxins,
        /// <summary>Own Resistant cells.</summary>
        OwnResistantCells,
        /// <summary>Enemy Resistant cells.</summary>
        EnemyResistantCells,
        /// <summary>Long borders with living enemy cells.</summary>
        EnemyContact,
        /// <summary>Room to branch around own cells.</summary>
        OpenSpace,
        /// <summary>Own cells with few open sides.</summary>
        CrampedSpace,
        /// <summary>Own territory near the crust or corners.</summary>
        BoardEdge,
        /// <summary>The effect scales with own colony size.</summary>
        LargeColony,
        /// <summary>Own colony trails the leaders.</summary>
        FallingBehind,
        /// <summary>Unspent mutation points carried between rounds.</summary>
        BankedPoints,
        /// <summary>Neutral nutrient patches on the board.</summary>
        NutrientPatches,
    }
}
