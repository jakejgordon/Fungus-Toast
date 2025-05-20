using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public abstract class MutationSpendingStrategyBase : IMutationSpendingStrategy
    {
        protected static readonly Random rng = new();

        public abstract void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board);

        protected Mutation? PickBestTendrilMutation(Player player, List<Mutation> options, GameBoard board)
        {
            var directionMap = new Dictionary<Mutation?, (int dx, int dy)>
            {
                [MutationRegistry.GetById(MutationIds.TendrilNorthwest)] = (-1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilNortheast)] = (1, 1),
                [MutationRegistry.GetById(MutationIds.TendrilSouthwest)] = (-1, -1),
                [MutationRegistry.GetById(MutationIds.TendrilSoutheast)] = (1, -1),
            };

            var cells = board.GetAllCellsOwnedBy(player.PlayerId);
            var scores = new Dictionary<Mutation, int>();

            foreach (var kvp in directionMap)
            {
                var mutation = kvp.Key;
                if (mutation == null || !options.Contains(mutation))
                    continue;

                var (dx, dy) = kvp.Value;
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

            if (scores.Count == 0)
                return null;

            return scores.OrderByDescending(kv => kv.Value).First().Key;
        }
    }
}
