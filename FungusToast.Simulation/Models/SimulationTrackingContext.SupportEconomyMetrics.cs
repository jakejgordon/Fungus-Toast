namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // Surgical Inoculation Drops
        private readonly Dictionary<int, int> surgicalInoculationDrops = new();
        public void RecordSurgicalInoculationDrop(int playerId, int count)
        {
            if (!surgicalInoculationDrops.ContainsKey(playerId))
                surgicalInoculationDrops[playerId] = 0;
            surgicalInoculationDrops[playerId] += count;
        }
        public int GetSurgicalInoculationDrops(int playerId)
            => surgicalInoculationDrops.TryGetValue(playerId, out var val) ? val : 0;

        // ────────────────
        // Putrefactive Rejuvenation Growth Cycles Reduced
        // ────────────────
        private readonly Dictionary<int, int> putrefactiveRejuvenationCyclesReduced = new();
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced)
        {
            if (!putrefactiveRejuvenationCyclesReduced.ContainsKey(playerId))
                putrefactiveRejuvenationCyclesReduced[playerId] = 0;
            putrefactiveRejuvenationCyclesReduced[playerId] += totalCyclesReduced;
        }
        public int GetPutrefactiveRejuvenationGrowthCyclesReduced(int playerId)
            => putrefactiveRejuvenationCyclesReduced.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllPutrefactiveRejuvenationGrowthCyclesReduced() => new(putrefactiveRejuvenationCyclesReduced);
    }
}
