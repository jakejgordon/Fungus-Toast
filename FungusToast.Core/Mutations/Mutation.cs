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

        /// <summary>
        /// Mutation points required to upgrade a standard (non-surge) mutation by one level.
        /// For surge mutations, upgrading is not possible while active.
        /// </summary>
        public int PointsPerUpgrade { get; private set; }

        /// <summary>
        /// Mutation points required to activate (trigger) this mutation. 
        /// For surge mutations, this is the base activation cost; for normal mutations, this usually matches PointsPerUpgrade.
        /// </summary>
        public int PointsPerActivation { get; private set; }

        /// <summary>
        /// For surge mutations, the additional cost per level (activation cost increases as level increases).
        /// Ignored for standard (non-surge) mutations.
        /// </summary>
        public int PointIncreasePerLevel { get; private set; }

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

        /// <summary>
        /// Indicates whether this is a temporary, activatable surge mutation.
        /// </summary>
        public bool IsSurge { get; private set; }

        /// <summary>
        /// Duration (in rounds) for which a surge mutation remains active after activation.
        /// 0 for standard mutations.
        /// </summary>
        public int SurgeDuration { get; private set; }

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
            MutationTier tier = MutationTier.Tier1,
            bool isSurge = false,
            int surgeDuration = 0,
            int pointsPerActivation = 1,
            int pointIncreasePerLevel = 0 // NEW FIELD for surges
        )
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
            IsSurge = isSurge;
            SurgeDuration = surgeDuration;
            PointsPerActivation = pointsPerActivation;
            PointIncreasePerLevel = pointIncreasePerLevel;

            Prerequisites = new List<MutationPrerequisite>();
            Children = new List<Mutation>();
        }

        /// <summary>
        /// Calculates whether the mutation can be upgraded based on current level.
        /// For surge mutations, this should return false if currently active.
        /// </summary>
        public bool CanUpgrade(int currentLevel, bool isSurgeActive = false)
        {
            if (IsSurge && isSurgeActive)
                return false;
            return currentLevel < MaxLevel;
        }

        /// <summary>
        /// Returns the effect for the given level.
        /// </summary>
        public float GetTotalEffect(int level) => level * EffectPerLevel;

        /// <summary>
        /// For surge mutations, computes the actual mutation point cost for the next activation, based on current level.
        /// </summary>
        public int GetSurgeActivationCost(int currentLevel)
        {
            return PointsPerActivation + (currentLevel * PointIncreasePerLevel);
        }
    }
}
