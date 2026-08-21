using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Requires a minimum amount of investment in root mutations of a given tier across
    /// a minimum number of distinct categories. Each qualifying category must independently
    /// meet <see cref="RequiredLevelsPerCategory"/>; levels are not pooled across categories.
    /// </summary>
    public sealed class MutationCategoryInvestmentPrerequisite
    {
        public MutationTier Tier { get; }
        public int RequiredLevelsPerCategory { get; }
        public int RequiredCategoryCount { get; }

        public MutationCategoryInvestmentPrerequisite(
            MutationTier tier,
            int requiredLevelsPerCategory,
            int requiredCategoryCount)
        {
            if (requiredLevelsPerCategory <= 0)
                throw new ArgumentOutOfRangeException(nameof(requiredLevelsPerCategory));
            if (requiredCategoryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(requiredCategoryCount));

            Tier = tier;
            RequiredLevelsPerCategory = requiredLevelsPerCategory;
            RequiredCategoryCount = requiredCategoryCount;
        }

        public bool IsMet(Player player, IReadOnlyDictionary<int, Mutation> rootMutations)
        {
            return GetCategoryInvestments(player, rootMutations)
                .Count(entry => entry.Value >= RequiredLevelsPerCategory) >= RequiredCategoryCount;
        }

        public IReadOnlyDictionary<MutationCategory, int> GetCategoryInvestments(
            Player player,
            IReadOnlyDictionary<int, Mutation> rootMutations)
        {
            return rootMutations.Values
                .Where(mutation => mutation.Tier == Tier)
                .GroupBy(mutation => mutation.Category)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(mutation => player.GetMutationLevel(mutation.Id)));
        }

        public bool Includes(Mutation mutation, IReadOnlyDictionary<int, Mutation> rootMutations)
        {
            return mutation.Tier == Tier && rootMutations.ContainsKey(mutation.Id);
        }
    }
}
