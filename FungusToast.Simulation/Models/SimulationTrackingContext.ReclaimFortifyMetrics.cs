namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Reclamation Rhizomorphs
        // ────────────────
        private readonly Dictionary<int, int> reclamationRhizomorphsSecondAttempts = new();
        public void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count)
        {
            if (!reclamationRhizomorphsSecondAttempts.ContainsKey(playerId))
                reclamationRhizomorphsSecondAttempts[playerId] = 0;
            reclamationRhizomorphsSecondAttempts[playerId] += count;
        }
        public int GetReclamationRhizomorphsSecondAttempts(int playerId)
            => reclamationRhizomorphsSecondAttempts.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllReclamationRhizomorphsSecondAttempts() => new(reclamationRhizomorphsSecondAttempts);

        // ────────────────
        // Necrophoric Adaptation Reclamations
        // ────────────────
        private readonly Dictionary<int, int> necrophoricAdaptationReclamations = new();
        public void RecordNecrophoricAdaptationReclamation(int playerId, int count)
        {
            if (!necrophoricAdaptationReclamations.ContainsKey(playerId))
                necrophoricAdaptationReclamations[playerId] = 0;
            necrophoricAdaptationReclamations[playerId] += count;
        }
        public int GetNecrophoricAdaptationReclamations(int playerId)
            => necrophoricAdaptationReclamations.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllNecrophoricAdaptationReclamations() => new(necrophoricAdaptationReclamations);

        // ────────────────
        // Ballistospore Discharge
        // ────────────────
        private readonly Dictionary<int, int> ballistosporeDischargeDrops = new();
        public void RecordBallistosporeDischarge(int playerId, int count)
        {
            if (!ballistosporeDischargeDrops.ContainsKey(playerId))
                ballistosporeDischargeDrops[playerId] = 0;
            ballistosporeDischargeDrops[playerId] += count;
        }
        public int GetBallistosporeDischargeDrops(int playerId)
            => ballistosporeDischargeDrops.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllBallistosporeDischargeDrops() => new(ballistosporeDischargeDrops);

        // ────────────────
        // Chitin Fortification Cells Fortified
        // ────────────────
        private readonly Dictionary<int, int> chitinFortificationCellsFortified = new();
        public void RecordChitinFortificationCellsFortified(int playerId, int count)
        {
            if (!chitinFortificationCellsFortified.ContainsKey(playerId))
                chitinFortificationCellsFortified[playerId] = 0;
            chitinFortificationCellsFortified[playerId] += count;
        }
        public int GetChitinFortificationCellsFortified(int playerId)
            => chitinFortificationCellsFortified.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllChitinFortificationCellsFortified() => new(chitinFortificationCellsFortified);

        // ────────────────
        // Necrotic Clearance
        // ────────────────
        private readonly Dictionary<int, int> necroticClearanceCorpsesCleared = new();
        private readonly Dictionary<int, int> necroticClearanceContestedCorpsesCleared = new();
        public void RecordNecroticClearanceCorpsesCleared(int playerId, int count, int contestedCount)
        {
            necroticClearanceCorpsesCleared[playerId] = GetNecroticClearanceCorpsesCleared(playerId) + count;
            necroticClearanceContestedCorpsesCleared[playerId] = GetNecroticClearanceContestedCorpsesCleared(playerId) + contestedCount;
        }
        public int GetNecroticClearanceCorpsesCleared(int playerId)
            => necroticClearanceCorpsesCleared.TryGetValue(playerId, out var value) ? value : 0;
        public int GetNecroticClearanceContestedCorpsesCleared(int playerId)
            => necroticClearanceContestedCorpsesCleared.TryGetValue(playerId, out var value) ? value : 0;
    }
}
