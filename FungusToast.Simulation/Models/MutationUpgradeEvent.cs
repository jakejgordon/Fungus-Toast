using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Models
{
    public sealed class MutationUpgradeEvent
    {
        public int PlayerId { get; init; }
        public int MutationId { get; init; }
        public string MutationName { get; init; } = string.Empty;
        public MutationTier MutationTier { get; init; }
        public int OldLevel { get; init; }
        public int NewLevel { get; init; }
        public int Round { get; init; }
        public int MutationPointsBefore { get; init; }
        public int MutationPointsAfter { get; init; }
        public int PointsSpent { get; init; }
        public string UpgradeSource { get; init; } = string.Empty;
        public string? StrategyName { get; set; }
    }
}
