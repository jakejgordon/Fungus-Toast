using FungusToast.Core.Players;

namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        // ────────────────
        // First-Acquired Rounds
        // ────────────────
        private readonly Dictionary<(int playerId, int mutationId), List<int>> firstUpgradeRounds = new();
        private readonly Dictionary<(int playerId, string strategy, int mutationId), List<int>> firstUpgradeRoundsByStrategy = new();

        public void RecordFirstUpgradeRounds(List<Player> players)
        {
            foreach (var player in players)
            {
                string strategyName = player.MutationStrategy?.StrategyName ?? "Unknown";
                foreach (var pm in player.PlayerMutations.Values)
                {
                    if (pm.FirstUpgradeRound.HasValue)
                    {
                        var key = (player.PlayerId, pm.MutationId);
                        if (!firstUpgradeRounds.ContainsKey(key))
                            firstUpgradeRounds[key] = new List<int>();
                        firstUpgradeRounds[key].Add(pm.FirstUpgradeRound.Value);

                        var strategyKey = (player.PlayerId, strategyName, pm.MutationId);
                        if (!firstUpgradeRoundsByStrategy.ContainsKey(strategyKey))
                            firstUpgradeRoundsByStrategy[strategyKey] = new List<int>();
                        firstUpgradeRoundsByStrategy[strategyKey].Add(pm.FirstUpgradeRound.Value);
                    }
                }
            }
        }

        public (double? avg, int? min, int? max, int count) GetFirstUpgradeStats(int playerId, int mutationId)
        {
            var key = (playerId, mutationId);
            if (!firstUpgradeRounds.ContainsKey(key) || firstUpgradeRounds[key].Count == 0)
                return (null, null, null, 0);
            var list = firstUpgradeRounds[key];
            return (list.Average(), list.Min(), list.Max(), list.Count);
        }

        public (double? avg, int? min, int? max, int count) GetFirstUpgradeStatsByStrategy(int playerId, string strategy, int mutationId)
        {
            var key = (playerId, strategy, mutationId);
            if (!firstUpgradeRoundsByStrategy.ContainsKey(key) || firstUpgradeRoundsByStrategy[key].Count == 0)
                return (null, null, null, 0);
            var list = firstUpgradeRoundsByStrategy[key];
            return (list.Average(), list.Min(), list.Max(), list.Count);
        }

        public Dictionary<(int playerId, int mutationId), List<int>> GetAllFirstUpgradeRounds() => new(firstUpgradeRounds);
    }
}
