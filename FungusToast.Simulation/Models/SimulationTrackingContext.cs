using FungusToast.Core.Metrics;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext : ISimulationObserver
    {
        public void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles) { }
    }
}
