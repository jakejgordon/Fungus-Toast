using FungusToast.Core;
using FungusToast.Core.Core.Mutations;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public class Mutation
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public string FlavorText { get; private set; } // Optional flavor text for the mutation

        public MutationType Type { get; private set; }
        public float EffectPerLevel { get; private set; }
        public int PointsPerUpgrade { get; private set; }
        public int MaxLevel { get; private set; }
        public MutationCategory Category { get; private set; }
        public MutationTier Tier { get; private set; }

        public int TierNumber => Tier switch
        {
            MutationTier.Tier1 => 1,
            MutationTier.Tier2 => 2,
            MutationTier.Tier3 => 3,
            MutationTier.Tier4 => 4,
            MutationTier.Tier5 => 5,
            MutationTier.Tier6 => 6,
            MutationTier.Tier7 => 7,
            MutationTier.Tier8 => 8,
            MutationTier.Tier9 => 9,
            MutationTier.Tier10 => 10,
            _ => 0
        };

        public List<MutationPrerequisite> Prerequisites { get; private set; }
        public List<Mutation> Children { get; private set; }

        public Mutation(
            int id,
            string name,
            string description,
            string flavorText,
            MutationType type,
            float effectPerLevel,
            int pointsPerUpgrade = 1,
            int maxLevel = 50,
            MutationCategory category = MutationCategory.Growth,
            MutationTier tier = MutationTier.Tier1)
        {
            Id = id;
            Name = name;
            Description = description;
            FlavorText = flavorText;
            Type = type;
            EffectPerLevel = effectPerLevel;
            PointsPerUpgrade = pointsPerUpgrade;
            MaxLevel = maxLevel;
            Category = category;
            Tier = tier;

            Prerequisites = new List<MutationPrerequisite>();
            Children = new List<Mutation>();
        }

        /// <summary>
        /// Calculates whether the mutation can be upgraded based on current level.
        /// </summary>
        public bool CanUpgrade(int currentLevel) => currentLevel < MaxLevel;

        /// <summary>
        /// Returns the effect for the given level.
        /// </summary>
        public float GetTotalEffect(int level) => level * EffectPerLevel;
    }
}
