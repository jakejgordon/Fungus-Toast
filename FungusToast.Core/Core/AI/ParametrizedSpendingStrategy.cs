using FungusToast.Core.Board;
using FungusToast.Core.Core.Mutations;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.AI
{
    public class ParameterizedSpendingStrategy : MutationSpendingStrategyBase
    {
        public override string StrategyName { get; }

        private readonly MutationTier maxTier;
        private readonly bool prioritizeHighTier;
        private readonly List<MutationCategory> priorityMutationCategories;

        public ParameterizedSpendingStrategy(
            string strategyName,
            MutationTier maxTier,
            bool prioritizeHighTier,
            List<MutationCategory> priorityMutationCategories)
        {
            StrategyName = strategyName;
            this.maxTier = maxTier;
            this.prioritizeHighTier = prioritizeHighTier;
            this.priorityMutationCategories = priorityMutationCategories;
        }

        public override void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            bool spent;
            do
            {
                spent = false;

                // Step 1: Attempt within priority categories
                foreach (var category in priorityMutationCategories)
                {
                    var candidates = allMutations
                        .Where(m =>
                            m.Category == category &&
                            (int)m.Tier <= (int)maxTier &&
                            player.CanUpgrade(m))
                        .ToList();

                    if (TrySpendWithinCategory(player, board, candidates))
                    {
                        spent = true;
                        break;
                    }
                }

                // Step 2: Fallback to all valid under max tier
                if (!spent)
                {
                    var fallbackCandidates = allMutations
                        .Where(m => (int)m.Tier <= (int)maxTier && player.CanUpgrade(m))
                        .ToList();

                    spent = TrySpendWithinCategory(player, board, fallbackCandidates);
                }

                // Step 3: Final fallback to random
                if (!spent)
                {
                    spent = MutationSpendingHelper.TrySpendRandomly(player, allMutations);
                }

            } while (spent && player.MutationPoints > 0);
        }

        private bool TrySpendWithinCategory(Player player, GameBoard board, List<Mutation> candidates)
        {
            // 1. High-tier priority logic
            if (prioritizeHighTier)
            {
                foreach (var m in candidates
                    .Where(m => m.Prerequisites.Any())
                    .OrderByDescending(m => m.Tier))
                {
                    if (TryUpgradeWithTendrilAwareness(player, m, candidates, board))
                        return true;
                }
            }

            // 2. Remaining candidates
            foreach (var m in candidates)
            {
                if (TryUpgradeWithTendrilAwareness(player, m, candidates, board))
                    return true;
            }

            return false;
        }

        private bool TryUpgradeWithTendrilAwareness(Player player, Mutation candidate, List<Mutation> allCandidates, GameBoard board)
        {
            // If it's a Tendril, re-evaluate which Tendril is best
            if (IsTendril(candidate))
            {
                var bestTendril = PickBestTendrilMutation(player, allCandidates.Where(IsTendril).ToList(), board);
                if (bestTendril != null)
                    return player.TryUpgradeMutation(bestTendril);
                return false;
            }

            // Otherwise, upgrade the candidate directly
            return player.TryUpgradeMutation(candidate);
        }

        private bool IsTendril(Mutation m)
        {
            return m.Id == MutationIds.TendrilNorthwest
                || m.Id == MutationIds.TendrilNortheast
                || m.Id == MutationIds.TendrilSouthwest
                || m.Id == MutationIds.TendrilSoutheast;
        }
    }
}
