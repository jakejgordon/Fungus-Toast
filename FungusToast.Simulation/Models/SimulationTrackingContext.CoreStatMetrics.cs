using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // Mutation Points
        // ────────────────

        private readonly Dictionary<int, Dictionary<MutationTier, int>> mutationPointsSpentByTier = new();
        public void RecordMutationPointsSpent(int playerId, MutationTier tier, int mutationPointsSpent)
        {
            if (!mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict))
                mutationPointsSpentByTier[playerId] = tierDict = new Dictionary<MutationTier, int>();
            if (!tierDict.ContainsKey(tier))
                tierDict[tier] = 0;
            tierDict[tier] += mutationPointsSpent;
        }
        public int GetMutationPointsSpent(int playerId, MutationTier tier)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var tierDict) && tierDict.TryGetValue(tier, out var val) ? val : 0;
        public Dictionary<MutationTier, int> GetMutationPointsSpentByTier(int playerId)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var dict) ? new Dictionary<MutationTier, int>(dict) : new();
        public int GetTotalMutationPointsSpent(int playerId)
            => mutationPointsSpentByTier.TryGetValue(playerId, out var dict) ? dict.Values.Sum() : 0;
        public Dictionary<int, Dictionary<MutationTier, int>> GetAllMutationPointsSpentByTier()
            => mutationPointsSpentByTier.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MutationTier, int>(kvp.Value));

        // ────────────────
        // Mutation Point Income
        // ────────────────

        private readonly Dictionary<int, int> mutationPointIncomeByPlayer = new();
        public void RecordMutationPointIncome(int playerId, int newMutationPoints)
        {
            if (!mutationPointIncomeByPlayer.ContainsKey(playerId))
                mutationPointIncomeByPlayer[playerId] = 0;
            mutationPointIncomeByPlayer[playerId] += newMutationPoints;
        }
        public int GetMutationPointIncome(int playerId)
            => mutationPointIncomeByPlayer.TryGetValue(playerId, out var val) ? val : 0;
        public IReadOnlyDictionary<int, int> GetAllMutationPointIncome()
            => mutationPointIncomeByPlayer;

        // ────────────────
        // Banked Points
        // ────────────────

        private readonly Dictionary<int, int> bankedPointsByPlayer = new();
        public void RecordBankedPoints(int playerId, int pointsBanked)
        {
            if (!bankedPointsByPlayer.ContainsKey(playerId))
                bankedPointsByPlayer[playerId] = 0;
            bankedPointsByPlayer[playerId] += pointsBanked;
        }
        public int GetBankedPoints(int playerId)
            => bankedPointsByPlayer.TryGetValue(playerId, out var val) ? val : 0;
        public IReadOnlyDictionary<int, int> GetAllBankedPoints()
            => bankedPointsByPlayer;

        // ────────────────
        // Nutrient Patches
        // ────────────────

        private int nutrientPatchesPlaced;
        private readonly Dictionary<int, int> nutrientPatchesConsumedByPlayer = new();
        private readonly Dictionary<int, int> nutrientMutationPointsEarnedByPlayer = new();

        public void RecordNutrientPatchesPlaced(int count)
        {
            nutrientPatchesPlaced += count;
        }

        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount)
        {
            if (!nutrientPatchesConsumedByPlayer.ContainsKey(playerId))
            {
                nutrientPatchesConsumedByPlayer[playerId] = 0;
            }

            if (!nutrientMutationPointsEarnedByPlayer.ContainsKey(playerId))
            {
                nutrientMutationPointsEarnedByPlayer[playerId] = 0;
            }

            nutrientPatchesConsumedByPlayer[playerId]++;
            if (rewardType == NutrientRewardType.MutationPoints)
            {
                nutrientMutationPointsEarnedByPlayer[playerId] += rewardAmount;
            }
        }

        public int GetNutrientPatchesPlaced() => nutrientPatchesPlaced;
        public int GetNutrientPatchesConsumed(int playerId)
            => nutrientPatchesConsumedByPlayer.TryGetValue(playerId, out var value) ? value : 0;
        public int GetNutrientMutationPointsEarned(int playerId)
            => nutrientMutationPointsEarnedByPlayer.TryGetValue(playerId, out var value) ? value : 0;

        // ────────────────
        // Cell Deaths by Reason
        // ────────────────

        private readonly Dictionary<int, Dictionary<DeathReason, int>> deathsByPlayerAndReason = new();
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1)
        {
            if (!deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
                deathsByPlayerAndReason[playerId] = reasonDict = new Dictionary<DeathReason, int>();
            if (!reasonDict.ContainsKey(reason))
                reasonDict[reason] = 0;
            reasonDict[reason] += deathCount;
        }
        public int GetCellDeathCount(int playerId, DeathReason reason)
            => deathsByPlayerAndReason.TryGetValue(playerId, out var reasonDict) && reasonDict.TryGetValue(reason, out var val) ? val : 0;
        public Dictionary<int, Dictionary<DeathReason, int>> GetAllCellDeathsByPlayerAndReason()
            => deathsByPlayerAndReason.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<DeathReason, int>(kvp.Value));
        public Dictionary<DeathReason, int> GetTotalDeathsByReason()
        {
            var result = new Dictionary<DeathReason, int>();
            foreach (var playerDict in deathsByPlayerAndReason.Values)
                foreach (var kvp in playerDict)
                    result[kvp.Key] = result.TryGetValue(kvp.Key, out var v) ? v + kvp.Value : kvp.Value;
            return result;
        }

        // ────────────────
        // Attributed Kills by Reason (attacker-caused)
        // ────────────────

        private readonly Dictionary<int, Dictionary<DeathReason, int>> attributedKillsByPlayerAndReason = new();
        public void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1)
        {
            if (!attributedKillsByPlayerAndReason.TryGetValue(playerId, out var reasonDict))
                attributedKillsByPlayerAndReason[playerId] = reasonDict = new Dictionary<DeathReason, int>();
            if (!reasonDict.ContainsKey(reason))
                reasonDict[reason] = 0;
            reasonDict[reason] += killCount;
        }
        public int GetAttributedKillCount(int playerId, DeathReason reason)
            => attributedKillsByPlayerAndReason.TryGetValue(playerId, out var reasonDict) && reasonDict.TryGetValue(reason, out var val) ? val : 0;
        public Dictionary<int, Dictionary<DeathReason, int>> GetAllAttributedKillsByPlayerAndReason()
            => attributedKillsByPlayerAndReason.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<DeathReason, int>(kvp.Value));
    }
}
