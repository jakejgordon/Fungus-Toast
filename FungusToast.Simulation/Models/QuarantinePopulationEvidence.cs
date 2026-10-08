namespace FungusToast.Simulation.Models;

/// <summary>Observation only: no replacement gameplay rules or RNG consumption.</summary>
public sealed class QuarantinePopulationEvidence
{
    public int PocketTileCount { get; init; }
    public int CorridorTileCount { get; init; }
    public int RotTileCount { get; init; }
    public int CorridorOrthogonalDistance { get; init; }
    public int CorridorDiagonalDistance { get; init; }
    public int? FirstPocketEntryRound { get; set; }
    public int? HalfPocketOccupiedRound { get; set; }
    public List<QuarantinePopulationSample> Samples { get; } = new();
}

public sealed class QuarantinePopulationSample
{
    public int Round { get; init; }
    public int PocketOccupiedTiles { get; init; }
    public int PocketLivingTiles { get; init; }
    public int CorridorOccupiedTiles { get; init; }
    public Dictionary<int,int> PocketLivingByPlayer { get; init; } = new();
}
