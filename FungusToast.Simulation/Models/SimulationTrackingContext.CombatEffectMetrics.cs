namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Putrefactive Cascade Effects
        // ────────────────
        private readonly Dictionary<int, int> putrefactiveCascadeKills = new();
        private readonly Dictionary<int, int> putrefactiveCascadeToxified = new();
        public void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills)
        {
            if (!putrefactiveCascadeKills.ContainsKey(playerId))
                putrefactiveCascadeKills[playerId] = 0;
            putrefactiveCascadeKills[playerId] += cascadeKills;
        }
        public void RecordPutrefactiveCascadeToxified(int playerId, int toxified)
        {
            if (!putrefactiveCascadeToxified.ContainsKey(playerId))
                putrefactiveCascadeToxified[playerId] = 0;
            putrefactiveCascadeToxified[playerId] += toxified;
        }
        public int GetPutrefactiveCascadeKills(int playerId)
            => putrefactiveCascadeKills.TryGetValue(playerId, out var val) ? val : 0;
        public int GetPutrefactiveCascadeToxified(int playerId)
            => putrefactiveCascadeToxified.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllPutrefactiveCascadeKills() => new(putrefactiveCascadeKills);
        public Dictionary<int, int> GetAllPutrefactiveCascadeToxified() => new(putrefactiveCascadeToxified);

        // ────────────────
        // Mimetic Resilience Cell Placements
        // ────────────────
        private readonly Dictionary<int, int> mimeticResilienceInfestations = new();
        private readonly Dictionary<int, int> mimeticResilienceDrops = new();

        public void RecordMimeticResilienceInfestations(int playerId, int infestations)
        {
            if (!mimeticResilienceInfestations.ContainsKey(playerId))
                mimeticResilienceInfestations[playerId] = 0;
            mimeticResilienceInfestations[playerId] += infestations;
        }

        public void RecordMimeticResilienceDrops(int playerId, int drops)
        {
            if (!mimeticResilienceDrops.TryGetValue(playerId, out _))
                mimeticResilienceDrops[playerId] = 0;
            mimeticResilienceDrops[playerId] += drops;
        }

        public int GetMimeticResilienceInfestations(int playerId)
            => mimeticResilienceInfestations.TryGetValue(playerId, out var val) ? val : 0;
        public int GetMimeticResilienceDrops(int playerId)
            => mimeticResilienceDrops.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllMimeticResilienceInfestations() => new(mimeticResilienceInfestations);
        public Dictionary<int, int> GetAllMimeticResilienceDrops() => new(mimeticResilienceDrops);

        // ────────────────
        // Cytolytic Burst Effects
        // ────────────────
        private readonly Dictionary<int, int> cytolyticBurstToxins = new();
        private readonly Dictionary<int, int> cytolyticBurstKills = new();

        public void RecordCytolyticBurstToxins(int playerId, int toxinsCreated)
        {
            if (!cytolyticBurstToxins.ContainsKey(playerId))
                cytolyticBurstToxins[playerId] = 0;
            cytolyticBurstToxins[playerId] += toxinsCreated;
        }

        public void RecordCytolyticBurstKills(int playerId, int cellsKilled)
        {
            if (!cytolyticBurstKills.ContainsKey(playerId))
                cytolyticBurstKills[playerId] = 0;
            cytolyticBurstKills[playerId] += cellsKilled;
        }

        public int GetCytolyticBurstToxins(int playerId)
            => cytolyticBurstToxins.TryGetValue(playerId, out var val) ? val : 0;
        public int GetCytolyticBurstKills(int playerId)
            => cytolyticBurstKills.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllCytolyticBurstToxins() => new(cytolyticBurstToxins);
        public Dictionary<int, int> GetAllCytolyticBurstKills() => new(cytolyticBurstKills);
    }
}
