using FungusToast.Core.AI;

namespace FungusToast.Simulation.Models
{
    public enum StrategySelectionSource
    {
        Sampled,
        ExplicitLineup
    }

    public sealed class SelectedStrategyMetadata
    {
        public required int LineupOrder { get; init; }
        public required string StrategyName { get; init; }
        public required string StrategyTheme { get; init; }
        public required string StrategyStatus { get; init; }
        public required string Intent { get; init; }
    }

    public sealed class SimulationRunMetadata
    {
        public required string ExperimentId { get; init; }
        public required DateTime RunTimestampUtc { get; init; }
        public required StrategySetEnum StrategySet { get; init; }
        public required int BaseSeed { get; init; }
        public required SlotAssignmentPolicy SlotAssignmentPolicy { get; init; }
        public required StrategySelectionPolicy StrategySelectionPolicy { get; init; }
        public required StrategySelectionSource StrategySelectionSource { get; init; }
        public required int NumberOfPlayers { get; init; }
        public required int NumberOfGamesRequested { get; init; }
        public required int BoardWidth { get; init; }
        public required int BoardHeight { get; init; }
        public required IReadOnlyList<SelectedStrategyMetadata> SelectedStrategies { get; init; }
    }
}
