using FungusToast.Core.AI;
using FungusToast.Simulation.Models;

namespace FungusToast.Simulation.Experiments;

/// <summary>
/// Versioned, machine-readable description of the conditions requested for an experiment.
/// This is the input contract; later phases will add the fully resolved evidence manifest.
/// </summary>
public sealed class ExperimentManifest
{
    public const string CurrentSchemaVersion = "fungus-toast.experiment-input.v1";
    public const int MaximumGamesPerCondition = 100;

    public required string SchemaVersion { get; init; }
    public required string ExperimentId { get; init; }
    public required string Purpose { get; init; }
    public required int GamesPerCondition { get; init; }
    public required int BaseSeed { get; init; }
    public required IReadOnlyList<ExperimentCondition> Conditions { get; init; }
}

public sealed record ExperimentCondition
{
    public required string ConditionId { get; init; }
    public required int PlayerCount { get; init; }
    public required ExperimentBoard Board { get; init; }
    public required ExperimentStrategySelection Strategies { get; init; }
    public required ExperimentSystems Systems { get; init; }
    public required ExperimentPositioning Positioning { get; init; }
    public required SlotAssignmentPolicy SlotAssignmentPolicy { get; init; }
}

public sealed class ExperimentBoard
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public string GeometryId { get; init; } = "rectangle";
    public IReadOnlyList<int> BlockedTileIds { get; init; } = Array.Empty<int>();
}

public sealed class ExperimentStrategySelection
{
    public required StrategySetEnum StrategySet { get; init; }
    public required StrategySelectionPolicy SelectionPolicy { get; init; }
    public IReadOnlyList<string> ExplicitStrategyNames { get; init; } = Array.Empty<string>();
    public ExperimentStrategyFilter Filter { get; init; } = new();
}

public sealed class ExperimentStrategyFilter
{
    public IReadOnlyList<StrategyArchetype> Archetypes { get; init; } = Array.Empty<StrategyArchetype>();
    public IReadOnlyList<StrategyPowerTier> PowerTiers { get; init; } = Array.Empty<StrategyPowerTier>();
    public IReadOnlyList<StrategyRole> Roles { get; init; } = Array.Empty<StrategyRole>();
    public IReadOnlyList<StrategyLifecycle> Lifecycles { get; init; } = Array.Empty<StrategyLifecycle>();
    public IReadOnlyList<DifficultyBand> DifficultyBands { get; init; } = Array.Empty<DifficultyBand>();
    public IReadOnlyList<CampaignDifficulty> CampaignDifficulties { get; init; } = Array.Empty<CampaignDifficulty>();
    public IReadOnlyList<StrategyPool> Pools { get; init; } = Array.Empty<StrategyPool>();
}

public sealed class ExperimentSystems
{
    public required bool NutrientPatchesEnabled { get; init; }
    public required bool MycovariantDraftEnabled { get; init; }
    public IReadOnlyList<PlayerStartingAdaptations> StartingAdaptations { get; init; } = Array.Empty<PlayerStartingAdaptations>();
}

public sealed class PlayerStartingAdaptations
{
    public required int PlayerSlot { get; init; }
    public required IReadOnlyList<string> AdaptationIds { get; init; }
}

public sealed class ExperimentPositioning
{
    public IReadOnlyList<BoardCoordinate> ExactStartingPositions { get; init; } = Array.Empty<BoardCoordinate>();
    public IReadOnlyList<PlayerStartingPositionPool> PreferredPositionPools { get; init; } = Array.Empty<PlayerStartingPositionPool>();
}

public sealed class PlayerStartingPositionPool
{
    public required int PlayerSlot { get; init; }
    public required IReadOnlyList<BoardCoordinate> Positions { get; init; }
}

public sealed class BoardCoordinate
{
    public required int X { get; init; }
    public required int Y { get; init; }
}
