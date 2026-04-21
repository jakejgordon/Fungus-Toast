using FungusToast.Core.Metrics;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext : ISimulationObserver
    {
        public void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles) { }
        public void RecordMycelialCrescendoSurge(int playerId, string surgeName) { }
        public void RecordPrimePulseTriggered(int playerId, int triggerRound, int mutationPointsAwarded) { }
    }
}
