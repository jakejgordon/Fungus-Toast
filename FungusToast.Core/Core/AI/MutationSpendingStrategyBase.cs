using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Core.Mutations;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

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

        public abstract void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board);

        protected Mutation? PickBestTendrilMutation(Player player, List<Mutation> options, GameBoard board)
        {
            // Tendril directions toward center
            var directionMap = new Dictionary<Mutation?, (int dx, int dy)>
            {
                [MutationRegistry.GetById(MutationIds.TendrilNorthwest)] = (-1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilNortheast)] = (1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilSouthwest)] = (-1, -1),
                [MutationRegistry.GetById(MutationIds.TendrilSoutheast)] = (1, -1),
            };

            // Filter to only available options in the direction map
            var tendrilMutations = directionMap.Keys
                .Where(m => m != null && options.Contains(m))
                .Cast<Mutation>()
                .ToList();

            // Determine which Tendrils this player already owns
            var ownedTendrils = tendrilMutations
                .Where(m => player.PlayerMutations.ContainsKey(m.Id) && player.PlayerMutations[m.Id].CurrentLevel > 0)
                .ToList();

            var unownedTendrils = tendrilMutations
                .Except(ownedTendrils)
                .ToList();

            // CASE 1: Player has no Tendril upgrades yet — pick the one pointing most toward center
            if (ownedTendrils.Count == 0)
            {
                return GetHighestScoringTendril(tendrilMutations, board, player.PlayerId, directionMap);
            }

            // CASE 2: Player has some Tendrils, but not all — pick one of the unowned ones at random
            if (unownedTendrils.Count > 0)
            {
                return unownedTendrils[rng.Next(unownedTendrils.Count)];
            }

            // CASE 3: Player has at least one point in all Tendrils
            // Prefer the one pointing toward center that is not maxed out
            var ordered = GetOrderedByScore(tendrilMutations, board, player.PlayerId, directionMap);
            foreach (var m in ordered)
            {
                var pm = player.PlayerMutations[m.Id];
                if (!pm.IsMaxedOut)
                    return m;
            }

            // All are maxed out — nothing left to upgrade
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

                    if (tile != null && !tile.IsOccupied)
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
