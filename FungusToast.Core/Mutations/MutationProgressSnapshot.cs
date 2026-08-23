using FungusToast.Core.Players;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mutations
{
    /// <summary>
    /// Immutable mutation progress/cost/relationship data for presentation surfaces.
    /// Does not claim full purchase eligibility, which can also depend on round and board state.
    /// </summary>
    public sealed class MutationProgressSnapshot
    {
        public Mutation Mutation { get; }
        public int CurrentLevel { get; }
        public int? NextLevel { get; }
        public float CurrentTotalEffect { get; }
        public float? NextTotalEffect { get; }
        public int Cost { get; }
        public int AvailablePoints { get; }
        public int ProjectedPointsAfterPurchase { get; }
        public bool IsAffordable { get; }
        public bool IsMaxed { get; }
        public bool IsActiveSurge { get; }
        public bool HasUnmetPrerequisites { get; }
        public IReadOnlyList<MutationRequirementProgress> Requirements { get; }
        public IReadOnlyList<MutationAnyRequirementGroupProgress> AnyRequirementGroups { get; }
        public IReadOnlyList<MutationCategoryInvestmentRequirementProgress> CategoryInvestmentRequirements { get; }
        public IReadOnlyList<Mutation> DirectDependents { get; }

        private MutationProgressSnapshot(
            Mutation mutation,
            int currentLevel,
            int? nextLevel,
            float currentTotalEffect,
            float? nextTotalEffect,
            int cost,
            int availablePoints,
            bool isActiveSurge,
            IReadOnlyList<MutationRequirementProgress> requirements,
            IReadOnlyList<MutationAnyRequirementGroupProgress> anyRequirementGroups,
            IReadOnlyList<MutationCategoryInvestmentRequirementProgress> categoryInvestmentRequirements,
            IReadOnlyList<Mutation> directDependents)
        {
            Mutation = mutation;
            CurrentLevel = currentLevel;
            NextLevel = nextLevel;
            CurrentTotalEffect = currentTotalEffect;
            NextTotalEffect = nextTotalEffect;
            Cost = cost;
            AvailablePoints = availablePoints;
            ProjectedPointsAfterPurchase = availablePoints - cost;
            IsAffordable = availablePoints >= cost;
            IsMaxed = currentLevel >= mutation.MaxLevel;
            IsActiveSurge = isActiveSurge;
            Requirements = requirements;
            AnyRequirementGroups = anyRequirementGroups;
            CategoryInvestmentRequirements = categoryInvestmentRequirements;
            HasUnmetPrerequisites = requirements.Any(requirement => !requirement.IsMet)
                || anyRequirementGroups.Any(group => !group.IsMet)
                || categoryInvestmentRequirements.Any(requirement => !requirement.IsMet);
            DirectDependents = directDependents;
        }

        public static MutationProgressSnapshot Create(Mutation mutation, Player player)
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            int? nextLevel = currentLevel < mutation.MaxLevel ? currentLevel + 1 : null;
            var requirements = mutation.Prerequisites
                .Select(prerequisite =>
                {
                    MutationRegistry.All.TryGetValue(prerequisite.MutationId, out var requiredMutation);
                    return new MutationRequirementProgress(
                        prerequisite.MutationId,
                        requiredMutation?.Name ?? $"Unknown Mutation {prerequisite.MutationId}",
                        player.GetMutationLevel(prerequisite.MutationId),
                        prerequisite.RequiredLevel);
                })
                .ToList();
            var categoryInvestmentRequirements = mutation.CategoryInvestmentPrerequisites
                .Select(prerequisite =>
                {
                    var investments = prerequisite.GetCategoryInvestments(player, MutationRegistry.Roots);
                    var categoryProgress = investments
                        .OrderBy(entry => entry.Key)
                        .Select(entry => new MutationCategoryInvestmentProgress(
                            entry.Key,
                            entry.Value,
                            prerequisite.RequiredLevelsPerCategory))
                        .ToList();

                    return new MutationCategoryInvestmentRequirementProgress(
                        prerequisite.Tier,
                        prerequisite.RequiredLevelsPerCategory,
                        prerequisite.RequiredCategoryCount,
                        categoryProgress);
                })
                .ToList();
            var anyRequirementGroups = mutation.AnyPrerequisiteGroups
                .Select(group => new MutationAnyRequirementGroupProgress(group.Alternatives
                    .Select(prerequisite =>
                    {
                        MutationRegistry.All.TryGetValue(prerequisite.MutationId, out var requiredMutation);
                        return new MutationRequirementProgress(prerequisite.MutationId, requiredMutation?.Name ?? $"Unknown Mutation {prerequisite.MutationId}", player.GetMutationLevel(prerequisite.MutationId), prerequisite.RequiredLevel);
                    }).ToList()))
                .ToList();
            var directDependents = MutationRegistry.All.Values
                .Where(candidate => candidate.Prerequisites.Any(prerequisite => prerequisite.MutationId == mutation.Id))
                .OrderBy(candidate => candidate.Tier)
                .ThenBy(candidate => candidate.Name)
                .ToList();

            return new MutationProgressSnapshot(
                mutation,
                currentLevel,
                nextLevel,
                mutation.GetTotalEffect(currentLevel),
                nextLevel.HasValue ? mutation.GetTotalEffect(nextLevel.Value) : null,
                player.GetMutationPointCost(mutation),
                player.MutationPoints,
                mutation.IsSurge && player.IsSurgeActive(mutation.Id),
                requirements,
                anyRequirementGroups,
                categoryInvestmentRequirements,
                directDependents);
        }
    }

    public sealed class MutationAnyRequirementGroupProgress
    {
        public IReadOnlyList<MutationRequirementProgress> Alternatives { get; }
        public bool IsMet => Alternatives.Any(requirement => requirement.IsMet);

        public MutationAnyRequirementGroupProgress(IReadOnlyList<MutationRequirementProgress> alternatives)
        {
            Alternatives = alternatives;
        }
    }

    public sealed class MutationRequirementProgress
    {
        public int MutationId { get; }
        public string MutationName { get; }
        public int CurrentLevel { get; }
        public int RequiredLevel { get; }
        public bool IsMet => CurrentLevel >= RequiredLevel;

        public MutationRequirementProgress(
            int mutationId,
            string mutationName,
            int currentLevel,
            int requiredLevel)
        {
            MutationId = mutationId;
            MutationName = mutationName;
            CurrentLevel = currentLevel;
            RequiredLevel = requiredLevel;
        }
    }

    public sealed class MutationCategoryInvestmentRequirementProgress
    {
        public MutationTier Tier { get; }
        public int RequiredLevelsPerCategory { get; }
        public int RequiredCategoryCount { get; }
        public IReadOnlyList<MutationCategoryInvestmentProgress> Categories { get; }
        public int SatisfiedCategoryCount => Categories.Count(category => category.IsMet);
        public bool IsMet => SatisfiedCategoryCount >= RequiredCategoryCount;

        public MutationCategoryInvestmentRequirementProgress(
            MutationTier tier,
            int requiredLevelsPerCategory,
            int requiredCategoryCount,
            IReadOnlyList<MutationCategoryInvestmentProgress> categories)
        {
            Tier = tier;
            RequiredLevelsPerCategory = requiredLevelsPerCategory;
            RequiredCategoryCount = requiredCategoryCount;
            Categories = categories;
        }
    }

    public sealed class MutationCategoryInvestmentProgress
    {
        public MutationCategory Category { get; }
        public int CurrentLevel { get; }
        public int RequiredLevel { get; }
        public bool IsMet => CurrentLevel >= RequiredLevel;

        public MutationCategoryInvestmentProgress(
            MutationCategory category,
            int currentLevel,
            int requiredLevel)
        {
            Category = category;
            CurrentLevel = currentLevel;
            RequiredLevel = requiredLevel;
        }
    }
}
