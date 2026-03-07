using FungusToast.Core.AI;

namespace FungusToast.Simulation.Models
{
    public sealed class SimulationRunMetadata
    {
        public required string ExperimentId { get; init; }
        public required DateTime RunTimestampUtc { get; init; }
        public required StrategySetEnum StrategySet { get; init; }
        public required int BaseSeed { get; init; }
        public required SlotAssignmentPolicy SlotAssignmentPolicy { get; init; }
        public required int NumberOfPlayers { get; init; }
        public required int NumberOfGamesRequested { get; init; }
        public required int BoardWidth { get; init; }
        public required int BoardHeight { get; init; }
    }
}
