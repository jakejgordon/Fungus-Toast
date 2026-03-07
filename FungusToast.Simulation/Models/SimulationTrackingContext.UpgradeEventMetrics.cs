using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        private readonly List<MutationUpgradeEvent> mutationUpgradeEvents = new();

        public void RecordMutationUpgradeEvent(
            int playerId,
            int mutationId,
            string mutationName,
            MutationTier mutationTier,
            int oldLevel,
            int newLevel,
            int round,
            int mutationPointsBefore,
            int mutationPointsAfter,
            int pointsSpent,
            string upgradeSource)
        {
            mutationUpgradeEvents.Add(new MutationUpgradeEvent
            {
                PlayerId = playerId,
                MutationId = mutationId,
                MutationName = mutationName,
                MutationTier = mutationTier,
                OldLevel = oldLevel,
                NewLevel = newLevel,
                Round = round,
                MutationPointsBefore = mutationPointsBefore,
                MutationPointsAfter = mutationPointsAfter,
                PointsSpent = pointsSpent,
                UpgradeSource = upgradeSource
            });
        }

        public IReadOnlyList<MutationUpgradeEvent> GetMutationUpgradeEvents()
            => mutationUpgradeEvents;
    }
}
