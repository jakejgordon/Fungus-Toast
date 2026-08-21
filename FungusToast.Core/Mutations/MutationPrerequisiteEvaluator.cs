using FungusToast.Core.Players;
using System.Linq;

namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Shared prerequisite semantics for manual purchases, automatic upgrades, unlock
    /// bookkeeping, AI planning, and presentation snapshots.
    /// </summary>
    public static class MutationPrerequisiteEvaluator
    {
        public static bool HasRequirements(Mutation mutation)
        {
            return mutation.Prerequisites.Count > 0
                || mutation.CategoryInvestmentPrerequisites.Count > 0;
        }

        public static bool AreAllMet(Mutation mutation, Player player)
        {
            return mutation.Prerequisites.All(
                       prerequisite => player.GetMutationLevel(prerequisite.MutationId) >= prerequisite.RequiredLevel)
                && mutation.CategoryInvestmentPrerequisites.All(
                       prerequisite => prerequisite.IsMet(player, MutationRegistry.Roots));
        }

        public static bool CouldBeAffectedByUpgrade(Mutation dependent, Mutation upgradedMutation)
        {
            return dependent.Prerequisites.Any(prerequisite => prerequisite.MutationId == upgradedMutation.Id)
                || dependent.CategoryInvestmentPrerequisites.Any(
                       prerequisite => prerequisite.Includes(upgradedMutation, MutationRegistry.Roots));
        }
    }
}
