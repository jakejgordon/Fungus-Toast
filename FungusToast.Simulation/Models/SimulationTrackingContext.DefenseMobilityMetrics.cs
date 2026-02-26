namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Neutralizing Mantle Effects
        // ────────────────
        private readonly Dictionary<int, int> neutralizingMantleEffects = new();
        public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized)
        {
            if (!neutralizingMantleEffects.ContainsKey(playerId))
                neutralizingMantleEffects[playerId] = 0;
            neutralizingMantleEffects[playerId] += toxinsNeutralized;
        }
        public int GetNeutralizingMantleEffects(int playerId)
            => neutralizingMantleEffects.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllNeutralizingMantleEffects() => new(neutralizingMantleEffects);

        // ────────────────
        // Bastioned Cells (Mycelial Bastion)
        // ────────────────
        private readonly Dictionary<int, int> bastionedCells = new();
        public void RecordBastionedCells(int playerId, int count)
        {
            if (!bastionedCells.ContainsKey(playerId))
                bastionedCells[playerId] = 0;
            bastionedCells[playerId] += count;
        }
        public int GetBastionedCells(int playerId) => bastionedCells.TryGetValue(playerId, out var val) ? val : 0;

        // --- Creeping Mold toxin jumps ---
        private readonly Dictionary<int, int> creepingMoldToxinJumps = new();
        public void RecordCreepingMoldToxinJump(int playerId)
        {
            if (!creepingMoldToxinJumps.ContainsKey(playerId))
                creepingMoldToxinJumps[playerId] = 0;
            creepingMoldToxinJumps[playerId]++;
        }
        public int GetCreepingMoldToxinJumps(int playerId)
            => creepingMoldToxinJumps.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllCreepingMoldToxinJumps() => new(creepingMoldToxinJumps);
    }
}
