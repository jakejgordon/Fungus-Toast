namespace FungusToast.Core.Events
{
    /// <summary>
    /// Canonical enumeration of per-cell board events used for aggregation in player-centric game logs.
    /// Limited to growth / takeover and toxin placement transitions that are summarized per phase.
    /// </summary>
    public enum CellEventKind
    {
        Colonized,
        Infested,
        Reclaimed,
        Overgrown,
        Toxified,
        Poisoned
    }
}
