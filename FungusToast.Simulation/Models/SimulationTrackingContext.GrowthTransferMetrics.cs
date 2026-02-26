namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Perimeter Proliferator Growths
        // ────────────────
        private readonly Dictionary<int, int> perimeterProliferatorGrowths = new();
        public void RecordPerimeterProliferatorGrowth(int playerId)
        {
            if (!perimeterProliferatorGrowths.ContainsKey(playerId))
                perimeterProliferatorGrowths[playerId] = 0;
            perimeterProliferatorGrowths[playerId]++;
        }
        public int GetPerimeterProliferatorGrowths(int playerId)
            => perimeterProliferatorGrowths.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllPerimeterProliferatorGrowths() => new(perimeterProliferatorGrowths);

        // ────────────────
        // Hyphal Resistance Transfer
        // ────────────────
        private readonly Dictionary<int, int> hyphalResistanceTransfers = new();
        public void RecordHyphalResistanceTransfer(int playerId, int count)
        {
            if (!hyphalResistanceTransfers.ContainsKey(playerId))
                hyphalResistanceTransfers[playerId] = 0;
            hyphalResistanceTransfers[playerId] += count;
        }
        public int GetHyphalResistanceTransfers(int playerId)
            => hyphalResistanceTransfers.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllHyphalResistanceTransfers() => new(hyphalResistanceTransfers);

        // ────────────────
        // Enduring Toxaphores Extended Cycles
        // ────────────────
        private readonly Dictionary<int, int> enduringToxaphoresExtendedCycles = new();
        public void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles)
        {
            if (!enduringToxaphoresExtendedCycles.ContainsKey(playerId))
                enduringToxaphoresExtendedCycles[playerId] = 0;
            enduringToxaphoresExtendedCycles[playerId] += cycles;
        }
        public int GetEnduringToxaphoresExtendedCycles(int playerId)
            => enduringToxaphoresExtendedCycles.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllEnduringToxaphoresExtendedCycles() => new(enduringToxaphoresExtendedCycles);

        // ────────────────
        // Enduring Toxaphores Existing Extensions
        // ────────────────
        private readonly Dictionary<int, int> enduringToxaphoresExistingExtensions = new();
        public void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles)
        {
            if (!enduringToxaphoresExistingExtensions.ContainsKey(playerId))
                enduringToxaphoresExistingExtensions[playerId] = 0;
            enduringToxaphoresExistingExtensions[playerId] += cycles;
        }
        public int GetEnduringToxaphoresExistingExtensions(int playerId)
            => enduringToxaphoresExistingExtensions.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllEnduringToxaphoresExistingExtensions() => new(enduringToxaphoresExistingExtensions);
    }
}
