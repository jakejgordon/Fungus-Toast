namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Ontogenic Regression Effects
        // ────────────────
        private readonly Dictionary<int, int> ontogenicRegressionActivations = new();
        private readonly Dictionary<int, int> ontogenicRegressionDevolvedLevels = new();
        private readonly Dictionary<int, int> ontogenicRegressionTier5PlusLevels = new();
        private readonly Dictionary<int, int> ontogenicRegressionFailureBonuses = new();
        private readonly Dictionary<int, int> ontogenicRegressionSacrificeCells = new();
        private readonly Dictionary<int, int> ontogenicRegressionSacrificeLevelOffsets = new();

        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained)
        {
            if (!ontogenicRegressionActivations.ContainsKey(playerId))
                ontogenicRegressionActivations[playerId] = 0;
            ontogenicRegressionActivations[playerId]++;

            if (!ontogenicRegressionDevolvedLevels.ContainsKey(playerId))
                ontogenicRegressionDevolvedLevels[playerId] = 0;
            ontogenicRegressionDevolvedLevels[playerId] += sourceLevelsLost;

            if (!ontogenicRegressionTier5PlusLevels.ContainsKey(playerId))
                ontogenicRegressionTier5PlusLevels[playerId] = 0;
            ontogenicRegressionTier5PlusLevels[playerId] += targetLevelsGained;
        }

        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints)
        {
            if (!ontogenicRegressionFailureBonuses.ContainsKey(playerId))
                ontogenicRegressionFailureBonuses[playerId] = 0;
            ontogenicRegressionFailureBonuses[playerId] += bonusPoints;
        }

        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset)
        {
            if (cellsKilled <= 0 && levelsOffset == 0) return;

            if (cellsKilled > 0)
            {
                if (!ontogenicRegressionSacrificeCells.ContainsKey(playerId))
                    ontogenicRegressionSacrificeCells[playerId] = 0;
                ontogenicRegressionSacrificeCells[playerId] += cellsKilled;
            }

            if (levelsOffset != 0)
            {
                if (!ontogenicRegressionSacrificeLevelOffsets.ContainsKey(playerId))
                    ontogenicRegressionSacrificeLevelOffsets[playerId] = 0;
                ontogenicRegressionSacrificeLevelOffsets[playerId] += levelsOffset;
            }
        }

        public int GetOntogenicRegressionActivations(int playerId)
            => ontogenicRegressionActivations.TryGetValue(playerId, out var val) ? val : 0;

        public int GetOntogenicRegressionDevolvedLevels(int playerId)
            => ontogenicRegressionDevolvedLevels.TryGetValue(playerId, out var val) ? val : 0;

        public int GetOntogenicRegressionTier5PlusLevels(int playerId)
            => ontogenicRegressionTier5PlusLevels.TryGetValue(playerId, out var val) ? val : 0;

        public int GetOntogenicRegressionFailureBonuses(int playerId)
            => ontogenicRegressionFailureBonuses.TryGetValue(playerId, out var val) ? val : 0;

        public int GetOntogenicRegressionSacrificeCells(int playerId)
            => ontogenicRegressionSacrificeCells.TryGetValue(playerId, out var v) ? v : 0;
        public int GetOntogenicRegressionSacrificeLevelOffset(int playerId)
            => ontogenicRegressionSacrificeLevelOffsets.TryGetValue(playerId, out var v) ? v : 0;

        public Dictionary<int, int> GetAllOntogenicRegressionActivations() => new(ontogenicRegressionActivations);
        public Dictionary<int, int> GetAllOntogenicRegressionDevolvedLevels() => new(ontogenicRegressionDevolvedLevels);
        public Dictionary<int, int> GetAllOntogenicRegressionTier5PlusLevels() => new(ontogenicRegressionTier5PlusLevels);
        public Dictionary<int, int> GetAllOntogenicRegressionFailureBonuses() => new(ontogenicRegressionFailureBonuses);

        // ────────────────
        // Competitive Antagonism Targeting
        // ────────────────
        private readonly Dictionary<int, int> competitiveAntagonismTargeting = new();

        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected)
        {
            if (!competitiveAntagonismTargeting.ContainsKey(playerId))
                competitiveAntagonismTargeting[playerId] = 0;
            competitiveAntagonismTargeting[playerId] += targetsAffected;
        }

        public int GetCompetitiveAntagonismTargeting(int playerId)
            => competitiveAntagonismTargeting.TryGetValue(playerId, out var val) ? val : 0;

        public Dictionary<int, int> GetAllCompetitiveAntagonismTargeting() => new(competitiveAntagonismTargeting);
    }
}
