namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Chemotactic Mycotoxins Relocations
        // ────────────────
        private readonly Dictionary<int, int> chemotacticMycotoxinsRelocations = new();

        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations)
        {
            if (!chemotacticMycotoxinsRelocations.ContainsKey(playerId))
                chemotacticMycotoxinsRelocations[playerId] = 0;
            chemotacticMycotoxinsRelocations[playerId] += relocations;
        }

        public int GetChemotacticMycotoxinsRelocations(int playerId)
            => chemotacticMycotoxinsRelocations.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllChemotacticMycotoxinsRelocations() => new(chemotacticMycotoxinsRelocations);

        // ────────────────
        // Hypersystemic Regeneration Effects
        // ────────────────
        private readonly Dictionary<int, int> hypersystemicRegenerationResistance = new();
        private readonly Dictionary<int, int> hypersystemicDiagonalReclaims = new();

        public void RecordHypersystemicRegenerationResistance(int playerId)
        {
            if (!hypersystemicRegenerationResistance.ContainsKey(playerId))
                hypersystemicRegenerationResistance[playerId] = 0;
            hypersystemicRegenerationResistance[playerId]++;
        }

        public void RecordHypersystemicDiagonalReclaim(int playerId)
        {
            if (!hypersystemicDiagonalReclaims.ContainsKey(playerId))
                hypersystemicDiagonalReclaims[playerId] = 0;
            hypersystemicDiagonalReclaims[playerId]++;
        }

        public int GetHypersystemicRegenerationResistance(int playerId)
            => hypersystemicRegenerationResistance.TryGetValue(playerId, out var val) ? val : 0;
        public int GetHypersystemicDiagonalReclaims(int playerId)
            => hypersystemicDiagonalReclaims.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllHypersystemicRegenerationResistance() => new(hypersystemicRegenerationResistance);
        public Dictionary<int, int> GetAllHypersystemicDiagonalReclaims() => new(hypersystemicDiagonalReclaims);
    }
}
