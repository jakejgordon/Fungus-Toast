using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;

namespace FungusToast.Core.AI
{
    public abstract class MutationSpendingStrategyBase : IMutationSpendingStrategy
    {
        protected static readonly Random rng = new();

        public abstract string StrategyName { get; }
        public virtual MutationTier? MaxTier { get; }
        public virtual bool? PrioritizeHighTier { get; }
        public virtual bool? UsesGrowth { get; }
        public virtual bool? UsesCellularResilience { get; }
        public virtual bool? UsesFungicide { get; }
        public virtual bool? UsesGeneticDrift { get; }

        public void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board,
            Random rnd, ISimulationObserver? simulationObserver = null)
        {
            // Compute MP before
            int[] mpBefore = GetPointsByTier(player);

            // Delegate actual spending logic to child
            PerformSpendingLogic(player, allMutations, board, rnd, simulationObserver);

            // Compute MP after
            int[] mpAfter = GetPointsByTier(player);

            // Record delta per tier
            if (simulationObserver != null)
            {
                for (int t = 0; t < mpBefore.Length; t++)
                {
                    int spent = mpAfter[t] - mpBefore[t];
                    if (spent > 0)
                    {
                        simulationObserver.RecordMutationPointsSpent(
                            player.PlayerId,
                            (MutationTier)t,
                            spent
                        );
                    }
                }
            }
        }

        protected int[] GetPointsByTier(Player player)
        {
            int tierCount = Enum.GetValues(typeof(MutationTier)).Length;
            int[] pointsByTier = new int[tierCount];
            foreach (var kvp in player.PlayerMutations)
            {
                var mutation = MutationRegistry.GetById(kvp.Key);
                if (mutation == null) continue;
                int tierIdx = (int)mutation.Tier;
                // Use TotalPointsSpent if you track it, else use CurrentLevel * PointsPerUpgrade
                pointsByTier[tierIdx] += kvp.Value.CurrentLevel * mutation.PointsPerUpgrade;
            }
            return pointsByTier;
        }


        // Each concrete strategy must implement this core logic:
        protected abstract void PerformSpendingLogic(Player player, List<Mutation> allMutations, GameBoard board,
            Random rnd, ISimulationObserver? simulationObserver);


        protected Mutation? PickBestTendrilMutation(Player player, List<Mutation> options, GameBoard board)
        {
            var directionMap = new Dictionary<Mutation?, (int dx, int dy)>
            {
                [MutationRegistry.GetById(MutationIds.TendrilNorthwest)] = (-1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilNortheast)] = (1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilSouthwest)] = (-1, -1),
                [MutationRegistry.GetById(MutationIds.TendrilSoutheast)] = (1, -1),
            };

            var tendrilMutations = directionMap.Keys
                .Where(m => m != null && options.Contains(m))
                .Cast<Mutation>()
                .ToList();

            var ownedTendrils = tendrilMutations
                .Where(m => player.PlayerMutations.ContainsKey(m.Id) && player.PlayerMutations[m.Id].CurrentLevel > 0)
                .ToList();

            var unownedTendrils = tendrilMutations
                .Except(ownedTendrils)
                .ToList();

            if (ownedTendrils.Count == 0)
            {
                return GetHighestScoringTendril(tendrilMutations, board, player.PlayerId, directionMap);
            }

            if (unownedTendrils.Count > 0)
            {
                return unownedTendrils[rng.Next(unownedTendrils.Count)];
            }

            var ordered = GetOrderedByScore(tendrilMutations, board, player.PlayerId, directionMap);
            foreach (var m in ordered)
            {
                var pm = player.PlayerMutations[m.Id];
                if (!pm.IsMaxedOut)
                    return m;
            }

            return null;
        }

        private Mutation? GetHighestScoringTendril(
            List<Mutation> mutations,
            GameBoard board,
            int playerId,
            Dictionary<Mutation?, (int dx, int dy)> directionMap)
        {
            return GetOrderedByScore(mutations, board, playerId, directionMap).FirstOrDefault();
        }

        private List<Mutation> GetOrderedByScore(
            List<Mutation> mutations,
            GameBoard board,
            int playerId,
            Dictionary<Mutation?, (int dx, int dy)> directionMap)
        {
            var cells = board.GetAllCellsOwnedBy(playerId);
            var scores = new Dictionary<Mutation, int>();

            foreach (var mutation in mutations)
            {
                var (dx, dy) = directionMap[mutation];
                int score = 0;

                foreach (var cell in cells)
                {
                    var (x, y) = board.GetXYFromTileId(cell.TileId);
                    int nx = x + dx;
                    int ny = y + dy;
                    var tile = board.GetTile(nx, ny);

                    if (tile == null)
                        continue;

                    // Score if tile is either empty or contains a non-toxic cell
                    if (tile.FungalCell == null || !tile.FungalCell.IsToxin)
                        score++;
                }

                scores[mutation] = score;
            }

            return scores
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
        }

    }
}
